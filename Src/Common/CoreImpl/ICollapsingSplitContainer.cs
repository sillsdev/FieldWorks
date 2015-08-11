// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Windows.Forms;

namespace SIL.CoreImpl
{
	/// <summary>
	/// Interface for the main control in FLEx.
	/// </summary>
	public interface ICollapsingSplitContainer
	{
		/// <summary>
		/// Label for expand/collapse area for first half of splitter.
		/// </summary>
		string FirstLabel { get; set; }

		/// <summary>
		/// The control that should not be resized in the shared dimension when resizing the window.
		/// It is always the left/top one at this point.
		/// </summary>
		/// <remarks>
		/// This will return null if the data member is null.
		/// </remarks>
		Control FirstControl { get; set; }

		/// <summary>
		/// The first (visible) child control.
		/// </summary>
		Control FirstVisibleControl { get; }

		/// <summary>
		/// Label for expand/collapse area for second half of splitter.
		/// </summary>
		string SecondLabel { get; set; }

		/// <summary>
		/// The control that should be resized in the shared dimension when resizing the window.
		/// It is always the right/bottom one at this point.
		/// </summary>
		/// <remarks>
		/// This will return null if the data member is null.
		/// </remarks>
		Control SecondControl { get; set; }
	}
}
