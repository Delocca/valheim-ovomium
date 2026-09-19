#!/bin/bash
# Publie une release GitHub : build, archive Windows (tools/package.sh), tag v<version>, release avec l'archive et
# l'installateur. La version est celle de <Version> dans Ovomium/Ovomium.csproj. À lancer hors bac à sable.
# Usage : tools/release.sh                 notes = section « Non publié » de CHANGELOG.md, que le script date et
#                                          renomme en <version> (commit « Changelog <version> ») avant de publier
#         tools/release.sh --notes "Texte" | tools/release.sh notes.md   (notes explicites, CHANGELOG.md non touché)
# Prérequis : arbre git propre, remote origin, `gh auth login` fait, tag v<version> inexistant.
set -euo pipefail

# Doit rester identique dans tools/release.sh et installer/Installer-Ovomium.bat.
GITHUB_REPO="Delocca/valheim-ovomium"

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$PROJECT"
VERSION="$(sed -n 's/.*<Version>\(.*\)<\/Version>.*/\1/p' Ovomium/Ovomium.csproj)"
TAG="v$VERSION"
ZIP="build/dist/Ovomium-$VERSION-windows.zip"
INSTALLER="installer/Installer-Ovomium.bat"
CHANGELOG="CHANGELOG.md"
UNRELEASED="## Non publié"
STAMP_CHANGELOG=""

case "${1:-}" in
    --notes) [ -n "${2:-}" ] || { echo "--notes attend un texte" >&2; exit 1; }; NOTES_ARGS=(--notes "$2") ;;
    "")      NOTES_FILE="$(mktemp)"
             # Section « Non publié » sans ses lignes vides de tête et de queue.
             awk -v h="$UNRELEASED" '$0==h{p=1;next} /^## /{p=0} p' "$CHANGELOG" \
                 | awk 'NF{f=1} f' | tac | awk 'NF{f=1} f' | tac > "$NOTES_FILE"
             [ -s "$NOTES_FILE" ] || { echo "$CHANGELOG : section « Non publié » vide, rien à publier." >&2; exit 1; }
             NOTES_ARGS=(--notes-file "$NOTES_FILE"); STAMP_CHANGELOG=1 ;;
    *)       [ -f "$1" ] || { echo "Fichier de notes introuvable : $1" >&2; exit 1; }; NOTES_ARGS=(--notes-file "$1") ;;
esac

[ -n "$VERSION" ] || { echo "Version introuvable dans Ovomium/Ovomium.csproj" >&2; exit 1; }
[ -z "$(git status --porcelain)" ] || { echo "Arbre git non propre : commite ou range d'abord." >&2; git status --short >&2; exit 1; }
git remote get-url origin >/dev/null 2>&1 || { echo "Pas de remote origin (dépôt $GITHUB_REPO)." >&2; exit 1; }
if git rev-parse -q --verify "refs/tags/$TAG" >/dev/null || [ -n "$(git ls-remote --tags origin "$TAG")" ]; then
    echo "Le tag $TAG existe déjà : incrémente <Version> dans Ovomium/Ovomium.csproj." >&2; exit 1
fi
gh auth status >/dev/null 2>&1 || { echo "gh non authentifié : lance \`gh auth login\`." >&2; exit 1; }
[ -f "$INSTALLER" ] || { echo "Installateur introuvable : $INSTALLER" >&2; exit 1; }

echo "=== Release Ovomium $VERSION ($TAG) sur $GITHUB_REPO ==="
if [ -n "$STAMP_CHANGELOG" ]; then
    awk -v h="$UNRELEASED" -v v="## $VERSION — $(date +%F)" '$0==h{print h; print ""; print v; next} {print}' \
        "$CHANGELOG" > "$CHANGELOG.tmp" && mv "$CHANGELOG.tmp" "$CHANGELOG"
    git add "$CHANGELOG" && git commit -q -m "Changelog $VERSION"
fi
tools/build.sh
tools/package.sh
[ -f "$ZIP" ] || { echo "Archive introuvable : $ZIP" >&2; exit 1; }

git tag -a "$TAG" -m "Ovomium $VERSION"
git push origin HEAD "$TAG"
gh release create "$TAG" "$ZIP" "$INSTALLER" --repo "$GITHUB_REPO" --title "Ovomium $VERSION" "${NOTES_ARGS[@]}"
echo "Release publiée : https://github.com/$GITHUB_REPO/releases/tag/$TAG"
