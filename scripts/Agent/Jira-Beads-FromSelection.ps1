param(
    [string]$Jql = "assignee = currentUser() AND statusCategory != Done ORDER BY updated DESC",
    [string]$SelectionFile = "",
    [string]$OutputFile = ""
)

Set-StrictMode -Version Latest

if ((-not $PSBoundParameters.ContainsKey("Jql") -or -not $Jql) -and $env:JIRA_JQL) {
    $Jql = $env:JIRA_JQL
}

$repoRoot = Split-Path -Parent $PSScriptRoot
$repoRoot = Split-Path -Parent $repoRoot

$exportScript = Join-Path $repoRoot ".github\skills\jira-to-beads\scripts\export_jira_assigned.py"
$createScript = Join-Path $repoRoot ".github\skills\jira-to-beads\scripts\create_beads_from_jira.py"

$exportArgs = @($exportScript, "--jql", $Jql)
if ($OutputFile) {
    $exportArgs += @("--output", $OutputFile)
}
if ($SelectionFile) {
    $exportArgs += @("--selection-file", $SelectionFile)
}

Write-Output "Exporting Jira issues..."
& python @exportArgs
if ($LASTEXITCODE -ne 0) {
    throw "Export failed."
}

$selectionPath = $SelectionFile
if (-not $selectionPath) {
    $selectionPath = Join-Path $repoRoot ".cache\jira_assigned.selection.txt"
}

if (-not (Test-Path $selectionPath)) {
    throw "Selection file not found: $selectionPath"
}

$codeCmd = Get-Command code -ErrorAction SilentlyContinue
if ($codeCmd) {
    Write-Output "Opening selection file for editing..."
    & $codeCmd.Source --wait $selectionPath
} else {
    Write-Output "Open the selection file and uncomment the issues to create:"
    Write-Output $selectionPath
    Read-Host "Press Enter to continue when ready"
}

Write-Output "Creating beads from selection file..."
& python $createScript --select-file $selectionPath
if ($LASTEXITCODE -ne 0) {
    throw "Create beads failed."
}

Write-Output "Done."
