param(
    [string]$RepoRoot = $PWD
)

$ErrorActionPreference = "Stop"

$LocalizationsDir = Join-Path $RepoRoot "Localizations"
$L10nsDir = Join-Path $LocalizationsDir "l10ns"
$DistFilesDir = Join-Path $RepoRoot "DistFiles"
$FwConfigDir = Join-Path $DistFilesDir "Language Explorer\Configuration"
$BuildToolsDir = Join-Path $RepoRoot "BuildTools"
$FwBuildTasksDll = Join-Path $BuildToolsDir "FwBuildTasks\Debug\FwBuildTasks.dll"

# Ensure FwBuildTasks is built
if (-not (Test-Path $FwBuildTasksDll)) {
    Write-Host "FwBuildTasks.dll not found. Building it..."
    & msbuild "$RepoRoot\Build\Tools.proj" /t:FwBuildTasks /p:Configuration=Debug
    if ($LASTEXITCODE -ne 0) { throw "Failed to build FwBuildTasks" }
}

# Load assembly for PoToXml
Add-Type -Path $FwBuildTasksDll

# Helper function to run PoToXml
function Run-PoToXml {
    param($PoFile, $StringsXml)
    # We can't easily instantiate a Task class in PS without loading MSBuild assemblies properly.
    # So we'll use a temporary MSBuild project.
    
    $projContent = @"
<Project xmlns='http://schemas.microsoft.com/developer/msbuild/2003'>
  <UsingTask TaskName='PoToXml' AssemblyFile='$FwBuildTasksDll' />
  <Target Name='Run'>
    <PoToXml PoFile='$PoFile' StringsXml='$StringsXml' />
  </Target>
</Project>
"@
    $projFile = Join-Path $env:TEMP "RunPoToXml.proj"
    Set-Content -Path $projFile -Value $projContent
    & msbuild $projFile /nologo /v:q
    if ($LASTEXITCODE -ne 0) { Write-Error "PoToXml failed for $PoFile" }
    Remove-Item $projFile -Force
}

# Create l10ns dir if missing
if (-not (Test-Path $L10nsDir)) { New-Item -ItemType Directory -Path $L10nsDir | Out-Null }

# Get strings-en.xml
$stringsEn = Join-Path $FwConfigDir "strings-en.xml"
if (-not (Test-Path $stringsEn)) { throw "strings-en.xml not found at $stringsEn" }

# Iterate PO files
$poFiles = Get-ChildItem -Path $LocalizationsDir -Filter "messages.*.po"
foreach ($po in $poFiles) {
    # Name is messages.fr.po -> fr
    $parts = $po.Name -split '\.'
    if ($parts.Count -lt 3) { continue }
    $locale = $parts[1]
    
    Write-Host "Processing $locale..."
    
    $localeDir = Join-Path $L10nsDir $locale
    if (-not (Test-Path $localeDir)) { New-Item -ItemType Directory -Path $localeDir | Out-Null }
    
    # 1. Copy PO
    Copy-Item $po.FullName -Destination (Join-Path $localeDir $po.Name) -Force
    
    # 2. Setup strings-LOCALE.xml
    $targetStrings = Join-Path $localeDir "strings-$locale.xml"
    Copy-Item $stringsEn -Destination $targetStrings -Force
    
    # 3. Update strings-LOCALE.xml
    Run-PoToXml -PoFile $po.FullName -StringsXml $targetStrings
    
    # 4. Handle LocalizedLists (copy directly to DistFiles/Templates instead of via l10ns)
    $listName = "LocalizedLists-$locale.xml"
    $sourceList = Join-Path $LocalizationsDir $listName
    if (Test-Path $sourceList) {
        $destList = Join-Path $DistFilesDir "Templates\$listName"
        Copy-Item $sourceList -Destination $destList -Force
        Write-Host "  Copied $listName to DistFiles/Templates"
    }
}

Write-Host "Localization setup complete. You can now run the app and select languages."
