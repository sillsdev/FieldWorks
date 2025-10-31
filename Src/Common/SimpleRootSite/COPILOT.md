---
last-reviewed: 2025-10-31
last-verified-commit: 9611cf70e
status: draft
---

# SimpleRootSite COPILOT summary

## Purpose
Simplified root site implementation providing streamlined API for hosting FieldWorks views. Base class SimpleRootSite extends UserControl and implements IVwRootSite, IRootSite, IxCoreColleague, IEditingCallbacks for standard view hosting scenarios. Includes ActiveViewHelper for view activation tracking, DataUpdateMonitor for coordinating data change notifications, EditingHelper for clipboard/undo/redo operations, SelectionHelper for selection management, PrintRootSite for printing infrastructure, and numerous helper classes. Easier-to-use alternative to RootSite for most view hosting needs. Used extensively throughout FieldWorks for text display and editing.

## Architecture
C# class library (.NET Framework 4.6.2) with simplified root site implementation (17K+ lines in SimpleRootSite.cs alone). SimpleRootSite base class provides complete view hosting with event handling, keyboard management, accessibility support, printing, selection tracking, and data update coordination. Helper classes separate concerns (EditingHelper for editing, SelectionHelper for selection, ActiveViewHelper for activation). Test project (SimpleRootSiteTests) validates functionality.

## Key Components
- **SimpleRootSite** class (SimpleRootSite.cs, massive file): Base view host control
  - Extends UserControl, implements IVwRootSite, IRootSite
  - Integrates with Views rendering engine (m_rootb: IVwRootBox)
  - Keyboard management (WindowsLanguageProfileSink, IKeyboardDefinition)
  - Mouse/selection handling
  - Scroll management
  - Printing support
  - Accessibility (IAccessible via AccessibilityWrapper)
  - Data update monitoring integration
- **ActiveViewHelper** (ActiveViewHelper.cs): View activation tracking
  - Tracks which view is currently active
  - Coordinates focus management
- **DataUpdateMonitor** (DataUpdateMonitor.cs): Change notification coordination
  - Monitors data updates in progress
  - Prevents reentrant updates
  - UpdateSemaphore for synchronization
- **EditingHelper** (EditingHelper.cs): Editing operations
  - Clipboard (cut/copy/paste)
  - Undo/redo coordination
  - Implements IEditingCallbacks
- **SelectionHelper** (SelectionHelper.cs): Selection management
  - Selection analysis and manipulation
  - SelInfo: Selection information capture
  - GetFirstWsOfSelection: Extract writing system from selection
- **SelectionRestorer** (SelectionRestorer.cs): Selection restoration
  - Preserves and restores selections across updates
- **PrintRootSite** (PrintRootSite.cs): Printing infrastructure
  - IPrintRootSite interface
  - Page layout and rendering for printing
- **VwSelectionArgs** (VwSelectionArgs.cs): Selection event args
- **SelPositionInfo** (SelPositionInfo.cs): Selection position tracking
- **TextSelInfo** (TextSelInfo.cs): Text selection details
- **ViewInputManager** (ViewInputManager.cs): Input management
- **VwBaseVc** (VwBaseVc.cs): Base view constructor
- **OrientationManager** (OrientationManager.cs): Vertical/horizontal text orientation
- **AccessibilityWrapper** (AccessibilityWrapper.cs): Accessibility support
  - Wraps IAccessible for Windows accessibility APIs
- **IbusRootSiteEventHandler** (IbusRootSiteEventHandler.cs): IBus integration (Linux)
- **IChangeRootObject** (IChangeRootObject.cs): Root object change interface
- **IControl** (IControl.cs): Control interface abstraction
- **IRootSite** (IRootSite.cs): Root site contract
- **ISelectionChangeNotifier** (ISelectionChangeNotifier.cs): Selection change notifications
- **LocalLinkArgs** (LocalLinkArgs.cs): Local hyperlink arguments
- **FwRightMouseClickEventArgs** (FwRightMouseClickEventArgs.cs): Right-click event args
- **HoldGraphics** (HoldGraphics.cs): Graphics context holder
- **RenderEngineFactory** (RenderEngineFactory.cs): Rendering engine creation

## Technology Stack
- C# .NET Framework 4.6.2 (net462)
- OutputType: Library
- Windows Forms (UserControl base)
- COM interop for Views engine (IVwRootSite, IVwRootBox)
- Accessibility APIs (IAccessible)
- IBus support for Linux keyboard input
- XCore for command routing (IxCoreColleague)

## Dependencies

### Upstream (consumes)
- **views**: Native rendering engine (IVwRootBox, IVwGraphics)
- **Common/ViewsInterfaces**: View interfaces (IVwRootSite, IVwSelection)
- **Common/FwUtils**: Utilities (Win32, ThreadHelper)
- **SIL.LCModel**: Data model
- **SIL.LCModel.Application**: Application services
- **SIL.Keyboarding**: Keyboard management
- **XCore**: Command routing (IxCoreColleague)
- **Windows Forms**: UI framework

### Downstream (consumed by)
- **xWorks**: Extensively uses SimpleRootSite for views
- **LexText**: Lexicon editing views
- **Common/RootSite**: Advanced root site extends SimpleRootSite
- Most FieldWorks components displaying text

