// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html).
// </copyright>
// --------------------------------------------------------------------------------------------
using System.IO;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Prompts the user to select a location for saving a file.
	/// </summary>
	public class SaveFileDialogAdapter: FileDialogAdapter, ISaveFileDialog
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SIL.FieldWorks.Common.Controls.FileDialog.SaveFileDialogAdapter"/> class.
		/// </summary>
		public SaveFileDialogAdapter()
		{
			m_dlg = Manager.CreateSaveFileDialog();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box prompts the user for permission
		/// to create a file if the user specifies a file that does not exist.
		/// </summary>
		public bool CreatePrompt
		{
			get { return ((ISaveFileDialog)m_dlg).CreatePrompt; }
			set { ((ISaveFileDialog)m_dlg).CreatePrompt = value; }
		}

		/// <summary>
		/// Gets or sets a value indicating whether the Save As dialog box displays a warning if
		/// the user specifies a file name that already exists.
		/// </summary>
		public bool OverwritePrompt
		{
			get { return ((ISaveFileDialog)m_dlg).OverwritePrompt; }
			set { ((ISaveFileDialog)m_dlg).OverwritePrompt = value; }
		}

		/// <summary>
		/// Opens the file with read/write permission selected by the user.
		/// </summary>
		public Stream OpenFile()
		{
			return ((ISaveFileDialog)m_dlg).OpenFile();
		}
	}
}
