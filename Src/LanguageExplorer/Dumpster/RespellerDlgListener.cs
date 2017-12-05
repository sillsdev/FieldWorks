// Copyright (c) 2005-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using LanguageExplorer.Works;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: Likely disposition: Dump RespellerDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the respelling.
	// TODO: Other classes in this file may need to live on.
	//
	// TODO: There is no DlgListenerBase base class now (not moved), since there will be no listeners.
	// TODO: Spin off: RespellerTemporaryRecordClerk, OccurrenceComparer, OccurrenceSorter, RespellerTemporaryRecordList, etc
	//
	/// <summary>
	/// XCore listener for the Respeller dlg.
	/// </summary>
	public class RespellerDlgListener : DlgListenerBase
	{
	#region Data members
		/// <summary>
		/// used to store the size and location of dialogs
		/// </summary>
		protected IPersistenceProvider m_persistProvider; // Was on DlgListenerBase base class

	#endregion Data members

	#region Properties

		/// <summary>
		/// Override to get a suitable label.
		/// </summary>
		protected string PersistentLabel
		{
			get { return "RespellerDlg"; } // Was on DlgListenerBase base class
		}

	#endregion Properties

	#region XCORE Message Handlers

		/// <summary>
		/// Try to find a WfiWordform object corresponding the the focus selection.
		/// If successful return its guid, otherwise, return Guid.Empty.
		/// </summary>
		/// <returns></returns>
		private ITsString ActiveWord()
		{
			if (InFriendliestTool)
			{
				// we should be able to get our info from the current record clerk.
				// but return null if we can't get the info, otherwise we allow the user to
				// bring up the change spelling dialog and crash because no wordform can be found (LT-8766).
				var clerk = RecordClerk.RecordClerkRepository.ActiveRecordClerk;
				if (clerk == null || clerk.CurrentObject == null)
					return null;
				var wfiWordform = clerk.CurrentObject as IWfiWordform;
				if (wfiWordform == null)
					return null;
				var tssVern = wfiWordform.Form.BestVernacularAlternative;
				return tssVern;
			}
			var app = PropertyTable.GetValue<IApp>("App");
			if (app == null)
				return null;
			var window = app.ActiveMainWindow as IFwMainWnd;
			if (window == null)
				return null;
			var activeView = window.ActiveView;
			if (activeView == null)
				return null;
			var roots = activeView.AllRootBoxes();
			if (roots.Count < 1)
				return null;
			var helper = SelectionHelper.Create(roots[0].Site);
			if (helper == null)
				return null;
			var tssWord = helper.SelectedWord;
			if (tssWord != null)
			{
				// Check for a valid vernacular writing system.  (See LT-8892.)
				var ws = TsStringUtils.GetWsAtOffset(tssWord, 0);
				var cache = PropertyTable.GetValue<FdoCache>("cache");
				CoreWritingSystemDefinition wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
				if (cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj))
					return tssWord;
			}
			return null;
		}

#if RANDYTODO
		/// <summary>
		/// Determine whether to show (and enable) the command to launch the respeller dialog.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayLaunchRespellerDlg(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = InFriendlyArea;		// See LT-8641.
			display.Enabled = display.Visible && ActiveWord() != null;
			return true; //we've handled this
		}
#endif

		/// <summary>
		/// Launch the Respeller dlg.
		/// </summary>
		/// <param name="argument">The xCore Command object.</param>
		/// <returns>true</returns>
		public bool OnLaunchRespellerDlg(object argument)
		{
			CheckDisposed();
			if (!InFriendliestTool)		// See LT-8641.
			{
				LaunchRespellerDlgOnWord(ActiveWord());
				return true;
			}
			var clerk = RecordClerk.RecordClerkRepository.ActiveRecordClerk;
			using (var luh = new ListUpdateHelper(clerk))
			{
				var changesWereMade = false;
				// Launch the Respeller Dlg.
				using (var dlg = new RespellerDlg())
				{
					dlg.InitializeFlexComponent(PropertyTable, Publisher, Subscriber);
#if RANDYTODO
					// TODO: That null in the next call used to be the xml config node.
#endif
					if (dlg.SetDlgInfo(null))
					{
						dlg.ShowDialog((Form)PropertyTable.GetValue<IFwMainWnd>("window"));
						changesWereMade = dlg.ChangesWereMade;
					}
					else
					{
						MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
					}
				}
				// The Respeller dialog can't make all necessary updates, since things like occurrence
				// counts depend on which texts are included, not just the data. So make sure we reload.
				luh.TriggerPendingReloadOnDispose = changesWereMade;
				if (changesWereMade)
				{
					// further try to refresh occurrence counts.
					var sda = clerk.VirtualListPublisher;
					while (sda != null)
					{
						if (sda is ConcDecorator)
						{
							((ConcDecorator)sda).Refresh();
							break;
						}
						if (!(sda is DomainDataByFlidDecoratorBase))
							break;
						sda = ((DomainDataByFlidDecoratorBase) sda).BaseSda;
					}
				}
			}
			return true;
		}

		private void LaunchRespellerDlgOnWord(ITsString tss)
		{
			if (tss == null || string.IsNullOrEmpty(tss.Text))
				return;

			var cache = PropertyTable.GetValue<FdoCache>("cache");
			var wordform = WordformApplicationServices.GetWordformForForm(cache, tss);
			using (var luh = new ListUpdateHelper(RecordClerk.RecordClerkRepository.ActiveRecordClerk))
			{
				// Launch the Respeller Dlg.
				using (var dlg = new RespellerDlg())
				{
#if RANDYTODO
					// TODO: Deal with that null (was xml config node).
#endif
					if (dlg.SetDlgInfo(wordform, null))
					{
						dlg.ShowDialog((Form)PropertyTable.GetValue<IFwMainWnd>("window"));
					}
					else
					{
						MessageBox.Show(TextAndWordsResources.ksCannotRespellWordform);
					}
				}
				//// We assume that RespellerDlg made all the necessary changes to relevant lists.
				//// no need to reload due to a propchange (e.g. change in paragraph contents).
				//luh.TriggerPendingReloadOnDispose = false;
			}
		}

	#endregion XCORE Message Handlers

		/// <summary>
		/// The entire "Texts & Words" area is our friend.  See LT-8641.
		/// </summary>
		private bool InFriendlyArea
		{
			get
			{
				return (PropertyTable.GetValue<string>("areaChoice") == AreaServices.TextAndWordsAreaMachineName);
			}
		}

		/// <summary>
		/// Inside "Analyses" tool is handled differently than elsewhere in the "Texts & Words" area.
		/// (But of course we must be in that area to be in the friendliest tool.)
		/// </summary>
		private bool InFriendliestTool
		{
			get
			{
				return InFriendlyArea && PropertyTable.GetValue<string>($"{AreaServices.ToolForAreaNamed_}{AreaServices.TextAndWordsAreaMachineName}") == AreaServices.AnalysesMachineName;
			}
		}
	}
#endif
}
