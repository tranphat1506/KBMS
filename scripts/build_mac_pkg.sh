#!/bin/bash
set -e

echo "========================================="
echo " KBMS macOS PKG Builder"
echo "========================================="

VERSION="3.5.0"
OUTPUT_PKG="releases/KBMS_Core_v${VERSION}_macOS.pkg"
PAYLOAD_DIR="/tmp/kbms_pkg_payload"
SCRIPTS_DIR="/tmp/kbms_pkg_scripts"

# Clean up
rm -rf "$PAYLOAD_DIR" "$SCRIPTS_DIR"
mkdir -p "$PAYLOAD_DIR/opt/kbms/server"
mkdir -p "$PAYLOAD_DIR/opt/kbms/cli"
mkdir -p "$PAYLOAD_DIR/etc/kbms"
mkdir -p "$SCRIPTS_DIR"

echo "Extracting macOS binaries from releases..."
unzip -q -o "releases/KBMS_Server_v${VERSION}_osx-arm64.zip" -d "$PAYLOAD_DIR/opt/kbms/server"
unzip -q -o "releases/KBMS_CLI_v${VERSION}_osx-arm64.zip" -d "$PAYLOAD_DIR/opt/kbms/cli"
rm -f "$PAYLOAD_DIR/opt/kbms/server/kbms.ini"

mkdir -p "$PAYLOAD_DIR/Library/LaunchDaemons"
cat <<EOF > "$PAYLOAD_DIR/Library/LaunchDaemons/com.thingent.kbms.plist"
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
    <key>Label</key>
    <string>com.thingent.kbms</string>
    <key>UserName</key>
    <string>_kbms</string>
    <key>ProgramArguments</key>
    <array>
        <string>/opt/kbms/server/KBMS.Server</string>
    </array>
    <key>RunAtLoad</key>
    <true/>
    <key>KeepAlive</key>
    <true/>
    <key>StandardErrorPath</key>
    <string>/var/lib/kbms/kbms-error.log</string>
    <key>StandardOutPath</key>
    <string>/var/lib/kbms/kbms-output.log</string>
</dict>
</plist>
EOF

cat <<EOF > "$PAYLOAD_DIR/etc/kbms/kbms.ini"
[Server]
host=127.0.0.1
port=3307
data_dir=/var/lib/kbms/data
max_connections=100
version=$VERSION
master_key=KBMS_V3_MASTER_SECRET_2026

[Root]
username=root
password=root

[Settings]
default_timeout=60
enable_audit_logs=true
EOF

# Create postinstall script to create symlinks
cat <<EOF > "$SCRIPTS_DIR/postinstall"
#!/bin/bash
mkdir -p /usr/local/bin
ln -sf /opt/kbms/server/KBMS.Server /usr/local/bin/kbms-server
ln -sf /opt/kbms/cli/KBMS.CLI /usr/local/bin/kbms-cli
mkdir -p /var/lib/kbms/data
chmod -R 755 /opt/kbms

# Create dedicated _kbms user if not exists
sysadminctl -addUser _kbms -home /var/empty -shell /usr/bin/false 2>/dev/null || true

# Grant ownership to the daemon user
chown -R _kbms /var/lib/kbms
chown -R _kbms /etc/kbms

# Start LaunchDaemon Service
launchctl load -w /Library/LaunchDaemons/com.thingent.kbms.plist || true
exit 0
EOF
chmod +x "$SCRIPTS_DIR/postinstall"

echo "Building package using pkgbuild..."
pkgbuild --root "$PAYLOAD_DIR" \
         --identifier com.thingent.kbms.core \
         --version "$VERSION" \
         --scripts "$SCRIPTS_DIR" \
         --install-location "/" \
         "$OUTPUT_PKG"

rm -rf "$PAYLOAD_DIR" "$SCRIPTS_DIR"

echo "Success! PKG saved to $OUTPUT_PKG"
