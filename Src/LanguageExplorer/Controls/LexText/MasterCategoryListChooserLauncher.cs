// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.LexText
{
	/// <summary>
	/// This is an attempt to avoid LT-11548 where the MSAPopupTreeManager was being disposed
	/// under certain circumstances while it was still processing AfterSelect messages.
	/// </summary>
	public class MasterCategoryListChooserLauncher
	{
		private readonly ILexSense m_sense;
		private readonly Form m_parentOfPopupMgr;
		private readonly IPropertyTable m_propertyTable;
		private readonly IPublisher m_publisher;
		private readonly string m_field;

		public MasterCategoryListChooserLauncher(Form popupMgrParent, IPropertyTable propertyTable, IPublisher publisher, ICmPossibilityList possibilityList, string fieldName, ILexSense sense)
		{
			m_parentOfPopupMgr = popupMgrParent;
			m_propertyTable = propertyTable;
			m_publisher = publisher;
			CategoryList = possibilityList;
			m_sense = sense;
			FieldName = fieldName;
			Cache = m_sense.Cache;

			Application.Idle += LaunchChooseFromMasterCategoryListOnIdle;
		}

		public ICmPossibilityList CategoryList { get; }
		public string FieldName { get; }
		public LcmCache Cache { get; }

		private void LaunchChooseFromMasterCategoryListOnIdle(object sender, EventArgs e)
		{
			Application.Idle -= LaunchChooseFromMasterCategoryListOnIdle; // now being handled

			// now launch the dialog
			using (var dlg = new MasterCategoryListDlg())
			{
				dlg.SetDlginfo(CategoryList, m_propertyTable, false, null);
				switch (dlg.ShowDialog(m_parentOfPopupMgr))
				{
					case DialogResult.OK:
						var sandboxMsa = new SandboxGenericMSA();
						sandboxMsa.MainPOS = dlg.SelectedPOS;
						sandboxMsa.MsaType = m_sense.GetDesiredMsaType();
						UndoableUnitOfWorkHelper.Do(string.Format(LexTextControls.ksUndoSetX, FieldName), string.Format(LexTextControls.ksRedoSetX, FieldName), m_sense, () =>
							{
								m_sense.SandboxMSA = sandboxMsa;
							});
						// everything should be setup with new node selected, so return.
						break;
					case DialogResult.Yes:
						// represents a click on the link to create a new Grammar Category.
						// Post a message so that we jump to Grammar(area)/Categories tool.
						// Do this before we close any parent dialog in case
						// the parent wants to check to see if such a Jump is pending.
						// NOTE: We use PostMessage here, rather than SendMessage which
						// disposes of the PopupTree before we and/or our parents might
						// be finished using it (cf. LT-2563).
						LinkHandler.JumpToTool(m_publisher, new FwLinkArgs(AreaServices.PosEditMachineName, dlg.SelectedPOS.Guid));
						if (m_parentOfPopupMgr != null && m_parentOfPopupMgr.Modal)
						{
							// Close the dlg that opened the master POS dlg,
							// since its hotlink was used to close it,
							// and a new POS has been created.
							m_parentOfPopupMgr.DialogResult = DialogResult.Cancel;
							m_parentOfPopupMgr.Close();
						}
						break;
					default:
						// NOTE: If the user has selected "Cancel", then don't change
						// our m_lastConfirmedNode to the "More..." node. Keep it
						// the value set by popupTree_PopupTreeClosed() when we
						// called pt.Hide() above. (cf. comments in LT-2522)
						break;
				}
			}
		}
	}
}