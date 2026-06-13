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
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.LCModel;
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
	/// RecordEditView implements a RecordView (view showing one object at a time from a sequence)
	/// in which the single object is displayed using a DataTree configured using XDEs.
	/// It requires that the XML configuration node have the attribute 'templatePath' (in addition
	/// to 'field' as required by RecordView to specify the list of objects). This tells it where
	/// to start looking for XDEs. This path is relative to the FW root directory (DistFiles in
	/// a development system), e.g., "IText\XDEs".
	/// This version uses the DetailControls version of DataTree, and will eventually replace the
	/// original.
	/// </summary>
	public class RecordEditView : RecordView, IVwNotifyChange, IFocusablePanePortion
	{
		#region Data members

		/// <summary>
		/// Mode string for DataTree to use at top level.
		/// </summary>
		protected string m_rootMode;
		/// <summary>
		/// handles creating the context menus for the data tree and funneling commands to the data tree.
		/// </summary>
		private DTMenuHandler m_menuHandler;

		/// <summary>
		/// indicates that when descendant objects are displayed they should be displayed within the context
		/// of its root object
		/// </summary>
		private bool m_showDescendantInRoot;

		private ImageList buttonImages;
		protected Panel m_panel;
		protected DataTree m_dataEntryForm;
		private IContainer components = null;
		private string m_layoutName;
		private string m_layoutChoiceField;
		private string m_titleField;
		private string m_titleStr;
		private string m_printLayout;
		private LexicalEditSurface m_lexicalEditSurface;
		private readonly LexicalEditSurfaceFactory m_lexicalEditSurfaceFactory;
		private readonly LexicalEditSurfaceSelectionService m_surfaceSelectionService = new LexicalEditSurfaceSelectionService();
		private LexicalEditHostControl m_avaloniaEntryForm;
		private bool m_legacySurfaceInitialized;
		private RecordClerkNavigationContext m_recordNavigationContext;
		// Owns the fenced edit context; swapping/clearing through it cancels any open session so an
		// open undo task is never orphaned (an orphan makes the shutdown Save throw "Commit at wrong place").
		private readonly RegionEditContextHolder m_regionEditContext = new RegionEditContextHolder();
		private AvaloniaRegionRefreshController m_avaloniaRefreshController;
		// winforms-free-lexeme-editor.md D4: the host services handed to region editor plugins —
		// today only the legacy-dialog launcher seam (this view is the sanctioned WinForms
		// carve-out; the pane itself stays WinForms-free).
		private RegionEditorServices m_regionEditorServices;
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

		//// <summary>
		//// used to associate menu commands with the slice that sent them
		//// </summary>
		//protected Slice m_sourceOfMenuCommandSlice=null;

		#endregion // Data members

		#region Construction and Removal
		/// <summary>
		/// Initializes a new instance of the <see cref="RecordEditView"/> class.
		/// </summary>
		public RecordEditView()
			: this(new DataTree())
		{
		}

		protected RecordEditView(DataTree dataEntryForm)
		{
			// This must be called before InitializeComponent()
			SetLexicalEditSurface(LexicalEditSurface.WinForms);
			m_dataEntryForm = dataEntryForm;
			m_lexicalEditSurfaceFactory = new LexicalEditSurfaceFactory(
				() => m_dataEntryForm,
				() => new LexicalEditHostControl());
			m_dataEntryForm.CurrentSliceChanged += m_dataEntryForm_CurrentSliceChanged;

			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();

			AccNameDefault = "RecordEditView";		// default accessibility name
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize this as an IxCoreColleague
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="propertyTable"></param>
		/// <param name="configurationParameters"></param>
		/// ------------------------------------------------------------------------------------
		public override void Init(Mediator mediator, PropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			InitBase(mediator, propertyTable, configurationParameters);

			m_showDescendantInRoot = XmlUtils.GetOptionalBooleanAttributeValue(configurationParameters, "showDescendantInRoot", false);

			// retrieve persisted clerk index and set it.
			int idx = m_propertyTable.GetIntProperty(Clerk.PersistedIndexProperty, -1, PropertyTable.SettingsGroup.LocalSettings);
			int lim = Clerk.ListSize;
			if (idx >= 0 && idx < lim)
			{
				int idxOld = Clerk.CurrentIndex;
				try
				{
					Clerk.JumpToIndex(idx);
				}
				catch
				{
					if (lim > idxOld && lim > 0)
						Clerk.JumpToIndex(idxOld >= 0 ? idxOld : 0);
				}
			}

			// If possible make it use the style sheet appropriate for its main window.
			SetLexicalEditSurface(ResolveConfiguredLexicalEditSurface());
			if (!ShouldUseAvaloniaLexicalEdit)
				m_dataEntryForm.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);
			m_fullyInitialized = true;

			Subscriber.Subscribe(EventConstants.ConsideringClosing, ConsideringClosing);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				Subscriber.Unsubscribe(EventConstants.ConsideringClosing, ConsideringClosing);

				if (components != null)
					components.Dispose();
				if (m_dataEntryForm != null)
				{
					m_dataEntryForm.CurrentSliceChanged -= m_dataEntryForm_CurrentSliceChanged;
					if (m_legacySurfaceInitialized)
						m_dataEntryForm.Dispose();
					else if (m_panel != null && m_panel.Controls.Contains(m_dataEntryForm))
						m_panel.Controls.Remove(m_dataEntryForm);
				}
				TearDownAvaloniaSurface();
				m_menuHandler?.Dispose();
				if (!string.IsNullOrEmpty(m_titleField))
					Cache.DomainDataByFlid.RemoveNotification(this);
			}
			m_dataEntryForm = null;
			m_avaloniaEntryForm = null;
			m_avaloniaRefreshController = null;

			base.Dispose(disposing);
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
			m_avaloniaRefreshController?.Dispose();
			SettleRegionEdits();
			m_regionEditContext.Clear();
			TearDownCompanionSlices();
			// The launcher may hold the last media player alive (legacy parity); release it with
			// the surface that handed it to plugins.
			(m_regionEditorServices?.LegacyDialogLauncher as IDisposable)?.Dispose();
			m_regionEditorServices = null;
			m_avaloniaEntryForm?.Dispose();
		}

		#endregion // Construction and Removal

		#region Message Handlers

		public override bool OnRecordNavigation(object argument)
		{
			CheckDisposed();

			if(!m_fullyInitialized)
				return false;
			if (RecordNavigationInfo.GetSendingClerk(argument) != Clerk)
				return false;

			// persist Clerk's CurrentIndex in a db specific way
			string propName = Clerk.PersistedIndexProperty;
			m_propertyTable.SetProperty(propName, Clerk.CurrentIndex, PropertyTable.SettingsGroup.LocalSettings, true);
			m_propertyTable.SetPropertyPersistence(propName, true, PropertyTable.SettingsGroup.LocalSettings);
			var window = m_propertyTable.GetValue<XWindow>("window");

			try
			{
				window.SuspendIdleProcessing();
				ShowRecord(argument as RecordNavigationInfo);
			}
			finally
			{
				window.ResumeIdleProcessing();
			}

			// Selection bridge (task 3.12): the real mediator broadcast delivered a record navigation
			// for this host's clerk, so let bridge subscribers (the Avalonia surface) follow it.
			m_recordNavigationContext?.NotifyCurrentRecordChanged();
			return true;	//we handled this.
		}

		private void ConsideringClosing(object obj)
		{
			CheckDisposed();

			if (!(obj is CancelEventArgs args))
			{
				Debug.Assert(false, "Received unexpected object type.");
				return;
			}
			// Return if the close has already been canceled by another Subscriber.
			if (args.Cancel)
			{
				return;
			}
			args.Cancel = !PrepareToGoAway();
		}

		/// <summary>
		/// From IxCoreContentControl
		/// </summary>
		/// <returns>true if ok to go away</returns>
		public override bool PrepareToGoAway()
		{
			CheckDisposed();

			// Auto-save (14.4): leaving the tool/area settles any open fenced session the same way
			// legacy slices save as the user moves on. Unconditional (the helper no-ops when no
			// session is open), so a session that survived a surface flip still settles safely.
			SettleRegionEdits();
			if (!ShouldUseAvaloniaLexicalEdit && m_dataEntryForm != null)
			{
				m_dataEntryForm.PrepareToGoAway();
			}
			return base.PrepareToGoAway();
		}

		private void m_dataEntryForm_CurrentSliceChanged(object sender, EventArgs e)
		{
			if (!m_showDescendantInRoot)
				return;

			if (m_dataEntryForm.Descendant != null && Clerk.CurrentObject != m_dataEntryForm.Descendant)
				// if the user has clicked on a different descendant's slice, update the currently
				// selected record (we want to keep the browse view in sync), but do not change the
				// focus
				Clerk.JumpToRecord(m_dataEntryForm.Descendant.Hvo, true);
		}

		public void OnPropertyChanged(string name)
		{
			CheckDisposed();

			// Viewing parity (11.x): the View → Show Hidden Fields toggle re-resolves the Avalonia
			// region just like it rebuilds the legacy DataTree.
			if (name != null && name.StartsWith("ShowHiddenFields-", StringComparison.Ordinal))
			{
				if (ShouldUseAvaloniaLexicalEdit)
					RefreshAvaloniaRegion();
				return;
			}

			if (name != LexicalEditSurfaceResolver.UIModePropertyName)
				return;

			var newSurface = ResolveConfiguredLexicalEditSurface();
			if (newSurface == m_lexicalEditSurface)
				return;

			// Settle any open fenced session BEFORE flipping the surface — without this, flipping
			// UIMode mid-edit would let Clerk.SaveOnChangeRecord force-commit invalid staged state
			// (review round 2).
			SettleRegionEdits();
			SetLexicalEditSurface(newSurface);
			ShowRecord(new RecordNavigationInfo(Clerk, Clerk.SuppressSaveOnChangeRecord, false, true));
		}

		#endregion // Message Handlers

		#region Other methods

		protected override void SetInfoBarText()
		{
			if (m_informationBar == null)
				return;

			// See if we have an AlternativeTitle string table id for an alternate title.
			string titleStr = null;
			if (!string.IsNullOrEmpty(m_titleStr))
			{
				titleStr = m_titleStr;
			}
			else if (!string.IsNullOrEmpty(m_titleField))
			{
				ICmObject curObj = Clerk.CurrentObject;
				if (curObj != null)
				{
					int flid = Cache.MetaDataCacheAccessor.GetFieldId2(curObj.ClassID, m_titleField, true);
					int hvo = Cache.DomainDataByFlid.get_ObjectProp(curObj.Hvo, flid);
					if (hvo != 0)
					{
						ICmObject titleObj = Cache.ServiceLocator.GetObject(hvo);
						titleStr = titleObj.ShortName;
					}
				}
			}

			if (!string.IsNullOrEmpty(titleStr))
				((IPaneBar) m_informationBar).Text = titleStr;
			else
				base.SetInfoBarText();
		}

		/// <summary>
		/// Schedules the record to be shown when the application is idle.
		/// </summary>
		/// <param name="rni">The record navigation info.</param>
		protected override void ShowRecord(RecordNavigationInfo rni)
		{
			if (!rni.SkipShowRecord)
			{
				m_mediator.IdleQueue.Add(IdleQueuePriority.High, ShowRecordOnIdle, rni);
			}
		}

		/// <summary>
		/// Shows the record.
		/// </summary>
		protected override void ShowRecord()
		{
			ShowRecord(new RecordNavigationInfo(Clerk, Clerk.SuppressSaveOnChangeRecord, false, false));
		}

		/// <summary>
		/// Shows the record on idle. This is where the record is actually shown.
		/// </summary>
		/// <param name="parameter">The parameter.</param>
		bool ShowRecordOnIdle(object parameter)
		{
			if (IsDisposed)
				return true;

			base.ShowRecord();
			int msStart = Environment.TickCount;
			Debug.Assert(m_dataEntryForm != null);

			var rni = (RecordNavigationInfo) parameter;
			// Auto-save (14.4) must run BEFORE the clerk's save-on-change-record:
			// RecordClerk.SaveOnChangeRecord force-EndUndoTasks any open undo task wholesale
			// (LT-16673), which would commit invalid staged state past the validation gate.
			// Unconditional: a no-op while legacy is active (no fenced session can be open).
			SettleRegionEdits();
			bool oldSuppressSaveOnChangeRecord = Clerk.SuppressSaveOnChangeRecord;
			Clerk.SuppressSaveOnChangeRecord = rni.SuppressSaveOnChangeRecord;
			PrepCacheForNewRecord();
			Clerk.SuppressSaveOnChangeRecord = oldSuppressSaveOnChangeRecord;

			if (Clerk.CurrentObject == null || Clerk.SuspendLoadingRecordUntilOnJumpToRecord)
			{
				if (ShouldUseAvaloniaLexicalEdit)
				{
					// Active-host contract (task 3.10): do not touch the legacy DataTree while Avalonia is active.
					// The record may be gone (deleted elsewhere); cancel rather than orphan the session.
					m_regionEditContext.Clear();
					EnsureAvaloniaSurfaceActive();
					TearDownCompanionSlices();
					m_avaloniaEntryForm.Clear();
				}
				else
				{
					EnsureLegacySurfaceVisible();
					m_dataEntryForm.Hide();
					m_dataEntryForm.Reset();	// in case user deleted the object it was based upon.
				}
				return true;
			}
			try
			{
				// Active-host contract (task 3.10): when the Avalonia surface is active we do NOT initialize
				// or drive the legacy DataTree. Only the active surface is created and shown.
				if (ShouldUseAvaloniaLexicalEdit)
				{
					EnsureAvaloniaSurfaceActive();
				}
				else
				{
					EnsureLegacySurfaceInitialized();
					EnsureLegacySurfaceVisible();
				}

				// Enhance: Maybe do something here to allow changing the templates without the starting the application.
				ICmObject obj = Clerk.CurrentObject;

				if (m_showDescendantInRoot)
				{
					// find the root object of the current object
					while (obj.Owner != Clerk.OwningObject)
						obj = obj.Owner;
				}

				if (ShouldUseAvaloniaLexicalEdit && m_avaloniaEntryForm != null)
				{
					// Sections 6/7: the product route composes the COMPLETE entry view from the live
					// compiled layouts (full cross-object walk, headers, ifdata) and falls back to the
					// fixed first slice only if composition fails; both are editable through the fenced
					// LCModel session (6.8/6.10) with refresh propagation (3.15).
					ShowAvaloniaEntry(obj);
				}
				else
				{
					m_dataEntryForm.ShowObject(obj, m_layoutName, m_layoutChoiceField, Clerk.CurrentObject, ShouldSuppressFocusChange(rni));
				}
			}
			catch (Exception error)
			{
				//don't really need to make the program stop just because we could not show this record.
				IApp app = m_propertyTable.GetValue<IApp>("App");
				ErrorReporter.ReportException(error, app.SettingsKey, m_propertyTable.GetValue<IFeedbackInfoProvider>("FeedbackInfoProvider").SupportEmailAddress,
					null, false);
			}
			int msEnd = Environment.TickCount;
			Debug.WriteLineIf(RuntimeSwitches.RecordTimingSwitch.TraceInfo, "ShowRecord took " + (msEnd - msStart) + " ms", RuntimeSwitches.RecordTimingSwitch.DisplayName);
			return true;
		}

		/// <summary>
		/// If this is not the focused pane in a multipane suppress, or if the navigation info requested
		/// a suppression of the focus change then return true (suppress)
		/// </summary>
		/// <param name="rni"></param>
		/// <returns></returns>
		private bool ShouldSuppressFocusChange(RecordNavigationInfo rni)
		{
			return !IsFocusedPane || rni.SuppressFocusChange;
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

			return m_surfaceSelectionService.Decide(uiMode, toolName).Surface;
		}

		// This plus the name of the vector gives a unique context for the DataTree control
		// parameters (e.g. "lexicon.basicEdit.DataTree").
		private string DataTreePersistContext
		{
			get
			{
				var persistContext = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "persistContext");
				return string.IsNullOrEmpty(persistContext)
					? m_vectorName + ".DataTree"
					: m_vectorName + "." + persistContext + ".DataTree";
			}
		}

		private void EnsureLegacySurfaceInitialized()
		{
			if (m_legacySurfaceInitialized)
				return;

			m_dataEntryForm.PersistenceProvder = new PersistenceProvider(m_mediator, m_propertyTable, DataTreePersistContext);

			// In Avalonia mode Init skips the stylesheet (the legacy tree is inactive); the lazy
			// command-routing adapter still needs it before ShowObject builds slices.
			if (m_dataEntryForm.StyleSheet == null)
				m_dataEntryForm.StyleSheet = FontHeightAdjuster.StyleSheetFromPropertyTable(m_propertyTable);

			SetupSliceFilter();
			m_dataEntryForm.Dock = DockStyle.Fill;
			m_dataEntryForm.SmallImages = m_propertyTable.GetValue<ImageCollection>("smallImages");
			string sDatabase = Cache.ProjectId.Name;
			m_dataEntryForm.Initialize(Cache, true, Inventory.GetInventory("layouts", sDatabase),
				Inventory.GetInventory("parts", sDatabase));
			m_dataEntryForm.Init(m_mediator, m_propertyTable, m_configurationParameters);
			if (m_dataEntryForm.AccessibilityObject != null)
				m_dataEntryForm.AccessibilityObject.Name = "RecordEditView.DataTree";

			m_menuHandler = DTMenuHandler.Create(m_dataEntryForm, m_configurationParameters);
			m_menuHandler.Init(m_mediator, m_propertyTable, m_configurationParameters);
			m_dataEntryForm.SetContextMenuHandler(m_menuHandler.ShowSliceContextMenu);

			AttachLegacySurfaceToPanel();

			m_legacySurfaceInitialized = true;
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
			var owningEntry = changed as ILexEntry ?? changed.OwnerOfClass<ILexEntry>();
			return owningEntry != null && owningEntry.Hvo == current.Hvo;
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

			if (!(obj is ILexEntry lexEntry))
			{
				m_regionEditContext.Clear();
				TearDownCompanionSlices();
				m_avaloniaEntryForm.ShowMessage(FwAvaloniaStrings.EntryTypeUnsupported);
				return;
			}

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
				composed = FullEntryRegionComposer.Compose(lexEntry, Cache, showHidden,
					services: EnsureRegionEditorServices());
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
			m_avaloniaEntryForm.ShowRegion(region, editContext,
				wsTag => LexicalEditRegionBuilder.ActivateKeyboardForWritingSystem(Cache, wsTag),
				GetPersistedExpansionState, PersistExpansionState,
				OnRegionMenuRequested, OnRegionLinkRequested);
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
					var items = XCoreMenuBridge.BuildMenuItems(window, idArray);
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
			foreach (var sliceObj in m_dataEntryForm.Slices)
			{
				if (sliceObj is Slice slice && slice.Object != null && slice.Object.Hvo == targetHvo)
				{
					m_dataEntryForm.CurrentSlice = slice;
					break;
				}
			}
		}

		// Viewing parity (11.8): expansion state persists per header stable id — in-session through the
		// dictionary, across sessions through PropertyTable local settings, the legacy ExpansionStateKey
		// behavior. Per-instance (review round 1): a process-wide static leaked state across
		// projects/windows for the app lifetime.
		private readonly Dictionary<string, bool> m_expansionStates = new Dictionary<string, bool>();

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

			DetachLegacySurfaceFromPanel();
			m_avaloniaEntryForm.Show();
			m_avaloniaEntryForm.BringToFront();
		}

		private void EnsureLegacySurfaceVisible()
		{
			SyncActiveHostContract();

			AttachLegacySurfaceToPanel();
			// The legacy DataTree builds its own MessageSlice/ChorusSystem; release the Avalonia
			// lane's companions so two Chorus systems never sit on the project at once.
			TearDownCompanionSlices();
			m_avaloniaEntryForm?.Hide();
			m_dataEntryForm.Show();
			m_dataEntryForm.BringToFront();
		}

		private void AttachLegacySurfaceToPanel()
		{
			if (m_dataEntryForm == null || m_panel == null)
				return;

			if (!m_panel.Controls.Contains(m_dataEntryForm))
				m_panel.Controls.Add(m_dataEntryForm);
		}

		private void DetachLegacySurfaceFromPanel()
		{
			if (m_dataEntryForm == null || m_panel == null)
				return;

			m_dataEntryForm.Hide();
			if (m_panel.Controls.Contains(m_dataEntryForm))
				m_panel.Controls.Remove(m_dataEntryForm);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Base method saves any time you switch between records.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void PrepCacheForNewRecord()
		{
			Clerk.SaveOnChangeRecord();
		}

		/// <summary>
		/// Read in the parameters to determine which collection we are editing.
		/// </summary>
		protected override void ReadParameters()
		{
			base.ReadParameters();

			m_layoutName = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layout");
			m_layoutChoiceField = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "layoutChoiceField");
			m_titleField = XmlUtils.GetAttributeValue(m_configurationParameters, "titleField");
			if (!string.IsNullOrEmpty(m_titleField))
				Cache.DomainDataByFlid.AddNotification(this);
			string titleId = XmlUtils.GetAttributeValue(m_configurationParameters, "altTitleId");
			if (titleId != null)
				m_titleStr = StringTable.Table.GetString(titleId, "AlternativeTitles");
			m_printLayout = XmlUtils.GetAttributeValue(m_configurationParameters, "printLayout");
		}

		protected override void SetupDataContext()
		{
			Debug.Assert(m_configurationParameters != null);

			base.SetupDataContext();

			// InitBase() calls SetupDataContext() before RecordEditView.Init() resolves the surface, so
			// resolve it here too — otherwise the first surface initialization would use the ctor default
			// (WinForms) and the active-host contract (task 3.10) would be violated for an Avalonia start.
			SetLexicalEditSurface(ResolveConfiguredLexicalEditSurface());

			// Surface-agnostic: the record list bar must update regardless of which detail surface is active.
			Clerk.UpdateRecordTreeBarIfNeeded();

			// Active-host contract (task 3.10): initialize only the active surface; the inactive surface is
			// not instantiated or driven. The legacy DataTree is initialized here only when legacy is active;
			// the Avalonia surface is created lazily in ShowRecordOnIdle so its construction stays on the
			// idle path (the inactive legacy DataTree is never built).
			if (!ShouldUseAvaloniaLexicalEdit)
			{
				EnsureLegacySurfaceInitialized();
				EnsureLegacySurfaceVisible();
			}
			else
			{
				DetachLegacySurfaceFromPanel();
			}
		}

		/// <summary>
		/// a slice filter is used to hide some slices.
		/// </summary>
		/// <remarks> this will set up a filter even if you do not specify a filter path, since
		/// some filtering is done by the FDO classes (CmObject.IsFieldRelevant)
		/// </remarks>
		/// <example>
		///		to set up a slice filter,kids the relative path in the filterPath attribute of the parameters:
		///		<control assemblyPath="xWorks.dll" class="SIL.FieldWorks.XWorks.RecordEditView">
		///			<parameters field="Entries" templatePath="LexEd\XDEs" filterPath="LexEd\basicFilter.xml">
		///			...
		///</example>
		private void SetupSliceFilter()
		{
			try
			{
				string filterPath = XmlUtils.GetOptionalAttributeValue(m_configurationParameters, "filterPath");
				if (filterPath!= null)
				{
					if (!Platform.IsWindows)
					{
						// TODO-Linux: fix the data
						filterPath = filterPath.Replace(@"\", "/");
					}

					var document = new XmlDocument();
					document.Load(FwDirectoryFinder.GetCodeFile(filterPath));
					m_dataEntryForm.SliceFilter = new SliceFilter(document);
				}
				else //just set up a minimal filter
					m_dataEntryForm.SliceFilter = new SliceFilter();
			}
			catch (Exception e)
			{
				// SIL.Utils-qualified: the SIL.Reporting using (Logger) also exports a ConfigurationException.
				throw new SIL.Utils.ConfigurationException ("Could not load the filter.", m_configurationParameters, e);
			}
		}

		#endregion // Other methods

		/// <summary>
		/// get our DataTree for testing
		/// </summary>
		public DataTree DatTree
		{
			get
			{
				CheckDisposed();

				return m_dataEntryForm;
			}
		}

		/// <summary>
		/// subclasses should override if they have more targets
		/// </summary>
		/// <returns></returns>
		protected override void GetMessageAdditionalTargets(List<IxCoreColleague> collector)
		{
			if(!m_fullyInitialized)
				return;

			// Legacy mode: the DataTree + menu handler are the normal targets. Avalonia mode: they
			// participate ONLY once the lazy "command-menu-routing" baseline adapter exists (13.4),
			// so the legacy command handlers can resolve and execute the context-menu commands.
			if (m_legacySurfaceInitialized && m_dataEntryForm != null)
				collector.Add(m_dataEntryForm);

			if (m_legacySurfaceInitialized && m_menuHandler != null)
				collector.Add(m_menuHandler);
		}

		#region IxCoreCtrlTabProvider implementation

		public override Control PopulateCtrlTabTargetCandidateList(List<Control> targetCandidates)
		{
			if (targetCandidates == null)
				throw new ArgumentNullException("targetCandidates");

			if (ShouldUseAvaloniaLexicalEdit && m_avaloniaEntryForm != null)
			{
				targetCandidates.Add(m_avaloniaEntryForm);
				return m_avaloniaEntryForm.ContainsFocus ? m_avaloniaEntryForm : null;
			}

			// when switching panes, we want to give the focus to the CurrentSlice(if any)
			if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null)
			{
				targetCandidates.Add(m_dataEntryForm.CurrentSlice);
				return m_dataEntryForm.CurrentSlice.ContainsFocus ? m_dataEntryForm.CurrentSlice : null;
			}

			return base.PopulateCtrlTabTargetCandidateList(targetCandidates);
		}

		#endregion  IxCoreCtrlTabProvider implementation

		private bool ShouldUseAvaloniaLexicalEdit
		{
			get { return m_lexicalEditSurface == LexicalEditSurface.Avalonia; }
		}

		#region Component Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			System.Resources.ResourceManager resources = new System.Resources.ResourceManager(typeof(RecordEditView));
			this.buttonImages = new System.Windows.Forms.ImageList(this.components);
			this.m_panel = new System.Windows.Forms.Panel();
			this.m_dataEntryForm.AccessibilityObject.Name = "RecordEditView.DataTree";
			this.SuspendLayout();
			//
			// m_informationBar
			//
			//this.m_informationBar.DockPadding.All = 5;
			//this.m_informationBar.Name = "m_informationBar";
			//
			// buttonImages
			//
			this.buttonImages.ImageSize = new System.Drawing.Size(16, 16);
			this.buttonImages.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("buttonImages.ImageStream")));
			this.buttonImages.TransparentColor = System.Drawing.Color.Fuchsia;
			//
			// m_panel
			//
			this.m_panel.Controls.Add(this.m_dataEntryForm);
			this.m_panel.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_panel.Location = new System.Drawing.Point(0, 0);
			this.m_panel.Name = "m_panel";
			this.m_panel.Size = new System.Drawing.Size(752, 150);
			this.m_panel.TabIndex = 2;
			this.m_panel.AccessibilityObject.Name = "Panel";
			//
			// m_dataEntryForm
			//
			this.m_dataEntryForm.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_dataEntryForm.Location = new System.Drawing.Point(0, 0);
			this.m_dataEntryForm.Name = "m_dataEntryForm";
			this.m_dataEntryForm.PersistenceProvder = null;
			this.m_dataEntryForm.Size = new System.Drawing.Size(752, 150);
			this.m_dataEntryForm.SliceFilter = null;
			this.m_dataEntryForm.SmallImages = null;
			this.m_dataEntryForm.StyleSheet = null;
			this.m_dataEntryForm.TabIndex = 3;
			//
			// RecordEditView
			//
			//this.Controls.Add(this.m_informationBar);
			this.Controls.Add(this.m_panel);
			this.Name = "RecordEditView";
			this.Controls.SetChildIndex(this.m_panel, 0);
			//this.Controls.SetChildIndex(this.m_informationBar, 0);
			this.ResumeLayout(false);

		}
		#endregion

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			// if the title field property changed, update the pane bar text
			if (!string.IsNullOrEmpty(m_titleField))
			{
				ICmObject curObj = Clerk.CurrentObject;
				if (curObj != null)
				{
					int flid = Cache.MetaDataCacheAccessor.GetFieldId2(curObj.ClassID, m_titleField, true);
					if (hvo == curObj.Hvo && tag == flid)
						SetInfoBarText();
				}
			}
		}

		#region Print methods

		public bool OnPrint(object args)
		{
			CheckDisposed();

			if (m_printLayout == null || Clerk.CurrentObject == null)
				return false;
			// Don't bother; this edit view does not specify a print layout, or there's nothing to print.

			var area = m_propertyTable.GetStringProperty("areaChoice", null);
			string toolId;
			switch (area)
			{
				case "notebook":
					toolId = "notebookDocument";
					break;
				case "lexicon":
					toolId = "lexiconDictionary";
					break;
				default:
					return false;
			}
			var docViewConfig = FindToolInXMLConfig(toolId);
			if (docViewConfig == null)
				return false;
			var innerControlNode = GetToolInnerControlNodeWithRightLayout(docViewConfig);
			if (innerControlNode == null)
				return false;
			using (var docView = CreateDocView(innerControlNode))
			{
				if (docView == null)
					return false;

				using (var pd = new PrintDocument())
				using (var dlg = new PrintDialog())
				{
					dlg.Document = pd;
					dlg.AllowSomePages = true;
					dlg.AllowSelection = false;
					dlg.PrinterSettings.FromPage = 1;
					dlg.PrinterSettings.ToPage = 1;
					if (dlg.ShowDialog() == DialogResult.OK)
					{
						// REVIEW: .NET does not appear to handle the collation setting correctly
						// so for now, we do not support non-collated printing.  Forcing the setting
						// seems to work fine.
						dlg.Document.PrinterSettings.Collate = true;
						docView.PrintFromDetail(pd, Clerk.CurrentObject.Hvo);
					}
				}
				return true;
			}
		}

		private XmlNode GetToolInnerControlNodeWithRightLayout(XmlNode docViewConfig)
		{
			var paramNode = docViewConfig.SelectSingleNode("control//parameters[@layout = \"" + m_printLayout + "\"]");
			if (paramNode == null)
				return null;
			return paramNode.ParentNode;
		}

		private XmlNode FindToolInXMLConfig(string docToolValue)
		{
			// At this point m_configurationParameters holds the RecordEditView parameter node.
			// We need to find the tool that has a value attribute matching our input
			// parameter (docToolValue).
			var path = ".//tools/tool[@value = \""+docToolValue+"\"]";
			return m_configurationParameters.OwnerDocument.SelectSingleNode(path);
		}

		private XmlDocView CreateDocView(XmlNode parentConfigNode)
		{
			Debug.Assert(parentConfigNode != null,
				"Can't create a view without the XML control configuration.");
			XmlDocView docView;
			try
			{
				docView = (XmlDocView)DynamicLoader.CreateObjectUsingLoaderNode(parentConfigNode);
			}
			catch (Exception)
			{
				return null;
			}
			// TODO: Not right yet!
			docView.Init(m_mediator, m_propertyTable, parentConfigNode.SelectSingleNode("parameters"));
			return docView;
		}

		#endregion

		public bool IsFocusedPane
		{
			get;
			set;
		}
	}
}
