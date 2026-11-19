param(
    [ValidateSet("greybox", "scene", "qa", "build", "all")]
    [string]$Step = "all",
    [string]$QaFolder = "pass1",
    [string]$RecordDir = "",
    [string]$Version = "6000.3.28f1"
)

$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $PSScriptRoot
# Unity 6 Bee/ILPP requires pure ASCII projectPath on Windows; prefer ASCII junction if mapped!
$asciiJunction = "C:\CTCC-Unity-Workspace"
$unityRoot = if (Test-Path -LiteralPath (Join-Path $asciiJunction "Prototype\ProjectSettings\ProjectVersion.txt")) { $asciiJunction } else { $root }
$proj = Join-Path $unityRoot "Prototype"
$ev = Join-Path $unityRoot "output\m0r-evidence"
$unity = Join-Path $env:USERPROFILE "Unity\Editors\$Version\Editor\Unity.exe"
New-Item -ItemType Directory -Path $ev -Force | Out-Null

function Invoke-UnityMethod {
    param([string]$Method, [string]$LogName, [string]$ExtraArgs = "", [switch]$UseGraphics, [switch]$NoQuit)
    $log = Join-Path $ev $LogName
    if (Test-Path -LiteralPath $log) { Remove-Item -LiteralPath $log -Force }
    $gfx = if ($UseGraphics) { "-force-d3d12 " } else { "-nographics " }
    $quit = if ($NoQuit) { "" } else { "-quit " }
    $argStr = "-batchmode $gfx$quit-job-worker-count 4 -projectPath `"$proj`" -executeMethod $Method $ExtraArgs -logFile `"$log`""
    Write-Output ">>> Running $Method (log: $LogName)..."
    $p = Start-Process -FilePath $unity -WorkingDirectory $proj -ArgumentList $argStr -WindowStyle Hidden -PassThru
    $p.WaitForExit()
    Write-Output "<<< $Method finished with ExitCode=$($p.ExitCode)"
    if (Test-Path -LiteralPath $log) {
        Select-String -LiteralPath $log -Pattern "CTCC_|error CS|Exception:|InvalidOperation|Compilation failed|Missing|Build Finished|Shader error" | Select-Object -Last 35 | ForEach-Object { Write-Output $_.Line }
    } else {
        Write-Output "WARNING: Log file was not created: $log"
    }
    if ($p.ExitCode -ne 0) {
        if (Test-Path -LiteralPath $log) {
            Write-Output "--- LAST 40 LINES OF $LogName ---"
            Get-Content -LiteralPath $log -Tail 40
        }
        throw "Unity failed on $Method (ExitCode=$($p.ExitCode))"
    }
}

if ($Step -eq "greybox" -or $Step -eq "all") {
    Invoke-UnityMethod -Method "CTCC.Editor.HospitalAuthoring.Greybox" -LogName "unity-greybox.log"
}
if ($Step -eq "scene" -or $Step -eq "all") {
    Invoke-UnityMethod -Method "CTCC.Editor.HospitalAuthoring.ReferenceScene" -LogName "unity-scene.log" -UseGraphics
}
if ($Step -eq "qa") {
    $qaPath = Join-Path $ev $QaFolder
    if (Test-Path -LiteralPath $qaPath) { Remove-Item -LiteralPath $qaPath -Recurse -Force }
    New-Item -ItemType Directory -Path $qaPath -Force | Out-Null
    $extra = "-ctcc-qa `"$qaPath`""
    if ($RecordDir -ne "") {
        $recPath = Join-Path $ev $RecordDir
        $extra += " -ctcc-record `"$recPath`""
    }
    Invoke-UnityMethod -Method "CTCC.Editor.HospitalAuthoring.TestScene" -LogName "unity-qa-$QaFolder.log" -ExtraArgs $extra -UseGraphics -NoQuit
    Write-Output "=== GAMEPLAY QA ==="
    if (Test-Path -LiteralPath (Join-Path $qaPath "gameplay.txt")) { Get-Content -LiteralPath (Join-Path $qaPath "gameplay.txt") }
}
if ($Step -eq "build" -or $Step -eq "all") {
    $buildProj = Join-Path $unityRoot "output\engine-spike\urp-comparison"
    if (Test-Path -LiteralPath (Join-Path $buildProj "ProjectSettings\ProjectVersion.txt")) {
        robocopy (Join-Path $proj "Assets\CTCC") (Join-Path $buildProj "Assets\CTCC") /MIR /NFL /NDL /NJH /NJS /NC /NS | Out-Null
        robocopy (Join-Path $proj "Assets\Settings") (Join-Path $buildProj "Assets\Settings") /MIR /NFL /NDL /NJH /NJS /NC /NS | Out-Null
        robocopy (Join-Path $proj "ProjectSettings") (Join-Path $buildProj "ProjectSettings") /MIR /NFL /NDL /NJH /NJS /NC /NS | Out-Null
        $proj = $buildProj
    }
    $outDir = Join-Path $unityRoot "output\CTCC-M0R"
    if (Test-Path -LiteralPath $outDir) { Remove-Item -LiteralPath $outDir -Recurse -Force }
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    $outExe = Join-Path $outDir "CaTrucCuoiCung.exe"
    Invoke-UnityMethod -Method "CTCC.Editor.HospitalAuthoring.Windows" -LogName "unity-build-final.log" -ExtraArgs "-ctcc-build `"$outExe`""
}
