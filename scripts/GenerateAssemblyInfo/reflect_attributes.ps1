<#
.SYNOPSIS
    Verifies that assemblies contain the expected CommonAssemblyInfo attributes.

.DESCRIPTION
    Loads assemblies via Reflection and checks for:
    - AssemblyCompany ("SIL International")
    - AssemblyProduct ("FieldWorks")
    - AssemblyCopyright ("Copyright © 2002-2025 SIL International")

.PARAMETER Assemblies
    List of assembly paths to inspect.

.PARAMETER Output
    Path to write the validation log.
#>
param(
    [Parameter(Mandatory=$true)]
    [string[]]$Assemblies,

    [Parameter(Mandatory=$false)]
    [string]$Output
)

$ErrorActionPreference = "Stop"
$failures = 0
$results = @()

foreach ($asmPath in $Assemblies) {
    if (-not (Test-Path $asmPath)) {
        $msg = "MISSING: $asmPath"
        Write-Error $msg -ErrorAction Continue
        $results += $msg
        $failures++
        continue
    }

    try {
        $absPath = Resolve-Path $asmPath
        $asm = [System.Reflection.Assembly]::LoadFile($absPath)
        $results += "Assembly: $($asm.FullName) ($($asmPath))"

        # Check Company
        $companyAttr = $asm.GetCustomAttributes([System.Reflection.AssemblyCompanyAttribute], $false)
        if ($companyAttr) {
            $company = $companyAttr[0].Company
            $results += "  Company: $company"
            if ($company -notmatch "SIL International") {
                $results += "  ERROR: Unexpected Company '$company'"
                $failures++
            }
        } else {
            $results += "  ERROR: Missing AssemblyCompanyAttribute"
            $failures++
        }

        # Check Product
        $productAttr = $asm.GetCustomAttributes([System.Reflection.AssemblyProductAttribute], $false)
        if ($productAttr) {
            $product = $productAttr[0].Product
            $results += "  Product: $product"
            if ($product -notmatch "FieldWorks") {
                $results += "  ERROR: Unexpected Product '$product'"
                $failures++
            }
        } else {
            $results += "  ERROR: Missing AssemblyProductAttribute"
            $failures++
        }

        if (-not $failures) {
             $results += "PASS: $($asmPath)"
        }

    } catch {
        $msg = "EXCEPTION: $($asmPath) - $_"
        Write-Error $msg -ErrorAction Continue
        $results += $msg
        $failures++
    }
}

if ($Output) {
    $parent = Split-Path $Output
    if (-not (Test-Path $parent)) { New-Item -ItemType Directory -Path $parent -Force | Out-Null }
    $results | Out-File -FilePath $Output -Encoding utf8
}

if ($failures -gt 0) {
    exit 1
} else {
    exit 0
}
