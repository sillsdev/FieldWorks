---
last-reviewed: 2025-10-31
last-reviewed-tree: 2d8a7d9e0ef0899cbb02f6011d6f779dfdcded66364eccfb19a7daf1211aec78
status: draft
---

<!-- copilot:auto-change-log start -->
## Change Log (auto)

This section is populated by running:
1. `python .github/plan_copilot_updates.py --folders <Folder>`
2. `python .github/copilot_apply_updates.py --folders <Folder>`

Do not edit this block manually; rerun the scripts above after code or doc updates.
<!-- copilot:auto-change-log end -->

# SimpleRootSite COPILOT summary

## Purpose
Simplified root site implementation providing streamlined API for hosting FieldWorks views. Base class SimpleRootSite extends UserControl and implements IVwRootSite, IRootSite, IxCoreColleague, IEditingCallbacks for standard view hosting scenarios. Includes ActiveViewHelper for view activation tracking, DataUpdateMonitor for coordinating data change notifications, EditingHelper for clipboard/undo/redo operations, SelectionHelper for selection management, PrintRootSite for printing infrastructure, and numerous helper classes. Easier-to-use alternative to RootSite for most view hosting needs. Used extensively throughout FieldWorks for text display and editing.

## Architecture
C# class library (.NET Framework 4.8.x) with simplified root site implementation (17K+ lines in SimpleRootSite.cs alone). SimpleRootSite base class provides complete view hosting with event handling, keyboard management, accessibility support, printing, selection tracking, and data update coordination. Helper classes separate concerns (EditingHelper for editing, SelectionHelper for selection, ActiveViewHelper for activation). Test project (SimpleRootSiteTests) validates functionality.

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
C# .NET Framework 4.8.x, Windows Forms (UserControl), COM interop for Views engine, Accessibility APIs, IBus (Linux), XCore.

## Dependencies
Consumes: views (native rendering), ViewsInterfaces, FwUtils, LCModel, Keyboarding, XCore. Used by: xWorks, LexText, RootSite (extends SimpleRootSite), most text display components.

## Interop & Contracts
IVwRootSite (COM Views callbacks), IRootSite (FW contract), IxCoreColleague (XCore), IEditingCallbacks, IAccessible, COM marshaling.

## Threading & Performance
UI thread required. DataUpdateMonitor prevents reentrant updates. UpdateSemaphore for synchronization. Efficient view hosting (17K+ lines comprehensive).

## Config & Feature Flags
Behavior controlled by View specifications and data. No explicit configuration.

## Build Information
SimpleRootSite.csproj (net48, Library). Test project: SimpleRootSiteTests/. Output: SimpleRootSite.dll.

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
Base class for view hosting controls throughout FieldWorks. Extend and override MakeRoot() to construct views.

## Test Index
SimpleRootSiteTests/ covers root site functionality, editing, selection, updates.

## Usage Hints
Extend SimpleRootSite for view hosting. Override MakeRoot() for view construction. Use EditingHelper (clipboard), SelectionHelper (analysis), DataUpdateMonitor.BeginUpdate/EndUpdate (coordinated updates). Prefer over RootSite unless advanced features needed.

## Related Folders
Common/RootSite (advanced infrastructure), ViewsInterfaces/, views/ (native engine), xWorks/ and LexText/ (major consumers).

## References
SimpleRootSite.csproj (net48). Key file: SimpleRootSite.cs (17K+ lines alone). Helper classes: ActiveViewHelper, DataUpdateMonitor, EditingHelper, SelectionHelper, PrintRootSite. See `.cache/copilot/diff-plan.json` for complete file listing.
