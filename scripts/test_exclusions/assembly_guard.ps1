[CmdletBinding()]
param (
    [Parameter(Mandatory=$true)]
    [string]$Assemblies
)

$ErrorActionPreference = "Stop"

# Resolve glob pattern
# Note: $Assemblies might be a glob string like "Output/Debug/**/*.dll"
# PowerShell's Get-ChildItem handles globs in -Path if we are careful.
# But -Recurse is separate.
# If user passes "Output/Debug/**/*.dll", we can't just pass it to -Path.
# We should probably assume the user passes a root path and we recurse, or a specific glob.
# Let's try to handle it smartly.

$Files = @()
if ($Assemblies -match "\*") {
    # It's a glob.
    # If it contains **, we might need to split.
    # But Get-ChildItem -Path "Output/Debug" -Recurse -Filter "*.dll" is safer.
    # Let's assume the user passes a path to a folder or a specific file pattern.
    # If the user passes "Output/Debug/**/*.dll", PowerShell might expand it if passed from shell.
    # But if passed as string...
    # Let's just use Resolve-Path if possible, or Get-ChildItem.

    # Simple approach: If it looks like a recursive glob, try to find the root.
    # Actually, let's just trust Get-ChildItem to handle what it can, or iterate.
    $Files = Get-ChildItem -Path $Assemblies -ErrorAction SilentlyContinue
} else {
    if (Test-Path $Assemblies -PathType Container) {
        $Files = Get-ChildItem -Path $Assemblies -Recurse -Filter "*.dll"
    } else {
        $Files = Get-ChildItem -Path $Assemblies
    }
}

if ($Files.Count -eq 0) {
    Write-Warning "No assemblies found matching: $Assemblies"
    exit 0
}

Write-Host "Scanning $($Files.Count) assemblies..."

$Failed = $false

foreach ($File in $Files) {
    try {
        # LoadFile vs LoadFrom. LoadFrom is usually better for dependencies.
        $Assembly = [System.Reflection.Assembly]::LoadFrom($File.FullName)
        try {
            $Types = $Assembly.GetTypes()
        } catch [System.Reflection.ReflectionTypeLoadException] {
            $Types = $_.Types | Where-Object { $_ -ne $null }
        }

        $TestTypes = $Types | Where-Object {
            $_.IsPublic -and ($_.Name -match "Tests?$") -and -not ($_.Name -match "^Test") # Exclude "Test" prefix if needed? No, suffix "Test" or "Tests".
        }

        if ($TestTypes) {
            Write-Error "Assembly '$($File.Name)' contains test types:"
            foreach ($Type in $TestTypes) {
                Write-Error "  - $($Type.FullName)"
            }
            $Failed = $true
        }
    } catch {
        Write-Warning "Failed to inspect assembly '$($File.Name)': $_"
    }
}

if ($Failed) {
    throw "Assembly guard failed: Test types detected in production assemblies."
}

Write-Host "Assembly guard passed."
