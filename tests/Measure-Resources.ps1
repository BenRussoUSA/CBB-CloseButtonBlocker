param(
    [Parameter(Mandatory=$true)][string]$BaselineExe,
    [Parameter(Mandatory=$true)][string]$CandidateExe,
    [string]$OutputPath = 'artifacts/resource-measurements.json',
    [int]$SampleSeconds = 30
)
$ErrorActionPreference = 'Stop'
if ($SampleSeconds -lt 10 -or $SampleSeconds -gt 60) { throw 'Use a sample between 10 and 60 seconds.' }
$BaselineExe = (Resolve-Path -LiteralPath $BaselineExe).Path
$CandidateExe = (Resolve-Path -LiteralPath $CandidateExe).Path
$settingsPath = Join-Path $env:LOCALAPPDATA 'CBB-CloseButtonBlocker/settings.json'
$hashBefore = if (Test-Path -LiteralPath $settingsPath) { (Get-FileHash -LiteralPath $settingsPath).Hash }
$runKey = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'
$startupBefore = Get-ItemPropertyValue -LiteralPath $runKey -Name 'CBB-CloseButtonBlocker' -ErrorAction SilentlyContinue
$hadRunningInstance = @(Get-Process -Name 'CBB-CloseButtonBlocker' -ErrorAction SilentlyContinue).Count -gt 0
function Stop-Cbb([string]$exe) {
    $stopper = Start-Process -FilePath $exe -ArgumentList '--stop' -WindowStyle Hidden -Wait -PassThru
    if ($stopper.ExitCode -ne 0) { throw 'CBB could not stop cleanly for measurement.' }
}
$results = @()
try {
    Stop-Cbb $BaselineExe
    foreach ($exe in @($BaselineExe, $CandidateExe)) {
        $version = (Get-Item -LiteralPath $exe).VersionInfo.ProductVersion
        Write-Output "Warming up CBB $version for 20 seconds."
        $app = Start-Process -FilePath $exe -WindowStyle Hidden -PassThru
        Start-Sleep -Seconds 20
        $app.Refresh()
        if ($app.HasExited) { throw 'The measured application exited unexpectedly.' }
        $startCpu = $app.TotalProcessorTime.TotalMilliseconds
        $clock = [Diagnostics.Stopwatch]::StartNew()
        $samples = @()
        for ($i=0; $i -lt $SampleSeconds; $i++) {
            Start-Sleep -Seconds 1
            $app.Refresh()
            $samples += [pscustomobject]@{WorkingSetBytes=$app.WorkingSet64;PrivateBytes=$app.PrivateMemorySize64}
        }
        $elapsed = $clock.Elapsed.TotalSeconds
        $cpu = $app.TotalProcessorTime.TotalMilliseconds - $startCpu
        $result = [pscustomobject]@{
            Version=$version;SampleSeconds=[Math]::Round($elapsed,3);CpuMilliseconds=$cpu
            OneCoreCpuPercent=[Math]::Round($cpu/($elapsed*10),4)
            MeanWorkingSetMiB=[Math]::Round(($samples.WorkingSetBytes | Measure-Object -Average).Average/1MB,2)
            MeanPrivateMiB=[Math]::Round(($samples.PrivateBytes | Measure-Object -Average).Average/1MB,2)
            MinWorkingSetMiB=[Math]::Round(($samples.WorkingSetBytes | Measure-Object -Minimum).Minimum/1MB,2)
            MaxWorkingSetMiB=[Math]::Round(($samples.WorkingSetBytes | Measure-Object -Maximum).Maximum/1MB,2)
        }
        $results += $result
        $result | ConvertTo-Json -Compress | Write-Output
        Stop-Cbb $exe
    }
    [pscustomobject]@{MeasuredUtc=[DateTime]::UtcNow.ToString('o');WarmupSeconds=20;Samples=$results} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath $OutputPath -Encoding UTF8
} finally {
    Stop-Cbb $CandidateExe
    if ($hadRunningInstance) { Start-Process -FilePath $BaselineExe -WindowStyle Hidden | Out-Null }
    $hashAfter = if (Test-Path -LiteralPath $settingsPath) { (Get-FileHash -LiteralPath $settingsPath).Hash }
    $startupAfter = Get-ItemPropertyValue -LiteralPath $runKey -Name 'CBB-CloseButtonBlocker' -ErrorAction SilentlyContinue
    if ($hashBefore -ne $hashAfter -or $startupBefore -ne $startupAfter) { throw 'Measurement changed settings or startup registration.' }
}
