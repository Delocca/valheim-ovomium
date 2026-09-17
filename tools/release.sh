#!/bin/bash
# Publie une release GitHub : build, archive Windows (tools/package.sh), tag v<version>, release avec l'archive et
# l'installateur. La version est celle de <Version> dans Ovomium/Ovomium.csproj. À lancer hors bac à sable.
# Usage : tools/release.sh --notes "Texte des notes"
#         tools/release.sh notes.md
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

case "${1:-}" in
    --notes) [ -n "${2:-}" ] || { echo "--notes attend un texte" >&2; exit 1; }; NOTES_ARGS=(--notes "$2") ;;
    "")      echo "Usage : $0 --notes \"texte\" | $0 notes.md" >&2; exit 1 ;;
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
tools/build.sh
tools/package.sh
[ -f "$ZIP" ] || { echo "Archive introuvable : $ZIP" >&2; exit 1; }

git tag -a "$TAG" -m "Ovomium $VERSION"
git push origin HEAD "$TAG"
gh release create "$TAG" "$ZIP" "$INSTALLER" --repo "$GITHUB_REPO" --title "Ovomium $VERSION" "${NOTES_ARGS[@]}"
echo "Release publiée : https://github.com/$GITHUB_REPO/releases/tag/$TAG"
