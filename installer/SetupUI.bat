@echo off
rem SetupUI.bat — запускает графический установщик (выбор диска/папки, прогресс, ярлыки).
rem Вызывается self-extracting установщиком после распаковки.
start "" /wait powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SetupUI.ps1"
