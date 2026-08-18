<#
.SYNOPSIS
	Temporary probe. Every comment below trips comment-hygiene on purpose, so the
	advisory pull request comment has something to report.
#>

Set-StrictMode -Version Latest

# Phase 3 note: chooses `Name` when the flag is set | otherwise the abbreviation
function Test-ProbeProcessFraming {
	return $true
}

# design-notes.md D1: a doc pointer carrying a finding code, both of which are flagged
function Test-ProbeDocPointer {
	return $true
}

# This comment runs well past the two hundred character budget that an implementation
# comment gets in straight-line code, which is the whole point of it being here. It
# keeps talking long after it has stopped saying anything a reader would need, so the
# scan reports the whole run as one comment-too-long violation.
function Test-ProbeTooLong {
	return $true
}

# This one line is deliberately wider than the ninety-eight display columns that .editorconfig declares for every file.
function Test-ProbeLineTooLong {
	return $true
}
