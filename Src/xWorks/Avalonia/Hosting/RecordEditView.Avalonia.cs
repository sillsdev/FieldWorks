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
			private readonly DetailLayoutSliceIdentity _sliceIdentity;

			internal PersistentCommandTargetIdentity(int hvo, string fieldName,
				string className, string layoutName, string callerPath,
				string layoutType = null, string choiceGuid = null,
				IReadOnlyList<DetailLayoutIdentity> layoutPath = null)
			{
				var layoutIdentity = new DetailLayoutIdentity(className, layoutType, layoutName,
					choiceGuid);
				var layoutPartIdentity = new DetailLayoutPartIdentity(layoutIdentity, callerPath,
					layoutPath);
				_sliceIdentity = new DetailLayoutSliceIdentity(layoutPartIdentity, hvo, fieldName);
			}

			internal int Hvo => _sliceIdentity.ObjectHvo;

			internal string FieldName => _sliceIdentity.FieldName;

			internal string ClassName => _sliceIdentity.LayoutPart.Layout.ClassName;

			internal string LayoutName => _sliceIdentity.LayoutPart.Layout.LayoutName;

			internal string CallerPath => _sliceIdentity.LayoutPart.CallerPath;
			internal string LayoutType => _sliceIdentity.LayoutPart.Layout.LayoutType;
			internal string ChoiceGuid => _sliceIdentity.LayoutPart.Layout.ChoiceGuid;
			internal IReadOnlyList<DetailLayoutIdentity> LayoutPath
				=> _sliceIdentity.LayoutPart.LayoutPath;

			public bool Equals(PersistentCommandTargetIdentity other)
				=> _sliceIdentity.Equals(other._sliceIdentity);

			public override bool Equals(object obj)
				=> obj is PersistentCommandTargetIdentity other && Equals(other);

			public override int GetHashCode() => _sliceIdentity.GetHashCode();
		}

		private readonly struct PersistentLazyTargetMatch
		{
			internal PersistentLazyTargetMatch(Slice slice, int index)
			{
				Slice = slice;
				Index = index;
			}

			internal Slice Slice { get; }
			internal int Index { get; }
		}

		private sealed class PersistentTargetMatches
		{
			internal readonly List<Slice> Realized = new List<Slice>();
			internal readonly List<PersistentLazyTargetMatch> Lazy
				= new List<PersistentLazyTargetMatch>();
		}

		internal enum PersistentTargetAction
		{
			Reject,
			UseRealized,
			RealizeLazy
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
		private DetailLayoutSliceIdentity? m_showAllWritingSystemsSlice;
		private bool m_pendingDetailFocusRefresh;
		private int m_detailFocusRefreshVersion;
		private int m_lastAvaloniaCurrentObjectHvo;

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
			m_showAllWritingSystemsSlice = null;
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
			m_pendingDetailFocusRefresh = false;
			m_detailFocusRefreshVersion++;
			// Auto-save: a session still open from the previous record/edit settles (commit
			// when valid, roll back when not) before the detail view is replaced. Replace's
			// cancel-on-displace remains the safety net.
			SettleDetailEdits();
			var currentObjectHvo = Clerk?.CurrentObject?.Hvo ?? 0;
			if (currentObjectHvo != m_lastAvaloniaCurrentObjectHvo)
				m_showAllWritingSystemsSlice = null;
			m_lastAvaloniaCurrentObjectHvo = currentObjectHvo;
			if (obj == null || m_dataEntryForm?.Root == null
				|| m_dataEntryForm.Root.Hvo != obj.Hvo)
			{
				m_showAllWritingSystemsSlice = null;
			}

			// Adapter hygiene: the hidden command-routing DataTree must never answer mediator
			// commands for a PREVIOUS record -- reset it whenever the shown record changes; the
			// next
			// right-click re-syncs it through the targeting path. Without this, Insert Sense from
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
				composed = DetailComposer.Compose(obj, Cache, m_layoutName, showHidden,
					source: source.GetSnapshot,
					layoutChoiceField: m_layoutChoiceField,
					showAllWritingSystemsSlices: RevealedWritingSystemSlices());
				if (composed != null)
				{
					if (m_showAllWritingSystemsSlice.HasValue
						&& !composed.Model.Fields.Any(field => field.LayoutSliceIdentity.Equals(
							m_showAllWritingSystemsSlice.Value)))
					{
						m_showAllWritingSystemsSlice = null;
					}
					detail = composed.Model;
					editContext = composed.EditContext;
				}
			}
			catch (LayoutNotFoundException)
			{
				throw;
			}
			catch (Exception e)
			{
				Logger.WriteError("Avalonia detail composition failed; using the host fallback.", e);
			}

			if (detail == null)
			{
				m_showAllWritingSystemsSlice = null;
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
				GetPersistedLabelColumnWidth, PersistLabelColumnWidth,
				OnDetailFieldFocused);
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
			var parts = Inventory.GetInventory("parts", Cache.ProjectId.Name);
			if (parts?.Root == null)
				throw new InvalidOperationException("The project part inventory is unavailable.");

			m_inventoryViewDefinitionSource = new InventoryViewDefinitionSource(layouts,
				parts.Root.OuterXml,
				Cache.MetaDataCacheAccessor, Cache);
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
			var refreshAfterMenu = TakePendingDetailFocusRefresh()
				| ClearShowAllFor(request.Field);
			try
			{
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
				// Render the SAME xCore menu natively in Avalonia -- identical items,
				// enablement, and mediator dispatch; only rendering changes.
				try
				{
					var items = CreateNativeDetailMenuItems(request.Field, idArray);
					if (items.Count > 0)
					{
						// A keyboard-opened menu anchors under the row it came from; a
						// right-click opens it at the pointer.
						var shown = m_avaloniaEntryForm.ShowContextMenu(items,
							request.AnchorControl, request.OpenAtPointer,
							refreshAfterMenu ? (Action)RefreshAvaloniaDetail : null);
						if (shown)
						{
							refreshAfterMenu = false;
							return;
						}
					}
				}
				catch (Exception nativeMenuError)
				{
					Logger.WriteError("Avalonia-native menu failed; the menu was not shown.",
						nativeMenuError);
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Detail context menu failed.", e);
			}
			finally
			{
				if (refreshAfterMenu)
					RefreshAvaloniaDetail();
			}
		}

		// Preserve the legacy command-routing entry point; all targeting still flows through the
		// invariant-enforcing path that clears stale CurrentSlice state on failure.
		private void EnsureMenuCommandAdapter(int targetHvo, string fieldName)
		{
			EnsureMenuCommandTarget(targetHvo, fieldName);
		}

		private IReadOnlyList<DetailMenuItem> CreateNativeDetailMenuItems(DetailField field,
			string[] menuIds)
		{
			var window = m_propertyTable.GetValue<XWindow>("window");
			var hasPersistentTarget = false;
			try
			{
				hasPersistentTarget = EnsurePersistentMenuCommandTarget(field);
			}
			catch (Exception adapterError)
			{
				Logger.WriteError("Detail menu command adapter failed; menu items that need "
					+ "the hidden colleague chain will be disabled.", adapterError);
			}
			return XCoreMenuBridge.CreateMenuItems(window, menuIds,
				(choice, display) => CreateLegacyCommandMenuItem(field, choice, display,
					hasPersistentTarget));
		}

		internal void OnDetailFieldFocused(DetailField field)
		{
			if (ClearShowAllFor(field))
			{
				m_pendingDetailFocusRefresh = true;
				var version = ++m_detailFocusRefreshVersion;
				Avalonia.Threading.Dispatcher.UIThread.Post(() =>
				{
					if (!m_pendingDetailFocusRefresh || version != m_detailFocusRefreshVersion)
						return;
					m_pendingDetailFocusRefresh = false;
					RefreshAvaloniaDetail();
				},
					Avalonia.Threading.DispatcherPriority.Background);
			}
		}

		private bool TakePendingDetailFocusRefresh()
		{
			if (!m_pendingDetailFocusRefresh)
				return false;
			m_pendingDetailFocusRefresh = false;
			m_detailFocusRefreshVersion++;
			return true;
		}

		private bool ClearShowAllFor(DetailField field)
		{
			if (!m_showAllWritingSystemsSlice.HasValue || field == null
				|| m_showAllWritingSystemsSlice.Value.Equals(field.LayoutSliceIdentity))
			{
				return false;
			}
			m_showAllWritingSystemsSlice = null;
			return true;
		}

		private ISet<DetailLayoutSliceIdentity> RevealedWritingSystemSlices()
			=> m_showAllWritingSystemsSlice.HasValue
				? new HashSet<DetailLayoutSliceIdentity> { m_showAllWritingSystemsSlice.Value }
				: null;

		private DetailMenuItem CreateLegacyCommandMenuItem(DetailField field, ChoiceBase choice,
			UIItemDisplayProperties display, bool hasPersistentTarget)
		{
			if (string.Equals(choice?.HelpId,
				"CmdDataTree-WritingSystemMenu-ShowAllRightNow", StringComparison.Ordinal))
			{
				return new DetailMenuItem(XCoreMenuBridge.StripAccelerator(display.Text),
					hasPersistentTarget && display.Enabled, false, null,
					hasPersistentTarget && display.Enabled ? (Action)(() =>
					{
						if (!EnsurePersistentMenuCommandTarget(field))
							return;
						m_showAllWritingSystemsSlice = field.LayoutSliceIdentity;
						RefreshAvaloniaDetail();
					}) : null);
			}

			var persistent = IsPersistentLayoutCommand(choice)
				|| IsWritingSystemVisibilityChoice(choice);
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
					var before = persistent ? PersistentLayoutXml(field) : null;
					captured.OnClick(null, EventArgs.Empty);
					if (persistent && !string.Equals(before, PersistentLayoutXml(field),
						StringComparison.Ordinal))
					{
						m_showAllWritingSystemsSlice = null;
					}
					RefreshAvaloniaDetail();
				});
		}

		private static bool IsWritingSystemVisibilityChoice(ChoiceBase choice)
		{
			if (choice is ListPropertyChoice list)
			{
				return string.Equals(list.ParentProperty,
					PropertyConstants.CurrentContextMenuSelectedWsIds, StringComparison.Ordinal);
			}
			return string.Equals(choice?.HelpId,
				"CmdDataTree-WritingSystemMenu-Configure", StringComparison.Ordinal);
		}

		private string PersistentLayoutXml(DetailField field)
		{
			var layout = Inventory.GetInventory("layouts", Cache.ProjectId.Name)?.GetElement("layout",
				new[] { field.ClassName, field.LayoutType, field.LayoutName, field.ChoiceGuid });
			return layout?.OuterXml;
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

		private bool EnsureMenuCommandAdapterInitialized()
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
			var shown = ResolveShownRecord(current);
			m_dataEntryForm.ShowObject(shown, m_layoutName, m_layoutChoiceField, current, true);
			return true;
		}

		private bool EnsureMenuCommandTarget(int targetHvo, string fieldName)
		{
			if (!EnsureMenuCommandAdapterInitialized())
				return false;

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
			if (!EnsureMenuCommandAdapterInitialized())
				return false;
			return EnsurePersistentMenuCommandTargetAfterExactMiss(field);
		}

		private bool EnsurePersistentMenuCommandTargetAfterExactMiss(DetailField field)
		{
			PersistentTargetMatches matches;
			try
			{
				matches = EnumeratePersistentTargetMatches(field);
			}
			catch (Exception e)
			{
				Logger.WriteError("Enumerating exact persistent DataTree target matches failed.", e);
				return FailPersistentTarget(field, 0, 0);
			}
			return ExecutePersistentTargetArbitration(matches.Realized.Count, matches.Lazy.Count,
				() =>
				{
					m_dataEntryForm.SetCurrentSliceForCommandTarget(matches.Realized[0]);
					return true;
				},
				() => RealizeExactLazyOccurrence(matches.Lazy[0]),
				() => TrySetPersistentMenuCommandTarget(field),
				() => FailPersistentTarget(field, matches.Realized.Count, matches.Lazy.Count));
		}

		private PersistentTargetMatches EnumeratePersistentTargetMatches(DetailField field)
		{
			var matches = new PersistentTargetMatches();
			if (!IsValidPersistentTarget(field))
				return matches;

			var target = PersistentTargetIdentity(field);
			for (var i = 0; i < m_dataEntryForm.Slices.Count; i++)
			{
				var slice = m_dataEntryForm.Slices[i] as Slice;
				if (slice == null)
					continue;
				if (slice.IsLazyPlaceholder)
				{
					if (IsExactLazyOccurrence(slice, field))
						matches.Lazy.Add(new PersistentLazyTargetMatch(slice, i));
				}
				else if (slice.Object != null && PersistentSliceIdentity(slice).Equals(target))
				{
					matches.Realized.Add(slice);
				}
			}
			return matches;
		}

		private bool IsValidPersistentTarget(DetailField field)
		{
			if (field == null || field.ObjectHvo == 0 || string.IsNullOrEmpty(field.Field)
				|| string.IsNullOrEmpty(field.ClassName) || field.LayoutName == null
				|| string.IsNullOrEmpty(field.SourceCallerPath)
				|| field.LayoutPath == null || field.LayoutPath.Count == 0)
				return false;

			try
			{
				var target = Cache.ServiceLocator.GetObject(field.ObjectHvo);
				return target != null
					&& Cache.MetaDataCacheAccessor.GetFieldId2(target.ClassID, field.Field, true) != 0;
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static PersistentCommandTargetIdentity PersistentTargetIdentity(DetailField field)
			=> new PersistentCommandTargetIdentity(field.ObjectHvo, field.Field, field.ClassName,
				field.LayoutName, field.SourceCallerPath, field.LayoutType, field.ChoiceGuid,
				field.LayoutPath);

		private bool RealizeExactLazyOccurrence(PersistentLazyTargetMatch match)
		{
			try
			{
				if (match.Slice == null || !match.Slice.IsLazyPlaceholder
					|| match.Index < 0 || match.Index >= m_dataEntryForm.Slices.Count
					|| !ReferenceEquals(m_dataEntryForm.Slices[match.Index], match.Slice))
					return false;

				m_dataEntryForm.FieldAt(match.Index);
				return true;
			}
			catch (Exception e)
			{
				Logger.WriteError("Realizing the exact lazy DataTree slice for persistent menu targeting failed.", e);
				return false;
			}
		}

		private bool IsExactLazyOccurrence(Slice slice, DetailField field)
		{
			try
			{
				if (slice.Object == null || slice.LazySequenceFlid == 0
					|| slice.LazySequenceIndex < 0)
					return false;

				var count = Cache.DomainDataByFlid.get_VecSize(slice.Object.Hvo,
					slice.LazySequenceFlid);
				if (slice.LazySequenceIndex >= count
					|| Cache.DomainDataByFlid.get_VecItem(slice.Object.Hvo,
						slice.LazySequenceFlid, slice.LazySequenceIndex) != field.ObjectHvo)
					return false;

				return LazyPathMatchesTarget(slice.LazySequencePath, field);
			}
			catch (Exception)
			{
				return false;
			}
		}

		private static bool LazyPathMatchesTarget(object[] lazyPath, DetailField field)
		{
			if (lazyPath == null || field == null)
				return false;

			var lazyLayouts = new List<DetailLayoutIdentity>();
			var callerPaths = new List<string>();
			string callerInLayout = null;
			foreach (var pathPart in lazyPath)
			{
				var node = pathPart as XmlNode;
				if (node == null)
					continue;
				if (node.Name == "sublayout")
				{
					lazyLayouts.Clear();
					callerPaths.Clear();
					callerInLayout = null;
				}
				else if (node.Name == "layout")
				{
					if (callerInLayout != null)
						callerPaths.Add(callerInLayout);
					callerInLayout = null;
					lazyLayouts.Add(new DetailLayoutIdentity(node.Attributes?["class"]?.Value,
						node.Attributes?["type"]?.Value, node.Attributes?["name"]?.Value,
						node.Attributes?["choiceGuid"]?.Value));
				}
				else if (node.Name == "part" && node.Attributes?["ref"] != null)
				{
					callerInLayout = LegacyLayoutCallerPath.Get(node) ?? callerInLayout;
				}
				else if ((node.Name == "obj" || node.Name == "seq") && callerInLayout == null)
				{
					callerInLayout = LegacyLayoutCallerPath.Get(node);
				}
			}
			if (callerInLayout != null)
				callerPaths.Add(callerInLayout);

			var lazyCallerPath = LegacyLayoutCallerPath.Combine(callerPaths.ToArray());
			if (string.IsNullOrEmpty(lazyCallerPath) || string.IsNullOrEmpty(field.SourceCallerPath)
				|| (!string.Equals(field.SourceCallerPath, lazyCallerPath, StringComparison.Ordinal)
					&& !field.SourceCallerPath.StartsWith(lazyCallerPath + "|",
						StringComparison.Ordinal)))
				return false;

			var targetLayouts = field.LayoutPath;
			if (lazyLayouts.Count == 0 || targetLayouts == null
				|| targetLayouts.Count < lazyLayouts.Count)
				return false;
			for (var i = 0; i < lazyLayouts.Count; i++)
			{
				if (!lazyLayouts[i].Equals(targetLayouts[i]))
					return false;
			}
			return true;
		}

		private bool TrySetPersistentMenuCommandTarget(DetailField field)
		{
			if (!IsValidPersistentTarget(field))
				return FailPersistentTarget(field, 0, 0);

			try
			{
				var candidates = new List<Slice>();
				var target = PersistentTargetIdentity(field);
				foreach (var sliceObj in m_dataEntryForm.Slices)
				{
					if (sliceObj is Slice slice && slice.Object != null && !slice.IsLazyPlaceholder
						&& PersistentSliceIdentity(slice).Equals(target))
						candidates.Add(slice);
				}
				if (ArbitratePersistentTarget(candidates.Count, 0)
					!= PersistentTargetAction.UseRealized)
					return FailPersistentTarget(field, candidates.Count, 0);

				m_dataEntryForm.SetCurrentSliceForCommandTarget(candidates[0]);
				return true;
			}
			catch (Exception e)
			{
				Logger.WriteError("Rescanning exact realized persistent DataTree targets failed.", e);
				return FailPersistentTarget(field, 0, 0);
			}
		}

		private bool FailPersistentTarget(DetailField field, int realizedCount, int lazyCount)
		{
			m_dataEntryForm.ClearCurrentSlice();
			Logger.WriteEvent(string.Format(
				"Detail layout command found {0} exact realized and {1} exact lazy target(s) for "
				+ "'{2}' at '{3}'; CurrentSlice was cleared.", realizedCount, lazyCount,
				field?.Field ?? string.Empty, field?.SourceCallerPath ?? string.Empty));
			return false;
		}

		internal static PersistentTargetAction ArbitratePersistentTarget(int realizedCount,
			int lazyCount)
		{
			if (realizedCount < 0 || lazyCount < 0 || realizedCount + lazyCount != 1)
				return PersistentTargetAction.Reject;
			return realizedCount == 1
				? PersistentTargetAction.UseRealized
				: PersistentTargetAction.RealizeLazy;
		}

		internal static bool ExecutePersistentTargetArbitration(int realizedCount, int lazyCount,
			Func<bool> useRealized, Func<bool> realizeLazy, Func<bool> rescanRealized,
			Func<bool> reject)
		{
			switch (ArbitratePersistentTarget(realizedCount, lazyCount))
			{
				case PersistentTargetAction.UseRealized:
					return useRealized();
				case PersistentTargetAction.RealizeLazy:
					if (!realizeLazy())
						return reject();
					return rescanRealized();
				default:
					return reject();
			}
		}

		internal PersistentCommandTargetIdentity PersistentSliceIdentity(Slice slice)
		{
			if (slice?.Key == null)
				return default;
			var start = 0;
			for (var i = slice.Key.Length - 1; i > 0; i--)
			{
				if (slice.Key[i] is XmlNode node && node.Name == "sublayout")
				{
					start = i + 1;
					break;
				}
			}
			var layout = start < slice.Key.Length ? slice.Key[start] as XmlNode : null;
			if (layout?.Name != "layout")
				return default;
			var layoutPaths = new List<string>();
			var selectedLayouts = new List<DetailLayoutIdentity> { LayoutIdentity(layout) };
			string lastPartPath = null;
			for (var i = start + 1; i < slice.Key.Length; i++)
			{
				if (!(slice.Key[i] is XmlNode node))
					continue;
				if (node.Name == "layout")
				{
					if (!string.IsNullOrEmpty(lastPartPath))
						layoutPaths.Add(lastPartPath);
					lastPartPath = null;
					selectedLayouts.Add(LayoutIdentity(node));
				}
				else if (node.Name == "part" && node.Attributes?["ref"] != null)
				{
					lastPartPath = LegacyLayoutCallerPath.Get(node) ?? lastPartPath;
				}
			}
			if (!string.IsNullOrEmpty(lastPartPath))
				layoutPaths.Add(lastPartPath);
			return new PersistentCommandTargetIdentity(slice.Object?.Hvo ?? 0, SliceFieldName(slice),
				layout?.Attributes?["class"]?.Value, layout?.Attributes?["name"]?.Value,
				LegacyLayoutCallerPath.Combine(layoutPaths.ToArray()), layout?.Attributes?["type"]?.Value,
				layout?.Attributes?["choiceGuid"]?.Value, selectedLayouts);
		}

		private static DetailLayoutIdentity LayoutIdentity(XmlNode layout)
			=> new DetailLayoutIdentity(layout?.Attributes?["class"]?.Value,
				layout?.Attributes?["type"]?.Value, layout?.Attributes?["name"]?.Value,
				layout?.Attributes?["choiceGuid"]?.Value);

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
		private bool RealizeLazySlices()
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

			return true;
		}

		private bool RealizeLazySlicesAndRetry(int targetHvo, string fieldName)
		{
			if (!RealizeLazySlices())
				return false;
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

		// Re-shows the detail view for the current record (external edit, commit/cancel).
		// Resolve it like ShowRecord so a showDescendantInRoot tool recomposes the root.
		private void RefreshAvaloniaDetail()
		{
			if (m_avaloniaEntryForm == null || !ShouldUseAvaloniaLexiconEdit)
				return;
			var current = ResolveShownRecord(Clerk?.CurrentObject);
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
