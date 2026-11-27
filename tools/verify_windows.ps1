param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$Evidence,
    [string]$RecordDir = "",
    [switch]$PlayerInput,
    [switch]$S1Only,
    [ValidateSet('D3D12','D3D11')][string]$GraphicsApi = 'D3D12',
    [ValidateRange(30,900)][int]$TimeoutSeconds = 240,
    [ValidateSet('module-failure','keyboard-failure','visual-failure','escape-premature','char-mesh','char-track','char-interruption','invalid-reasoning','broken-progression','v36-t02-wrong-key','v36-vo-leak','v36-ex01-autosolve','v36-f101-unverified-link')][string]$ProbeFailure,
    [ValidateSet('Low','Medium','High')][string]$Quality = 'Medium'
)

$ErrorActionPreference='Stop'
. "$PSScriptRoot/focus_m1p_player.ps1"
if ($PlayerInput -and $ProbeFailure) { throw 'Failure probes belong to the module suite.' }
if ($S1Only -and !$PlayerInput) { throw 'S1Only requires PlayerInput.' }
$ctccExe=(Resolve-Path -LiteralPath $Executable).Path
$ctccEvidence=[System.IO.Path]::GetFullPath($Evidence)
if(Test-Path -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt')) {
    throw 'Use a new evidence directory for a new verification run.'
}
New-Item -ItemType Directory -Path $ctccEvidence -Force | Out-Null
$ctccLog=Join-Path $ctccEvidence 'player.log'
$qaArgs = if ($PlayerInput) { "-ctcc-input-qa `"$ctccEvidence`" -ctcc-save `"$ctccEvidence/input-save`"" } else { "-ctcc-qa `"$ctccEvidence`"" }
$apiArg = "-force-" + $GraphicsApi.ToLowerInvariant()
$ctccArgs="$apiArg -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -ctcc-quality $Quality $qaArgs -logFile `"$ctccLog`""
if ($ProbeFailure) { $ctccArgs += " -ctcc-qa-probe $ProbeFailure" }
if ($S1Only) { $ctccArgs += ' -ctcc-input-s1-only true' }
if($RecordDir -ne "") {
    $recFull = [System.IO.Path]::GetFullPath($RecordDir)
    New-Item -ItemType Directory -Path $recFull -Force | Out-Null
    $ctccArgs += " -ctcc-record `"$recFull`""
}
$ctccRun=Start-Process -FilePath $ctccExe -WorkingDirectory (Split-Path -Parent $ctccExe) -ArgumentList $ctccArgs -PassThru
if ($PlayerInput) {
    Show-CtccPlayerWindow $ctccRun -CalibrationClick
    $startupLimit = [DateTime]::UtcNow.AddSeconds(25)
    while ([DateTime]::UtcNow -lt $startupLimit -and !$ctccRun.WaitForExit(350)) {
        $ctccRun.Refresh()
        if ($ctccRun.MainWindowHandle -ne [IntPtr]::Zero -and
            [CtccPlayerWindow]::GetForegroundWindow() -ne $ctccRun.MainWindowHandle) {
            Show-CtccPlayerWindow $ctccRun -CalibrationClick
        }
    }
}
if(!$ctccRun.WaitForExit($TimeoutSeconds * 1000)) { $ctccRun.Kill(); throw "Windows verification timed out. Check $ctccLog." }
if ($ProbeFailure) {
    $verdict = Join-Path $ctccEvidence 'gameplay.txt'
    $expected = switch ($ProbeFailure) {
        'module-failure' { 'intentional QA module failure' }
        'keyboard-failure' { 'T06 keyboard: first=False second=True escaped=True alt=True' }
        'visual-failure' { 'intentional QA visual failure' }
        'escape-premature' { 'Failed escape publication retains live encounter and previous snapshot' }
        'char-mesh' { 'missing skinned component Lan_Eye_L_LOD1' }
        'char-track' { 'Invalid character track or stale audio/text: CH01-D03' }
        'char-interruption' { 'interrupted character voice did not release face, voice source and callback' }
        'invalid-reasoning' { 'CH01-R fault-injection guard: unsupported falsification overclaim must be rejected' }
        'broken-progression' { 'CH01-R fault-injection guard: Unverified must open G01 without blocking CH01 progression' }
        'v36-t02-wrong-key' { 'V36-CH01-Q01 KEY_WING_C rejected at outer gate (both resolver and Casework service) and retained' }
        'v36-vo-leak' { 'VO-SILENT-Q01 CH01-D01 renders as SilentThought with WCAG AA scrim and zero AudioBus usage' }
        'v36-ex01-autosolve' { 'V36-CH01-Q03 EX01 never auto-solves when all 3 sources are discovered' }
        'v36-f101-unverified-link' { 'V36-CH01-Q02 backend rejects unverified F101 link without SpatialFieldVerified provenance' }
    }
    if ($ctccRun.ExitCode -ne 1 -or !(Test-Path -LiteralPath $verdict) -or
        !(Select-String -LiteralPath $verdict -Quiet -Pattern '^RESULT=FAIL$') -or
        !(Select-String -LiteralPath $verdict -Quiet -SimpleMatch $expected)) {
        throw "Failed to verify the injected failure. Exit $($ctccRun.ExitCode). Check $ctccLog."
    }
    "Verified $ProbeFailure propagated to the Windows process exit code (1)."
    return
}
if($ctccRun.ExitCode -ne 0){throw "Windows verification exit $($ctccRun.ExitCode). Check $ctccLog."}
$required = if ($PlayerInput) { @('gameplay.txt') } else { @('gameplay.txt','room101-R.png','room101-B.png') }
foreach($file in $required) {
    if(!(Test-Path -LiteralPath (Join-Path $ctccEvidence $file))){throw "Missing verification artifact $file."}
}
if (!(Select-String -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt') -Quiet -Pattern '^RESULT=PASS$')) {
    throw 'No passing QA verdict in gameplay.txt.'
}
if ($S1Only -and !(Select-String -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt') -Quiet -SimpleMatch 'V33-Q05 INPUT S1 PASS')) {
    throw 'Targeted Player Input QA did not complete the S1 round trip.'
}
Get-Content -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt')
