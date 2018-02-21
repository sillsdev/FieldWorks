// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This interface is implemented by all drop down boxes.
	/// </summary>
	public interface IDropDownBox : IDisposable
	{
		/// <summary>
		/// Gets the drop down form.
		/// </summary>
		/// <value>The form.</value>
		Form Form { get; }
		/// <summary>
		/// Gets or sets the form that the dropdown box is launched from.
		/// </summary>
		/// <remarks>
		/// This is needed for PopupTree on Mono/Linux.
		/// </remarks>
		Form LaunchingForm { get; set; }
		/// <summary>
		/// Launches the drop down box.
		/// </summary>
		void Launch(Rectangle launcherBounds, Rectangle screenBounds);
		/// <summary>
		/// Hides the drop down box.
		/// </summary>
		void HideForm();
		/// <summary>
		/// Find the width that will display the full width of all items.
		/// Note that if the height is set to less than the natural height,
		/// some additional space may be wanted for a scroll bar.
		/// </summary>
		int NaturalWidth { get; }
		/// <summary>
		/// Find the height that will display the full height of all items.
		/// </summary>
		int NaturalHeight { get; }
		/// <summary>
		/// Returns Control's IsDisposed bool.
		/// </summary>
		bool IsDisposed { get; }
	}
}