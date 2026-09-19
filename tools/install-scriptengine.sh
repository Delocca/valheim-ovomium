#!/bin/bash
# Installe ScriptEngine (BepInEx.Debug) dans le jeu : rechargement à chaud des DLL de BepInEx/scripts/
# (à lancer hors bac à sable, par Edia). Ensuite : tools/deploy.sh --dev (cf. README).
# Source : https://github.com/BepInEx/BepInEx.Debug
exec > >(tee /tmp/install-scriptengine.log) 2>&1
set -e

RELEASE="r11.1"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
URL="https://github.com/BepInEx/BepInEx.Debug/releases/download/$RELEASE/ScriptEngine_$RELEASE.zip"
CFG="$GAME/BepInEx/config/com.bepis.bepinex.scriptengine.cfg"  # nommé d'après le GUID du plugin
WORK="$(mktemp -d /tmp/scriptengine-XXXX)"

[ -d "$GAME/BepInEx/plugins" ] || { echo "BepInEx absent dans $GAME, lance d'abord tools/install-bepinex.sh"; exit 1; }

echo "=== Étape 1 : télécharger ScriptEngine $RELEASE ==="
echo "Commande : curl -L \"$URL\" -o $WORK/se.zip"
echo "Appuie sur Entrée pour exécuter, ou Ctrl+C pour annuler..."
read
curl -L "$URL" -o "$WORK/se.zip"
unzip -q "$WORK/se.zip" -d "$WORK/unz"
find "$WORK/unz" -type f

echo
echo "=== Étape 2 : copier ScriptEngine.dll dans $GAME/BepInEx/plugins/ et créer BepInEx/scripts/ ==="
echo "Appuie sur Entrée pour exécuter..."
read
DLL="$(find "$WORK/unz" -name ScriptEngine.dll | head -n 1)"
[ -n "$DLL" ] || { echo "ScriptEngine.dll introuvable dans le zip"; exit 1; }
cp "$DLL" "$GAME/BepInEx/plugins/ScriptEngine.dll"
mkdir -p "$GAME/BepInEx/scripts"
ls -l "$GAME/BepInEx/plugins/ScriptEngine.dll" "$GAME/BepInEx/scripts"

echo
echo "=== Étape 3 : config $CFG ==="
if [ -e "$CFG" ]; then
    echo "Existe déjà, conservée. Vérifie LoadOnStart = true et EnableFileSystemWatcher = true :"
    cat "$CFG"
else
    echo "Écrit : chargement des scripts au démarrage (LoadOnStart), rechargement auto 3 s après modification"
    echo "d'un fichier de BepInEx/scripts/ (EnableFileSystemWatcher), touche manuelle F6."
    echo "Appuie sur Entrée pour exécuter..."
    read
    cat > "$CFG" <<'EOF'
[General]
LoadOnStart = true
ReloadKey = F6
QuietMode = false
IncludeSubdirectories = false

[AutoReload]
EnableFileSystemWatcher = true
AutoReloadDelay = 3
DumpAssemblies = false
EOF
    cat "$CFG"
fi
# Erreur d'une première version de ce script : fichier mal nommé, jamais lu par le plugin.
[ -e "$GAME/BepInEx/config/ScriptEngine.cfg" ] && gio trash "$GAME/BepInEx/config/ScriptEngine.cfg"

echo
echo "=== Terminé. Prochaine étape : tools/deploy.sh --dev (déplace Ovomium.dll vers BepInEx/scripts/) ==="
echo "Appuie sur Entrée pour fermer."
read
