#!/bin/bash
set -e

# =========================================
# Office Kiosk - Publish the agent
# Builds the KioskAgent as a Velopack release
# (AppImage + update feed) for the Raspberry Pi.
#
# Run this on a dev/CI machine, NOT the Pi.
#
# Prereqisites:
#   dotnet SDK 10
#   Velopack CLI:  dotnet tool install -g vpk
#
# Usage:
#   ./publish-agent.sh <version> [runtime]
#   ./publish-agent.sh 1.0.0
#   ./publish-agent.sh 1.0.1 linux-arm64
#
# Output (./releases/agent) must be served at:
#   <BACKEND_URL>/releases/agent/
# so both first-install (setup.sh) and self-updates (Velopack) can reach it.
# =========================================

VERSION="${1:?Usage: ./publish-agent.sh <version> [runtime]}"
RUNTIME="${2:-linux-arm64}"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
PUBLISH_DIR="$REPO_ROOT/Agent/bin/publish/$RUNTIME"
RELEASE_DIR="$REPO_ROOT/releases/agent"

echo "=== Publishing KioskAgent $VERSION ($RUNTIME) ==="

# 1. Framework-dependent? No — self-contained so the Pi needs no dotnet runtime.
dotnet publish "$REPO_ROOT/Agent/Agent.csproj" \
    -c Release \
    -r "$RUNTIME" \
    --self-contained true \
    -o "$PUBLISH_DIR"

# 2. Pack into a Velopack Linux release (AppImage + delta feed).
vpk pack \
    --packId KioskAgent \
    --packVersion "$VERSION" \
    --packDir "$PUBLISH_DIR" \
    --mainExe KioskAgent \
    --runtime "$RUNTIME" \
    --outputDir "$RELEASE_DIR"

# 3. Provide a stable download name for first-time installs (setup.sh).
APPIMAGE="$(ls -1 "$RELEASE_DIR"/*.AppImage 2>/dev/null | head -n1 || true)"
if [ -n "$APPIMAGE" ]; then
    cp -f "$APPIMAGE" "$RELEASE_DIR/KioskAgent.AppImage"
fi

echo ""
echo "=== Done ==="
echo "  Artifacts: $RELEASE_DIR"
echo "  Serve them at <BACKEND_URL>/releases/agent/ and run setup.sh on the Pi."
echo ""
