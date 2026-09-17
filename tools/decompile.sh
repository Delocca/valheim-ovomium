#!/bin/bash
# Décompile assembly_valheim.dll en projet C# via ilspycmd dans le conteneur ovomiam-build.
# Usage : tools/decompile.sh [dossier_sortie]   (défaut : $TMPDIR/valheim-decompiled)
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
MANAGED="$GAME/valheim_Data/Managed"
# Chemin absolu obligatoire : podman prend un chemin relatif pour un nom de volume
OUT="$(realpath -m "${1:-${TMPDIR:-/tmp}/valheim-decompiled}")"

[ -f "$MANAGED/assembly_valheim.dll" ] || { echo "Introuvable : $MANAGED/assembly_valheim.dll" >&2; exit 1; }
mkdir -p "$OUT"

podman run --rm --userns=keep-id \
    -v "$PROJECT:/src" \
    -v "$MANAGED:/game/Managed:ro" \
    -v "$OUT:/out" \
    ovomiam-build \
    ilspycmd -p -o /out -r /game/Managed /game/Managed/assembly_valheim.dll

echo "Décompilé dans : $OUT"
