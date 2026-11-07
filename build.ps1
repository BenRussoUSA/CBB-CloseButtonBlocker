param(
    [switch]$SkipTests,
    [string]$ProductVersion = '0.9.4',
    [string]$ProductCode = '{73F36BA7-B95D-4A8D-9B4E-24821687E7BA}',
    [string]$ReleaseName = '0.9.4beta',
    [string]$SigningMetadataPath,
    [string]$SignToolPath,
    [string]$SigningDlibPath
)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_CLI_HOME = Join-Path $PSScriptRoot '.tools/cli-home'
$env:NUGET_PACKAGES = Join-Path $PSScriptRoot '.tools/packages'
$dotnet = if (Test-Path '.tools/dotnet/dotnet.exe') { (Resolve-Path '.tools/dotnet/dotnet.exe').Path } else { (Get-Command dotnet -ErrorAction Stop).Source }
$env:DOTNET_ROOT = Split-Path $dotnet
$wix = (Resolve-Path '.tools/wix/wix.exe' -ErrorAction Stop).Path
function Run-Dotnet([string[]]$Arguments) { & $dotnet @Arguments; if ($LASTEXITCODE -ne 0) { throw "dotnet failed with exit code $LASTEXITCODE" } }
$buildId = Get-Date -Format 'yyyyMMdd-HHmmss'
$work = Join-Path $PSScriptRoot "artifacts/build-$buildId"
$payload = Join-Path $work 'payload'
$release = Join-Path $PSScriptRoot 'artifacts/release'
New-Item -ItemType Directory -Force $payload, $release | Out-Null
Run-Dotnet @('restore', 'src/CBB/CBB.csproj', '--configfile', 'NuGet.Config')
Run-Dotnet @('publish', 'src/CBB/CBB.csproj', '-c', 'Release', '--no-restore', '-o', $payload, '-p:DebugType=None', '-p:DebugSymbols=false')
if (Get-ChildItem -LiteralPath $payload -File | Where-Object Extension -eq '.dll') { throw 'Unexpected DLL in Framework-only payload.' }
if ($SigningMetadataPath) {
    & ./installer/Sign-Artifacts.ps1 -Files (Join-Path $payload 'CBB-CloseButtonBlocker.exe') -MetadataPath $SigningMetadataPath -SignToolPath $SignToolPath -DlibPath $SigningDlibPath
}
if (-not $SkipTests) {
    Run-Dotnet @('restore', 'tests/CBB.Tests/CBB.Tests.csproj', '--configfile', 'NuGet.Config')
    Run-Dotnet @('build', 'tests/CBB.Tests/CBB.Tests.csproj', '-c', 'Release', '--no-restore')
    & './tests/CBB.Tests/bin/Release/net481/CBB.Tests.exe' 'artifacts/test-results-0.9.4' | Tee-Object -FilePath 'artifacts/test-results.txt'
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
Copy-Item 'src/CBB/app.ico' $payload
foreach ($file in @('LICENSE.txt', 'COPYRIGHT.txt', 'README.txt')) { Copy-Item -LiteralPath $file -Destination $payload }
$source = Join-Path $payload 'src'
New-Item -ItemType Directory -Force $source | Out-Null
foreach ($name in @('src', 'tests', 'installer', 'docs', '.github')) {
    Get-ChildItem -LiteralPath $name -Recurse -File | Where-Object {
        $segments = $_.FullName.Split([IO.Path]::DirectorySeparatorChar)
        ($segments -notcontains 'bin') -and ($segments -notcontains 'obj') -and
        ($_.Name -notlike '*.local.json') -and ($_.Name -notlike '.env*') -and
        ($_.Name -notlike 'credentials*.json') -and ($_.Name -notlike 'secrets*.json') -and
        ($_.Extension -notin @('.pfx', '.p12', '.pem', '.key'))
    } | ForEach-Object {
        $relative = $_.FullName.Substring($PSScriptRoot.Length + 1)
        $target = Join-Path $source $relative
        New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $target
    }
}
foreach ($file in @('build.ps1', 'global.json', 'NuGet.Config', 'LICENSE.txt', 'COPYRIGHT.txt', 'README.txt', 'README.md', 'CONTRIBUTING.md', '.gitignore')) { Copy-Item -LiteralPath $file -Destination $source }
if (Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object Extension -in @('.dll', '.exe', '.pdb', '.msi', '.zip')) { throw 'Compiled outputs were unexpectedly included in the source payload.' }
@'
CBB-CloseButtonBlocker uses Windows' .NET Framework 4.8.1 and Windows Forms.
No .NET runtime, SDK, or reference assemblies are redistributed in this package.
CBB source itself is GPL-3.0-only; see LICENSE.txt and COPYRIGHT.txt.
The installer was built with WiX Toolset 4.0.6 (Microsoft Reciprocal License).
The MSI includes WiX's native Framework detection custom action.
WiX license and source: https://github.com/wixtoolset/wix/tree/v4.0.6
The Windows Installer runtime is supplied by Windows, not by this package.
'@ | Set-Content -LiteralPath (Join-Path $payload 'THIRD-PARTY-NOTICES.txt') -Encoding UTF8
$payloadWxs = Join-Path $work 'Payload.wxs'
& ./installer/Generate-Payload.ps1 -PayloadDir $payload -OutputPath $payloadWxs
$msi = Join-Path $release "CBB-CloseButtonBlocker-$ReleaseName-win-x64.msi"
& $wix build installer/Package.wxs $payloadWxs -ext WixToolset.Netfx.wixext/4.0.6 -arch x64 -d "PayloadDir=$payload" -d "ProductVersion=$ProductVersion" -d "ProductCode=$ProductCode" -o $msi
if ($LASTEXITCODE -ne 0) { throw 'MSI build failed.' }
if ($SigningMetadataPath) {
    & ./installer/Sign-Artifacts.ps1 -Files $msi -MetadataPath $SigningMetadataPath -SignToolPath $SignToolPath -DlibPath $SigningDlibPath
}
$zip = Join-Path $release "CBB-CloseButtonBlocker-$ReleaseName-win-x64.zip"
Compress-Archive -Path "$payload/*" -DestinationPath $zip -Force
$hashes = foreach ($file in @($msi, $zip)) { $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $file; "$($hash.Hash.ToLowerInvariant())  $(Split-Path $file -Leaf)" }
$hashes | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding ASCII
Write-Output "Release artifacts: $release"
