@echo off
rem SetupUI.bat - launches the graphical Steam-style installer.
rem Direct call (no "start"): the child inherits this hidden console, so no
rem extra console window flashes. Keep this file pure ASCII.
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SetupUI.ps1"
