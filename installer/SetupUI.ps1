# SetupUI.ps1 — установщик Dear Passengers «как в Steam»: одно графическое окно,
# этапы с галочками, прогресс, без видимых консолей. Запускается self-extracting
# установщиком после распаковки во временную папку.
# Режимы:
#   • рядом DearPassengers.exe → установка готовой игры (без Unity);
#   • иначе → установка исходников: автозагрузка Unity Hub + редактора, вход в
#     Unity ID (единственный ручной шаг), сборка игры, ярлыки, запуск.

$ErrorActionPreference = 'Continue'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$script:src = $PSScriptRoot
$script:gameMode = Test-Path (Join-Path $script:src 'DearPassengers.exe')
$script:unityVer = '2022.3.62f1'
$script:unityRev = '4af31df58517'

# ================= ПОИСК UNITY (по всему ПК) =================

function Find-Hub {
    $c = @(
        "$env:ProgramFiles\Unity Hub\Unity Hub.exe"
        "${env:ProgramFiles(x86)}\Unity Hub\Unity Hub.exe"
        "$env:LOCALAPPDATA\Programs\Unity Hub\Unity Hub.exe"
        "$env:LOCALAPPDATA\Microsoft\WindowsApps\Unity Hub.exe"
    )
    foreach ($x in $c) { if (Test-Path -LiteralPath $x) { return $x } }
    try {
        $proto = (Get-ItemProperty 'Registry::HKEY_CLASSES_ROOT\unityhub\shell\open\command' -ErrorAction Stop).'(default)'
        if ($proto -match '"([^"]+\.exe)"') { if (Test-Path -LiteralPath $Matches[1]) { return $Matches[1] } }
        if ($proto -match '^(\S+\.exe)') { if (Test-Path -LiteralPath $Matches[1]) { return $Matches[1] } }
    } catch {}
    foreach ($k in @(
        'HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Unity Hub'
        'HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Unity Hub'
        'HKCU:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Unity Hub'
    )) {
        try {
            $p = Get-ItemProperty -LiteralPath $k -ErrorAction Stop
            foreach ($f in @($p.DisplayIcon, $p.InstallLocation)) {
                if (-not $f) { continue }
                $v = $f.Trim('"').Trim()
                if ($v -and (Test-Path -LiteralPath $v) -and $v.EndsWith('.exe')) { return $v }
                $j = Join-Path $v 'Unity Hub.exe'
                if ($v -and (Test-Path -LiteralPath $j)) { return $j }
            }
        } catch {}
    }
    return $null
}

function Find-Editor {
    $v = $script:unityVer
    $c = @(
        "$env:ProgramFiles\Unity\Hub\Editor\$v\Editor\Unity.exe"
        "$env:LOCALAPPDATA\Programs\Unity\Hub\Editor\$v\Editor\Unity.exe"
    )
    foreach ($x in $c) { if (Test-Path -LiteralPath $x) { return $x } }
    try {
        $j = Get-Content -LiteralPath ($env:APPDATA + '\UnityHub\editors.json') -Raw -ErrorAction Stop | ConvertFrom-Json
        foreach ($e in $j.PSObject.Properties.Value) {
            if ($e.version -like "$v*" -and $e.location) {
                $u = Join-Path $e.location 'Editor\Unity.exe'
                if (Test-Path -LiteralPath $u) { return $u }
            }
        }
    } catch {}
    foreach ($d in [IO.DriveInfo]::GetDrives()) {
        if ($d.DriveType -eq 'Fixed' -and $d.IsReady) {
            $u = Join-Path $d.RootDirectory.FullName "Unity\Hub\Editor\$v\Editor\Unity.exe"
            if (Test-Path -LiteralPath $u) { return $u }
        }
    }
    return $null
}

# ================= ФОРМА =================

$orange = [System.Drawing.Color]::FromArgb(242, 115, 33)
$dim = [System.Drawing.Color]::FromArgb(110, 110, 110)