## Interop & Contracts
- **IVwRootSite**: COM interface for Views engine callbacks
- **IRootSite**: FieldWorks root site contract
- **IxCoreColleague**: XCore command routing
- **IEditingCallbacks**: Editing operation notifications
- **IAccessible**: Windows accessibility
- COM marshaling for Views engine integration

## Threading & Performance
- **UI thread requirement**: All view operations must be on UI thread
- **DataUpdateMonitor**: Prevents reentrant updates
- **UpdateSemaphore**: Synchronization primitive
- **Performance**: Efficient view hosting; 17K lines indicates comprehensive functionality

## Config & Feature Flags
No explicit configuration. Behavior controlled by View specifications and data.

## Build Information
- **Project file**: SimpleRootSite.csproj (net462, OutputType=Library)
- **Test project**: SimpleRootSiteTests/SimpleRootSiteTests.csproj
- **Output**: SimpleRootSite.dll
- **Build**: Via top-level FW.sln or: `msbuild SimpleRootSite.csproj /p:Configuration=Debug`
- **Run tests**: `dotnet test SimpleRootSiteTests/SimpleRootSiteTests.csproj`

## Interfaces and Data Models

- **SimpleRootSite** (SimpleRootSite.cs)
  - Purpose: Base class for view hosting controls
  - Inputs: View specifications, root object, stylesheet
  - Outputs: Rendered text display with editing, selection, scrolling
  - Notes: Comprehensive 17K+ line implementation; extend for custom views

- **IVwRootSite** (implemented by SimpleRootSite)
  - Purpose: COM contract for Views engine callbacks
  - Inputs: Notifications from Views (selection change, scroll, etc.)
  - Outputs: Responses to Views queries (client rect, graphics, etc.)
  - Notes: Core interface bridging managed and native Views

- **IRootSite** (IRootSite.cs, implemented by SimpleRootSite)
  - Purpose: FieldWorks root site contract
  - Inputs: N/A (properties)
  - Outputs: RootBox, cache, stylesheet access
  - Notes: FieldWorks-specific extensions beyond IVwRootSite

- **EditingHelper** (EditingHelper.cs)
  - Purpose: Editing operations (clipboard, undo/redo)
  - Inputs: Edit commands, clipboard data
  - Outputs: Executes edits via Views
  - Notes: Implements IEditingCallbacks

- **SelectionHelper** (SelectionHelper.cs)
  - Purpose: Selection analysis and manipulation utilities
  - Inputs: IVwSelection objects
  - Outputs: Selection details (SelInfo), writing systems, ranges
  - Notes: Static helper methods for selection operations

- **DataUpdateMonitor** (DataUpdateMonitor.cs)
  - Purpose: Coordinates data change notifications, prevents reentrant updates
  - Inputs: Begin/EndUpdate calls
  - Outputs: IsUpdateInProgress flag
  - Notes: UpdateSemaphore for synchronization; critical for data consistency

- **ActiveViewHelper** (ActiveViewHelper.cs)
  - Purpose: Tracks active view for focus management
  - Inputs: View activation events
  - Outputs: Current active view
  - Notes: Singleton pattern for global active view tracking

- **PrintRootSite** (PrintRootSite.cs)
  - Purpose: Printing infrastructure and page layout
  - Inputs: Print settings, page dimensions
  - Outputs: Rendered pages for printing
  - Notes: Implements IPrintRootSite

## Entry Points
Extended by view hosting controls throughout FieldWorks. SimpleRootSite is base class for most text display components.

## Test Index
- **Test project**: SimpleRootSiteTests
- **Run tests**: `dotnet test SimpleRootSiteTests/SimpleRootSiteTests.csproj`
- **Coverage**: Root site functionality, editing, selection, updates

## Usage Hints
- Extend SimpleRootSite for view hosting controls
- Override MakeRoot() to construct view
- Use EditingHelper for clipboard operations
- Use SelectionHelper for selection analysis
- Use DataUpdateMonitor.BeginUpdate/EndUpdate for coordinated updates
- Simpler than RootSite; prefer SimpleRootSite unless advanced features needed

## Related Folders
- **Common/RootSite**: Advanced root site infrastructure (SimpleRootSite uses some RootSite classes)
- **Common/ViewsInterfaces**: Interfaces implemented by SimpleRootSite
- **views/**: Native rendering engine
- **xWorks, LexText**: Major consumers of SimpleRootSite

## References
- **Project files**: SimpleRootSite.csproj (net462), SimpleRootSiteTests/SimpleRootSiteTests.csproj
- **Target frameworks**: .NET Framework 4.6.2
- **Key C# files**: SimpleRootSite.cs (massive, 17K+ lines), ActiveViewHelper.cs, DataUpdateMonitor.cs, EditingHelper.cs, SelectionHelper.cs, SelectionRestorer.cs, PrintRootSite.cs, and others
- **Total lines of code**: 17073+ (SimpleRootSite.cs alone)
- **Output**: Output/Debug/SimpleRootSite.dll
- **Namespace**: SIL.FieldWorks.Common.RootSites
