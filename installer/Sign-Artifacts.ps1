param(
    [Parameter(Mandatory=$true)][string[]]$Files,
    [Parameter(Mandatory=$true)][string]$MetadataPath,
    [Parameter(Mandatory=$true)][string]$SignToolPath,
    [Parameter(Mandatory=$true)][string]$DlibPath
)
$ErrorActionPreference = 'Stop'
foreach ($path in @($MetadataPath, $SignToolPath, $DlibPath) + $Files) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { throw 'A signing input file is missing.' }
}
foreach ($file in $Files) {
    & $SignToolPath sign /fd SHA256 /tr 'http://timestamp.acs.microsoft.com' /td SHA256 /dlib $DlibPath /dmdf $MetadataPath $file
    if ($LASTEXITCODE -ne 0) { throw 'Artifact Signing failed. Release packaging has stopped.' }
    & $SignToolPath verify /pa /all $file
    if ($LASTEXITCODE -ne 0) { throw 'Signature verification failed. Release packaging has stopped.' }
    $signature = Get-AuthenticodeSignature -LiteralPath $file
    if ($signature.Status -ne 'Valid' -or $null -eq $signature.TimeStamperCertificate) {
        throw 'A valid timestamped Authenticode signature is required.'
    }
}
