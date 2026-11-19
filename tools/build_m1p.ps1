param(
    [Parameter(Mandatory)][string]$Output,
    [string]$Stage,
    [string]$Baseline,
    [switch]$Development
)

$ErrorActionPreference = 'Stop'
$repo = Split-Path -Parent $PSScriptRoot
$originalRoot = $repo
if ($repo -match '[^\x00-\x7F]') {
    $alias = 'C:\CTCC-Unity-Workspace'
    if (!(Test-Path -LiteralPath $alias) -or (Get-Item -LiteralPath $alias).Target -ne $repo) {
        throw "Unity Bee needs an ASCII path. Create a directory junction at $alias pointing to this repository, or use an ASCII checkout."
    }
    $repo = $alias
}
$outputRoot = Join-Path $repo 'output'
if (!$Stage) { $Stage = Join-Path $outputRoot 'm1p-build-stage' }
$Stage = [IO.Path]::GetFullPath($Stage)
$Output = [IO.Path]::GetFullPath($Output)
if ($Output.StartsWith("$originalRoot\", [StringComparison]::OrdinalIgnoreCase)) {
    $Output = $repo + $Output.Substring($originalRoot.Length)
}
if (!$Stage.StartsWith("$outputRoot\", [StringComparison]::OrdinalIgnoreCase) -or $Stage -match '[^\x00-\x7F]') {
    throw 'The Unity staging directory must be a dedicated ASCII directory under output.'
}
New-Item -ItemType Directory -Force -Path $Stage,$Output | Out-Null
$marker = Join-Path $Stage '.ctcc-m1p-stage'
if (!(Test-Path -LiteralPath $marker)) {
    if (Get-ChildItem -LiteralPath $Stage -Force | Select-Object -First 1) { throw 'Choose an empty staging directory.' }
    New-Item -ItemType File -Path $marker | Out-Null
}
$source = Join-Path $repo 'Prototype'
$temporary = $null
try {
    if ($Baseline) {
        $temporary = Join-Path $Stage 'baseline-source'
        if (Test-Path -LiteralPath $temporary) { throw "Baseline scratch directory already exists: $temporary" }
        New-Item -ItemType Directory -Path $temporary | Out-Null
        $archive = Join-Path $temporary 'tree.tar'
        & git -C $repo archive --format=tar -o $archive $Baseline Prototype/Assets Prototype/ProjectSettings Prototype/Packages
        if ($LASTEXITCODE) { throw 'Git could not archive the baseline project.' }
        & tar -xf $archive -C $temporary
        if ($LASTEXITCODE) { throw 'Baseline archive extraction failed.' }
        $source = Join-Path $temporary 'Prototype'
        $runtime = Join-Path $source 'Assets/CTCC/Runtime'
        foreach ($name in 'HospitalGame.Benchmark.cs','HospitalGame.Benchmark.cs.meta') {
            Copy-Item -LiteralPath (Join-Path "$repo/Prototype/Assets/CTCC/Runtime" $name) -Destination $runtime
        }
        $game = Join-Path $runtime 'HospitalGame.cs'
        $original = [IO.File]::ReadAllText($game)
        $hook = '            if (qaFolder != null)'
        if (!$original.Contains($hook)) { throw 'Baseline start hook changed.' }
        $patched = $original -replace '(?m)^            if \(qaFolder != null\)\r?$', @'
            var benchmark = Argument("-ctcc-benchmark");
            if (benchmark != null)
            {
                StartCoroutine(BenchmarkVisit(Path.GetFullPath(benchmark)));
                return;
            }
            if (qaFolder != null)
'@
        # Only the first if-block in Start opens a block on the next line in the pinned source.
        [IO.File]::WriteAllText($game, $patched)
    }
    foreach ($tree in 'Assets','Packages','ProjectSettings') {
        $destination = Join-Path $Stage $tree
        if ([IO.Path]::GetFullPath($destination) -ne "$Stage\$tree") { throw 'Unexpected mirror destination.' }
        & robocopy (Join-Path $source $tree) $destination /MIR /XJ /NFL /NDL /NJH /NJS /NP | Out-Null
        if ($LASTEXITCODE -ge 8) { throw "Staging failed for $tree." }
    }
    $version = (Get-Content -LiteralPath (Join-Path $Stage 'ProjectSettings/ProjectVersion.txt') -TotalCount 1).Split(' ')[1]
    $editor = Join-Path $env:USERPROFILE "Unity/Editors/$version/Editor/Unity.exe"
    if (!(Test-Path -LiteralPath $editor)) { $editor = "C:/Program Files/Unity/Hub/Editor/$version/Editor/Unity.exe" }
    if (!(Test-Path -LiteralPath $editor)) { throw "Cannot find Unity $version." }
    $exe = Join-Path $Output 'CaTrucCuoiCung.exe'
    $log = Join-Path $Output 'build.log'
    $flags = if ($Development) { ' -ctcc-development' } else { '' }
    $runner = Start-Process -FilePath $editor -WorkingDirectory $Stage -ArgumentList "-batchmode -nographics -quit -job-worker-count 4 -projectPath `"$Stage`" -executeMethod CTCC.Editor.HospitalAuthoring.Windows -ctcc-build `"$exe`"$flags -logFile `"$log`"" -WindowStyle Hidden -PassThru -Wait
    if ($runner.ExitCode -ne 0 -or !(Select-String -LiteralPath $log -Quiet -Pattern '^CTCC_BUILD ')) {
        Get-Content -LiteralPath $log -Tail 40
        throw "Windows build failed. See $log."
    }
    $revision = if ($Baseline) { & git -C $repo rev-parse $Baseline } else { & git -C $repo rev-parse HEAD }
    $status = if ($Baseline) { 'benchmark overlay only' } else { (& git -C $repo status --porcelain -- Prototype) -join ' | ' }
    $managed = Join-Path $Output 'CaTrucCuoiCung_Data/Managed/Assembly-CSharp.dll'
    @("revision=$revision", "prototype_diff=$status", "development=$Development", "unity=$version",
        "assembly_sha256=$((Get-FileHash -LiteralPath $managed).Hash.ToLowerInvariant())",
        "scene_sha256=$((Get-FileHash -LiteralPath (Join-Path $Stage 'Assets/CTCC/Scenes/AnSinh.unity')).Hash.ToLowerInvariant())",
        "benchmark_sha256=$((Get-FileHash -LiteralPath (Join-Path $Stage 'Assets/CTCC/Runtime/HospitalGame.Benchmark.cs')).Hash.ToLowerInvariant())") |
        Set-Content -LiteralPath (Join-Path $Output 'build-identity.txt')
    Get-Content -LiteralPath (Join-Path $Output 'build-identity.txt')
} finally {
    if ($temporary -and (Test-Path -LiteralPath $temporary) -and (Resolve-Path -LiteralPath $temporary).Path -eq "$Stage\baseline-source") {
        Remove-Item -LiteralPath $temporary -Recurse -Force
    }
}
