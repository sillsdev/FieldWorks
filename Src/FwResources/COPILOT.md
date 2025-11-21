---
last-reviewed: 2025-10-31
last-reviewed-tree: b7e0ecd2b293fa48143b5bf53150c7b689b9b3cf0f985bf769af6e039d621bd6
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

- Snapshot: HEAD~1
- Risk: none
- Files: 0 (code=0, tests=0, resources=0)

### Prompt seeds
- Update COPILOT.md for Src/FwResources. Prioritize Purpose/Architecture sections using planner data.
- Highlight API or UI updates, then confirm Usage/Test sections reflect 0 files changed (code=0, tests=0, resources=0); risk=none.
- Finish with verification notes and TODOs for manual testing.
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
- OutputType: Library
- Resource files (.resx) for localization
- Embedded resources for images/icons
- System.Resources for resource management

## Dependencies

### Upstream (consumes)
- **System.Resources**: .NET resource management
- **System.Drawing**: Image/Icon loading
- **SIL.LCModel.Utils**: Utility classes
- Minimal dependencies (resource library)

### Downstream (consumed by)
- **All FieldWorks applications**: xWorks, LexText, FwCoreDlgs, etc.
- **UI components**: Reference FwResources for strings and images
- **Help system**: Uses HelpTopicPaths
- Universal dependency across FieldWorks

## Interop & Contracts
- **FileFilterType enum**: Standard contract for file dialog filters
- **Resource classes**: Type-safe access to strings and images via Designer.cs classes
- **.resx format**: Standard .NET resource format for localization

## Threading & Performance
- **Static resources**: Loaded on demand and cached by .NET resource manager
- **Thread-safe**: .NET ResourceManager is thread-safe
- **Performance**: Efficient resource lookup; images cached after first load

## Config & Feature Flags
- **Localization**: .resx files for different cultures
- **FileFilterType**: Extensible enum for new file types
- No runtime configuration; compile-time resource embedding

## Build Information
- **Project file**: FwResources.csproj (net48, OutputType=Library)
- **Output**: FwResources.dll (embedded resources)
- **Build**: Via top-level FieldWorks.sln
- **Localization**: .resx files compiled into satellite assemblies for different cultures

## Interfaces and Data Models

- **ResourceHelper** (ResourceHelper.cs)
  - Purpose: Utility class for resource access and file filters
  - Inputs: FileFilterType enum values
  - Outputs: File dialog filter strings, resource strings, images
  - Notes: Static methods for resource access

- **FileFilterType enum** (ResourceHelper.cs)
  - Purpose: Standardized file type specifications for file dialogs
  - Values: AllFiles, XML, Text, PDF, LIFT, OXES, AllImage, AllAudio, AllVideo, etc.
  - Notes: Each enum has corresponding resource string kstid{EnumMember}

- **FwStrings** (FwStrings.Designer.cs)
  - Purpose: General localized strings for FieldWorks UI
  - Access: FwStrings.ResourceString (type-safe properties)
  - Notes: Auto-generated from FwStrings.resx

- **FwTMStrings** (FwTMStrings.Designer.cs)
  - Purpose: Task management localized strings
  - Access: FwTMStrings.ResourceString
  - Notes: Auto-generated from FwTMStrings.resx

- **HelpTopicPaths** (HelpTopicPaths.Designer.cs)
  - Purpose: Help system topic path mappings
  - Access: HelpTopicPaths.TopicName
  - Notes: Maps help topics to paths

- **Images** (Images.Designer.cs)
  - Purpose: Type-safe access to embedded image resources
  - Access: Images.ImageName (returns Bitmap or Icon)
  - Notes: Images organized in subfolders (Edit/, File/, etc.)

## Entry Points
Referenced as library by all FieldWorks components. Resources accessed via static Designer classes.

## Test Index
No dedicated test project (resource library). Tested via consuming applications.

## Usage Hints
- Access strings: FwStrings.ResourceStringName
- Access images: Images.ImageName (returns Bitmap/Icon)
- File filters: ResourceHelper.FileFilter(FileFilterType.XML) for OpenFileDialog
- Localization: Add/modify .resx files; satellite assemblies built automatically
- Help paths: HelpTopicPaths.TopicName for context-sensitive help
- Images organized by menu category (Edit, File, Format, Help, Tools, View, Window)

## Related Folders
- **All FieldWorks applications**: Consume FwResources
- **Localization tools**: Process .resx files for translation

## References
- **Project files**: FwResources.csproj (net48)
- **Target frameworks**: .NET Framework 4.8.x
- **Key C# files**: ResourceHelper.cs (32K lines), FwFileExtensions.cs, FwStrings.Designer.cs (110K lines), FwTMStrings.Designer.cs (47K lines), HelpTopicPaths.Designer.cs (28K lines), ToolBarSystemStrings.Designer.cs (17K lines), Images.Designer.cs, ResourceHelperImpl.cs, SearchingAnimation.cs
- **Resource files**: FwStrings.resx (69K), FwTMStrings.resx (37K), HelpTopicPaths.resx (22K), ToolBarSystemStrings.resx, Images.resx, ResourceHelperImpl.resx (104K), SearchingAnimation.resx
- **Images folder**: Edit/, File/, Format/, Help/, Tools/, View/, Window/ subfolders with icons/images
- **Total C# lines**: 7458 (plus extensive Designer.cs auto-generated code)
- **Output**: FwResources.dll with embedded resources
- **Namespace**: SIL.FieldWorks.Resources
