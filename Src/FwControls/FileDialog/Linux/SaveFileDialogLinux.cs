// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using Gtk;

namespace SIL.FieldWorks.Common.Controls.FileDialog.Linux
{
	internal class SaveFileDialogLinux : FileDialogLinux, ISaveFileDialog
	{
		public SaveFileDialogLinux()
		{
			Action = FileChooserAction.Save;
			LocalReset();
		}

		#region ISaveFileDialog implementation
		public Stream OpenFile()
		{
			return new FileStream(FileName, FileMode.Create);
		}

		public bool CreatePrompt { get; set; }
		public bool OverwritePrompt { get; set; }
		#endregion

		private void LocalReset()
		{
			CreatePrompt = false;
			OverwritePrompt = true;
			Title = FileDialogStrings.TitleSave;
		}

		public override void Reset()
		{
			base.Reset();
			LocalReset();
		}

		protected override void ReportFileNotFound(string fileName)
		{
			ShowMessageBox(string.Format(FileDialogStrings.FileNotFoundSave, Environment.NewLine), ButtonsType.Ok, MessageType.Warning, fileName);
		}

		private bool OkToCreateFile()
		{
			return ShowMessageBox(string.Format(FileDialogStrings.CreateFile, Environment.NewLine), ButtonsType.YesNo, MessageType.Question, InternalFileName) == ResponseType.Yes;
		}

		protected override FileChooserDialog CreateFileChooserDialog()
		{
			var dlg = base.CreateFileChooserDialog();
			dlg.DoOverwriteConfirmation = OverwritePrompt;
			return dlg;
		}

		protected override void OnFileOk(System.ComponentModel.CancelEventArgs e)
		{
			if (CreatePrompt && !File.Exists(InternalFileName))
			{
				if (!OkToCreateFile())
				{
					e.Cancel = true;
					return;
				}
			}
			base.OnFileOk(e);
		}
	}
}