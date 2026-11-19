param(
    [Parameter(Mandatory)][string]$Executable,
    [Parameter(Mandatory)][string]$PresentMon,
    [Parameter(Mandatory)][string]$Evidence,
    [string]$Quality = 'Medium',
    [ValidateSet(0,1)][int]$VSync = 0,
    [ValidateSet('dual-probes','trilight','apv')][string]$GiMode = 'dual-probes',
    [ValidateSet('none','ssao','decals','shadows','probes','transparency','geometry')][string]$Ablation = 'none',
    [switch]$CharacterStress
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/focus_m1p_player.ps1"
$exe = (Resolve-Path -LiteralPath $Executable).Path
$captureExe = (Resolve-Path -LiteralPath $PresentMon).Path
$out = [IO.Path]::GetFullPath($Evidence)
if (Test-Path -LiteralPath $out) { throw "Use a new evidence directory for each run." }
New-Item -ItemType Directory -Path $out | Out-Null
$session = "CTCC-M2-$([Guid]::NewGuid().ToString('N').Substring(0,8))"
$capture = Start-Process -FilePath $captureExe -ArgumentList "--process_name $([IO.Path]::GetFileName($exe)) --session_name $session --no_console_stats --v2_metrics --qpc_time --output_file `"$out/presents.csv`" --terminate_on_proc_exit" -RedirectStandardError "$out/presentmon-error.txt" -RedirectStandardOutput "$out/presentmon-output.txt" -WindowStyle Hidden -PassThru
$player = $null
$logman = Join-Path $env:SystemRoot 'System32/logman.exe'
try {
    Start-Sleep -Milliseconds 600
    if ($capture.HasExited) { throw "PresentMon startup failed. See $out/presentmon-error.txt" }
    $launchQpc = [Diagnostics.Stopwatch]::GetTimestamp()
    $stressArgs = if ($CharacterStress) { '-ctcc-character-stress true' } else { '' }
    $player = Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -ArgumentList "-force-d3d12 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -ctcc-quality $Quality -ctcc-vsync $VSync -ctcc-gi $GiMode -ctcc-benchmark `"$out`" -ctcc-save `"$out/save`" -ctcc-ablation $Ablation $stressArgs -logFile `"$out/player.log`"" -PassThru
    Show-CtccPlayerWindow $player
    $deadline = [Diagnostics.Stopwatch]::StartNew()
    $processSamples = [Collections.Generic.List[string]]::new()
    $processSamples.Add('qpc,working_set_bytes,private_bytes,foreground')
    while (!$player.WaitForExit(500)) {
        $player.Refresh()
        $focused = [int]($player.MainWindowHandle -ne [IntPtr]::Zero -and
            [CtccPlayerWindow]::GetForegroundWindow() -eq $player.MainWindowHandle)
        $processSamples.Add("$([Diagnostics.Stopwatch]::GetTimestamp()),$($player.WorkingSet64),$($player.PrivateMemorySize64),$focused")
        if ($deadline.Elapsed.TotalSeconds -gt 180) { $player.Kill(); throw "Timed out: $out/player.log" }
    }
    $processSamples | Set-Content -LiteralPath "$out/process.csv"
    if ($player.ExitCode -ne 0) { throw "Benchmark player exited $($player.ExitCode). See $out/result.txt and player.log" }
    $managed = Join-Path (Split-Path -Parent $exe) "$([IO.Path]::GetFileNameWithoutExtension($exe))_Data/Managed/Assembly-CSharp.dll"
    "pid=$($player.Id)`nlaunch_qpc=$launchQpc`nassembly_sha256=$((Get-FileHash -LiteralPath $managed).Hash.ToLowerInvariant())`nexe_sha256=$((Get-FileHash -LiteralPath $exe).Hash.ToLowerInvariant())`nrequested_quality=$Quality`ngi_mode=$GiMode`nablation=$Ablation" | Set-Content -LiteralPath "$out/identity.txt"
    if (!$capture.WaitForExit(1500)) {
        & $logman stop $session -ets | Out-Null
        if (!$capture.WaitForExit(10000)) { throw "Could not finalize ETW session $session" }
    }
    if ($capture.ExitCode -ne 0) { throw "PresentMon exited $($capture.ExitCode): $out/presentmon-error.txt" }
    foreach ($artifact in 'frames.csv','actions.csv','presents.csv','result.txt') {
        if (!(Test-Path -LiteralPath "$out/$artifact") -or (Get-Item -LiteralPath "$out/$artifact").Length -eq 0) {
            throw "Missing capture artifact $artifact"
        }
    }
    if (!(Select-String -LiteralPath "$out/result.txt" -Quiet -Pattern '^passed=True$')) { throw 'The route failed its final state assertion.' }
    Get-Content -LiteralPath "$out/result.txt"
} finally {
    if ($capture -and !$capture.HasExited) {
        & $logman stop $session -ets 2>$null | Out-Null
        if (!$capture.WaitForExit(5000)) { $capture.Kill() }
    }
    if ($player -and !$player.HasExited) { $player.Kill() }
}
