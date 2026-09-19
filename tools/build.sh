#!/bin/bash
# Compile Ovomium.sln (Ovomium.dll + patcher Ovomium.Updater.dll) dans le conteneur ovomiam-build (à lancer hors
# bac à sable). Usage : tools/build.sh [Debug|Release]   (défaut : Release).
# Sorties : Ovomium/bin/<config>/net48/Ovomium.dll et Ovomium.Updater/bin/<config>/net48/Ovomium.Updater.dll
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
    -w /src \
    ovomiam-build \
    dotnet build Ovomium.sln -c "$CONFIG" -nologo

echo "DLL     : $PROJECT/Ovomium/bin/$CONFIG/net48/Ovomium.dll"
echo "Patcher : $PROJECT/Ovomium.Updater/bin/$CONFIG/net48/Ovomium.Updater.dll"
