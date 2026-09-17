<# : batch portion
@echo off
rem Ce fichier est a la fois un .bat et un script PowerShell 5.1 (Windows 10/11). Lignes CRLF obligatoires.
setlocal
chcp 65001 >nul
title Ovomium - installation / mise a jour
set "OVOMIUM_SELF=%~f0"
powershell -NoProfile -ExecutionPolicy Bypass -Command "& ([ScriptBlock]::Create((Get-Content -Raw -LiteralPath $env:OVOMIUM_SELF -Encoding UTF8)))"
echo.
pause
exit /b
#>

# Installe ou met à jour Ovomium (mod Valheim) depuis la dernière release GitHub :
# BepInEx + BepInEx/plugins/Ovomium/ copiés dans le dossier du jeu trouvé via Steam.
# GITHUB_REPO doit rester identique dans tools/release.sh et installer/Installer-Ovomium.bat.
$GITHUB_REPO = 'Delocca/valheim-ovomium'
$VALHEIM_APPID = '892970'

$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'   # la barre de progression de PowerShell 5.1 ralentit énormément le téléchargement
[Console]::OutputEncoding = [Text.Encoding]::UTF8
[Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12

function Write-Step($text) { Write-Host ''; Write-Host "== $text" -ForegroundColor Cyan }
function Write-Ok($text)   { Write-Host "   $text" -ForegroundColor Green }
function Write-Info($text) { Write-Host "   $text" }

# Dossier Valheim via le registre Steam et les bibliothèques (libraryfolders.vdf).
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
        if (-not $answer) { throw "Installation annulée : dossier de Valheim inconnu." }
        if (Test-Path -LiteralPath (Join-Path $answer 'valheim.exe')) { return $answer }
        Write-Info "Pas de valheim.exe dans « $answer », réessaie."
    }
}

try {
    Write-Host 'Ovomium - installation / mise à jour du mod Valheim' -ForegroundColor Yellow

    Write-Step 'Vérification que Valheim est fermé'
    if (Get-Process -Name 'valheim' -ErrorAction SilentlyContinue) {
        throw "Valheim est en cours d'exécution : ferme le jeu, puis relance ce fichier."
    }
    Write-Ok 'Valheim est fermé.'

    Write-Step 'Recherche du dossier de Valheim'
    $game = Find-ValheimFromSteam
    if (-not $game) { $game = Ask-ValheimFolder }
    Write-Ok $game

    Write-Step 'Recherche de la dernière version sur GitHub'
    try {
        $release = Invoke-RestMethod -Uri "https://api.github.com/repos/$GITHUB_REPO/releases/latest" `
            -Headers @{ 'User-Agent' = 'Ovomium-Installer'; 'Accept' = 'application/vnd.github+json' }
    } catch {
        # Le dépôt n'est ouvert au public que le temps des mises à jour : 404 le reste du temps.
        if ($_.Exception.Response -and [int]$_.Exception.Response.StatusCode -eq 404) {
            throw "Les mises à jour ne sont pas ouvertes en ce moment : demande à Edia, puis relance ce fichier."
        }
        throw
    }
    $asset = @($release.assets | Where-Object { $_.name -like 'Ovomium-*-windows.zip' })[0]
    if (-not $asset) { throw "La release $($release.tag_name) ne contient pas d'archive Ovomium-*-windows.zip." }
    $latest = [regex]::Match($asset.name, '^Ovomium-(.+)-windows\.zip$').Groups[1].Value
    Write-Ok "Dernière version : $latest"

    $versionFile = Join-Path $game 'BepInEx\plugins\Ovomium\version.txt'
    $installed = $null
    if (Test-Path -LiteralPath $versionFile) { $installed = (Get-Content -Raw -LiteralPath $versionFile).Trim() }
    if ($installed) { Write-Info "Version installée : $installed" } else { Write-Info 'Ovomium n''est pas encore installé.' }
    if ($installed -eq $latest) {
        Write-Ok "Déjà à jour ($installed), rien à faire."
        $again = Read-Host '   Réinstaller quand même ? (o/N)'
        if ($again.Trim().ToLower() -ne 'o') { Write-Host ''; Write-Host 'Relance le même fichier plus tard pour mettre à jour.'; return }
    }

    Write-Step "Téléchargement de $($asset.name) ($([math]::Round($asset.size / 1KB)) Ko)"
    $work = Join-Path $env:TEMP "Ovomium-install-$PID"
    if (Test-Path -LiteralPath $work) { Remove-Item -LiteralPath $work -Recurse -Force }
    New-Item -ItemType Directory -Path $work | Out-Null
    $zip = Join-Path $work $asset.name
    Invoke-WebRequest -Uri $asset.browser_download_url -OutFile $zip -UseBasicParsing -Headers @{ 'User-Agent' = 'Ovomium-Installer' }
    Write-Ok 'Téléchargé.'

    Write-Step 'Extraction'
    $extracted = Join-Path $work 'extracted'
    Expand-Archive -LiteralPath $zip -DestinationPath $extracted -Force
    if (-not (Test-Path -LiteralPath (Join-Path $extracted 'BepInEx\plugins\Ovomium\Ovomium.dll'))) {
        throw "Archive inattendue : Ovomium.dll introuvable après extraction."
    }
    Write-Ok 'Extrait.'

    Write-Step "Copie dans $game"
    Copy-Item -Path (Join-Path $extracted '*') -Destination $game -Recurse -Force
    foreach ($f in 'winhttp.dll', 'BepInEx\core\BepInEx.dll', 'BepInEx\plugins\Ovomium\Ovomium.dll', 'BepInEx\plugins\Ovomium\version.txt') {
        if (-not (Test-Path -LiteralPath (Join-Path $game $f))) { throw "Copie incomplète : $f manquant dans le dossier du jeu." }
    }
    Remove-Item -LiteralPath $work -Recurse -Force -ErrorAction SilentlyContinue
    Write-Ok 'Copié.'

    Write-Host ''
    if ($installed -and $installed -ne $latest) {
        Write-Host "Ovomium mis à jour : $installed -> $latest" -ForegroundColor Green
    } else {
        Write-Host "Ovomium $latest installé." -ForegroundColor Green
    }
    Write-Host 'Lance Valheim normalement depuis Steam : le mod se charge tout seul.'
    Write-Host 'Relance le même fichier plus tard pour mettre à jour.'
}
catch {
    Write-Host ''
    Write-Host "Erreur : $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "Rien n'a été cassé : le jeu reste tel quel. Réessaie plus tard ou envoie ce message à Edia."
}
