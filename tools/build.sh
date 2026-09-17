#!/bin/bash
# Compile Ovomium.dll dans le conteneur ovomiam-build (à lancer hors bac à sable).
# Usage : tools/build.sh [Debug|Release]   (défaut : Release). Sortie : Ovomium/bin/<config>/net48/Ovomium.dll
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
MANAGED="$GAME/valheim_Data/Managed"
CONFIG="${1:-Release}"

[ -f "$MANAGED/assembly_valheim.dll" ] || { echo "Introuvable : $MANAGED/assembly_valheim.dll" >&2; exit 1; }

# État NuGet dans build/nuget (gitignoré) : un volume nommé serait possédé par root, illisible avec keep-id.
mkdir -p "$PROJECT/build/nuget"
podman run --rm --userns=keep-id \
    -v "$PROJECT:/src" \
    -v "$MANAGED:/game/Managed:ro" \
    -v "$PROJECT/build/nuget:/nuget" \
    -w /src/Ovomium \
    ovomiam-build \
    dotnet build -c "$CONFIG" -nologo

echo "DLL : $PROJECT/Ovomium/bin/$CONFIG/net48/Ovomium.dll"
