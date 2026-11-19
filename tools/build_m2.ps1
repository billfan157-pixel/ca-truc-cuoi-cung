param(
    [string]$Output = 'output\m2\windows',
    [switch]$SkipGenerate,
    [switch]$SkipBuild,
    [switch]$Development
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$originalRoot = $repo
if ($repo -match '[^\x00-\x7F]') {
    $alias = 'C:\CTCC-Unity-Workspace'
    if (!(Test-Path -LiteralPath $alias) -or (Get-Item -LiteralPath $alias).Target -ne $repo) {
        throw "Unity Bee needs an ASCII junction at $alias -> $repo."
    }
    $repo = $alias
}

$project = Join-Path $repo 'Prototype'
$outAbs = if ([IO.Path]::IsPathRooted($Output)) { $Output } else { Join-Path $repo $Output }
if ($outAbs.StartsWith("$originalRoot\", [StringComparison]::OrdinalIgnoreCase)) {
    $outAbs = $repo + $outAbs.Substring($originalRoot.Length)
}
New-Item -ItemType Directory -Force -Path $outAbs | Out-Null

$version = (Get-Content -LiteralPath (Join-Path $project 'ProjectSettings\ProjectVersion.txt') -TotalCount 1).Split(' ')[1]
$editor = Join-Path $env:USERPROFILE "Unity\Editors\$version\Editor\Unity.exe"
if (!(Test-Path -LiteralPath $editor)) { $editor = "C:\Program Files\Unity\Hub\Editor\$version\Editor\Unity.exe" }
if (!(Test-Path -LiteralPath $editor)) { throw "Cannot find Unity $version." }

function Invoke-CleanUnity {
    param([string[]]$ArgsList, [string]$WorkDir)
    # Use clean env + true NUL stdin (without CreateNoWindow broken pipe) so Unity Bee never stalls.
    $argJson = ($ArgsList | ConvertTo-Json -Compress)
    if ($ArgsList.Count -eq 1) { $argJson = "[$argJson]" }
    $argB64 = [Convert]::ToBase64String([Text.Encoding]::UTF8.GetBytes($argJson))
    $py = @"
import base64, json, os, subprocess, sys
env = {k: v for k, v in os.environ.items() if not k.startswith('ANTIGRAVITY_') and not k.startswith('CHROME_')}
cmd = [sys.argv[1]] + json.loads(base64.b64decode(sys.argv[2]).decode('utf-8'))
p = subprocess.run(cmd, cwd=sys.argv[3], env=env, stdin=subprocess.DEVNULL)
sys.exit(p.returncode)
"@
    & python -c $py $editor $argB64 $WorkDir | Out-Host
    return $LASTEXITCODE
}

function Invoke-PrewarmTypeDb {
    param([string]$ProjectDir)
    $bee = Join-Path (Split-Path -Parent $editor) 'Data\Tools\BuildPipeline\bee_backend.exe'
    $dag = Join-Path $ProjectDir 'Library\Bee\1900b0aESkipCompile.dag'
    if ((Test-Path -LiteralPath $bee) -and (Test-Path -LiteralPath $dag)) {
        Write-Host "[M2] Pre-warming Bee TypeDb cache..."
        $py = @"
import os, subprocess, sys
env = {k: v for k, v in os.environ.items() if not k.startswith('ANTIGRAVITY_') and not k.startswith('CHROME_')}
cmd = [sys.argv[1], '--dagfile=Library/Bee/1900b0aESkipCompile.dag', '--continue-on-failure', '--dagfilejson=Library\\Bee\\1900b0aESkipCompile.dag.json', 'ScriptAssembliesAndTypeDB']
p = subprocess.run(cmd, cwd=sys.argv[2], env=env, stdin=subprocess.DEVNULL, timeout=45)
sys.exit(p.returncode)
"@
        & python -c $py $bee $ProjectDir
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "TypeDb pre-warm exited $LASTEXITCODE (continuing to Unity build)."
        }
    }
}

if (!$SkipGenerate) {
    $genLog = Join-Path $outAbs 'generate-scenes.log'
    Write-Host "[M2] Generating 5 additive scenes, dual GI LightProbes & multi-scene Occlusion..."
    $rc = Invoke-CleanUnity -WorkDir $project -ArgsList @(
        '-batchmode', '-quit', '-job-worker-count', '4',
        '-projectPath', $project,
        '-executeMethod', 'CTCC.Editor.HospitalAuthoring.ReferenceScene',
        '-logFile', $genLog
    )
    if ($rc -ne 0 -or !(Select-String -LiteralPath $genLog -Quiet -Pattern '^CTCC_SCENE ')) {
        Get-Content -LiteralPath $genLog -Tail 60
        throw "M2 scene generation failed (exit $rc). See $genLog."
    }
    Select-String -LiteralPath $genLog -Pattern '^CTCC_' | ForEach-Object { $_.Line }
}

if (!$SkipBuild) {
    Invoke-PrewarmTypeDb -ProjectDir $project
    $exe = Join-Path $outAbs 'CaTrucCuoiCung.exe'
    $buildLog = Join-Path $outAbs 'build.log'
    $buildArgs = @(
        '-batchmode', '-nographics', '-quit', '-job-worker-count', '4',
        '-projectPath', $project,
        '-executeMethod', 'CTCC.Editor.HospitalAuthoring.Windows',
        '-ctcc-build', $exe,
        '-logFile', $buildLog
    )
    if ($Development) { $buildArgs += '-ctcc-development' }
    Write-Host "[M2] Building Windows x64 Player -> $exe ..."
    $rc = Invoke-CleanUnity -WorkDir $project -ArgsList $buildArgs
    if ($rc -ne 0 -or !(Select-String -LiteralPath $buildLog -Quiet -Pattern '^CTCC_BUILD ')) {
        Get-Content -LiteralPath $buildLog -Tail 60
        throw "M2 Windows build failed (exit $rc). See $buildLog."
    }
    Select-String -LiteralPath $buildLog -Pattern '^CTCC_BUILD ' | ForEach-Object { $_.Line }

    $revision = & git -C $repo rev-parse HEAD
    $protoDiff = (& git -C $repo status --porcelain -- Prototype) -join ';'
    $managed = Join-Path $outAbs 'CaTrucCuoiCung_Data\Managed\Assembly-CSharp.dll'
    $lines = @(
        "revision=$revision",
        "development=$Development",
        "unity=$version",
        "prototype_diff=$protoDiff",
        "assembly_sha256=$((Get-FileHash -LiteralPath $managed).Hash.ToLowerInvariant())"
    )
    foreach ($sc in 'AnSinh_Bootstrap','LOC01_Exterior','LOC02_Reception','LOC03_Ward101','LOC04_Emergency') {
        $p = Join-Path $project "Assets\CTCC\Scenes\$sc.unity"
        if (Test-Path -LiteralPath $p) {
            $lines += "scene_${sc}_sha256=$((Get-FileHash -LiteralPath $p).Hash.ToLowerInvariant())"
        }
    }
    $lines | Set-Content -LiteralPath (Join-Path $outAbs 'build-identity.txt')
    Get-Content -LiteralPath (Join-Path $outAbs 'build-identity.txt')
}
