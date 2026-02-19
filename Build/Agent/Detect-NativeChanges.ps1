[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$EventName,
    [string]$PullRequestBaseRef,
    [string]$EventBefore,
    [string]$RepoRoot = ".",
    [string]$OutputPath = $env:GITHUB_OUTPUT,
    [string]$StepSummaryPath = $env:GITHUB_STEP_SUMMARY
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Push-Location $RepoRoot
try {
    $headRef = 'HEAD'

    if ($EventName -eq 'pull_request') {
        if ([string]::IsNullOrWhiteSpace($PullRequestBaseRef)) {
            throw 'PullRequestBaseRef is required when EventName is pull_request.'
        }

        git fetch --no-tags --prune --depth=1 origin $PullRequestBaseRef
        if ($LASTEXITCODE -ne 0) {
            throw "Failed to fetch base branch origin/$PullRequestBaseRef"
        }

        $baseRef = "origin/$PullRequestBaseRef"
        $mergeBase = (git merge-base $headRef $baseRef).Trim()
        if ([string]::IsNullOrWhiteSpace($mergeBase)) {
            throw "Failed to resolve merge-base between $headRef and $baseRef"
        }

        $diffRange = "$mergeBase..$headRef"
    }
    else {
        if (-not [string]::IsNullOrWhiteSpace($EventBefore) -and $EventBefore -ne '0000000000000000000000000000000000000000') {
            $diffRange = "$EventBefore..$headRef"
        }
        else {
            $diffRange = 'HEAD~1..HEAD'
        }
    }

    Write-Host "Detecting native changes in range: $diffRange" -ForegroundColor Cyan
    $changedFiles = @(git diff --name-only $diffRange)
    if ($LASTEXITCODE -ne 0) {
        throw "git diff failed for range $diffRange"
    }

    $nativePattern = '^(Src/.*\.(cpp|h|hpp|cc|ixx|def|vcxproj|vcxproj\.filters|mak)|Lib/src/unit\+\+/|Build/Src/NativeBuild/)'
    $nativeChangedFiles = @($changedFiles | Where-Object { $_ -match $nativePattern })
    $nativeChanged = $nativeChangedFiles.Count -gt 0
    $nativeChangedValue = if ($nativeChanged) { 'true' } else { 'false' }

    if (-not [string]::IsNullOrWhiteSpace($OutputPath)) {
        "native=$nativeChangedValue" | Add-Content -Path $OutputPath
    }

    if (-not [string]::IsNullOrWhiteSpace($StepSummaryPath)) {
        Add-Content -Path $StepSummaryPath -Value '### Native change detection'
        Add-Content -Path $StepSummaryPath -Value ''
        Add-Content -Path $StepSummaryPath -Value "Diff range: $diffRange"
        Add-Content -Path $StepSummaryPath -Value "Native changes detected: $nativeChangedValue"

        if ($nativeChangedFiles.Count -gt 0) {
            Add-Content -Path $StepSummaryPath -Value ''
            Add-Content -Path $StepSummaryPath -Value 'Native-changed files:'
            foreach ($file in $nativeChangedFiles) {
                Add-Content -Path $StepSummaryPath -Value "- $file"
            }
        }
    }
}
finally {
    Pop-Location
}
