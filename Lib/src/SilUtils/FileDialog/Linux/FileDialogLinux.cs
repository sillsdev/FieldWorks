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
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using Gtk;

namespace SIL.Utils.FileDialog.Linux
{
	internal abstract class FileDialogLinux: IFileDialog, IDisposable
	{
		protected FileChooserDialog m_dlg;
		protected FileChooserAction Action { get; set; }

		public FileDialogLinux()
		{
			LocalReset();
		}

		#region Disposable stuff
#if DEBUG
		/// <summary>Finalizer</summary>
		~FileDialogLinux()
		{
			Dispose(false);
		}
#endif

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (fDisposing && m_dlg != null)
			{
				ResetFilter(m_dlg);

				//Don't forget to call Destroy() or the dialog window won't get closed.
				m_dlg.Destroy();
				m_dlg.Dispose();

				OnDisposed(EventArgs.Empty);
			}
			m_dlg = null;
		}
		#endregion

		#region Filter related private methods
		protected void ApplyFilter(FileChooserDialog dlg)
		{
			if (string.IsNullOrEmpty(Filter))
				return;

			var parts = Filter.Split('|');
			if (parts.Length % 2 != 0)
				return;

			ResetFilter(dlg);

			for (int i = 0; i < parts.Length - 1; i += 2)
			{
				var filter = new FileFilter();
				filter.Name = parts[i];
				var patterns = parts[i + 1].Split(';');
				foreach (var pattern in patterns)
					filter.AddPattern(pattern.Trim());
				dlg.AddFilter(filter);
			}

			if (FilterIndex > 0 && FilterIndex <= dlg.Filters.Length)
				dlg.Filter = dlg.Filters[FilterIndex - 1];
		}

		private void ResetFilter(FileChooserDialog dlg)
		{
			while (dlg.Filters.Length > 0)
			{
				var filter = dlg.Filters[0];
				dlg.RemoveFilter(filter);
				filter.Dispose();
			}
		}

		private int CurrentFilterIndex
		{
			get
			{
				var currentFilter = m_dlg.Filter;
				for (int i = 0; i < m_dlg.Filters.Length; i++)
				{
					if (currentFilter == m_dlg.Filters[i])
						return i + 1;
				}
				return 0;
			}
		}
		#endregion

		protected string GetCurrentFileName(string fileName)
		{
			var tmpFileName = fileName;
			if (AddExtension && string.IsNullOrEmpty(Path.GetExtension(tmpFileName)))
			{
				var filterParts = string.IsNullOrEmpty(Filter) ? null : Filter.Split('|');

				if (CheckFileExists)
				{
					if (filterParts == null)
					{
						tmpFileName = Path.ChangeExtension(tmpFileName, DefaultExt);
						if (File.Exists(tmpFileName))
							return tmpFileName;
					}
					else
					{
						var patterns = filterParts[(FilterIndex - 1) * 2 + 1].Split(';');
						foreach (var pattern in patterns)
						{
							var ext = Path.GetExtension(pattern);
							tmpFileName = Path.ChangeExtension(tmpFileName, ext);
							if (File.Exists(tmpFileName))
								return tmpFileName;
						}
					}
				}
				else
				{
					if (filterParts == null)
						return Path.ChangeExtension(tmpFileName, DefaultExt);
					var patterns = filterParts[(FilterIndex - 1) * 2 + 1].Split(';');
					var ext = Path.GetExtension(patterns[0]);
					return Path.ChangeExtension(tmpFileName, ext);
				}
			}
			return fileName;
		}

		protected virtual void OnDisposed(EventArgs e)
		{
			if (Disposed != null)
				Disposed(this, e);
		}

		protected virtual void OnHelpRequest(EventArgs e)
		{
			if (HelpRequest != null)
				HelpRequest(this, e);
		}

		protected virtual void OnFileOk(CancelEventArgs e)
		{
			if (FileOk != null)
				FileOk(this, e);
		}

		protected string AcceptButtonText
		{
			get
			{
				switch (Action)
				{
					case FileChooserAction.Open:
						return FileDialogStrings.Open;
					case FileChooserAction.Save:
						return FileDialogStrings.Save;
					default:
						return FileDialogStrings.OK;
				}
			}
		}

		protected ResponseType ShowMessageBox(string formatMessage, ButtonsType buttons,
			MessageType msgType, string fileName)
		{
			using (var messageBox = new MessageDialog(m_dlg, DialogFlags.Modal,
				msgType, buttons, formatMessage, fileName))
			{
				messageBox.Title = Title;
				int retVal = messageBox.Run();
				messageBox.Destroy();
				return (ResponseType)retVal;
			}
		}

		protected abstract void ReportFileNotFound(string fileName);

		protected string InternalFileName
		{
			get
			{
				if (m_dlg.Filenames.Length > 0)
					return GetCurrentFileName(m_dlg.Filenames[0]);
				return string.Empty;
			}
		}

