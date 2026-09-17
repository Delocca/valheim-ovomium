#!/bin/bash
# Copie OvoMiam.dll dans BepInEx/plugins/OvoMiam/ du jeu, et les artworks valheim_art/ dans son sous-dossier
# loading/ (LoadingArt). À lancer hors bac à sable.
# Usage : tools/deploy.sh [--wait] [Debug|Release]   (défaut : Release)
#   --wait : si le jeu tourne, attend sa fermeture (sondage toutes les 5 s) au lieu de refuser.
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
WAIT=0
[ "${1:-}" = "--wait" ] && { WAIT=1; shift; }
CONFIG="${1:-Release}"
DLL="$PROJECT/OvoMiam/bin/$CONFIG/net48/OvoMiam.dll"
DEST="$GAME/BepInEx/plugins/OvoMiam"

[ -f "$DLL" ] || { echo "DLL absente, lance d'abord tools/build.sh : $DLL" >&2; exit 1; }
[ -d "$GAME/BepInEx" ] || { echo "BepInEx absent dans $GAME, lance d'abord tools/install-bepinex.sh" >&2; exit 1; }

# Mono lit les corps de méthodes à la demande dans le fichier : remplacer la DLL pendant que le jeu tourne
# provoque des « BadImageFormatException: Method has zero rva » dans les patches pas encore exécutés.
if pgrep -f "$GAME/valheim.x86_64" >/dev/null; then
    if [ "$WAIT" = 1 ]; then
        echo "Valheim est lancé : déploiement dès sa fermeture…"
        while pgrep -f "$GAME/valheim.x86_64" >/dev/null; do sleep 5; done
    else
        echo "Valheim est lancé : ferme-le avant de déployer (sinon la DLL en cours d'exécution est corrompue)." >&2
        exit 1
    fi
fi

mkdir -p "$DEST"
cp "$DLL" "$DEST/"
echo "Déployé : $DEST/OvoMiam.dll"
if [ -d "$PROJECT/valheim_art" ]; then
    rsync -a "$PROJECT/valheim_art/" "$DEST/loading/"
    echo "Artworks : $(ls "$DEST/loading" | wc -l) fichier(s) dans $DEST/loading/"
fi
