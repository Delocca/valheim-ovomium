#!/bin/bash
# Exporte la configuration Valheim/Ovomium de ce PC Linux vers un paquet à installer sur un PC Windows (VR) :
# build/windows-config/ et build/Ovomium-config-windows.zip, installés par un double-clic sur Installer-Config.bat.
# Contenu : ovo.ovomium.cfg (FirstPerson désactivé, conflit avec la caméra VR), serverlist_local/{favorite,recent},
# valheim-prefs.reg (PlayerPrefs filtrées : touches, jeu, interface, audio ; ni graphismes/écran ni état/identifiants),
# Installer-Config.bat et LISEZMOI.txt (sources dans installer/, convertis en CRLF).
# Non exportés : ovo.ovomium.passwords.txt et ovo.ovomium.serverlinks.txt, chiffrés par une clé dérivée du nom de
# machine et d'utilisateur (SecretFile), illisibles sur un autre PC.
# Sources lues sans les modifier. Usage : tools/export-windows-config.sh
set -euo pipefail

PROJECT="$(cd "$(dirname "$0")/.." && pwd)"
GAME="${VALHEIM_DIR:-$HOME/.local/share/Steam/steamapps/common/Valheim}"
UNITY="$HOME/.config/unity3d/IronGate/Valheim"
CFG="$GAME/BepInEx/config/ovo.ovomium.cfg"
OUT="$PROJECT/build/windows-config"
ZIP="$PROJECT/build/Ovomium-config-windows.zip"

for f in "$CFG" "$UNITY/prefs" "$UNITY/serverlist_local/favorite" "$UNITY/serverlist_local/recent" \
         "$PROJECT/installer/Installer-Config.bat" "$PROJECT/installer/LISEZMOI-config.txt"; do
    [ -f "$f" ] || { echo "Fichier source absent : $f" >&2; exit 1; }
done

rm -rf "$OUT"
mkdir -p "$OUT"

# 1. Config d'Ovomium : Enabled = false dans la seule section [FirstPerson] (échec si la ligne n'y est pas).
awk '/^\[/ { section = $0 }
     section == "[FirstPerson]" && /^Enabled *=/ { $0 = "Enabled = false"; n++ }
     { print }
     END { if (n != 1) exit 1 }' "$CFG" > "$OUT/ovo.ovomium.cfg" \
    || { echo "[FirstPerson] Enabled introuvable (ou en double) dans $CFG" >&2; exit 1; }

# 2. Liste des serveurs (format binaire du jeu, identique sous Windows).
cp "$UNITY/serverlist_local/favorite" "$UNITY/serverlist_local/recent" "$OUT/"

# 3. PlayerPrefs Linux (XML, chaînes en base64) -> .reg au format registre de Unity sous Windows :
#    nom de valeur « <clé>_h<djb2-xor 32 bits non signé, décimal> » ; int = REG_DWORD ; float = double 8 octets
#    little-endian sous le type REG_DWORD (hex(4) : c'est ce qu'écrit Unity, regedit l'affiche « valeur DWORD non
#    valide ») ; string = REG_BINARY des octets UTF-8 suivis d'un octet nul. Fichier UTF-16 LE avec BOM, CRLF.
python3 - "$UNITY/prefs" "$OUT/valheim-prefs.reg" <<'EOF'
import base64, struct, sys
import xml.etree.ElementTree as ET

