# Build CBB for Windows

Build on Windows 11 x64 using .NET SDK 10.0.403 or a later patch in that feature
band. The SDK compiles modern C# syntax, but the application targets .NET
Framework 4.8.1. End users do not need the SDK or a separate modern .NET runtime.
NuGet downloads the Framework reference assemblies for compilation only.

Install the pinned WiX 4.0.6 build tool and its Framework detection extension:

```powershell
dotnet tool install wix --version 4.0.6 --tool-path .tools/wix
./.tools/wix/wix.exe extension add WixToolset.Netfx.wixext/4.0.6
./build.ps1
```

The script uses `.tools/dotnet/dotnet.exe` when present, otherwise `dotnet` from
PATH. It publishes the Framework EXE and its configuration, runs the tests, then
creates an MSI and ZIP in `artifacts/release`. The first build needs internet
access to restore build dependencies. Installing the finished MSI does not.

The app has no third-party runtime package dependencies. Framework JSON support
replaces System.Text.Json, so publishing does not copy modern .NET assemblies.
The MSI uses WiX's native Framework detection action to reject a missing
Framework 4.8.1 installation with an explanation. WiX is a build tool; end users
use Windows Installer, which is supplied by Windows.

The installed `src` folder contains the application source and test harness,
including the installer definitions and these build instructions. Local tools
and build outputs are excluded. To rebuild from that folder, use the commands
above. WiX's license is included at `installer/licenses/WiX-LICENSE.txt`; its
source is available from the [WiX v4.0.6 tag](https://github.com/wixtoolset/wix/tree/v4.0.6).

The tests create temporary fixture windows and render synthetic UI screenshots.
They never change the Close commands of the user's other windows. Event tests
pump the Windows message loop and verify detection without calling a refresh.
Settings migration tests load the JSON shape written by 0.9.0beta.

For a release, update AppInfo.Version, the project versions, and the manifest.
Increase the three-part MSI ProductVersion and assign a new ProductCode in
build.ps1. Keep the UpgradeCode unchanged. Do not distribute different packages
under an existing published version. Tag the source that produced each release.

Settings are outside the MSI-managed application folder. Major upgrades stop
CBB and replace program files while retaining saved settings and the startup
registry value. Upgrading from 0.9.0beta also removes its bundled runtime files.
A normal uninstall removes startup registration and keeps rules for reinstalling.

Before publication, test the MSI in a disposable Windows profile or VM using
`installer/Test-InstallUpgrade.ps1`. That script requires a clean CBB profile;
it refuses to alter an existing installation. To validate an in-place upgrade
without deleting current settings, use `installer/Upgrade-Current.ps1`. Inspect
its parameters and the MSI log if any check fails.

Unsigned builds are the default. Optional Microsoft Artifact Signing support
signs the EXE before packaging and signs the MSI before checksums are generated.
See [signing setup](SIGNING.md) for its identity requirements and costs.