$form = New-Object System.Windows.Forms.Form
$form.Text = 'Dear Passengers — установка'
$form.ClientSize = New-Object System.Drawing.Size(620, 470)
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

$form.Controls.Add((New-Label 'DEAR PASSENGERS' 20 12 420 40 20 ([System.Drawing.FontStyle]::Bold) $orange))
$form.Controls.Add((New-Label '18+ | Создана в развлекательных целях. Не повторяйте в реальной жизни. Все совпадения случайны.' 20 52 580 18 8.25 ([System.Drawing.FontStyle]::Regular) $dim))

$modeText = if ($script:gameMode) { 'Готовая игра — пара минут и в путь' } else { 'Первая установка займёт 20–40 минут (загрузка Unity ~4 ГБ). Всё автоматически, участие не требуется.' }
$form.Controls.Add((New-Label $modeText 20 70 580 18 8.25 ([System.Drawing.FontStyle]::Regular) $dim))

$form.Controls.Add((New-Label 'Куда установить:' 20 100 200 20 9.75 ([System.Drawing.FontStyle]::Bold) ([System.Drawing.Color]::Black)))

$txt = New-Object System.Windows.Forms.TextBox
$txt.Location = New-Object System.Drawing.Point(20, 122)
$txt.Size = New-Object System.Drawing.Size(450, 24)
$txt.Font = New-Object System.Drawing.Font('Segoe UI', 9.75)
$form.Controls.Add($txt)

$btnBrowse = New-Object System.Windows.Forms.Button
$btnBrowse.Text = 'Обзор…'
$btnBrowse.Location = New-Object System.Drawing.Point(480, 120)
$btnBrowse.Size = New-Object System.Drawing.Size(120, 28)
$form.Controls.Add($btnBrowse)

$lblFree = New-Object System.Windows.Forms.Label
$lblFree.Location = New-Object System.Drawing.Point(20, 150)
$lblFree.Size = New-Object System.Drawing.Size(580, 18)
$lblFree.Font = New-Object System.Drawing.Font('Segoe UI', 8.25)
$lblFree.ForeColor = $dim
$form.Controls.Add($lblFree)

$chkLnk = New-Object System.Windows.Forms.CheckBox
$chkLnk.Text = 'Ярлыки на рабочем столе и в меню «Пуск»'
$chkLnk.Location = New-Object System.Drawing.Point(20, 174)
$chkLnk.Size = New-Object System.Drawing.Size(420, 22)
$chkLnk.Checked = $true
$form.Controls.Add($chkLnk)

$chkRun = New-Object System.Windows.Forms.CheckBox
$chkRun.Text = 'Запустить игру сразу после установки'
$chkRun.Location = New-Object System.Drawing.Point(20, 198)
$chkRun.Size = New-Object System.Drawing.Size(420, 22)
$chkRun.Checked = $true
$form.Controls.Add($chkRun)

# --- Список этапов (появляется после нажатия «Установить») ---
$stages = New-Object System.Windows.Forms.ListBox
$stages.Location = New-Object System.Drawing.Point(20, 230)
$stages.Size = New-Object System.Drawing.Size(580, 150)
$stages.Font = New-Object System.Drawing.Font('Segoe UI', 10)
$stages.BorderStyle = 'None'
$stages.BackColor = [System.Drawing.Color]::FromArgb(245, 245, 245)
$stages.DrawMode = 'Normal'
$stages.Visible = $false
$form.Controls.Add($stages)

$progress = New-Object System.Windows.Forms.ProgressBar
$progress.Location = New-Object System.Drawing.Point(20, 388)
$progress.Size = New-Object System.Drawing.Size(580, 22)
$form.Controls.Add($progress)

$status = New-Object System.Windows.Forms.Label
$status.Location = New-Object System.Drawing.Point(20, 414)
$status.Size = New-Object System.Drawing.Size(580, 22)
$status.Font = New-Object System.Drawing.Font('Segoe UI', 8.75)
$form.Controls.Add($status)

