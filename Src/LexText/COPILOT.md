---
last-reviewed: 2025-10-31
last-verified-commit: 404f639
status: draft
---

# LexText COPILOT summary

## Purpose
Organizational parent folder containing lexicon and text analysis components of FieldWorks Language Explorer (FLEx). Houses 11 subfolders covering lexicon management, interlinear text analysis, discourse charting, morphological parsing, and Pathway publishing integration. No direct source files; see individual subfolder COPILOT.md files for detailed documentation.

## Architecture
Container folder organizing related lexicon/text functionality into cohesive modules.

## Subfolders
- **Discourse/**: Discourse chart analysis tools (COPILOT.md)
- **FlexPathwayPlugin/**: Pathway publishing plugin integration (COPILOT.md)
- **Interlinear/**: Interlinear text analysis and glossing (COPILOT.md)
- **LexTextControls/**: Shared UI controls for lexicon/text features (COPILOT.md)
- **LexTextDll/**: Core lexicon/text business logic library (COPILOT.md)
- **LexTextExe/**: FLEx application entry point (COPILOT.md)
- **Lexicon/**: Lexicon editing and management UI (COPILOT.md)
- **Morphology/**: Morphological analysis tools (COPILOT.md)
- **ParserCore/**: Parser engine core logic (COPILOT.md)
- **ParserUI/**: Parser UI and configuration (COPILOT.md)
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

### Downstream (consumed by)
- **xWorks**: Main FLEx application shell
- **FLEx users**: Lexicon and text analysis features

## Related Folders
- **xWorks/**: Main application container
- **Common/**: Shared infrastructure
- **FdoUi**: Data object UI

## References
See individual subfolder COPILOT.md files:
- Discourse/COPILOT.md
- FlexPathwayPlugin/COPILOT.md
- Interlinear/COPILOT.md
- LexTextControls/COPILOT.md
- LexTextDll/COPILOT.md
- LexTextExe/COPILOT.md
- Lexicon/COPILOT.md
- Morphology/COPILOT.md
- ParserCore/COPILOT.md
- ParserUI/COPILOT.md
