<# : batch portion
@echo off
rem Ce fichier est a la fois un .bat et un script PowerShell 5.1 (Windows 10/11). Lignes CRLF obligatoires.
setlocal
chcp 65001 >nul
title Ovomium - import de la configuration
set "OVOMIUM_SELF=%~f0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "& ([ScriptBlock]::Create((Get-Content -Raw -LiteralPath $env:OVOMIUM_SELF -Encoding UTF8)))"
echo.
pause
exit /b
#>

# Importe sur ce PC Windows la configuration Valheim/Ovomium exportée depuis Linux par tools/export-windows-config.sh :
# installe Ovomium s'il manque (Installer-Ovomium.bat de la dernière release), copie la config d'Ovomium, les favoris
# et récents de la liste des serveurs, les réglages du jeu (touches, audio, interface) dans le registre, puis VHVR
# (mod de réalité virtuelle, oui par défaut). Chaque fichier remplacé est d'abord sauvegardé en .bak (une seule fois :
# le .bak garde l'original). Sans droits admin, quel que soit le répertoire courant (fichiers lus à côté du .bat).
$VALHEIM_APPID = '892970'
$PREFS_KEY = 'HKCU\Software\IronGate\Valheim'
# GITHUB_REPO identique à tools/release.sh et installer/Installer-Ovomium.bat.
$OVOMIUM_INSTALLER_URL = 'https://github.com/Delocca/valheim-ovomium/releases/latest/download/Installer-Ovomium.bat'
$VHVR_VERSION = '0.10.5'
$VHVR_URL = "https://github.com/brandonmousseau/vhvr-mod/releases/download/v$VHVR_VERSION/vhvr.zip"
# SHA-256 de BepInEx\plugins\ValheimVRMod.dll de cette version (sa FileVersion reste 1.0.0.0) : déjà installée = rien à faire.
$VHVR_DLL_SHA256 = '8F5F8584DD7AF23698F75B50F1D12467D2BDDC0C9956E09C6314F144040B656D'

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # la barre de progression de PowerShell 5.1 ralentit énormément le téléchargement
[Console]::OutputEncoding = [Text.Encoding]::UTF8
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

function Write-Step($text) { Write-Host ''; Write-Host "== $text" -ForegroundColor Cyan }
function Write-Ok($text)   { Write-Host "   $text" -ForegroundColor Green }
function Write-Info($text) { Write-Host "   $text" }

