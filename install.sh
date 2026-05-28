#!/bin/bash
set -e

# KBMS Core Installer for macOS and Linux
# Usage: curl -sSL https://raw.githubusercontent.com/tranphat1506/KBMS/main/install.sh | sudo bash

echo "========================================="
echo " KBMS Core Installer"
echo "========================================="

# Detect OS and Arch
OS="$(uname -s)"
ARCH="$(uname -m)"

if [ "$OS" = "Darwin" ]; then
    OS_NAME="osx"
elif [ "$OS" = "Linux" ]; then
    OS_NAME="linux"
else
    echo "Unsupported OS: $OS"
    exit 1
fi

if [ "$ARCH" = "x86_64" ]; then
    ARCH_NAME="x64"
elif [ "$ARCH" = "arm64" ] || [ "$ARCH" = "aarch64" ]; then
    ARCH_NAME="arm64"
else
    echo "Unsupported Architecture: $ARCH"
    exit 1
fi

RID="${OS_NAME}-${ARCH_NAME}"
VERSION="3.5.0"
REPO="tranphat1506/KBMS"

echo "[1/4] Detected Platform: $RID"

# Check root
if [ "$EUID" -ne 0 ]; then
  echo "Error: Please run this script as root."
  echo "Use: curl -sSL https://raw.githubusercontent.com/tranphat1506/KBMS/main/install.sh | sudo bash"
  exit 1
fi

INSTALL_DIR="/opt/kbms"
DATA_DIR="/var/lib/kbms/data"
BIN_DIR="/usr/local/bin"

echo "[2/4] Downloading KBMS Core v$VERSION..."
mkdir -p /tmp/kbms_install
cd /tmp/kbms_install

SERVER_ZIP="KBMS_Server_v${VERSION}_${RID}.zip"
CLI_ZIP="KBMS_CLI_v${VERSION}_${RID}.zip"

SERVER_URL="https://github.com/${REPO}/releases/download/v${VERSION}/${SERVER_ZIP}"
CLI_URL="https://github.com/${REPO}/releases/download/v${VERSION}/${CLI_ZIP}"

# Download from GitHub
echo "Downloading $SERVER_ZIP..."
curl -sSL -O "$SERVER_URL"
echo "Downloading $CLI_ZIP..."
curl -sSL -O "$CLI_URL"

echo "[3/4] Installing to $INSTALL_DIR..."
mkdir -p "$INSTALL_DIR"
mkdir -p "$DATA_DIR"

unzip -q -o "$SERVER_ZIP" -d "$INSTALL_DIR/server"
rm -f "$INSTALL_DIR/server/kbms.ini"
unzip -q -o "$CLI_ZIP" -d "$INSTALL_DIR/cli"

# Setup config
CONFIG_FILE="/etc/kbms/kbms.ini"
mkdir -p /etc/kbms
cat <<EOF > "$CONFIG_FILE"
[Server]
host=127.0.0.1
port=3307
data_dir=$DATA_DIR
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

# Ensure executable permissions
chmod +x "$INSTALL_DIR/server/KBMS.Server" 2>/dev/null || true
chmod +x "$INSTALL_DIR/cli/KBMS.CLI" 2>/dev/null || true

echo "[4/5] Creating global aliases..."
ln -sf "$INSTALL_DIR/server/KBMS.Server" "$BIN_DIR/kbms-server"
ln -sf "$INSTALL_DIR/cli/KBMS.CLI" "$BIN_DIR/kbms-cli"

echo "[5/5] Registering KBMS as a System Service..."
if [ "$OS" = "Darwin" ]; then
    PLIST_FILE="/Library/LaunchDaemons/com.thingent.kbms.plist"
    cat <<EOF > "$PLIST_FILE"
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
        <string>$INSTALL_DIR/server/KBMS.Server</string>
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
    sysadminctl -addUser _kbms -home /var/empty -shell /usr/bin/false 2>/dev/null || true
    chown -R _kbms /var/lib/kbms
    chown -R _kbms /etc/kbms
    launchctl load -w "$PLIST_FILE" || true
    echo "Service registered with launchd (macOS) under user _kbms."
elif [ "$OS" = "Linux" ]; then
    SERVICE_FILE="/etc/systemd/system/kbms.service"
    cat <<EOF > "$SERVICE_FILE"
[Unit]
Description=KBMS Server Daemon
After=network.target

[Service]
ExecStart=$INSTALL_DIR/server/KBMS.Server
Restart=always
User=kbms

[Install]
WantedBy=multi-user.target
EOF
    useradd -r -s /bin/false kbms 2>/dev/null || true
    chown -R kbms:kbms /var/lib/kbms
    chown -R kbms:kbms /etc/kbms
    systemctl daemon-reload || true
    systemctl enable kbms || true
    systemctl start kbms || true
    echo "Service registered with systemd (Linux) under user kbms."
fi

# Cleanup
rm -rf /tmp/kbms_install

echo "========================================="
echo " KBMS Core has been successfully installed!"
echo " Configuration: $CONFIG_FILE"
echo " Data Directory: $DATA_DIR"
echo ""
echo " Try running: kbms-cli --help"
echo "========================================="
