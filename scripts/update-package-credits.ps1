$ErrorActionPreference = 'Stop'
$projectDirectory = Join-Path $PSScriptRoot '../src/AudioSwitcher'
$assets = Get-Content (Join-Path $projectDirectory 'obj/project.assets.json') -Raw | ConvertFrom-Json
$credits = foreach ($library in $assets.libraries.PSObject.Properties | Sort-Object Name) {
    if ($library.Value.type -ne 'package') { continue }
    $name, $version = $library.Name.Split('/')
    $nuspecPath = $null
    foreach ($folder in $assets.packageFolders.PSObject.Properties.Name) {
        $candidate = Join-Path $folder "$($library.Value.path)/$($name.ToLowerInvariant()).nuspec"
        if (Test-Path -LiteralPath $candidate) { $nuspecPath = $candidate; break }
    }
    if (-not $nuspecPath) { throw "Missing restored metadata for $name $version. Run dotnet restore first." }
    [xml]$nuspec = Get-Content -LiteralPath $nuspecPath -Raw
    $metadata = $nuspec.package.metadata
    $license = if ($metadata.license) { $metadata.license.InnerText } else { [string]$metadata.licenseUrl }
    if (-not $license) { $license = 'See package license' }
    [ordered]@{
        Name = $name
        Version = $version
        Authors = [string]$metadata.authors
        License = $license
        Url = "https://www.nuget.org/packages/$name/$version"
    }
}
$credits | Sort-Object { $_.Name } | ConvertTo-Json -Depth 3 | Set-Content (Join-Path $projectDirectory 'Assets/PackageCredits.json') -Encoding utf8
