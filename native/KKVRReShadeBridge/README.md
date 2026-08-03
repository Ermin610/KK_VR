# KK VR ReShade Bridge

This optional x64 ReShade add-on is the native half of the wrist-menu ReShade controls.
It uses the official ReShade 6.7.1 Add-on API (API version 18) and applies managed
requests from each effect runtime's own `reshade_present` callback. Desktop and
OpenVR runtimes are both controlled and reported, so the same public API can be
tested without wearing a headset and then reused unchanged in VR.

The repository's main project and `build-release.ps1` build and stage this add-on automatically. Release builds require Visual Studio 2022 with the **Desktop development with C++** workload and a Windows SDK.

To build only the native project:

```powershell
& "D:\VS\APP\MSBuild\Current\Bin\MSBuild.exe" `
  .\KKVRReShadeBridge.vcxproj /m /p:Configuration=Release /p:Platform=x64
```

Copy `bin\x64\Release\KKVRReShadeBridge.addon64` next to `CharaStudio.exe`, then
restart CharaStudio. ReShade only discovers `.addon64` modules while it initializes.

The vendored API headers are from crosire/ReShade tag `v6.7.1`, commit
`8cf85bd12d56697d756d4fcb45e501f5d1b540fa`, under the included BSD-3-Clause license.
