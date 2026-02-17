---
applyTo: "Src/Paratext8Plugin/**"
name: "paratext8plugin.instructions"
description: "Auto-generated concise instructions from COPILOT.md for Paratext8Plugin"
---

# Paratext8Plugin (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **Paratext8Provider**: Implements IScriptureProvider via MEF [Export]. Wraps Paratext.Data API: ScrTextCollection for project enumeration, ParatextData.Initialize() for SDK setup, Alert.Implementation = ParatextAlert() for alert bridging. Provides project filtering (NonEditableTexts, ScrTextNames), scripture text wrapping (ScrTexts() → PT8ScrTextWrapper), verse reference creation (MakeVerseRef() → PT8VerseRefWrapper), parser state (GetParserState() → PT8ParserStateWrapper).
- Properties:
- SettingsDirectory: Paratext settings folder (ScrTextCollection.SettingsDirectory)
- NonEditableTexts: Resource/inaccessible projects
- ScrTextNames: All accessible projects
- MaximumSupportedVersion: Paratext version installed

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: 68897dd419050f2e9c0f59ed91a75f5770ebd5aef2a9185ea42583a6d9d208d9
status: reviewed
---

# Paratext8Plugin

## Purpose
Paratext 8 integration adapter implementing IScriptureProvider interface for FLEx↔Paratext data interchange. Wraps Paratext.Data API (Paratext SDK v8) to provide FieldWorks-compatible scripture project access, verse reference handling (PT8VerseRefWrapper), text data wrappers (PT8ScrTextWrapper), and USFM parser state (PT8ParserStateWrapper). Enables Send/Receive synchronization between FLEx back translations and Paratext translation projects, supporting collaborative translation workflows where linguistic analysis (FLEx) informs translation (Paratext8) and vice versa.

## Architecture
C# library (net48) with 7 source files (~546 lines). Implements MEF-based plugin pattern via [Export(typeof(IScriptureProvider))] attribute with [ExportMetadata("Version", "8")] for Paratext 8 API versioning. Wraps Paratext.Data types (ScrText, VerseRef, ScrParserState) with FLEx-compatible interfaces.

## Key Components

### Paratext Provider
- **Paratext8Provider**: Implements IScriptureProvider via MEF [Export]. Wraps Paratext.Data API: ScrTextCollection for project enumeration, ParatextData.Initialize() for SDK setup, Alert.Implementation = ParatextAlert() for alert bridging. Provides project filtering (NonEditableTexts, ScrTextNames), scripture text wrapping (ScrTexts() → PT8ScrTextWrapper), verse reference creation (MakeVerseRef() → PT8VerseRefWrapper), parser state (GetParserState() → PT8ParserStateWrapper).
  - Properties:
    - SettingsDirectory: Paratext settings folder (ScrTextCollection.SettingsDirectory)
    - NonEditableTexts: Resource/inaccessible projects
    - ScrTextNames: All accessible projects
    - MaximumSupportedVersion: Paratext version installed
    - IsInstalled: Checks ParatextInfo.IsParatextInstalled
  - Methods:
    - Initialize(): Sets up ParatextData SDK and alert system
    - RefreshScrTex
