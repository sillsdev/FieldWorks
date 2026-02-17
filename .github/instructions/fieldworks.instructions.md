---
applyTo: "Src/Common/FieldWorks/**"
name: "fieldworks.instructions"
description: "Auto-generated concise instructions from COPILOT.md for FieldWorks"
---

# FieldWorks (Concise)

## Purpose & Scope
Summarized key points from COPILOT.md

## Key Rules
- **FieldWorks** class (FieldWorks.cs): Main application singleton
- Manages application lifecycle and FwApp instances
- Handles project selection, opening, and closing
- Coordinates window creation and management
- Provides LcmCache access
- Main entry point: OutputType=WinExe

## Example (from summary)

---
last-reviewed: 2025-10-31
last-reviewed-tree: 02736fd2ef91849ac9b0a8f07f2043ff2e3099bbab520031f8b27b4fe42a33cf
status: draft
---

# FieldWorks COPILOT summary

## Purpose
Core FieldWorks-specific application infrastructure and utilities providing fundamental application services. Includes project management (FieldWorksManager interface, ProjectId for project identification), settings management (FwRestoreProjectSettings for backup restoration), application startup coordination (WelcomeToFieldWorksDlg, FieldWorks main class), busy state handling (ApplicationBusyDialog), Windows installer querying (WindowsInstallerQuery), remote request handling (RemoteRequest), lexical service provider integration (ILexicalProvider, LexicalServiceProvider), and Phonology Assistant integration objects (PaObjects/ namespace). Central to coordinating application lifecycle, managing shared resources, and enabling interoperability across FieldWorks applications.

## Architecture
C# Windows executable (WinExe) targeting .NET Framework 4.8.x. Main entry point for FieldWorks application launcher. Contains FieldWorks singleton class managing application lifecycle, project opening/closing, and window management. Includes three specialized namespaces: LexicalProvider/ for lexicon service integration, PaObjects/ for Phonology Assistant data transfer objects, and main SIL.FieldWorks namespace for core infrastructure. Test project (FieldWorksTests) provides unit tests for project ID, PA objects, and welcome dialog.

## Key Components
- **FieldWorks** class (FieldWorks.cs): Main application singleton
  - Manages application lifecycle and FwApp instances
  - Handles project selection, opening, and closing
  - Coordinates window creation and management
  - Provides LcmCache access
  - Main entry point: OutputType=WinExe
- **FieldWorksManager** class (FieldWorksManager.cs): IFieldWorksManager implementation
  - Pass-through facade ensuring single FieldWorks instance per process
  - `Cache` propert
