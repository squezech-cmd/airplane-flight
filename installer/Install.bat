@echo off
rem Install.bat — «под ключ»: проверяет/ставит Unity, собирает игру, делает ярлыки и запускает.
rem Запускается автоматически self-extracting установщиком из папки установки.
chcp 65001 >nul
title Dear Passengers — установка
setlocal
cd /d "%~dp0"

set "UNITY_VER=2022.3.62f1"
set "HUBEXE=%ProgramFiles%\Unity Hub\Unity Hub.exe"
set "UNITYEXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe"
set "GAMEEXE=%~dp0builds\DearPassengers\DearPassengers.exe"

echo ============================================================
echo    DEAR PASSENGERS — установка под ключ (18+)
echo    Создана в развлекательных целях. Не повторяйте в жизни.
echo ============================================================
echo.

if exist "%GAMEEXE%" goto play

rem ---------- 1/4 Unity Hub ----------
if exist "%HUBEXE%" goto editor
echo [1/4] Unity Hub не найден — устанавливаю через winget...
winget install --id Unity.UnityHub -e --accept-source-agreements --accept-package-agreements
if not exist "%HUBEXE%" (
  echo.
  echo Не удалось автоматически. Скачайте Hub: https://unity.com/download
  echo Установите его и запустите этот установщик снова.
  pause
  exit /b 1
)

:editor
rem ---------- 2/4 Unity Editor ----------
if exist "%UNITYEXE%" goto build
echo [2/4] Устанавливаю Unity %UNITY_VER% (~4 ГБ; 10-30 минут)...
"%HUBEXE%" -- --headless install --version %UNITY_VER%
if not exist "%UNITYEXE%" (
  echo.
  echo Открываю Unity Hub: вкладка Installs -^> Install Editor -^> %UNITY_VER%.
  echo После установки запустите этот установщик снова.
  start "" "%HUBEXE%"
  pause
  exit /b 1
)

:build
rem ---------- 3/4 Сборка игры ----------
echo [3/4] Собираю игру (первый раз 5-15 минут, дальше мгновенно)...
"%UNITYEXE%" -batchmode -quit -projectPath "%~dp0." -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "%~dp0build.log"
if exist "%GAMEEXE%" goto shortcuts
echo.
echo Похоже, Unity просит вход в Unity ID (Personal — бесплатно).
echo Открываю Unity Hub: войдите, подождите 10 секунд и нажмите здесь любую клавишу.
start "" "%HUBEXE%"
pause >nul
echo Повторяю сборку...
"%UNITYEXE%" -batchmode -quit -projectPath "%~dp0." -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "%~dp0build.log"
if not exist "%GAMEEXE%" (
  echo.
  echo Сборка не удалась. Подробности в build.log рядом с этим файлом.
  pause
  exit /b 1
)

:shortcuts
rem ---------- 4/4 Ярлыки ----------
echo [4/4] Создаю ярлыки...
powershell -NoProfile -Command "$ws=New-Object -ComObject WScript.Shell; $dirs=@([Environment]::GetFolderPath('Desktop'),(Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs')); foreach($d in $dirs){ $s=$ws.CreateShortcut((Join-Path $d 'Dear Passengers.lnk')); $s.TargetPath='%GAMEEXE%'; $s.WorkingDirectory='%~dp0builds\DearPassengers'; $s.IconLocation='%GAMEEXE%,0'; $s.Save() }"

:play
echo.
echo Готово! Запускаю Dear Passengers...
start "" "%GAMEEXE%"
echo Ярлык «Dear Passengers» — на рабочем столе и в меню «Пуск».
timeout /t 5 >nul
endlocal
