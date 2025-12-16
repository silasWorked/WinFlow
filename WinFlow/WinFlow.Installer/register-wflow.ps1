# Registers .wflow file association for current user (HKCU)
# Run from a PowerShell prompt. No admin required.

$ErrorActionPreference = 'Stop'

# Resolve winflow CLI exe path (dev scenario)
$repoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$cliExe = Join-Path $repoRoot 'WinFlow.Cli\bin\Debug\net8.0\WinFlow.Cli.exe'

if (!(Test-Path $cliExe)) {
    Write-Host "CLI exe not found at $cliExe. Build the CLI first." -ForegroundColor Yellow
}

$progId = 'WinFlow.Script'

New-Item -Path HKCU:\Software\Classes\.wflow -Force | Out-Null
Set-ItemProperty -Path HKCU:\Software\Classes\.wflow -Name '(Default)' -Value $progId

New-Item -Path HKCU:\Software\Classes\$progId -Force | Out-Null
Set-ItemProperty -Path HKCU:\Software\Classes\$progId -Name '(Default)' -Value 'WinFlow Script'

New-Item -Path HKCU:\Software\Classes\$progId\DefaultIcon -Force | Out-Null
Set-ItemProperty -Path HKCU:\Software\Classes\$progId\DefaultIcon -Name '(Default)' -Value 'shell32.dll,70'

New-Item -Path HKCU:\Software\Classes\$progId\shell\open\command -Force | Out-Null

# Quote the path and pass "%1"
$cmd = '"' + $cliExe + '" "%1"'
Set-ItemProperty -Path HKCU:\Software\Classes\$progId\shell\open\command -Name '(Default)' -Value $cmd

Write-Host ".wflow is now associated to: $cmd" -ForegroundColor Green
