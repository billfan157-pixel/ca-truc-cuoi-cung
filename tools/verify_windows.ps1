param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$Evidence,
    [string]$RecordDir = "",
    [switch]$PlayerInput,
    [ValidateSet('module-failure','keyboard-failure','visual-failure')][string]$ProbeFailure
)

$ErrorActionPreference='Stop'
$ctccExe=(Resolve-Path -LiteralPath $Executable).Path
$ctccEvidence=[System.IO.Path]::GetFullPath($Evidence)
if(Test-Path -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt')) {
    throw 'Use a new evidence directory for a new verification run.'
}
New-Item -ItemType Directory -Path $ctccEvidence -Force | Out-Null
$ctccLog=Join-Path $ctccEvidence 'player.log'
$qaArgs = if ($PlayerInput) { "-ctcc-input-qa `"$ctccEvidence`" -ctcc-save `"$ctccEvidence/input-save`"" } else { "-ctcc-qa `"$ctccEvidence`"" }
$ctccArgs="-force-d3d12 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 $qaArgs -logFile `"$ctccLog`""
if ($ProbeFailure) { $ctccArgs += " -ctcc-qa-probe $ProbeFailure" }
if($RecordDir -ne "") {
    $recFull = [System.IO.Path]::GetFullPath($RecordDir)
    New-Item -ItemType Directory -Path $recFull -Force | Out-Null
    $ctccArgs += " -ctcc-record `"$recFull`""
}
$ctccRun=Start-Process -FilePath $ctccExe -WorkingDirectory (Split-Path -Parent $ctccExe) -ArgumentList $ctccArgs -PassThru
if ($PlayerInput) {
    $shell = New-Object -ComObject WScript.Shell
    for ($attempt = 0; $attempt -lt 10 -and !$ctccRun.HasExited; $attempt++) {
        Start-Sleep -Milliseconds 250
        if ($shell.AppActivate($ctccRun.Id)) { break }
    }
}
if(!$ctccRun.WaitForExit(180000)) { $ctccRun.Kill(); throw "Windows verification timed out. Check $ctccLog." }
if ($ProbeFailure -and $ctccRun.ExitCode -eq 1 -and (Select-String -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt') -Quiet -Pattern '^RESULT=FAIL$')) {
    "Verified $ProbeFailure propagated to the Windows process exit code (1)."
    return
}
if ($ProbeFailure) { throw "Failed to verify the injected failure. Exit $($ctccRun.ExitCode). Check $ctccLog." }
if($ctccRun.ExitCode -ne 0){throw "Windows verification exit $($ctccRun.ExitCode). Check $ctccLog."}
$required = if ($PlayerInput) { @('gameplay.txt') } else { @('gameplay.txt','room101-R.png','room101-B.png') }
foreach($file in $required) {
    if(!(Test-Path -LiteralPath (Join-Path $ctccEvidence $file))){throw "Missing verification artifact $file."}
}
if (!(Select-String -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt') -Quiet -Pattern '^RESULT=PASS$')) {
    throw 'No passing QA verdict in gameplay.txt.'
}
Get-Content -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt')
