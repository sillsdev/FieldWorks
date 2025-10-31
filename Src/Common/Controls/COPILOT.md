---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# Controls COPILOT summary

## Purpose
Organizational parent folder containing shared UI controls library providing reusable widgets and XML-based view components for FieldWorks applications. Groups together control design-time components (Design/), property editing controls (DetailControls/), FieldWorks-specific controls (FwControls/), general-purpose widgets (Widgets/), and XML-driven view composition (XMLViews/). These components enable consistent UI patterns across all FieldWorks applications and support complex data-driven interfaces through declarative XML specifications.

## Architecture
Organizational folder with 5 immediate subfolders. No source files directly in this folder - all code resides in subfolders. Each subfolder has its own COPILOT.md documenting its specific purpose, components, and dependencies.

## Key Components
This folder does not contain source files directly. See subfolder COPILOT.md files for specific components:
- **Design/**: Design-time components for Visual Studio/IDE support (custom designers for controls)
- **DetailControls/**: Property editing controls (slices, launchers, choosers for data editing)
- **FwControls/**: FieldWorks-specific UI controls (specialized controls for linguistic data)
- **Widgets/**: General-purpose reusable controls (buttons, panels, navigation, file dialogs)
- **XMLViews/**: XML-driven view composition system (BulkEditBar, XmlBrowseView, PartGenerator, LayoutFinder)

## Technology Stack
C# .NET WinForms with custom control development and XML-driven UI configuration. See individual subfolder COPILOT.md files for specific technologies.

## Dependencies

### Upstream (consumes)
Common upstream dependencies across subfolders:
- **Common/Framework/**: Application framework infrastructure
- **Common/ViewsInterfaces/**: View interfaces for rendering
- **Common/SimpleRootSite/**: Root site infrastructure for view hosting
- Windows Forms (System.Windows.Forms)

### Downstream (consumed by)
- **xWorks/**: Major consumer of Common controls for application UI
- **LexText/**: Uses Common controls for lexicon editing interfaces
- **FwCoreDlgs/**: Dialog system built on Common controls
- Any FieldWorks application requiring UI controls

## Interop & Contracts
Controls interact with native views layer via ViewsInterfaces. See individual subfolder COPILOT.md files for specific interop boundaries.

## Threading & Performance
UI components require UI thread marshaling. Threading models vary by subfolder - see individual COPILOT.md files.

## Config & Feature Flags
Configuration varies by subfolder. XML-driven view system (XMLViews) uses XML files for declarative UI configuration.

## Build Information
- No project file in this organizational folder; each subfolder has its own .csproj
- Build via top-level FW.sln (Visual Studio/MSBuild) or run: `bash ./agent-build-fw.sh`
- All subfolder projects are built as part of the main solution
- Test projects: DetailControlsTests/, FwControlsTests/, WidgetsTests/, XMLViewsTests/

## Interfaces and Data Models
See individual subfolder COPILOT.md files for interfaces and data models. This organizational folder does not define interfaces directly.

## Entry Points
See individual subfolder COPILOT.md files for entry points. Common/Controls subfolders provide libraries of reusable controls rather than executable entry points.

## Test Index
Multiple test projects across subfolders:
- **DetailControls/**: DetailControlsTests (property editing controls tests)
- **FwControls/**: FwControlsTests (FieldWorks-specific controls tests)
- **Widgets/**: WidgetsTests (general widgets tests)
- **XMLViews/**: XMLViewsTests (XML-driven view system tests)

Run tests via: `dotnet test` or Visual Studio Test Explorer

## Usage Hints
This is an organizational folder. For usage guidance, see individual subfolder COPILOT.md files:
- Design/COPILOT.md for design-time components
- DetailControls/COPILOT.md for property editing controls
- FwControls/COPILOT.md for FieldWorks-specific controls
- Widgets/COPILOT.md for general-purpose widgets
- XMLViews/COPILOT.md for XML-driven view composition

## Related Folders
- **Common/Framework/**: Application framework using these controls
- **Common/ViewsInterfaces/**: Interfaces implemented by controls
- **Common/SimpleRootSite/**: Root site infrastructure for view hosting
- **xWorks/**: Major consumer of Common controls
- **LexText/**: Uses Common controls for lexicon UI
- **FwCoreDlgs/**: Dialog system using Common controls

## References
- **Project files**: No project file in this organizational folder; see subfolder COPILOT.md files
- **Subfolders**: Design/, DetailControls/, FwControls/, Widgets/, XMLViews/
- **Documentation**: Each subfolder has its own COPILOT.md file with detailed documentation
- **Test projects**: DetailControlsTests/, FwControlsTests/, WidgetsTests/, XMLViewsTests/
