param(
    [string]$InstallerPath = 'artifacts/release/CBB-CloseButtonBlocker-0.9.4beta-win-x64.msi',
    [string]$PayloadPath,
    [switch]$DoNotLaunch
)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
Set-Location $repoRoot
$installer = (Resolve-Path -LiteralPath $InstallerPath).Path
if (-not $PayloadPath) {
    $build = Get-ChildItem artifacts -Directory -Filter 'build-*' | Sort-Object Name -Descending | Select-Object -First 1
    $PayloadPath = Join-Path $build.FullName 'payload'
}
$payload = (Resolve-Path -LiteralPath $PayloadPath).Path.TrimEnd('\')
$installDir = Join-Path $env:LOCALAPPDATA 'Programs/CBB-CloseButtonBlocker'
$settingsFile = Join-Path $env:LOCALAPPDATA 'CBB-CloseButtonBlocker/settings.json'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$startupBefore = Get-ItemPropertyValue -Path $runKey -Name 'CBB-CloseButtonBlocker' -ErrorAction SilentlyContinue
$settingsBefore = if (Test-Path -LiteralPath $settingsFile) { (Get-FileHash -LiteralPath $settingsFile).Hash } else { $null }
New-Item -ItemType Directory -Force artifacts | Out-Null
$log = Join-Path $repoRoot 'artifacts/upgrade-0.9.4beta.log'
$arguments = "/i `"$installer`" /qn /norestart /l*v `"$log`""
$process = Start-Process -FilePath "$env:SystemRoot/System32/msiexec.exe" -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
if ($process.ExitCode -notin @(0,3010)) { throw "Upgrade failed with code $($process.ExitCode). Inspect the local MSI log." }
$settingsAfter = if (Test-Path -LiteralPath $settingsFile) { (Get-FileHash -LiteralPath $settingsFile).Hash } else { $null }
$startupAfter = Get-ItemPropertyValue -Path $runKey -Name 'CBB-CloseButtonBlocker' -ErrorAction SilentlyContinue
if ($settingsBefore -ne $settingsAfter) { throw 'Saved settings changed during upgrade.' }
if ($startupBefore -ne $startupAfter) { throw 'Startup registration changed during upgrade.' }
foreach ($file in Get-ChildItem -LiteralPath $payload -Recurse -File) {
    $relative = $file.FullName.Substring($payload.Length + 1)
    $target = Join-Path $installDir $relative
    if (-not (Test-Path -LiteralPath $target) -or (Get-FileHash -LiteralPath $target).Hash -ne (Get-FileHash -LiteralPath $file.FullName).Hash) {
        throw "Installed-file verification failed: $relative"
    }
}
if (Get-ChildItem -LiteralPath $installDir -File -Filter '*.dll') { throw 'An old application or runtime DLL remains in the installation folder.' }
$entries = @(foreach ($registryRoot in @('HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall','HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall')) {
    Get-ChildItem $registryRoot -ErrorAction SilentlyContinue | Get-ItemProperty | Where-Object DisplayName -eq 'CBB-CloseButtonBlocker'
})
if ($entries.Count -ne 1 -or $entries[0].DisplayVersion -ne '0.9.4') { throw 'Unexpected installed-app registration.' }
if (-not $DoNotLaunch) {
    $app = Start-Process -FilePath (Join-Path $installDir 'CBB-CloseButtonBlocker.exe') -PassThru -WindowStyle Hidden
    Start-Sleep -Seconds 3
    if ($app.HasExited) { throw 'The upgraded app exited unexpectedly.' }
}
Write-Output 'PASS: 0.9.4beta installed; one Installed apps entry; settings and startup unchanged; all payload files match; no runtime DLLs remain.'
