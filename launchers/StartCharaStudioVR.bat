@echo off
setlocal
cd /d "%~dp0"

rem Select OpenVR during Unity bootstrap so ReShade can hook the compositor
rem before D3D11 is created. --studiovr then enables the managed VR features.
rem Keep this launcher deterministic so native and managed modes cannot split.
start "" "%~dp0CharaStudio.exe" --studiovr -vrmode OpenVR
