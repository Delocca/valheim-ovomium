#!/bin/bash
# Installe BepInExPack_Valheim dans le dossier du jeu (à lancer hors bac à sable, par Edia).
# Source : https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/
exec > >(tee /tmp/install-bepinex.log) 2>&1
set -e

VERSION="5.4.2350"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
URL="https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/$VERSION/"
WORK="$(mktemp -d /tmp/bepinex-XXXX)"

[ -x "$GAME/valheim.x86_64" ] || { echo "Jeu introuvable dans $GAME"; exit 1; }

echo "=== Étape 1 : télécharger BepInExPack_Valheim $VERSION ==="
echo "Commande : curl -L \"$URL\" -o $WORK/pack.zip"
echo "Appuie sur Entrée pour exécuter, ou Ctrl+C pour annuler..."
read
curl -L "$URL" -o "$WORK/pack.zip"
unzip -q "$WORK/pack.zip" -d "$WORK/unz"
ls "$WORK/unz/BepInExPack_Valheim"

echo
echo "=== Étape 2 : copier le contenu de BepInExPack_Valheim/ dans $GAME ==="
echo "(BepInEx/, doorstop_libs/, doorstop_config.ini, start_game_bepinex.sh, etc. ; aucun fichier du jeu n'est remplacé)"
echo "Appuie sur Entrée pour exécuter..."
read
cp -r "$WORK/unz/BepInExPack_Valheim/." "$GAME/"
chmod u+x "$GAME/start_game_bepinex.sh"
ls "$GAME"

echo
echo "=== Terminé. Dernière étape, à la main dans Steam ==="
echo "Bibliothèque > Valheim > Propriétés > Options de lancement :"
echo "    ./start_game_bepinex.sh %command%"
echo "Au prochain lancement, BepInEx écrira $GAME/BepInEx/LogOutput.log"
echo "Appuie sur Entrée pour fermer."
read
