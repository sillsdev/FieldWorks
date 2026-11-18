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
using SIL.FieldWorks.Common.DetailRules;
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
	public partial class RecordEditView : IRefreshableRoot
	{
		/// <summary>
		/// Recomposes the Avalonia detail view when the window refreshes, as after View >
		/// Refresh or the Styles dialog (LT-22811); an open edit completes first. Always false,
		/// so the views this one contains still refresh themselves.
		/// </summary>
		public bool RefreshDisplay()
		{
			if (ShouldUseAvaloniaLexiconEdit)
				m_avaloniaRefreshController?.RequestRefresh();
			return false;
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
		// The per-project home of the sparse view-definition override patches that
		// drive the Avalonia detail view's per-field Field Visibility / Move Field commands. Lazily built from
		// the project ConfigurationSettings folder; the detail view reads it at Compose and the gear
		// menu writes it. The legacy WinForms DataTree path NEVER touches this -- it keeps its
		// Inventory
		// store untouched.
		private ViewDefinitionOverrideStore m_viewOverrideStore;
		// The active-host contract for the current framework, kept in sync by SetUIFramework.
		// While Avalonia is active, nothing may build or drive the WinForms DataTree.
		private ActiveHostContract m_activeHostContract;

		// Expansion state by DetailField.ExpansionKey for this session; a PropertyTable local
		// setting carries it across sessions. Per-instance, so no state leaks across windows.
		private readonly Dictionary<string, bool> m_expansionStates = new Dictionary<string, bool>();

		/// <summary>The PropertyTable local-setting name prefix of a persisted expansion
		/// state.</summary>
		internal const string ExpansionSettingPrefix = "LexEditExpansion:";

		// The transient "Show all right now" reveal: template StableIds of parts whose rows
		// show every writing-system option. Cleared when the shown record changes; never
		// persisted.
		private readonly HashSet<string> m_showAllWsFields = new HashSet<string>(StringComparer.Ordinal);
		// The record m_showAllWsFields belongs to; a different record expires the reveal.
		private int m_showAllWsRecordHvo;
		// The record the Avalonia view shows now, and the objects its rows are bound to: what
		// a change must touch to refresh the view. Null and empty while no record is shown.
		private ICmObject m_shownRecord;
		private HashSet<int> m_shownRowObjects = new HashSet<int>();
		// The clerk's current object when the shown record was shown: a subrecord, when the
		// tool shows a current subrecord inside its root. 0 while no record is shown.
		private int m_shownForHvo;

		private bool ShouldUseAvaloniaLexiconEdit
		{
			get { return m_activeUIFramework == UIFramework.Avalonia; }
		}

		// Memoized, a failed read included: a missing filter file is an install fault, and
		// re-reading it would log the failure for every record shown.
		private ISet<string> m_hiddenSliceIds;

		/// <summary>The slice ids this tool's filter list withholds.</summary>
		private ISet<string> HiddenSliceIds
			=> m_hiddenSliceIds ?? (m_hiddenSliceIds = ReadSliceFilterIds(m_configurationParameters));

		/// <summary>
		/// The slice ids named by the filter list a tool's configuration points at through its
		/// filterPath. Empty for a configuration that names none, and empty when the file cannot
		/// be read: a detail view showing an extra row beats one that will not open.
		/// </summary>
		/// <param name="configuration">The tool's configuration parameters; null yields an empty
		/// set.</param>
		internal static ISet<string> ReadSliceFilterIds(XmlNode configuration)
		{
			var ids = new HashSet<string>(StringComparer.Ordinal);
			try
			{
				var filterPath = XmlUtils.GetOptionalAttributeValue(configuration, "filterPath");
				if (string.IsNullOrEmpty(filterPath))
					return ids;
				if (!Platform.IsWindows)
					filterPath = filterPath.Replace(@"\", "/");

				var document = new XmlDocument();
				document.Load(FwDirectoryFinder.GetCodeFile(filterPath));
				foreach (XmlNode node in document.SelectNodes("SliceFilter/node"))
				{
					var id = XmlUtils.GetOptionalAttributeValue(node, "id");
					if (!string.IsNullOrEmpty(id))
						ids.Add(id);
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Reading the tool's slice filter failed; no row is withheld "
					+ "by it.", e);
			}

			return ids;
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
			UnregisterCommandAuthority();
			if (m_avaloniaEntryForm != null)
			{
				m_avaloniaEntryForm.DetailEditCompleted -= OnAvaloniaDetailEditCompleted;
				m_avaloniaEntryForm.DetailInteractionCompleted -= OnAvaloniaInteractionCompleted;
			}
			m_detailEditContext.DetachDeactivateHook();
			m_detailEditContext.DetachUndoGuard();
			m_detailEditContext.InvalidEditRolledBack = null;
			m_avaloniaRefreshController?.Dispose();
			m_refreshQuietTimer?.Dispose();
			m_refreshQuietTimer = null;
			m_afterRefreshQuiet = null;
			SettleDetailEdits();
			m_detailEditContext.Clear();
			m_avaloniaEntryForm?.Dispose();
			// Transient view state dies with the view: rebuilding the host must not
			// resurrect a stale reveal.
			m_showAllWsFields.Clear();
			m_showAllWsRecordHvo = 0;
			SetShownRecord(null, null);
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
			m_avaloniaEntryForm.DetailInteractionCompleted += OnAvaloniaInteractionCompleted;
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
				// The record on display, which is the top-level record when the tool shows a
				// current subrecord inside its root.
				() => RefreshRecord,
				// "Busy" means an open session OR a click in flight: Commit clears IsOpen BEFORE
				// EndUndoTask raises PropChanged, so its own notification would recompose
				// midclick.
				() => m_detailEditContext.Current?.IsOpen == true
					|| m_avaloniaEntryForm?.IsDetailInteractionInFlight == true,
				RefreshAvaloniaDetail,
				new RefreshCoordinator(),
				ScheduleOnUiThread,
				// This host's relevance rule; the controller itself stays host-agnostic.
				changed => IsChangeWithinEntry(changed, RefreshRecord, m_shownRowObjects),
				DeferUntilQuiet);
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
		/// The host's refresh relevance: a change is relevant when the changed object is, or is
		/// owned by, the record on display, or an object one of its rows is bound to (in
		/// <paramref name="shownRowObjects"/>; null for none). Bound objects reach what the
		/// record only references, such as a notebook record's Text (LT-22692). This is the
		/// predicate the host injects into <see cref="AvaloniaDetailRefreshController"/>; static
		/// and internal so it is unit-testable without a live view.
		/// </summary>
		internal static bool IsChangeWithinEntry(ICmObject changed, ICmObject current,
			ICollection<int> shownRowObjects = null)
		{
			if (changed == null || current == null)
				return false;
			// Walking the owner chain (rather than checking only the root) keeps this correct for
			// any record class, not just entries, so nested edits also trigger the coalesced
			// refresh.
			for (var o = changed; o != null; o = o.Owner)
			{
				if (o.Hvo == current.Hvo || (shownRowObjects?.Contains(o.Hvo) ?? false))
					return true;
			}
			return false;
		}

		// How long a burst of changes made outside the detail view must go quiet before the
		// view rebuilds once for the rest of it, in milliseconds (LT-22692).
		private const int RefreshQuietMilliseconds = 250;

		private System.Windows.Forms.Timer m_refreshQuietTimer;
		private Action m_afterRefreshQuiet;

		/// <summary>
		/// Runs <paramref name="action"/> on the UI thread once RefreshQuietMilliseconds pass
		/// with no further call; each call restarts the wait and replaces the action.
		/// </summary>
		internal void DeferUntilQuiet(Action action)
		{
			if (IsDisposed)
				return;
			if (InvokeRequired)
			{
				try
				{
					BeginInvoke((Action)(() => DeferUntilQuiet(action)));
				}
				catch (InvalidOperationException)
				{
					// The view is going away; there is nothing left to refresh.
				}
				return;
			}
			m_afterRefreshQuiet = action;
			if (m_refreshQuietTimer == null)
			{
				m_refreshQuietTimer = new System.Windows.Forms.Timer
				{
					Interval = RefreshQuietMilliseconds
				};
				m_refreshQuietTimer.Tick += (sender, e) => OnRefreshQuiet();
			}
			m_refreshQuietTimer.Stop();
			m_refreshQuietTimer.Start();
		}

		/// <summary>
		/// The quiet wait has run out: runs the last deferred action, once.
		/// </summary>
		internal void OnRefreshQuiet()
		{
			m_refreshQuietTimer?.Stop();
			var pending = m_afterRefreshQuiet;
			m_afterRefreshQuiet = null;
			pending?.Invoke();
		}

		/// <summary>Whether a deferred action is waiting for the quiet wait to run out.</summary>
		internal bool IsRefreshQuietPending => m_refreshQuietTimer?.Enabled == true;

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
		/// The detail this view composes for <paramref name="obj"/>, under the tool's own
		/// configuration: its layout, its view overrides and its slice filter list.
		/// </summary>
		internal ComposedDetail ComposeDetail(ICmObject obj, bool showHidden)
		{
			var lexEntry = obj as ILexEntry;
			return lexEntry != null
				? DetailComposer.Compose(lexEntry, Cache, showHidden,
					overrides: ResolveViewOverride,
					showAllWritingSystemsFields: m_showAllWsFields,
					writingSystemFocused: OnDetailWritingSystemFocused,
					hiddenSliceIds: HiddenSliceIds)
				// Other roots use the tool's layout (m_layoutName, default "Normal");
				// a type-selected one, such as RnGenericRec keyed on "Type", resolves
				// inside Compose.
				: DetailComposer.Compose(obj, Cache,
					string.IsNullOrEmpty(m_layoutName) ? "Normal" : m_layoutName, showHidden,
					overrides: ResolveViewOverride,
					layoutChoiceField: m_layoutChoiceField,
					showAllWritingSystemsFields: m_showAllWsFields,
					writingSystemFocused: OnDetailWritingSystemFocused,
					hiddenSliceIds: HiddenSliceIds);
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

			// Record-navigation expiry: the transient reveal belongs to ONE record -- it
			// survives focus changes within the record and dies when the record changes.
			if (obj == null || obj.Hvo != m_showAllWsRecordHvo)
				m_showAllWsFields.Clear();
			m_showAllWsRecordHvo = obj?.Hvo ?? 0;

			// A WinForms DataTree left from before a switch to the New UI drops its slices when
			// the shown record changes.
			if (m_dataTreeInitialized && m_dataEntryForm?.Root != null && obj != null
				&& m_dataEntryForm.Root.Hvo != obj.Hvo)
			{
				m_dataEntryForm.Reset();
			}

			if (obj == null)
			{
				m_detailEditContext.Clear();
				SetShownRecord(null, null);
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
				composed = ComposeDetail(obj, showHidden);
				if (composed != null)
				{
					detail = composed.Model;
					editContext = composed.EditContext;
				}
			}
			catch (Exception e)
			{
				// The user silently gets the fixed first-slice view instead of the full entry;
				// that degradation must be diagnosable from the log, not just a debugger.
				Logger.WriteError("Full-entry composition failed; falling back to the first slice.", e);
			}

			if (detail == null)
			{
				if (lexEntry == null)
				{
					// No first-slice fallback exists for a non-LexEntry root: show the unsupported state
					// rather than crash.
					m_detailEditContext.Clear();
					SetShownRecord(null, null);
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
			SetShownRecord(obj, detail);
			DetailShowCount++;
			m_avaloniaEntryForm.ShowDetail(detail, editContext,
				OnDetailWritingSystemFocused,
				GetPersistedExpansionState, PersistExpansionState,
				OnDetailMenuRequested, OnDetailLinkRequested,
				new FwTsStringClipboard(Cache.WritingSystemFactory),
				GetPersistedLabelColumnWidth, PersistLabelColumnWidth,
				rootHvo: obj.Hvo, hotlinkItems: HotlinkItemsFor);
		}

		private void SetShownRecord(ICmObject record, DetailModel detail)
		{
			m_shownRecord = record;
			m_shownForHvo = record == null ? 0 : Clerk?.CurrentObject?.Hvo ?? record.Hvo;
			m_shownRowObjects = detail == null
				? new HashSet<int>()
				: new HashSet<int>(detail.Fields.Select(f => f.ObjectHvo).Where(hvo => hvo != 0));
		}

		// The record a change must touch to refresh the view: the one shown, or the clerk's
		// current record while none is, so a record the clerk selects without a show still
		// refreshes on an outside change.
		private ICmObject RefreshRecord => m_shownRecord ?? Clerk?.CurrentObject;

		/// <summary>How many times this view has built its Avalonia detail view.</summary>
		internal int DetailShowCount { get; private set; }

		// Forgets the shown record once the clerk's current object changes, even to a sibling
		// subrecord, so settling the edit left open is judged against the next one (LT-22692).
		private void ForgetShownRecordIfLeaving()
		{
			if (m_shownRecord == null)
				return;
			var next = Clerk?.CurrentObject;
			if (next == null || !next.IsValidObject || next.Hvo != m_shownForHvo)
				SetShownRecord(null, null);
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
		/// The Pronunciation form's menu: a row bound to it also sets the project's current
		/// pronunciation writing systems when its shown set changes (LT-9620).
		/// </summary>
		internal const string PronunciationMenuId = "mnuDataTree-Pronunciation";

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
				if (request.Kind == DetailMenuKind.ItemMenu)
				{
					OnDetailItemMenuRequested(request);
					return;
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

				// A menu that offers nothing is not shown; one that cannot be built is logged
				// below and not shown either.
				var items = BuildMenuItems(idArray, request);
				if (items.Count == 0)
					return;

				// A keyboard-opened menu anchors under the row it came from; a right-click opens
				// it at the pointer. The menu may take focus from the row's editor, which holds
				// its commit until the menu closes.
				request.BeginMenuGesture();
				try
				{
					m_avaloniaEntryForm.ShowContextMenu(items, request.AnchorControl,
						request.OpenAtPointer, request.EndMenuGesture);
				}
				catch
				{
					request.EndMenuGesture();
					throw;
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Detail context menu failed; nothing is shown.", e);
			}
		}

		/// <summary>
		/// The merged menu of the given ids for a request, answered by the row's native
		/// authorities. An id none of them owns shows its commands disabled, and a log line
		/// names it.
		/// </summary>
		internal IReadOnlyList<DetailMenuItem> BuildMenuItems(string[] menuIds, DetailMenuRequest request)
		{
			var authority = CreateMenuAuthority(request);
			foreach (var id in menuIds)
			{
				if (!authority.Owns(id))
					Logger.WriteEvent("No native authority owns the menu '" + id + "'; its commands show disabled.");
			}
			return XCoreMenuBridge.CreateMenuItems(m_propertyTable.GetValue<XWindow>("window"), menuIds, authority);
		}

		/// <summary>
		/// The commands a section header's hotlinks offer, one leaf per command in menu order,
		/// each with its command's current enabled state; empty when the header has no hotlinks
		/// menu, the menu offers no command, or it cannot be built. A menu no native authority
		/// owns shows its commands disabled.
		/// </summary>
		internal IReadOnlyList<DetailMenuItem> HotlinkItemsFor(DetailField field)
		{
			if (string.IsNullOrEmpty(field?.HotlinksId))
				return Array.Empty<DetailMenuItem>();
			try
			{
				return BuildMenuItems(new[] { field.HotlinksId },
					DetailMenuRequest.FromAnchor(null, field, DetailMenuKind.Hotlinks));
			}
			catch (Exception e)
			{
				Logger.WriteError("Building the hotlinks of '" + field.HotlinksId + "' failed; none are shown.", e);
				return Array.Empty<DetailMenuItem>();
			}
		}

		// The per-(class, layout) override file lives in this project's
		// ConfigurationSettings folder. Built lazily and
		// reused; one store per view instance, so it caches the patches it has loaded.
		private ViewDefinitionOverrideStore ViewOverrideStore
		{
			get
			{
				if (m_viewOverrideStore == null && Cache?.ProjectId?.ProjectFolder != null)
				{
					m_viewOverrideStore = new ViewDefinitionOverrideStore(
						LcmFileHelper.GetConfigSettingsDir(Cache.ProjectId.ProjectFolder));
				}

				return m_viewOverrideStore;
			}
		}

		/// <summary>
		/// The help topic of a detail row: its <see cref="DetailField.HelpTopicId"/> when set,
		/// else one generated from the row's field and object and the current tool. A row with
		/// no object generates from its field and label alone, so every row ends at a topic,
		/// the generic one at worst.
		/// </summary>
		internal string ResolveHelpTopic(DetailField field)
		{
			if (field == null)
				return null;
			var source = field.HelpTopicSource;
			var provider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
			var subject = new HelpTopicSubject
			{
				FieldName = source?.FieldName ?? field.Field,
				Label = source?.Label ?? field.Label,
				ClassName = source?.ClassName,
				OwnerClassName = source?.OwnerClassName,
				SortKey = source?.SortKey,
				TargetsParentIsEntry = source?.TargetsParentIsEntry,
				AreaName = m_propertyTable.GetStringProperty("areaChoice", null),
				ToolName = m_propertyTable.GetStringProperty("currentContentControl", null)
			};
			return FieldHelpTopics.Resolve(field.HelpTopicId, FieldHelpTopics.FieldPrefix, subject,
				FieldHelpTopics.KnownBy(provider));
		}

		// The resolver the composer calls for each compiled (class, layout); null result = shipped
		// definition. A load failure is logged, not fatal -- compose then uses the shipped
		// definition.
		private ViewDefinitionOverride ResolveViewOverride(string className, string layoutName)
			=> ViewOverrideStore?.TryGet(className, layoutName,
				(path, error) => Logger.WriteError("Failed to load view-definition override '" + path
					+ "'; using the shipped definition.", error));

		/// <summary>
		/// The native authorities for the request's row, asked in order: the five that own
		/// their menu ids outright, then the Lexicon authority that owns by predicate.
		/// </summary>
		internal IDetailMenuAuthority CreateMenuAuthority(DetailMenuRequest request)
		{
			// One per-object authority serves both the multi-string menu and the composite, so
			// the row's override target is located once per menu, not once per authority.
			var objectMenu = CreateObjectMenuAuthority(request.Field);
			return new CompositeMenuAuthority(
				CreateReorderVectorAuthority(request),
				new EnvironmentInsertMenuAuthority(request, this),
				new StructuredTextMenuAuthority(request, StructuredTextItem),
				CreateWritingSystemsMenuAuthority(request.Field, objectMenu),
				objectMenu,
				CreateLexiconMenuAuthority(request.Field));
		}

		/// <summary>The native authority for the label menu of a row that chooses its
		/// writing systems.</summary>
		internal IDetailMenuAuthority CreateWritingSystemsMenuAuthority(DetailField field)
			=> CreateWritingSystemsMenuAuthority(field, CreateObjectMenuAuthority(field));

		private IDetailMenuAuthority CreateWritingSystemsMenuAuthority(DetailField field,
			IDetailMenuAuthority sharedLeaves)
			=> new WritingSystemsMenuAuthority(field, sharedLeaves,
				menu: WritingSystemMenuOf,
				show: ShowWritingSystems,
				showAll: ShowAllWritingSystemsItem,
				configure: ConfigureWritingSystemsItem);

		/// <summary>The native authority for the row's per-object and Help menus.</summary>
		internal IDetailMenuAuthority CreateObjectMenuAuthority(DetailField field)
			=> new ObjectMenuAuthority(field, LocateOverrideTarget,
				fieldVisibility: (label, target, visibility) =>
					VisibilityItem(label, field, target.Location, visibility),
				moveField: (label, target, up) => MoveItem(label, field, target.Location, up),
				helpTopic: KnownHelpTopic,
				showHelp: ShowDetailHelp);

		// The row's override target, or null (with the reason logged) when it cannot be located.
		// A failure disables the row's field commands rather than failing the whole menu.
		private OverrideTarget LocateOverrideTarget(DetailField field)
		{
			try
			{
				return TryLocateOverrideTarget(field, out _, out var location)
					? new OverrideTarget(location)
					: null;
			}
			catch (Exception e)
			{
				Logger.WriteError("Locating the row's override target failed; its Field Visibility and "
					+ "Move Field commands are disabled.", e);
				return null;
			}
		}

		/// <summary>
		/// The row's help topic when the help provider has it, else null. A failure hides Help
		/// rather than failing the whole menu.
		/// </summary>
		internal string KnownHelpTopic(DetailField field)
		{
			try
			{
				var topic = ResolveHelpTopic(field);
				if (topic == null)
					return null;
				var provider = m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider");
				return provider?.GetHelpString(topic) != null ? topic : null;
			}
			catch (Exception e)
			{
				Logger.WriteError("Resolving the row's help topic failed; Help is hidden.", e);
				return null;
			}
		}

		private void ShowDetailHelp(string topic)
			=> ShowHelp.ShowHelpTopic(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider"), topic);

		/// <summary>
		/// Locates the row's node in its own compiled model, with the current override applied,
		/// so visibility checkmarks and move enablement reflect the live state. False without a
		/// log when the row can never be a target: no class, layout or override store, or a row
		/// the composer synthesized with no node of its own. False with the reason logged when
		/// the compile fails or the model has no node for the row's template id.
		/// </summary>
		internal bool TryLocateOverrideTarget(DetailField field, out string templateId,
			out ViewNodeLocation location)
		{
			templateId = null;
			location = null;
			if (field == null || string.IsNullOrEmpty(field.ClassName) || string.IsNullOrEmpty(field.LayoutName)
				|| ViewOverrideStore == null || !TryTemplateIdOf(field.StableId, out templateId))
			{
				return false;
			}
			try
			{
				if (Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var fieldObj))
				{
					var model = DetailComposer.CompileForObject(Cache, fieldObj, field.LayoutName,
						ResolveViewOverride);
					if (model != null)
						location = ViewDefinitionOverrideEditor.LocateTarget(model, templateId);
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Resolving the field's override target failed; its Field Visibility "
					+ "and Move Field commands are disabled.", e);
				return false;
			}
			if (location == null)
			{
				Logger.WriteEvent(string.Format("Detail row '{0}' has no node in its compiled model; its "
					+ "Field Visibility and Move Field commands are disabled.",
					templateId));
				return false;
			}
			return true;
		}

		// The node a row's stable id names; false for a synthesized row with no node of its
		// own (an item or relation path after the hvo, or a custom field). A ghost row stands
		// in for its empty sequence node.
		private static bool TryTemplateIdOf(string stableId, out string templateId)
		{
			templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(stableId);
			if (templateId.Contains(DetailField.CustomFieldStableIdMarker))
				return false;
			var at = stableId.IndexOf('@');
			var path = at < 0 ? -1 : stableId.IndexOf('/', at);
			if (path < 0)
				return true;
			if (!stableId.Substring(path).StartsWith(DetailField.GhostStableIdSuffix, StringComparison.Ordinal))
				return false;
			templateId = stableId.Substring(0, at);
			return true;
		}

		// A Field Visibility menu item: checked when it is the field's current visibility, executes the
		// SetVisibility override mutation (idempotent -- re-choosing the current value is a
		// harmless write).
		private DetailMenuItem VisibilityItem(string label, DetailField field,
			ViewNodeLocation location, ViewVisibility target)
		{
			var isChecked = location.Visibility == target;
			return new DetailMenuItem(label, isEnabled: true, isChecked: isChecked, children: null,
				execute: () => ApplyFieldVisibility(field, location, target));
		}

		// A Move Field item: disabled at the first sibling (up) / last sibling (down) / when alone.
		private DetailMenuItem MoveItem(string label, DetailField field, ViewNodeLocation location, bool up)
		{
			var canMove = up ? location.CanMoveUp : location.CanMoveDown;
			return new DetailMenuItem(label, isEnabled: canMove, isChecked: false, children: null,
				execute: canMove ? (Action)(() => ApplyMoveField(field, location, up)) : null);
		}

		/// <summary>
		/// The "Show all right now" item: marks the row's part for the transient reveal (every
		/// row of the part shows every writing-system option until the user navigates to another
		/// record) and recomposes. The reveal is view state, not a command, so the item
		/// dispatches nothing and never writes the override.
		/// </summary>
		private DetailMenuItem ShowAllWritingSystemsItem(string label, DetailField field)
			=> new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null, execute: () =>
				{
					m_showAllWsFields.Add(ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId));
					RefreshAvaloniaDetail();
				});

		// The row's Writing Systems menu, checked from its stored selection.
		private IReadOnlyList<WritingSystemMenuOption> WritingSystemMenuOf(DetailField field)
			=> WritingSystemChoicesOf(field).Menu(field.VisibleWritingSystems);

		/// <summary>
		/// The writing systems the row offers and shows, from the shared rule its kind of row
		/// follows: the Reversal Entries rule, or the multi-string rule over the row's own
		/// layout facts. The menu and the Configure dialog both read these.
		/// </summary>
		internal WritingSystemChoices WritingSystemChoicesOf(DetailField field)
			=> ReversalIndexEntryPlugin.Built(field)
				? ReversalEntryWritingSystems.Choices(Cache)
				: FieldWritingSystemOptions.Choices(Cache, WritingSystemSpecOf(field));

		private static WritingSystemFieldSpec WritingSystemSpecOf(DetailField field)
			=> WritingSystemFieldSpec.FromLayout(field.ObjectHvo, field.WritingSystem,
				field.OptionalWritingSystem, field.ForceIncludeEnglish);

		/// <summary>
		/// Makes the row show exactly the given writing systems: stores the selection in the
		/// row's view override, keeps the project's pronunciation writing systems in step on a
		/// Pronunciation row, and recomposes. Nothing here reaches the mediator.
		/// </summary>
		private void ShowWritingSystems(DetailField field, IReadOnlyList<string> writingSystems)
		{
			try
			{
				// The menu never offers an empty set, so empty means nothing to store -- and an
				// empty op would CLEAR the restriction rather than narrow it.
				if (writingSystems == null || writingSystems.Count == 0)
					return;
				var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId);
				var op = new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibleWritingSystems,
					templateId, writingSystems: writingSystems.ToList());
				if (!TryMutateOverride(field, op))
					return;
				if (string.Equals(field.MenuId, PronunciationMenuId, StringComparison.Ordinal))
				{
					// Exactly the chosen writing systems, never a fallback set: a stale id
					// must not rewrite the project's list to the defaults.
					var chosen = new HashSet<string>(writingSystems, StringComparer.OrdinalIgnoreCase);
					var resolved = FieldWritingSystemOptions.Options(Cache, WritingSystemSpecOf(field))
						.Where(ws => chosen.Contains(ws.Id)).ToList();
					if (resolved.Count == chosen.Count)
						PronunciationWritingSystems.Sync(Cache, resolved);
				}
				// A persisted set supersedes any transient reveal on the part.
				m_showAllWsFields.Remove(templateId);
				RefreshAvaloniaDetail();
			}
			catch (Exception e)
			{
				Logger.WriteError("Storing the row's writing-system selection failed.", e);
			}
		}

		// Opens the per-field Configure dialog on the row's own options and shown set, and
		// applies the result.
		private DetailMenuItem ConfigureWritingSystemsItem(string label, DetailField field)
			=> new DetailMenuItem(label, isEnabled: true, isChecked: false, children: null, execute: () =>
				{
					try
					{
						var choices = WritingSystemChoicesOf(field);
						var shown = choices.Shown(field.VisibleWritingSystems);
						using (var dlg = new ConfigureWritingSystemsDlg(
							choices.Options, shown,
							m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
						{
							dlg.Text = ConfigureWritingSystemsDlg.TitleFor(field.Label);
							if (dlg.ShowDialog() != System.Windows.Forms.DialogResult.OK)
								return;
							var chosen = dlg.SelectedWritingSystems.Select(ws => ws.Id).ToList();
							// The dialog answers in option order, the stored set may not:
							// same set means nothing changed.
							if (new HashSet<string>(chosen, StringComparer.Ordinal)
								.SetEquals(shown.Select(ws => ws.Id)))
								return; // nothing changed: no override write.
							ShowWritingSystems(field, chosen);
						}
					}
					catch (Exception e)
					{
						Logger.WriteError("Configuring the row's writing systems failed.", e);
					}
				});

		// Writes a SetVisibility op for each of the location's visibility targets into the
		// project override and recomposes.
		private void ApplyFieldVisibility(DetailField field, ViewNodeLocation location,
			ViewVisibility target)
		{
			var ops = location.VisibilityTargets.Select(id => new ViewOverrideOperation(
				ViewOverrideOperationKind.SetVisibility, id, visibility: target)).ToArray();
			MutateOverrideAndRefresh(field, ops);
		}

		// Writes a ReorderChildren op on the field's PARENT into the project override, then
		// recomposes. A top-level row has no parent id; the applier keys the root list on an
		// empty id.
		private void ApplyMoveField(DetailField field, ViewNodeLocation location, bool up)
		{
			var moved = ViewDefinitionOverrideEditor.ComputeMovedOrder(location.SiblingOrder, location.Index, up);
			if (moved == null)
				return; // first, last or only sibling.
			var op = new ViewOverrideOperation(ViewOverrideOperationKind.ReorderChildren,
				location.ParentStableId ?? string.Empty, childOrder: moved);
			MutateOverrideAndRefresh(field, op);
		}

		// Loads-or-creates the (class, layout) override, folds the op in, saves it, and
		// recomposes the Avalonia detail view. Only the override file is written -- not the
		// shipped XML or the part-ref inventory.
		private void MutateOverrideAndRefresh(DetailField field, params ViewOverrideOperation[] ops)
		{
			if (TryMutateOverride(field, ops))
				RefreshAvaloniaDetail();
		}

		// Folds the ops into the (class, layout) override and saves it once, WITHOUT recomposing.
		// Returns whether the save succeeded, so a failed save can leave view state untouched.
		private bool TryMutateOverride(DetailField field, params ViewOverrideOperation[] ops)
		{
			try
			{
				var store = ViewOverrideStore;
				if (store == null)
					return false;

				var merged = store.TryGet(field.ClassName, field.LayoutName)
					?? new ViewDefinitionOverride(field.ClassName, field.LayoutName, "detail", null, null);
				foreach (var op in ops)
					merged = ViewDefinitionOverrideEditor.MergeOperation(merged, op);
				store.Save(merged);
				return true;
			}
			catch (Exception e)
			{
				Logger.WriteError("Applying the field override failed.", e);
				return false;
			}
		}

		/// <summary>
		/// Follows a chooser jump link (e.g. "Edit the Publications list" on Publish In) the
		/// way <c>ReallySimpleListChooser.HandleAnyJump</c> does on link click: the flyout has
		/// already closed, then the link built from the layout's <c>chooserLink</c> is followed.
		/// </summary>
		private void OnDetailLinkRequested(DetailLinkRequest request)
		{
			try
			{
				FollowLink(CreateFollowLinkArgs(request));
			}
			catch (Exception e)
			{
				Logger.WriteError("Detail chooser link jump failed.", e);
			}
		}

		/// <summary>
		/// Follows a link from the detail view: pending edits settle first, since the jump
		/// leaves the record, then the link is posted. Every native jump comes through here,
		/// so replacing the post reaches them all at once.
		/// </summary>
		internal void FollowLink(FwLinkArgs link)
		{
			SettleDetailEdits();
#pragma warning disable 618 // suppress obsolete warning
			m_mediator.PostMessage("FollowLink", link);
#pragma warning restore 618
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

		internal bool? GetPersistedExpansionState(string expansionKey)
		{
			if (m_expansionStates.TryGetValue(expansionKey, out var expanded))
				return expanded;
			var stored = m_propertyTable?.GetStringProperty(ExpansionSettingPrefix + expansionKey, null,
				PropertyTable.SettingsGroup.LocalSettings);
			return stored == null ? (bool?)null : stored == "1";
		}

		internal void PersistExpansionState(string expansionKey, bool expanded)
		{
			m_expansionStates[expansionKey] = expanded;
			if (m_propertyTable == null)
				return;
			var key = ExpansionSettingPrefix + expansionKey;
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
		// Resolves the shown record like ShowRecord, so a showDescendantInRoot tool recomposes
		// the root, not the subrecord.
		private void RefreshAvaloniaDetail()
		{
			if (m_avaloniaEntryForm == null || !ShouldUseAvaloniaLexiconEdit)
				return;
			var current = ResolveShownRecord(Clerk?.CurrentObject);
			if (current == null)
				return;

			ShowAvaloniaEntry(current);
		}

		// The view is idle again, so anything held for the interaction can land now.
		private void OnAvaloniaInteractionCompleted(object sender, EventArgs e)
		{
			m_avaloniaRefreshController?.ReleaseHeldRefresh();
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
					? ActiveHostContract.ForAvalonia()
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
			RegisterCommandAuthority();

			DetachDataTreeFromPanel();
			m_avaloniaEntryForm.Show();
			m_avaloniaEntryForm.BringToFront();
		}
	}
}