# Inclusion explicite : une clé inconnue (nouvelle version du jeu) n'est pas exportée par défaut.
INCLUDE_PREFIXES = ("kbmBinding_", "Radial", "Gamepad", "MotionSensor", "Invert")
INCLUDE = {
    # touches et manette
    "ConsoleBindings", "ControllerLayout", "gamepad_glyphs", "input_switching_mode", "SwapTriggers",
    "MouseSensitivity",
    # jeu, accessibilité, interface
    "ToggleRun", "ToggleBlock", "GuiScale", "KeyHints", "CameraShake", "ShipCameraTilt", "AttackTowardsPlayerLookDir",
    "QuickPieceSelect", "ShowBuildPieceAuthor", "TutorialsEnabled", "SkipIntroCinematic", "language", "EnableConsole",
    "AutoBackups", "ReduceFlashingLights", "ClosedCaptions", "DirectionalSoundIndicators", "ReduceBackgroundUsage",
    "EulaAccepted", "DontShowBannedAgain",
    # audio
    "MasterVolume", "SfxVolume", "MusicVolume", "ContinousMusic",
}
# Exclusions connues (écran, graphismes, état, identifiants) : ignorées sans avertissement.
EXCLUDE_PREFIXES = ("Screenmanager", "unity.", "unity_connect.")
EXCLUDE = {
    "UnitySelectMonitor", "Target3DResolutionVertical", "GraphicsQualityMode", "UpscalingAlgorithm", "VSync",
    "FPSLimit", "AntiAliasing", "Bloom", "DOF", "MotionBlur", "SSAO_2", "SunShafts", "ChromaticAberration",
    "Tesselation", "SoftPart", "ShadowQuality", "DistantShadows", "PointLightShadows", "PointLights", "Lights",
    "LodBias", "ClutterQuality", "ClothQuality", "SimulationDistance",
    "profile", "world", "OvomiumLastSession", "serverListTab", "CampaignProgress", "LegacyNuked",
}

def unity_hash(name):
    h = 5381
    for b in name.encode("utf-8"):
        h = ((h * 33) ^ b) & 0xFFFFFFFF
    return h

def hex_bytes(data):
    return ",".join(f"{b:02x}" for b in data)

def reg_value(kind, text):
    if kind == "int":
        return f"dword:{int(text) & 0xFFFFFFFF:08x}"
    if kind == "float":
        return "hex(4):" + hex_bytes(struct.pack("<d", float(text)))
    if kind == "string":
        return "hex:" + hex_bytes(base64.b64decode(text) + b"\0")
    raise ValueError(f"type inconnu : {kind}")

src, dst = sys.argv[1:3]
lines = ["Windows Registry Editor Version 5.00", "", r"[HKEY_CURRENT_USER\Software\IronGate\Valheim]"]
kept, excluded, unknown = [], [], []
for pref in sorted(ET.parse(src).getroot(), key=lambda p: p.get("name")):
    name, kind, text = pref.get("name"), pref.get("type"), pref.text or ""
    if name in INCLUDE or name.startswith(INCLUDE_PREFIXES):
        lines.append(f'"{name}_h{unity_hash(name)}"={reg_value(kind, text)}')
        kept.append(name)
    elif name in EXCLUDE or name.startswith(EXCLUDE_PREFIXES):
        excluded.append(name)
    else:
        unknown.append(name)
with open(dst, "w", encoding="utf-16", newline="\r\n") as f:   # utf-16 : little-endian avec BOM
    f.write("\n".join(lines) + "\n\n")

print(f"Prefs exportées : {len(kept)}")
print(f"Prefs exclues (écran, graphismes, état) : {len(excluded)} : {' '.join(excluded)}")
if unknown:
    print(f"ATTENTION, prefs inconnues ignorées (à classer dans ce script) : {len(unknown)} : {' '.join(unknown)}")
EOF

# 4. Installateur (CRLF obligatoire pour cmd) et LISEZMOI (CRLF + BOM UTF-8 pour le Bloc-notes).
sed 's/\r$//; s/$/\r/' "$PROJECT/installer/Installer-Config.bat" > "$OUT/Installer-Config.bat"
{ printf '\xef\xbb\xbf'; sed 's/\r$//; s/$/\r/' "$PROJECT/installer/LISEZMOI-config.txt"; } > "$OUT/LISEZMOI.txt"

# 5. Zip (-X : pas d'attributs Unix, inutiles sous Windows ; -j : fichiers à la racine).
rm -f "$ZIP"
zip -q -j -X "$ZIP" "$OUT"/*
echo "Dossier : $OUT"
echo "Archive : $ZIP ($(du -h "$ZIP" | cut -f1)) : $(ls "$OUT" | tr '\n' ' ')"
