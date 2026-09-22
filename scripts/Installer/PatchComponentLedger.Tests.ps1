[CmdletBinding()]
param()

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
Import-Module (Join-Path $PSScriptRoot 'PatchComponentLedger.psm1') -Force

$script:AssertionCount = 0
$script:TestFiles = New-Object System.Collections.Generic.List[string]

function Assert-True {
    param([bool]$Condition, [string]$Message)
    $script:AssertionCount++
    if (-not $Condition) { throw $Message }
}

function Assert-Equal {
    param($Actual, $Expected, [string]$Message)
    $script:AssertionCount++
    if ($Actual -ne $Expected) {
        throw "$Message Expected: $Expected Actual: $Actual"
    }
}

function Assert-Contains {
    param([string]$Actual, [string]$Expected, [string]$Message)
    $script:AssertionCount++
    if (-not $Actual.Contains($Expected)) {
        throw "$Message Expected to find: $Expected Actual: $Actual"
    }
}

function Assert-Throws {
    param([scriptblock]$Action, [string]$ExpectedText)
    $script:AssertionCount++
    try {
        & $Action
    }
    catch {
        if ($_.Exception.Message.Contains($ExpectedText)) { return }
        throw "Expected an error containing '$ExpectedText', got '$($_.Exception.Message)'."
    }
    throw "Expected an error containing '$ExpectedText'."
}

function New-TestLedgerFile {
    param([string[]]$Lines)
    $path = [IO.Path]::GetTempFileName()
    Set-Content -LiteralPath $path -Value $Lines -Encoding Ascii
    $script:TestFiles.Add($path)
    return $path
}

