[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$UnityEditor,
    [Parameter(Mandatory)][string]$Archive,
    [string]$Output = 'artifacts/unity-smoke',
    [switch]$TextMeshPro
)

$ErrorActionPreference = 'Stop'
if (!$IsWindows) { throw 'This fixture targets the Windows IL2CPP Player.' }
$fixture = Join-Path (Split-Path -Parent $PSScriptRoot) 'tests/LuminTask.Tests.Unity'
$outputPath = [IO.Path]::GetFullPath($Output)
if (Test-Path $outputPath) { throw 'Choose a new Unity smoke output directory.' }
$project = Join-Path $outputPath 'project'
Expand-Archive -LiteralPath $Archive -DestinationPath $project
$editorAssets = New-Item -ItemType Directory -Path (Join-Path $project 'Assets/Editor')
$resources = New-Item -ItemType Directory -Path (Join-Path $project 'Assets/Resources')
$packages = New-Item -ItemType Directory -Path (Join-Path $project 'Packages')
Copy-Item (Join-Path $fixture 'SmokeBuild.cs') $editorAssets
Copy-Item (Join-Path $fixture 'SmokeEntry.cs'), (Join-Path $fixture 'SmokeCoroutineHost.cs') (Join-Path $project 'Assets')
Set-Content -NoNewline -Encoding ascii (Join-Path $resources 'lumin-smoke.txt') 'lumin'
$manifest = Get-Content -Raw (Join-Path $fixture 'manifest.json') | ConvertFrom-Json -AsHashtable
if ($TextMeshPro) {
    $manifest.dependencies['com.unity.textmeshpro'] = '3.0.7'
    Copy-Item (Join-Path $project 'Optional/TextMeshPro'), (Join-Path $project 'Optional/TextMeshPro.meta') (Join-Path $project 'Assets/LuminTask') -Recurse
    Copy-Item (Join-Path $fixture 'SmokeTmp.cs') (Join-Path $project 'Assets/SmokeEntry.Tmp.cs')
}
$manifest | ConvertTo-Json -Depth 4 | Set-Content (Join-Path $packages 'manifest.json')

function Invoke-HiddenProcess($executable, [string[]]$arguments, [int]$timeoutSeconds) {
    $info = [Diagnostics.ProcessStartInfo]::new($executable)
    $info.UseShellExecute = $false
    $info.CreateNoWindow = $true
    foreach ($argument in $arguments) { $info.ArgumentList.Add($argument) }
    $process = [Diagnostics.Process]::Start($info)
    try {
        if (!$process.WaitForExit(1000 * $timeoutSeconds)) {
            $process.Kill($true)
            throw "Process timed out. Logs are in $outputPath."
        }
        if ($process.ExitCode -ne 0) {
            throw "$executable exited with code $($process.ExitCode). Logs are in $outputPath."
        }
    }
    finally { $process.Dispose() }
}

$player = Join-Path $outputPath 'Player/Smoke.exe'
$buildLog = Join-Path $outputPath 'build.log'
$playerLog = Join-Path $outputPath 'player.log'
Invoke-HiddenProcess $UnityEditor @('-batchmode','-nographics','-quit','-projectPath',$project,
    '-buildTarget','StandaloneWindows64','-executeMethod','SmokeBuild.Run','-luminPlayer',$player,'-logFile',$buildLog) 1200
Invoke-HiddenProcess $player @('-batchmode','-nographics','-logFile',$playerLog) 90
if (!(Select-String -Path $playerLog -SimpleMatch 'LUMIN_UNITY_TEST_PASS' -Quiet)) {
    throw "Missing success marker in $playerLog."
}
Write-Host "Unity IL2CPP package smoke passed. Logs are in $outputPath."
