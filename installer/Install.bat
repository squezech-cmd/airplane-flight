@echo off
rem Install.bat - turnkey setup: finds/installs Unity, builds the game, makes shortcuts, launches.
rem Launched automatically by the self-extracting installer from the install folder.
rem IMPORTANT: keep this file pure ASCII - cmd.exe breaks on non-ASCII batch files.
title Dear Passengers - Setup
setlocal EnableDelayedExpansion
cd /d "%~dp0"

set "UNITY_VER=2022.3.62f1"
set "UNITY_REV=4af31df58517"
set "GAMEEXE=%~dp0builds\DearPassengers\DearPassengers.exe"

echo ============================================================
echo    DEAR PASSENGERS - SETUP (18+)
echo    Made for entertainment. Do not repeat in real life.
echo ============================================================
echo Install folder: %~dp0
echo.

if exist "!GAMEEXE!" goto play

rem ---------- 1/4 locate Unity Hub (Program Files or per-user LocalAppData) ----------
set "HUBEXE="
if exist "%ProgramFiles%\Unity Hub\Unity Hub.exe" set "HUBEXE=%ProgramFiles%\Unity Hub\Unity Hub.exe"
if not defined HUBEXE if exist "%LocalAppData%\Programs\Unity Hub\Unity Hub.exe" set "HUBEXE=%LocalAppData%\Programs\Unity Hub\Unity Hub.exe"

if defined HUBEXE goto hub_ok
echo [1/4] Unity Hub not found - installing via winget...
winget install --id Unity.UnityHub -e --accept-source-agreements --accept-package-agreements
if exist "%LocalAppData%\Programs\Unity Hub\Unity Hub.exe" set "HUBEXE=%LocalAppData%\Programs\Unity Hub\Unity Hub.exe"
if not defined HUBEXE if exist "%ProgramFiles%\Unity Hub\Unity Hub.exe" set "HUBEXE=%ProgramFiles%\Unity Hub\Unity Hub.exe"

:hub_ok
if defined HUBEXE goto hub_found
echo.
echo Unity Hub was not found and could not be installed automatically.
echo Download it manually: https://unity.com/download
echo Install it, then run this setup again.
pause
exit /b 1

:hub_found
echo [1/4] Unity Hub found: "!HUBEXE!"

rem ---------- 2/4 locate or install the Unity editor ----------
set "UNITYEXE="
call :find_unity
if defined UNITYEXE goto editor_ok

echo [2/4] Installing Unity %UNITY_VER% - about 4 GB, 10-30 minutes...
"%HUBEXE%" -- --headless install --version %UNITY_VER%
call :find_unity
if defined UNITYEXE goto editor_ok

echo.
echo Silent install did not finish. Opening Unity Hub at the editor page.
echo Click Install for %UNITY_VER% there, wait until it finishes,
echo then come back to this window and press any key.
start "" "unityhub://!UNITY_VER!/!UNITY_REV!"
pause >nul
call :find_unity
if defined UNITYEXE goto editor_ok

echo.
echo Unity editor is still not found.
echo Open Unity Hub - Installs - Install Editor - %UNITY_VER%,
echo finish the install, then run this setup again.
pause
exit /b 1

:editor_ok
echo [2/4] Unity editor: "!UNITYEXE!"

rem ---------- 3/4 build the game ----------
echo [3/4] Building the game - 5-15 minutes, please wait...
"!UNITYEXE!" -batchmode -quit -projectPath "%~dp0." -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "%~dp0build.log"
if exist "!GAMEEXE!" goto shortcuts

echo.
echo Unity needs a license sign-in. Unity ID is free.
echo Unity Hub is opening now:
echo   1. Sign in or create an account.
echo   2. If asked - Preferences, Licenses, activate the free Personal license.
echo   3. Come back to this window and press any key.
start "" "!HUBEXE!"
pause >nul
echo Retrying the build...
"!UNITYEXE!" -batchmode -quit -projectPath "%~dp0." -executeMethod DearPassengers.EditorTools.BuildScript.BuildWindows -logFile "%~dp0build.log"
if not exist "!GAMEEXE!" (
  echo.
  echo Build failed. Details are in build.log next to this file.
  pause
  exit /b 1
)

:shortcuts
rem ---------- 4/4 shortcuts - skipped if SetupUI passed "noshortcuts" ----------
if /i "%~1"=="noshortcuts" goto play
echo [4/4] Creating shortcuts...
powershell -NoProfile -Command "$ws=New-Object -ComObject WScript.Shell; $dirs=@([Environment]::GetFolderPath('Desktop'),(Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs')); foreach($d in $dirs){ $s=$ws.CreateShortcut((Join-Path $d 'Dear Passengers.lnk')); $s.TargetPath='%GAMEEXE%'; $s.WorkingDirectory='%~dp0builds\DearPassengers'; $s.IconLocation='%GAMEEXE%,0'; $s.Save() }"

:play
echo.
echo Done! Starting Dear Passengers...
start "" "!GAMEEXE!"
echo Shortcut "Dear Passengers" is on your Desktop and in the Start Menu.
timeout /t 5 >nul
endlocal
exit /b 0

rem ================= subroutine: find the Unity editor =================
:find_unity
set "UNITYEXE="
rem 1) default machine-wide location
if exist "%ProgramFiles%\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe" set "UNITYEXE=%ProgramFiles%\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe" & goto :eof
rem 2) default per-user location
if exist "%LocalAppData%\Programs\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe" set "UNITYEXE=%LocalAppData%\Programs\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe" & goto :eof
rem 3) Unity Hub own registry of installed editors - covers custom install roots
for /f "usebackq delims=" %%i in (`powershell -NoProfile -Command "try{$p=$env:APPDATA+'\UnityHub\editors.json';if(Test-Path -LiteralPath $p){(Get-Content -LiteralPath $p -Raw|ConvertFrom-Json).PSObject.Properties.Value|Where-Object{$_.version -like '%UNITY_VER%*'}|ForEach-Object{Join-Path $_.location 'Editor\Unity.exe'}}}catch{}"`) do if not defined UNITYEXE set "UNITYEXE=%%i"
if defined UNITYEXE if exist "!UNITYEXE!" goto :eof
set "UNITYEXE="
rem 4) common custom roots on any drive
for %%D in (C D E F G H I J) do if not defined UNITYEXE if exist "%%D:\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe" set "UNITYEXE=%%D:\Unity\Hub\Editor\%UNITY_VER%\Editor\Unity.exe"
goto :eof
