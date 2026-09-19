#!/bin/bash
# Copie Ovomium.dll dans BepInEx/plugins/Ovomium/ du jeu, et les artworks valheim_art/ dans son sous-dossier
# loading/ (LoadingArt). À lancer hors bac à sable.
# Sûr jeu lancé : la DLL est remplacée par renommage atomique, l'ancien fichier (mappé par Mono, qui lit les
# méthodes à la demande) reste intact ; la nouvelle DLL sert au prochain lancement.
# Usage : tools/deploy.sh [--relaunch] [Debug|Release]   (défaut : Release)
#   --relaunch : après la copie, attend la fermeture du jeu s'il tourne (sondage toutes les 5 s), puis le relance
#                via Steam avec l'argument -ovomium-autojoin (reconnexion automatique par le mod).
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
RELAUNCH=0
[ "${1:-}" = "--relaunch" ] && { RELAUNCH=1; shift; }
CONFIG="${1:-Release}"
DLL="$PROJECT/Ovomium/bin/$CONFIG/net48/Ovomium.dll"
DEST="$GAME/BepInEx/plugins/Ovomium"
GAME_PROC="$GAME/valheim.x86_64"

[ -f "$DLL" ] || { echo "DLL absente, lance d'abord tools/build.sh : $DLL" >&2; exit 1; }
[ -d "$GAME/BepInEx" ] || { echo "BepInEx absent dans $GAME, lance d'abord tools/install-bepinex.sh" >&2; exit 1; }
[ "$RELAUNCH" = 0 ] || command -v steam >/dev/null || { echo "steam introuvable dans le PATH" >&2; exit 1; }

# Migration depuis l'ancien nom OvoMiam (0.6.0 et avant) : l'ancienne DLL ne doit pas être chargée en double,
# les réglages et mots de passe sont conservés sous le nouveau nom s'il n'existe pas déjà.
OLD_DEST="$GAME/BepInEx/plugins/OvoMiam"
if [ -d "$OLD_DEST" ]; then
    gio trash "$OLD_DEST"
    echo "Ancien plugin OvoMiam mis à la corbeille : $OLD_DEST"
fi
for f in cfg passwords.txt; do
    OLD_CFG="$GAME/BepInEx/config/ovo.ovomiam.$f"
    NEW_CFG="$GAME/BepInEx/config/ovo.ovomium.$f"
    if [ -f "$OLD_CFG" ] && [ ! -e "$NEW_CFG" ]; then
        mv "$OLD_CFG" "$NEW_CFG"
        echo "Renommé : $OLD_CFG → $NEW_CFG"
    fi
done

mkdir -p "$DEST"
cp "$DLL" "$DEST/Ovomium.dll.new"
mv -f "$DEST/Ovomium.dll.new" "$DEST/Ovomium.dll"
echo "Déployé : $DEST/Ovomium.dll"
if [ -d "$PROJECT/valheim_art" ]; then
    rsync -a "$PROJECT/valheim_art/" "$DEST/loading/"
    echo "Artworks : $(ls "$DEST/loading" | wc -l) fichier(s) dans $DEST/loading/"
fi

[ "$RELAUNCH" = 1 ] || exit 0
if pgrep -f "$GAME_PROC" >/dev/null; then
    echo "Valheim est lancé : en attente de la fermeture du jeu…"
    while pgrep -f "$GAME_PROC" >/dev/null; do sleep 5; done
    sleep 3  # laisse Steam constater la fin du jeu, sinon -applaunch peut être ignoré
fi
echo "Relance de Valheim via Steam (-ovomium-autojoin)…"
setsid nohup steam -applaunch 892970 -ovomium-autojoin >/dev/null 2>&1 &
