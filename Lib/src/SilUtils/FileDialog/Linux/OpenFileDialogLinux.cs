// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html).
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
