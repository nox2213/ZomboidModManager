function Rename-LogFile {
    param (
        [string]$logFile
    )
    # Überprüfen, ob der Log-Pfad korrekt ist
    $logDir = Split-Path -Parent $logFile
    if (-Not (Test-Path $logDir)) {
        Write-Host "Log-Verzeichnis '$logDir' existiert nicht. Erstelle es..."
        New-Item -Path $logDir -ItemType Directory -Force | Out-Null
    }

    # Überprüfen, ob die Log-Datei existiert
    if (-Not (Test-Path $logFile)) {
        Write-Host "Log-Datei '$logFile' existiert nicht. Erstelle sie..."
        New-Item -Path $logFile -ItemType File | Out-Null
    } else {
        # Erzeuge neuen Dateinamen mit Zeitstempel
        $timestamp = Get-Date -Format "yyyyMMdd_HHmmss"
        $newLogFile = Join-Path -Path $logDir -ChildPath ("starter_$timestamp.log")

        # Umbenennen der alten Log-Datei
        Write-Host "Benenne Log-Datei um in '$newLogFile'..."
        Rename-Item -Path $logFile -NewName $newLogFile -Force
    }
}

# Logrotation
$logFile = "$PSScriptRoot/log/starter.log"
Rename-LogFile -logFile $logFile

# Hauptprogramm
Add-Content -Path $logFile -Value "INFO: Skript gestartet"

function Install-PythonAndPip {
    $scriptDir = Split-Path -Path $PSCommandPath -Parent
    $pythonDir = Join-Path -Path $scriptDir -ChildPath "src/python-3.11.9-embed-amd64"
    $pythonPath = Join-Path -Path $pythonDir -ChildPath "python.exe"
    $getPipPath = Join-Path -Path $scriptDir -ChildPath "src/scripts/get-pip.py"

    if (-Not (Test-Path $pythonDir)) {
        Write-Host "Python-Verzeichnis nicht gefunden. Lade es herunter und entpacke es..."
        Add-Content -Path $logFile -Value "INFO: Python wird heruntergeladen und entpackt."
        Invoke-WebRequest -Uri "https://www.python.org/ftp/python/3.11.9/python-3.11.9-embed-amd64.zip" -OutFile "$scriptDir/src/python-embed.zip"
        New-Item -ItemType Directory -Path $pythonDir -Force | Out-Null
        Expand-Archive -Path "$scriptDir/src/python-embed.zip" -DestinationPath $pythonDir
        Remove-Item "$scriptDir/src/python-embed.zip"

        $pthFile = Join-Path -Path $pythonDir -ChildPath "python311._pth"
        if (Test-Path $pthFile) {
            $pthContent = Get-Content -Path $pthFile -Encoding utf8
            $updatedContent = $pthContent -replace "#import site", "import site"
            Set-Content -Path $pthFile -Value $updatedContent -Encoding utf8
            Add-Content -Path $logFile -Value "INFO: python311._pth wurde angepasst."
        } else {
            Write-Host "Fehler: python311._pth nicht gefunden."
            Add-Content -Path $logFile -Value "ERROR: python311._pth nicht gefunden."
            exit 1
        }
    }

    if (-Not (Test-Path $getPipPath)) {
        Write-Host "get-pip.py wird heruntergeladen..."
        Add-Content -Path $logFile -Value "INFO: get-pip.py wird heruntergeladen."
        Invoke-WebRequest -Uri "https://bootstrap.pypa.io/get-pip.py" -OutFile $getPipPath
    }

    Write-Host "Installiere pip..."
    Add-Content -Path $logFile -Value "INFO: Installiere pip."
    & $pythonPath $getPipPath 2>&1 | Add-Content -Path $logFile

    if (Test-Path $getPipPath) {
        Write-Host "Entferne get-pip.py..."
        Remove-Item $getPipPath -Force
        Add-Content -Path $logFile -Value "INFO: get-pip.py wurde entfernt."
    }
}

