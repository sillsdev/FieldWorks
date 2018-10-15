// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Interface to the FolderBrowserDialog
	/// </summary>
	public interface IFolderBrowserDialog
	{
		/// <summary>
		/// Gets or sets the descriptive text displayed above the tree view control in the dialog box.
		/// </summary>
		/// <value>The description.</value>
		string Description { get; set; }

		/// <summary>
		/// Gets or sets the root folder where the browsing starts from.
		/// </summary>
		/// <value>The root folder.</value>
		Environment.SpecialFolder RootFolder { get; set; }

		/// <summary>
		/// Gets or sets the path selected by the user.
		/// </summary>
		/// <value>The selected path.</value>
		string SelectedPath { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the New Folder button appears in the folder
		/// browser dialog box.
		/// </summary>
		/// <value><c>true</c> if show new folder button; otherwise, <c>false</c>.</value>
		bool ShowNewFolderButton { get; set; }

		/// <summary>
		/// Gets or sets an object that contains data about the control.
		/// </summary>
		/// <value>The tag.</value>
		object Tag { get; set; }

		/// <summary>
		/// Resets properties to their default values.
		/// </summary>
		void Reset();

		/// <summary>
		/// Runs the dialog box with a default owner.
		/// </summary>
		/// <returns>The dialog result.</returns>
		DialogResult ShowDialog();

		/// <summary>
		/// Runs the dialog box with the specified owner.
		/// </summary>
		/// <returns>The dialog result.</returns>
		/// <param name="owner">Owner.</param>
		DialogResult ShowDialog(IWin32Window owner);

		/// <summary>
		/// Occurs when the component is disposed by a call to the Dispose method.
		/// </summary>
		event EventHandler Disposed;
	}
}
