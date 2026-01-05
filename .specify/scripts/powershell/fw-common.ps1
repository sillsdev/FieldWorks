#!/usr/bin/env pwsh
# FieldWorks spec-kit loader
# Use this instead of directly sourcing common.ps1 to get FW extensions
#
# Usage in other scripts:
#   . "$PSScriptRoot/fw-common.ps1"
#
# This will load:
#   1. Upstream common.ps1 (from spec-kit)
#   2. fw-extensions.ps1 (FieldWorks customizations)

$scriptDir = $PSScriptRoot

# Load upstream spec-kit common functions
. "$scriptDir/common.ps1"

# Apply FieldWorks extensions (overrides some functions)
. "$scriptDir/fw-extensions.ps1"
