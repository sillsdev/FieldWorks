---
last-reviewed: 2025-10-31
last-reviewed-tree: 4f41c46ca278de62d2a4c3c39279468da088607063910aa2f6c8f6c1e03ee901
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# FwResources COPILOT summary

## Purpose
Centralized resource management for FieldWorks applications. Shared images, icons, localized strings, and UI assets used across xWorks, LexText, and other FieldWorks components. ResourceHelper utility class provides file filter specifications, resource string access, and image loading. Localized string resources: FwStrings (general strings), FwTMStrings (task management strings), HelpTopicPaths (help system paths), ToolBarSystemStrings (toolbar text). Images organized by category (Edit/, File/, Format/, Help/, Tools/, View/, Window/). SearchingAnimation for progress indicators. FwFileExtensions defines standard file extensions. Essential for consistent UI appearance and localization across FieldWorks.

## Architecture
C# class library (.NET Framework 4.8.x) with embedded resources. Resource files (.resx) with auto-generated Designer.cs classes for type-safe access. Images/ folder organized by UI category (Edit, File, Format, Help, Tools, View, Window). ResourceHelper main utility class with FileFilterType enum for standardized file dialog filters. Extensive localization support via .resx files. 7458 lines of C# code plus extensive resource files.

## Key Components
- **ResourceHelper** class (ResourceHelper.cs, 32K lines): Resource access utilities
  - FileFilterType enum: Standardized file type filters (AllFiles, XML, Text, PDF, LIFT, etc.)
  - FileFilter() method: Generate file dialog filter strings
  - Resource string access methods
  - Image loading utilities
- **FwFileExtensions** (FwFileExtensions.cs): Standard file extension constants
  - Defines .fwdata, .fwbackup, and other FW extensions
- **FwStrings** (FwStrings.Designer.cs/.resx): General localized strings (110K lines Designer.cs, 69K .resx)
  - Comprehensive string resources for FieldWorks UI
- **FwTMStrings** (FwTMStrings.Designer.cs/.resx): Task management strings (47K lines Designer.cs, 37K .resx)
- **HelpTopicPaths** (HelpTopicPaths.Designer.cs/.resx): Help system topic paths (28K lines Designer.cs, 22K .resx)
- **ToolBarSystemStrings** (ToolBarSystemStrings.Designer.cs/.resx): Toolbar text resources (17K lines Designer.cs)
- **Images** (Images.Designer.cs/.resx): Image resource access (4K lines)
  - Images/ folder: Icon and image files organized by category
    - Edit/: Edit operation icons
    - File/: File operation icons
    - Format/: Formatting icons
    - Help/: Help system icons
    - Tools/: Tools menu icons
    - View/: View menu icons
    - Window/: Window management icons
- **ResourceHelperImpl** (ResourceHelperImpl.cs/.Designer.cs/.resx): Resource helper implementation (104K .resx)
- **SearchingAnimation** (SearchingAnimation.cs/.Designer.cs/.resx): Animated search progress indicator

## Technology Stack
- C# .NET Framework 4.8.x (net8)

## Dependencies
- Upstream: .NET resource management
- Downstream: xWorks, LexText, FwCoreDlgs, etc.

## Interop & Contracts
- **FileFilterType enum**: Standard contract for file dialog filters

## Threading & Performance
- **Static resources**: Loaded on demand and cached by .NET resource manager

## Config & Feature Flags
- **Localization**: .resx files for different cultures

## Build Information
- **Project file**: FwResources.csproj (net48, OutputType=Library)

## Interfaces and Data Models
ResourceHelper, FwStrings, FwTMStrings, HelpTopicPaths, Images.

## Entry Points
Referenced as library by all FieldWorks components. Resources accessed via static Designer classes.

## Test Index
No dedicated test project (resource library). Tested via consuming applications.

## Usage Hints
- Access strings: FwStrings.ResourceStringName

## Related Folders
- **All FieldWorks applications**: Consume FwResources

## References
See `.cache/copilot/diff-plan.json` for file details.
