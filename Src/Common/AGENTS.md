---
last-reviewed: 2025-11-21
last-reviewed-tree: 4807ad69f2046ab660d562c93d6ce51aa6e901f1f80f02835c461cea12d547c0
status: draft
---
anchors:
  - change-log-auto
  - purpose
  - subfolder-map
  - when-updating-this-folder
  - related-guidance

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# Common Overview

## Purpose
Organizational parent folder containing cross-cutting utilities and shared infrastructure used throughout FieldWorks. Groups UI controls, application services, data filtering, framework components, utility functions, view site management, scripture utilities, UI adapter abstractions, and view interfaces. This folder provides building blocks for all FieldWorks applications.

## Subfolder Map
| Subfolder | Key Project | Notes |
|-----------|-------------|-------|
| Controls | Controls.csproj | Shared UI controls library - [Controls/AGENTS.md](Controls/AGENTS.md) |
| FieldWorks | FieldWorks.csproj | Core application infrastructure - [FieldWorks/AGENTS.md](FieldWorks/AGENTS.md) |
| Filters | Filters.csproj | Data filtering and sorting - [Filters/AGENTS.md](Filters/AGENTS.md) |
| Framework | Framework.csproj | Application framework components - [Framework/AGENTS.md](Framework/AGENTS.md) |
| FwUtils | FwUtils.csproj | General utility functions - [FwUtils/AGENTS.md](FwUtils/AGENTS.md) |
| RootSite | RootSite.csproj | Root-level site management - [RootSite/AGENTS.md](RootSite/AGENTS.md) |
| ScriptureUtils | ScriptureUtils.csproj | Scripture-specific utilities - [ScriptureUtils/AGENTS.md](ScriptureUtils/AGENTS.md) |
| SimpleRootSite | SimpleRootSite.csproj | Simplified root site API - [SimpleRootSite/AGENTS.md](SimpleRootSite/AGENTS.md) |
| UIAdapterInterfaces | UIAdapterInterfaces.csproj | UI adapter abstractions - [UIAdapterInterfaces/AGENTS.md](UIAdapterInterfaces/AGENTS.md) |
| ViewsInterfaces | ViewsInterfaces.csproj | View rendering interfaces - [ViewsInterfaces/AGENTS.md](ViewsInterfaces/AGENTS.md) |

## When Updating This Folder
1. Run `python .github/plan_copilot_updates.py --folders Src/Common`
2. Run `python .github/copilot_apply_updates.py --folders Src/Common`
3. Update subfolder tables if projects are added/removed
4. Run `python .github/check_copilot_docs.py --paths Src/Common/AGENTS.md`

## Related Guidance
- See `.github/AI_GOVERNANCE.md` for shared expectations and the AGENTS.md baseline
- Use the planner output (`.cache/copilot/diff-plan.json`) for the latest project and file references
- Trigger `.github/prompts/copilot-folder-review.prompt.md` after edits for an automated dry run

