// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer.LcmUi
{
	/// <summary>
	/// FsFeatDefnUi provides UI-specific methods for the PartOfSpeech class.
	/// </summary>
	public class FsFeatDefnUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a FsFeatDefn.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeLcmModelUiObject, which is
		/// passed an obj anyway.
		/// </summary>
		protected FsFeatDefnUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IFsFeatDefn);
		}

		internal FsFeatDefnUi() {}

		/// <summary>
		/// Handle the context menu for inserting an FsFeatDefn.
		/// </summary>
		public static FsFeatDefnUi MakeLcmModelUiObject(LcmCache cache, IPropertyTable propertyTable, IPublisher publisher, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			Guard.AgainstNull(cache, nameof(cache));

			FsFeatDefnUi ffdUi = null;
			var className = "FsClosedFeature";
			if (classId == FsComplexFeatureTags.kClassId)
			{
				className = "FsComplexFeature";
			}
			using (var dlg = new MasterInflectionFeatureListDlg(className))
			{
				dlg.SetDlginfo(cache.LanguageProject.MsFeatureSystemOA, propertyTable, true);
				switch (dlg.ShowDialog(propertyTable.GetValue<Form>(FwUtils.window)))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						ffdUi = new FsFeatDefnUi(dlg.SelectedFeatDefn);
						publisher.Publish("JumpToRecord", dlg.SelectedFeatDefn.Hvo);
						break;
				}
			}
			return ffdUi;
		}
	}
}
