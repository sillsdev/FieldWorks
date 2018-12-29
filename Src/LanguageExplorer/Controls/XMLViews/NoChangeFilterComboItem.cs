// Copyright (c) 2011-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This is a base class for filter combo items that don't actually involve a mather. Typically
	/// (e.g., TextComboItem) the only purpose is for Invoke to launch a dialog.
	/// </summary>
	public class NoChangeFilterComboItem : FilterComboItem
	{
		/// <summary />
		public NoChangeFilterComboItem(ITsString tssName) : base(tssName, null, null)
		{
		}

		/// <summary>
		/// Default for this class is to do nothing.
		/// </summary>
		public override bool Invoke()
		{
			return false; // no filter was applied.
		}
	}
}