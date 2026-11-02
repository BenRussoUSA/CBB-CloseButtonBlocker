param(
    [switch]$SkipTests,
    [string]$ProductVersion = '0.8.0',
    [string]$ProductCode = '{9A3F9047-E185-460D-BFD4-078C800A5401}',
    [string]$ReleaseName = '0.8.0alpha'
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
Run-Dotnet @('restore', 'src/CBB/CBB.csproj', '-r', 'win-x64', '--configfile', 'NuGet.Config')
Run-Dotnet @('publish', 'src/CBB/CBB.csproj', '-c', 'Release', '-r', 'win-x64', '--self-contained', 'true', '--no-restore', '-o', $payload, '-p:DebugType=None', '-p:DebugSymbols=false')
if (-not $SkipTests) {
    Run-Dotnet @('restore', 'tests/CBB.Tests/CBB.Tests.csproj', '--configfile', 'NuGet.Config')
    Run-Dotnet @('build', 'tests/CBB.Tests/CBB.Tests.csproj', '-c', 'Release', '--no-restore')
    & $dotnet 'tests/CBB.Tests/bin/Release/net10.0-windows/CBB.Tests.dll' 'artifacts/test-results' | Tee-Object -FilePath 'artifacts/test-results.txt'
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
if (-not (Test-Path 'artifacts/test-results/app.ico')) { throw 'Run tests once to generate the application icon.' }
Copy-Item 'artifacts/test-results/app.ico' $payload
foreach ($file in @('LICENSE.txt', 'COPYRIGHT.txt', 'README.txt')) { Copy-Item -LiteralPath $file -Destination $payload }
$source = Join-Path $payload 'src'
New-Item -ItemType Directory -Force $source | Out-Null
foreach ($name in @('src', 'tests', 'installer', 'docs', '.github')) {
    Get-ChildItem -LiteralPath $name -Recurse -File | Where-Object {
        $segments = $_.FullName.Split([IO.Path]::DirectorySeparatorChar)
        ($segments -notcontains 'bin') -and ($segments -notcontains 'obj')
    } | ForEach-Object {
        $relative = $_.FullName.Substring($PSScriptRoot.Length + 1)
        $target = Join-Path $source $relative
        New-Item -ItemType Directory -Force (Split-Path $target) | Out-Null
        Copy-Item -LiteralPath $_.FullName -Destination $target
    }
}
foreach ($file in @('build.ps1', 'global.json', 'NuGet.Config', 'LICENSE.txt', 'COPYRIGHT.txt', 'README.txt', 'README.md', 'CONTRIBUTING.md', '.gitignore')) { Copy-Item -LiteralPath $file -Destination $source }
if (Get-ChildItem -LiteralPath $source -Recurse -File | Where-Object Extension -in @('.dll', '.exe', '.pdb', '.msi', '.zip')) { throw 'Compiled outputs were unexpectedly included in the source payload.' }
$licenses = Join-Path $payload 'licenses'
New-Item -ItemType Directory -Force $licenses | Out-Null
Copy-Item (Join-Path $env:DOTNET_ROOT 'LICENSE.txt') (Join-Path $licenses 'dotnet-LICENSE.txt')
Copy-Item (Join-Path $env:DOTNET_ROOT 'ThirdPartyNotices.txt') (Join-Path $licenses 'dotnet-ThirdPartyNotices.txt')
$desktop = Get-ChildItem (Join-Path $env:DOTNET_ROOT 'sdk') -Directory | Sort-Object Name -Descending | Select-Object -First 1
Copy-Item (Join-Path $desktop.FullName 'Sdks/Microsoft.NET.Sdk.WindowsDesktop/LICENSE.TXT') (Join-Path $licenses 'WindowsDesktop-LICENSE.txt')
Copy-Item (Join-Path $desktop.FullName 'Sdks/Microsoft.NET.Sdk.WindowsDesktop/THIRD-PARTY-NOTICES.TXT') (Join-Path $licenses 'WindowsDesktop-THIRD-PARTY-NOTICES.txt')
$runtimeConfig = Get-Content (Join-Path $payload 'CBB-CloseButtonBlocker.runtimeconfig.json') -Raw | ConvertFrom-Json
$coreVersion = ($runtimeConfig.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.NETCore.App').version
$desktopVersion = ($runtimeConfig.runtimeOptions.includedFrameworks | Where-Object name -eq 'Microsoft.WindowsDesktop.App').version
Copy-Item (Join-Path $env:NUGET_PACKAGES "microsoft.netcore.app.runtime.win-x64/$coreVersion/LICENSE.TXT") (Join-Path $licenses 'dotnet-runtime-LICENSE.txt')
Copy-Item (Join-Path $env:NUGET_PACKAGES "microsoft.netcore.app.runtime.win-x64/$coreVersion/THIRD-PARTY-NOTICES.TXT") (Join-Path $licenses 'dotnet-runtime-THIRD-PARTY-NOTICES.txt')
Copy-Item (Join-Path $env:NUGET_PACKAGES "microsoft.windowsdesktop.app.runtime.win-x64/$desktopVersion/LICENSE") (Join-Path $licenses 'WindowsDesktop-runtime-LICENSE.txt')
@'
CBB-CloseButtonBlocker uses the .NET runtime and Windows Forms under their
respective MIT and third-party licenses. See licenses/ for full notices.
CBB source itself is GPL-3.0-only; see LICENSE.txt and COPYRIGHT.txt.
The installer was built with WiX Toolset 4.0.6 (Microsoft Reciprocal License).
The Windows Installer runtime is supplied by Windows, not by this package.
'@ | Set-Content -LiteralPath (Join-Path $payload 'THIRD-PARTY-NOTICES.txt') -Encoding UTF8
$payloadWxs = Join-Path $work 'Payload.wxs'
& ./installer/Generate-Payload.ps1 -PayloadDir $payload -OutputPath $payloadWxs
$msi = Join-Path $release "CBB-CloseButtonBlocker-$ReleaseName-win-x64.msi"
& $wix build installer/Package.wxs $payloadWxs -arch x64 -d "PayloadDir=$payload" -d "ProductVersion=$ProductVersion" -d "ProductCode=$ProductCode" -o $msi
if ($LASTEXITCODE -ne 0) { throw 'MSI build failed.' }
$zip = Join-Path $release "CBB-CloseButtonBlocker-$ReleaseName-win-x64.zip"
Compress-Archive -Path "$payload/*" -DestinationPath $zip -Force
$hashes = foreach ($file in @($msi, $zip)) { $hash = Get-FileHash -Algorithm SHA256 -LiteralPath $file; "$($hash.Hash.ToLowerInvariant())  $(Split-Path $file -Leaf)" }
$hashes | Set-Content -LiteralPath (Join-Path $release 'SHA256SUMS.txt') -Encoding ASCII
Write-Output "Release artifacts: $release"
