// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>Cross-platform OpenFile dialog. On Windows it displays .NET's WinForms
	/// OpenFileDialog, on Linux the GTK FileChooserDialog.</summary>
	public sealed class OpenFileDialogAdapter : FileDialogAdapter, IOpenFileDialog
	{
		/// <summary />
		public OpenFileDialogAdapter()
		{
			_dlg = FileDialogFactory.CreateOpenFileDialog();
		}

		#region IOpenFileDialog implementation

		/// <inheritdoc />
		Stream IOpenFileDialog.OpenFile()
		{
			return ((IOpenFileDialog)_dlg).OpenFile();
		}

		/// <inheritdoc />
		bool IOpenFileDialog.Multiselect
		{
			get => ((IOpenFileDialog)_dlg).Multiselect;
			set => ((IOpenFileDialog)_dlg).Multiselect = value;
		}
		#endregion
	}
}
