param(
    [Parameter(Mandatory)]
    [ValidatePattern('^\d+\.\d+\.\d+$')]
    [string]$Version
)

$ErrorActionPreference = 'Stop'
$repositoryRoot = [System.IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$artifactsDirectory = [System.IO.Path]::GetFullPath((Join-Path $repositoryRoot 'artifacts'))

if (-not $artifactsDirectory.StartsWith($repositoryRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
    throw 'The artifacts directory resolved outside the repository.'
}

if (Test-Path -LiteralPath $artifactsDirectory) {
    Remove-Item -LiteralPath $artifactsDirectory -Recurse -Force
}

$publishDirectory = Join-Path $artifactsDirectory 'publish'
$releaseDirectory = Join-Path $artifactsDirectory 'releases'

dotnet tool restore
dotnet publish (Join-Path $repositoryRoot 'src/AudioSwitcher/AudioSwitcher.csproj') `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    --output $publishDirectory `
    -p:Version=$Version

dotnet vpk pack `
    --packId Diddlik.AudioSwitcher `
    --packVersion $Version `
    --packDir $publishDirectory `
    --mainExe AudioSwitcher.exe `
    --packTitle AudioSwitcher `
    --packAuthors Diddlik `
    --runtime win-x64 `
    --icon (Join-Path $repositoryRoot 'src/AudioSwitcher/Assets/audioswitcher.ico') `
    --outputDir $releaseDirectory

Write-Host "Installer created in $releaseDirectory"
