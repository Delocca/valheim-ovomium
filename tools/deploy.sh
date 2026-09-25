#!/bin/bash
# Copie le chargeur Ovomium.dll et le cœur Ovomium.Core.dll (+ .pdb) dans BepInEx/plugins/Ovomium/ du jeu, le
# patcher Ovomium.Updater.dll dans BepInEx/patchers/ (pris en compte à la relance), et les artworks valheim_art/ dans
# le sous-dossier loading/ du plugin (LoadingArt). À lancer hors bac à sable.
# Sûr jeu lancé : chaque fichier est remplacé par renommage atomique ; le chargeur, mappé par Mono (qui lit les
# méthodes à la demande), reste intact et le nouveau sert au prochain lancement ; le cœur est lu depuis ses octets.
# Usage : tools/deploy.sh [--build] [--dev] [--relaunch] [Debug|Release]   (défaut : Release)
#   --build    : compile d'abord (tools/build.sh), en une seule commande hors bac à sable.
#   --dev      : rechargement à chaud : pose le marqueur plugins/Ovomium/dev-reload, le chargeur redémarre alors le
#                cœur dès que la date d'Ovomium.Core.dll change (sondage chaque seconde, F6 en secours), même en
#                partie. Sans --dev, le marqueur est retiré (mode normal, à garder pour le test final avant release).
#   --relaunch : après la copie, attend la fermeture du jeu s'il tourne (sondage toutes les 5 s), puis le relance
#                via Steam avec l'argument -ovomium-autojoin (reconnexion automatique par le mod).
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
DEV=0
RELAUNCH=0
BUILD=0
while [ $# -gt 0 ]; do
    case "$1" in
        --build) BUILD=1; shift ;;
        --dev) DEV=1; shift ;;
        --relaunch) RELAUNCH=1; shift ;;
        *) break ;;
    esac
done
CONFIG="${1:-Release}"
LOADER="$PROJECT/Ovomium.Loader/bin/$CONFIG/net48/Ovomium.dll"
CORE="$PROJECT/Ovomium/bin/$CONFIG/net48/Ovomium.Core.dll"
PATCHER="$PROJECT/Ovomium.Updater/bin/$CONFIG/net48/Ovomium.Updater.dll"
PATCHERS="$GAME/BepInEx/patchers"
DEST="$GAME/BepInEx/plugins/Ovomium"
GAME_PROC="$GAME/valheim.x86_64"

[ "$BUILD" = 0 ] || "$PROJECT/tools/build.sh" "$CONFIG"
for f in "$LOADER" "$CORE" "${CORE%.dll}.pdb" "$PATCHER"; do
    [ -f "$f" ] || { echo "Absent, lance d'abord tools/build.sh : $f" >&2; exit 1; }
done
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

# Mise à jour en attente (Updater, ovomium_updatetest) : le patcher l'installerait au lancement par-dessus ce
# déploiement, chargeur compris.
if [ -d "$DEST/update" ]; then
    gio trash "$DEST/update"
    echo "Mise à jour en attente mise à la corbeille : $DEST/update"
fi

# Copie par renommage atomique : jamais de fichier à moitié écrit, ni pour Mono ni pour le sondage du chargeur.
# Le .pdb avant le cœur : le chargeur relit les deux dès que la date du cœur change.
install_atomic() {
    cp "$1" "$2.new"
    mv -f "$2.new" "$2"
}
mkdir -p "$DEST"
install_atomic "$LOADER" "$DEST/Ovomium.dll"
install_atomic "${CORE%.dll}.pdb" "$DEST/Ovomium.Core.pdb"
install_atomic "$CORE" "$DEST/Ovomium.Core.dll"
echo "Déployé : $DEST/Ovomium.dll (chargeur, actif à la relance) et Ovomium.Core.dll"
if [ "$DEV" = 1 ]; then
    touch "$DEST/dev-reload"
    echo "Mode dev : rechargement à chaud actif (marqueur $DEST/dev-reload)"
elif [ -f "$DEST/dev-reload" ]; then
    rm -f "$DEST/dev-reload"
    echo "Mode normal : marqueur dev-reload retiré"
fi
mkdir -p "$PATCHERS"
cp "$PATCHER" "$PATCHERS/Ovomium.Updater.dll.new"
mv -f "$PATCHERS/Ovomium.Updater.dll.new" "$PATCHERS/Ovomium.Updater.dll"
echo "Patcher : $PATCHERS/Ovomium.Updater.dll (actif à la relance)"
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
