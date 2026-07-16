#!/bin/bash
set -e

# =========================================
# Office Kiosk - Setup
# Installs the self-updating C# kiosk agent
# (KioskAgent) on a Raspberry Pi. The agent
# launches Chromium in kiosk mode, watches
# its health, ships logs to Better Stack and
# updates itself via Velopack.
# =========================================

# Args / env:
#   $1  BACKEND_URL       (default https://kiosk.coolify.hesamian.com)
#   AGENT_RELEASE_URL     override the AppImage download URL
BACKEND_URL="${1:-${KIOSK_BACKEND_URL:-https://kiosk.coolify.hesamian.com}}"
BACKEND_URL="${BACKEND_URL%/}"
KIOSK_DIR="/opt/kiosk"
AGENT_BIN="$KIOSK_DIR/KioskAgent.AppImage"

# The agent ships one self-contained AppImage per CPU architecture. Pick the
# one that matches this machine (Raspberry Pi = aarch64, most PCs = x86_64).
ARCH="$(uname -m)"
case "$ARCH" in
    aarch64|arm64) AGENT_RID="linux-arm64" ;;
    x86_64|amd64)  AGENT_RID="linux-x64" ;;
    *)
        echo "ERROR: unsupported architecture '$ARCH'."
        echo "       The agent ships for 64-bit ARM (aarch64) and 64-bit x86 (x86_64) only."
        exit 1
        ;;
esac
AGENT_RELEASE_URL="${AGENT_RELEASE_URL:-https://github.com/amir734jj/kiosk/releases/latest/download/KioskAgent-$AGENT_RID.AppImage}"

echo "=== Office Kiosk Setup ==="
echo "Backend URL: $BACKEND_URL"
echo "Agent from:  $AGENT_RELEASE_URL"

# --- Architecture check -------------------------------------------------------
# 32-bit Pi OS can't run the 64-bit AppImage; warn if we somehow got here on one.
if [ "$ARCH" != "aarch64" ] && [ "$ARCH" != "x86_64" ]; then
    echo "WARNING: architecture '$ARCH' is not 64-bit; the agent may fail to start."
fi

# --- Dependencies -------------------------------------------------------------
# The agent shells out to chromium/unclutter/xdotool/xset, so they must be present.
echo "Installing Chromium and utilities..."
sudo apt-get update -qq
# Chromium package name differs across Pi OS releases (chromium vs chromium-browser).
sudo apt-get install -y -qq chromium || sudo apt-get install -y -qq chromium-browser || true
sudo apt-get install -y -qq unclutter xdotool curl x11-xserver-utils
# FUSE is needed to run the AppImage; the package was renamed on Bookworm.
sudo apt-get install -y -qq libfuse2 || sudo apt-get install -y -qq libfuse2t64 || true

# --- Force 1080p resolution (Pi defaults to 4K on capable displays) -----------
BOOT_CONFIG="/boot/firmware/config.txt"
if [ ! -f "$BOOT_CONFIG" ]; then
    BOOT_CONFIG="/boot/config.txt"
fi
if [ -f "$BOOT_CONFIG" ]; then
    echo "Setting display resolution to 1080p..."
    sudo sed -i '/^hdmi_group=/d; /^hdmi_mode=/d; /^hdmi_force_hotplug=/d; /^framebuffer_width=/d; /^framebuffer_height=/d' "$BOOT_CONFIG"
    sudo tee -a "$BOOT_CONFIG" > /dev/null << HDMI
hdmi_force_hotplug=1
hdmi_group=1
hdmi_mode=16
framebuffer_width=1920
framebuffer_height=1080
HDMI
fi

# --- Remove any legacy bash-based kiosk install -------------------------------
# Older setups wrote start.sh/watchdog.sh/launch.sh + cron jobs. Clear them so
# the agent is the single source of truth.
pkill -f watchdog.sh 2>/dev/null || true
(crontab -l 2>/dev/null | grep -v 'apt-get.*chromium' | grep -v 'kiosk') | crontab - 2>/dev/null || true
sudo rm -f "$KIOSK_DIR"/watchdog.sh "$KIOSK_DIR"/launch.sh "$KIOSK_DIR"/restart.sh "$KIOSK_DIR"/url.txt

# --- Install the agent --------------------------------------------------------
sudo mkdir -p "$KIOSK_DIR"
echo "Downloading kiosk agent..."
sudo curl -fSL "$AGENT_RELEASE_URL" -o "$AGENT_BIN"
sudo chmod +x "$AGENT_BIN"

# Backend URL for the agent to fetch its runtime config from.
sudo tee "$KIOSK_DIR/kiosk.env" > /dev/null << ENV
KIOSK_BACKEND_URL=$BACKEND_URL
ENV

# Launcher wrapper: loads env, runs the agent from a writable working dir so it
# can roll log files and stage Velopack updates.
sudo tee "$KIOSK_DIR/start.sh" > /dev/null << 'SCRIPT'
#!/bin/bash
sleep 5
set -a
. /opt/kiosk/kiosk.env
set +a
cd /opt/kiosk
exec /opt/kiosk/KioskAgent.AppImage "$KIOSK_BACKEND_URL"
SCRIPT
sudo chmod +x "$KIOSK_DIR/start.sh"

# The agent runs as the desktop user and writes rolling logs + stages Velopack
# updates inside its working dir, so that user must own /opt/kiosk.
sudo chown -R "$USER":"$USER" "$KIOSK_DIR"

# --- Autostart (LXDE - Raspberry Pi OS) ---------------------------------------
mkdir -p "$HOME/.config/lxsession/LXDE-pi"
if [ -f "$HOME/.config/lxsession/LXDE-pi/autostart" ]; then
    sed -i '/@bash \/opt\/kiosk\/start.sh/d' "$HOME/.config/lxsession/LXDE-pi/autostart"
fi
echo "@bash /opt/kiosk/start.sh" >> "$HOME/.config/lxsession/LXDE-pi/autostart"

# --- Autostart (XDG - other desktops) -----------------------------------------
mkdir -p "$HOME/.config/autostart"
cat > "$HOME/.config/autostart/kiosk.desktop" << EOF
[Desktop Entry]
Type=Application
Name=Office Kiosk
Exec=/opt/kiosk/start.sh
X-GNOME-Autostart-enabled=true
EOF

echo ""
echo "=== Setup complete ==="
echo "  Reboot to start:    sudo reboot"
echo "  Exit kiosk:          pkill KioskAgent"
echo "  Live logs:           tail -f /opt/kiosk/logs/agent-*.json"
echo "  Remote logs:         Better Stack (Application = kiosk-agent)"
echo "  Remove:              ./uninstall.sh"
echo ""
echo "  The agent self-updates via Velopack and reports health to Better Stack."
echo ""
exit 0
