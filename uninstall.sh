#!/bin/bash
# KBMS Uninstaller Script for macOS and Linux
# Run with: curl -sSL https://raw.githubusercontent.com/tranphat1506/KBMS/main/uninstall.sh | sudo bash

set -e

if [ "$EUID" -ne 0 ]; then
  echo "Please run as root (use sudo)"
  exit 1
fi

OS=$(uname -s)
echo "Starting KBMS Uninstallation on $OS..."

# 1. Stop and Remove Services
if [ "$OS" = "Darwin" ]; then
    PLIST_FILE="/Library/LaunchDaemons/com.thingent.kbms.plist"
    if [ -f "$PLIST_FILE" ]; then
        echo "Stopping macOS LaunchDaemon..."
        launchctl unload -w "$PLIST_FILE" || true
        rm -f "$PLIST_FILE"
    fi
elif [ "$OS" = "Linux" ]; then
    SERVICE_FILE="/etc/systemd/system/kbms.service"
    if [ -f "$SERVICE_FILE" ]; then
        echo "Stopping Linux Systemd Service..."
        systemctl stop kbms || true
        systemctl disable kbms || true
        rm -f "$SERVICE_FILE"
        systemctl daemon-reload || true
    fi
fi

# 2. Remove Global Aliases
echo "Removing global aliases..."
rm -f /usr/local/bin/kbms-server
rm -f /usr/local/bin/kbms-cli

# 3. Remove Core Binaries and Config
echo "Removing core binaries (/opt/kbms) and configuration (/etc/kbms)..."
rm -rf /opt/kbms
rm -rf /etc/kbms

# 4. Data Directory Warning
DATA_DIR="/var/lib/kbms"
if [ -d "$DATA_DIR" ]; then
    echo "============================================================"
    echo "WARNING: Your database files are still located in $DATA_DIR"
    echo "To prevent accidental data loss, this script did NOT delete them."
    echo "If you want to completely wipe all data, manually run:"
    echo "sudo rm -rf $DATA_DIR"
    echo "============================================================"
fi

echo "KBMS has been successfully uninstalled!"
