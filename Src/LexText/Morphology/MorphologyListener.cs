// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MorphologyListener.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Xml;
using System.Windows.Forms;
using System.Collections;
using System.Collections.Generic;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using XCore;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.LexText.Controls;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.IText;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for MorphologyListener.
	/// JohnT: rather contrary to its name, appears to be a place to put handlers for commands common
	/// to tools in the Words area.
	/// </summary>
	[XCore.MediatorDispose]
	public class MorphologyListener : IxCoreColleague, IFWDisposable
	{
		#region Data members

		/// <summary>
		/// Mediator that passes off messages.
		/// </summary>
		private XCore.Mediator m_mediator;
		private XmlNode m_configurationParameters;

		#endregion Data members

		#region Properties

		private IWfiWordform Wordform
		{
			get
			{
				IWfiWordform wf = null;
				string clerkId = XmlUtils.GetManditoryAttributeValue(m_configurationParameters, "clerk");
				string propertyName = RecordClerk.GetCorrespondingPropertyName(clerkId);
				RecordClerk clerk = (RecordClerk)m_mediator.PropertyTable.GetValue(propertyName);
				if (clerk != null)
					wf = clerk.CurrentObject as IWfiWordform;
				return wf;
			}
		}

		#endregion Properties

		#region Construction

		public MorphologyListener()
		{
		}

		#endregion Construction

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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_configurationParameters = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation

		public virtual void Init(Mediator mediator, XmlNode configurationParameters)
		{
			CheckDisposed();

			m_mediator = mediator;
			m_configurationParameters = configurationParameters;
			m_mediator.AddColleague(this);
		}

		public IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			List<IxCoreColleague> targets = new List<IxCoreColleague>();
			targets.Add(this);
			return targets.ToArray();
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
			bool checking = EnchantHelper.DictionaryExists(Cache.DefaultVernWs, Cache.LanguageWritingSystemFactoryAccessor);
			// Make sure the property value is consistent. It can get off, e.g., if the value FLEx remembers from its
			// last run is not consistent with what TE stored in the database.
			m_mediator.PropertyTable.SetProperty("UseVernSpellingDictionary", checking);
			display.Checked = checking;
			return true; //we've handled this
		}

		public bool OnUseVernSpellingDictionary(object argument)
		{
			bool checking = !m_mediator.PropertyTable.GetBoolProperty("UseVernSpellingDictionary", true);
			if (checking)
				OnEnableVernacularSpelling();
			else
				Cache.LangProject.WordformInventoryOA.DisableVernacularSpellingDictionary();
			if (FwApp.App is FwXApp)
				(FwApp.App as FwXApp).RefreshDisplay(Cache);
			m_mediator.PropertyTable.SetProperty("UseVernSpellingDictionary", checking);
			return true;
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

			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			if (cache == null)
				return false; // impossible?
			cache.LangProject.WordformInventoryOA.ConformSpellingDictToWfi();
			return true; // handled
		}

		private FdoCache Cache
		{
			get { return (FdoCache)m_mediator.PropertyTable.GetValue("cache"); }
		}

		/// <summary>
		/// Enable vernacular spelling.
		/// </summary>
		void OnEnableVernacularSpelling()
		{
			// Enable all vernacular spelling dictionaries by changing those that are set to <None>
			// to point to the appropriate Locale ID. Do this BEFORE updating the spelling dictionaries,
			// otherwise, the update won't see that there is any dictionary set to update.
			FdoCache cache = Cache;
			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			foreach (LgWritingSystem wsObj in cache.LangProject.CurVernWssRS)
			{
				int ws = wsObj.Hvo;
				IWritingSystem engine = wsf.get_EngineOrNull(ws);
				if (engine == null)
					continue; // paranoia
				// This allows it to try to find a dictionary, but doesn't force one to exist.
				if (engine.SpellCheckDictionary == "<None>")
					engine.SpellCheckDictionary = engine.IcuLocale;
			}
			// This forces the default verancular WS spelling dictionary to exist, and updates
			// all existing ones.
			OnAddWordsToSpellDict(null);
		}
		public virtual bool OnDisplayGotoWfiWordform(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (InFriendlyArea && m_mediator != null)
			{
				RecordClerk clrk = m_mediator.PropertyTable.GetValue("ActiveClerk") as RecordClerk;
				if (clrk != null && clrk.Id == "concordanceWords")
				{
					FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
					display.Visible = true;

					// we only want to enable if we have more than one, because there's no point in finding
					// the one we've already selected.
					display.Enabled = cache.LangProject.WordformInventoryOA.WordformsOC.Count > 1;
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
		internal static Guid ActiveWordform(FdoCache cache)
		{
			if (!(FwApp.App is FwXApp))
				return Guid.Empty;
			FwXWindow window = (FwApp.App as FwXApp).ActiveMainWindow as FwXWindow;
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
			int hvoWordform = cache.LangProject.WordformInventoryOA.GetWordformId(word);
			if (hvoWordform == 0 || cache.IsDummyObject(hvoWordform))
				return Guid.Empty;
			return cache.GetGuidFromId(hvoWordform);
		}

		/// <summary>
		/// Called by reflection to implement the command.
		/// </summary>
		/// <param name="argument"></param>
		/// <returns></returns>
		public bool OnEditSpellingStatus(object argument)
		{
			// Without checking both the SpellingStatus and (virtual) FullConcordanceCount
			// fields for the ActiveWordform(Cache) result, it's too likely that the user
			// will get a puzzling "Target not found" message popping up.  See LT-8717.
			FwLink link = FwLink.Create("Language Explorer", "toolBulkEditWordforms", Guid.Empty,
				Cache.ServerName, Cache.DatabaseName);
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeReviewUndecidedSpelling"));
			m_mediator.PostMessage("FollowLink", link);
			return true;
		}

		public bool OnViewIncorrectWords(object argument)
		{
			FwLink link = FwLink.Create("Language Explorer", "Analyses", ActiveWordform(Cache),
										Cache.ServerName, Cache.DatabaseName);
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeCorrectSpelling"));
			m_mediator.PostMessage("FollowLink", link);
			return true;
		}

		/// <summary>
		/// Handles the xCore message to go to a reversal entry.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnGotoWfiWordform(object argument)
		{
			CheckDisposed();

			using (WordformGoDlg dlg = new WordformGoDlg())
			{
				List<IWfiWordform> filteredEntries = new List<IWfiWordform>();
				filteredEntries.Add(Wordform);
				WindowParams wp = new WindowParams();
				wp.m_btnText = MEStrings.ks_GoTo;
				wp.m_label = MEStrings.ks_Find_;
				wp.m_title = MEStrings.ksFindWordform;
				dlg.SetDlgInfo(m_mediator, wp, filteredEntries); // , false
				if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
				{
					// Can't Go to a subentry, so we have to go to its main entry.
					FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
					IWfiWordform selWordform = WfiWordform.CreateFromDBObject(cache, dlg.SelectedID);
					m_mediator.BroadcastMessageUntilHandled("JumpToRecord", selWordform.Hvo);
				}
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
				return (m_mediator.PropertyTable.GetStringProperty("areaChoice", null) == "textsWords");
				//	&& m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_lexicon", null) == "reversalEditComplete");
			}
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
					m_mainWindowNode = (XmlNode)m_mediator.PropertyTable.GetValue("WindowConfiguration");
				return m_mainWindowNode;
			}
		}

		/// <summary>
		/// Returns the object of the current slice, or (if no slice is marked current)
		/// the object of the first slice, or (if there are no slices, or no data entry form) null.
		/// </summary>
		private ICmObject CurrentSliceObject
		{
			get
			{
				if (m_dataEntryForm == null)
					return null;
				if (m_dataEntryForm.CurrentSlice != null)
					return m_dataEntryForm.CurrentSlice.Object;
				if (m_dataEntryForm.Controls.Count == 0)
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
				IWfiWordform wf = null;
				ICmObject curObject = CurrentSliceObject;
				if (curObject is IWfiWordform)
					wf = curObject as IWfiWordform;
				else if (curObject is IWfiAnalysis && curObject.OwnerHVO != 0)
					wf = WfiWordform.CreateFromDBObject(Cache, curObject.OwnerHVO);
				else if (curObject is IWfiGloss && curObject.OwnerHVO != 0)
				{
					IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(Cache, curObject.OwnerHVO);
					if (anal.OwnerHVO != 0)
						wf = WfiWordform.CreateFromDBObject(Cache, anal.OwnerHVO);
				}
				return wf;
			}
		}

		private IWfiAnalysis Analysis
		{
			get
			{
				IWfiAnalysis anal = null;
				ICmObject curObject = CurrentSliceObject;
				if (curObject is IWfiAnalysis)
					anal = curObject as IWfiAnalysis;
				else if (curObject is IWfiGloss)
					anal = WfiAnalysis.CreateFromDBObject(Cache, curObject.OwnerHVO);
				return anal;
			}
		}

		private IWfiGloss Gloss
		{
			get
			{
				IWfiGloss gloss = null;
				ICmObject curObject = CurrentSliceObject;
				if (curObject is IWfiGloss)
					gloss = curObject as IWfiGloss;
				return gloss;
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
				return (m_mediator.PropertyTable.GetStringProperty("areaChoice", null) == "textsWords");
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
			// Getting the wordform's hvo here fixes: LT-5990 and its twin LT-5988.
			int wordformHvo = anal.OwnerHVO;
			int currentStatus = anal.ApprovalStatusIcon;
			if (currentStatus == newStatus)
				return;

			Cache.BeginUndoTask(MEStrings.ksUndoChangingApprovalStatus,
				MEStrings.ksRedoChangingApprovalStatus);
			if (currentStatus == 1)
				anal.MoveConcAnnotationsToWordform();
			anal.ApprovalStatusIcon = newStatus;
			if (newStatus == 1)
			{
				// make sure default senses are set to be real values,
				// since the user has seen the defaults, and approved the analysis based on them.
				foreach (IWfiMorphBundle mb in anal.MorphBundlesOS)
				{
					int currentSense = mb.SenseRAHvo;
					int defaultSense = mb.DefaultSense;
					if (currentSense == 0 && defaultSense > 0)
						mb.SenseRAHvo = defaultSense;
				}
			}
			Cache.EndUndoTask();

			// Remove it from the old virtual property.
			int oldVFlid = 0;
			switch (currentStatus)
			{
				case 0: // Unknown.
					oldVFlid = BaseVirtualHandler.GetInstalledHandlerTag(Cache, "WfiWordform", "HumanNoOpinionParses");
					break;
				case 1: // Approve.
					oldVFlid = BaseVirtualHandler.GetInstalledHandlerTag(Cache, "WfiWordform", "HumanApprovedAnalyses");
					break;
				case 2: // Disapprove.
					oldVFlid = BaseVirtualHandler.GetInstalledHandlerTag(Cache, "WfiWordform", "HumanDisapprovedParses");
					break;
			}
			Debug.Assert(oldVFlid != 0);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, wordformHvo, oldVFlid, 0, 0, 1);

			// Add it to the new virtual property.
			int newVFlid = 0;
			switch (newStatus)
			{
				case 0: // Unknown.
					newVFlid = BaseVirtualHandler.GetInstalledHandlerTag(Cache, "WfiWordform", "HumanNoOpinionParses");
					break;
				case 1: // Approve.
					newVFlid = BaseVirtualHandler.GetInstalledHandlerTag(Cache, "WfiWordform", "HumanApprovedAnalyses");
					break;
				case 2: // Disapprove.
					newVFlid = BaseVirtualHandler.GetInstalledHandlerTag(Cache, "WfiWordform", "HumanDisapprovedParses");
					break;
			}
			Debug.Assert(newVFlid != 0);
			Cache.PropChanged(null, PropChangeType.kpctNotifyAll, wordformHvo, newVFlid, 0, 1, 0);

			// No need to do a PropChanged on "WfiWordform"-"UserCount" here,
			// since it gets done in ApprovalStatusIcon in FDO.

			// Wipe all of the old slices out,
			// so we get new numbers.
			// This fixes LT-5935.
			m_dataEntryForm.RefreshList(true);
		}

		private void ShowConcDlg(ICmObject concordOnObject)
		{
			using (IFwGuiControl ctrl = new ConcordanceDlg())
			{
				ctrl.Init(m_mediator, MainWindowNode, concordOnObject);
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
			IWfiWordform wf = Wordform;
			Debug.Assert(wf != null, "Could not find wordform object.");

			ShowConcDlg(Wordform);

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
			IWfiGloss gloss = Gloss;
			Debug.Assert(gloss != null, "Could not find gloss object.");

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
			IWfiAnalysis anal = Analysis;
			Debug.Assert(anal != null, "Could not find analysis object.");

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
			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			XCore.Command cmd = (XCore.Command)commandObject;
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			uint specifiedClsid = cache.MetaDataCacheAccessor.GetClassId(className);
			IWfiAnalysis anal = Analysis;
			if (anal != null)
			{
				if (anal.ClassID == (int)specifiedClsid)
				{
					display.Enabled = display.Visible = true;
					return true;
				}
				else if (specifiedClsid == (uint)WfiGloss.kclsidWfiGloss)
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
			FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
			XCore.Command cmd = (XCore.Command)commandObject;
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			int hvo = 0;
			if (className == "WfiAnalysis")
			{
				IWfiAnalysis anal = Analysis;
				if (anal != null)
					hvo = anal.Hvo;
			}
			else if (className == "WfiGloss")
			{
				if (m_dataEntryForm != null && m_dataEntryForm.CurrentSlice != null &&
					CurrentSliceObject != null && CurrentSliceObject.ClassID == WfiGloss.kclsidWfiGloss)
				{
					hvo = CurrentSliceObject.Hvo;
				}
			}
			if (hvo != 0)
			{
				string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "tool");
				m_mediator.PostMessage("FollowLink",
					SIL.FieldWorks.FdoUi.FwLink.Create(tool, cache.GetGuidFromId(hvo), cache.ServerName, cache.DatabaseName));
				return true;
			}
			else
			{
				return false;
			}
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

		public override bool OnDataTreeDelete(object cmd)
		{
			base.OnDataTreeDelete(cmd);

			// Wipe all of the old slices out,
			// so we get new numbers, where needed.
			// This fixes LT-5974.
			m_dataEntryForm.RefreshList(true);

			return true;	//we handled this.
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
				FdoCache cache = (FdoCache)m_mediator.PropertyTable.GetValue("cache");
				display.Visible = true;
				display.Enabled = Wordform != null;
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
			using (EditMorphBreaksDlg dlg = new EditMorphBreaksDlg())
			{
				IWfiWordform wf = Wordform;
				if (wf == null)
					return true;
				ITsString tssWord = Wordform.Form.BestVernacularAlternative;
				string morphs = tssWord.Text;
				FdoCache cache = Cache;
				dlg.Initialize(tssWord, morphs, cache.MainCacheAccessor.WritingSystemFactory,
					cache, m_dataEntryForm.Mediator.StringTbl, m_dataEntryForm.StyleSheet);
				Form mainWnd = m_dataEntryForm.FindForm();
				// Making the form active fixes problems like LT-2619.
				// I'm (RandyR) not sure what adverse impact might show up by doing this.
				mainWnd.Activate();
				if (dlg.ShowDialog(mainWnd) == DialogResult.OK)
				{
					morphs = dlg.GetMorphs().Trim();
					if (morphs.Length == 0)
						return true;

					string[] prefixMarkers = MoMorphType.PrefixMarkers(cache);
					string[] postfixMarkers = MoMorphType.PostfixMarkers(cache);

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

					using (UndoRedoCommandHelper undoRedoTask = new UndoRedoCommandHelper(Cache,
						argument as Command))
					{
						IWfiAnalysis newAnalysis = Wordform.AnalysesOC.Add(new WfiAnalysis());
						newAnalysis.ApprovalStatusIcon = 1; // Make it human approved.
						int vernWS = StringUtils.GetWsAtOffset(tssWord, 0);
						foreach (string morph in fullForm.Split(Unicode.SpaceChars))
						{
							if (morph != null && morph.Length != 0)
							{
								IWfiMorphBundle mb = newAnalysis.MorphBundlesOS.Append(new WfiMorphBundle());
								mb.Form.SetAlternative(morph, vernWS);
							}
						}
						int outlineFlid = BaseVirtualHandler.GetInstalledHandlerTag(cache, "WfiAnalysis", "HumanApprovedNumber");
						foreach (int haaHvo in Wordform.HumanApprovedAnalyses)
						{
							// Do PropChanged for the outline number for all of them.
							// This fixes LT-5007, as the older ones kept their old number,
							// which could have been a duplicate number.
							cache.PropChanged(
								null,
								PropChangeType.kpctNotifyAll,
								haaHvo,
								outlineFlid,
								0, 0, 0);
						}
						cache.PropChanged(
							null,
							PropChangeType.kpctNotifyAll,
							Wordform.Hvo,
							BaseVirtualHandler.GetInstalledHandlerTag(cache, "WfiWordform", "HumanApprovedAnalyses"),
							0, 1, 0);
					}
				}
			}

			return true;
		}

		#endregion New analysis message handler

		#endregion XCore Message handlers
	}
}
