// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog.Windows
{
	internal class OpenFileDialogWindows : FileDialogWindows, IOpenFileDialog
	{
		public OpenFileDialogWindows()
		{
			m_dlg = new OpenFileDialog();
		}

		#region IOpenFileDialog implementation
		public Stream OpenFile()
		{
			return ((OpenFileDialog)m_dlg).OpenFile();
		}

		public bool Multiselect
		{
			get { return ((OpenFileDialog)m_dlg).Multiselect; }
			set { ((OpenFileDialog)m_dlg).Multiselect = value; }
		}
		#endregion
	}
}