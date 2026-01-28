<#
PowerShell wrapper for `py` shim.
Handles heredoc syntax tokens like `<<MARKER` by reading lines from stdin until the marker,
and pipes the collected input to `python -`.
Also falls back to `python` or `py` executables when available.
#>

param(
    [Parameter(ValueFromRemainingArguments=$true)]
    [string[]]$RemainingArgs
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-Python {
    $py = Get-Command python -ErrorAction SilentlyContinue
    if ($py) { return $py.Source }
    $py = Get-Command py -ErrorAction SilentlyContinue
    if ($py) { return $py.Source }
    return $null
}

# Detect heredoc token in args: an arg like '<<PY' or '<<\'PY\'' or '<<"PY"'
$heredocArg = $null
foreach ($a in $RemainingArgs) {
    if ($a -match '^<<') { $heredocArg = $a; break }
}

$python = Resolve-Python
if (-not $python) {
    Write-Error "Neither 'python' nor 'py' found on PATH. Install Python or adjust workspace shims."
    exit 1
}

if ($heredocArg) {
    $marker = $heredocArg -replace '^<<', ''
    # strip surrounding quotes if present
    if ($marker -match '^\'(.+)\'$') { $marker = $Matches[1] }
    if ($marker -match '^\"(.+)\"$') { $marker = $Matches[1] }

    Write-Host "Heredoc marker detected: '$marker' (enter lines, finish with a line equal to the marker)"
    $lines = New-Object System.Collections.Generic.List[string]
    while ($true) {
        try {
            $line = [Console]::In.ReadLine()
        } catch {
            break
        }
        if ($null -eq $line) { break }
        if ($line -eq $marker) { break }
        $lines.Add($line)
    }

    $inputText = $lines -join "`n"
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $python
    $psi.Arguments = "-"
    $psi.RedirectStandardInput = $true
    $psi.RedirectStandardOutput = $false
    $psi.RedirectStandardError = $false
    $psi.UseShellExecute = $false
    $p = [System.Diagnostics.Process]::Start($psi)
    $p.StandardInput.WriteLine($inputText)
    $p.StandardInput.Close()
    $p.WaitForExit()
    exit $p.ExitCode
}

# No heredoc: pass through to python (or py) with provided args
$argLine = ""
if ($RemainingArgs) { $argLine = [string]::Join(' ', $RemainingArgs) }
& $python $RemainingArgs
exit $LASTEXITCODE
