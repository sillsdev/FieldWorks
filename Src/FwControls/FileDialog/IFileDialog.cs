// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Interface to the FileDialog
	/// </summary>
	public interface IFileDialog
	{
		/// <summary>
		/// Gets or sets a value indicating whether the dialog box automatically adds an extension
		/// to a file name if the user omits the extension.
		/// </summary>
		bool AddExtension { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box displays a warning if the user
		/// specifies a file name that does not exist.
		/// </summary>
		bool CheckFileExists { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box displays a warning if the user
		/// specifies a path that does not exist.
		/// </summary>
		bool CheckPathExists { get; set; }

		/// <summary>
		/// Gets or sets the default file name extension.
		/// </summary>
		string DefaultExt { get; set; }

		/// <summary>
		/// Gets or sets a string containing the file name selected in the file dialog box.
		/// </summary>
		string FileName { get; set; }

		/// <summary>
		/// Gets the file names of all selected files in the dialog box.
		/// </summary>
		string[] FileNames { get; }

		/// <summary>
		/// Gets or sets the current file name filter string, which determines the choices that
		/// appear in the "Save as file type" or "Files of type" box in the dialog box.
		/// </summary>
		string Filter { get; set; }

		/// <summary>
		/// Gets or sets the index of the filter currently selected in the file dialog box.
		/// </summary>
		int FilterIndex { get; set; }

		/// <summary>
		/// Gets or sets the initial directory displayed by the file dialog box.
		/// </summary>
		string InitialDirectory { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box restores the directory to the
		/// previously selected directory before closing.
		/// </summary>
		bool RestoreDirectory { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the Help button is displayed in the file
		/// dialog box.
		/// </summary>
		bool ShowHelp { get; set; }

		/// <summary>
		/// Gets or sets whether the dialog box supports displaying and saving files that have
		/// multiple file name extensions.
		/// </summary>
		bool SupportMultiDottedExtensions { get; set; }

		/// <summary>
		/// Gets or sets the file dialog box title.
		/// </summary>
		string Title { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box accepts only valid Win32 file names.
		/// </summary>
		bool ValidateNames { get; set; }

		/// <summary>
		/// Resets all properties to their default values.
		/// </summary>
		void Reset();

		/// <summary>
		/// Runs the dialog box with a default owner.
		/// </summary>
		DialogResult ShowDialog();

		/// <summary>
		/// Runs the dialog box with the specified owner.
		/// </summary>
		DialogResult ShowDialog(IWin32Window owner);

		/// <summary>
		/// Occurs when the component is disposed by a call to the Dispose method.
		/// </summary>
		event EventHandler Disposed;

		/// <summary>
		/// Occurs when the user clicks on the Open or Save button on a file dialog box.
		/// </summary>
		event CancelEventHandler FileOk;

		/// <summary>
		/// Occurs when the user clicks the Help button on a common dialog box.
		/// </summary>
		event EventHandler HelpRequest;
	}
}
