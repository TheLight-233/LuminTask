[CmdletBinding()]
param(
    [string]$Version,
    [string]$Output = 'artifacts/release',
    [switch]$NativeAot,
    [switch]$Tag
)

$ErrorActionPreference = 'Stop'
$PSNativeCommandUseErrorActionPreference = $false
$root = Split-Path -Parent $PSScriptRoot
$outputRoot = [IO.Path]::GetFullPath($Output)
Push-Location $root
try {
    $versions = ([xml](Get-Content -Raw eng/Versions.props)).Project.PropertyGroup
    $declared = $versions.LuminTaskVersion
    if (!$Version) { $Version = $declared }
    if ($Version -cnotmatch '^([1-9]\d*|0)\.([1-9]\d*|0)\.([1-9]\d*|0)(-[0-9a-z-]+(\.[0-9a-z-]+)*)?$') {
        throw 'Expected a normalized NuGet version such as 2.0.0 or 2.0.0-rc.1.'
    }
    if ($Tag -and $Version -cne $declared) {
        throw "Tagged release must match LuminTaskVersion ($declared)."
    }
    if (Test-Path $outputRoot) {
        if (Get-ChildItem -LiteralPath $outputRoot -Force | Select-Object -First 1) {
            throw "Choose an empty candidate directory ($outputRoot)."
        }
    }

    function Invoke-DotNet {
        & dotnet @args
        if ($LASTEXITCODE -ne 0) { throw "dotnet $($args[0]) failed ($LASTEXITCODE)." }
    }

    $packages = New-Item -ItemType Directory -Force -Path (Join-Path $outputRoot 'packages')
    $checks = New-Item -ItemType Directory -Force -Path (Join-Path $outputRoot 'checks')
    $versionArg = "-p:LuminTaskVersion=$Version"
    Invoke-DotNet build src/LuminTask.AsyncEx -c Release $versionArg
    Invoke-DotNet pack src/LuminTask -c Release --no-build $versionArg -o $packages.FullName

    $package = Join-Path $packages "LuminTask.$Version.nupkg"
    $symbols = Join-Path $packages "LuminTask.$Version.snupkg"
    $tfms = 'netstandard2.1', 'net8.0', 'net9.0', 'net10.0'
    function Assert-Archive($file, $entries) {
        $archive = [IO.Compression.ZipFile]::OpenRead($file)
        try {
            foreach ($name in $entries) {
                if (!$archive.GetEntry($name)) { throw "$file is missing $name." }
            }
        }
        finally { $archive.Dispose() }
    }
    $runtimeEntries = foreach ($tfm in $tfms) { "lib/$tfm/LuminTask.dll"; "lib/$tfm/LuminTask.AsyncEx.dll" }
    $symbolEntries = $runtimeEntries -replace '\.dll$', '.pdb'
    Assert-Archive $package ($runtimeEntries + 'analyzers/dotnet/cs/LuminTask.Analyzer.dll' + 'README.md')
    Assert-Archive $symbols $symbolEntries

    $consumer = Join-Path $checks 'LuminTask.Tests.Package'
    New-Item -ItemType Directory -Force $consumer | Out-Null
    Copy-Item tests/LuminTask.Tests.Package/*.cs, tests/LuminTask.Tests.Package/*.csproj $consumer
    $config = New-Object System.Xml.XmlDocument
    $config.LoadXml(@'
<configuration>
  <packageSources>
    <clear />
    <add key="candidate" value="" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="candidate"><package pattern="LuminTask" /></packageSource>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
  </packageSourceMapping>
</configuration>
'@)
    $config.configuration.packageSources.add[0].SetAttribute('value', $packages.FullName)
    $configPath = Join-Path $checks 'NuGet.Config'
    $config.Save($configPath)
    $restoreArgs = @('--configfile', $configPath, '--packages', (Join-Path $checks 'nuget'), $versionArg)
    Invoke-DotNet restore $consumer @restoreArgs
    Invoke-DotNet run --project $consumer -c Release --no-restore $versionArg

    $invalid = & dotnet build $consumer -c Release --no-restore $versionArg '-p:DefineConstants=INVALID_CONSUMPTION' 2>&1
    $invalidExitCode = $LASTEXITCODE
    $invalid | Set-Content (Join-Path $checks 'analyzer.log')
    if ($invalidExitCode -eq 0 -or ($invalid -join "`n") -notmatch 'error LUM1001') {
        throw 'The installed analyzer did not reject an abandoned task with LUM1001.'
    }

    if ($NativeAot) {
        $aotProject = Join-Path $checks 'LuminTask.Tests.Aot'
        New-Item -ItemType Directory -Force $aotProject | Out-Null
        Copy-Item tests/LuminTask.Tests.Aot/*.csproj $aotProject
        Invoke-DotNet restore $aotProject @restoreArgs '-warnaserror:NU1903,NU1904'
        $native = Join-Path $checks 'native'
        Invoke-DotNet publish $aotProject -c Release --no-restore $versionArg -o $native
        $executable = Join-Path $native ('LuminTask.Tests.Aot' + $(if ($IsWindows) { '.exe' }))
        & $executable
        if ($LASTEXITCODE -ne 0) { throw 'The NativeAOT package consumer failed.' }
    }

    $unity = Join-Path $checks 'unity'
    $runtime = New-Item -ItemType Directory -Force -Path (Join-Path $unity 'Assets/LuminTask')
    $optional = New-Item -ItemType Directory -Force -Path (Join-Path $unity 'Optional')
    Copy-Item src/LuminTask.Unity/*.cs, src/LuminTask.Unity/*.cs.meta $runtime
    Get-ChildItem src/LuminTask.Unity -Directory | ForEach-Object {
        Copy-Item -LiteralPath $_.FullName -Destination $optional -Recurse
        $meta = "$($_.FullName).meta"
        if (Test-Path $meta) { Copy-Item -LiteralPath $meta -Destination $optional }
    }
    $archive = [IO.Compression.ZipFile]::OpenRead($package)
    try {
        foreach ($entry in $runtimeEntries | Where-Object { $_ -like 'lib/netstandard2.1/*' }) {
            [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry($entry), (Join-Path $runtime (Split-Path -Leaf $entry)))
        }
        [IO.Compression.ZipFileExtensions]::ExtractToFile($archive.GetEntry('analyzers/dotnet/cs/LuminTask.Analyzer.dll'), (Join-Path $optional 'LuminTask.Analyzer.dll'))
    }
    finally { $archive.Dispose() }
    $assets = Get-Content -Raw src/LuminTask/obj/project.assets.json | ConvertFrom-Json -AsHashtable
    $unsafe = foreach ($folder in $assets.packageFolders.Keys) {
        $path = Join-Path $folder "system.runtime.compilerservices.unsafe/$($versions.UnsafeVersion)/lib/netstandard2.0/System.Runtime.CompilerServices.Unsafe.dll"
        if (Test-Path -LiteralPath $path) { $path; break }
    }
    if (!$unsafe) { throw 'The restored netstandard Unsafe assembly was not found.' }
    Copy-Item -LiteralPath $unsafe -Destination $runtime
    Copy-Item LICENSE, eng/Unity-Installation.md, eng/licenses/Unsafe-LICENSE.txt $unity
    $zip = Join-Path $packages "LuminTask.Unity.$Version.zip"
    [IO.Compression.ZipFile]::CreateFromDirectory($unity, $zip)

    Get-ChildItem -LiteralPath $packages -File | Sort-Object Name | ForEach-Object {
        "$((Get-FileHash $_.FullName -Algorithm SHA256).Hash.ToLowerInvariant())  $($_.Name)"
    } | Set-Content -Encoding ascii (Join-Path $packages 'SHA256SUMS')
    Copy-Item CHANGELOG.md $outputRoot
    Write-Host "Validated LuminTask $Version at $outputRoot"
}
finally { Pop-Location }