		protected virtual FileChooserDialog CreateFileChooserDialog()
		{
			// TODO: set parent
			var dlg = new FileChooserDialog(Title, null, Action);
			dlg.Response += HandleDlgResponse;

			if (ShowHelp)
				dlg.AddButton(FileDialogStrings.Help, ResponseType.Help);
			dlg.AddButton(FileDialogStrings.Cancel, ResponseType.Cancel);
			dlg.AddButton(AcceptButtonText, ResponseType.Accept);

			dlg.LocalOnly = true;
			dlg.SelectMultiple = Multiselect;
			if (!string.IsNullOrEmpty(InitialDirectory))
				dlg.SetCurrentFolder(InitialDirectory);
			if (!string.IsNullOrEmpty(FileName))
				dlg.SetFilename(FileName);

			ApplyFilter(dlg);

			return dlg;
		}

		protected virtual void HandleDlgResponse(object o, ResponseArgs args)
		{
			switch (args.ResponseId)
			{
				case ResponseType.Accept:
					if (OnOk())
					{
						DialogResult = DialogResult.OK;
						Close();
					}
					break;
				case ResponseType.Help:
					OnHelpRequest(EventArgs.Empty);
					break;
				default:
					DialogResult = DialogResult.Cancel;
					Close();
					break;
			}
		}

		protected void Close()
		{
			m_dlg.Hide();
			Gtk.Application.RunIteration(); // allow window to hide
			Gtk.Application.Quit();
		}

		protected DialogResult DialogResult { get; set; }

		protected virtual bool OnOk()
		{
			for (int i = 0; i < m_dlg.Filenames.Length; i++)
			{
				var fileName = GetCurrentFileName(m_dlg.Filenames[i]);
				if (CheckFileExists && !File.Exists(fileName))
				{
					ReportFileNotFound(fileName);
					return false;
				}
				if (ValidateNames &&
					(Path.GetFileName(fileName).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
					fileName.IndexOfAny(Path.GetInvalidPathChars()) > 0))
				{
					return false;
				}
			}

			var oldFileName = FileName;
			FilterIndex = CurrentFilterIndex;
			FileName = InternalFileName;

			var eventArgs = new CancelEventArgs();

			OnFileOk(eventArgs);
			if (eventArgs.Cancel)
			{
				FileName = oldFileName;
				return false;
			}
			return true;
		}

		private void LocalReset()
		{
			m_Filter = null;
			FilterIndex = 1;
			FileName = string.Empty;
			AddExtension = true;
			CheckFileExists = false;
			CheckPathExists = true;
			DefaultExt = string.Empty;
			InitialDirectory = string.Empty;
			RestoreDirectory = false;
			ShowHelp = false;
			SupportMultiDottedExtensions = false;
			Title = string.Empty;
			ValidateNames = true;

			if (m_dlg == null)
				return;
			ResetFilter(m_dlg);
		}

		#region IFileDialog implementation
		public DialogResult ShowDialog()
		{
			return ShowDialog(null);
		}

		public DialogResult ShowDialog(IWin32Window owner)
		{
			var oldDirectory = Directory.GetCurrentDirectory();
			try
			{
				m_dlg = CreateFileChooserDialog();

				m_dlg.Show();
				Gtk.Application.Run();
				return DialogResult;
			}
			finally
			{
				if (RestoreDirectory)
					Directory.SetCurrentDirectory(oldDirectory);
			}
		}

		public event EventHandler Disposed;
		public event CancelEventHandler FileOk;
		public event EventHandler HelpRequest;

		public virtual void Reset()
		{
			LocalReset();
		}

		public string[] FileNames
		{
			get
			{
				if (m_dlg != null)
				{
					var fileNames = new string[m_dlg.Filenames.Length];
					for (int i = 0; i < m_dlg.Filenames.Length; i++)
						fileNames[i] = GetCurrentFileName(m_dlg.Filenames[i]);
					return fileNames;
				}
				return new string[0];
			}
		}

		private string m_Filter;

		public string Filter
		{
			get { return m_Filter; }
			set
			{
				if (value == null)
					throw new ArgumentException();

				var parts = value.Split('|');
				if (parts.Length % 2 != 0)
					throw new ArgumentException();

				m_Filter = value;
			}
		}

		/// <summary>Always returns true with Gtk dialog</summary>
		public bool CheckPathExists
		{
			get { return true;}
			set {}
		}

		public bool AddExtension { get; set; }
		public bool CheckFileExists { get; set; }
		public string DefaultExt { get; set; }
		public string FileName { get; set; }
		public int FilterIndex { get; set; }
		public string InitialDirectory { get; set; }
		public bool RestoreDirectory { get; set; }
		public bool ShowHelp { get; set; }
		// TODO: Currently ignored
		public bool SupportMultiDottedExtensions { get; set; }
		public string Title { get; set; }
		public bool ValidateNames { get; set; }
		#endregion

		// From IOpenFileDialog
		public bool Multiselect { get; set; }
	}
}
#endif