try {
    $patch2758 = 'jobs/FieldWorks-Win-all-Release-Patch/2758/FieldWorks_9.3.12.2758_b1452_x64.msp'
    $patch2760 = 'jobs/FieldWorks-Win-all-Release-Patch/2760/FieldWorks_9.3.12.2760_b1452_x64.msp'
    $patchOtherBase = 'jobs/FieldWorks-Win-all-Release-Patch/2759/FieldWorks_9.3.12.2759_b1453_x64.msp'
    $ledger2760 = $patch2760 -replace '\.msp$', '_components.tsv'
    $selection = Select-PreviousPublishedPatch -PatchKeys @($patch2758, $patchOtherBase, $patch2760) -LedgerKeys @($ledger2760) -BaseBuildNumber '1452' -PatchVersion '9.3.12.2761'
    Assert-Equal "$($selection.PatchKey)|$($selection.LedgerKey)" "$patch2760|$ledger2760" 'The selector must choose the latest earlier patch and ledger on the requested base.'

    $unmatchedLedger = $patch2760 -replace '\.msp$', '_components.tsv'
    $bootstrap = Select-PreviousPublishedPatch -PatchKeys @($patch2758) -LedgerKeys @($unmatchedLedger) -BaseBuildNumber '1452' -PatchVersion '9.3.12.2761'
    Assert-Equal "$($bootstrap.PatchKey)|$($bootstrap.LedgerKey)" "$patch2758|" 'A ledger without its corresponding published patch must not be returned.'

    Assert-Throws {
        Select-PreviousPublishedPatch -PatchKeys @($patch2758, $patch2760) -LedgerKeys @($patch2758 -replace '\.msp$', '_components.tsv') -BaseBuildNumber '1452' -PatchVersion '9.3.12.2761'
    } 'Bootstrap has ended'

    $master = @{
        '{33333333-3333-3333-3333-333333333333}' = [pscustomobject]@{
            ComponentId = '{33333333-3333-3333-3333-333333333333}'
            Component   = 'cmpBase'
            File        = 'Base.dll'
            Feature     = 'Complete'
        }
    }
    Assert-Equal (Get-MsiDirectoryName -DefaultDir 'short|Long Name:SourceDir') 'Long Name' 'Directory parsing must use the target long name before the source portion.'
    Assert-Equal (Get-MsiDirectoryName -DefaultDir 'TargetDir:SourceDir') 'TargetDir' 'Directory parsing must retain a target name without a short name.'
    Assert-Equal (Get-MsiDirectoryName -DefaultDir '.') '' 'Dot directories must be transparent.'
    $directories = @{
        APPFOLDER = [pscustomobject]@{ Name = 'App'; Parent = '' }
        Help      = [pscustomobject]@{ Name = 'Help'; Parent = 'APPFOLDER' }
        Nested    = [pscustomobject]@{ Name = 'Nested'; Parent = 'Help' }
        Dot       = [pscustomobject]@{ Name = ''; Parent = 'Help' }
        Outside   = [pscustomobject]@{ Name = 'Outside'; Parent = '' }
    }
    Assert-Equal (Get-RelativeMsiFilePath -Directories $directories -DirectoryId 'APPFOLDER' -FileName 'Root.dll') 'Root.dll' 'Files directly under APPFOLDER must keep their basename.'
    Assert-Equal (Get-RelativeMsiFilePath -Directories $directories -DirectoryId 'Nested' -FileName 'Nested.dll') 'Help/Nested/Nested.dll' 'Nested files must keep their path below APPFOLDER.'
    Assert-Equal (Get-RelativeMsiFilePath -Directories $directories -DirectoryId 'Dot' -FileName 'Dot.dll') 'Help/Dot.dll' 'Dot directories must continue to their parent.'
    Assert-Equal (Get-RelativeMsiFilePath -Directories $directories -DirectoryId 'Outside' -FileName 'Outside.dll') $null 'Files outside APPFOLDER must be excluded.'

    $update = @{
        '{33333333-3333-3333-3333-333333333333}' = $master['{33333333-3333-3333-3333-333333333333}']
        '{44444444-4444-4444-4444-444444444444}' = [pscustomobject]@{
            ComponentId = '{44444444-4444-4444-4444-444444444444}'
            Component   = 'cmpNew'
            File        = 'New.dll'
            Feature     = 'Complete'
        }
        '{55555555-5555-5555-5555-555555555555}' = [pscustomobject]@{
            ComponentId = '{55555555-5555-5555-5555-555555555555}'
            Component   = 'cmpOther'
            File        = 'Other.dll'
            Feature     = 'Complete'
        }
    }
    $newLedgerEntries = @(Get-UpdateMinusBaseLedgerEntries -Master $master -Update $update)
    Assert-Equal (($newLedgerEntries | Sort-Object File | ForEach-Object File) -join ',') 'New.dll,Other.dll' 'The generated ledger must contain every current update-minus-base component.'
    $ledgerPath = New-TestLedgerFile
    Write-ComponentLedger -Path $ledgerPath -Entries $newLedgerEntries -Heading 'test'
    $writtenLines = Get-Content -LiteralPath $ledgerPath
    Assert-Equal $writtenLines[1] ('# ComponentId' + [char]9 + 'Component' + [char]9 + 'File' + [char]9 + 'Feature') 'Ledgers must contain only the four component fields.'
    $roundTrip = Read-ComponentLedger -Path $ledgerPath
    Assert-Equal (($roundTrip.Values | Sort-Object File | ForEach-Object File) -join ',') 'New.dll,Other.dll' 'The complete generated ledger must be readable.'

    $previous = [pscustomobject]@{
        ComponentId = '{66666666-6666-6666-6666-666666666666}'
        Component   = 'cmpPrevious'
        File        = 'Previous.dll'
        Feature     = 'Complete'
    }
    $required = @{}
    foreach ($component in $master.Values) { $required[$component.ComponentId] = $component }
    $required[$previous.ComponentId] = $previous
    $missing = @(Get-MissingComponents -Required $required -Available $update)
    Assert-Equal (($missing | Sort-Object File | ForEach-Object File) -join ',') 'Previous.dll' 'The comparison must detect the component missing from the previous-patch requirements.'

    $message = Format-DroppedComponentMessage -PatchVersion '9.3.12.2761' -BaseBuildNumber '1452' -Dropped @($previous)
    Assert-Contains $message 'preserve the relative output path when one exists' 'The remediation must describe relative paths without making a universal exact-path claim.'
    Assert-Contains $message '<RemovedSinceLastBase Include="$(dir-outputBase)/Previous.dll" />' 'The remediation must provide an actionable stand-in example.'

    $targetText = Get-Content -Raw (Join-Path $PSScriptRoot '..\..\Build\Installer.legacy.targets')
    $releaseFlag = '$' + '(FailOnRemovedSinceLastBase)'
    Assert-True ($targetText.Contains('<Warning') -and $targetText.Contains("'$releaseFlag' != 'true'")) 'A verification base build must warn about stand-ins.'
    Assert-True ($targetText.Contains('<Error') -and $targetText.Contains("'$releaseFlag' == 'true'")) 'A base release must fail while stand-ins remain.'

    Assert-Equal (Read-ComponentLedger -Path @()).Count 0 'A patch line with no S3 ledger must start with an empty previous-patch set.'
    $headerOnlyPath = New-TestLedgerFile @('# ComponentId' + [char]9 + 'Component' + [char]9 + 'File' + [char]9 + 'Feature')
    Assert-Equal (Read-ComponentLedger -Path $headerOnlyPath).Count 0 'A valid header-only ledger must be accepted.'
    $malformedPath = New-TestLedgerFile @(
        ('# ComponentId' + [char]9 + 'Component' + [char]9 + 'File' + [char]9 + 'Feature'),
        ('id' + [char]9 + 'component' + [char]9 + 'file')
    )
    Assert-Throws { Read-ComponentLedger -Path $malformedPath } 'Malformed component ledger row'
    $missingPath = Join-Path ([IO.Path]::GetTempPath()) "missing-ledger-$([guid]::NewGuid()).tsv"
    Assert-Throws { Read-ComponentLedger -Path $missingPath } 'Component ledger file not found'

    Write-Output "[OK] $script:AssertionCount patch-component-ledger assertions passed."
}
finally {
    foreach ($path in $script:TestFiles) {
        Remove-Item -LiteralPath $path -Force -ErrorAction SilentlyContinue
    }
}
