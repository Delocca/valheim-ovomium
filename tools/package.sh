#!/bin/bash
# Construit l'archive Windows distribuée aux joueuses : build/dist/Ovomium-<version>-windows.zip
# Contenu (à extraire tel quel dans le dossier de Valheim) : BepInExPack_Valheim pour Windows (winhttp.dll,
# doorstop_config.ini, BepInEx/) + BepInEx/plugins/Ovomium/{Ovomium.dll,version.txt} + BepInEx/patchers/Ovomium.Updater.dll
# (installe la mise à jour téléchargée par la feature Updater au lancement suivant). Pas d'artworks.
# Le pack est téléchargé depuis Thunderstore (même version que install-bepinex.sh) et gardé dans build/cache/.
# À lancer hors bac à sable (téléchargement). Usage : tools/package.sh   (après tools/build.sh)
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
DLL="$PROJECT/Ovomium/bin/Release/net48/Ovomium.dll"
PATCHER="$PROJECT/Ovomium.Updater/bin/Release/net48/Ovomium.Updater.dll"
VERSION="$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' "$PROJECT/Ovomium/Ovomium.csproj")"
BEPINEX_VERSION="$(sed -n 's/^VERSION="\(.*\)"$/\1/p' "$PROJECT/tools/install-bepinex.sh")"
CACHE="$PROJECT/build/cache/BepInExPack_Valheim-$BEPINEX_VERSION.zip"
DIST="$PROJECT/build/dist"
ZIP="$DIST/Ovomium-$VERSION-windows.zip"

[ -n "$VERSION" ] || { echo "Version introuvable dans Ovomium/Ovomium.csproj" >&2; exit 1; }
[ -n "$BEPINEX_VERSION" ] || { echo "VERSION= introuvable dans tools/install-bepinex.sh" >&2; exit 1; }
[ -f "$DLL" ] || { echo "DLL absente, lance d'abord tools/build.sh : $DLL" >&2; exit 1; }
[ -f "$PATCHER" ] || { echo "Patcher absent, lance d'abord tools/build.sh : $PATCHER" >&2; exit 1; }

# 1. Pack BepInEx (cache) — téléchargé dans un fichier temporaire pour ne pas garder un zip tronqué en cache.
if [ ! -f "$CACHE" ]; then
    mkdir -p "$(dirname "$CACHE")"
    URL="https://thunderstore.io/package/download/denikson/BepInExPack_Valheim/$BEPINEX_VERSION/"
    echo "Téléchargement de BepInExPack_Valheim $BEPINEX_VERSION…"
    curl -fL --retry 3 "$URL" -o "$CACHE.part"
    mv "$CACHE.part" "$CACHE"
fi
# Structure attendue (Thunderstore) : manifest.json, icon.png, README.md, CHANGELOG.md à la racine,
# et BepInExPack_Valheim/{winhttp.dll, doorstop_config.ini, BepInEx/, doorstop_libs/, *.sh, changelog.txt}.
for f in BepInExPack_Valheim/winhttp.dll BepInExPack_Valheim/doorstop_config.ini BepInExPack_Valheim/BepInEx/core/BepInEx.dll; do
    unzip -l "$CACHE" "$f" >/dev/null 2>&1 || { echo "Zip inattendu (pas de $f) : $CACHE" >&2; exit 1; }
done

# 2. Arborescence de l'archive : racine du jeu = contenu de BepInExPack_Valheim/ sans les fichiers Linux/macOS
#    ni les métadonnées Thunderstore, puis le plugin.
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
unzip -q "$CACHE" -d "$WORK/unz"
rsync -a "$WORK/unz/BepInExPack_Valheim/" "$WORK/root/" \
    --exclude manifest.json --exclude icon.png --exclude README.md --exclude CHANGELOG.md --exclude changelog.txt \
    --exclude 'start_game_bepinex.sh' --exclude 'start_server_bepinex.sh' --exclude 'run_bepinex.sh' \
    --exclude 'doorstop_libs/' --exclude 'libdoorstop*' --exclude '.DS_Store'
if find "$WORK/root" \( -name '*.sh' -o -name '*.so' -o -name '*.dylib' \) | grep -q .; then
    echo "Fichiers non Windows restants dans l'archive :" >&2
    find "$WORK/root" \( -name '*.sh' -o -name '*.so' -o -name '*.dylib' \) >&2
    exit 1
fi
PLUGIN="$WORK/root/BepInEx/plugins/Ovomium"
mkdir -p "$PLUGIN"
cp "$DLL" "$PLUGIN/"
printf '%s\n' "$VERSION" > "$PLUGIN/version.txt"   # lu par installer/Installer-Ovomium.bat (« déjà à jour »)
mkdir -p "$WORK/root/BepInEx/patchers"
cp "$PATCHER" "$WORK/root/BepInEx/patchers/"

# 3. Zip (-X : pas d'attributs Unix, inutiles sous Windows).
mkdir -p "$DIST"
rm -f "$ZIP"
(cd "$WORK/root" && zip -q -r -X "$ZIP" .)

echo "Archive : $ZIP ($(du -h "$ZIP" | cut -f1))"
echo "Racine  : $(ls "$WORK/root" | tr '\n' ' ')"
echo "Plugin  : $(ls "$PLUGIN" | tr '\n' ' ')"
echo "Patcher : $(ls "$WORK/root/BepInEx/patchers" | tr '\n' ' ')"
