// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if !__MonoCS__
using System.IO;
using System.Windows.Forms;

namespace SIL.Utils.FileDialog.Windows
{
	internal class SaveFileDialogWindows: FileDialogWindows, ISaveFileDialog
	{
		public SaveFileDialogWindows()
		{
			m_dlg = new SaveFileDialog();
		}

		public bool CreatePrompt
		{
			get { return ((SaveFileDialog)m_dlg).CreatePrompt; }
			set { ((SaveFileDialog)m_dlg).CreatePrompt = value; }
		}

		public bool OverwritePrompt
		{
			get { return ((SaveFileDialog)m_dlg).OverwritePrompt; }
			set { ((SaveFileDialog)m_dlg).OverwritePrompt = value; }
		}

		public Stream OpenFile()
		{
			return ((SaveFileDialog)m_dlg).OpenFile();
		}
	}
}
#endif
