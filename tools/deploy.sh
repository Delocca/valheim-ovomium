#!/bin/bash
# Copie Ovomium.dll dans BepInEx/plugins/Ovomium/ du jeu, le patcher Ovomium.Updater.dll dans BepInEx/patchers/
# (pris en compte à la relance), et les artworks valheim_art/ dans le sous-dossier loading/ du plugin (LoadingArt).
# À lancer hors bac à sable.
# Sûr jeu lancé : la DLL est remplacée par renommage atomique, l'ancien fichier (mappé par Mono, qui lit les
# méthodes à la demande) reste intact ; la nouvelle DLL sert au prochain lancement.
# Usage : tools/deploy.sh [--build] [--dev] [--relaunch] [Debug|Release]   (défaut : Release)
#   --build    : compile d'abord (tools/build.sh), en une seule commande hors bac à sable.
#   --dev      : rechargement à chaud (ScriptEngine, tools/install-scriptengine.sh) : la DLL va dans BepInEx/scripts/
#                et celle de plugins/Ovomium/ est retirée (sinon double chargement) ; ScriptEngine la recharge
#                automatiquement ~3 s après la copie (ou F6 en jeu). Sans --dev, retour au mode normal.
#                Changer de mode exige une relance du jeu (la DLL déjà chargée reste active).
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
DLL="$PROJECT/Ovomium/bin/$CONFIG/net48/Ovomium.dll"
PATCHER="$PROJECT/Ovomium.Updater/bin/$CONFIG/net48/Ovomium.Updater.dll"
PATCHERS="$GAME/BepInEx/patchers"
DEST="$GAME/BepInEx/plugins/Ovomium"
SCRIPTS="$GAME/BepInEx/scripts"
GAME_PROC="$GAME/valheim.x86_64"

[ "$BUILD" = 0 ] || "$PROJECT/tools/build.sh" "$CONFIG"
[ -f "$DLL" ] || { echo "DLL absente, lance d'abord tools/build.sh : $DLL" >&2; exit 1; }
[ -f "$PATCHER" ] || { echo "Patcher absent, lance d'abord tools/build.sh : $PATCHER" >&2; exit 1; }
[ -d "$GAME/BepInEx" ] || { echo "BepInEx absent dans $GAME, lance d'abord tools/install-bepinex.sh" >&2; exit 1; }
[ "$DEV" = 0 ] || [ -f "$GAME/BepInEx/plugins/ScriptEngine.dll" ] \
    || { echo "ScriptEngine absent, lance d'abord tools/install-scriptengine.sh" >&2; exit 1; }
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

# Copie par renommage atomique : jamais de DLL à moitié écrite, ni pour Mono ni pour le guetteur de ScriptEngine.
if [ "$DEV" = 1 ]; then
    TARGET="$SCRIPTS/Ovomium.dll"; OTHER="$DEST/Ovomium.dll"
else
    TARGET="$DEST/Ovomium.dll"; OTHER="$SCRIPTS/Ovomium.dll"
fi
if [ -f "$OTHER" ]; then
    gio trash "$OTHER"
    echo "Changement de mode : $OTHER mis à la corbeille (relance du jeu nécessaire)"
fi
mkdir -p "$DEST" "$(dirname "$TARGET")"
if [ "$DEV" = 1 ]; then  # ScriptEngine lit la DLL avec Cecil et exige ses symboles (.pdb à côté), avant la DLL
    cp "${DLL%.dll}.pdb" "${TARGET%.dll}.pdb.new"
    mv -f "${TARGET%.dll}.pdb.new" "${TARGET%.dll}.pdb"
fi
cp "$DLL" "$TARGET.new"
mv -f "$TARGET.new" "$TARGET"
echo "Déployé : $TARGET"
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
