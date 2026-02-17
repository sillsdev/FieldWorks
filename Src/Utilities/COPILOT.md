---
last-reviewed: 2025-11-21
last-reviewed-tree: df6e92d11431aa3b0f6927f91f8cf7479733e6936e68cf34a24824a1e9b0a730
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Utilities Overview

## Purpose
Organizational parent folder containing utility subfolders for data repair, enhanced dialogs, error reporting, SFM analysis/conversion, and XML helpers.

## Subfolder Map
| Subfolder | Key Project | Notes |
|-----------|-------------|-------|
| FixFwData | FixFwData.csproj | Data repair tool (WinExe) - [FixFwData/COPILOT.md](FixFwData/COPILOT.md) |
| FixFwDataDll | FixFwDataDll.csproj | Data repair library - [FixFwDataDll/COPILOT.md](FixFwDataDll/COPILOT.md) |
| MessageBoxExLib | MessageBoxExLib.csproj | Enhanced dialogs - [MessageBoxExLib/COPILOT.md](MessageBoxExLib/COPILOT.md) |
| Reporting | Reporting.csproj | Error reporting - [Reporting/COPILOT.md](Reporting/COPILOT.md) |
| SfmStats | SfmStats.csproj | SFM statistics tool - [SfmStats/COPILOT.md](SfmStats/COPILOT.md) |
| SfmToXml | Sfm2Xml.csproj, ConvertSFM.csproj | SFM→XML converter - [SfmToXml/COPILOT.md](SfmToXml/COPILOT.md) |
| XMLUtils | XMLUtils.csproj | XML utilities - [XMLUtils/COPILOT.md](XMLUtils/COPILOT.md) |

## When Updating This Folder
1. Run `python .github/plan_copilot_updates.py --folders Src/Utilities`
2. Run `python .github/copilot_apply_updates.py --folders Src/Utilities`
3. Update subfolder tables if projects are added/removed
4. Run `python .github/check_copilot_docs.py --paths Src/Utilities/COPILOT.md`

## Related Guidance
- Reference `.github/instructions/organizational-folders.instructions.md` for shared expectations
- Use the planner output (`.cache/copilot/diff-plan.json`) for the latest project and file references
- Trigger `.github/prompts/copilot-folder-review.prompt.md` after edits for an automated dry run
