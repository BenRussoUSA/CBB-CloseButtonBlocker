# Build CBB for Windows

Requirements: Windows 11 x64, .NET SDK 10.0.403 (or a later patch in that feature
band), and WiX CLI 4.0.6. Build tools are not required by end users.
WiX 4.0.6 is pinned to reproduce this beta's installer. Its source is licensed
under the [Microsoft Reciprocal License](https://github.com/wixtoolset/wix/blob/v4.0.6/LICENSE.TXT).
WiX is a build tool; end users run the Windows Installer supplied by Windows.

From the repository root in PowerShell:

```powershell
dotnet tool install wix --version 4.0.6 --tool-path .tools/wix
./build.ps1
```

The script uses `.tools/dotnet/dotnet.exe` when present, otherwise `dotnet` from
PATH. It builds a self-contained x64 app and runs the tests, then makes an MSI
and a ZIP under `artifacts/release`. The tests create temporary fixture windows
and snapshots. Tests only change fixture windows, not the user's other windows.

The installed `src` folder is a copy of this source tree without Git history,
build outputs, or local tools. From that installed source folder, run the same
commands to rebuild. NuGet supplies .NET runtime packs. Required notices are
copied from the installed SDK and included in the release.

For a later release, increase the three-part MSI version and generate a new
ProductCode. Keep the UpgradeCode unchanged. Update AppInfo.Version and the
project's versions together. Do not ship a different MSI under an unchanged
product/version. Tag each source release to match its MSI.

Settings are external to MSI-managed files. Major upgrades stop CBB, replace
application files, and retain settings and the startup registry value. The
normal uninstall action additionally removes startup registration. An MSI
failure must be investigated using the verbose log, not assumed successful.

Before release, run the MSI upgrade test in a disposable Windows profile/VM.
The local unit/native-window harness alone does not certify MSI upgrade behavior.
