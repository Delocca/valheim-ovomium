#!/bin/bash
# Archive des artworks de chargement pour les amies : valheim_art/ → build/dist/Ovomium-loading.zip, contenant un
# dossier loading/ (à décompresser dans BepInEx/plugins/Ovomium/). Images recompressées en jpg qualité 95, résolution
# limitée à MAX_SIZE (largeur ou hauteur), sans métadonnées. Non distribuée sur GitHub (artworks Iron Gate).
# Usage : tools/package-art.sh [taille max en px, défaut 3840]
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
SRC="$PROJECT/valheim_art"
ZIP="$PROJECT/build/dist/Ovomium-loading.zip"
MAX_SIZE="${1:-3840}"
QUALITY=95

command -v convert >/dev/null || { echo "ImageMagick (convert) requis." >&2; exit 1; }
[ -d "$SRC" ] || { echo "Dossier d'artworks introuvable : $SRC" >&2; exit 1; }

STAGE="$(mktemp -d)"
trap 'rm -rf "$STAGE"' EXIT
mkdir -p "$STAGE/loading" "$(dirname "$ZIP")"

for f in "$SRC"/*; do
    case "${f,,}" in *.jpg|*.jpeg|*.png) ;; *) continue ;; esac
    name="$(basename "${f%.*}").jpg"
    convert "$f" -auto-orient -strip -resize "${MAX_SIZE}x${MAX_SIZE}>" -quality "$QUALITY" "$STAGE/loading/$name"
    printf '%-28s %s → %s\n' "$name" "$(du -h "$f" | cut -f1)" "$(du -h "$STAGE/loading/$name" | cut -f1)"
done

rm -f "$ZIP"
(cd "$STAGE" && zip -q -r "$ZIP" loading)
echo "Archive : $ZIP ($(du -h "$ZIP" | cut -f1), $(ls "$STAGE/loading" | wc -l) images)"
