// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// This class is the arguments for a CheckBoxChangedEventHandler.
	/// </summary>
	public class CheckBoxChangedEventArgs : EventArgs
	{
		/// <summary>
		/// Initializes a new instance of the <see><cref>T:CheckBoxChangedEventArgs</cref></see> class.
		/// </summary>
		/// <param name="hvosChanged">The hvos changed.</param>
		public CheckBoxChangedEventArgs(int[] hvosChanged)
		{
			HvosChanged = hvosChanged;
		}

		/// <summary>
		/// Gets the hvos changed.
		/// </summary>
		/// <value>The hvos changed.</value>
		public int[] HvosChanged { get; }
	}

	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	public delegate void CheckBoxChangedEventHandler(object sender, CheckBoxChangedEventArgs e);
}