param(
    [Parameter(Mandatory = $true)][string]$PublishDirectory,
    [Parameter(Mandatory = $true)][string]$OutputDirectory
)

$ErrorActionPreference = "Stop"
$publish = (Resolve-Path $PublishDirectory).Path
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..")).Path
$output = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $OutputDirectory))
$stage = Join-Path $output ".stage"
$zipPath = Join-Path $output "AutoClicker-Portable-win-x64.zip"
$sumPath = Join-Path $output "SHA256SUMS.txt"

if (Test-Path $output) { Remove-Item $output -Recurse -Force }
New-Item -ItemType Directory -Path $stage -Force | Out-Null

try {
    Copy-Item (Join-Path $publish "*") $stage -Recurse -Force
    Copy-Item (Join-Path $repoRoot "LICENSE") $stage -Force
    Copy-Item (Join-Path $repoRoot "README.md") $stage -Force

    Add-Type -AssemblyName System.IO.Compression
    $fileStream = [System.IO.File]::Open($zipPath, [System.IO.FileMode]::CreateNew)
    try {
        $archive = [System.IO.Compression.ZipArchive]::new(
            $fileStream,
            [System.IO.Compression.ZipArchiveMode]::Create,
            $false
        )
        try {
            Get-ChildItem $stage -File -Recurse | Sort-Object FullName | ForEach-Object {
                $relative = ([System.IO.Path]::GetRelativePath($stage, $_.FullName) -replace '\\', '/')
                $entry = $archive.CreateEntry($relative, [System.IO.Compression.CompressionLevel]::Optimal)
                $entry.LastWriteTime = [DateTimeOffset]::new(2000, 1, 1, 0, 0, 0, [TimeSpan]::Zero)
                $input = [System.IO.File]::OpenRead($_.FullName)
                try {
                    $entryStream = $entry.Open()
                    try { $input.CopyTo($entryStream) }
                    finally { $entryStream.Dispose() }
                }
                finally { $input.Dispose() }
            }
        }
        finally { $archive.Dispose() }
    }
    finally { $fileStream.Dispose() }

    $hash = (Get-FileHash $zipPath -Algorithm SHA256).Hash.ToLowerInvariant()
    [System.IO.File]::WriteAllText(
        $sumPath,
        "$hash  AutoClicker-Portable-win-x64.zip`n",
        [System.Text.Encoding]::ASCII
    )
}
finally {
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
}

Write-Host "Created $zipPath"
Write-Host "Created $sumPath"
