// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html).
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using Gtk;
using SIL.Utils.FileDialog;

namespace SIL.Utils.FileDialog.Linux
{
	internal class FolderBrowserDialogLinux: FileDialogLinux, IFolderBrowserDialog
	{
		public FolderBrowserDialogLinux()
		{
			Action = FileChooserAction.SelectFolder;
			LocalReset();
		}

		private Environment.SpecialFolder? InternalRootFolder { get; set; }

		#region IFolderBrowserDialog implementation
		public string Description
		{
			get { return Title; }
			set { Title = value; }
		}

		public Environment.SpecialFolder RootFolder
		{
			get { return InternalRootFolder.GetValueOrDefault();}
			set { InternalRootFolder = value;}
		}

		public string SelectedPath { get; set; }

		// TODO: currently we always show the Create Folder button regardless of the
		// ShowNewFolderButton property.
		public bool ShowNewFolderButton { get; set; }
		public object Tag { get; set; }
		#endregion

		protected override void ReportFileNotFound(string fileName)
		{
		}

		private void LocalReset()
		{
			InternalRootFolder = null;
			SelectedPath = null;
			ShowNewFolderButton = true;
			Multiselect = false;
		}

		public override void Reset()
		{
			base.Reset();
			LocalReset();
		}

		protected override FileChooserDialog CreateFileChooserDialog()
		{
			var dlg = base.CreateFileChooserDialog();

			if (InternalRootFolder.HasValue)
				dlg.SetCurrentFolder(Environment.GetFolderPath(RootFolder));
			if (!string.IsNullOrEmpty(SelectedPath))
				dlg.SetFilename(SelectedPath);
			return dlg;
		}

		protected override bool OnOk()
		{
			// Don't call base class
			SelectedPath = m_dlg.Filename;
			return true;
		}
	}
}
#endif
