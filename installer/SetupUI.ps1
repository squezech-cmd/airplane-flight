# SetupUI.ps1 — графический установщик Dear Passengers с выбором диска и папки.
# Запускается self-extracting установщиком после распаковки во временную папку.
# Два режима:
#   • рядом DearPassengers.exe  → установка готовой игры (без Unity);
#   • рядом Install.bat         → установка исходников + автосборка через Unity.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$src = $PSScriptRoot
$gameMode = Test-Path (Join-Path $src 'DearPassengers.exe')
$modeText = if ($gameMode) { 'Готовая игра — установка займёт пару минут' } else { 'Исходники + автосборка через Unity (редактор поставится сам)' }

# --- Диск по умолчанию: fixed-диск с максимумом свободного места ---
$best = $null
foreach ($d in [System.IO.DriveInfo]::GetDrives()) {
    if ($d.DriveType -eq 'Fixed' -and $d.IsReady) {
        if ($null -eq $best -or $d.TotalFreeSpace -gt $best.TotalFreeSpace) { $best = $d }
    }
}
$defaultDir = if ($best) { Join-Path $best.RootDirectory 'Games\Dear Passengers' } else { 'C:\Games\Dear Passengers' }

# --- Форма ---
$form = New-Object System.Windows.Forms.Form
$form.Text = 'Dear Passengers — установка'
$form.ClientSize = New-Object System.Drawing.Size(600, 380)
$form.StartPosition = 'CenterScreen'
$form.FormBorderStyle = 'FixedDialog'
$form.MaximizeBox = $false

function New-Label([string]$text, [int]$x, [int]$y, [int]$w, [int]$h, [float]$size, [System.Drawing.FontStyle]$style, [System.Drawing.Color]$color) {
    $l = New-Object System.Windows.Forms.Label
    $l.Text = $text
    $l.Location = New-Object System.Drawing.Point($x, $y)
    $l.Size = New-Object System.Drawing.Size($w, $h)
    $l.Font = New-Object System.Drawing.Font('Segoe UI', $size, $style)
    $l.ForeColor = $color
    return $l
}

$orange = [System.Drawing.Color]::FromArgb(242, 115, 33)
$dimgray = [System.Drawing.Color]::FromArgb(110, 110, 110)

$form.Controls.Add((New-Label 'DEAR PASSENGERS' 20 14 420 42 20 ([System.Drawing.FontStyle]::Bold) $orange))
$form.Controls.Add((New-Label '18+ | Создана в развлекательных целях. Не повторяйте в реальной жизни. Все совпадения случайны.' 20 56 560 20 8.25 ([System.Drawing.FontStyle]::Regular) $dimgray))
$form.Controls.Add((New-Label $modeText 20 76 560 20 8.25 ([System.Drawing.FontStyle]::Regular) $dimgray))

$form.Controls.Add((New-Label 'Куда установить:' 20 108 200 20 9.75 ([System.Drawing.FontStyle]::Bold) ([System.Drawing.Color]::Black)))

$txt = New-Object System.Windows.Forms.TextBox
$txt.Location = New-Object System.Drawing.Point(20, 130)
$txt.Size = New-Object System.Drawing.Size(430, 24)
$txt.Font = New-Object System.Drawing.Font('Segoe UI', 9.75)
$txt.Text = $defaultDir
$form.Controls.Add($txt)

$btnBrowse = New-Object System.Windows.Forms.Button
$btnBrowse.Text = 'Обзор…'
$btnBrowse.Location = New-Object System.Drawing.Point(460, 128)
$btnBrowse.Size = New-Object System.Drawing.Size(120, 28)
$form.Controls.Add($btnBrowse)

$lblFree = New-Object System.Windows.Forms.Label
$lblFree.Location = New-Object System.Drawing.Point(20, 158)
$lblFree.Size = New-Object System.Drawing.Size(560, 18)
$lblFree.Font = New-Object System.Drawing.Font('Segoe UI', 8.25)
$lblFree.ForeColor = $dimgray
$form.Controls.Add($lblFree)

