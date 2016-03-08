// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;
using SIL.CoreImpl;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Interface for a Pane bar.
	/// </summary>
	public interface IPaneBar : IFlexComponent
	{
		/// <summary>
		/// Set the text of the pane bar.
		/// </summary>
		string Text { set; }

		/// <summary>
		/// Refresh the pane bar display.
		/// </summary>
		void RefreshPane();

		/// <summary>
		/// Add contorls to the IPaneBar.
		/// </summary>
		/// <param name="paneBarControls">Controls to be added to IPaneBar.</param>
		void AddControls(IList<Control> paneBarControls);
	}
}