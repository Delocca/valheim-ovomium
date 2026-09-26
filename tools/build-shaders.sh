#!/bin/bash
# Compile le shader de SmoothShading (tools/unity-shaders/) en bundle Unity, copié dans
# Ovomium/Features/SmoothShading/smoothshading.bundle puis embarqué dans Ovomium.Core.dll par tools/build.sh.
# À relancer seulement si le shader change ou si la version Unity du jeu change (IMAGE ci-dessous à suivre).
# Lancé par Edia dans un terminal (réseau et podman hors bac à sable), identifiants Unity demandés au clavier :
#   gnome-terminal -- <dépôt>/tools/build-shaders.sh        Journal : /tmp/build-shaders.log (sans le mot de passe)
#   … build-shaders.sh --release-seat   rend seulement le siège Unity Personal (passage qui ne l'a pas rendu)
exec > >(tee /tmp/build-shaders.log) 2>&1
set -uo pipefail

MODE=build
[ "${1:-}" = "--release-seat" ] && MODE=release
REPO="$(cd "$(dirname "$0")/.." && pwd)"
IMAGE=docker.io/unityci/editor:ubuntu-6000.0.75f1-windows-mono-3
SHADER="$REPO/tools/unity-shaders/Assets/Shaders/OvoSmoothDeferredShading.shader"
BUNDLE="$REPO/Ovomium/Features/SmoothShading/smoothshading.bundle"

step() { echo; echo "=== $* ==="; }
finish() {
    echo
    echo "Journal : /tmp/build-shaders.log"
    echo "Appuie sur Entrée pour fermer."
    read -r
}
trap finish EXIT

if [ "${1:-}" = "--help" ]; then
    sed -n 2,7p "$0"
    exit 0
fi

step "Vérifications"
podman image exists "$IMAGE" || { echo "Image absente : lancer d'abord  podman pull $IMAGE"; exit 1; }
[ -f "$SHADER" ] || { echo "Shader absent : $SHADER"; exit 1; }
echo "Image : $IMAGE"
echo "Shader : $SHADER"

# Les serveurs Unity refusent les connexions venant de Mullvad : le conteneur passe à côté du VPN (mullvad-exclude
# ne concerne que cette commande, la configuration de Mullvad n'est pas modifiée).
NET=()
if command -v mullvad-exclude >/dev/null; then
    NET=(mullvad-exclude)
    echo "Réseau : conteneur lancé hors VPN (mullvad-exclude)"
fi

step "Identifiants du compte Unity (servent seulement à activer puis rendre un siège Personal)"
read -rp "E-mail : " UNITY_EMAIL
read -rsp "Mot de passe (rien ne s'affiche) : " UNITY_PASSWORD
echo

if [ "$MODE" = release ]; then
    step "Conteneur Unity : restitution du siège"
else
    step "Conteneur Unity : activation, compilation, restitution du siège (plusieurs minutes)"
fi
before=$(stat -c %Y "$BUNDLE" 2>/dev/null || echo 0)
# Identifiants par l'entrée standard (printf est interne au shell : jamais sur une ligne de commande) ; pas par
# l'environnement, que mullvad-exclude (setuid) ne transmet pas (activation reçue sans identifiants, 2026-09-27).
# Nom d'hôte fixe : le client de licence voit la même machine d'un passage à l'autre (supposé, non vérifié).
printf '%s\n%s\n' "$UNITY_EMAIL" "$UNITY_PASSWORD" | "${NET[@]}" podman run --rm -i --init \
    --hostname ovomium-unity \
    -v "$REPO:/repo" \
    --entrypoint /bin/bash \
    "$IMAGE" /repo/tools/unity-shaders/build-in-container.sh "$MODE"
code=$?
unset UNITY_PASSWORD
[ "$MODE" = release ] && exit "$code"

step "Résultat"
after=$(stat -c %Y "$BUNDLE" 2>/dev/null || echo 0)
if [ "$code" -eq 0 ] && [ "$after" -gt "$before" ]; then
    echo "RÉUSSI : $BUNDLE ($(stat -c %s "$BUNDLE") octets)"
    echo "Suite : tools/build.sh Release pour l'embarquer dans Ovomium.Core.dll."
else
    echo "ÉCHEC (code $code) : voir le journal ci-dessus (lignes « ÉCHEC », « error », « Shader error »)."
    exit 1
fi