$btnInstall = New-Object System.Windows.Forms.Button
$btnInstall.Text = 'УСТАНОВИТЬ'
$btnInstall.Location = New-Object System.Drawing.Point(360, 410)
$btnInstall.Size = New-Object System.Drawing.Size(240, 40)
$btnInstall.Font = New-Object System.Drawing.Font('Segoe UI', 11, [System.Drawing.FontStyle]::Bold)
$btnInstall.BackColor = $orange
$btnInstall.ForeColor = [System.Drawing.Color]::White
$btnInstall.FlatStyle = 'Flat'
$form.Controls.Add($btnInstall)

$btnCancel = New-Object System.Windows.Forms.Button
$btnCancel.Text = 'Отмена'
$btnCancel.Location = New-Object System.Drawing.Point(260, 416)
$btnCancel.Size = New-Object System.Drawing.Size(90, 30)
$form.Controls.Add($btnCancel)

# ================= ВСПОМОГАТЕЛЬНЫЕ =================

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

function Pump { [System.Windows.Forms.Application]::DoEvents() }

# Запуск скрытого процесса с ожиданием (наследует скрытую консоль установщика).
function Run-Hidden([string]$exe, [string]$arguments, [string]$outLog) {
    try {
        $p = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $script:dst -NoNewWindow -PassThru -RedirectStandardOutput $outLog -RedirectStandardError ($outLog + '.err')
    } catch {
        $p = Start-Process -FilePath $exe -ArgumentList $arguments -WorkingDirectory $script:dst -PassThru
    }
    while ($p -and -not $p.HasExited) { Pump; Start-Sleep -Milliseconds 400 }
    if ($p) { return $p.ExitCode }
    return -1
}

