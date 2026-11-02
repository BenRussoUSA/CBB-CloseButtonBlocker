param([Parameter(Mandatory=$true)][string]$PayloadDir, [Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference = 'Stop'
$payloadRoot = (Resolve-Path -LiteralPath $PayloadDir).Path.TrimEnd('\')
$xmlSettings = [System.Xml.XmlWriterSettings]::new()
$xmlSettings.Indent = $true
$writer = [System.Xml.XmlWriter]::Create($OutputPath, $xmlSettings)
$ns = 'http://wixtoolset.org/schemas/v4/wxs'
$components = [System.Collections.Generic.List[string]]::new()
function Get-Id([string]$prefix, [string]$relative) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try { $bytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($relative.ToLowerInvariant())); return $prefix + (($bytes | ForEach-Object { $_.ToString('x2') }) -join '').Substring(0, 24) }
    finally { $sha.Dispose() }
}
function Get-ComponentGuid([string]$relative) {
    $sha = [System.Security.Cryptography.SHA256]::Create()
    try {
        $bytes = $sha.ComputeHash([Text.Encoding]::UTF8.GetBytes('BenRussoUSA.CBB.peruser.x64/' + $relative.ToLowerInvariant()))
        $hex = (($bytes | ForEach-Object { $_.ToString('x2') }) -join '').Substring(0, 32)
        return [Guid]::ParseExact($hex, 'N').ToString('B')
    } finally { $sha.Dispose() }
}
function Write-Directory([string]$full, [string]$directoryId) {
    foreach ($file in Get-ChildItem -LiteralPath $full -File | Sort-Object Name) {
        $relative = $file.FullName.Substring($payloadRoot.Length + 1)
        $id = Get-Id 'C_' $relative
        $components.Add($id)
        $writer.WriteStartElement('Component', $ns); $writer.WriteAttributeString('Id', $id); $writer.WriteAttributeString('Guid', (Get-ComponentGuid $relative))
        $writer.WriteStartElement('File', $ns); $writer.WriteAttributeString('Id', (Get-Id 'F_' $relative)); $writer.WriteAttributeString('Source', $file.FullName); $writer.WriteEndElement()
        $writer.WriteStartElement('RegistryValue', $ns); $writer.WriteAttributeString('Root', 'HKCU'); $writer.WriteAttributeString('Key', 'Software\BenRussoUSA\CBB-CloseButtonBlocker\Components'); $writer.WriteAttributeString('Name', $id); $writer.WriteAttributeString('Type', 'integer'); $writer.WriteAttributeString('Value', '1'); $writer.WriteAttributeString('KeyPath', 'yes'); $writer.WriteEndElement()
        $writer.WriteStartElement('RemoveFolder', $ns); $writer.WriteAttributeString('Id', (Get-Id 'R_' $relative)); $writer.WriteAttributeString('Directory', $directoryId); $writer.WriteAttributeString('On', 'uninstall'); $writer.WriteEndElement()
        $writer.WriteEndElement()
    }
    foreach ($folder in Get-ChildItem -LiteralPath $full -Directory | Sort-Object Name) {
        $relative = $folder.FullName.Substring($payloadRoot.Length + 1)
        $id = Get-Id 'D_' $relative
        $writer.WriteStartElement('Directory', $ns); $writer.WriteAttributeString('Id', $id); $writer.WriteAttributeString('Name', $folder.Name)
        Write-Directory $folder.FullName $id
        $writer.WriteEndElement()
    }
}
try {
    $writer.WriteStartElement('Wix', $ns); $writer.WriteStartElement('Fragment', $ns)
    $writer.WriteStartElement('DirectoryRef', $ns); $writer.WriteAttributeString('Id', 'INSTALLFOLDER')
    Write-Directory $payloadRoot 'INSTALLFOLDER'
    $writer.WriteEndElement()
    $writer.WriteStartElement('ComponentGroup', $ns); $writer.WriteAttributeString('Id', 'Payload')
    foreach ($id in $components) { $writer.WriteStartElement('ComponentRef', $ns); $writer.WriteAttributeString('Id', $id); $writer.WriteEndElement() }
    $writer.WriteEndElement(); $writer.WriteEndElement(); $writer.WriteEndElement()
} finally { $writer.Dispose() }
