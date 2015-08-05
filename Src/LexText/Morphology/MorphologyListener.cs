// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: MorphologyListener.cs
// Responsibility: Randy Regnier
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.IText;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for MorphologyListener.
	/// JohnT: rather contrary to its name, appears to be a place to put handlers for commands common
	/// to tools in the Words area.
	/// </summary>
	[XCore.MediatorDispose]
	public class MorphologyListener : IxCoreColleague, IVwNotifyChange, IFWDisposable
	{
		#region Data members

		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		private XCore.Mediator m_mediator;
		private IPropertyTable m_propertyTable;
		private IWfiWordformRepository m_wordformRepos;

		#endregion Data members

		#region IDisposable & Co. implementation
		// Region last reviewed: never

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// <summary>
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed = false;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~MorphologyListener()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		/// Implemented to reset spell-checking everywhere when the spelling status of a wordform changes.
		/// </summary>
		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == WfiWordformTags.kflidSpellingStatus)
			{
				RestartSpellChecking();
				// This keeps the spelling dictionary in sync with the WFI.
				// Arguably this should be done in FDO. However the spelling dictionary is used to
				// keep the UI showing squiggles, so it's also arguable that it is a UI function.
				// In any case it's easier to do it in PropChanged (which also fires in Undo/Redo)
				// than in a data-change method which does not.
				var wf = m_wordformRepos.GetObject(hvo);
				string text = wf.Form.VernacularDefaultWritingSystem.Text;
				if (!string.IsNullOrEmpty(text))
				{
					SpellingHelper.SetSpellingStatus(text, Cache.DefaultVernWs,
													Cache.LanguageWritingSystemFactoryAccessor,
													wf.SpellingStatus == (int)SpellingStatusStates.correct);
				}

			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SupressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
				if (Cache != null && !Cache.IsDisposed && Cache.DomainDataByFlid != null)
					Cache.DomainDataByFlid.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_propertyTable = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation

		public virtual void Init(Mediator mediator, IPropertyTable propertyTable, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_propertyTable = propertyTable;
			m_mediator.AddColleague(this);
			Cache = m_propertyTable.GetValue<FdoCache>("cache");
			m_wordformRepos = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			Cache.DomainDataByFlid.AddNotification(this);
			if (IsVernacularSpellingEnabled())
				OnEnableVernacularSpelling();
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			targets.Add(this);
			return targets.ToArray();
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Medium; }
		}

		#endregion IxCoreColleague implementation

		#region XCore Message handlers

		/// <summary>
		///
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMergeWordform(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
		public bool OnMergeWordform(object argument)
		{
			CheckDisposed();

			// Do something meaningful,
			// whenever the definition of merging wordforms gets developed.
			MessageBox.Show(MEStrings.ksCannotMergeWordformsYet);
			return true;
		}

		/// <summary>
		/// Enable the spelling tool always. Correct the property value if need be to match whether
		/// we are actually showing vernacular spelling.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayUseVernSpellingDictionary(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Enabled = display.Visible = Cache != null;
			if (Cache == null)
				return true;
			display.Checked = IsVernacularSpellingEnabled();
			return true; //we've handled this
		}

		public bool OnUseVernSpellingDictionary(object argument)
		{
			bool checking = !IsVernacularSpellingEnabled();
			if (checking)
				OnEnableVernacularSpelling();
			else
				WfiWordformServices.DisableVernacularSpellingDictionary(Cache);
			m_propertyTable.SetProperty("UseVernSpellingDictionary", checking, true, true);
			RestartSpellChecking();
			return true;
		}

		// currently duplicated in FLExBridgeListener, to avoid an assembly dependency.
		private bool IsVernacularSpellingEnabled()
		{
			return m_propertyTable.GetValue("UseVernSpellingDictionary", true);
		}

		private void RestartSpellChecking()
		{
			IApp app = m_propertyTable.GetValue<IApp>("App");
			if (app != null)
			{
				app.RestartSpellChecking();
			}
		}

		/// <summary>
		/// Implement the add words to spelling dictionary command. (May be called by reflection,
		/// though I don't think there is a current explicit menu item.)
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnAddWordsToSpellDict(object argument)
		{
			CheckDisposed();

			if (Cache == null)
				return false; // impossible?
			WfiWordformServices.ConformSpellingDictToWordforms(Cache);
			return true; // handled
		}

		private FdoCache Cache { get; set; }

		/// <summary>
		/// Enable vernacular spelling.
		/// </summary>
		void OnEnableVernacularSpelling()
		{
			// Enable all vernacular spelling dictionaries by changing those that are set to <None>
			// to point to the appropriate Locale ID. Do this BEFORE updating the spelling dictionaries,
			// otherwise, the update won't see that there is any dictionary set to update.
			var cache = Cache;
			foreach (IWritingSystem wsObj in cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				// This allows it to try to find a dictionary, but doesn't force one to exist.
				if (wsObj.SpellCheckingId == null || wsObj.SpellCheckingId == "<None>") // LT-13556 new langs were null here
					wsObj.SpellCheckingId = wsObj.Id.Replace('-', '_');
			}
			// This forces the default vernacular WS spelling dictionary to exist, and updates
			// all existing ones.
			OnAddWordsToSpellDict(null);
		}

		public virtual bool OnDisplayGotoWfiWordform(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (InFriendlyArea && m_mediator != null)
			{
				var clrk = m_propertyTable.GetValue<RecordClerk>("ActiveClerk");
				if (clrk != null && !clrk.IsDisposed && clrk.Id == "concordanceWords")
				{
					display.Visible = true;

					// we only want to enable if we have more than one, because there's no point in finding
					// the one we've already selected.
					display.Enabled = m_wordformRepos.Count > 1;
					return true;
				}
			}
			// Unless everything lines up to make the command workable disable it.
			display.Enabled = display.Visible = false;
			return true; //we've handled this
		}

		/// <summary>
		/// Try to find a WfiWordform object corresponding the the focus selection.
		/// If successful return its guid, otherwise, return Guid.Empty.
		/// </summary>
		/// <returns></returns>
		internal static Guid ActiveWordform(FdoCache cache, IPropertyTable propertyTable)
		{
			IApp app = propertyTable.GetValue<IApp>("App");
			if (app == null)
				return Guid.Empty;
			IFwMainWnd window = app.ActiveMainWindow as IFwMainWnd;
			if (window == null)
				return Guid.Empty;
			IRootSite activeView = window.ActiveView;
			if (activeView == null)
				return Guid.Empty;
			List<IVwRootBox> roots = activeView.AllRootBoxes();
			if (roots.Count < 1)
				return Guid.Empty;
			SelectionHelper helper = SelectionHelper.Create(roots[0].Site);
			if (helper == null)
				return Guid.Empty;
			ITsString word = helper.SelectedWord;
			if (word == null || word.Length == 0)
				return Guid.Empty;
#if WANTPORT // FWR-2784
			int hvoWordform = cache.LangProject.WordformInventoryOA.GetWordformId(word);
			if (hvoWordform == 0 || cache.IsDummyObject(hvoWordform))
				return Guid.Empty;
			return cache.GetGuidFromId(hvoWordform);
#else
			return Guid.Empty;
#endif
		}

		public bool OnDisplayEditSpellingStatus(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		/// Called by reflection to implement the command.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnEditSpellingStatus(object argument)
		{
			// Without checking both the SpellingStatus and (virtual) FullConcordanceCount
			// fields for the ActiveWordform() result, it's too likely that the user
			// will get a puzzling "Target not found" message popping up.  See LT-8717.
			FwLinkArgs link = new FwAppArgs(Cache.ProjectId.Handle,
				"toolBulkEditWordforms", Guid.Empty);
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeReviewUndecidedSpelling"));
			m_mediator.PostMessage("FollowLink", link);
			return true;
		}

		public bool OnDisplayViewIncorrectWords(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		public bool OnViewIncorrectWords(object argument)
		{
			FwLinkArgs link = new FwAppArgs(Cache.ProjectId.Handle,
				"Analyses", ActiveWordform(Cache, m_propertyTable));
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeCorrectSpelling"));
			m_mediator.PostMessage("FollowLink", link);
			return true;
		}

		/// <summary>
		/// Handles the xCore message to go to a wordform.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnGotoWfiWordform(object argument)
		{
			CheckDisposed();

			using (var dlg = new WordformGoDlg())
			{
				dlg.SetDlgInfo(Cache, null, m_mediator, m_propertyTable);
				if (dlg.ShowDialog() == DialogResult.OK)
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", dlg.SelectedObject.Hvo);
			}
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		protected bool InFriendlyArea
		{
			get
			{
				return (m_propertyTable.GetValue<string>("areaChoice") == "textsWords");
			}
		}

		/// <summary>
		/// Handle enabled menu items for jumping to another tool, or another location in the
		/// current tool.
		/// </summary>
		public virtual bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			if (!InFriendlyArea)
				return false;
			var command = (Command)commandObject;
			if (command.TargetId != Guid.Empty)
			{
				var tool = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "tool");
				m_mediator.PostMessage("FollowLink", new FwLinkArgs(tool, command.TargetId));
				command.TargetId = Guid.Empty;	// clear the target for future use.
				return true;
			}
			return false;
		}
		#endregion XCore Message handlers
	}

	/// <summary>
	/// WordsEditToolMenuHandler inherits from DTMenuHandler and adds some special smarts.
	/// this class would normally be constructed by the factory method on DTMenuHandler,
	/// when the XML configuration of the RecordEditView specifies this class.
	///
	/// This is an IxCoreColleague, so it gets a chance to modify
	/// the display characteristics of the menu just before the menu is displayed.
	/// </summary>
	public class WordsEditToolMenuHandler : DTMenuHandler
	{
		private XmlNode m_mainWindowNode;

		#region Properties

		private XmlNode MainWindowNode
		{
			get
			{
				if (m_mainWindowNode == null)
					m_mainWindowNode = m_propertyTable.GetValue<XmlNode>("WindowConfiguration");
				return m_mainWindowNode;
			}
		}

		/// <summary>
		/// Returns the object of the current slice, or (if no slice is marked current)
		/// the object of the first slice, or (if there are no slices, or no data entry form) null.
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification="FieldAt() returns a reference")]
		private ICmObject CurrentSliceObject
		{
			get
			{
				if (m_dataEntryForm == null)
					return null;
				if (m_dataEntryForm.CurrentSlice != null)
					return m_dataEntryForm.CurrentSlice.Object;
				if (m_dataEntryForm.Slices.Count == 0)
					return null;
				return m_dataEntryForm.FieldAt(0).Object;
			}
		}

		private IWfiWordform Wordform
		{
			get
			{
				// Note that we may get here after the owner (or the owner's owner) of the
				// current object has been deleted: see LT-10124.
				var curObject = CurrentSliceObject;
				if (curObject is IWfiWordform)
					return (IWfiWordform)curObject;

				if (curObject is IWfiAnalysis && curObject.Owner != null)
					return (IWfiWordform)(curObject.Owner);

				if (curObject is IWfiGloss && curObject.Owner != null)
				{
					var anal = curObject.OwnerOfClass<IWfiAnalysis>();
					if (anal.Owner != null)
						return anal.OwnerOfClass<IWfiWordform>();
				}
				return null;
			}
		}

		private IWfiAnalysis Analysis
		{
			get
			{
				var curObject = CurrentSliceObject;
				if (curObject is IWfiAnalysis)
					return (IWfiAnalysis)curObject;
				if (curObject is IWfiGloss)
					return curObject.OwnerOfClass<IWfiAnalysis>();
				return null;
			}
		}

		private IWfiGloss Gloss
		{
			get
			{
				var curObject = CurrentSliceObject;
				if (curObject is IWfiGloss)
					return (IWfiGloss)curObject;
				return null;
			}
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>
		/// This is something of a hack until we come up with a generic solution to
		/// the problem on how to control we are CommandSet are handled by listeners are
		/// visible.
		/// </remarks>
		private bool InFriendlyArea
		{
			get
			{
				return (m_propertyTable.GetValue<string>("areaChoice") == "textsWords");
			}
		}

		#endregion Properties

		#region Construction

		//need a default constructor for dynamic loading
		public WordsEditToolMenuHandler()
		{
		}

		#endregion Construction

		#region Other methods

		private void SetNewStatus(IWfiAnalysis anal, int newStatus)
		{
			int currentStatus = anal.ApprovalStatusIcon;
			if (currentStatus == newStatus)
				return;

			UndoableUnitOfWorkHelper.Do(MEStrings.ksUndoChangingApprovalStatus,
				MEStrings.ksRedoChangingApprovalStatus, Cache.ActionHandlerAccessor,
				() =>
					{
						if (currentStatus == 1)
							anal.MoveConcAnnotationsToWordform();
						anal.ApprovalStatusIcon = newStatus;
						if (newStatus == 1)
						{
							// make sure default senses are set to be real values,
							// since the user has seen the defaults, and approved the analysis based on them.
							foreach (var mb in anal.MorphBundlesOS)
							{
								var currentSense = mb.SenseRA;
								if (currentSense == null)
									mb.SenseRA = mb.DefaultSense;
							}
						}
					});

			// Wipe all of the old slices out, so we get new numbers and newly placed objects.
			// This fixes LT-5935. Also removes the need to somehow make the virtual properties like HumanApprovedAnalyses update.
			m_dataEntryForm.RefreshList(true);
		}

		private void ShowConcDlg(ICmObject concordOnObject)
		{
			using (IFwGuiControl ctrl = new ConcordanceDlg())
			{
				ctrl.Init(m_mediator, m_propertyTable, MainWindowNode, concordOnObject);
				ctrl.Launch();
			}
		}

		#endregion Other methods

		#region XCore Message handlers

		#region Concordance Message handlers

		//
		public virtual bool OnDisplayShowWordformConc(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnShowWordformConc(object argument)
		{
			var wf = Wordform;
			if (wf == null)
				throw new InvalidOperationException("Could not find wordform object.");

			ShowConcDlg(wf);

			return true;
		}

		public virtual bool OnDisplayShowWordGlossConc(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnShowWordGlossConc(object argument)
		{
			var gloss = Gloss;
			if (gloss == null)
				throw new InvalidOperationException("Could not find gloss object.");

			ShowConcDlg(gloss);

			return true;
		}

		public virtual bool OnDisplayShowHumanApprovedAnalysisConc(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnShowHumanApprovedAnalysisConc(object argument)
		{
			var anal = Analysis;
			if (anal == null)
				throw new InvalidOperationException("Could not find analysis object.");

			ShowConcDlg(anal);

			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			var cmd = (Command)commandObject;
			var className = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			var specifiedClsid = 0;
			if ((Cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged).ClassExists(className))
				specifiedClsid = Cache.MetaDataCacheAccessor.GetClassId(className);
			var anal = Analysis;
			if (anal != null)
			{
				if (anal.ClassID == specifiedClsid)
				{
					display.Enabled = display.Visible = true;
					return true;
				}

				if (specifiedClsid == WfiGlossTags.kClassId)
				{
					if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null &&
						CurrentSliceObject != null && CurrentSliceObject.ClassID == specifiedClsid)
					{
						display.Enabled = display.Visible = true;
						return true;
					}
				}
			}
			return false;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
			var cmd = (Command)commandObject;
			var className = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			var guid = Guid.Empty;
			switch (className)
			{
				case "WfiAnalysis":
					var anal = Analysis;
					if (anal != null)
						guid = anal.Guid;
					break;
				case "WfiGloss":
					if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null &&
						CurrentSliceObject != null && CurrentSliceObject.ClassID == WfiGlossTags.kClassId)
					{
						guid = CurrentSliceObject.Guid;
					}
					break;
			}
			if (guid != Guid.Empty)
			{
				var tool = XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
				m_mediator.PostMessage("FollowLink", new FwLinkArgs(tool, guid));
				return true;
			}
			return false;
		}
		#endregion Concordance Message handlers

		#region Approval Status Message handlers

		public virtual bool OnDisplayAnalysisApprove(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			display.Checked = Analysis != null && Analysis.ApprovalStatusIcon == 1;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAnalysisApprove(object argument)
		{
			IWfiAnalysis anal = Analysis;
			Debug.Assert(anal != null, "Could not find analysis object.");
			SetNewStatus(anal, 1);

			return true;
		}

		public virtual bool OnDisplayAnalysisUnknown(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			display.Checked = Analysis.ApprovalStatusIcon == 0;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAnalysisUnknown(object argument)
		{
			IWfiAnalysis anal = Analysis;
			Debug.Assert(anal != null, "Could not find analysis object.");
			SetNewStatus(anal, 0);

			return true;
		}

		public virtual bool OnDisplayAnalysisDisapprove(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			display.Checked = Analysis.ApprovalStatusIcon == 2;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAnalysisDisapprove(object argument)
		{
			IWfiAnalysis anal = Analysis;
			Debug.Assert(anal != null, "Could not find analysis object.");
			SetNewStatus(anal, 2);

			return true;
		}

		#endregion Approval Status Message handlers

#if NOTYET
		#region SpellingStatus Message handlers

		public virtual bool OnDisplaySpellingStatusUnknown(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnSpellingStatusUnknown(object argument)
		{
			MessageBox.Show("TODO: Set spelling status to 'Unknown' goes here, when the integer values that get stored in the database get defined.");

			return true;
		}

		public virtual bool OnDisplaySpellingStatusGood(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnSpellingStatusGood(object argument)
		{
			MessageBox.Show("TODO: Set spelling status to 'Good' goes here, when the integer values that get stored in the database get defined.");

			return true;
		}

		public virtual bool OnDisplaySpellingStatusDisapprove(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnSpellingStatusDisapprove(object argument)
		{
			MessageBox.Show("TODO: Set spelling status to 'Disapproved' goes here, when the integer values that get stored in the database get defined.");

			return true;
		}

		#endregion SpellingStatus Message handlers
#endif

		#region Wordform edit Message handlers

		protected override bool DeleteObject(Command command)
		{
			if (base.DeleteObject(command))
			{
				// Wipe all of the old slices out,
				// so we get new numbers, where needed.
				// This fixes LT-5974.
				m_dataEntryForm.RefreshList(true);
				return true;
			}
			return false;
		}

		public virtual bool OnDisplayWordformEditForm(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnWordformEditForm(object argument)
		{
			MessageBox.Show(MEStrings.ksTodo_WordformEditing);

			return true;
		}

		public virtual bool OnDisplayWordformChangeCase(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnWordformChangeCase(object argument)
		{
			MessageBox.Show("TODO: Case changing editing happens here.");

			return true;
		}

		#endregion Wordform edit Message handlers

		#region New analysis message handler

		//

		public virtual bool OnDisplayAddApprovedAnalysis(object commandObject,
			ref UIItemDisplayProperties display)
		{
			// The null test covers cases where there is no current object because the list (as filtered) is empty.
			if (InFriendlyArea && m_mediator != null && m_dataEntryForm.Root != null)
			{
#pragma warning disable 0219
				display.Visible = true;
				display.Enabled = Wordform != null;
#pragma warning restore 0219
			}
			else
			{
				display.Enabled = display.Visible = false;
			}
			return true; //we've handled this
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnAddApprovedAnalysis(object argument)
		{
			var mainWnd = (FwXWindow)m_dataEntryForm.FindForm();
			using (EditMorphBreaksDlg dlg = new EditMorphBreaksDlg(m_propertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				IWfiWordform wf = Wordform;
				if (wf == null)
					return true;
				ITsString tssWord = Wordform.Form.BestVernacularAlternative;
				string morphs = tssWord.Text;
				var cache = Cache;
				dlg.Initialize(tssWord, morphs, cache.MainCacheAccessor.WritingSystemFactory,
					cache, m_dataEntryForm.StyleSheet);
				// Making the form active fixes problems like LT-2619.
				// I'm (RandyR) not sure what adverse impact might show up by doing this.
				mainWnd.Activate();
				if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
				{
					morphs = dlg.GetMorphs().Trim();
					if (morphs.Length == 0)
						return true;

					string[] prefixMarkers = MorphServices.PrefixMarkers(cache);
					string[] postfixMarkers = MorphServices.PostfixMarkers(cache);

					List<string> allMarkers = new List<string>();
					foreach (string s in prefixMarkers)
					{
						allMarkers.Add(s);
					}

					foreach (string s in postfixMarkers)
					{
						if (!allMarkers.Contains(s))
							allMarkers.Add(s);
					}
					allMarkers.Add(" ");

					string[] breakMarkers = new string[allMarkers.Count];
					for (int i = 0; i < allMarkers.Count; ++i)
						breakMarkers[i] = allMarkers[i];

					string fullForm = SandboxBase.MorphemeBreaker.DoBasicFinding(morphs, breakMarkers, prefixMarkers, postfixMarkers);

					var command = (Command) argument;
					UndoableUnitOfWorkHelper.Do(command.UndoText, command.RedoText, cache.ActionHandlerAccessor,
						() =>
							{
								IWfiAnalysis newAnalysis = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
								Wordform.AnalysesOC.Add(newAnalysis);
								newAnalysis.ApprovalStatusIcon = 1; // Make it human approved.
								int vernWS = TsStringUtils.GetWsAtOffset(tssWord, 0);
								foreach (string morph in fullForm.Split(Unicode.SpaceChars))
								{
									if (morph != null && morph.Length != 0)
									{
										IWfiMorphBundle mb = cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
										newAnalysis.MorphBundlesOS.Add(mb);
										mb.Form.set_String(vernWS, Cache.TsStrFactory.MakeString(morph, vernWS));
									}
								}
							});
				}
			}
			return true;
		}

		#endregion New analysis message handler

		#endregion XCore Message handlers
	}
}
