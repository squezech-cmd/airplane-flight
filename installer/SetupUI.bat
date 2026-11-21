@echo off
rem SetupUI.bat - launches the graphical installer (disk/folder selection, progress, shortcuts).
rem Launched by the self-extracting installer right after extraction.
rem IMPORTANT: keep this file pure ASCII - cmd.exe breaks on non-ASCII batch files.
start "" /wait powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0SetupUI.ps1"
