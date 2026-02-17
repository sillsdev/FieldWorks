<#
.SYNOPSIS
    Reads file content with built-in head/tail limiting.

.DESCRIPTION
    Alternative to "Get-Content file | Select-Object -First N" that auto-approves.
    Supports reading from beginning, end, or both, with optional line numbers.

.PARAMETER Path
    Path to the file to read.

.PARAMETER HeadLines
    Number of lines to show from the beginning. Default: 0 (all)

.PARAMETER TailLines
    Number of lines to show from the end. Default: 0 (all)

.PARAMETER LineNumbers
    If specified, prefix each line with its line number.

.PARAMETER Pattern
    Optional regex pattern to filter lines (like Select-String but simpler output).

.EXAMPLE
    .\Read-FileContent.ps1 -Path "src/file.cs" -HeadLines 50

.EXAMPLE
    .\Read-FileContent.ps1 -Path "build.log" -TailLines 100

.EXAMPLE
    .\Read-FileContent.ps1 -Path "src/file.cs" -HeadLines 20 -TailLines 20

.EXAMPLE
    .\Read-FileContent.ps1 -Path "src/file.cs" -Pattern "class\s+\w+" -LineNumbers
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Path,

    [int]$HeadLines = 0,

    [int]$TailLines = 0,

    [switch]$LineNumbers,

    [string]$Pattern
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Path)) {
    throw "File not found: $Path"
}

$lines = Get-Content -Path $Path -Encoding UTF8
$total = $lines.Count

# Apply pattern filter if specified
if ($Pattern) {
    $filtered = @()
    $lineNums = @()
    for ($i = 0; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match $Pattern) {
            $filtered += $lines[$i]
            $lineNums += ($i + 1)
        }
    }
    $lines = $filtered
    $originalLineNums = $lineNums
}

function Format-Line {
    param([string]$Line, [int]$Number)
    if ($LineNumbers) {
        return "{0,5}: {1}" -f $Number, $Line
    }
    return $Line
}

# Determine what to output
if ($HeadLines -gt 0 -and $TailLines -gt 0) {
    # Show head and tail
    if ($lines.Count -le ($HeadLines + $TailLines)) {
        for ($i = 0; $i -lt $lines.Count; $i++) {
            $num = if ($Pattern) { $originalLineNums[$i] } else { $i + 1 }
            Write-Output (Format-Line -Line $lines[$i] -Number $num)
        }
    } else {
        # Head
        for ($i = 0; $i -lt $HeadLines; $i++) {
            $num = if ($Pattern) { $originalLineNums[$i] } else { $i + 1 }
            Write-Output (Format-Line -Line $lines[$i] -Number $num)
        }
        Write-Output "... ($($lines.Count - $HeadLines - $TailLines) lines omitted) ..."
        # Tail
        $startIdx = $lines.Count - $TailLines
        for ($i = $startIdx; $i -lt $lines.Count; $i++) {
            $num = if ($Pattern) { $originalLineNums[$i] } else { $i + 1 }
            Write-Output (Format-Line -Line $lines[$i] -Number $num)
        }
    }
}
elseif ($HeadLines -gt 0) {
    $showCount = [Math]::Min($HeadLines, $lines.Count)
    for ($i = 0; $i -lt $showCount; $i++) {
        $num = if ($Pattern) { $originalLineNums[$i] } else { $i + 1 }
        Write-Output (Format-Line -Line $lines[$i] -Number $num)
    }
    if ($lines.Count -gt $HeadLines) {
        Write-Output "... ($($lines.Count - $HeadLines) more lines) ..."
    }
}
elseif ($TailLines -gt 0) {
    if ($lines.Count -gt $TailLines) {
        Write-Output "... ($($lines.Count - $TailLines) lines omitted) ..."
    }
    $startIdx = [Math]::Max(0, $lines.Count - $TailLines)
    for ($i = $startIdx; $i -lt $lines.Count; $i++) {
        $num = if ($Pattern) { $originalLineNums[$i] } else { $i + 1 }
        Write-Output (Format-Line -Line $lines[$i] -Number $num)
    }
}
else {
    # Show all
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $num = if ($Pattern) { $originalLineNums[$i] } else { $i + 1 }
        Write-Output (Format-Line -Line $lines[$i] -Number $num)
    }
}

# Summary
if ($Pattern) {
    Write-Host ""
    Write-Host "Found $($lines.Count) matching lines out of $total total" -ForegroundColor Gray
}
