@echo off
rem GameSetup.bat — запускается self-extracting установщиком СОБРАННОЙ игры:
rem создаёт ярлыки и стартует DearPassengers.exe из папки распаковки.
chcp 65001 >nul
title Dear Passengers
setlocal
cd /d "%~dp0"

if not exist "%~dp0DearPassengers.exe" (
  echo Не найден DearPassengers.exe в "%~dp0"
  pause
  exit /b 1
)

powershell -NoProfile -Command "$ws=New-Object -ComObject WScript.Shell; $dirs=@([Environment]::GetFolderPath('Desktop'),(Join-Path $env:APPDATA 'Microsoft\Windows\Start Menu\Programs')); foreach($d in $dirs){ $s=$ws.CreateShortcut((Join-Path $d 'Dear Passengers.lnk')); $s.TargetPath='%~dp0DearPassengers.exe'; $s.WorkingDirectory='%~dp0'; $s.IconLocation='%~dp0DearPassengers.exe,0'; $s.Save() }"

echo Запускаю Dear Passengers... (ярлык — на рабочем столе и в «Пуске»)
start "" "%~dp0DearPassengers.exe"
endlocal
