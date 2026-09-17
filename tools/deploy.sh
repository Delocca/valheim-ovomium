#!/bin/bash
# Copie OvoMiam.dll dans BepInEx/plugins/ du jeu (à lancer hors bac à sable).
# Usage : tools/deploy.sh [Debug|Release]   (défaut : Release)
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
CONFIG="${1:-Release}"
DLL="$PROJECT/OvoMiam/bin/$CONFIG/net48/OvoMiam.dll"
DEST="$GAME/BepInEx/plugins/OvoMiam"

[ -f "$DLL" ] || { echo "DLL absente, lance d'abord tools/build.sh : $DLL" >&2; exit 1; }
[ -d "$GAME/BepInEx" ] || { echo "BepInEx absent dans $GAME, lance d'abord tools/install-bepinex.sh" >&2; exit 1; }

# Mono lit les corps de méthodes à la demande dans le fichier : remplacer la DLL pendant que le jeu tourne
# provoque des « BadImageFormatException: Method has zero rva » dans les patches pas encore exécutés.
if pgrep -f "$GAME/valheim.x86_64" >/dev/null; then
    echo "Valheim est lancé : ferme-le avant de déployer (sinon la DLL en cours d'exécution est corrompue)." >&2
    exit 1
fi

mkdir -p "$DEST"
cp "$DLL" "$DEST/"
echo "Déployé : $DEST/OvoMiam.dll"
