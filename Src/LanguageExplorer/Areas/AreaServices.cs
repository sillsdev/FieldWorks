// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.LexText.DataNotebook;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Area level services
	/// </summary>
	internal static class AreaServices
	{
		/// <summary>
		/// Handle the provided import dialog.
		/// </summary>
		internal static void HandleDlg(Form importDlg, LcmCache cache, IFlexApp flexApp, IFwMainWnd mainWindow, IPropertyTable propertyTable, IPublisher publisher)
		{
			var oldWsUser = cache.WritingSystemFactory.UserWs;
			((IFwExtension)importDlg).Init(cache, propertyTable, publisher);
			if (importDlg.ShowDialog((Form)mainWindow) != DialogResult.OK)
			{
				return;
			}
			// NB: Some clients are not any of the types that are checked below, which is fine. That means nothing else is done here.
			if (importDlg is LexOptionsDlg)
			{
				if (oldWsUser != cache.WritingSystemFactory.UserWs || ((LexOptionsDlg)importDlg).PluginsUpdated)
				{
					flexApp.ReplaceMainWindow(mainWindow);
				}
			}
			else if (importDlg is LinguaLinksImportDlg || importDlg is InterlinearImportDlg || importDlg is LexImportWizard || importDlg is NotebookImportWiz || importDlg is LiftImportDlg)
			{
				// Make everything we've imported visible.
				mainWindow.RefreshAllViews();
			}
		}
	}
}