$chkLnk = New-Object System.Windows.Forms.CheckBox
$chkLnk.Text = 'Ярлыки на рабочем столе и в меню «Пуск»'
$chkLnk.Location = New-Object System.Drawing.Point(20, 184)
$chkLnk.Size = New-Object System.Drawing.Size(400, 22)
$chkLnk.Checked = $true
$form.Controls.Add($chkLnk)

$chkRun = New-Object System.Windows.Forms.CheckBox
$chkRun.Text = 'Запустить игру сразу после установки'
$chkRun.Location = New-Object System.Drawing.Point(20, 208)
$chkRun.Size = New-Object System.Drawing.Size(400, 22)
$chkRun.Checked = $true
$form.Controls.Add($chkRun)

$progress = New-Object System.Windows.Forms.ProgressBar
$progress.Location = New-Object System.Drawing.Point(20, 240)
$progress.Size = New-Object System.Drawing.Size(560, 22)
$form.Controls.Add($progress)

$status = New-Object System.Windows.Forms.Label
$status.Location = New-Object System.Drawing.Point(20, 268)
$status.Size = New-Object System.Drawing.Size(560, 40)
$status.Font = New-Object System.Drawing.Font('Segoe UI', 8.75)
$status.Text = ''
$form.Controls.Add($status)

