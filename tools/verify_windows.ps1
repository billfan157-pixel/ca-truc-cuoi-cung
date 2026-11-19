param(
    [Parameter(Mandatory=$true)][string]$Executable,
    [Parameter(Mandatory=$true)][string]$Evidence
)

$ErrorActionPreference='Stop'
$ctccExe=(Resolve-Path -LiteralPath $Executable).Path
$ctccEvidence=[System.IO.Path]::GetFullPath($Evidence)
if(Test-Path -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt')) {
    throw 'Use a new evidence directory for a new verification run.'
}
New-Item -ItemType Directory -Path $ctccEvidence -Force | Out-Null
$ctccLog=Join-Path $ctccEvidence 'player.log'
$ctccArgs="-force-d3d12 -screen-fullscreen 0 -screen-width 1920 -screen-height 1080 -ctcc-qa `"$ctccEvidence`" -logFile `"$ctccLog`""
$ctccRun=Start-Process -FilePath $ctccExe -WorkingDirectory (Split-Path -Parent $ctccExe) -ArgumentList $ctccArgs -WindowStyle Hidden -PassThru
if(!$ctccRun.WaitForExit(240000)) { throw "Windows verification is still running. Check $ctccLog." }
if($ctccRun.ExitCode -ne 0){throw "Windows verification exit $($ctccRun.ExitCode). Check $ctccLog."}
foreach($file in 'gameplay.txt','profile.txt','room101-R.png','room101-B.png') {
    if(!(Test-Path -LiteralPath (Join-Path $ctccEvidence $file))){throw "Missing verification artifact $file."}
}
Get-Content -LiteralPath (Join-Path $ctccEvidence 'gameplay.txt'),(Join-Path $ctccEvidence 'profile.txt')
