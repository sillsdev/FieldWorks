---
last-reviewed: 2025-10-31
last-reviewed-tree: 5cde600285aadf3960755718098deb2f15e3d908a15a698cc9ad88ef61d5239f
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Controls Overview

## Purpose
Organizational parent folder containing shared UI controls library with reusable widgets and XML-driven views for FieldWorks applications.

## Subfolder Map
| Subfolder | Key Project | Notes |
|-----------|-------------|-------|
| Design | Design.csproj | Design-time components for IDE - [Design/COPILOT.md](Design/COPILOT.md) |
| DetailControls | DetailControls.csproj | Property editing controls - [DetailControls/COPILOT.md](DetailControls/COPILOT.md) |
| FwControls | FwControls.csproj | FieldWorks-specific controls - [FwControls/COPILOT.md](FwControls/COPILOT.md) |
| Widgets | Widgets.csproj | General-purpose widgets - [Widgets/COPILOT.md](Widgets/COPILOT.md) |
| XMLViews | XMLViews.csproj | XML-driven view composition - [XMLViews/COPILOT.md](XMLViews/COPILOT.md) |

## When Updating This Folder
1. Run `python .github/plan_copilot_updates.py --folders Src/Common/Controls`
2. Run `python .github/copilot_apply_updates.py --folders Src/Common/Controls`
3. Update subfolder tables if projects are added/removed
4. Run `python .github/check_copilot_docs.py --paths Src/Common/Controls/COPILOT.md`

## Related Guidance
- See `.github/AI_GOVERNANCE.md` for shared expectations and the COPILOT.md baseline
- Use the planner output (`.cache/copilot/diff-plan.json`) for the latest project and file references
- Trigger `.github/prompts/copilot-folder-review.prompt.md` after edits for an automated dry run
