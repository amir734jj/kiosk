#!/bin/bash
set -e

# =========================================
# Office Kiosk - Setup
# Installs Chromium kiosk mode on a
# Raspberry Pi pointing to the display URL
# =========================================

APP_URL="${1:-https://kiosk.hesamian.com/display}"
KIOSK_DIR="/opt/kiosk"

echo "=== Office Kiosk Setup ==="
echo "Display URL: $APP_URL"

# Install dependencies
echo "Installing Chromium and utilities..."
sudo apt-get update -qq
sudo apt-get install -y -qq chromium-browser unclutter

# Create kiosk directory
sudo mkdir -p "$KIOSK_DIR"

# Save URL for reference
echo "$APP_URL" | sudo tee "$KIOSK_DIR/url.txt" > /dev/null

# Create kiosk launcher
sudo tee "$KIOSK_DIR/start.sh" > /dev/null << SCRIPT
#!/bin/bash
sleep 5

# Disable screen blanking
xset s off
xset -dpms
xset s noblank

# Hide mouse cursor
unclutter -idle 0.5 -root &

# Launch Chromium in kiosk mode
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

# Add cron job for weekly Chromium update + reboot (Sunday 3am)
(crontab -l 2>/dev/null | grep -v 'apt-get.*chromium'; echo "0 3 * * 0 sudo apt-get update -qq && sudo apt-get upgrade -y -qq chromium-browser && sudo reboot") | crontab -

# Configure autostart (LXDE - Raspberry Pi OS)
mkdir -p "$HOME/.config/lxsession/LXDE-pi"
if [ -f "$HOME/.config/lxsession/LXDE-pi/autostart" ]; then
    sed -i '/@bash \/opt\/kiosk\/start.sh/d' "$HOME/.config/lxsession/LXDE-pi/autostart"
fi
echo "@bash /opt/kiosk/start.sh" >> "$HOME/.config/lxsession/LXDE-pi/autostart"

# Configure autostart (XDG - other desktops)
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
echo "  Reboot to start:   sudo reboot"
echo "  Exit kiosk:         Alt+F4 or: pkill chromium"
echo "  Remove:             ./uninstall.sh"
echo ""
echo "  Page auto-refreshes every 5 minutes via meta tag"
echo ""
exit 0
