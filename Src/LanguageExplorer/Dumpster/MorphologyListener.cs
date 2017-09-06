// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.SpellChecking;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: I don't expect this class to survive, but its useful code moved elsewhere, as ordinary event handlers.
#endif
	/// <summary>
	/// Summary description for MorphologyListener.
	/// JohnT: rather contrary to its name, appears to be a place to put handlers for commands common
	/// to tools in the Words area.
	/// </summary>
	internal sealed class MorphologyListener : IFlexComponent, IVwNotifyChange, IDisposable
	{
		#region Data members

		private IWfiWordformRepository m_wordformRepos;

		#endregion Data members

		#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; private set; }

		#endregion

		#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

		#endregion

		#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;

			Cache = PropertyTable.GetValue<LcmCache>("cache");
			m_wordformRepos = Cache.ServiceLocator.GetInstance<IWfiWordformRepository>();
			Cache.DomainDataByFlid.AddNotification(this);
			if (IsVernacularSpellingEnabled())
				OnEnableVernacularSpelling();
		}

		#endregion

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
		private void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (Cache != null && !Cache.IsDisposed && Cache.DomainDataByFlid != null)
					Cache.DomainDataByFlid.RemoveNotification(this);
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			m_isDisposed = true;
		}

		#endregion IDisposable & Co. implementation

		#region XCore Message handlers

#if RANDYTODO
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
#endif

		public bool OnMergeWordform(object argument)
		{
			CheckDisposed();

			// Do something meaningful,
			// whenever the definition of merging wordforms gets developed.
			MessageBox.Show(LanguageExplorerResources.ksCannotMergeWordformsYet);
			return true;
		}

#if RANDYTODO
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
#endif

		public bool OnUseVernSpellingDictionary(object argument)
		{
			bool checking = !IsVernacularSpellingEnabled();
			if (checking)
				OnEnableVernacularSpelling();
			else
				WfiWordformServices.DisableVernacularSpellingDictionary(Cache);
			PropertyTable.SetProperty("UseVernSpellingDictionary", checking, true, true);
			RestartSpellChecking();
			return true;
		}

		// currently duplicated in FLExBridgeListener, to avoid an assembly dependency.
		private bool IsVernacularSpellingEnabled()
		{
			return PropertyTable.GetValue("UseVernSpellingDictionary", true);
		}

		private void RestartSpellChecking()
		{
			IApp app = PropertyTable.GetValue<IApp>("App");
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

		private LcmCache Cache { get; set; }

		/// <summary>
		/// Enable vernacular spelling.
		/// </summary>
		void OnEnableVernacularSpelling()
		{
			// Enable all vernacular spelling dictionaries by changing those that are set to <None>
			// to point to the appropriate Locale ID. Do this BEFORE updating the spelling dictionaries,
			// otherwise, the update won't see that there is any dictionary set to update.
			var cache = Cache;
			foreach (CoreWritingSystemDefinition wsObj in cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems)
			{
				// This allows it to try to find a dictionary, but doesn't force one to exist.
				if (string.IsNullOrEmpty(wsObj.SpellCheckingId) || wsObj.SpellCheckingId == "<None>") // LT-13556 new langs were null here
					wsObj.SpellCheckingId = wsObj.Id.Replace('-', '_');
			}
			// This forces the default vernacular WS spelling dictionary to exist, and updates
			// all existing ones.
			OnAddWordsToSpellDict(null);
		}

		/// <summary>
		/// Try to find a WfiWordform object corresponding the the focus selection.
		/// If successful return its guid, otherwise, return Guid.Empty.
		/// </summary>
		/// <returns></returns>
		private static Guid ActiveWordform(IWfiWordformRepository wordformRepos, IPropertyTable propertyTable)
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
			IWfiWordform wordform;
			return wordformRepos.TryGetObject(word, out wordform) ? wordform.Guid : Guid.Empty;
		}

#if RANDYTODO
		public bool OnDisplayEditSpellingStatus(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

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
				"bulkEditWordforms", Guid.Empty);
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeReviewUndecidedSpelling"));
			var commands = new List<string>
										{
											"AboutToFollowLink",
											"FollowLink"
										};
			var parms = new List<object>
										{
											null,
											link
										};
			Publisher.Publish(commands, parms);
			return true;
		}

#if RANDYTODO
		public bool OnDisplayViewIncorrectWords(object commandObject,
			ref UIItemDisplayProperties display)
		{
			display.Enabled = display.Visible = InFriendlyArea;
			return true; //we've handled this
		}
#endif

		public bool OnViewIncorrectWords(object argument)
		{
			FwLinkArgs link = new FwAppArgs(Cache.ProjectId.Handle,
				"Analyses", ActiveWordform(m_wordformRepos, PropertyTable));
			List<Property> additionalProps = link.PropertyTableEntries;
			additionalProps.Add(new Property("SuspendLoadListUntilOnChangeFilter", link.ToolName));
			additionalProps.Add(new Property("LinkSetupInfo", "TeCorrectSpelling"));
			var commands = new List<string>
										{
											"AboutToFollowLink",
											"FollowLink"
										};
			var parms = new List<object>
										{
											null,
											link
										};
			Publisher.Publish(commands, parms);
			return true;
		}

#if RANDYTODO
		public virtual bool OnDisplayGotoWfiWordform(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (InFriendlyArea && m_mediator != null)
			{
				var clrk = RecordClerk.RecordClerkRepository.ActiveRecordClerk;
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
#endif

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
				dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
				dlg.SetDlgInfo(Cache, null);
				if (dlg.ShowDialog() == DialogResult.OK)
					Publisher.Publish("JumpToRecord", dlg.SelectedObject.Hvo);
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
		private bool InFriendlyArea
		{
			get
			{
				return (PropertyTable.GetValue<string>("areaChoice") == "textsWords");
			}
		}

		/// <summary>
		/// Handle enabled menu items for jumping to another tool, or another location in the
		/// current tool.
		/// </summary>
		public bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			if (!InFriendlyArea)
				return false;
#if RANDYTODO
			var command = (Command)commandObject;
			if (command.TargetId != Guid.Empty)
			{
				var tool = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "tool");
				var commands = new List<string>
											{
												"AboutToFollowLink",
												"FollowLink"
											};
				var parms = new List<object>
											{
												null,
												new FwLinkArgs(tool, command.TargetId)
											};
				Publisher.Publish(commands, parms);
				command.TargetId = Guid.Empty;	// clear the target for future use.
				return true;
			}
#endif
			return false;
		}
		#endregion XCore Message handlers
	}
}
