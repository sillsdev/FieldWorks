// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Discourse;
using LanguageExplorer.DictionaryConfiguration;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace LanguageExplorer.Dumpster
{
#if RANDYTODO
	// TODO: Likely disposition: Dump RespellerDlgListener and just have relevant tool(s) add normal menu/toolbar event handlers for the respelling.
	// TODO: Other classes in this file may need to live on.
	//
	// TODO: There is no DlgListenerBase base class now (not moved), since there will be no listeners.
	// TODO: Spin off: OccurrenceComparer, OccurrenceSorter, RespellerTemporaryRecordList, etc
	//
	/// <summary>
	/// XCore listener for the Respeller dlg.
	/// </summary>
	public class RespellerDlgListener : DlgListenerBase
	{
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
				// we should be able to get our info from the current record list.
				// but return null if we can't get the info, otherwise we allow the user to
				// bring up the change spelling dialog and crash because no wordform can be found (LT-8766).
				var recordList = RecordList.RecordListRepository.ActiveRecordList;
				var tssVern = (recordList?.CurrentObject as IWfiWordform)?.Form?.BestVernacularAlternative;
				return tssVern;
			}
			var app = PropertyTable.GetValue<IApp>(LanguageExplorerConstants.App);
			var roots = (app?.ActiveMainWindow as FwXWindow)?.ActiveView?.AllRootBoxes();
			if (roots == null || roots.Count < 1 || roots[0] == null)
				return null;
			var tssWord = SelectionHelper.Create(roots[0].Site)?.SelectedWord;
			if (tssWord != null)
			{
				// Check for a valid vernacular writing system.  (See LT-8892.)
				var ws = TsStringUtils.GetWsAtOffset(tssWord, 0);
				var cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
				CoreWritingSystemDefinition wsObj = cache.ServiceLocator.WritingSystemManager.Get(ws);
				if (cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(wsObj))
					return tssWord;
			}
			return null;
		}

#if RANDYTODO
			// TODO: Used for respelling in:
			if (!InFriendliestTool)		// See LT-8641.
			{
				LaunchRespellerDlgOnWord(ActiveWord());
				return true;
			}
#endif
		private void LaunchRespellerDlgOnWord(ITsString tss)
		{
			if (tss == null || string.IsNullOrEmpty(tss.Text))
				return;

			var cache = PropertyTable.GetValue<LcmCache>(FwUtils.cache);
			var wordform = WordformApplicationServices.GetWordformForForm(cache, tss);
			using (var luh = new ListUpdateHelper(RecordList.RecordListRepository.ActiveRecordList))
			{
				// Launch the Respeller Dlg.
				using (var dlg = new RespellerDlg())
				{
#if RANDYTODO
					// TODO: Deal with that null (was xml config node).
#endif
					if (dlg.SetDlgInfo(wordform, null))
					{
						dlg.ShowDialog((Form)PropertyTable.GetValue<IFwMainWnd>(LanguageExplorerConstants.window));
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
#endif
}
