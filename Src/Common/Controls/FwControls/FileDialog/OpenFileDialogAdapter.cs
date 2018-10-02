// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.IO;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>Cross-platform OpenFile dialog. On Windows it displays .NET's WinForms
	/// OpenFileDialog, on Linux the GTK FileChooserDialog.</summary>
	public class OpenFileDialogAdapter: FileDialogAdapter, IOpenFileDialog
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="SIL.FieldWorks.Common.Controls.FileDialog.OpenFileDialogAdapter"/> class.
		/// </summary>
		public OpenFileDialogAdapter()
		{
			m_dlg = Manager.CreateOpenFileDialog();
		}

		#region IOpenFileDialog implementation
		/// <summary>
		/// Opens the file selected by the user, with read-only permission. The file is specified
		/// by the FileName property.
		/// </summary>
		public Stream OpenFile()
		{
			return ((IOpenFileDialog)m_dlg).OpenFile();
		}

		/// <summary>
		/// Gets or sets a value indicating whether the dialog box allows multiple files to be
		/// selected.
		/// </summary>
		public bool Multiselect
		{
			get { return ((IOpenFileDialog)m_dlg).Multiselect; }
			set { ((IOpenFileDialog)m_dlg).Multiselect = value; }
		}
		#endregion
	}
}
