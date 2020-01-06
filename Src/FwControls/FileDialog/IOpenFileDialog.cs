// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Interface to the OpenFileDialog
	/// </summary>
	public interface IOpenFileDialog : IFileDialog
	{
		/// <summary>
		/// Gets or sets a value indicating whether the dialog box allows multiple files to be
		/// selected.
		/// </summary>
		bool Multiselect { get; set; }

		/// <summary>
		/// Opens the file selected by the user, with read-only permission. The file is specified
		/// by the FileName property.
		/// </summary>
		Stream OpenFile();
	}
}