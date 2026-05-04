#!/bin/bash
set -e

# =========================================
# Office Kiosk - Install
# Sets up Chromium in kiosk mode pointing
# to the hosted Blazor display page
# =========================================

APP_URL="${1:-https://kiosk.hesamian.com/display}"
KIOSK_DIR="/opt/kiosk"

echo "=== Office Kiosk Install ==="
echo "Display URL: $APP_URL"

# Install dependencies
echo "Installing Chromium and utilities..."
sudo apt-get update -qq
sudo apt-get install -y -qq chromium-browser unclutter

# Create kiosk launcher script
sudo mkdir -p "$KIOSK_DIR"
sudo tee "$KIOSK_DIR/start.sh" > /dev/null << SCRIPT
#!/bin/bash
sleep 5

xset s off
xset -dpms
xset s noblank

unclutter -idle 0.5 -root &

chromium-browser \\
    --noerrdialogs \\
    --disable-infobars \\
    --kiosk \\
    --disable-translate \\
    --disable-features=TranslateUI \\
    --disable-session-crashed-bubble \\
    --disable-component-update \\
    --no-first-run \\
    --start-fullscreen \\
    --autoplay-policy=no-user-gesture-required \\
    "$APP_URL"
SCRIPT
sudo chmod +x "$KIOSK_DIR/start.sh"

# Save URL for reference
echo "$APP_URL" | sudo tee "$KIOSK_DIR/url.txt" > /dev/null

# Configure autostart (LXDE - Raspberry Pi OS default)
mkdir -p "$HOME/.config/lxsession/LXDE-pi"
if [ -f "$HOME/.config/lxsession/LXDE-pi/autostart" ]; then
    sed -i '/@bash \/opt\/kiosk\/start.sh/d' "$HOME/.config/lxsession/LXDE-pi/autostart"
fi
echo "@bash /opt/kiosk/start.sh" >> "$HOME/.config/lxsession/LXDE-pi/autostart"

# Configure autostart (XDG - other Linux desktops)
mkdir -p "$HOME/.config/autostart"
cat > "$HOME/.config/autostart/kiosk.desktop" << EOF
[Desktop Entry]
Type=Application
Name=Office Kiosk
Exec=/opt/kiosk/start.sh
X-GNOME-Autostart-enabled=true
EOF

echo ""
echo "=== Install complete ==="
echo "  Reboot to start:  sudo reboot"
echo "  Exit kiosk:        Alt+F4 or: pkill chromium"
echo "  Uninstall:         ./uninstall.sh"
