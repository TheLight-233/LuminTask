param(
    [Parameter(Mandatory = $true)][string]$CoreAssembly,
    [Parameter(Mandatory = $true)][string]$UnsafeAssembly,
    [string]$ProjectPath
)
$ErrorActionPreference = 'Stop'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '../..')).Path
if (-not $ProjectPath) { $ProjectPath = Join-Path $repoRoot 'artifacts/unity-extreme' }
$projectRoot = [IO.Path]::GetFullPath($ProjectPath)
New-Item -ItemType Directory -Path (Join-Path $projectRoot 'Assets/Editor'), (Join-Path $projectRoot 'Assets/Plugins'), (Join-Path $projectRoot 'Packages'), (Join-Path $projectRoot 'ProjectSettings') -Force | Out-Null
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'BenchEntry.cs') -Destination (Join-Path $projectRoot 'Assets/BenchEntry.cs')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'Editor/BenchBuild.cs') -Destination (Join-Path $projectRoot 'Assets/Editor/BenchBuild.cs')
Copy-Item -LiteralPath (Join-Path $repoRoot 'Unity/UnityPlayerLoop.cs') -Destination (Join-Path $projectRoot 'Assets/UnityPlayerLoop.cs')
Copy-Item -LiteralPath $CoreAssembly -Destination (Join-Path $projectRoot 'Assets/Plugins/LuminTask.dll')
Copy-Item -LiteralPath $UnsafeAssembly -Destination (Join-Path $projectRoot 'Assets/Plugins/System.Runtime.CompilerServices.Unsafe.dll')
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Destination (Join-Path $projectRoot 'Packages/manifest.json')
Write-Output $projectRoot
