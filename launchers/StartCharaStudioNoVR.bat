@echo off
setlocal
cd /d "%~dp0"

rem This package enables OpenVR in globalgamemanagers for the VR/ReShade path.
rem Unity must receive -vrmode None before BepInEx starts; --novr is the
rem matching managed-plugin safety gate. Keep this launcher deterministic:
rem reserved VR arguments are intentionally not accepted from the command line.
start "" "%~dp0CharaStudio.exe" --novr -vrmode None
