// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// PartOfSpeechUi provides UI-specific methods for the PartOfSpeech class.
	/// </summary>
	public class PartOfSpeechUi : CmPossibilityUi
	{
		/// <summary>
		/// Create one. Argument must be a PartOfSpeech.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
		/// passed an obj anyway.
		/// </summary>
		private PartOfSpeechUi(IPartOfSpeech obj) : base(obj)
		{
		}

		internal PartOfSpeechUi() { }

		/// <summary>
		/// Handle the context menu for inserting a POS.
		/// </summary>
		public static PartOfSpeechUi MakeLcmModelUiObject(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			Guard.AgainstNull(cache, nameof(cache));

			PartOfSpeechUi posUi = null;
			using (var dlg = new MasterCategoryListDlg())
			{
				var newOwner = cache.ServiceLocator.GetInstance<IPartOfSpeechRepository>().GetObject(hvoOwner);
				dlg.SetDlginfo(newOwner.OwningList, propertyTable, true, newOwner);
				switch (dlg.ShowDialog(propertyTable.GetValue<Form>(FwUtils.window)))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						posUi = new PartOfSpeechUi(dlg.SelectedPOS);
						publisher.Publish(LanguageExplorerConstants.JumpToRecord, dlg.SelectedPOS.Hvo);
						break;
				}
			}
			return posUi;
		}
	}
}