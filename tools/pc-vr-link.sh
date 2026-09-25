#!/bin/bash
# Relie ce PC Linux au PC VR Windows par Syncthing, sans passer par l'interface web :
#  1. génère build/Installer-PC-VR.bat (tools/make-pc-vr-installer.sh), le fichier à lancer sur le PC VR ;
#  2. crée les trois partages (mêmes ID que dans installer/Installer-PC-VR.bat.in) :
#       ovomium-pc-vr          build/pc-vr/                envoi et réception (config poussée vers le PC VR)
#       ovomium-pc-vr-bepinex  build/pc-vr-logs/bepinex/   réception seule (LogOutput.log du PC VR)
#       ovomium-pc-vr-unity    build/pc-vr-logs/unity/     réception seule (Player.log, Player-prev.log)
#  3. copie build/windows-config/ (tools/export-windows-config.sh) dans build/pc-vr/config/ ;
#  4. attend (30 min max) que le PC VR se présente, l'accepte et lui partage les trois dossiers.
# Idempotent : relançable sans dégât (après un nouvel export de config, par exemple).
# À lancer hors bac à sable (API Syncthing locale). Usage : tools/pc-vr-link.sh    Journal : /tmp/pc-vr-link.log
set -euo pipefail
exec > >(tee /tmp/pc-vr-link.log) 2>&1

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
CONFIG="${XDG_STATE_HOME:-$HOME/.local/state}/syncthing/config.xml"
REMOTE_NAME="PC VR"
WAIT_S=1800
# id|type|dossier|libellé
FOLDERS=(
    "ovomium-pc-vr|sendreceive|$PROJECT/build/pc-vr|Ovomium PC VR"
    "ovomium-pc-vr-bepinex|receiveonly|$PROJECT/build/pc-vr-logs/bepinex|PC VR - journal BepInEx"
    "ovomium-pc-vr-unity|receiveonly|$PROJECT/build/pc-vr-logs/unity|PC VR - journal Unity"
)

step() { echo; echo "== $*"; }
die()  { echo "ERREUR : $*" >&2; exit 1; }

# Clé API et adresse de l'interface, lues dans config.xml.
[ -f "$CONFIG" ] || die "config Syncthing introuvable : $CONFIG"
read -r API_KEY GUI_ADDR GUI_TLS < <(python3 - "$CONFIG" <<'EOF'
import sys, xml.etree.ElementTree as ET
gui = ET.parse(sys.argv[1]).getroot().find('gui')
print(gui.findtext('apikey'), gui.findtext('address'), gui.get('tls', 'false'))
EOF
)
[ -n "$API_KEY" ] || die "pas de <apikey> dans $CONFIG"
GUI_ADDR="${GUI_ADDR/0.0.0.0/127.0.0.1}"
if [ "$GUI_TLS" = true ]; then BASE="https://$GUI_ADDR"; else BASE="http://$GUI_ADDR"; fi
CURL=(curl -sS -k -H "X-API-Key: $API_KEY")

# api MÉTHODE CHEMIN [JSON] : sortie = corps de la réponse ; échoue si le code HTTP n'est pas 2xx.
api() {
    local args=(--fail-with-body -X "$1" "$BASE$2")
    [ $# -ge 3 ] && args+=(-H 'Content-Type: application/json' --data-binary "$3")
    "${CURL[@]}" "${args[@]}"
}
# py CODE [ARGS…] : petit utilitaire JSON (stdin = JSON reçu de l'API).
py() { local code="$1"; shift; python3 -c "import json, sys; $code" "$@"; }

step "Syncthing"
if ! "${CURL[@]}" -o /dev/null "$BASE/rest/system/ping" 2>/dev/null; then
    echo "   Syncthing ne répond pas, démarrage du service…"
    systemctl --user start syncthing.service
    for _ in $(seq 30); do "${CURL[@]}" -o /dev/null "$BASE/rest/system/ping" 2>/dev/null && break; sleep 1; done
fi
MY_ID="$(api GET /rest/system/status | py 'print(json.load(sys.stdin)["myID"])')"
echo "   OK ($BASE), cet appareil : $MY_ID"

step "Installateur du PC VR"
"$PROJECT/tools/make-pc-vr-installer.sh"

step "Partages"
for spec in "${FOLDERS[@]}"; do
    IFS='|' read -r id type path label <<<"$spec"
    mkdir -p "$path"
    current="$(api GET /rest/config/folders/"$id" 2>/dev/null)" || current=""
    if [ -n "$current" ]; then
        known_path="$(py 'print(json.load(sys.stdin)["path"])' <<<"$current")"
        if [ "${known_path%/}" = "$path" ]; then
            echo "   $id : déjà présent"
        else
            echo "   ATTENTION : $id existe déjà sur un autre dossier ($known_path), laissé tel quel."
        fi
        continue
    fi
    body="$(py 'print(json.dumps({"id": sys.argv[1], "type": sys.argv[2], "path": sys.argv[3], "label": sys.argv[4],
        "devices": [{"deviceID": sys.argv[5]}], "fsWatcherEnabled": True}))' "$id" "$type" "$path" "$label" "$MY_ID")"
    api POST /rest/config/folders "$body" >/dev/null
    echo "   $id : créé ($type) sur $path"
