#!/bin/bash
set -e

# =========================================
# Office Kiosk - Uninstall
# =========================================

KIOSK_DIR="/opt/kiosk"

echo "=== Office Kiosk Uninstall ==="

pkill -f "chromium.*kiosk" 2>/dev/null || true
pkill unclutter 2>/dev/null || true

# Remove cron jobs
(crontab -l 2>/dev/null | grep -v 'apt-get.*chromium') | crontab - 2>/dev/null || true

# Remove autostart
rm -f "$HOME/.config/autostart/kiosk.desktop"
if [ -f "$HOME/.config/lxsession/LXDE-pi/autostart" ]; then
    sed -i '/@bash \/opt\/kiosk\/start.sh/d' "$HOME/.config/lxsession/LXDE-pi/autostart"
fi

# Remove kiosk files
sudo rm -rf "$KIOSK_DIR"

echo "=== Uninstall complete ==="
