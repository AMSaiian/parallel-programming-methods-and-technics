param(
    [Parameter(Mandatory)][string]$Method,
    [Parameter(Mandatory)][string]$Param
)

$ScriptDir = $PSScriptRoot
$SrcFile = Join-Path $ScriptDir "IpcServer.java"
$ClsFile = Join-Path $ScriptDir "IpcServer.class"

if (-not (Test-Path $ClsFile) -or
    (Get-Item $SrcFile).LastWriteTimeUtc -gt (Get-Item $ClsFile).LastWriteTimeUtc) {
    Write-Host "  Compiling IpcServer.java..."
    javac $SrcFile
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

java -cp $ScriptDir IpcServer $Method $Param