done

step "Config Windows"
if [ -d "$PROJECT/build/windows-config" ]; then
    mkdir -p "$PROJECT/build/pc-vr/config"
    cp -a "$PROJECT/build/windows-config/." "$PROJECT/build/pc-vr/config/"
    api POST "/rest/db/scan?folder=ovomium-pc-vr" >/dev/null
    echo "   build/windows-config/ copié dans build/pc-vr/config/ (sera envoyé au PC VR)."
else
    echo "   ATTENTION : build/windows-config/ absent : lance tools/export-windows-config.sh, puis relance ce script."
    echo "   (sans lui, Installer-PC-VR.bat attendra la config sur le PC VR)"
fi

step "Appairage avec « $REMOTE_NAME »"
# Déjà connu (script relancé) ? Sinon, l'appareil en attente nommé « PC VR », ou le seul en attente.
find_known() { api GET /rest/config/devices | py '
for d in json.load(sys.stdin):
    if d["name"] == sys.argv[1] and d["deviceID"] != sys.argv[2]: print(d["deviceID"]); break' "$REMOTE_NAME" "$MY_ID"; }
find_pending() { api GET /rest/cluster/pending/devices | py '
pending = json.load(sys.stdin) or {}
named = [i for i, d in pending.items() if d.get("name") == sys.argv[1]]
pick = named or (list(pending) if len(pending) == 1 else [])
if pick: print(pick[0])
elif pending: print("   Appareils en attente, aucun nommé « %s » : %s" % (sys.argv[1], ", ".join(
    "%s (%s)" % (d.get("name") or "?", i[:7]) for i, d in pending.items())), file=sys.stderr)' "$REMOTE_NAME"; }

REMOTE_ID="$(find_known)"
if [ -n "$REMOTE_ID" ]; then
    echo "   Déjà appairé : $REMOTE_ID"
else
    echo "   Porte maintenant $PROJECT/build/Installer-PC-VR.bat sur le PC VR (clé USB…) et lance-le (double-clic)."
    deadline=$((SECONDS + WAIT_S)); next_msg=0
    while :; do
        REMOTE_ID="$(find_pending)"
        [ -n "$REMOTE_ID" ] && break
        [ $SECONDS -ge $deadline ] && die "le PC VR ne s'est pas présenté en $((WAIT_S / 60)) min. Relance ce script quand Installer-PC-VR.bat tourne sur le PC VR."
        if [ $SECONDS -ge $next_msg ]; then
            echo "   En attente du PC VR… ($(( (deadline - SECONDS + 59) / 60 )) min restantes)"
            next_msg=$((SECONDS + 60))
        fi
        sleep 5
    done
    body="$(py 'print(json.dumps({"deviceID": sys.argv[1], "name": sys.argv[2], "addresses": ["dynamic"]}))' "$REMOTE_ID" "$REMOTE_NAME")"
    api POST /rest/config/devices "$body" >/dev/null
    echo "   Appareil ajouté : $REMOTE_ID"
fi

step "Partage des dossiers avec « $REMOTE_NAME »"
for spec in "${FOLDERS[@]}"; do
    IFS='|' read -r id _ <<<"$spec"
    # Liste des appareils du partage complétée si besoin ; vide = déjà partagé.
    devices="$(api GET /rest/config/folders/"$id" | py '
devs = json.load(sys.stdin)["devices"]
if all(d["deviceID"] != sys.argv[1] for d in devs):
    print(json.dumps({"devices": devs + [{"deviceID": sys.argv[1]}]}))' "$REMOTE_ID")"
    if [ -n "$devices" ]; then
        api PATCH /rest/config/folders/"$id" "$devices" >/dev/null
        echo "   $id : partagé"
    else
        echo "   $id : déjà partagé"
    fi
done

step "Connexion"
connected=""
for _ in $(seq 24); do
    connected="$(api GET /rest/system/connections | py '
c = json.load(sys.stdin)["connections"].get(sys.argv[1], {})
print("oui" if c.get("connected") else "")' "$REMOTE_ID")"
    [ -n "$connected" ] && break
    sleep 5
done
if [ -n "$connected" ]; then echo "   Connecté au PC VR."; else echo "   Pas encore connecté (normal si le PC VR est éteint) : la synchro se fera dès qu'il le sera."; fi

echo
echo "Terminé. Journaux du PC VR, mis à jour en continu :"
echo "   $PROJECT/build/pc-vr-logs/bepinex/LogOutput.log"
echo "   $PROJECT/build/pc-vr-logs/unity/Player.log"
echo "Nouvelle config pour le PC VR : tools/export-windows-config.sh puis relancer ce script."
