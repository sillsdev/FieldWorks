// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Interface to the SaveFileDialog
	/// </summary>
	public interface ISaveFileDialog : IFileDialog
	{
		/// <summary>
		/// Gets or sets a value indicating whether the dialog box prompts the user for permission
		/// to create a file if the user specifies a file that does not exist.
		/// </summary>
		bool CreatePrompt { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether the Save As dialog box displays a warning if
		/// the user specifies a file name that already exists.
		/// </summary>
		bool OverwritePrompt { get; set; }

		/// <summary>
		/// Opens the file with read/write permission selected by the user.
		/// </summary>
		Stream OpenFile();
	}
}
