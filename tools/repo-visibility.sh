#!/bin/bash
# Ouvre (public) ou ferme (private) le dépôt GitHub. Il reste privé sauf pendant les fenêtres de mise à jour des
# amies : l'installateur et l'API releases répondent 404 quand il est privé. À lancer hors bac à sable (gh authentifié).
# Usage : tools/repo-visibility.sh public|private|status
set -euo pipefail

GITHUB_REPO="Delocca/valheim-ovomium"

case "${1:-status}" in
    public|private)
        gh repo edit "$GITHUB_REPO" --visibility "$1" --accept-visibility-change-consequences
        echo "Dépôt $GITHUB_REPO : $1" ;;
    status)
        gh repo view "$GITHUB_REPO" --json visibility --jq '"Dépôt '"$GITHUB_REPO"' : " + (.visibility | ascii_downcase)' ;;
    *)
        echo "Usage : $0 public|private|status" >&2; exit 1 ;;
esac
