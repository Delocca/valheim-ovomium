#!/bin/bash
# Publie (ou remplace) build/dist/Ovomium-loading.zip dans la release GitHub « artworks », indépendante des versions
# du mod. Le mod la télécharge (LoadingArt.DownloadUrl) au premier lancement sans dossier loading/, pendant une fenêtre
# où le dépôt est public (tools/repo-visibility.sh). À lancer hors bac à sable, après tools/package-art.sh.
set -euo pipefail

# Doit rester identique dans tools/release-art.sh et LoadingArtConfig.cs (DownloadUrl).
GITHUB_REPO="Delocca/valheim-ovomium"
TAG="artworks"

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
ZIP="$PROJECT/build/dist/Ovomium-loading.zip"

[ -f "$ZIP" ] || { echo "Archive introuvable : $ZIP (lance tools/package-art.sh)" >&2; exit 1; }
gh auth status >/dev/null 2>&1 || { echo "gh non authentifié : lance \`gh auth login\`." >&2; exit 1; }

if gh release view "$TAG" --repo "$GITHUB_REPO" >/dev/null 2>&1; then
    gh release upload "$TAG" "$ZIP" --repo "$GITHUB_REPO" --clobber
    echo "Archive remplacée dans la release $TAG."
else
    gh release create "$TAG" "$ZIP" --repo "$GITHUB_REPO" --title "Artworks de chargement" \
        --notes "Images des écrans de chargement (LoadingArt). Téléchargées automatiquement par le mod ; à défaut, décompresser dans BepInEx/plugins/Ovomium/."
    echo "Release $TAG créée."
fi
echo "URL : https://github.com/$GITHUB_REPO/releases/download/$TAG/$(basename "$ZIP")"