# Dossier Valheim via le registre Steam et les bibliothèques (libraryfolders.vdf). Copie d'Installer-Ovomium.bat.
function Find-ValheimFromSteam {
    $steam = (Get-ItemProperty -Path 'HKCU:\Software\Valve\Steam' -ErrorAction SilentlyContinue).SteamPath
    if (-not $steam) { return $null }
    $steam = $steam -replace '/', '\'
    $libraries = @($steam)
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    if (Test-Path -LiteralPath $vdf) {
        foreach ($m in [regex]::Matches((Get-Content -Raw -LiteralPath $vdf), '"path"\s+"([^"]+)"')) {
            $libraries += ($m.Groups[1].Value -replace '\\\\', '\')
        }
    }
    foreach ($lib in $libraries) {
        try {   # une bibliothèque sur un disque débranché fait échouer Join-Path/Test-Path : on l'ignore
            $acf = Join-Path $lib "steamapps\appmanifest_$VALHEIM_APPID.acf"
            if (-not (Test-Path -LiteralPath $acf)) { continue }
            $dir = 'Valheim'
            $m = [regex]::Match((Get-Content -Raw -LiteralPath $acf), '"installdir"\s+"([^"]+)"')
            if ($m.Success) { $dir = $m.Groups[1].Value }
            $game = Join-Path $lib "steamapps\common\$dir"
            if (Test-Path -LiteralPath (Join-Path $game 'valheim.exe')) { return $game }
        } catch { continue }
    }
    return $null
}

function Ask-ValheimFolder {
    Write-Info "Dossier de Valheim introuvable via Steam."
    Write-Info "Dans Steam : clic droit sur Valheim > Gérer > Parcourir les fichiers locaux, puis copie le chemin ici."
    while ($true) {
        $answer = Read-Host '   Chemin du dossier de Valheim (vide pour annuler)'
        $answer = $answer.Trim().Trim('"')
        if (-not $answer) { throw "Import annulé : dossier de Valheim inconnu." }
        if (Test-Path -LiteralPath (Join-Path $answer 'valheim.exe')) { return $answer }
        Write-Info "Pas de valheim.exe dans « $answer », réessaie."
    }
}

# Copie $name du paquet dans $destDir. Un fichier existant est gardé en .bak, sauf si un .bak existe déjà :
# c'est alors l'original d'un premier import, qu'on ne veut pas écraser avec notre propre copie.
function Copy-WithBackup($name, $destDir) {
    if (-not (Test-Path -LiteralPath $destDir)) { New-Item -ItemType Directory -Path $destDir -Force | Out-Null }
    $dest = Join-Path $destDir $name
    if ((Test-Path -LiteralPath $dest) -and -not (Test-Path -LiteralPath "$dest.bak")) {
        Copy-Item -LiteralPath $dest -Destination "$dest.bak" -Force
        Write-Info "Ancien $name gardé : $dest.bak"
    }
    Copy-Item -LiteralPath (Join-Path $here $name) -Destination $dest -Force
    Write-Ok "$name -> $destDir"
}

# reg.exe écrit parfois son message de succès sur stderr, que PowerShell 5.1 transforme en erreur : on juge au code retour.
function Invoke-Reg([string[]]$arguments) {
    $previous = $ErrorActionPreference
    $ErrorActionPreference = 'Continue'
    try { $output = & reg.exe @arguments 2>&1 | Out-String } finally { $ErrorActionPreference = $previous }
    if ($LASTEXITCODE -ne 0) { throw "reg $($arguments[0]) a échoué : $($output.Trim())" }
}

# Télécharge Installer-Ovomium.bat et exécute sa partie PowerShell dans cette console (sans son « pause » final).
function Install-Ovomium($game) {
    $bat = Join-Path $env:TEMP "Installer-Ovomium-$PID.bat"
    try {
        Invoke-WebRequest -Uri $OVOMIUM_INSTALLER_URL -OutFile $bat -UseBasicParsing -Headers @{ 'User-Agent' = 'Ovomium-Installer' }
    } catch {
        # Le dépôt n'est ouvert au public que le temps des mises à jour : 404 le reste du temps.
        throw "Téléchargement d'Installer-Ovomium.bat impossible ($($_.Exception.Message)). Les mises à jour ne sont peut-être pas ouvertes en ce moment : demande à Edia, puis relance ce fichier."
    }
    $self = $env:OVOMIUM_SELF
    $env:OVOMIUM_SELF = $bat
    try {
        & powershell.exe -NoProfile -ExecutionPolicy Bypass -Command '& ([ScriptBlock]::Create((Get-Content -Raw -LiteralPath $env:OVOMIUM_SELF -Encoding UTF8)))'
    } finally {
        $env:OVOMIUM_SELF = $self
        Remove-Item -LiteralPath $bat -Force -ErrorAction SilentlyContinue
    }
    if (-not (Test-Path -LiteralPath (Join-Path $game 'BepInEx\core\BepInEx.dll'))) {
        throw "Ovomium n'a pas pu être installé (voir le message ci-dessus). Relance ce fichier plus tard ou demande à Edia."
    }
}

# Renvoie la ligne du résumé final.
function Install-Vhvr($game) {
    $dll = Join-Path $game 'BepInEx\plugins\ValheimVRMod.dll'
    if ((Test-Path -LiteralPath $dll) -and (Get-FileHash -LiteralPath $dll -Algorithm SHA256).Hash -eq $VHVR_DLL_SHA256) {
        Write-Ok "VHVR $VHVR_VERSION déjà installé."
        return "VHVR $VHVR_VERSION déjà installé."
    }
    $answer = Read-Host "   Télécharger et installer VHVR $VHVR_VERSION maintenant ? (O/n, Entrée = oui)"
    if ("$answer".Trim().ToLower() -eq 'n') { Write-Info 'VHVR non installé.'; return 'VHVR non installé (relancer ce fichier pour l''installer).' }
    $zip = Join-Path $env:TEMP "vhvr-$PID.zip"
    Write-Info 'Téléchargement de vhvr.zip (6 Mo)…'
    Invoke-WebRequest -Uri $VHVR_URL -OutFile $zip -UseBasicParsing -Headers @{ 'User-Agent' = 'Ovomium-Installer' }
    Write-Info "Extraction dans $game…"
    Expand-Archive -LiteralPath $zip -DestinationPath $game -Force
    Remove-Item -LiteralPath $zip -Force -ErrorAction SilentlyContinue
    if (-not (Test-Path -LiteralPath $dll)) { throw "Installation de VHVR incomplète : ValheimVRMod.dll introuvable dans BepInEx\plugins." }
    Write-Ok "VHVR $VHVR_VERSION installé."
    return "VHVR $VHVR_VERSION installé."
}

$done = @()
try {
    Write-Host 'Ovomium - import de la configuration Valheim' -ForegroundColor Yellow
    $here = Split-Path -Parent $env:OVOMIUM_SELF
    $files = 'ovo.ovomium.cfg', 'favorite', 'recent', 'valheim-prefs.reg'
    foreach ($f in $files) {
        if (-not (Test-Path -LiteralPath (Join-Path $here $f))) {
            throw "Fichier $f absent à côté de ce script. Extrais d'abord tout le zip (clic droit > Extraire tout), puis lance Installer-Config.bat depuis le dossier extrait."
        }
    }

    Write-Step 'Vérification que Valheim est fermé'
    if (Get-Process -Name 'valheim' -ErrorAction SilentlyContinue) {
        throw "Valheim est en cours d'exécution (il réécrit ses réglages en quittant) : ferme le jeu, puis relance ce fichier."
    }
    Write-Ok 'Valheim est fermé.'

    Write-Step 'Recherche du dossier de Valheim'
    $game = Find-ValheimFromSteam
    if (-not $game) { $game = Ask-ValheimFolder }
    Write-Ok $game

    Write-Step 'Vérification qu''Ovomium est installé'
    if (Test-Path -LiteralPath (Join-Path $game 'BepInEx\core\BepInEx.dll')) {
        Write-Ok 'Ovomium (BepInEx) est installé.'
    } else {
        Write-Info 'Pas encore installé : lancement d''Installer-Ovomium.bat (dernière version)…'
        Install-Ovomium $game
        Write-Host ''
        Write-Ok 'Ovomium installé, suite de l''import.'
        $done += 'Ovomium installé.'
    }

    Write-Step 'Copie de la configuration d''Ovomium'
    Copy-WithBackup 'ovo.ovomium.cfg' (Join-Path $game 'BepInEx\config')
    $done += 'Configuration d''Ovomium copiée (vue subjective FirstPerson désactivée, pour la VR).'

    Write-Step 'Copie des serveurs favoris et récents'
    $serverDir = Join-Path $env:USERPROFILE 'AppData\LocalLow\IronGate\Valheim\serverlist_local'
    Copy-WithBackup 'favorite' $serverDir
    Copy-WithBackup 'recent' $serverDir
    $done += 'Serveurs favoris et récents copiés.'

    Write-Step 'Réglages du jeu (touches, son, interface) dans le registre'
    # Sauvegarde unique, comme les .bak : un nouvel import (ex. relance automatique) ne remplace pas l'original.
    $backup = Join-Path $env:USERPROFILE 'AppData\LocalLow\IronGate\Valheim\valheim-prefs-avant-import.reg'
    if ((Test-Path -LiteralPath 'HKCU:\Software\IronGate\Valheim') -and -not (Test-Path -LiteralPath $backup)) {
        New-Item -ItemType Directory -Path (Split-Path -Parent $backup) -Force | Out-Null
        Invoke-Reg @('export', $PREFS_KEY, $backup, '/y')
        Write-Info "Anciens réglages sauvegardés : $backup"
    }
    Invoke-Reg @('import', (Join-Path $here 'valheim-prefs.reg'))
    Write-Ok 'Réglages importés (les réglages graphiques de ce PC sont conservés).'
    $done += 'Réglages du jeu importés (touches, son, interface ; graphismes inchangés).'

    Write-Step 'Mod de réalité virtuelle VHVR'
    $done += Install-Vhvr $game

    Write-Host ''
    Write-Host 'Terminé :' -ForegroundColor Green
    foreach ($line in $done) { Write-Host "   - $line" -ForegroundColor Green }
    Write-Host 'Pour revenir en arrière : voir LISEZMOI.txt (fichiers .bak et sauvegarde du registre).'
}
catch {
    Write-Host ''
    Write-Host "Erreur : $($_.Exception.Message)" -ForegroundColor Red
    if ($done) {
        Write-Host 'Déjà fait avant l''erreur :'
        foreach ($line in $done) { Write-Host "   - $line" }
    } else {
        Write-Host 'Rien n''a été modifié.'
    }
    Write-Host 'Envoie ce message à Edia si besoin.'
}
