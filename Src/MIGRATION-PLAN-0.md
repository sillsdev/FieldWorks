Below is a practical, evidence-based mapping and a patch-by-patch migration backlog. I focused on “flip-only” cutovers that replace large swaths of custom WinForms/Views code with single Avalonia elements or a focused add-on, avoiding interim bridges inside each screen.

Note: the code search results shown here are samples, not exhaustive. GitHub code search limits results per query. For more, use the search links provided at the end of each bucket.

Mapping current screens into the five buckets

1. Docking/workspace (MDI, tool windows, layout persistence)
* WeifenLuo DockPanel and custom “DockExtender”:
  * LCMBrowser window using DockPanel:
    * [Src/LCMBrowser/ModelWnd.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/LCMBrowser/ModelWnd.cs#L1)
  * ObjectBrowser shell using DockPanel:
    * [Lib/src/ObjectBrowser/ObjectBrowser.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Lib/src/ObjectBrowser/ObjectBrowser.cs#L1)
    * [Lib/src/ObjectBrowser/InspectorWnd.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Lib/src/ObjectBrowser/InspectorWnd.cs#L1)
    * [Lib/src/ObjectBrowser/InspectorGrid.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Lib/src/ObjectBrowser/InspectorGrid.cs#L191)
  * Custom docking framework:
    * [Src/Common/Controls/FwControls/DockExtender/DockExtender.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/FwControls/DockExtender/DockExtender.cs#L1)
    * [Src/Common/Controls/FwControls/DockExtender/Floaty.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/FwControls/DockExtender/Floaty.cs#L364)
    * [Src/Common/Controls/FwControls/DockExtender/IFloaty.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/FwControls/DockExtender/IFloaty.cs#L19)
* Replace with: Dock.Avalonia (full docking) or FluentAvalonia NavigationView \+ TabView shell.
2. Grids and hierarchical data views
* Heavy customizations around DataGridView:
  * Core wrappers:
    * [FwTextBoxControl](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Widgets/DataGridView/FwTextBoxControl.cs#L49)
    * [FwTextBoxColumn](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Widgets/DataGridView/FwTextBoxColumn.cs#L250)
    * [FwTextBoxCell](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Widgets/DataGridView/FwTextBoxCell.cs#L162)
    * [FwTextBoxRow](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Widgets/DataGridView/FwTextBoxRow.cs#L4)
    * [SortableBindingList\<T\>](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Widgets/DataGridView/SortableBindingList.cs#L1)
    * [SilButtonCell/Column](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Widgets/DataGridView/SilButtonCell.cs#L3)
  * Screens using grid-like displays:
    * Webonary log viewer (DataGridView-based): [Src/xWorks/WebonaryLogViewer.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/WebonaryLogViewer.cs#L12)
    * CharContext grids: [Src/FwCoreDlgs/CharContextCtrl.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/CharContextCtrl.cs#L400)
    * Object inspector grid: [InspectorGrid.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Lib/src/ObjectBrowser/InspectorGrid.cs#L191)
    * Browse viewer (custom list \+ scroller): [BrowseViewer.cs layout logic](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/XMLViews/BrowseViewer.cs#L1704)
* Replace with: Avalonia DataGrid and TreeDataGrid.
3. Property editors / inspectors (property grid-style UI)
* Styles and formatting editors:
  * Styles dialog: [FwStylesDlg](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/FwStylesDlg.cs#L1)
  * Font attributes tab: [FwFontAttributes](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/FwCoreDlgControls/FwFontAttributes.cs#L1)
  * Paragraph tab: [FwParagraphTab](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/FwCoreDlgControls/FwParagraphTab.cs#L1)
* Design-time property editing enhancements:
  * [EnhancedCollectionEditor](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Design/EnhancedCollectionEditor.cs#L1)
* Object inspector:
  * [InspectorWnd.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Lib/src/ObjectBrowser/InspectorWnd.cs#L1)
* Replace with: Avalonia.PropertyGrid (community) \+ custom editors where needed.
4. Embedded web panes and HTML-based content
* GeckoFX/XULRunner usage (must be retired):
  * Global initialization: [FieldWorks.cs (Gecko/XULRUNNER init)](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/FieldWorks/FieldWorks.cs#L131)
  * Linux env scripts: [environ-xulrunner](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/environ-xulrunner#L1), [environ](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/environ#L40)
* Web-backed views:
  * Xhtml document views: [XhtmlDocView](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/XhtmlDocView.cs#L7), [XhtmlRecordDocView](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/XhtmlRecordDocView.cs#L1)
  * HtmlControl base: [XCore/HtmlControl.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/XCore/HtmlControl.cs#L1)
  * Dictionary configuration preview: [DictionaryConfigurationDlg (Gecko preview)](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/DictionaryConfigurationDlg.cs#L119)
  * Import and help dialogs with Gecko: [MGAHtmlHelpDialog](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/LexText/Morphology/MGA/MGAHtmlHelpDialog.cs#L1), [LexImportWizardMarker uses Gecko/XSLT](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/LexText/LexTextControls/LexImportWizardMarker.cs#L1)
  * Other browser-embedded panels: [ReallySimpleListChooser help browser](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs#L1050)
* Replace with: Avalonia.WebView (WebView2/Edge on Win; WebKit on macOS/Linux).
5. Menus, toolbars, command routing
* Legacy menu/toolbar adapter:
  * [UIAdapterInterfaces/TMInterface.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/UIAdapterInterfaces/TMInterface.cs#L39)
* XCore-based command plumbing (throughout xWorks/XCore)
* Replace with: FluentAvalonia (Menu, CommandBar, NavigationView) \+ MVVM commands (ReactiveUI or CommunityToolkit.Mvvm).

Patch-by-patch backlog with estimates and dependencies

Legend

* Effort estimate is person-days (PD) for 1 dev, excluding QA; add 30–50% for review/QA.
* Dependencies call out prerequisites for a clean “flip” and removal of old code in the same patch.
* Each patch is a single flip (no mixed WinForms controls in the same screen).

Phase 0: Foundations

* P0.1 Create Avalonia solution, app host, and theme baseline; wire DI/MVVM, logging
  * 3–5 PD. Dependency: none.
* P0.2 Choose libraries and set up packages
  * Docking: Dock.Avalonia; UI: FluentAvalonia; DataGrid/TreeDataGrid; PropertyGrid; WebView.
  * 1–2 PD. Dependency: P0.1.
1. Docking/workspace
* P1.1 Avalonia app shell with Dock.Avalonia; define tool/document layout model and layout persistence
  * 5–8 PD. Dependency: P0.x.
  * Acceptance: Shell starts; panes/tabs managed; layout saved/loaded.
* P1.2 Port ObjectBrowser host to Avalonia pane; keep inspector content temporarily blank or minimal
  * 4–6 PD. Dep: P1.1.
  * Remove WinForms ObjectBrowser shell (leave the inspector screen for a later patch).
* P1.3 Port LCMBrowser ModelWnd host to Avalonia pane
  * 4–6 PD. Dep: P1.1.
* P1.4 Remove custom DockExtender and WeifenLuo from those hosts; keep independent windows (dialogs) as is
  * 2–3 PD. Dep: P1.2–P1.3.
2. Grids and hierarchical data
* P2.1 Introduce Avalonia DataGrid binding patterns and a TsString-like cell template
  * Build a reusable Avalonia cell template for multilingual text (bind text, RTL, font per WS).
  * 5–8 PD. Dep: P0.x.
* P2.2 Replace WebonaryLogViewer grid with Avalonia DataGrid
  * [WebonaryLogViewer.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/WebonaryLogViewer.cs#L12)
  * 3–5 PD. Dep: P2.1.
* P2.3 Replace ObjectBrowser InspectorGrid with Avalonia DataGrid or TreeDataGrid
  * [InspectorGrid.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Lib/src/ObjectBrowser/InspectorGrid.cs#L191)
  * 6–9 PD. Dep: P1.2, P2.1.
* P2.4 Replace CharContext grids (FwCoreDlgs/CharContextCtrl) with Avalonia DataGrid
  * [CharContextCtrl.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/CharContextCtrl.cs#L400)
  * 4–6 PD. Dep: P2.1.
* P2.5 Replace BrowseViewer list \+ scroller with Avalonia TreeDataGrid
  * [BrowseViewer.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/XMLViews/BrowseViewer.cs#L1704)
  * 8–12 PD. Dep: P1.1, P2.1.
* P2.6 Retire DataGridView wrappers (FwTextBox\*, SortableBindingList, SilButtonCell) where no longer used
  * 2–3 PD. Dep: P2.2–P2.5.
3. Property editors / inspectors
* P3.1 Introduce Avalonia.PropertyGrid, define editor templates for common types
  * 4–6 PD. Dep: P0.x.
* P3.2 Replace ObjectBrowser InspectorWnd property-edit panel with PropertyGrid where applicable
  * [InspectorWnd.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Lib/src/ObjectBrowser/InspectorWnd.cs#L1)
  * 5–8 PD. Dep: P1.2, P3.1.
* P3.3 Replace Styles dialog (FwStylesDlg) with Avalonia UI using PropertyGrid \+ custom editors for font/paragraph props
  * [FwStylesDlg.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/FwStylesDlg.cs#L1), [FwFontAttributes.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/FwCoreDlgControls/FwFontAttributes.cs#L1), [FwParagraphTab.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/FwCoreDlgs/FwCoreDlgControls/FwParagraphTab.cs#L1)
  * 10–15 PD. Dep: P3.1.
* P3.4 Replace EnhancedCollectionEditor usages with Avalonia alternatives or custom PropertyGrid editor
  * [EnhancedCollectionEditor.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/Design/EnhancedCollectionEditor.cs#L1)
  * 3–5 PD. Dep: P3.1.
4. Embedded web panes
* P4.1 Introduce Avalonia.WebView, create a WebPane abstraction (navigate, content, inject JS, DOM callbacks)
  * 5–8 PD. Dep: P0.x.
* P4.2 Replace HtmlControl with WebPane and migrate XhtmlDocView, XhtmlRecordDocView
  * [HtmlControl.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/XCore/HtmlControl.cs#L1), [XhtmlDocView.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/XhtmlDocView.cs#L7), [XhtmlRecordDocView.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/XhtmlRecordDocView.cs#L1)
  * 8–12 PD. Dep: P4.1.
* P4.3 Replace Dialog/Preview web panes (DictionaryConfigurationDlg preview, MGAHtmlHelpDialog, ReallySimpleListChooser help)
  * [DictionaryConfigurationDlg.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/xWorks/DictionaryConfigurationDlg.cs#L119), [MGAHtmlHelpDialog.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/LexText/Morphology/MGA/MGAHtmlHelpDialog.cs#L1), [ReallySimpleListChooser.cs (help browser)](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/Controls/XMLViews/ReallySimpleListChooser.cs#L1050)
  * 6–10 PD. Dep: P4.1.
* P4.4 Remove Gecko/XULRunner initialization and env script usage
  * [FieldWorks.cs init](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/FieldWorks/FieldWorks.cs#L131), [environ-xulrunner](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/environ-xulrunner#L1), [environ](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/environ#L40)
  * 2–3 PD. Dep: P4.2–P4.3 complete.
5. Menus, toolbars, commands
* P5.1 Introduce FluentAvalonia menus/command surfaces; define MVVM command layer
  * 6–10 PD. Dep: P0.x.
* P5.2 Replace legacy TMInterface-based menus/toolbar on main shell
  * [TMInterface.cs](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/Common/UIAdapterInterfaces/TMInterface.cs#L39)
  * 8–12 PD. Dep: P5.1, P1.1.
* P5.3 Migrate contextual menus used in the ported screens (ObjectBrowser, LCMBrowser, grids)
  * 4–6 PD. Dep: P5.2 and respective screen migrations.

Cross-cutting and deferrals

* Writing system and text shaping
  * For grid/editor cells, ensure RTL alignment and font per WS (done in P2.1).
  * Defer “heavy” text editors based on RootSite/Views (custom shaping/selection/IME) until after shells, grids, properties, and web panes are complete.
* Accessibility
  * Convert accessibility helpers (e.g., [XCoreUserControl](https://github.com/sillsdev/FieldWorks/blob/158e05dc5a688ba39babc8c5f83a21e581ea076f/Src/XCore/xCoreUserControl.cs#L4)) to Avalonia AutomationPeers as each screen is ported.
* Printing/pagination
  * Defer until after the five buckets; Avalonia Document/printing or custom paginator for the migrated views.

Dependencies summary

* P0 → P1 (shell) → P2/P3/P4 (feature screens) → P5 (menus) is the recommended sequence.
* Each screen’s patch depends on the shell and the relevant shared component (DataGrid, PropertyGrid, WebView).

Scope control for “flip, not flop”

* Each patch replaces a whole screen/pane at once. For example, “WebonaryLogViewer to Avalonia DataGrid” fully removes its WinForms/DataGridView code in the same PR.
* Use navigation-level feature flags to route to old vs. new implementations between releases, but never mix on the same surface.

High-confidence “first batch” to start next sprint

* P1.1 (shell), P2.1 (grid foundation), P2.2 (WebonaryLogViewer), P4.1 (WebView foundation), P4.2 (XhtmlDocView-only, read-only view first).
* These yield visible value quickly and kill big custom surfaces (docking, grid wrappers, Gecko) with minimal risk to core text editing.

Risk notes

* Embedded web: swapping Gecko to WebView2 is straightforward but ensure event bridging for DomClick and custom JS interop used in XhtmlDocView/RecordDocView.
* Grid cell rendering for multilingual text: validate RTL and per-WS font/size; Avalonia DataGrid supports this with templates and style triggers.
* Menus/commands: migrating XCore/TMInterface message routing to MVVM requires careful mapping; do it after key screens are in Avalonia.

Useful GitHub search links to explore more (full repository)

* DataGridView usages: [https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+DataGridView\&type=code](https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+DataGridView&type=code)
* Gecko/XULRunner usages: [https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+GeckoWebBrowser+OR+XULRUNNER+OR+XWebBrowser\&type=code](https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+GeckoWebBrowser+OR+XULRUNNER+OR+XWebBrowser&type=code)
* WeifenLuo docking usages: [https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+WeifenLuo.WinFormsUI.Docking\&type=code](https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+WeifenLuo.WinFormsUI.Docking&type=code)
* DockExtender framework: [https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+DockExtender+Floaty\&type=code](https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+DockExtender+Floaty&type=code)
* Styles/formatting editors: [https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+FwStylesDlg+OR+FwFontAttributes+OR+FwParagraphTab\&type=code](https://github.com/search?q=repo%3Asillsdev%2FFieldWorks+FwStylesDlg+OR+FwFontAttributes+OR+FwParagraphTab&type=code)

Limitations and next step

* The mapping above is based on targeted code searches (limited result windows). If you want, I can produce a CSV inventory of all forms/controls per bucket by running broader searches and grouping by namespace and path, then finalize the first 6–8 patches with assignees and acceptance criteria.