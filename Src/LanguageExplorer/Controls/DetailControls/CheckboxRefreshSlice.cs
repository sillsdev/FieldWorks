// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal class CheckboxRefreshSlice : CheckboxSlice
	{
		public CheckboxRefreshSlice(LcmCache cache, ICmObject obj, int flid, XElement node)
			: base(cache, obj, flid, node)
		{
		}

		private void RefreshDisplay()
		{
			var dt = ContainingDataTree;
			var result = dt.RefreshDisplay();
			if (result)
			{
				dt.RefreshList(true);
			}
		}

		#region IVwNotifyChange methods
		// We use PropChanged instead of OnChanged so that the BrowseActiveViewer can tell us when
		// a select column item has been changed.
		public override void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag != PhSegmentRuleTags.kflidDisabled && tag != MoCompoundRuleTags.kflidDisabled && tag != MoAdhocProhibTags.kflidDisabled)
			{
				return;
			}
			UpdateDisplayFromDatabase();
			RefreshDisplay();
		}
		#endregion
	}
}