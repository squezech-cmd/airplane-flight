@echo off
rem Install.bat - turnkey setup: checks/installs Unity, builds the game, makes shortcuts, launches.
rem Launched automatically by the self-extracting installer from the install folder.
rem IMPORTANT: this file must stay pure ASCII (cmd.exe breaks on non-ASCII batch files).
title Dear Passengers - Setup
setlocal
cd /d "%~dp0"

set "UNITY_VER=2022.3.62f1"
set "HUBEXE=%ProgramFiles%\Unity Hub\Unity Hub.exe"
set "UNITYEXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe"
set "GAMEEXE=%~dp0builds\DearPassengers\DearPassengers.exe"

echo ============================================================
echo    DEAR PASSENGERS - SETUP (18+)
echo    Made for entertainment. Do not repeat in real life.
echo ============================================================
echo Install folder: %~dp0
echo.

if exist "%GAMEEXE%" goto play

rem ---------- 1/4 Unity Hub ----------
if exist "%HUBEXE%" goto editor
echo [1/4] Unity Hub not found - installing via winget...
winget install --id Unity.UnityHub -e --accept-source-agreements --accept-package-agreements
if not exist "%HUBEXE%" (
  echo.
  echo Automatic install failed.
  echo Download Unity Hub manually: https://unity.com/download
  echo Install it, then run this setup again.
  pause
  exit /b 1
)

:editor
rem ---------- 2/4 Unity Editor ----------
if exist "%UNITYEXE%" goto build
echo [2/4] Installing Unity %UNITY_VER% - about 4 GB, 10-30 minutes...
"%HUBEXE%" -- --headless install --version %UNITY_VER%
if not exist "%UNITYEXE%" (
  echo.
  echo Unity editor was not installed. Opening Unity Hub:
  echo go to Installs - Install Editor - %UNITY_VER%,
  echo then run this setup again.
  start "" "%HUBEXE%"
  pause
  exit /b 1
)

:build
rem ---------- 3/4 Build the game ----------
echo [3/4] Building the game - 5-15 minutes, please wait...
"%UNITYEXE%" -batchmode -quit -projectPath "%~dp0." -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "%~dp0build.log"
if exist "%GAMEEXE%" goto shortcuts
echo.
echo Unity needs a license sign-in (Unity ID is free).
echo Unity Hub is opening now - sign in or create an account,
echo then come back to this window and press any key.
start "" "%HUBEXE%"
pause >nul
echo Retrying the build...
"%UNITYEXE%" -batchmode -quit -projectPath "%~dp0." -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "%~dp0build.log"
if not exist "%GAMEEXE%" (
  echo.
  echo Build failed. Details are in build.log next to this file.
  pause
  exit /b 1
)

:shortcuts
rem ---------- 4/4 Shortcuts (skipped if SetupUI passed "noshortcuts") ----------
if /i "%~1"=="noshortcuts" goto play
echo [4/4] Creating shortcuts...
powershell -NoProfile -Command "$ws=New-Object -ComObject WScript.Shell; $dirs=@([Environment]::GetFolderPath('Desktop'),(Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs')); foreach($d in $dirs){ $s=$ws.CreateShortcut((Join-Path $d 'Dear Passengers.lnk')); $s.TargetPath='%GAMEEXE%'; $s.WorkingDirectory='%~dp0builds\DearPassengers'; $s.IconLocation='%GAMEEXE%,0'; $s.Save() }"

:play
echo.
echo Done! Starting Dear Passengers...
start "" "%GAMEEXE%"
echo Shortcut "Dear Passengers" is on your Desktop and in the Start Menu.
timeout /t 5 >nul
endlocal
