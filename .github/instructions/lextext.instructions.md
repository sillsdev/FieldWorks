---
applyTo: "Src/LexText/**"
name: "lextext.instructions"
description: "Auto-generated concise instructions from COPILOT.md for LexText"
---

# LexText (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **Discourse/**: Discourse chart analysis (Discourse.csproj)
- **FlexPathwayPlugin/**: Pathway publishing integration (FlexPathwayPlugin.csproj)
- **Interlinear/**: Interlinear text analysis (ITextDll.csproj)
- **LexTextControls/**: Shared UI controls (LexTextControls.csproj)
- **LexTextDll/**: Core business logic (LexTextDll.csproj)
- **LexTextExe/**: Application entry point (LexTextExe.csproj)

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: c399812b4465460b9d8163ce5e2d1dfee7116f679fa3ec0a64c6ceb477091ed8
status: draft
---

# LexText COPILOT summary

## Purpose
Organizational parent folder containing lexicon and text analysis components of FieldWorks Language Explorer (FLEx). Houses 11 subfolders covering lexicon management, interlinear text analysis, discourse charting, morphological parsing, and Pathway publishing integration. No direct source files; see individual subfolder COPILOT.md files for detailed documentation.

## Architecture
Container folder organizing related lexicon/text functionality into cohesive modules.

## Key Components
This is an organizational parent folder. Key components are in the subfolders:
- **Discourse/**: Discourse chart analysis (Discourse.csproj)
- **FlexPathwayPlugin/**: Pathway publishing integration (FlexPathwayPlugin.csproj)
- **Interlinear/**: Interlinear text analysis (ITextDll.csproj)
- **LexTextControls/**: Shared UI controls (LexTextControls.csproj)
- **LexTextDll/**: Core business logic (LexTextDll.csproj)
- **LexTextExe/**: Application entry point (LexTextExe.csproj)
- **Lexicon/**: Lexicon editor UI (LexEdDll.csproj)
- **Morphology/**: Morphological analysis (MorphologyEditorDll.csproj, MGA.csproj)
- **ParserCore/**: Parser engine (ParserCore.csproj, XAmpleCOMWrapper.vcxproj)
- **ParserUI/**: Parser UI (ParserUI.csproj)
- **images/**: Shared image resources

## Technology Stack
See individual subfolder COPILOT.md files.

## Dependencies

### Upstream (subfolders consume)
- **LCModel**: Data model
- **Common/**: Shared FW infrastructure
- **XCore**: Application framework
- **FdoUi**: Data object UI
- **FwCoreDlgs**: Common dialogs
