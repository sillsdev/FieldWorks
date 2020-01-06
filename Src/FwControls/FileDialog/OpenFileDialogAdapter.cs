// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>Cross-platform OpenFile dialog. On Windows it displays .NET's WinForms
	/// OpenFileDialog, on Linux the GTK FileChooserDialog.</summary>
	public class OpenFileDialogAdapter : FileDialogAdapter, IOpenFileDialog
	{
		/// <summary />
		public OpenFileDialogAdapter()
		{
			m_dlg = Manager.CreateOpenFileDialog();
		}

		#region IOpenFileDialog implementation

		/// <inheritdoc />
		public Stream OpenFile()
		{
			return ((IOpenFileDialog)m_dlg).OpenFile();
		}

		/// <inheritdoc />
		public bool Multiselect
		{
			get { return ((IOpenFileDialog)m_dlg).Multiselect; }
			set { ((IOpenFileDialog)m_dlg).Multiselect = value; }
		}
		#endregion
	}
}
