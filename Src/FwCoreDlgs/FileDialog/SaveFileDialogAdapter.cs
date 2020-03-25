// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;

namespace SIL.FieldWorks.FwCoreDlgs.FileDialog
{
	/// <summary>
	/// Prompts the user to select a location for saving a file.
	/// </summary>
	public sealed class SaveFileDialogAdapter : FileDialogAdapter, ISaveFileDialog
	{
		/// <summary />
		public SaveFileDialogAdapter()
		{
			_dlg = FileDialogFactory.CreateSaveFileDialog();
		}

		private ISaveFileDialog DlgAsISaveFileDialog => (ISaveFileDialog)_dlg;

		/// <inheritdoc />
		bool ISaveFileDialog.CreatePrompt
		{
			get => DlgAsISaveFileDialog.CreatePrompt;
			set => DlgAsISaveFileDialog.CreatePrompt = value;
		}

		/// <inheritdoc />
		bool ISaveFileDialog.OverwritePrompt
		{
			get => DlgAsISaveFileDialog.OverwritePrompt;
			set => DlgAsISaveFileDialog.OverwritePrompt = value;
		}

		/// <inheritdoc />
		Stream ISaveFileDialog.OpenFile()
		{
			return DlgAsISaveFileDialog.OpenFile();
		}
	}
}
