param([string]$Version = '6000.3.28f1')

$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
$runRoot = Join-Path $projectRoot 'output/engine-spike'
$editor = Join-Path $env:USERPROFILE "Unity/Editors/$Version/Editor/Unity.exe"
$cli = Join-Path $env:LOCALAPPDATA 'Unity/bin/unity.exe'
New-Item -ItemType Directory -Path $runRoot -Force | Out-Null

$deadline = (Get-Date).AddMinutes(20)
while (Get-Process -Name "UnitySetup64-$Version" -ErrorAction SilentlyContinue) {
    if ((Get-Date) -ge $deadline) { throw "Unity $Version setup still running." }
    Start-Sleep -Seconds 8
}
foreach ($required in 'Data/Tools/7z.exe', 'Data/Resources/PackageManager/Server/UnityPackageManager.exe', 'Data/PlaybackEngines/windowsstandalonesupport') {
    if (!(Test-Path -LiteralPath (Join-Path (Split-Path -Parent $editor) $required))) {
        throw "Unity $Version installation is missing $required"
    }
}

$templates = @{ URP = 'com.unity.template.urp-blank'; HDRP = 'com.unity.template.hdrp-blank' }
foreach ($pipeline in 'URP','HDRP') {
    $folderName = "$($pipeline.ToLowerInvariant())-comparison"
    $project = Join-Path $runRoot $folderName
    if (!(Test-Path -LiteralPath (Join-Path $project 'ProjectSettings/ProjectVersion.txt'))) {
        & $cli projects create $folderName --output-dir $runRoot --editor-version $Version --template $templates[$pipeline] --bare --no-cloud --non-interactive --format json
        if ($LASTEXITCODE -ne 0) { throw "Could not create $pipeline spike project." }
    }
    $source = Join-Path $project 'Assets/Spike'
    New-Item -ItemType Directory -Path (Join-Path $source 'Editor') -Force | Out-Null
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'engine-spike/SpikeRun.cs'),(Join-Path $PSScriptRoot 'engine-spike/BedRail.obj') -Destination $source -Force
    Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'engine-spike/Editor/BuildSpike.cs') -Destination (Join-Path $source 'Editor') -Force

    $editorLog = Join-Path $runRoot "$pipeline-editor.log"
    $arguments = "-batchmode -nographics -quit -job-worker-count 4 -projectPath `"$project`" -executeMethod CTCC.Spike.BuildSpike.BuildWindows -logFile `"$editorLog`""
    $build = Start-Process -FilePath $editor -WorkingDirectory $project -ArgumentList $arguments -WindowStyle Hidden -PassThru -Wait
    if ($build.ExitCode -ne 0) { throw "$pipeline build failed. See $editorLog." }
    $buildDir = Join-Path $runRoot "output/$pipeline"
    $measureDir = Join-Path $runRoot "$pipeline-data"
    $playLog = Join-Path $runRoot "$pipeline-player.log"
    $playerArgs = "-screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -force-d3d12 -ctcc-report `"$measureDir`" -logFile `"$playLog`""
    $run = Start-Process -FilePath (Join-Path $buildDir 'Spike.exe') -ArgumentList $playerArgs -WindowStyle Hidden -PassThru
    if (!$run.WaitForExit(180000)) { throw "$pipeline spike did not finish after 180 seconds. See $playLog." }
    if ($run.ExitCode -ne 0) { throw "$pipeline spike exit $($run.ExitCode). See $playLog." }
    $benchmark = Join-Path $measureDir 'benchmark.txt'
    "build_bytes=$((Get-ChildItem -LiteralPath $buildDir -File -Recurse | Measure-Object Length -Sum).Sum)" | Add-Content -LiteralPath $benchmark
    Get-Content -LiteralPath $benchmark
}
