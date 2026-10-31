#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
APP_DIR="$HOME/.local/share/sargon"
BIN_DIR="$HOME/.local/bin"
APP_LAUNCHER="$BIN_DIR/sargon"

install_packages() {
  if ! command -v clamscan >/dev/null 2>&1 || ! python3 -c 'import tkinter' >/dev/null 2>&1; then
    if command -v apt-get >/dev/null 2>&1; then
      sudo apt-get update
      sudo apt-get install -y python3 clamav clamav-freshclam python3-tk policykit-1
    elif command -v dnf >/dev/null 2>&1; then
      sudo dnf install -y python3 clamav clamav-update python3-tkinter polkit
    elif command -v pacman >/dev/null 2>&1; then
      sudo pacman -S --needed python clamav tk polkit
    elif command -v zypper >/dev/null 2>&1; then
      sudo zypper install -y python3 clamav python3-tk polkit
    else
      echo "Nepavyko nustatyti paketų tvarkyklės. Įdiekite ClamAV, Python 3, Tkinter ir PolicyKit rankiniu būdu." >&2
      exit 1
    fi
  fi
}

install_packages
mkdir -p "$APP_DIR" "$BIN_DIR" "$HOME/.local/share/applications" "$HOME/.local/share/icons/hicolor/scalable/apps"
install -m 0644 "$SCRIPT_DIR/sargon.py" "$APP_DIR/sargon.py"
install -m 0644 "$SCRIPT_DIR/scanner.py" "$APP_DIR/scanner.py"
install -m 0644 "$SCRIPT_DIR/sargon.svg" "$HOME/.local/share/icons/hicolor/scalable/apps/sargon.svg"

cat > "$APP_LAUNCHER" <<'LAUNCHER'
#!/usr/bin/env bash
set -euo pipefail
exec python3 "$HOME/.local/share/sargon/sargon.py" "$@"
LAUNCHER
chmod 0755 "$APP_LAUNCHER"

cat > "$HOME/.local/share/applications/sargon.desktop" <<DESKTOP
[Desktop Entry]
Type=Application
Name=SARGON Security
Comment=ClamAV failų patikros sąsaja
Exec="$APP_LAUNCHER"
TryExec=$APP_LAUNCHER
Icon=$HOME/.local/share/icons/hicolor/scalable/apps/sargon.svg
Terminal=false
Categories=System;Security;
DESKTOP

echo "SARGON įdiegtas. Paleiskite jį programų meniu arba komanda: $APP_LAUNCHER"
echo "Pirmą kartą atverkite „Atnaujinti bazę“, kad gautumėte ClamAV parašus."
