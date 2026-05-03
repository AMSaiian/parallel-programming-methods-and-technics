param(
    [Parameter(Mandatory)][ValidateSet("sharedmemory","namedpipe","tcp")][string]$Method,
    [int]$Threads = 12,
    [int]$Seed = 42,
    [switch]$Loud
)

$ScriptDir = $PSScriptRoot
$RunnerDir = Join-Path $ScriptDir "..\Runner"
$JavaDir = Join-Path $ScriptDir "..\Java"
$JavaScript = Join-Path $JavaDir "ipc-server.ps1"

$Param = switch ($Method.ToLower()) {
    "sharedmemory" { "ipc_shm.bin" }
    "namedpipe" { "ipc_cs_java" }
    "tcp" { "12500" }
}

$csArgs = @("run", "--project", $RunnerDir, "ipc-demo", "-t", $Threads, "-m", $Method, "-s", $Seed)
if ($Loud) { $csArgs += "-vs" }

$javaArgs = @("-NoProfile", "-ExecutionPolicy", "Bypass", "-File", $JavaScript,
              "-Method", $Method, "-Param", $Param)

if ($Method.ToLower() -eq "sharedmemory") {
    foreach ($dir in @($JavaDir, $RunnerDir)) {
        $stale = Join-Path $dir $Param
        if (Test-Path $stale) {
            Remove-Item $stale -Force
            Write-Host "  removed stale shared-memory file: $stale"
        }
    }
}

Write-Host "Starting C# (server) and Java (client); Java retries until C# is ready..."
Write-Host "  method: $Method  param: $Param"

$cs = Start-Process dotnet -ArgumentList $csArgs -PassThru -NoNewWindow -WorkingDirectory $RunnerDir
$java = Start-Process powershell -ArgumentList $javaArgs -PassThru -NoNewWindow -WorkingDirectory $JavaDir

$cs.WaitForExit()
$java.WaitForExit()

Write-Host "Both processes exited (C#: $($cs.ExitCode), Java: $($java.ExitCode))."
