// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing.Printing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.Framework.DetailControls;
// Bare DataTree in this file means the legacy WinForms tree; the Avalonia twin stays qualified.
using DataTree = SIL.FieldWorks.Common.Framework.DetailControls.DataTree;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;
using XCore;
using System.Collections.Generic;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.PlatformUtilities;
using SIL.Reporting;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Avalonia half of <see cref="RecordEditView"/>: everything that only exists because a
	/// record can be shown on the new <see cref="DetailHostControl"/> instead of the
	/// legacy <see cref="DataTree"/>. Kept in its own file (mirroring this codebase's
	/// Form/Form.Designer.cs split) so the legacy-facing file stays small; nothing here
	/// changes behavior when <c>UIMode</c> is Legacy.
	/// </summary>
	public partial class RecordEditView
	{
		internal readonly struct PersistentCommandTargetIdentity
			: IEquatable<PersistentCommandTargetIdentity>
		{
			internal PersistentCommandTargetIdentity(int hvo, string fieldName,
				string className, string layoutName, string callerPath)
			{
				Hvo = hvo;
				FieldName = fieldName;
				ClassName = className;
				LayoutName = layoutName;
				CallerPath = callerPath;
			}

			internal int Hvo { get; }

			internal string FieldName { get; }

			internal string ClassName { get; }

			internal string LayoutName { get; }

			internal string CallerPath { get; }

			public bool Equals(PersistentCommandTargetIdentity other)
			{
				return Hvo == other.Hvo
					&& string.Equals(FieldName, other.FieldName, StringComparison.Ordinal)
					&& string.Equals(ClassName, other.ClassName, StringComparison.Ordinal)
					&& string.Equals(LayoutName, other.LayoutName, StringComparison.Ordinal)
					&& string.Equals(CallerPath, other.CallerPath, StringComparison.Ordinal);
			}

			public override bool Equals(object obj)
				=> obj is PersistentCommandTargetIdentity other && Equals(other);

			public override int GetHashCode()
			{
				unchecked
				{
					var hash = Hvo;
					hash = (hash * 397) ^ (FieldName?.GetHashCode() ?? 0);
					hash = (hash * 397) ^ (ClassName?.GetHashCode() ?? 0);
					hash = (hash * 397) ^ (LayoutName?.GetHashCode() ?? 0);
					return (hash * 397) ^ (CallerPath?.GetHashCode() ?? 0);
				}
			}
		}

		private UIFramework m_activeUIFramework;
		private readonly EditControlFactory m_lexicalEditControlFactory;
		private readonly UIFrameworkSelectionService m_frameworkSelectionService = new UIFrameworkSelectionService();
		private DetailHostControl m_avaloniaEntryForm;
		private RecordClerkNavigationContext m_recordNavigationContext;
		// Owns the fenced edit context; swapping/clearing through it cancels any open session so an
		// open undo task is never orphaned (an orphan makes the shutdown Save throw "Commit at wrong place").
		private readonly DetailEditContextHolder m_detailEditContext = new DetailEditContextHolder();
		private AvaloniaDetailRefreshController m_avaloniaRefreshController;
		private InventoryViewDefinitionSource m_inventoryViewDefinitionSource;
		private string m_inventoryViewDefinitionProjectName;
		// The approved baseline-adapter ids -- the ONLY routes allowed to drive hidden legacy
		// infrastructure while Avalonia is active.
		internal const string CommandMenuRoutingAdapterId = "command-menu-routing";
		private static readonly string[] ApprovedBaselineAdapters = { CommandMenuRoutingAdapterId };
		// The active-host contract for the CURRENT framework, kept in sync with every
		// m_activeUIFramework assignment (SetUIFramework) from the approved set above.
		// Assert sites only pass the adapter id they claim, so an unlisted id actually trips -- a
		// contract constructed at the assert site from the very id it then asserts could never fail.
		private ActiveHostContract m_activeHostContract;

		// Viewing parity: expansion state persists per header stable id -- in-session through the
		// dictionary, across sessions through PropertyTable local settings, the legacy ExpansionStateKey
		// behavior. Per-instance deliberately: a process-wide static would leak state across
		// projects/windows for the app lifetime.
		private readonly Dictionary<string, bool> m_expansionStates = new Dictionary<string, bool>();

		private bool ShouldUseAvaloniaLexiconEdit
		{
			get { return m_activeUIFramework == UIFramework.Avalonia; }
		}

		/// <summary>
		/// Auto-save: settles any open fenced edit session -- commit when validation is
		/// clean, roll back otherwise. The holder guards internally (no-op when nothing is open),
		/// so this is idempotent and safe to call unconditionally from ANY host path -- including
		/// while Legacy is active, when no fenced session can be open.
		/// </summary>
		private void SettleDetailEdits()
		{
			m_detailEditContext.Settle();
		}

		/// <summary>
		/// Settles any open fenced edit session before the window's save-on-tool-switch commit runs, so
		/// the outgoing view's open undo task never faults that commit. The same auto-save the view
		/// already performs on record navigation, UIMode flip, and go-away, exposed for the tool/area
		/// switch (which reaches the view from outside, not through a record/navigation path).
		/// Explicit implementation: the class already carries the legacy vetoing
		/// <c>bool PrepareToGoAway()</c> override, so the void seam keeps out of its way.
		/// </summary>
		void IPrepareToGoAway.PrepareToGoAway()
		{
			if (IsDisposed)
				return;
			SettleDetailEdits();
		}

		/// <summary>
		/// The host's response when <see cref="Settle"/> rolled
		/// back a pending lexical edit because it failed validation. The data was already rolled back
		/// safely; this only tells the user WHY, so a cleared-required-field edit is not silently lost
		/// on navigate/close. The host is the sanctioned WinForms carve-out (the Avalonia pane stays
		/// WinForms-free), so the warning uses the standard WinForms MessageBox over this control's form.
		/// </summary>
		private void ShowInvalidEditRolledBackWarning(IReadOnlyList<string> reasons)
		{
			if (reasons == null || reasons.Count == 0 || IsDisposed)
				return;
			var message = string.Format(
				System.Globalization.CultureInfo.CurrentCulture,
				FwAvaloniaStrings.EditDiscardedInvalidFormat,
				string.Join(Environment.NewLine, reasons));
			MessageBox.Show(FindForm(), message, FwAvaloniaStrings.EditDiscardedInvalidTitle,
				MessageBoxButtons.OK, MessageBoxIcon.Warning);
		}

		/// <summary>
		/// The ordering-sensitive teardown of the Avalonia entry-form plumbing -- this is the ONE
		/// place that ordering lives. Teardown order matters: stop the event/notification
		/// plumbing FIRST so the settle's commit/rollback PropChanged cannot re-enter a dying
		/// view, then settle (auto-save extends to teardown: a valid pending edit commits,
		/// invalid rolls back), then drop the context, and only then dispose the companions and
		/// the host control itself.
		/// </summary>
		private void TearDownAvaloniaEntryForm()
		{
			if (m_avaloniaEntryForm != null)
				m_avaloniaEntryForm.DetailEditCompleted -= OnAvaloniaDetailEditCompleted;
			m_detailEditContext.DetachDeactivateHook();
			m_detailEditContext.DetachUndoGuard();
			m_detailEditContext.InvalidEditRolledBack = null;
			m_avaloniaRefreshController?.Dispose();
			SettleDetailEdits();
			m_detailEditContext.Clear();
			m_avaloniaEntryForm?.Dispose();
			// Null the host + refresh controller after disposing them. The recreation guards
			// (EnsureAvaloniaEntryFormInitialized / EnsureAvaloniaRefreshController) key on `== null`, so a
			// runtime flip New->Legacy->New rebuilds a fresh entry form instead of re-showing a disposed one.
			m_avaloniaEntryForm = null;
			m_avaloniaRefreshController = null;
		}

		/// <summary>
		/// The bidirectional selection bridge for this host's clerk. Created on first use so
		/// the clerk is initialized. Views (including the Avalonia host) follow the current-record bus
		/// through its event and publish their own selection back through it.
		/// </summary>
		internal IRecordNavigationContext RecordNavigationContext
		{
			get
			{
				if (m_recordNavigationContext == null && Clerk != null)
					m_recordNavigationContext = new RecordClerkNavigationContext(Clerk);
				return m_recordNavigationContext;
			}
		}

		private UIFramework ResolveConfiguredUIFramework()
		{
			// Route the per-host decision through the explicit selection service rather than
			// inferring product routing ad hoc from settings/PropertyTable state.
			var uiMode = m_propertyTable != null
				? m_propertyTable.GetStringProperty(UIFrameworkResolver.UIModePropertyName, UIFrameworkResolver.LegacyUIMode)
				: UIFrameworkResolver.LegacyUIMode;
			var toolName = m_propertyTable != null
				? m_propertyTable.GetStringProperty("currentContentControl", string.Empty)
				: string.Empty;

			// Per-tool opt-out from the UIModeDisabledTools setting (the master UIMode=New switch defaults
			// every catalog tool on; this is the individual override a user can flip back off).
			bool? overrideEnabled = null;
			if (m_propertyTable != null)
			{
				var disabledTools = m_propertyTable.GetStringProperty(
					UIFrameworkResolver.UIModeDisabledToolsPropertyName, string.Empty);
				if (UIFrameworkResolver.IsToolDisabledByUser(disabledTools, toolName))
					overrideEnabled = false;
			}

			return m_frameworkSelectionService.Decide(uiMode, toolName, overrideEnabled).Framework;
		}

		private void EnsureAvaloniaEntryFormInitialized()
		{
			if (m_avaloniaEntryForm != null)
				return;

			m_avaloniaEntryForm = (DetailHostControl)m_lexicalEditControlFactory.Create(UIFramework.Avalonia);
			m_avaloniaEntryForm.Dock = DockStyle.Fill;
			m_avaloniaEntryForm.DetailEditCompleted += OnAvaloniaDetailEditCompleted;
			if (!m_panel.Controls.Contains(m_avaloniaEntryForm))
				m_panel.Controls.Add(m_avaloniaEntryForm);
		}

		/// <summary>
		/// Subscribe the Avalonia view to the real PropChanged bus so external edits to
		/// the displayed entry (legacy views, refresh-driven reloads) re-resolve the detail view.
		/// Refreshes are held while this view's own edit session is open and delivered on completion.
		/// </summary>
		private void EnsureAvaloniaRefreshController()
		{
			if (m_avaloniaRefreshController != null)
				return;

			// The controller owns the ONE coalesced, editing-aware refresh queue (PropChanged
			// deliveries and host-requested re-shows alike); the host only supplies UI-thread
			// deferral, so a late-queued refresh still re-checks "is the user typing now?" inside
			// the controller's runner before recomposing.
			m_avaloniaRefreshController = new AvaloniaDetailRefreshController(
				Cache,
				() => Clerk?.CurrentObject,
				() => m_detailEditContext.Current?.IsOpen == true,
				RefreshAvaloniaDetail,
				new RefreshCoordinator(),
				ScheduleOnUiThread,
				// This lexical host's relevance rule; the controller itself stays host-agnostic.
				changed => IsChangeWithinEntry(changed, Clerk?.CurrentObject));
			// Global Undo/Redo while a fenced session is open would re-enter the UOW write lock
			// (LockRecursionException); the guard settles the pending edit instead.
			m_detailEditContext.AttachUndoGuard(Cache.ActionHandlerAccessor);
			// When Settle rolls back a pending edit because it
			// failed validation (e.g. the required lexeme form was cleared, then the user navigated
			// away), tell the user WHY rather than discarding it silently. The rollback still happens
			// (the safe close that keeps the open undo task from stranding); we only surface the reason.
			m_detailEditContext.InvalidEditRolledBack = ShowInvalidEditRolledBackWarning;
			// The guard only hooks THIS window's undo stack -- it cannot reach other windows'
			// stacks,
			// so Ctrl+Z in another window while this one holds an open session would still re-enter
			// the write lock. Mitigate by settling whenever this view's top-level window deactivates
			// (the user must focus another window before they can undo there).
			m_detailEditContext.AttachDeactivateHook(FindForm());
		}

		/// <summary>
		/// The lexical host's refresh relevance: a change is relevant when the changed
		/// object is, or is owned by, the entry on display. This is the predicate the host injects
		/// into <see cref="AvaloniaDetailRefreshController"/>; static and internal so it is
		/// unit-testable without a live view.
		/// </summary>
		internal static bool IsChangeWithinEntry(ICmObject changed, ICmObject current)
		{
			if (changed == null || current == null)
				return false;
			// Walking the owner chain (rather than checking only the root) keeps this correct for
			// any record class, not just entries, so nested edits also trigger the coalesced
			// refresh.
			for (var o = changed; o != null; o = o.Owner)
				if (o.Hvo == current.Hvo)
					return true;
			return false;
		}

		// UI-thread deferral for the controller's coalesced refresh queue: posting to the message
		// queue lets the current call stack (commit/rollback PropChanged, the focus transition
		// that triggered an auto-save) unwind before the view is rebuilt.
		private void ScheduleOnUiThread(Action runner)
		{
			if (IsDisposed)
				return;
			if (IsHandleCreated)
			{
				try
				{
					BeginInvoke(runner);
				}
				catch (InvalidOperationException)
				{
					// Teardown race: the handle can die between the IsHandleCreated check and the
					// post, and BeginInvoke then throws. The view is going away, so drop the
					// refresh rather than rethrow into the LCModel PropChanged loop that asked.
				}
			}
			else
			{
				runner();
			}
		}

		/// <summary>
		/// Shows the Avalonia detail view for a record: the composed full-entry view when the record is a
		/// lexical entry (first-slice fallback if composition fails), or the resource-backed
		/// unsupported state otherwise.
		/// </summary>
		private void ShowAvaloniaEntry(ICmObject obj)
		{
			// Auto-save: a session still open from the previous record/edit settles (commit
			// when valid, roll back when not) before the detail view is replaced. Replace's
			// cancel-on-displace remains the safety net.
			SettleDetailEdits();

			// Adapter hygiene: the hidden command-routing DataTree must never answer mediator
			// commands for a PREVIOUS record -- reset it whenever the shown record changes; the
			// next
			// right-click re-syncs it (EnsureMenuCommandAdapter). Without this, Insert Sense from
			// the main menu could silently target the entry that was last right-clicked.
			if (m_dataTreeInitialized && m_dataEntryForm?.Root != null && obj != null
				&& m_dataEntryForm.Root.Hvo != obj.Hvo)
			{
				m_dataEntryForm.Reset();
			}

			if (obj == null)
			{
				m_detailEditContext.Clear();
				m_avaloniaEntryForm.ShowMessage(FwAvaloniaStrings.EntryTypeUnsupported);
				return;
			}

			// The composer is class-general -- it works for any record-root class. Only
			// LexEntry gets the first-slice fallback below; other classes that fail to
			// compose show the unsupported message, never an NRE.
			var lexEntry = obj as ILexEntry;

			// Viewing parity: honor the same View -> Show Hidden Fields setting legacy DataTree
			// reads (ShowHiddenFields-{tool}, local settings).
			var toolName = m_propertyTable.GetStringProperty("currentContentControl", string.Empty);
			var showHidden = m_propertyTable.GetBoolProperty("ShowHiddenFields-" + toolName, false,
				PropertyTable.SettingsGroup.LocalSettings);

			DetailModel detail = null;
			IDetailEditContext editContext = null;
			ComposedDetail composed = null;
			try
			{
				var source = GetInventoryViewDefinitionSource();
				composed = lexEntry != null
					? DetailComposer.Compose(lexEntry, Cache, showHidden,
						source: source.GetSnapshot)
					// Non-entry roots compose against the tool's configured layout
					// (m_layoutName, default "Normal"); a type-selected layout (m_layoutChoiceField, e.g.
					// Notebook RnGenericRec keyed on "Type") resolves to the right variant inside Compose.
					: DetailComposer.Compose(obj, Cache,
						string.IsNullOrEmpty(m_layoutName) ? "Normal" : m_layoutName, showHidden,
						source: source.GetSnapshot,
						layoutChoiceField: m_layoutChoiceField);
				if (composed != null)
				{
					detail = composed.Model;
					editContext = composed.EditContext;
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Avalonia detail composition failed; using the host fallback.", e);
			}

			if (detail == null)
			{
				if (lexEntry == null)
				{
					// No first-slice fallback exists for a non-LexEntry root: show the unsupported state
					// rather than crash.
					m_detailEditContext.Clear();
					m_avaloniaEntryForm.ShowMessage(FwAvaloniaStrings.EntryTypeUnsupported);
					return;
				}
				detail = LexiconEditErrorFallback.Build(lexEntry, Cache);
				editContext = new LexiconFirstSliceEditContext(lexEntry, Cache);
			}

			// Re-showing mid-edit (navigation, refresh, Show Hidden Fields, window
			// activation) cancels displaced context's fenced session -- an orphaned
			// undo task makes shutdown Save throw "Commit at wrong place".
			m_detailEditContext.Replace(editContext);

			EnsureAvaloniaRefreshController();
			// The deactivate-settle hook needs a realized top-level Form. If the handle was not yet
			// created when the controller was first ensured (e.g. the very first record shows before
			// the window realizes), retry here on each show until it attaches -- otherwise the
			// cross-window-undo mitigation would be silently lost for this host's lifetime.
			if (!m_detailEditContext.IsDeactivateHookAttached)
				m_detailEditContext.AttachDeactivateHook(FindForm());
			m_avaloniaEntryForm.ShowDetail(detail, editContext,
				OnDetailWritingSystemFocused,
				GetPersistedExpansionState, PersistExpansionState,
				OnDetailMenuRequested, OnDetailLinkRequested,
				new FwTsStringClipboard(Cache.WritingSystemFactory),
				GetPersistedLabelColumnWidth, PersistLabelColumnWidth);
		}

		private InventoryViewDefinitionSource GetInventoryViewDefinitionSource()
		{
			var projectName = Cache?.ProjectId?.Name;
			if (string.IsNullOrEmpty(projectName))
				throw new InvalidOperationException("The project layout inventory key is unavailable.");
			if (m_inventoryViewDefinitionSource != null
				&& m_inventoryViewDefinitionProjectName == projectName)
			{
				return m_inventoryViewDefinitionSource;
			}

			var layouts = Inventory.GetInventory("layouts", Cache.ProjectId.Name);
			if (layouts == null)
				throw new InvalidOperationException("The project layout inventory is unavailable.");
			var partsXml = DetailComposer.GetMergedPartsXml();
			if (string.IsNullOrEmpty(partsXml))
				throw new InvalidOperationException("The merged detail parts are unavailable.");

			m_inventoryViewDefinitionSource = new InventoryViewDefinitionSource(layouts, partsXml,
				Cache.MetaDataCacheAccessor);
			m_inventoryViewDefinitionProjectName = projectName;
			return m_inventoryViewDefinitionSource;
		}

		/// <summary>
		/// Called when a writing system editor gains focus. Never throws: a focus
		/// event can race view teardown, so failures are logged instead.
		/// </summary>
		internal void OnDetailWritingSystemFocused(string wsTag)
		{
			if (IsDisposed || m_propertyTable == null)
				return;
			try
			{
				// Without this, the last-focused root site still answers WasFocused() and the
				// property change below would make it steal focus and re-tag its selection.
				SimpleRootSite.ForgetLastFocusedRootSite();
				WritingSystemKeyboards.Activate(Cache, wsTag);
				var ws = Cache.WritingSystemFactory.GetWsFromStr(wsTag);
				if (ws <= 0)
					return;
				Publisher.Publish(new PublisherParameterObject(
					EventConstants.WritingSystemUnderCursorChanged, ws, m_propertyTable.GetWindow()));
			}
			catch (Exception e)
			{
				Logger.WriteError("Writing-system focus handling failed.", e);
			}
		}

		/// <summary>The shared per-object menu group (Field Visibility / Move Field / Help).</summary>
		internal const string ObjectMenuId = "mnuDataTree-Object";

		/// <summary>
		/// The multi-writing-system slice menu group: the Writing Systems submenu PLUS the same
		/// Field Visibility / Move Field / Help leaves <see cref="ObjectMenuId"/> defines.
		/// </summary>
		internal const string MultiStringSliceMenuId = "mnuDataTree-MultiStringSlice";

		/// <summary>
		/// Composes the ordered menu-id list for a row's SLICE menu (label right-click and the
		/// field-options button): the row's own <c>menu=</c> binding, then exactly ONE shared
		/// trailing group. Both shared menus define Field Visibility / Move Field / Help, so
		/// adding both would show that group twice. Section hotlinks never join this menu; they
		/// stay on their own affordance. Internal-static so the composition is unit-testable
		/// without a live window.
		/// </summary>
		internal static IReadOnlyList<string> ComposeSliceMenuIds(string fieldMenuId,
			bool isMultiStringRow)
		{
			var menus = new List<string>();
			if (!string.IsNullOrEmpty(fieldMenuId))
				menus.Add(fieldMenuId);
			// Exactly one shared group, whichever route put it there: a row whose own binding IS
			// one of the two already carries it.
			if (menus.TrueForAll(id => id != MultiStringSliceMenuId && id != ObjectMenuId))
				menus.Add(isMultiStringRow ? MultiStringSliceMenuId : ObjectMenuId);
			return menus;
		}

		/// <summary>
		/// Composes the menu-id list for an IN-STRING right-click (inside a row's value).
		/// </summary>
		internal static IReadOnlyList<string> ComposeInStringMenuIds(string fieldContextMenuId)
		{
			var menus = new List<string>();
			if (!string.IsNullOrEmpty(fieldContextMenuId))
				menus.Add(fieldContextMenuId);
			return menus;
		}

		private void OnDetailMenuRequested(DetailMenuRequest request)
		{
			try
			{
				// An adapter failure must not suppress the menu itself: items that need the hidden
				// colleague chain disable, everything else still works (and the failure is logged).
				try
				{
					EnsureMenuCommandAdapter(request.Field.ObjectHvo, request.Field.Field);
				}
				catch (Exception adapterError)
				{
					Logger.WriteError("Detail menu command adapter failed; menu items that need "
						+ "the hidden colleague chain will be disabled.", adapterError);
				}

				var ids = new List<string>();
				switch (request.Kind)
				{
					case DetailMenuKind.ContextMenu:
						ids.AddRange(ComposeInStringMenuIds(request.Field.ContextMenuId));
						break;
					case DetailMenuKind.Hotlinks:
						ids.Add(request.Field.HotlinksId);
						break;
					default:
						ids.AddRange(ComposeSliceMenuIds(request.Field.MenuId,
							request.Field.IsMultiStringRow));
						break;
				}

				var idArray = ids.Where(id => !string.IsNullOrEmpty(id)).ToArray();
				var window = m_propertyTable.GetValue<XWindow>("window");

				// Render the SAME xCore menu natively in Avalonia -- identical items,
				// enablement, and mediator dispatch; only rendering changes. The WinForms
				// adapter menu remains the fallback if materialization fails.
				try
				{
					var items = CreateNativeDetailMenuItems(request.Field, idArray);
					if (items.Count > 0)
					{
						// A keyboard-opened menu anchors under the row it came from; a
						// right-click opens it at the pointer.
						m_avaloniaEntryForm.ShowContextMenu(items, request.AnchorControl,
							request.OpenAtPointer);
						return;
					}
				}
				catch (Exception nativeMenuError)
				{
					Logger.WriteError("Avalonia-native menu failed; falling back to the adapter menu.",
						nativeMenuError);
				}

				window.ShowContextMenu(idArray, AdapterMenuScreenPoint(request), null, null);
				RefreshAvaloniaDetail();
			}
			catch (Exception e)
			{
				Logger.WriteError("Detail context menu failed.", e);
			}
		}

		private IReadOnlyList<DetailMenuItem> CreateNativeDetailMenuItems(DetailField field,
			string[] menuIds)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var hasBroadTarget = EnsureMenuCommandTarget(field.ObjectHvo, field.Field);
			var hasPersistentTarget = hasBroadTarget
				&& TrySetPersistentMenuCommandTarget(field, false);
			return XCoreMenuBridge.CreateMenuItems(window, menuIds,
				choice => CreateLegacyCommandMenuItem(field, choice, hasPersistentTarget));
		}

		private DetailMenuItem CreateLegacyCommandMenuItem(DetailField field, ChoiceBase choice,
			bool hasPersistentTarget)
		{
			var persistent = IsPersistentLayoutCommand(choice);
			var display = choice.GetDisplayProperties();
			var captured = choice;
			return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(display.Text),
				(!persistent || hasPersistentTarget) && display.Enabled,
				display.Checked, null, () =>
				{
					var canExecute = persistent
						? EnsurePersistentMenuCommandTarget(field)
						: EnsureMenuCommandTarget(field.ObjectHvo, field.Field);
					if (!canExecute)
						return;
					var currentDisplay = captured.GetDisplayProperties();
					if (!currentDisplay.Visible || !currentDisplay.Enabled)
						return;
					captured.OnClick(null, EventArgs.Empty);
					RefreshAvaloniaDetail();
				});
		}

		private static bool IsPersistentLayoutCommand(ChoiceBase choice)
		{
			switch (choice?.HelpId)
			{
				case "CmdAlwaysVisible":
				case "CmdIfData":
				case "CmdNormallyHidden":
				case "CmdDataTree-MoveFieldUp":
				case "CmdDataTree-MoveFieldDown":
					return true;
				default:
					return false;
			}
		}

		// The adapter fallback needs a raw screen point: cursor position for a right-click,
		// the anchor's bottom-left otherwise. Both corners are mapped since
		// RTL flow mirrors X in PointToScreen.
		private static System.Drawing.Point AdapterMenuScreenPoint(DetailMenuRequest request)
		{
			var anchor = request.AnchorControl;
			if (request.OpenAtPointer || anchor == null)
				return System.Windows.Forms.Cursor.Position;
			var left = Avalonia.VisualExtensions.PointToScreen(anchor,
				new Avalonia.Point(0, anchor.Bounds.Height));
			var right = Avalonia.VisualExtensions.PointToScreen(anchor,
				new Avalonia.Point(anchor.Bounds.Width, anchor.Bounds.Height));
			return new System.Drawing.Point(Math.Min(left.X, right.X), left.Y);
		}

		/// <summary>
		/// Follows a chooser jump link (e.g. "Edit the Publications list" on Publish In) the
		/// EXACT way the legacy chooser does on link click -- the dialog closes, then
		/// <c>ReallySimpleListChooser.HandleAnyJump</c> posts <c>FollowLink</c> with the
		/// <c>FwLinkArgs(tool, guid)</c> built from the layout's <c>chooserLink</c>
		/// (ReallySimpleListChooser.cs:900/1657). Here the flyout has already closed; any open
		/// fenced edit session settles first (the jump navigates away from this record), then the
		/// same mediator message posts.
		/// </summary>
		private void OnDetailLinkRequested(DetailLinkRequest request)
		{
			try
			{
				SettleDetailEdits();
#pragma warning disable 618 // legacy parity: ReallySimpleListChooser.HandleAnyJump posts the same way
				m_mediator.PostMessage("FollowLink", CreateFollowLinkArgs(request));
#pragma warning restore 618
			}
			catch (Exception e)
			{
				Logger.WriteError("Detail chooser link jump failed.", e);
			}
		}

		/// <summary>
		/// The legacy translation: <c>new FwLinkArgs(sTool, m_guidLink)</c> -- the tool from the
		/// layout's chooserLink, the target guid empty unless the link resolved one (none of the
		/// lexeme-editor chooserInfos set <c>flidTextParam</c>, so empty mirrors legacy exactly).
		/// The mapping is unit-testable without a mediator.
		/// </summary>
		internal static FwLinkArgs CreateFollowLinkArgs(DetailLinkRequest request)
		{
			var target = Guid.Empty;
			if (!string.IsNullOrEmpty(request.Link.TargetGuid))
				Guid.TryParse(request.Link.TargetGuid, out target);
			return new FwLinkArgs(request.Link.Tool, target);
		}

		// Approved baseline adapter "command-menu-routing": the hidden legacy DataTree +
		// DTMenuHandler provide the colleague chain and CurrentSlice context the legacy command
		// handlers require. Created lazily on first right-click; never attached/visible while the
		// Avalonia is active.
		private void EnsureMenuCommandAdapter(int targetHvo, string fieldName)
		{
			EnsureMenuCommandTarget(targetHvo, fieldName);
		}

		private bool EnsureMenuCommandTarget(int targetHvo, string fieldName)
		{
			// The active-host contract is enforced, not just documented: driving the hidden
			// legacy DataTree is legal only through an adapter id the host's contract lists. The
			// contract was built from ApprovedBaselineAdapters when the view activated; this
			// site only claims its own id (the fallback covers a menu raised before activation).
			(m_activeHostContract ?? ActiveHostContract.ForAvalonia(ApprovedBaselineAdapters))
				.AssertLegacyDataTreeDriveAllowed(CommandMenuRoutingAdapterId);

			if (!m_dataTreeInitialized)
			{
				EnsureDataTreeInitialized();
				DetachDataTreeFromPanel(); // adapter only: Avalonia stays active
			}
			// Display logic gating on Visible (e.g. OnDisplayDataTreeInsert) treats the hidden
			// adapter tree as active.
			m_dataEntryForm.IsExternalCommandAdapter = true;

			var current = Clerk?.CurrentObject;
			if (current == null)
			{
				// No current record: drop any target left by a previous interaction, same
				// fail-loud rule as the no-slice-found path below.
				m_dataEntryForm.ClearCurrentSlice();
				return false;
			}
			m_dataEntryForm.ShowObject(current, m_layoutName, m_layoutChoiceField, current, true);

			if (targetHvo == 0)
			{
				// The row carries no object, so no slice can be its target.
				m_dataEntryForm.ClearCurrentSlice();
				return false;
			}

			// Targeting hardening: the legacy command handlers act on m_dataEntryForm.CurrentSlice,
			// so the adapter must point CurrentSlice at the slice bound to the clicked row's object. A first
			// pass over the already-realized slices handles the common case (small sequences build their
			// slices instantly). When the target lives inside an UNREALIZED DummyObjectSlice (a sequence with
			// >= DataTree.kInstantSliceMax items builds lazy placeholders whose Object is the OWNER, not the
			// target), no real slice carries the target hvo yet -- realize the lazy slices and
			// retry rather
			// than silently leaving the wrong (or stale) CurrentSlice pointed, which would make the command
			// mutate the wrong object or, for Merge's class guard, silently fail.
			if (TrySetCurrentSliceForRow(targetHvo, fieldName))
				return true;

			if (RealizeLazySlicesAndRetry(targetHvo, fieldName))
				return true;

			// Fail loud, not silent: if we still cannot produce a slice for the target we must NOT leave
			// CurrentSlice pointed at whatever the previous interaction selected (it would mis-target the
			// command). Clear it so command handlers see "no current slice" and no-op, and log so the
			// degradation is diagnosable from the field rather than only in a debugger.
			m_dataEntryForm.ClearCurrentSlice();
			Logger.WriteEvent(string.Format(
				"Detail menu command adapter found no DataTree slice for target hvo {0} field '{1}'; "
				+ "CurrentSlice was cleared so the command no-ops rather than mis-targeting another object.",
				targetHvo, fieldName ?? string.Empty));
			return false;
		}

		private bool EnsurePersistentMenuCommandTarget(DetailField field)
		{
			if (!EnsureMenuCommandTarget(field.ObjectHvo, field.Field))
				return false;
			return TrySetPersistentMenuCommandTarget(field, true);
		}

		private bool TrySetPersistentMenuCommandTarget(DetailField field, bool clearOnFailure)
		{
			var candidates = new List<Slice>();
			foreach (var sliceObj in m_dataEntryForm.Slices)
			{
				if (sliceObj is Slice slice && slice.Object != null && !slice.IsLazyPlaceholder)
					candidates.Add(slice);
			}
			var identities = candidates.Select(slice => PersistentSliceIdentity(slice)).ToList();
			var target = new PersistentCommandTargetIdentity(field.ObjectHvo, field.Field,
				field.ClassName, field.LayoutName, field.SourceCallerPath);
			var index = ChoosePersistentTargetSliceIndex(identities, target);
			if (index < 0)
			{
				if (clearOnFailure)
				{
					m_dataEntryForm.ClearCurrentSlice();
					Logger.WriteEvent(string.Format(
						"Detail layout command found no unique slice for '{0}' at '{1}'; CurrentSlice was cleared.",
						field.Field ?? string.Empty, field.SourceCallerPath ?? string.Empty));
				}
				return false;
			}
			m_dataEntryForm.SetCurrentSliceForCommandTarget(candidates[index]);
			return true;
		}

		internal static int ChoosePersistentTargetSliceIndex(
			IReadOnlyList<PersistentCommandTargetIdentity> candidates,
			PersistentCommandTargetIdentity target)
		{
			if (candidates == null || string.IsNullOrEmpty(target.CallerPath))
				return -1;
			var match = -1;
			for (var i = 0; i < candidates.Count; i++)
			{
				if (!candidates[i].Equals(target))
					continue;
				if (match >= 0)
					return -1;
				match = i;
			}
			return match;
		}

		private PersistentCommandTargetIdentity PersistentSliceIdentity(Slice slice)
		{
			if (slice?.Key == null)
				return default;
			XmlNode layout = null;
			XmlNode part = null;
			foreach (var keyItem in slice.Key)
			{
				if (!(keyItem is XmlNode node))
					continue;
				if (node.Name == "layout")
				{
					layout = node;
					part = null;
				}
				else if (layout != null && node.Name == "part"
					&& node.Attributes?["ref"] != null && LegacyLayoutCallerPath.Get(node) != null)
				{
					part = node;
				}
			}
			return new PersistentCommandTargetIdentity(slice.Object?.Hvo ?? 0, SliceFieldName(slice),
				layout?.Attributes?["class"]?.Value, layout?.Attributes?["name"]?.Value,
				LegacyLayoutCallerPath.Get(part));
		}

		/// <summary>
		/// Picks the slice a menu request targets. Sibling rows can share one object (a MoForm's
		/// Form, Morph Type, ...) while handlers key on <c>CurrentSlice.Flid</c>, so prefer a
		/// match on object AND field; fall back to object alone (headers, ghosts, custom rows).
		/// Returns -1 when nothing matches.
		/// </summary>
		internal static int ChooseTargetSliceIndex(IReadOnlyList<(int Hvo, string FieldName)> candidates,
			int targetHvo, string fieldName)
		{
			if (candidates == null)
				return -1;

			if (!string.IsNullOrEmpty(fieldName))
			{
				for (var i = 0; i < candidates.Count; i++)
				{
					if (candidates[i].Hvo == targetHvo
						&& string.Equals(candidates[i].FieldName, fieldName, StringComparison.Ordinal))
					{
						return i;
					}
				}
			}

			for (var i = 0; i < candidates.Count; i++)
			{
				if (candidates[i].Hvo == targetHvo)
					return i;
			}
			return -1;
		}

		// Targets the best realized slice for the row; false when nothing matches (the
		// target may still sit inside a lazy placeholder).
		private bool TrySetCurrentSliceForRow(int targetHvo, string fieldName)
		{
			// Not filtered on Slice.IsRealSlice: for a view slice that is
			// RootSite.AllowLayout, false forever in a tree that never lays out. Lazy
			// placeholders report their OWNER, so exclude them by type instead.
			var candidates = new List<Slice>();
			foreach (var sliceObj in m_dataEntryForm.Slices)
			{
				if (sliceObj is Slice slice && slice.Object != null && !slice.IsLazyPlaceholder)
					candidates.Add(slice);
			}

			// Field names are only ever compared for hvo matches, so skip the metadata
			// lookup for every other slice (and entirely when no field name was requested).
			var index = ChooseTargetSliceIndex(
				candidates.Select(s => (
					s.Object.Hvo,
					s.Object.Hvo == targetHvo && !string.IsNullOrEmpty(fieldName)
						? SliceFieldName(s)
						: null)).ToList(),
				targetHvo, fieldName);
			if (index < 0)
				return false;

			// ShowObject suspends ordinary CurrentSlice assignment until idle; the
			// command-target setter applies immediately.
			m_dataEntryForm.SetCurrentSliceForCommandTarget(candidates[index]);
			return true;
		}

		// The model field name a slice edits, or null (header/object rows, unknown flids).
		private string SliceFieldName(Slice slice)
		{
			var flid = slice.Flid;
			// A ViewPropertySlice can carry its field only in FieldId, with no "field"
			// attribute on its configuration node.
			if (flid == 0 && slice is ViewPropertySlice viewPropertySlice)
				flid = viewPropertySlice.FieldId;
			// Virtual and decorator-only flids are absent from the MDC and never field-match.
			var mdc = (IFwMetaDataCacheManaged)Cache.MetaDataCacheAccessor;
			return flid != 0 && mdc.FieldExists(flid) ? mdc.GetFieldName(flid) : null;
		}

		/// <summary>
		/// Expands every lazy placeholder in place, then retries the match: a placeholder reports
		/// its OWNER as its Object, so the target cannot match until expansion. Walks by index
		/// because the collection mutates as placeholders expand.
		/// </summary>
		private bool RealizeLazySlicesAndRetry(int targetHvo, string fieldName)
		{
			try
			{
				for (var i = 0; i < m_dataEntryForm.Slices.Count; i++)
				{
					if (m_dataEntryForm.Slices[i] is Slice slice && slice.IsLazyPlaceholder)
						m_dataEntryForm.FieldAt(i); // expands the placeholder at i in place
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Realizing lazy DataTree slices for detail command targeting failed.", e);
				return false;
			}

			return TrySetCurrentSliceForRow(targetHvo, fieldName);
		}

		private bool? GetPersistedExpansionState(string stableId)
		{
			if (m_expansionStates.TryGetValue(stableId, out var expanded))
				return expanded;
			var stored = m_propertyTable?.GetStringProperty("LexEditExpansion:" + stableId, null,
				PropertyTable.SettingsGroup.LocalSettings);
			return stored == null ? (bool?)null : stored == "1";
		}

		private void PersistExpansionState(string stableId, bool expanded)
		{
			m_expansionStates[stableId] = expanded;
			if (m_propertyTable == null)
				return;
			var key = "LexEditExpansion:" + stableId;
			m_propertyTable.SetProperty(key, expanded ? "1" : "0", PropertyTable.SettingsGroup.LocalSettings, false);
			m_propertyTable.SetPropertyPersistence(key, true, PropertyTable.SettingsGroup.LocalSettings);
		}

		// Viewing parity: the label/value splitter width persists per tool -- in-session
		// through the host's remembered field, ACROSS sessions through a PropertyTable local setting,
		// mirroring the expansion-persistence pattern above and the legacy slice-splitter behavior
		// (a host-only field would be process-scoped and lost on shutdown). Keyed by tool so each
		// detail tool keeps its own column width. Returns null when nothing has been persisted yet,
		// so the view falls back to the density default.
		private string LabelColumnWidthKey
			=> "LexEditLabelColumnWidth:" + m_propertyTable?.GetStringProperty("currentContentControl", string.Empty);

		private double? GetPersistedLabelColumnWidth()
		{
			var stored = m_propertyTable?.GetStringProperty(LabelColumnWidthKey, null,
				PropertyTable.SettingsGroup.LocalSettings);
			if (string.IsNullOrEmpty(stored))
				return null;
			// Invariant culture so a width written under one locale parses under another.
			return double.TryParse(stored, System.Globalization.NumberStyles.Float,
				System.Globalization.CultureInfo.InvariantCulture, out var width) && width > 0
				? (double?)width
				: null;
		}

		private void PersistLabelColumnWidth(double width)
		{
			if (m_propertyTable == null || width <= 0)
				return;
			var key = LabelColumnWidthKey;
			m_propertyTable.SetProperty(key,
				width.ToString(System.Globalization.CultureInfo.InvariantCulture),
				PropertyTable.SettingsGroup.LocalSettings, false);
			m_propertyTable.SetPropertyPersistence(key, true, PropertyTable.SettingsGroup.LocalSettings);
		}

		// Re-resolves and re-shows the detail view for the current record from current domain state
		// (after an external edit or this view's commit/cancel).
		private void RefreshAvaloniaDetail()
		{
			if (m_avaloniaEntryForm == null || !ShouldUseAvaloniaLexiconEdit)
				return;
			var current = Clerk?.CurrentObject;
			if (current == null)
				return;

			ShowAvaloniaEntry(current);
		}

		private void OnAvaloniaDetailEditCompleted(object sender, EventArgs e)
		{
			// ONE re-show covers the completed edit AND any refresh held during it: drop the held
			// delivery and request a single coalesced refresh through the controller's queue.
			if (m_avaloniaRefreshController != null)
			{
				m_avaloniaRefreshController.DiscardHeldRefresh();
				m_avaloniaRefreshController.RequestRefresh();
			}
			else
			{
				RefreshAvaloniaDetail();
			}
		}

		// Assigns the resolved framework and keeps the active-host contract in
		// lockstep -- reflecting it from construction on, not only after first
		// activation, since a headless host may never activate.
		private void SetUIFramework(UIFramework framework)
		{
			m_activeUIFramework = framework;
			SyncActiveHostContract();
		}

		private void SyncActiveHostContract()
		{
			var kind = ShouldUseAvaloniaLexiconEdit
				? UIFramework.Avalonia
				: UIFramework.Legacy;
			if (m_activeHostContract == null || m_activeHostContract.ActiveUIFramework != kind)
			{
				m_activeHostContract = ShouldUseAvaloniaLexiconEdit
					? ActiveHostContract.ForAvalonia(ApprovedBaselineAdapters)
					: ActiveHostContract.ForLegacy();
			}
		}

		private void EnsureAvaloniaEntryFormActive()
		{
			// Re-sync the contract BEFORE realizing the entry form so it reflects the activation
			// even
			// if its construction fails part-way.
			SyncActiveHostContract();

			if (m_avaloniaEntryForm == null)
				EnsureAvaloniaEntryFormInitialized();

			// The refresh controller must exist for the whole time the view is active,
			// not only once a record has actually been composed via ShowAvaloniaEntry. A tool that
			// loads directly with UIMode=New (the ordinary case for a user who already has the setting
			// on) shows the entry form here on the first idle -- and when the clerk has not yet
			// selected a
			// record that first show takes the CurrentObject==null branch, which never reaches
			// ShowAvaloniaEntry. Without wiring the controller here, PropChanged-driven external-edit
			// refresh would silently not work until the user manually navigated to another record once.
			// EnsureAvaloniaRefreshController is idempotent (its m_avaloniaRefreshController != null guard),
			// so the later ShowAvaloniaEntry call is a no-op rather than a duplicate registration.
			EnsureAvaloniaRefreshController();

			DetachDataTreeFromPanel();
			m_avaloniaEntryForm.Show();
			m_avaloniaEntryForm.BringToFront();
		}
	}
}