$btnInstall = New-Object System.Windows.Forms.Button
$btnInstall.Text = 'УСТАНОВИТЬ'
$btnInstall.Location = New-Object System.Drawing.Point(340, 320)
$btnInstall.Size = New-Object System.Drawing.Size(240, 40)
$btnInstall.Font = New-Object System.Drawing.Font('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)
$btnInstall.BackColor = $orange
$btnInstall.ForeColor = [System.Drawing.Color]::White
$btnInstall.FlatStyle = 'Flat'
$form.Controls.Add($btnInstall)

$btnCancel = New-Object System.Windows.Forms.Button
$btnCancel.Text = 'Отмена'
$btnCancel.Location = New-Object System.Drawing.Point(240, 326)
$btnCancel.Size = New-Object System.Drawing.Size(90, 30)
$form.Controls.Add($btnCancel)

# --- Функции ---

function Get-FreeText([string]$path) {
    try {
        $q = Split-Path -Qualifier $path -ErrorAction Stop
        $d = Get-PSDrive ($q.TrimEnd(':')) -ErrorAction Stop
        return ('Свободно на диске {0}: {1:N1} ГБ' -f $q, ($d.Free / 1GB))
    } catch { return '' }
}

function Make-Shortcuts([string]$target) {
    $ws = New-Object -ComObject WScript.Shell
    $dirs = @([Environment]::GetFolderPath('Desktop'), (Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs'))
    foreach ($d in $dirs) {
        $s = $ws.CreateShortcut((Join-Path $d 'Dear Passengers.lnk'))
        $s.TargetPath = $target
        $s.WorkingDirectory = Split-Path $target
        $s.IconLocation = "$target,0"
        $s.Save()
    }
}

$txt.Add_TextChanged({ $lblFree.Text = Get-FreeText $txt.Text })

$btnBrowse.Add_Click({
    $fbd = New-Object System.Windows.Forms.FolderBrowserDialog
    $fbd.Description = 'Выберите диск и папку для установки Dear Passengers'
    $fbd.ShowNewFolderButton = $true
    try { $fbd.SelectedPath = $txt.Text } catch { }
    if ($fbd.ShowDialog($form) -eq [System.Windows.Forms.DialogResult]::OK) {
        $txt.Text = $fbd.SelectedPath
    }
})

$btnCancel.Add_Click({ $form.Close() })

$btnInstall.Add_Click({
    try {
        $btnInstall.Enabled = $false
        $btnBrowse.Enabled = $false

        $dst = $txt.Text.Trim()
        if ([string]::IsNullOrWhiteSpace($dst)) { throw 'Укажите папку для установки.' }
        $dst = [System.IO.Path]::GetFullPath($dst)
        $root = [System.IO.Path]::GetPathRoot($dst)
        if ($dst.TrimEnd('\') -eq $root.TrimEnd('\')) { throw 'Нельзя устанавливать в корень диска — выберите или создайте папку (например, C:\Games\Dear Passengers).' }
        if ($dst.StartsWith($src, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Выберите папку вне временной директории установщика.' }

        # Свободное место.
        $drive = New-Object System.IO.DriveInfo($root)
        $needGb = if ($gameMode) { 1 } else { 3 }
        if (-not $drive.IsReady) { throw "Диск $root недоступен." }
        if ($drive.TotalFreeSpace -lt $needGb * 1GB) {
            throw ('Недостаточно места на {0}: свободно {1:N1} ГБ, нужно минимум {2} ГБ.' -f $root, ($drive.TotalFreeSpace / 1GB), $needGb)
        }
        if (-not $gameMode -and $drive.TotalFreeSpace -lt 6GB) {
            [System.Windows.Forms.MessageBox]::Show("Внимание: на $root всего $([math]::Round($drive.TotalFreeSpace/1GB,1)) ГБ. Для сборки проекта желательно 6+ ГБ.", 'Dear Passengers', 'OK', 'Warning') | Out-Null
        }

        New-Item -ItemType Directory -Path $dst -Force | Out-Null
        $status.Text = 'Проверка прав записи…'
        [System.Windows.Forms.Application]::DoEvents()
        $probe = Join-Path $dst '.write_test'
        'ok' | Out-File -FilePath $probe -Encoding ascii
        Remove-Item $probe -Force

        # Копирование с прогрессом.
        $items = @(Get-ChildItem -LiteralPath $src -Force)
        $progress.Maximum = $items.Count
        $progress.Value = 0
        foreach ($it in $items) {
            $status.Text = 'Копирую: ' + $it.Name
            [System.Windows.Forms.Application]::DoEvents()
            Copy-Item -LiteralPath $it.FullName -Destination $dst -Recurse -Force
            $progress.Value++
        }

        $gameExe = if ($gameMode) { Join-Path $dst 'DearPassengers.exe' } else { Join-Path $dst 'builds\DearPassengers\DearPassengers.exe' }

        # Сборка (только режим исходников и только если игры ещё нет).
        if (-not $gameMode -and -not (Test-Path $gameExe)) {
            $status.Text = 'Дальше — в консоли: проверка Unity, сборка игры. Не закрывайте чёрное окно!'
            $progress.Style = 'Marquee'
            [System.Windows.Forms.Application]::DoEvents()
            $batArgs = @('/c', 'Install.bat')
            if (-not $chkLnk.Checked) { $batArgs += 'noshortcuts' }
            Start-Process -FilePath 'cmd.exe' -ArgumentList $batArgs -WorkingDirectory $dst -Wait
            $progress.Style = 'Continuous'
        }

        if (-not (Test-Path $gameExe)) {
            [System.Windows.Forms.MessageBox]::Show("Файлы скопированы в:`n$dst`n`nОднако игра не собралась (подробнее — build.log в папке установки).`nЗапустите Install.bat в папке установки после входа в Unity ID.", 'Dear Passengers', 'OK', 'Warning') | Out-Null
        } else {
            if ($gameMode -and $chkLnk.Checked) { Make-Shortcuts $gameExe }
            if ($chkRun.Checked) { Start-Process -FilePath $gameExe -WorkingDirectory (Split-Path $gameExe) }
            $r = [System.Windows.Forms.MessageBox]::Show("Готово! Игра установлена в:`n$dst`n`nОткрыть папку установки?", 'Dear Passengers — установка завершена', 'YesNo', 'Information')
            if ($r -eq [System.Windows.Forms.DialogResult]::Yes) { Start-Process explorer.exe $dst }
        }
        $form.Close()
    }
    catch {
        [System.Windows.Forms.MessageBox]::Show($_.Exception.Message, 'Ошибка установки', 'OK', 'Error') | Out-Null
        $btnInstall.Enabled = $true
        $btnBrowse.Enabled = $true
    }
})

$lblFree.Text = Get-FreeText $defaultDir
$form.Add_Shown({ $form.Activate() })
[void]$form.ShowDialog()
