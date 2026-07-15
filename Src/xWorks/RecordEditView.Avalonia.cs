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
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel;
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
	/// record can be shown on the new <see cref="LexicalEditHostControl"/> surface instead of the
	/// legacy <see cref="DataTree"/>. Kept in its own file (mirroring this codebase's
	/// Form/Form.Designer.cs split) so the legacy-facing file stays a small, reviewable diff over
	/// the pre-Avalonia original; nothing here changes behavior when <c>UIMode</c> is Legacy.
	/// </summary>
	public partial class RecordEditView
	{
		private LexicalEditSurface m_lexicalEditSurface;
		private readonly LexicalEditSurfaceFactory m_lexicalEditSurfaceFactory;
		private readonly LexicalEditSurfaceSelectionService m_surfaceSelectionService = new LexicalEditSurfaceSelectionService();
		private LexicalEditHostControl m_avaloniaEntryForm;
		private RecordClerkNavigationContext m_recordNavigationContext;
		// Owns the fenced edit context; swapping/clearing through it cancels any open session so an
		// open undo task is never orphaned (an orphan makes the shutdown Save throw "Commit at wrong place").
		private readonly RegionEditContextHolder m_regionEditContext = new RegionEditContextHolder();
		private AvaloniaRegionRefreshController m_avaloniaRefreshController;
		// winforms-free-lexeme-editor.md D4: the host services handed to region editor plugins —
		// today only the legacy-dialog launcher seam (this view is the sanctioned WinForms
		// carve-out; the pane itself stays WinForms-free).
		private RegionEditorServices m_regionEditorServices;
		private SIL.FieldWorks.Common.FwAvalonia.Region.IRegionMediaServices m_regionMediaServices;
		// advanced-entry-view: the per-project home of the sparse view-definition override patches that
		// drive the Avalonia surface's per-field Field Visibility / Move Field commands. Lazily built from
		// the project ConfigurationSettings folder; the Avalonia surface reads it at Compose and the gear
		// menu writes it. The legacy WinForms DataTree path NEVER touches this — it keeps its Inventory
		// store untouched.
		private ViewDefinitionOverrideStore m_viewOverrideStore;
		// 13.4: the approved baseline-adapter ids — the ONLY routes allowed to drive hidden legacy
		// infrastructure while Avalonia is active. Keep in sync with the region manifest's
		// allowedAdapters (openspec/changes/lexical-edit-avalonia-migration/region-manifest.md);
		// hardcoded here because the manifest is documentation, not yet machine-readable.
		internal const string CommandMenuRoutingAdapterId = "command-menu-routing";
		private static readonly string[] ApprovedBaselineAdapters = { CommandMenuRoutingAdapterId };
		// The active-host contract (task 3.10) for the CURRENT surface, kept in sync with every
		// m_lexicalEditSurface assignment (SetLexicalEditSurface) from the approved set above.
		// Assert sites only pass the adapter id they claim, so an unlisted id actually trips — a
		// contract constructed at the assert site from the very id it then asserts could never fail.
		private ActiveHostContract m_activeHostContract;
		// Hybrid companion lane: the real WinForms slices (today the Chorus Messages notes bar)
		// promoted out of the Avalonia model into the host's companion strip, plus their editor
		// controls (reparented into the strip, so the slice's Dispose no longer reaches them).
		// Recreated per shown record; torn down on record change/clear/dispose.
		private readonly List<Slice> m_companionSlices = new List<Slice>();
		private readonly List<Control> m_companionControls = new List<Control>();

		// Viewing parity (11.8): expansion state persists per header stable id — in-session through the
		// dictionary, across sessions through PropertyTable local settings, the legacy ExpansionStateKey
		// behavior. Per-instance (review round 1): a process-wide static leaked state across
		// projects/windows for the app lifetime.
		private readonly Dictionary<string, bool> m_expansionStates = new Dictionary<string, bool>();

		private bool ShouldUseAvaloniaLexicalEdit
		{
			get { return m_lexicalEditSurface == LexicalEditSurface.Avalonia; }
		}

		/// <summary>
		/// Auto-save (14.4): settles any open fenced edit session — commit when validation is
		/// clean, roll back otherwise. The holder guards internally (no-op when nothing is open),
		/// so this is idempotent and safe to call unconditionally from ANY host path — including
		/// while the legacy surface is active, when no fenced session can be open.
		/// </summary>
		private void SettleRegionEdits()
		{
			m_regionEditContext.Settle();
		}

		/// <summary>
		/// ITEM 2 (invalid-edit-on-navigate UX): the host's response when <see cref="Settle"/> rolled
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
		/// The ordering-sensitive teardown of the Avalonia surface plumbing — this is the ONE
		/// place that ordering lives. Teardown order matters: stop the event/notification
		/// plumbing FIRST so the settle's commit/rollback PropChanged cannot re-enter a dying
		/// view, then settle (auto-save 14.4 extends to teardown: a valid pending edit commits,
		/// invalid rolls back), then drop the context, and only then dispose the companions and
		/// the host control itself.
		/// </summary>
		private void TearDownAvaloniaSurface()
		{
			if (m_avaloniaEntryForm != null)
				m_avaloniaEntryForm.RegionEditCompleted -= OnAvaloniaRegionEditCompleted;
			m_regionEditContext.DetachDeactivateHook();
			m_regionEditContext.DetachUndoGuard();
			m_regionEditContext.InvalidEditRolledBack = null;
			m_avaloniaRefreshController?.Dispose();
			SettleRegionEdits();
			m_regionEditContext.Clear();
			TearDownCompanionSlices();
			// The launcher may hold the last media player alive (legacy parity); release it with
			// the surface that handed it to plugins.
			(m_regionEditorServices?.LegacyDialogLauncher as IDisposable)?.Dispose();
			m_regionEditorServices = null;
			m_avaloniaEntryForm?.Dispose();
			// WIRE-01: null the host + refresh controller after disposing them. The recreation guards
			// (EnsureAvaloniaSurfaceInitialized / EnsureAvaloniaRefreshController) key on `== null`, so a
			// runtime flip New->Legacy->New rebuilds a fresh surface instead of re-showing a disposed one.
			m_avaloniaEntryForm = null;
			m_avaloniaRefreshController = null;
		}

		/// <summary>
		/// The bidirectional selection bridge for this host's clerk (task 3.12). Created on first use so
		/// the clerk is initialized. Surfaces (including the Avalonia host) follow the current-record bus
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

		private LexicalEditSurface ResolveConfiguredLexicalEditSurface()
		{
			// Task 3.9: route the per-host decision through the explicit selection service rather than
			// inferring product routing ad hoc from settings/PropertyTable state.
			var uiMode = m_propertyTable != null
				? m_propertyTable.GetStringProperty(LexicalEditSurfaceResolver.UIModePropertyName, LexicalEditSurfaceResolver.LegacyUIMode)
				: LexicalEditSurfaceResolver.LegacyUIMode;
			var toolName = m_propertyTable != null
				? m_propertyTable.GetStringProperty("currentContentControl", string.Empty)
				: string.Empty;

			// Per-tool opt-out from "Manage Individual Features" (the master UIMode=New switch defaults
			// every catalog tool on; this is the individual override a user can flip back off).
			bool? overrideEnabled = null;
			if (m_propertyTable != null)
			{
				var disabledTools = m_propertyTable.GetStringProperty(
					LexicalEditSurfaceResolver.UIModeDisabledToolsPropertyName, string.Empty);
				if (LexicalEditSurfaceResolver.IsToolDisabledByUser(disabledTools, toolName))
					overrideEnabled = false;
			}

			return m_surfaceSelectionService.Decide(uiMode, toolName, overrideEnabled).Surface;
		}

		private void EnsureAvaloniaSurfaceInitialized()
		{
			if (m_avaloniaEntryForm != null)
				return;

			m_avaloniaEntryForm = (LexicalEditHostControl)m_lexicalEditSurfaceFactory.Create(LexicalEditSurface.Avalonia);
			m_avaloniaEntryForm.Dock = DockStyle.Fill;
			m_avaloniaEntryForm.RegionEditCompleted += OnAvaloniaRegionEditCompleted;
			if (!m_panel.Controls.Contains(m_avaloniaEntryForm))
				m_panel.Controls.Add(m_avaloniaEntryForm);
		}

		/// <summary>
		/// Task 3.15: subscribe the Avalonia surface to the real PropChanged bus so external edits to
		/// the displayed entry (legacy surfaces, refresh-driven reloads) re-resolve the region.
		/// Refreshes are held while this surface's own edit session is open and delivered on completion.
		/// </summary>
		private void EnsureAvaloniaRefreshController()
		{
			if (m_avaloniaRefreshController != null)
				return;

			// The controller owns the ONE coalesced, editing-aware refresh queue (PropChanged
			// deliveries and host-requested re-shows alike); the host only supplies UI-thread
			// deferral, so a late-queued refresh still re-checks "is the user typing now?" inside
			// the controller's runner before recomposing.
			m_avaloniaRefreshController = new AvaloniaRegionRefreshController(
				Cache,
				() => Clerk?.CurrentObject,
				() => m_regionEditContext.Current?.IsOpen == true,
				RefreshAvaloniaRegion,
				new RefreshCoordinator(),
				ScheduleOnUiThread,
				// This lexical host's relevance rule; the controller itself stays host-agnostic.
				changed => IsChangeWithinEntry(changed, Clerk?.CurrentObject));
			// Global Undo/Redo while a fenced session is open would re-enter the UOW write lock
			// (LockRecursionException); the guard settles the pending edit instead.
			m_regionEditContext.AttachUndoGuard(Cache.ActionHandlerAccessor);
			// ITEM 2 (invalid-edit-on-navigate UX): when Settle rolls back a pending edit because it
			// failed validation (e.g. the required lexeme form was cleared, then the user navigated
			// away), tell the user WHY rather than discarding it silently. The rollback still happens
			// (the safe close that keeps the open undo task from stranding); we only surface the reason.
			m_regionEditContext.InvalidEditRolledBack = ShowInvalidEditRolledBackWarning;
			// The guard only hooks THIS window's undo stack — it cannot reach other windows' stacks,
			// so Ctrl+Z in another window while this one holds an open session would still re-enter
			// the write lock. Mitigate by settling whenever this view's top-level window deactivates
			// (the user must focus another window before they can undo there).
			m_regionEditContext.AttachDeactivateHook(FindForm());
		}

		/// <summary>
		/// The lexical host's refresh relevance (task 3.15): a change is relevant when the changed
		/// object is, or is owned by, the entry on display. This is the predicate the host injects
		/// into <see cref="AvaloniaRegionRefreshController"/>; static and internal so it is
		/// unit-testable without a live view.
		/// </summary>
		internal static bool IsChangeWithinEntry(ICmObject changed, ICmObject current)
		{
			if (changed == null || current == null)
				return false;
			// §20.1.3: class-agnostic — a change is "within" the displayed record when the changed object IS
			// the record or is OWNED (at any depth) by it. The old code special-cased ILexEntry via
			// OwnerOfClass<ILexEntry>(); walking the owner chain up to the current record is equivalent for an
			// entry root AND correct for any other record class (RnGenericRec, CmPossibility, …) so their edits
			// also trigger the coalesced refresh.
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
		/// Shows the Avalonia surface for a record: the composed full-entry view when the record is a
		/// lexical entry (first-slice fallback if composition fails), or the resource-backed
		/// unsupported state otherwise.
		/// </summary>
		private void ShowAvaloniaEntry(ICmObject obj)
		{
			// Auto-save (14.4): a session still open from the previous record/edit settles before
			// the region is replaced (commit when valid, roll back when not) — the same policy
			// every host path shares; Replace's cancel-on-displace stays the safety net.
			SettleRegionEdits();

			// 13.4 adapter hygiene: the hidden command-routing DataTree must never answer mediator
			// commands for a PREVIOUS record — reset it whenever the shown record changes; the next
			// right-click re-syncs it (EnsureMenuCommandAdapter). Without this, Insert Sense from
			// the main menu could silently target the entry that was last right-clicked.
			if (m_legacySurfaceInitialized && m_dataEntryForm?.Root != null && obj != null
				&& m_dataEntryForm.Root.Hvo != obj.Hvo)
			{
				m_dataEntryForm.Reset();
			}

			if (obj == null)
			{
				m_regionEditContext.Clear();
				TearDownCompanionSlices();
				m_avaloniaEntryForm.ShowMessage(FwAvaloniaStrings.EntryTypeUnsupported);
				return;
			}

			// §20.1.3: the composer is class-general — compose the structured view for ANY record root
			// (LexEntry for the lexicon tool; RnGenericRec / CmPossibility / PartOfSpeech once other tools
			// register). Only a LexEntry has the first-slice fallback below; any other class that fails to
			// compose shows the unsupported message (never a NRE).
			var lexEntry = obj as ILexEntry;

			// Viewing parity (11.x): honor the same View → Show Hidden Fields setting legacy DataTree
			// reads (ShowHiddenFields-{tool}, local settings).
			var toolName = m_propertyTable.GetStringProperty("currentContentControl", string.Empty);
			var showHidden = m_propertyTable.GetBoolProperty("ShowHiddenFields-" + toolName, false,
				PropertyTable.SettingsGroup.LocalSettings);

			LexicalEditRegionModel region = null;
			IRegionEditContext editContext = null;
			ComposedEntryRegion composed = null;
			try
			{
				composed = lexEntry != null
					? FullEntryRegionComposer.Compose(lexEntry, Cache, showHidden,
						services: EnsureRegionEditorServices(), overrides: ResolveViewOverride)
					// §20.1.3/§20.1.4: non-entry roots compose against the tool's configured layout
					// (m_layoutName, default "Normal"); a type-selected layout (m_layoutChoiceField, e.g.
					// Notebook RnGenericRec keyed on "Type") resolves to the right variant inside Compose.
					: FullEntryRegionComposer.Compose(obj, Cache,
						string.IsNullOrEmpty(m_layoutName) ? "Normal" : m_layoutName, showHidden,
						services: EnsureRegionEditorServices(), overrides: ResolveViewOverride,
						layoutChoiceField: m_layoutChoiceField);
				if (composed != null)
				{
					region = composed.Model;
					editContext = composed.EditContext;
				}
			}
			catch (Exception e)
			{
				// The user silently gets the fixed first-slice view instead of the full entry;
				// that degradation must be diagnosable from the log, not just a debugger.
				Logger.WriteError("Full-entry composition failed; falling back to the first slice.", e);
			}

			if (region == null)
			{
				if (lexEntry == null)
				{
					// No first-slice fallback exists for a non-LexEntry root: show the unsupported state
					// rather than crash. (This path is only reachable once a non-lexicon tool registers.)
					m_regionEditContext.Clear();
					TearDownCompanionSlices();
					m_avaloniaEntryForm.ShowMessage(FwAvaloniaStrings.EntryTypeUnsupported);
					return;
				}
				region = LexicalEditRegionBuilder.Build(lexEntry, Cache);
				editContext = new LexicalEditRegionEditContext(lexEntry, Cache);
			}

			// Hybrid companion lane: WinForms-only custom slices (the Chorus Messages notes bar)
			// are realized for real in the host's companion strip and their placeholder rows are
			// removed from the Avalonia model. Always runs (also clears the strip on fallback or
			// when the layout no longer reaches a companion slice).
			region = PromoteCompanionSlices(composed, region);

			// Re-showing mid-edit (record navigation, refresh delivery, Show Hidden Fields, window
			// activation) must cancel the displaced context's open fenced session — orphaning the
			// open undo task makes the shutdown Save throw "Commit at wrong place".
			m_regionEditContext.Replace(editContext);

			EnsureAvaloniaRefreshController();
			// The deactivate-settle hook needs a realized top-level Form. If the handle was not yet
			// created when the controller was first ensured (e.g. the very first record shows before
			// the window realizes), retry here on each show until it attaches — otherwise the
			// cross-window-undo mitigation would be silently lost for this host's lifetime.
			if (!m_regionEditContext.IsDeactivateHookAttached)
				m_regionEditContext.AttachDeactivateHook(FindForm());
			m_avaloniaEntryForm.ShowRegion(region, editContext,
				wsTag => LexicalEditRegionBuilder.ActivateKeyboardForWritingSystem(Cache, wsTag),
				GetPersistedExpansionState, PersistExpansionState,
				OnRegionMenuRequested, OnRegionLinkRequested,
				new FwTsStringClipboard(Cache.WritingSystemFactory),
				GetPersistedLabelColumnWidth, PersistLabelColumnWidth,
				EnsureRegionMediaServices());
		}

		/// <summary>
		/// winforms-free-lexeme-editor.md D4: the services region editor plugins may use beyond
		/// (object, node, edit context, cache) — the legacy-dialog launcher seam, implemented here
		/// because this host is the only place allowed to touch WinForms during coexistence. Any
		/// open fenced edit session settles before a dialog launches (a legacy dialog opens its own
		/// UOW; doing that under the fence's open write lock would throw, the undo-guard hazard).
		/// The dialog commits through its own UOW, so the refresh controller's PropChanged
		/// subscription re-renders the region after the dialog closes — no explicit refresh here.
		/// </summary>
		// §19d: the host media seam (picture file pick + properties dialog + audio play/record). Created
		// lazily and reused; the file picker resolves the Avalonia IStorageProvider off the live hosted
		// surface, the dialog is owned by this host's WinForms Form, and audio rides libpalaso's device.
		private SIL.FieldWorks.Common.FwAvalonia.Region.IRegionMediaServices EnsureRegionMediaServices()
		{
			if (m_regionMediaServices == null)
			{
				m_regionMediaServices = new LcmRegionMediaServices(Cache,
					() => FindForm(),
					() => m_avaloniaEntryForm?.HostedContent);
			}
			return m_regionMediaServices;
		}

		private RegionEditorServices EnsureRegionEditorServices()
		{
			if (m_regionEditorServices == null)
			{
				m_regionEditorServices = new RegionEditorServices
				{
					LegacyDialogLauncher = new WinFormsLegacyDialogLauncher(Cache, m_mediator,
						m_propertyTable, FindForm, SettleRegionEdits)
				};
			}
			return m_regionEditorServices;
		}

		/// <summary>
		/// Hybrid companion lane: tears down the previous companions, instantiates the real legacy
		/// slice for each designated WinForms-only custom editor the composer found (today the
		/// Chorus Messages notes bar — its NotesBarView cannot render inside Avalonia), hands the
		/// slices' editor controls to the host's companion strip, and returns the region model with
		/// the promoted placeholder rows removed. When the slice cannot be created (Chorus
		/// unavailable) the row degrades to nothing — logged by AvaloniaCompanionSlices.
		/// </summary>
		private LexicalEditRegionModel PromoteCompanionSlices(ComposedEntryRegion composed,
			LexicalEditRegionModel region)
		{
			TearDownCompanionSlices();

			var promotions = AvaloniaCompanionSlices.SelectPromotions(composed?.CustomEditorFields);
			if (promotions.Count == 0)
				return region;

			var companionControls = new List<Control>();
			var promotedIds = new List<string>();
			foreach (var binding in promotions)
			{
				// The unsupported row never renders for a designated companion slice, whether or
				// not the real slice could be created.
				promotedIds.Add(binding.FieldStableId);

				var slice = AvaloniaCompanionSlices.CreateCompanionSlice(binding, Cache);
				if (slice == null)
					continue;
				var control = slice.Control;
				if (control == null)
				{
					slice.Dispose();
					continue;
				}

				// Track both: the strip reparents the control out of the slice, so the slice's
				// Dispose no longer disposes it — TearDownCompanionSlices owns both lifetimes.
				m_companionSlices.Add(slice);
				m_companionControls.Add(control);
				companionControls.Add(control);
			}

			if (companionControls.Count > 0)
				m_avaloniaEntryForm.SetCompanionControls(companionControls);
			return AvaloniaCompanionSlices.RemovePromotedFields(region, promotedIds);
		}

		/// <summary>
		/// Disposes the companion slices created for the previously shown record and empties the
		/// host's companion strip. The strip never disposes anything itself; this view created the
		/// slices, so it disposes them (the editor control first — it was reparented into the strip
		/// and is no longer reachable from the slice — then the slice, which releases its backing
		/// services, e.g. MessageSlice's ChorusSystem).
		/// </summary>
		private void TearDownCompanionSlices()
		{
			if (m_companionSlices.Count == 0 && m_companionControls.Count == 0)
				return;

			if (m_avaloniaEntryForm != null && !m_avaloniaEntryForm.IsDisposed)
				m_avaloniaEntryForm.SetCompanionControls(null);

			foreach (var control in m_companionControls)
			{
				if (!control.IsDisposed)
					control.Dispose();
			}
			m_companionControls.Clear();

			foreach (var slice in m_companionSlices)
			{
				if (!slice.IsDisposed)
					slice.Dispose();
			}
			m_companionSlices.Clear();
		}

		/// <summary>
		/// Section 13: shows the SAME xCore-defined context menu the legacy slice shows, over the
		/// Avalonia surface — the menu ids come from the layout (imported into the typed IR), the menu
		/// is materialized from the window configuration and dispatched through the mediator, exactly
		/// the legacy `DTMenuHandler.MakeSliceContextMenu` recipe (menu + mnuDataTree-Object; in-string
		/// menus add mnuDataTree-MultiStringSlice). Command targeting (13.4) uses the approved baseline
		/// adapter "command-menu-routing": the legacy DataTree + DTMenuHandler are initialized lazily and
		/// kept HIDDEN purely as the command-target colleague chain, with CurrentSlice pointed at the
		/// slice bound to the clicked row's object — never shown, never the active surface.
		/// </summary>
		private void OnRegionMenuRequested(RegionMenuRequest request)
		{
			try
			{
				// An adapter failure must not suppress the menu itself: items that need the hidden
				// colleague chain disable, everything else still works (and the failure is logged).
				try
				{
					EnsureMenuCommandAdapter(request.Field.ObjectHvo);
				}
				catch (Exception adapterError)
				{
					Logger.WriteError("Region menu command adapter failed; menu items that need "
						+ "the hidden colleague chain will be disabled.", adapterError);
				}

				var ids = new List<string>();
				switch (request.Kind)
				{
					case RegionMenuKind.ContextMenu:
						ids.Add(request.Field.ContextMenuId);
						ids.Add("mnuDataTree-MultiStringSlice");
						ids.Add("mnuDataTree-Object");
						break;
					case RegionMenuKind.Hotlinks:
						ids.Add(request.Field.HotlinksId);
						break;
					default:
						ids.Add(request.Field.MenuId);
						if (!string.IsNullOrEmpty(request.Field.HotlinksId))
							ids.Add(request.Field.HotlinksId); // section link commands stay reachable
						ids.Add("mnuDataTree-Object");
						break;
				}

				var idArray = ids.Where(id => !string.IsNullOrEmpty(id)).ToArray();
				var window = m_propertyTable.GetValue<XWindow>("window");

				// 15.1: render the SAME xCore menu natively in Avalonia (identical items, enablement,
				// and mediator dispatch — only the chrome changes). The WinForms adapter menu remains
				// the fallback so a materialization failure never costs the user the menu.
				try
				{
					// advanced-entry-view: retarget the per-field Field Visibility / Move Field commands
					// to the project override layer for the Avalonia surface; every other command (Help,
					// inserts, writing-system menu, ...) keeps its normal mediator dispatch.
					var interceptor = BuildOverrideCommandInterceptor(request.Field);
					var items = XCoreMenuBridge.BuildMenuItems(window, idArray, interceptor);
					if (items.Count > 0)
					{
						m_avaloniaEntryForm.ShowContextMenu(items);
						return;
					}
				}
				catch (Exception nativeMenuError)
				{
					Logger.WriteError("Avalonia-native menu failed; falling back to the adapter menu.",
						nativeMenuError);
				}

				window.ShowContextMenu(idArray,
					new System.Drawing.Point(request.ScreenX, request.ScreenY), null, null);
			}
			catch (Exception e)
			{
				Logger.WriteError("Region context menu failed.", e);
			}
		}

		// advanced-entry-view: the per-(class, layout) override file lives in this project's
		// ConfigurationSettings folder (canonical-view-definition-design.md Layer 2). Built lazily and
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

		// The resolver the composer calls for each compiled (class, layout); null result = shipped
		// definition. A load failure is logged, not fatal — compose then uses the shipped definition.
		private ViewDefinitionOverride ResolveViewOverride(string className, string layoutName)
			=> ViewOverrideStore?.TryGet(className, layoutName,
				(path, error) => Logger.WriteError("Failed to load view-definition override '" + path
					+ "'; using the shipped definition.", error));

		/// <summary>
		/// advanced-entry-view: builds the interceptor that retargets the per-field Field Visibility and
		/// Move Field commands to the project override layer for the Avalonia surface. Returns null
		/// (intercept nothing — every command keeps its normal mediator dispatch) when the clicked row
		/// carries no (class, layout) context, e.g. the first-slice fallback rows; that keeps the legacy
		/// behavior intact when the override layer cannot be addressed.
		/// </summary>
		private Func<ChoiceBase, RegionMenuItem> BuildOverrideCommandInterceptor(LexicalEditRegionField field)
		{
			if (field == null || string.IsNullOrEmpty(field.ClassName) || string.IsNullOrEmpty(field.LayoutName)
				|| ViewOverrideStore == null)
			{
				return null;
			}

			var templateId = ViewDefinitionOverrideEditor.StripRuntimeSuffix(field.StableId);
			// Locate the clicked node in the field's OWN compiled model (with any current override
			// already applied), so visibility checkmarks and move enablement reflect the live state.
			ViewNodeLocation location = null;
			try
			{
				if (Cache.ServiceLocator.ObjectRepository.TryGetObject(field.ObjectHvo, out var fieldObj))
				{
					var model = FullEntryRegionComposer.CompileForObject(Cache, fieldObj, field.LayoutName,
						ResolveViewOverride);
					if (model != null)
						location = ViewDefinitionOverrideEditor.LocateTarget(model, templateId);
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Resolving the field's override target failed; the gear-menu field "
					+ "commands fall back to the legacy path for this row.", e);
				return null;
			}

			if (location == null)
				return null; // unknown/stale target: leave commands on the legacy path rather than guess.

			return choice =>
			{
				switch (choice.HelpId)
				{
					case "CmdAlwaysVisible":
						return VisibilityItem(choice, field, templateId, location, ViewVisibility.Always);
					case "CmdIfData":
						return VisibilityItem(choice, field, templateId, location, ViewVisibility.IfData);
					case "CmdNormallyHidden":
						return VisibilityItem(choice, field, templateId, location, ViewVisibility.Never);
					case "CmdDataTree-MoveFieldUp":
						return MoveItem(choice, field, location, up: true);
					case "CmdDataTree-MoveFieldDown":
						return MoveItem(choice, field, location, up: false);
					default:
						return null; // not a field command: keep its normal mediator dispatch.
				}
			};
		}

		// A Field Visibility menu item: checked when it is the field's current visibility, executes the
		// SetVisibility override mutation (idempotent — re-choosing the current value is a harmless write).
		private RegionMenuItem VisibilityItem(ChoiceBase choice, LexicalEditRegionField field,
			string templateId, ViewNodeLocation location, ViewVisibility target)
		{
			var label = XCoreMenuBridge.StripAccelerator(choice.GetDisplayProperties().Text);
			var isChecked = location.Visibility == target;
			return new RegionMenuItem(label, isEnabled: true, isChecked: isChecked, children: null,
				execute: () => ApplyFieldVisibility(field, templateId, target));
		}

		// A Move Field item: disabled at the first sibling (up) / last sibling (down) / when alone.
		private RegionMenuItem MoveItem(ChoiceBase choice, LexicalEditRegionField field,
			ViewNodeLocation location, bool up)
		{
			var label = XCoreMenuBridge.StripAccelerator(choice.GetDisplayProperties().Text);
			var canMove = up ? location.CanMoveUp : location.CanMoveDown;
			return new RegionMenuItem(label, isEnabled: canMove, isChecked: false, children: null,
				execute: canMove ? (Action)(() => ApplyMoveField(field, location, up)) : null);
		}

		// Writes a SetVisibility op for the field's template id into the project override and recomposes.
		private void ApplyFieldVisibility(LexicalEditRegionField field, string templateId, ViewVisibility target)
		{
			var op = new ViewOverrideOperation(ViewOverrideOperationKind.SetVisibility, templateId,
				visibility: target);
			MutateOverrideAndRefresh(field, op);
		}

		// Writes a ReorderChildren op on the field's PARENT (the sibling order with this field swapped one
		// position) into the project override and recomposes. A no-op when the move is not possible.
		private void ApplyMoveField(LexicalEditRegionField field, ViewNodeLocation location, bool up)
		{
			var moved = ViewDefinitionOverrideEditor.ComputeMovedOrder(location.SiblingOrder, location.Index, up);
			if (moved == null || string.IsNullOrEmpty(location.ParentStableId))
				return; // first/last/only sibling, or a root-level row with no parent to reorder.
			var op = new ViewOverrideOperation(ViewOverrideOperationKind.ReorderChildren,
				location.ParentStableId, childOrder: moved);
			MutateOverrideAndRefresh(field, op);
		}

		// Loads-or-creates the (class, layout) override, folds the op in, saves it, and recomposes the
		// Avalonia region so the change is visible immediately. The legacy DataTree/Inventory is untouched.
		private void MutateOverrideAndRefresh(LexicalEditRegionField field, ViewOverrideOperation op)
		{
			try
			{
				var store = ViewOverrideStore;
				if (store == null)
					return;

				var existing = store.TryGet(field.ClassName, field.LayoutName)
					?? new ViewDefinitionOverride(field.ClassName, field.LayoutName, "detail", null, null);
				var merged = ViewDefinitionOverrideEditor.MergeOperation(existing, op);
				store.Save(merged);
				RefreshAvaloniaRegion();
			}
			catch (Exception e)
			{
				Logger.WriteError("Applying the field override failed.", e);
			}
		}

		/// <summary>
		/// B7: follows a chooser jump link (e.g. "Edit the Publications list" on Publish In) the
		/// EXACT way the legacy chooser does on link click — the dialog closes, then
		/// <c>ReallySimpleListChooser.HandleAnyJump</c> posts <c>FollowLink</c> with the
		/// <c>FwLinkArgs(tool, guid)</c> built from the layout's <c>chooserLink</c>
		/// (ReallySimpleListChooser.cs:900/1657). Here the flyout has already closed; any open
		/// fenced edit session settles first (the jump navigates away from this record), then the
		/// same mediator message posts.
		/// </summary>
		private void OnRegionLinkRequested(RegionLinkRequest request)
		{
			try
			{
				SettleRegionEdits();
#pragma warning disable 618 // legacy parity: ReallySimpleListChooser.HandleAnyJump posts the same way
				m_mediator.PostMessage("FollowLink", BuildFollowLinkArgs(request));
#pragma warning restore 618
			}
			catch (Exception e)
			{
				Logger.WriteError("Region chooser link jump failed.", e);
			}
		}

		/// <summary>
		/// The legacy translation: <c>new FwLinkArgs(sTool, m_guidLink)</c> — the tool from the
		/// layout's chooserLink, the target guid empty unless the link resolved one (none of the
		/// lexeme-editor chooserInfos set <c>flidTextParam</c>, so empty mirrors legacy exactly).
		/// Internal so the mapping is unit-testable without a mediator.
		/// </summary>
		internal static FwLinkArgs BuildFollowLinkArgs(RegionLinkRequest request)
		{
			var target = Guid.Empty;
			if (!string.IsNullOrEmpty(request.Link.TargetGuid))
				Guid.TryParse(request.Link.TargetGuid, out target);
			return new FwLinkArgs(request.Link.Tool, target);
		}

		// Approved baseline adapter "command-menu-routing" (13.4): the hidden legacy DataTree +
		// DTMenuHandler provide the colleague chain and CurrentSlice context the legacy command
		// handlers require. Created lazily on first right-click; never attached/visible while the
		// Avalonia surface is active.
		private void EnsureMenuCommandAdapter(int targetHvo)
		{
			// The active-host contract (3.10) is enforced, not just documented: driving the hidden
			// legacy DataTree is legal only through an adapter id the host's contract lists. The
			// contract was built from ApprovedBaselineAdapters when the surface activated; this
			// site only claims its own id (the fallback covers a menu raised before activation).
			(m_activeHostContract ?? ActiveHostContract.ForAvalonia(ApprovedBaselineAdapters))
				.AssertLegacyDataTreeDriveAllowed(CommandMenuRoutingAdapterId);

			if (!m_legacySurfaceInitialized)
			{
				EnsureLegacySurfaceInitialized();
				DetachLegacySurfaceFromPanel(); // adapter only: the Avalonia surface stays active
			}
			// 15.4: display logic gating on Visible (e.g. OnDisplayDataTreeInsert) treats the hidden
			// adapter tree as active.
			m_dataEntryForm.IsExternalCommandAdapter = true;

			var current = Clerk?.CurrentObject;
			if (current == null)
				return;
			m_dataEntryForm.ShowObject(current, m_layoutName, m_layoutChoiceField, current, true);

			if (targetHvo == 0)
				return;

			// Targeting hardening (Task B): the legacy command handlers act on m_dataEntryForm.CurrentSlice,
			// so the adapter must point CurrentSlice at the slice bound to the clicked row's object. A first
			// pass over the already-realized slices handles the common case (small sequences build their
			// slices instantly). When the target lives inside an UNREALIZED DummyObjectSlice (a sequence with
			// >= DataTree.kInstantSliceMax items builds lazy placeholders whose Object is the OWNER, not the
			// target), no real slice carries the target hvo yet — realize the lazy slices and retry rather
			// than silently leaving the wrong (or stale) CurrentSlice pointed, which would make the command
			// mutate the wrong object or, for Merge's class guard, silently fail.
			if (TrySetCurrentSliceForHvo(targetHvo))
				return;

			if (RealizeLazySlicesAndRetry(targetHvo))
				return;

			// Fail loud, not silent: if we still cannot produce a slice for the target we must NOT leave
			// CurrentSlice pointed at whatever the previous interaction selected (it would mis-target the
			// command). Clear it so command handlers see "no current slice" and no-op, and log so the
			// degradation is diagnosable from the field rather than only in a debugger.
			m_dataEntryForm.CurrentSlice = null;
			Logger.WriteEvent(string.Format(
				"Region menu command adapter found no DataTree slice for target hvo {0}; CurrentSlice was "
				+ "cleared so the command no-ops rather than mis-targeting another object.", targetHvo));
		}

		// Points CurrentSlice at the (already-realized) slice whose bound object is targetHvo. Returns
		// false when no realized slice matches (the target may still be inside an unrealized dummy).
		private bool TrySetCurrentSliceForHvo(int targetHvo)
		{
			foreach (var sliceObj in m_dataEntryForm.Slices)
			{
				if (sliceObj is Slice slice && slice.IsRealSlice && slice.Object != null
					&& slice.Object.Hvo == targetHvo)
				{
					m_dataEntryForm.CurrentSlice = slice;
					return true;
				}
			}
			return false;
		}

		// Forces every lazy DummyObjectSlice to become real, then retries the hvo match. A dummy stands
		// in for a run of objects in a sequence and reports its OWNER as its Object, so the target hvo
		// cannot match until the dummy is realized into the per-object slices. DataTree.FieldAt(i)
		// realizes the slice at index i in place (replacing the dummy); we walk by index because the
		// collection mutates as dummies expand.
		private bool RealizeLazySlicesAndRetry(int targetHvo)
		{
			try
			{
				for (var i = 0; i < m_dataEntryForm.Slices.Count; i++)
				{
					if (m_dataEntryForm.Slices[i] is Slice slice && !slice.IsRealSlice)
						m_dataEntryForm.FieldAt(i); // realizes the dummy at i in place
				}
			}
			catch (Exception e)
			{
				Logger.WriteError("Realizing lazy DataTree slices for region command targeting failed.", e);
				return false;
			}

			return TrySetCurrentSliceForHvo(targetHvo);
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

		// Viewing parity (Task C2): the label/value splitter width persists per tool — in-session
		// through the host's remembered field, ACROSS sessions through a PropertyTable local setting,
		// mirroring the expansion-persistence pattern above and the legacy slice-splitter behavior
		// (the former host-only field was process-scoped and lost on shutdown). Keyed by tool so each
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

		// Re-resolves and re-shows the region for the current record from current domain state
		// (after an external edit or this surface's commit/cancel).
		private void RefreshAvaloniaRegion()
		{
			if (m_avaloniaEntryForm == null || !ShouldUseAvaloniaLexicalEdit)
				return;
			var current = Clerk?.CurrentObject;
			if (current == null)
				return;

			ShowAvaloniaEntry(current);
		}

		private void OnAvaloniaRegionEditCompleted(object sender, EventArgs e)
		{
			// ONE re-show covers the completed edit AND any refresh held during it (the old
			// NotifyEditCompleted + direct-refresh pair recomposed twice per commit): drop the held
			// delivery and request a single coalesced refresh through the controller's queue.
			if (m_avaloniaRefreshController != null)
			{
				m_avaloniaRefreshController.DiscardHeldRefresh();
				m_avaloniaRefreshController.RequestRefresh();
			}
			else
			{
				RefreshAvaloniaRegion();
			}
		}

		// Assigns the resolved surface and keeps the active-host contract (task 3.10) in lockstep,
		// so the contract reflects the resolved surface from construction on — not only after the
		// first activation (which a headless host may never reach).
		private void SetLexicalEditSurface(LexicalEditSurface surface)
		{
			m_lexicalEditSurface = surface;
			SyncActiveHostContract();
		}

		private void SyncActiveHostContract()
		{
			var kind = ShouldUseAvaloniaLexicalEdit
				? LexicalEditSurfaceKind.Avalonia
				: LexicalEditSurfaceKind.Legacy;
			if (m_activeHostContract == null || m_activeHostContract.ActiveSurface != kind)
			{
				m_activeHostContract = ShouldUseAvaloniaLexicalEdit
					? ActiveHostContract.ForAvalonia(ApprovedBaselineAdapters)
					: ActiveHostContract.ForLegacy();
			}
		}

		private void EnsureAvaloniaSurfaceActive()
		{
			// Re-sync the contract BEFORE realizing the surface so it reflects the activation even
			// if surface construction fails part-way.
			SyncActiveHostContract();

			if (m_avaloniaEntryForm == null)
				EnsureAvaloniaSurfaceInitialized();

			// Task 3.15: the refresh controller must exist for the whole time the surface is active,
			// not only once a record has actually been composed via ShowAvaloniaEntry. A tool that
			// loads directly with UIMode=New (the ordinary case for a user who already has the setting
			// on) shows the surface here on the first idle — and when the clerk has not yet selected a
			// record that first show takes the CurrentObject==null branch, which never reaches
			// ShowAvaloniaEntry. Without wiring the controller here, PropChanged-driven external-edit
			// refresh would silently not work until the user manually navigated to another record once.
			// EnsureAvaloniaRefreshController is idempotent (its m_avaloniaRefreshController != null guard),
			// so the later ShowAvaloniaEntry call is a no-op rather than a duplicate registration.
			EnsureAvaloniaRefreshController();

			DetachLegacySurfaceFromPanel();
			m_avaloniaEntryForm.Show();
			m_avaloniaEntryForm.BringToFront();
		}
	}
}