# Загрузка файла с показом размера в статусе.
function Download-File([string]$url, [string]$dest) {
    $wc = New-Object System.Net.WebClient
    [Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
    $task = $wc.DownloadFileTaskAsync($url, $dest)
    while (-not $task.IsCompleted) {
        if ($task.IsFaulted) { throw $task.Exception.GetBaseException() }
        $sz = 0
        try { $sz = (Get-Item -LiteralPath $dest -ErrorAction Stop).Length } catch {}
        $script:status.Text = ('Загрузка Unity Hub… {0:N1} МБ' -f ($sz / 1MB))
        Pump
        Start-Sleep -Milliseconds 200
    }
    if ($task.IsFaulted) { throw $task.Exception.GetBaseException() }
}

# Отметка этапа: 0 — ожидание, 1 — выполняется, 2 — готово, 3 — ошибка.
function Set-Stage([int]$i, [int]$state, [string]$suffix) {
    $names = $script:stageNames
    if ($i -ge $names.Count) { return }
    $mark = switch ($state) { 0 { '·' } 1 { '▶' } 2 { '✓' } 3 { '✗' } }
    $txt2 = "$mark  $($names[$i])$suffix"
    if ($i -lt $stages.Items.Count) { $stages.Items[$i] = $txt2 } else { [void]$stages.Items.Add($txt2) }
    $stages.SelectedIndex = [Math]::Min($i, $stages.Items.Count - 1)
    Pump
}

function Msg([string]$text, [string]$title, [System.Windows.Forms.MessageBoxButtons]$buttons, [System.Windows.Forms.MessageBoxIcon]$icon) {
    return [System.Windows.Forms.MessageBox]::Show($form, $text, $title, $buttons, $icon)
}

# ================= СОБЫТИЯ =================

$txt.Add_TextChanged({ $lblFree.Text = Get-FreeText $txt.Text })

$btnBrowse.Add_Click({
    $fbd = New-Object System.Windows.Forms.FolderBrowserDialog
    $fbd.Description = 'Выберите диск и папку для установки Dear Passengers'
    $fbd.ShowNewFolderButton = $true
    try { $fbd.SelectedPath = $txt.Text } catch {}
    if ($fbd.ShowDialog($form) -eq [System.Windows.Forms.DialogResult]::OK) { $txt.Text = $fbd.SelectedPath }
})

$btnCancel.Add_Click({ $form.Close() })

$btnInstall.Add_Click({
    try {
        $btnInstall.Enabled = $false
        $btnBrowse.Enabled = $false

        # --- Проверка пути ---
        $dst = $txt.Text.Trim()
        if ([string]::IsNullOrWhiteSpace($dst)) { throw 'Укажите папку для установки.' }
        $dst = [System.IO.Path]::GetFullPath($dst)
        $script:dst = $dst
        $root = [System.IO.Path]::GetPathRoot($dst)
        if ($dst.TrimEnd('\') -eq $root.TrimEnd('\')) { throw 'Нельзя устанавливать в корень диска — выберите или создайте папку (например, C:\Games\Dear Passengers).' }
        if ($dst.StartsWith($script:src, [System.StringComparison]::OrdinalIgnoreCase)) { throw 'Выберите папку вне временной директории установщика.' }
        $drive = New-Object System.IO.DriveInfo($root)
        if (-not $drive.IsReady) { throw "Диск $root недоступен." }
        $needGb = if ($script:gameMode) { 1 } else { 3 }
        if ($drive.TotalFreeSpace -lt $needGb * 1GB) {
            throw ('Недостаточно места на {0}: свободно {1:N1} ГБ, нужно минимум {2} ГБ.' -f $root, ($drive.TotalFreeSpace / 1GB), $needGb)
        }
        New-Item -ItemType Directory -Path $dst -Force | Out-Null
        $probe = Join-Path $dst '.write_test'
        'ok' | Out-File -FilePath $probe -Encoding ascii
        Remove-Item $probe -Force

        # --- Переход в режим прогресса ---
        $txt.Visible = $false; $btnBrowse.Visible = $false; $lblFree.Visible = $false
        $chkLnk.Visible = $false; $chkRun.Visible = $false
        $stages.Visible = $true
        $stages.Items.Clear()

        if ($script:gameMode) {
            $script:stageNames = @('Копирование файлов игры', 'Ярлыки и запуск')
        } else {
            $script:stageNames = @(
                'Копирование файлов',
                'Поиск Unity Hub',
                'Загрузка Unity Hub (если его нет)',
                'Установка Unity 2022.3.62f1 (~4 ГБ)',
                'Вход в Unity ID (единственный ручной шаг)',
                'Сборка игры',
                'Ярлыки и запуск'
            )
        }
        foreach ($n in $script:stageNames) { [void]$stages.Items.Add("·  $n") }
        Pump

        $gameExe = if ($script:gameMode) { Join-Path $dst 'DearPassengers.exe' } else { Join-Path $dst 'builds\DearPassengers\DearPassengers.exe' }

        # ---------- Этап 1: копирование ----------
        Set-Stage 0 1 ''
        $items = @(Get-ChildItem -LiteralPath $script:src -Force)
        $progress.Maximum = $items.Count
        $progress.Value = 0
        foreach ($it in $items) {
            $script:status.Text = 'Копирую: ' + $it.Name
            Pump
            Copy-Item -LiteralPath $it.FullName -Destination $dst -Recurse -Force
            $progress.Value++
        }
        Set-Stage 0 2 ''
        $progress.Value = 0

        if ($script:gameMode) {
            # ---------- Игра готова: ярлыки и запуск ----------
            Set-Stage 1 1 ''
            if ($chkLnk.Checked -and (Test-Path -LiteralPath $gameExe)) { Make-Shortcuts $gameExe }
            Set-Stage 1 2 ''
            $progress.Value = $progress.Maximum
            if ($chkRun.Checked -and (Test-Path -LiteralPath $gameExe)) {
                Start-Process -FilePath $gameExe -WorkingDirectory (Split-Path $gameExe)
            }
            $script:status.Text = 'Установка завершена!'
            $r = Msg "Готово! Игра установлена в:`n$dst`n`nОткрыть папку установки?" 'Dear Passengers' 'YesNo' 'Information'
            if ($r -eq [System.Windows.Forms.DialogResult]::Yes) { Start-Process explorer.exe $dst }
            $form.Close()
            return
        }

        # ---------- Этап 2: поиск Unity Hub ----------
        Set-Stage 1 1 ''
        $script:status.Text = 'Ищу Unity Hub по всему компьютеру…'
        Pump
        $hub = Find-Hub
        Set-Stage 1 2 ''
        if ($hub) { Set-Stage 2 2 ' (уже установлен)' }

        # ---------- Этап 3: загрузка Unity Hub ----------
        if (-not $hub) {
            Set-Stage 2 1 ''
            $script:status.Text = 'Устанавливаю Unity Hub через winget…'
            Pump
            try { [void](Run-Hidden 'winget.exe' 'install --id Unity.UnityHub -e --force --accept-source-agreements --accept-package-agreements' (Join-Path $dst 'winget.log')) } catch {}
            $hub = Find-Hub
            if (-not $hub) {
                $script:status.Text = 'Загружаю Unity Hub с unity.com…'
                Pump
                $setup = Join-Path $env:TEMP 'UnityHubSetup.exe'
                Download-File 'https://public-cdn.cloud.unity3d.com/hub/prod/UnityHubSetup.exe' $setup
                $script:status.Text = 'Устанавливаю Unity Hub (тихо)…'
                Pump
                try { Start-Process -FilePath $setup -ArgumentList '/S' -Wait } catch {}
                $hub = Find-Hub
            }
            if (-not $hub) {
                Set-Stage 2 3 ''
                Msg 'Не удалось найти или установить Unity Hub.`nУстановите его вручную: https://unity.com/download`nи запустите установщик снова.' 'Dear Passengers' 'OK' 'Warning' | Out-Null
                throw 'Unity Hub недоступен.'
            }
            Set-Stage 2 2 ''
        }
        $script:status.Text = 'Unity Hub: ' + $hub
        Pump

        # ---------- Этап 4: установка редактора Unity ----------
        Set-Stage 3 1 ''
        $editor = Find-Editor
        if (-not $editor -and -not (Test-Path -LiteralPath $gameExe)) {
            $script:status.Text = 'Устанавливаю Unity 2022.3.62f1 — это долго (10–30 минут)…'
            Pump
            $progress.Style = 'Marquee'
            try { Start-Process -FilePath $hub -ArgumentList ('--', '--headless', 'install', '--version', $script:unityVer) -Wait } catch {}
            $progress.Style = 'Continuous'
            $editor = Find-Editor
            if (-not $editor) {
                # Запасной путь: открыть страницу редактора в Hub.
                $r = Msg 'Тихая установка не завершилась.`nСейчас откроется Unity Hub на странице редактора 2022.3.62f1 — нажмите там «Install» и дождитесь окончания.`nКогда редактор установится, нажмите «ОК» в этом окне.' 'Установка Unity' 'OKCancel' 'Information'
                if ($r -eq [System.Windows.Forms.DialogResult]::OK) {
                    Start-Process ('unityhub://' + $script:unityVer + '/' + $script:unityRev)
                    $editor = Find-Editor
                }
            }
        }
        if (-not $editor -and -not (Test-Path -LiteralPath $gameExe)) {
            Set-Stage 3 3 ''
            throw 'Редактор Unity 2022.3.62f1 не установлен (см. инструкции выше).'
        }
        if ($editor) { Set-Stage 3 2 ''; $script:status.Text = 'Unity: ' + $editor; Pump }
        elseif (Test-Path -LiteralPath $gameExe) { Set-Stage 3 2 ' (не требуется)' }

        # ---------- Этап 5-6: вход в Unity ID + сборка ----------
        if (-not (Test-Path -LiteralPath $gameExe)) {
            Set-Stage 4 1 ''
            Set-Stage 5 1 ''
            $script:status.Text = 'Собираю игру (5–15 минут)…'
            Pump
            $progress.Style = 'Marquee'
            $buildLog = Join-Path $dst 'build.log'
            [void](Run-Hidden $editor ('-batchmode -quit -projectPath "{0}" -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "{1}"' -f $dst, $buildLog) $buildLog)
            $progress.Style = 'Continuous'
            if (-not (Test-Path -LiteralPath $gameExe)) {
                # Нужен вход в Unity ID (лицензия Personal бесплатна).
                Set-Stage 4 1 ''
                $r = Msg 'Unity просит войти в аккаунт (Unity ID — бесплатно).`nСейчас откроется Unity Hub:`n  1. Войдите или создайте аккаунт.`n  2. Если спросит лицензию — выберите Personal.`n  3. Вернитесь и нажмите «ОК» здесь.' 'Вход в Unity ID' 'OKCancel' 'Information'
                if ($r -ne [System.Windows.Forms.DialogResult]::OK) { throw 'Вход в Unity ID отменён.' }
                try { Start-Process -FilePath $hub } catch {}
                $script:status.Text = 'Ожидаю вход в Unity ID… нажмите ОК, когда войдёте.'
                Pump
                Msg 'Вошли? Нажмите «ОК» — продолжаю сборку.' 'Unity ID' 'OK' 'Question' | Out-Null
                $script:status.Text = 'Повторяю сборку…'
                Pump
                $progress.Style = 'Marquee'
                [void](Run-Hidden $editor ('-batchmode -quit -projectPath "{0}" -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "{1}"' -f $dst, $buildLog) $buildLog)
                $progress.Style = 'Continuous'
                if (-not (Test-Path -LiteralPath $gameExe)) {
                    Set-Stage 5 3 ''
                    throw "Сборка не удалась. Подробности — в build.log в папке установки:`n$buildLog"
                }
            }
            Set-Stage 4 2 ''
            Set-Stage 5 2 ''
        } else {
            Set-Stage 4 2 ' (игра уже собрана)'
            Set-Stage 5 2 ' (игра уже собрана)'
        }

        # ---------- Этап 7: ярлыки и запуск ----------
        Set-Stage 6 1 ''
        if ($chkLnk.Checked -and (Test-Path -LiteralPath $gameExe)) { Make-Shortcuts $gameExe }
        Set-Stage 6 2 ''
        $progress.Value = $progress.Maximum
        $script:status.Text = 'Установка завершена!'
        if ($chkRun.Checked -and (Test-Path -LiteralPath $gameExe)) {
            Start-Process -FilePath $gameExe -WorkingDirectory (Split-Path $gameExe)
        }
        $r = Msg "Готово! Игра установлена в:`n$dst`n`nОткрыть папку установки?" 'Dear Passengers — установка завершена' 'YesNo' 'Information'
        if ($r -eq [System.Windows.Forms.DialogResult]::Yes) { Start-Process explorer.exe $dst }
        $form.Close()
    }
    catch {
        $script:status.Text = 'Ошибка: ' + $_.Exception.Message
        Msg $_.Exception.Message 'Ошибка установки' 'OK' 'Error' | Out-Null
        $btnInstall.Enabled = $true
        $btnBrowse.Enabled = $true
    }
})

# ================= Диск по умолчанию =================
$best = $null
foreach ($d in [System.IO.DriveInfo]::GetDrives()) {
    if ($d.DriveType -eq 'Fixed' -and $d.IsReady) {
        if ($null -eq $best -or $d.TotalFreeSpace -gt $best.TotalFreeSpace) { $best = $d }
    }
}
$txt.Text = if ($best) { Join-Path $best.RootDirectory 'Games\Dear Passengers' } else { 'C:\Games\Dear Passengers' }
$lblFree.Text = Get-FreeText $txt.Text
$form.Add_Shown({ $form.Activate() })
[void]$form.ShowDialog()
