#!/bin/bash
# Génère build/Installer-PC-VR.bat depuis installer/Installer-PC-VR.bat.in : l'identifiant Syncthing de ce PC Linux
# y est embarqué, fins de ligne converties en CRLF (exigé par cmd). Fichier unique à porter sur le PC VR Windows.
# Appelé par tools/pc-vr-link.sh ; lançable seul (aucun accès réseau).
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
TEMPLATE="$PROJECT/installer/Installer-PC-VR.bat.in"
OUT="$PROJECT/build/Installer-PC-VR.bat"

command -v syncthing >/dev/null || { echo "syncthing introuvable : installe-le d'abord sur ce PC." >&2; exit 1; }
LINUX_ID="$(syncthing device-id)"
[[ "$LINUX_ID" =~ ^[A-Z0-9]{7}(-[A-Z0-9]{7}){7}$ ]] || { echo "Identifiant Syncthing inattendu : « $LINUX_ID »" >&2; exit 1; }
grep -q '@@LINUX_DEVICE_ID@@' "$TEMPLATE" || { echo "Marqueur @@LINUX_DEVICE_ID@@ absent de $TEMPLATE" >&2; exit 1; }

mkdir -p "$(dirname "$OUT")"
sed -e "s/@@LINUX_DEVICE_ID@@/$LINUX_ID/g" -e 's/\r$//' -e 's/$/\r/' "$TEMPLATE" > "$OUT.part"
mv -f "$OUT.part" "$OUT"
echo "Généré : $OUT (appareil Linux $LINUX_ID)"
