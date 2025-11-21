---
last-reviewed: 2025-11-21
last-reviewed-tree: b5c173866485988d8044821e9c191a7d4cb529916ee3706b99a10ad83af2d895
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# LexText Overview

## Purpose
Organizational parent folder containing lexicon and text analysis components of FieldWorks Language Explorer (FLEx). Houses lexicon management, interlinear text analysis, discourse charting, morphological parsing, and Pathway publishing integration.

## Subfolder Map
| Subfolder | Key Project | Notes |
|-----------|-------------|-------|
| Discourse | Discourse.csproj | Discourse chart analysis - [Discourse/COPILOT.md](Discourse/COPILOT.md) |
| FlexPathwayPlugin | FlexPathwayPlugin.csproj | Pathway publishing integration - [FlexPathwayPlugin/COPILOT.md](FlexPathwayPlugin/COPILOT.md) |
| Interlinear | ITextDll.csproj | Interlinear text analysis - [Interlinear/COPILOT.md](Interlinear/COPILOT.md) |
| LexTextControls | LexTextControls.csproj | Shared UI controls - [LexTextControls/COPILOT.md](LexTextControls/COPILOT.md) |
| LexTextDll | LexTextDll.csproj | Core business logic - [LexTextDll/COPILOT.md](LexTextDll/COPILOT.md) |
| Lexicon | LexEdDll.csproj | Lexicon editor UI - [Lexicon/COPILOT.md](Lexicon/COPILOT.md) |
| Morphology | MorphologyEditorDll.csproj, MGA.csproj | Morphological analysis - [Morphology/COPILOT.md](Morphology/COPILOT.md) |
| ParserCore | ParserCore.csproj, XAmpleCOMWrapper.vcxproj | Parser engine - [ParserCore/COPILOT.md](ParserCore/COPILOT.md) |
| ParserUI | ParserUI.csproj | Parser UI - [ParserUI/COPILOT.md](ParserUI/COPILOT.md) |
| images | - | Shared image resources |

## When Updating This Folder
1. Run `python .github/plan_copilot_updates.py --folders Src/LexText`
2. Run `python .github/copilot_apply_updates.py --folders Src/LexText`
3. Update subfolder tables if projects are added/removed
4. Run `python .github/check_copilot_docs.py --paths Src/LexText/COPILOT.md`

## Related Guidance
- Reference `.github/instructions/organizational-folders.instructions.md` for shared expectations
- Use the planner output (`.cache/copilot/diff-plan.json`) for the latest project and file references
- Trigger `.github/prompts/copilot-folder-review.prompt.md` after edits for an automated dry run
