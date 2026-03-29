#!/bin/bash

# KBMS Multi-Platform Packaging Script
# Version: 3.4.0
# Support: macOS, Linux, Windows

set -e

VERSION="3.4.0"
ROOT_DIR=$(pwd)
RELEASE_DIR="$ROOT_DIR/releases"
RIDS=("osx-arm64" "linux-x64" "win-x64")

echo "Starting KBMS Multi-Platform Packaging (v$VERSION)..."

# 1. Environment Check
echo "[1/4] Checking environment..."
command -v dotnet >/dev/null 2>&1 || { echo >&2 "Error: .NET SDK is required."; exit 1; }
command -v node >/dev/null 2>&1 || { echo >&2 "Error: NodeJS is required."; exit 1; }
command -v npm >/dev/null 2>&1 || { echo >&2 "Error: npm is required."; exit 1; }
command -v zip >/dev/null 2>&1 || { echo >&2 "Error: zip utility is required."; exit 1; }

# 2. Cleanup
echo "[2/4] Cleaning up old releases..."
rm -rf "$RELEASE_DIR"
rm -rf "$ROOT_DIR/kbms-studio/release"
mkdir -p "$RELEASE_DIR"

# 3. Build & Package Server & CLI (for all RIDs)
echo "[3/4] Building .NET Components (Server & CLI)..."

for RID in "${RIDS[@]}"; do
    echo "Processing Platform: $RID"
    
    # 3.1. Build Server
    echo "  Building Server for $RID..."
    dotnet publish KBMS.Server/KBMS.Server.csproj -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o "$ROOT_DIR/temp_server"
    cp "$ROOT_DIR/LICENSE" "$ROOT_DIR/temp_server/"
    cd "$ROOT_DIR/temp_server"
    zip -rq "$RELEASE_DIR/KBMS_Server_v$VERSION""_$RID.zip" .
    cd "$ROOT_DIR"
    rm -rf "$ROOT_DIR/temp_server"

    # 3.2. Build CLI
    echo "  Building CLI for $RID..."
    dotnet publish KBMS.CLI/KBMS.CLI.csproj -c Release -r $RID --self-contained true -p:PublishSingleFile=true -o "$ROOT_DIR/temp_cli"
    cp "$ROOT_DIR/LICENSE" "$ROOT_DIR/temp_cli/"
    cd "$ROOT_DIR/temp_cli"
    zip -rq "$RELEASE_DIR/KBMS_CLI_v$VERSION""_$RID.zip" .
    cd "$ROOT_DIR"
    rm -rf "$ROOT_DIR/temp_cli"
    
    echo "  Completed $RID"
done

# 4. Build & Package KBMS Studio (Multi-platform)
echo "[4/4] Building KBMS Studio (macOS, Linux, Windows)..."
cp "$ROOT_DIR/LICENSE" "$ROOT_DIR/kbms-studio/"
cd "$ROOT_DIR/kbms-studio"
npm install
npm version "$VERSION" --no-git-tag-version || true
npm run build

# Build for all targets
# --mac: zip, dmg
# --linux: AppImage, deb, rpm
# --win: exe, zip
npx electron-builder build --mac --linux --win --publish never

# Copy Studio artifacts to root releases folder
echo "Organizing Studio artifacts..."
cd "$ROOT_DIR/kbms-studio/release"
cp *.dmg "$RELEASE_DIR/" 2>/dev/null || true
cp *.exe "$RELEASE_DIR/" 2>/dev/null || true
cp *.deb "$RELEASE_DIR/" 2>/dev/null || true
cp *.AppImage "$RELEASE_DIR/" 2>/dev/null || true
cp *.zip "$RELEASE_DIR/" 2>/dev/null || true
cd "$ROOT_DIR"

# Finalize
echo "--------------------------------------------------"
echo "Multi-Platform Build completed successfully!"
echo "Artifacts are ready in: $RELEASE_DIR"
ls -lh "$RELEASE_DIR"
echo "--------------------------------------------------"
