using System;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;
using SIL.FieldWorks.LexText.Controls;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// FsFeatDefnUi provides UI-specific methods for the PartOfSpeech class.
	/// </summary>
	public class FsFeatDefnUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a FsFeatDefn.
		/// Note that declaring it to be forces us to just do a cast in every case of MakeUi, which is
		/// passed an obj anyway.
		/// </summary>
		/// <param name="obj"></param>
		public FsFeatDefnUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IFsFeatDefn);
		}

		internal FsFeatDefnUi() {}

		/// <summary>
		/// Handle the context menu for inserting an FsFeatDefn.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="classId"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="insertionPosition"></param>
		/// <returns></returns>
		public new static FsFeatDefnUi CreateNewUiObject(Mediator mediator, uint classId, int hvoOwner, int flid, int insertionPosition)
		{
			FsFeatDefnUi ffdUi = null;
			string className = "FsClosedFeature";
			if (classId == FDO.Cellar.FsComplexFeature.kClassId)
				className = "FsComplexFeature";
			using (MasterInflectionFeatureListDlg dlg = new MasterInflectionFeatureListDlg(className))
			{
				FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
				Debug.Assert(cache != null);
				dlg.SetDlginfo(cache.LangProject.MsFeatureSystemOA, mediator, true);
				switch (dlg.ShowDialog((Form)mediator.PropertyTable.GetValue("window")))
				{
					case DialogResult.OK: // Fall through.
					case DialogResult.Yes:
						ffdUi = new FsFeatDefnUi(dlg.SelectedFeatDefn);
						mediator.SendMessage("JumpToRecord", dlg.SelectedFeatDefn.Hvo);
						break;
				}
			}
			return ffdUi;
		}
	}
}
