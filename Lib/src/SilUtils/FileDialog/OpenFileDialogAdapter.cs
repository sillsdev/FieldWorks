// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System.IO;

namespace SIL.Utils.FileDialog
{
	/// <summary>Cross-platform OpenFile dialog. On Windows it displays .NET's WinForms
	/// OpenFileDialog, on Linux the GTK FileChooserDialog.</summary>
	public class OpenFileDialogAdapter: FileDialogAdapter, IOpenFileDialog
	{
		public OpenFileDialogAdapter()
		{
			m_dlg = Manager.CreateOpenFileDialog();
		}

		#region IOpenFileDialog implementation
		public Stream OpenFile()
		{
			return ((IOpenFileDialog)m_dlg).OpenFile();
		}

		public bool Multiselect
		{
			get { return ((IOpenFileDialog)m_dlg).Multiselect; }
			set { ((IOpenFileDialog)m_dlg).Multiselect = value; }
		}
		#endregion
	}
}