function Install-Requirements {
    # Sicherstellen, dass SteamCMD vorhanden ist
    $scriptDir = Split-Path -Path $PSCommandPath -Parent
    $steamCmdDir = Join-Path -Path $scriptDir -ChildPath "src/steamcmd"
    $steamCmdPath = Join-Path -Path $steamCmdDir -ChildPath "steamcmd.exe"

    if (-Not (Test-Path $steamCmdPath)) {
        Write-Host "SteamCMD wird heruntergeladen..."
        Add-Content -Path $logFile -Value "INFO: SteamCMD wird heruntergeladen."
        Invoke-WebRequest -Uri "https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip" -OutFile "$scriptDir/src/steamcmd.zip"
        Expand-Archive -Path "$scriptDir/src/steamcmd.zip" -DestinationPath $steamCmdDir
        Remove-Item "$scriptDir/src/steamcmd.zip"

        if (-Not (Test-Path $steamCmdPath)) {
            Write-Host "Fehler: SteamCMD konnte nicht heruntergeladen oder entpackt werden."
            Add-Content -Path $logFile -Value "ERROR: SteamCMD konnte nicht bereitgestellt werden."
            exit 1
        }
    }

    Write-Host "SteamCMD ist bereitgestellt."
    Add-Content -Path $logFile -Value "INFO: SteamCMD ist bereitgestellt."
    $scriptDir = Split-Path -Path $PSCommandPath -Parent
    $pythonDir = Join-Path -Path $scriptDir -ChildPath "src/python-3.11.9-embed-amd64"
    $pythonPath = Join-Path -Path $pythonDir -ChildPath "python.exe"
    $requirementsPath = Join-Path -Path $scriptDir -ChildPath "src/scripts/requirements.txt"

    Write-Host "Installiere Anforderungen aus requirements.txt..."
    Add-Content -Path $logFile -Value "INFO: Installiere Anforderungen aus requirements.txt."
    & $pythonPath -m pip install -r $requirementsPath 2>&1 | Add-Content -Path $logFile

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler beim Installieren der Anforderungen."
        Add-Content -Path $logFile -Value "ERROR: Fehler beim Installieren der Anforderungen."
        exit 1
    }
}

function Invoke-Extraktor {
    $scriptDir = Split-Path -Path $PSCommandPath -Parent
    Add-Content -Path $logFile -Value "DEBUG: Starte extraktor.py"
    $pythonPath = Join-Path -Path $scriptDir -ChildPath "src/python-3.11.9-embed-amd64/python.exe"
    $scriptPath = Join-Path -Path $scriptDir -ChildPath "src/scripts/extraktor.py"

    if (-Not (Test-Path $scriptPath)) {
        Write-Host "Fehler: extraktor.py nicht gefunden. Bitte prüfen Sie, ob die Datei vorhanden ist."
        Add-Content -Path $logFile -Value "ERROR: extraktor.py nicht gefunden."
        exit 1
    }

    Write-Host "Starte extraktor.py..."
    & $pythonPath $scriptPath 2>&1 | Tee-Object -FilePath "$scriptDir/log/extractor_output.log"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler beim Ausführen von extraktor.py. Siehe extractor_output.log für Details."
        Add-Content -Path $logFile -Value "ERROR: Fehler beim Ausführen von extraktor.py"
        exit 1
    }

    Add-Content -Path $logFile -Value "INFO: extraktor.py erfolgreich ausgeführt"
}

function Invoke-DeepSearch {
    $scriptDir = Split-Path -Path $PSCommandPath -Parent
    Add-Content -Path $logFile -Value "DEBUG: Starte steam_extract.py"
    $pythonPath = Join-Path -Path $scriptDir -ChildPath "src/python-3.11.9-embed-amd64/python.exe"
    $scriptPath = Join-Path -Path $scriptDir -ChildPath "src/scripts/steam_extract.py"

    if (-Not (Test-Path $scriptPath)) {
        Write-Host "Fehler: steam_extract.py nicht gefunden. Bitte prüfen Sie, ob die Datei vorhanden ist."
        Add-Content -Path $logFile -Value "ERROR: steam_extract.py nicht gefunden."
        exit 1
    }

    Write-Host "Starte steam_extract.py..."
    & $pythonPath $scriptPath 2>&1 | Tee-Object -FilePath "$scriptDir/log/steam_extract_output.log"

    if ($LASTEXITCODE -ne 0) {
        Write-Host "Fehler beim Ausführen von steam_extract.py. Siehe steam_extract_output.log für Details."
        Add-Content -Path $logFile -Value "ERROR: Fehler beim Ausführen von steam_extract.py"
        exit 1
    }

    Add-Content -Path $logFile -Value "INFO: steam_extract.py erfolgreich ausgeführt"
}

# Überprüfen und Bereitstellen von Python und Pip
Install-PythonAndPip
Install-Requirements

# Auswahlmenü für den Benutzer
Write-Host "Bitte wählen Sie die gewünschte Option:"
Write-Host "1: Schnelle Extraktion (extraktor.py)"
Write-Host "2: DeepSearch (steam_extract.py)"
$choice = Read-Host "Geben Sie die Nummer Ihrer Wahl ein (1 oder 2)"

switch ($choice) {
    "1" {
        Write-Host "Schnelle Extraktion mit extraktor.py wird gestartet..."
        Invoke-Extraktor
    }
    "2" {
        Write-Host "DeepSearch mit steam_extract.py wird gestartet..."
        Invoke-DeepSearch
    }
    default {
        Write-Host "Ungültige Auswahl. Bitte starten Sie das Skript erneut und wählen Sie eine gültige Option."
        Add-Content -Path $logFile -Value "ERROR: Ungültige Auswahl des Benutzers."
        exit 1
    }
}

Add-Content -Path $logFile -Value "INFO: Skript erfolgreich abgeschlossen"
Write-Host "Skript abgeschlossen. Drücken Sie eine beliebige Taste, um fortzufahren..."
Pause
