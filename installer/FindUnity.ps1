# FindUnity.ps1 — поиск Unity Hub.exe по всему ПК.
# Печатает найденный путь в stdout (его читает Install.bat через for /f).
# Порядок поиска:
#   1) стандартные папки (Program Files, LocalAppData, алиас Microsoft Store);
#   2) реестр: обработчик протокола unityhub:// (его пишет установщик Hub);
#   3) реестр: записи деинсталляции Unity Hub (HKLM/HKCU, обычный и WOW6432Node);
#   4) типовые папки в корне каждого жёсткого диска (D:\Unity Hub\... и т.п.);
#   5) полный обход всех жёстких дисков (медленно, но «по всему ПК»).

$ErrorActionPreference = 'SilentlyContinue'

function Test-Exe([string]$p) {
    if ([string]::IsNullOrWhiteSpace($p)) { return $false }
    return (Test-Path -LiteralPath $p)
}

# --- 1) стандартные места ---
$candidates = @(
    "$env:ProgramFiles\Unity Hub\Unity Hub.exe"
    "${env:ProgramFiles(x86)}\Unity Hub\Unity Hub.exe"
    "$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe"
    "$env:LOCALAPPDATA\Microsoft\WindowsApps\Unity Hub.exe"
)
foreach ($c in $candidates) {
    if (Test-Exe $c) { Write-Output $c; exit }
}

# --- 2) реестр: протокол unityhub:// ---
$proto = (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\unityhub\shell\open\command').'(default)'
if ($proto) {
    if ($proto -match '"([^"]+\.exe)"') {
        if (Test-Exe $Matches[1]) { Write-Output $Matches[1]; exit }
    }
    if ($proto -match '^(\S+\.exe)') {
        if (Test-Exe $Matches[1]) { Write-Output $Matches[1]; exit }
    }
}

# --- 3) реестр: записи деинсталляции ---
$keys = @(
    'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Unity Hub'
    'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Unity Hub'
    'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Unity Hub'
)
foreach ($k in $keys) {
    $p = Get-ItemProperty -LiteralPath $k
    foreach ($field in @($p.DisplayIcon, $p.InstallLocation)) {
        if (-not $field) { continue }
        $v = $field.Trim('"').Trim()
        if (Test-Exe $v) { Write-Output $v; exit }
        $joined = Join-Path $v 'Unity Hub.exe'
        if (Test-Exe $joined) { Write-Output $joined; exit }
    }
}

# --- 4) типовые папки в корне каждого жёсткого диска ---
foreach ($d in [IO.DriveInfo]::GetDrives()) {
    if ($d.DriveType -ne 'Fixed' -or -not $d.IsReady) { continue }
    foreach ($rel in @(
        'Unity Hub\Unity Hub.exe'
        'Program Files\Unity Hub\Unity Hub.exe'
        'Program Files (x86)\Unity Hub\Unity Hub.exe'
        'Programs\Unity Hub\Unity Hub.exe'
        'Games\Unity Hub\Unity Hub.exe'
        'Apps\Unity Hub\Unity Hub.exe'
        'Utils\Unity Hub\Unity Hub.exe'
        'Unity\Hub\Unity Hub.exe'
    )) {
        $p = Join-Path $d.RootDirectory.FullName $rel
        if (Test-Exe $p) { Write-Output $p; exit }
    }
}

# --- 5) полный обход всех жёстких дисков ---
Write-Host 'Deep search: scanning all drives for Unity Hub.exe - this can take a few minutes...'
foreach ($d in [IO.DriveInfo]::GetDrives()) {
    if ($d.DriveType -ne 'Fixed' -or -not $d.IsReady) { continue }
    $hit = Get-ChildItem -Path $d.RootDirectory.FullName -Filter 'Unity Hub.exe' -Recurse -File -Force -ErrorAction SilentlyContinue |
        Select-Object -First 1
    if ($hit) { Write-Output $hit.FullName; exit }
}
