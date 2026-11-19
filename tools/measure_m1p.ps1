param(
    [Parameter(Mandatory)][string]$Executable,
    [Parameter(Mandatory)][string]$PresentMon,
    [Parameter(Mandatory)][string]$Evidence,
    [string]$Quality = 'Medium',
    [ValidateSet(0,1)][int]$VSync = 0,
    [ValidateSet('none','ssao','decals','shadows','probes','transparency','geometry')][string]$Ablation = 'none'
)

$ErrorActionPreference = 'Stop'
$exe = (Resolve-Path -LiteralPath $Executable).Path
$captureExe = (Resolve-Path -LiteralPath $PresentMon).Path
$out = [IO.Path]::GetFullPath($Evidence)
if (Test-Path -LiteralPath $out) { throw "Evidence directory already exists: $out" }
New-Item -ItemType Directory -Path $out | Out-Null
$session = "CTCC-M1P-$([Guid]::NewGuid().ToString('N').Substring(0,8))"
$capture = Start-Process -FilePath $captureExe -ArgumentList "--process_name $([IO.Path]::GetFileName($exe)) --session_name $session --no_console_stats --v2_metrics --qpc_time --output_file `"$out/presents.csv`" --terminate_on_proc_exit" -RedirectStandardError "$out/presentmon-error.txt" -RedirectStandardOutput "$out/presentmon-output.txt" -WindowStyle Hidden -PassThru
$player = $null
$logman = Join-Path $env:SystemRoot 'System32/logman.exe'
try {
    Start-Sleep -Milliseconds 600
    if ($capture.HasExited) { throw "PresentMon startup failed. See $out/presentmon-error.txt" }
    $player = Start-Process -FilePath $exe -WorkingDirectory (Split-Path -Parent $exe) -ArgumentList "-force-d3d12 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -ctcc-quality $Quality -ctcc-vsync $VSync -ctcc-benchmark `"$out`" -ctcc-save `"$out/save`" -ctcc-ablation $Ablation -logFile `"$out/player.log`"" -PassThru
    $shell = New-Object -ComObject WScript.Shell
    for ($attempt = 0; $attempt -lt 10 -and !$player.HasExited; $attempt++) {
        Start-Sleep -Milliseconds 250
        if ($shell.AppActivate($player.Id)) { break }
    }
    if (!$player.WaitForExit(150000)) { $player.Kill(); throw "Timed out: $out/player.log" }
    if ($player.ExitCode -ne 0) { throw "Benchmark player exited $($player.ExitCode). See $out/result.txt and player.log" }
    $managed = Join-Path (Split-Path -Parent $exe) "$([IO.Path]::GetFileNameWithoutExtension($exe))_Data/Managed/Assembly-CSharp.dll"
    "pid=$($player.Id)`nassembly_sha256=$((Get-FileHash -LiteralPath $managed).Hash.ToLowerInvariant())`nexe_sha256=$((Get-FileHash -LiteralPath $exe).Hash.ToLowerInvariant())`nrequested_quality=$Quality`nablation=$Ablation" | Set-Content -LiteralPath "$out/identity.txt"
    if (!$capture.WaitForExit(1500)) {
        # On a non-elevated capture ETW can continue tracing after the target exits.
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
