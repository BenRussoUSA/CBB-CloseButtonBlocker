# Run only with permission to install CBB for the current user.
# This script requires a clean CBB profile and leaves the final alpha installed.
param([switch]$ResumeAfterUpgrade)
$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot
Set-Location $repoRoot
$releaseMsi = (Resolve-Path 'artifacts/release/CBB-CloseButtonBlocker-0.8.3-win-x64.msi').Path
$installDir = Join-Path $env:LOCALAPPDATA 'Programs/CBB-CloseButtonBlocker'
$settingsDir = Join-Path $env:LOCALAPPDATA 'CBB-CloseButtonBlocker'
$settingsFile = Join-Path $settingsDir 'settings.json'
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$name = 'CBB-CloseButtonBlocker'
$productCode = '{E5E14797-42C0-4205-A5D7-401CA5C7AA49}'
$oldCode = '{428160C4-D002-4AE3-90A5-33A173FFBE6C}'
if (-not $ResumeAfterUpgrade) {
    if ((Test-Path $installDir) -or (Test-Path $settingsDir)) { throw 'A CBB installation or settings directory already exists. Test aborted without modifying it.' }
    if ((Get-ItemProperty -Path $runKey -Name $name -ErrorAction SilentlyContinue)) { throw 'Existing CBB startup value found. Test aborted.' }
    if (Get-Process -Name $name -ErrorAction SilentlyContinue) { throw 'CBB is already running. Test aborted.' }
} elseif ((Get-Content $settingsFile -Raw | ConvertFrom-Json).Rules[0].Pattern -ne 'CBB upgrade test sentinel 864591') { throw 'Expected temporary test settings not found.' }
$testDir = Join-Path $repoRoot 'artifacts/installer-tests'
New-Item -ItemType Directory -Force $testDir | Out-Null
$buildDir = Get-ChildItem artifacts -Directory -Filter 'build-*' | Sort-Object Name -Descending | Select-Object -First 1
$env:DOTNET_ROOT = Join-Path $repoRoot '.tools/dotnet'
$oldMsi = Join-Path $testDir 'CBB-upgrade-fixture-0.7.99.msi'
if (-not $ResumeAfterUpgrade) {
    & ./.tools/wix/wix.exe build installer/Package.wxs (Join-Path $buildDir.FullName 'Payload.wxs') -arch x64 -d "PayloadDir=$($buildDir.FullName)/payload" -d 'ProductVersion=0.7.99' -d "ProductCode=$oldCode" -o $oldMsi
    if ($LASTEXITCODE -ne 0) { throw 'Upgrade fixture packaging failed.' }
}
function Run-Msi([string]$action, [string]$target, [string]$label, [switch]$ExpectFailure) {
    $log = Join-Path $testDir "$label.log"
    $arguments = "$action `"$target`" /qn /norestart /l*v `"$log`""
    $process = Start-Process -FilePath "$env:SystemRoot/System32/msiexec.exe" -ArgumentList $arguments -PassThru -Wait -WindowStyle Hidden
    Write-Output "$label exit=$($process.ExitCode)"
    if ($ExpectFailure) { if ($process.ExitCode -eq 0) { throw 'Downgrade incorrectly succeeded.' } }
    elseif ($process.ExitCode -notin @(0, 3010)) { throw "$label failed. Inspect $log" }
}
if (-not $ResumeAfterUpgrade) {
Run-Msi '/i' $oldMsi '01-install-old'
if (-not (Test-Path (Join-Path $installDir "$name.exe"))) { throw 'Installed executable missing.' }
New-Item -ItemType Directory -Force $settingsDir | Out-Null
$sentinel = '{"SchemaVersion":1,"LaunchAtStartup":true,"Rules":[{"Pattern":"CBB upgrade test sentinel 864591","IsRegex":false}]}'
[IO.File]::WriteAllText($settingsFile, $sentinel)
if (-not (Test-Path $runKey)) { New-Item -Path $runKey | Out-Null }
New-ItemProperty -Path $runKey -Name $name -Value "`"$installDir\$name.exe`"" -PropertyType String -Force | Out-Null
$before = (Get-FileHash $settingsFile).Hash
$running = Start-Process -FilePath (Join-Path $installDir "$name.exe") -PassThru -WindowStyle Hidden
Start-Sleep -Seconds 2
if ($running.HasExited) { throw 'Installed self-contained application exited unexpectedly.' }
Run-Msi '/i' $releaseMsi '02-upgrade-alpha'
if ((Get-FileHash $settingsFile).Hash -ne $before) { throw 'Upgrade changed saved settings.' }
if (-not (Get-ItemProperty -Path $runKey -Name $name -ErrorAction SilentlyContinue)) { throw 'Upgrade lost startup setting.' }
if (-not $running.WaitForExit(5000)) { throw 'Upgrade did not stop the running application.' }
}
$entries = @(foreach ($registryRoot in @('HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall','HKLM:\Software\Microsoft\Windows\CurrentVersion\Uninstall')) { Get-ChildItem $registryRoot -ErrorAction SilentlyContinue | Get-ItemProperty | Where-Object DisplayName -eq $name })
if ($entries.Count -ne 1 -or $entries[0].DisplayVersion -ne '0.8.3') { throw 'Upgrade registration is incorrect.' }
Run-Msi '/i' $oldMsi '03-reject-downgrade' -ExpectFailure
$downgradeLog = Get-Content (Join-Path $testDir '03-reject-downgrade.log') -Raw
if ($downgradeLog -notmatch 'A newer version of CBB-CloseButtonBlocker is already installed') { throw 'Downgrade failed for an unexpected reason.' }
Run-Msi '/x' $productCode '04-uninstall'
if (Test-Path (Join-Path $installDir "$name.exe")) { throw 'Uninstall left the application executable.' }
if (Get-ItemProperty -Path $runKey -Name $name -ErrorAction SilentlyContinue) { throw 'Uninstall left startup enabled.' }
$retained = Get-Content $settingsFile -Raw | ConvertFrom-Json
if ($retained.Rules[0].Pattern -ne 'CBB upgrade test sentinel 864591') { throw 'Uninstall lost retained rules.' }
# Only remove the exact test-generated settings files after verifying their contents.
foreach ($file in @($settingsFile, ($settingsFile + '.bak'))) {
    if (Test-Path -LiteralPath $file) {
        $data = Get-Content -LiteralPath $file -Raw | ConvertFrom-Json
        if ($data.Rules.Count -ne 1 -or $data.Rules[0].Pattern -ne 'CBB upgrade test sentinel 864591') { throw 'Unexpected settings found; retaining them.' }
        Remove-Item -LiteralPath $file
    }
}
if (@(Get-ChildItem -LiteralPath $settingsDir -Force).Count -eq 0) { Remove-Item -LiteralPath $settingsDir }
Run-Msi '/i' $releaseMsi '05-install-final-alpha'
Write-Output 'PASS: install, running-app shutdown, settings/startup preservation, downgrade rejection, uninstall, and final clean install.'
Write-Output 'CBB 0.8.3 is installed for the current user; launch from Start for UAT.'
