# Mirror regression checks

Run with .NET 9 SDK from the repository root:

```sh
dotnet run --project tests/MirrorRegression/MirrorRegression.csproj -c Release
```

This executable compiles the **production** Manager.cs and VRReflection.cs against
small managed Unity/SteamVR doubles. It verifies exception recovery, subsequent
mirror rendering, runtime readiness, multi-material conversion, transactional
rollback, shared-material preservation, and owned-material cleanup. Failed
assertions terminate with a nonzero exit code. No third-party test packages or
game files are required.

The doubles do not implement projection geometry, Unity native object lifetime,
shader execution or GPU behavior. These checks do **not** replace engine/headset
tests of stereo alignment, clipping, scene reload and frame time. The embedded
shader bundle is passed through a test loader, not executed.
