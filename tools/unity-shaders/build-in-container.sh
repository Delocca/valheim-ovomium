#!/bin/bash
# Exécuté DANS le conteneur unityci/editor par tools/build-shaders.sh (dépôt monté sur /repo), avec le compte Unity
# d'Edia (e-mail puis mot de passe, une ligne chacun sur l'entrée standard) :
#   build (défaut) : active un siège Unity Personal, compile le bundle, puis rend le siège dans tous les cas ;
#   release        : rend seulement le siège (reprise après un passage qui ne l'a pas rendu).
# Un siège Personal reste pris tant qu'il n'est pas rendu. Client de licence : Unity.Licensing.Client 1.17 (--help) ;
# --return-ulf de game-ci/cli (PR #246) ne rend que les licences .ulf, pas le siège pris par --activate-all.
# Éditeur : wrapper unity-editor de l'image (game-ci/docker, images/ubuntu/editor/Dockerfile).
set -uo pipefail

MODE="${1:-build}"
ULC=/opt/unity/Editor/Data/Resources/Licensing/Client/Unity.Licensing.Client
PROJECT=/repo/tools/unity-shaders

step() { echo; echo "=== $* ==="; }
ulc() { "$ULC" "$@" --username "$UNITY_EMAIL" --password "$UNITY_PASSWORD"; }

release_seat() {
    step "Restitution du siège Unity Personal"
    if ulc --deactivate-all; then
        echo "Siège rendu."
    else
        echo "ÉCHEC de la restitution : le siège reste peut-être pris (voir « Sièges » ci-dessous)."
    fi
    step "Sièges du compte après restitution"
    ulc --show-seats
}

step "Identifiants (lus sur l'entrée standard)"
read -r UNITY_EMAIL
read -r UNITY_PASSWORD
[ -n "$UNITY_EMAIL" ] && [ -n "$UNITY_PASSWORD" ] || { echo "ÉCHEC : e-mail ou mot de passe vide"; exit 11; }
echo "Reçus."

step "Sortie réseau du conteneur (doit être hors Mullvad pour les serveurs Unity)"
curl -s --max-time 10 https://am.i.mullvad.net/connected || echo "(test impossible)"

step "Sièges du compte avant l'opération"
ulc --show-seats

if [ "$MODE" = release ]; then
    release_seat
    exit 0
fi

# Armé avant l'activation : une activation interrompue ou mal notée ne laisse pas de siège pris.
trap release_seat EXIT
trap 'exit 130' INT TERM HUP

step "Activation de la licence Unity Personal"
ulc --activate-all --include-personal
code=$?
if [ "$code" -ne 0 ]; then
    echo "ÉCHEC de l'activation (code $code). Causes connues : identifiants, réseau (Mullvad), plus de siège libre"
    echo "(passage précédent non rendu : tools/build-shaders.sh --release-seat), double authentification activée."
    exit 10
fi

step "Compilation du shader et du bundle (éditeur Unity, cible Windows 64 bits)"
unity-editor -nographics -quit -buildTarget Win64 -projectPath "$PROJECT" \
    -executeMethod BuildShaderBundle.Build -logFile -
code=$?
if [ "$code" -eq 0 ]; then echo "Éditeur Unity : OK"; else echo "Éditeur Unity : ÉCHEC (code $code)"; fi
exit "$code"
