// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Prompts the user to select a location for saving a file.
	/// </summary>
	public class SaveFileDialogAdapter : FileDialogAdapter, ISaveFileDialog
	{
		/// <summary />
		public SaveFileDialogAdapter()
		{
			m_dlg = Manager.CreateSaveFileDialog();
		}

		/// <inheritdoc />
		public bool CreatePrompt
		{
			get { return ((ISaveFileDialog)m_dlg).CreatePrompt; }
			set { ((ISaveFileDialog)m_dlg).CreatePrompt = value; }
		}

		/// <inheritdoc />
		public bool OverwritePrompt
		{
			get { return ((ISaveFileDialog)m_dlg).OverwritePrompt; }
			set { ((ISaveFileDialog)m_dlg).OverwritePrompt = value; }
		}

		/// <inheritdoc />
		public Stream OpenFile()
		{
			return ((ISaveFileDialog)m_dlg).OpenFile();
		}
	}
}
