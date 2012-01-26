// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using System.IO;
using Gtk;

namespace SIL.Utils.FileDialog.Linux
{
	internal class OpenFileDialogLinux: FileDialogLinux, IOpenFileDialog
	{
		public OpenFileDialogLinux()
		{
			Action = FileChooserAction.Open;
			LocalReset();
		}

		#region IOpenFileDialog implementation
		public Stream OpenFile()
		{
			return new FileStream(FileName, FileMode.Open);
		}
		#endregion

		protected override void ReportFileNotFound(string fileName)
		{
			ShowMessageBox(FileDialogStrings.FileNotFoundOpen, ButtonsType.Ok, MessageType.Warning,
				fileName);
		}

		private void LocalReset()
		{
			Title = FileDialogStrings.TitleOpen;
		}

		public override void Reset()
		{
			base.Reset();
			LocalReset();
		}
	}
}
#endif
