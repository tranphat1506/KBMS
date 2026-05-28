#!/bin/bash
set -e

echo "========================================="
echo " KBMS Linux DEB Builder"
echo "========================================="

VERSION="3.5.0"
ARCH="amd64"
OUTPUT_DEB="releases/kbms-server_${VERSION}_${ARCH}.deb"
PAYLOAD_DIR="/tmp/kbms_deb_payload"

# Clean up
rm -rf "$PAYLOAD_DIR"
mkdir -p "$PAYLOAD_DIR/opt/kbms/server"
mkdir -p "$PAYLOAD_DIR/opt/kbms/cli"
mkdir -p "$PAYLOAD_DIR/etc/kbms"
mkdir -p "$PAYLOAD_DIR/usr/local/bin"
mkdir -p "$PAYLOAD_DIR/DEBIAN"

echo "Extracting Linux binaries from releases..."
unzip -q -o "releases/KBMS_Server_v${VERSION}_linux-x64.zip" -d "$PAYLOAD_DIR/opt/kbms/server"
unzip -q -o "releases/KBMS_CLI_v${VERSION}_linux-x64.zip" -d "$PAYLOAD_DIR/opt/kbms/cli"
rm -f "$PAYLOAD_DIR/opt/kbms/server/kbms.ini"

mkdir -p "$PAYLOAD_DIR/etc/systemd/system"
cat <<EOF > "$PAYLOAD_DIR/etc/systemd/system/kbms.service"
[Unit]
Description=KBMS Server Daemon
After=network.target

[Service]
ExecStart=/opt/kbms/server/KBMS.Server
Restart=always
User=root

[Install]
WantedBy=multi-user.target
EOF

cat <<EOF > "$PAYLOAD_DIR/DEBIAN/control"
Package: kbms-core
Version: $VERSION
Section: database
Priority: optional
Architecture: $ARCH
Maintainer: Tran Phat <tranphat1506@github.com>
Description: KBMS Server and CLI Core
 A highly performant Concept-Oriented Knowledge Base Management System.
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

# Create postinst script to create symlinks
cat <<EOF > "$PAYLOAD_DIR/DEBIAN/postinst"
#!/bin/bash
ln -sf /opt/kbms/server/KBMS.Server /usr/local/bin/kbms-server
ln -sf /opt/kbms/cli/KBMS.CLI /usr/local/bin/kbms-cli
mkdir -p /var/lib/kbms/data
chmod -R 755 /opt/kbms

# Start Systemd Service
systemctl daemon-reload || true
systemctl enable kbms || true
systemctl start kbms || true
exit 0
EOF
chmod +x "$PAYLOAD_DIR/DEBIAN/postinst"

echo "Building package using dpkg-deb..."
if command -v dpkg-deb >/dev/null 2>&1; then
    dpkg-deb --build "$PAYLOAD_DIR" "$OUTPUT_DEB"
    echo "Success! DEB saved to $OUTPUT_DEB"
else
    echo "Warning: dpkg-deb not found. You need a Debian/Ubuntu system to build the package."
fi

rm -rf "$PAYLOAD_DIR"
