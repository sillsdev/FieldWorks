[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$InputPath,

    [Parameter(Mandatory = $true)]
    [string]$OutputPath,

    [string]$BaseDirectory
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

function Resolve-IncludePath {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$CurrentDirectory
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        return $Path
    }

    return [System.IO.Path]::Combine($CurrentDirectory, $Path)
}

function Expand-File {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [hashtable]$Vars
    )

    $currentDir = Split-Path -Parent $Path
    if (-not (Test-Path -LiteralPath $Path)) {
        throw "Input/include file not found: $Path"
    }

    $outLines = New-Object System.Collections.Generic.List[string]

    foreach ($line in (Get-Content -LiteralPath $Path)) {
        $defineMatch = [regex]::Match($line, '^\s*<\?define\s+([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*\?>\s*$')
        if ($defineMatch.Success) {
            $name = $defineMatch.Groups[1].Value
            $value = $defineMatch.Groups[2].Value
            $Vars[$name] = $value
            continue
        }

        $includeMatch = [regex]::Match($line, '^\s*<\?include\s+(.+?)\s*\?>\s*$')
        if ($includeMatch.Success) {
            $raw = $includeMatch.Groups[1].Value.Trim()
            $inc = $raw.Trim('"', "'")
            $resolved = Resolve-IncludePath -Path $inc -CurrentDirectory $currentDir
            $expanded = Expand-File -Path $resolved -Vars $Vars
            $outLines.AddRange([string[]]$expanded)
            continue
        }

        $expandedLine = [regex]::Replace(
            $line,
            '\$\(var\.([A-Za-z_][A-Za-z0-9_]*)\)',
            {
                param($m)
                $key = $m.Groups[1].Value
                if ($Vars.ContainsKey($key)) { return $Vars[$key] }
                return $m.Value
            }
        )

        $outLines.Add($expandedLine)
    }

    return $outLines
}

$inputFullPath = $InputPath
if (-not [System.IO.Path]::IsPathRooted($inputFullPath)) {
    if ([string]::IsNullOrWhiteSpace($BaseDirectory)) {
        $BaseDirectory = (Get-Location).Path
    }
    $inputFullPath = [System.IO.Path]::Combine($BaseDirectory, $InputPath)
}

$outputFullPath = $OutputPath
if (-not [System.IO.Path]::IsPathRooted($outputFullPath)) {
    if ([string]::IsNullOrWhiteSpace($BaseDirectory)) {
        $BaseDirectory = (Get-Location).Path
    }
    $outputFullPath = [System.IO.Path]::Combine($BaseDirectory, $OutputPath)
}

$vars = @{}
$expandedLines = Expand-File -Path $inputFullPath -Vars $vars

$outDir = Split-Path -Parent $outputFullPath
if (-not (Test-Path -LiteralPath $outDir)) {
    [void](New-Item -ItemType Directory -Path $outDir -Force)
}

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines($outputFullPath, $expandedLines, $utf8NoBom)
Write-Output ("Wrote preprocessed output: {0}" -f $outputFullPath)
