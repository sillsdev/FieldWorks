// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Forms;
using Gtk;
using SIL.PlatformUtilities;

namespace SIL.FieldWorks.FwCoreDlgs.FileDialog
{
	/// <summary>
	/// FileDialogFactory class for the OpenFileDialog. Allows to use a different OpenFileDialog during
	/// unit tests.
	/// </summary>
	internal static class FileDialogFactory
	{
		private static Type s_OpenFileDialogType;
		private static Type s_SaveFileDialogType;
		private static Type s_FolderBrowserDialogType;

		static FileDialogFactory()
		{
			OuterReset();
		}

		/// <summary>
		/// Sets the OpenFileDialog type.
		/// </summary>
		private static void SetOpenFileDialog<T>() where T : IOpenFileDialog
		{
			s_OpenFileDialogType = typeof(T);
		}

		/// <summary>
		/// Sets the SaveFileDialog type.
		/// </summary>
		private static void SetSaveFileDialog<T>() where T : ISaveFileDialog
		{
			s_SaveFileDialogType = typeof(T);
		}

		/// <summary>
		/// Sets the FolderBrowserDialog type.
		/// </summary>
		private static void SetFolderBrowserDialog<T>() where T : IFolderBrowserDialog
		{
			s_FolderBrowserDialogType = typeof(T);
		}

		/// <summary>
		/// Resets the dialog types to the default.
		/// </summary>
		private static void OuterReset()
		{
			if (Platform.IsWindows)
			{
				ResetWindows();
			}
			else
			{
				ResetLinux();
			}
		}

		// NOTE: leave this as a separate method. Otherwise we need the gtk-sharp assemblies
		// on Windows.
		private static void ResetLinux()
		{
			SetOpenFileDialog<OpenFileDialogLinux>();
			SetSaveFileDialog<SaveFileDialogLinux>();
			SetFolderBrowserDialog<FolderBrowserDialogLinux>();
		}

		private static void ResetWindows()
		{
			SetOpenFileDialog<OpenFileDialogWindows>();
			SetSaveFileDialog<SaveFileDialogWindows>();
			SetFolderBrowserDialog<FolderBrowserDialogWindows>();
		}

		/// <summary>
		/// Creates the open file dialog.
		/// </summary>
		internal static IOpenFileDialog CreateOpenFileDialog()
		{
			return (IOpenFileDialog)Activator.CreateInstance(s_OpenFileDialogType, true);
		}

		/// <summary>
		/// Creates the save file dialog.
		/// </summary>
		internal static ISaveFileDialog CreateSaveFileDialog()
		{
			return (ISaveFileDialog)Activator.CreateInstance(s_SaveFileDialogType, true);
		}

		/// <summary>
		/// Creates the folder browser dialog.
		/// </summary>
		internal static IFolderBrowserDialog CreateFolderBrowserDialog()
		{
			return (IFolderBrowserDialog)Activator.CreateInstance(s_FolderBrowserDialogType, true);
		}

		#region Linux dlgs

		private abstract class FileDialogLinux : IFileDialog, IDisposable
		{
			private string _filter;
			protected FileChooserDialog _dlg;
			protected FileChooserAction Action { get; set; }

			protected FileDialogLinux()
			{
				LocalReset();
			}

			#region Disposable stuff

			/// <summary />
			~FileDialogLinux()
			{
				Dispose(false);
			}

			/// <inheritdoc />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary />
			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". *******");

				if (disposing)
				{
					if (_dlg != null)
					{
						ResetFilter(_dlg);
						//Don't forget to call Destroy() or the dialog window won't get closed.
						_dlg.Destroy();
						_dlg.Dispose();
					}

					OnDisposed(EventArgs.Empty);
				}
				_dlg = null;
			}
			#endregion

#pragma warning disable 0067 // The event is never used
			public event EventHandler Disposed;
			public event CancelEventHandler FileOk;
			public event EventHandler HelpRequest;
#pragma warning restore 0067

			#region Private methods
			private void ApplyFilter(FileChooserDialog dlg)
			{
				if (string.IsNullOrEmpty(Filter))
				{
					return;
				}
				var parts = Filter.Split('|');
				if (parts.Length % 2 != 0)
				{
					return;
				}
				ResetFilter(dlg);

				for (var i = 0; i < parts.Length - 1; i += 2)
				{
					var filter = new FileFilter
					{
						Name = parts[i]
					};
					var patterns = parts[i + 1].Split(';');
					foreach (var pattern in patterns)
					{
						filter.AddPattern(pattern.Trim());
					}
					dlg.AddFilter(filter);
				}

				if (FilterIndex > 0 && FilterIndex <= dlg.Filters.Length)
				{
					dlg.Filter = dlg.Filters[FilterIndex - 1];
				}
			}

			private static void ResetFilter(FileChooserDialog dlg)
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
					var currentFilter = _dlg.Filter;
					for (var i = 0; i < _dlg.Filters.Length; i++)
					{
						if (currentFilter == _dlg.Filters[i])
						{
							return i + 1;
						}
					}
					return 0;
				}
			}

			private string GetCurrentFileName(string fileName)
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
							{
								return tmpFileName;
							}
						}
						else
						{
							var patterns = filterParts[(FilterIndex - 1) * 2 + 1].Split(';');
							foreach (var pattern in patterns)
							{
								var ext = Path.GetExtension(pattern);
								tmpFileName = Path.ChangeExtension(tmpFileName, ext);
								if (File.Exists(tmpFileName))
								{
									return tmpFileName;
								}
							}
						}
					}
					else
					{
						if (filterParts == null)
						{
							return Path.ChangeExtension(tmpFileName, DefaultExt);
						}
						var patterns = filterParts[(FilterIndex - 1) * 2 + 1].Split(';');
						var ext = Path.GetExtension(patterns[0]);
						return Path.ChangeExtension(tmpFileName, ext);
					}
				}
				return fileName;
			}

			private string AcceptButtonText
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

			private void Close()
			{
				_dlg.Hide();
				Gtk.Application.RunIteration(); // allow window to hide
				Gtk.Application.Quit();
			}

			private DialogResult DialogResult { get; set; }

			private void LocalReset()
			{
				_filter = null;
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

				if (_dlg == null)
				{
					return;
				}
				ResetFilter(_dlg);
			}
			#endregion

			protected virtual void OnDisposed(EventArgs e)
			{
				Disposed?.Invoke(this, e);
			}

			protected virtual void OnHelpRequest(EventArgs e)
			{
				HelpRequest?.Invoke(this, e);
			}

			protected virtual void OnFileOk(CancelEventArgs e)
			{
				FileOk?.Invoke(this, e);
			}

			protected ResponseType ShowMessageBox(string formatMessage, ButtonsType buttons, MessageType msgType, string fileName)
			{
				using (var messageBox = new MessageDialog(_dlg, DialogFlags.Modal, msgType, buttons, formatMessage, fileName))
				{
					messageBox.Title = Title;
					var retVal = messageBox.Run();
					messageBox.Destroy();
					return (ResponseType)retVal;
				}
			}

			protected abstract void ReportFileNotFound(string fileName);

			protected string InternalFileName => _dlg.Filenames.Length > 0 ? GetCurrentFileName(_dlg.Filenames[0]) : string.Empty;

			protected virtual bool SelectMultiple { get; set; }

			protected virtual FileChooserDialog CreateFileChooserDialog()
			{
				// TODO: set parent
				var dlg = new FileChooserDialog(Title, null, Action);
				dlg.Response += HandleDlgResponse;

				if (ShowHelp)
				{
					dlg.AddButton(FileDialogStrings.Help, ResponseType.Help);
				}
				dlg.AddButton(FileDialogStrings.Cancel, ResponseType.Cancel);
				dlg.AddButton(AcceptButtonText, ResponseType.Accept);
				dlg.LocalOnly = true;
				dlg.SelectMultiple = SelectMultiple;
				if (!string.IsNullOrEmpty(InitialDirectory))
				{
					dlg.SetCurrentFolder(InitialDirectory);
				}
				if (!string.IsNullOrEmpty(FileName))
				{
					dlg.SetFilename(FileName);
				}
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

			protected virtual bool OnOk()
			{
				foreach (var filename in _dlg.Filenames)
				{
					var fileName = GetCurrentFileName(filename);
					if (CheckFileExists && !File.Exists(fileName))
					{
						ReportFileNotFound(fileName);
						return false;
					}
					if (ValidateNames && (Path.GetFileName(fileName).IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || fileName.IndexOfAny(Path.GetInvalidPathChars()) > 0))
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
					_dlg = CreateFileChooserDialog();

					_dlg.Show();
					Gtk.Application.Run();
					return DialogResult;
				}
				finally
				{
					if (RestoreDirectory)
					{
						Directory.SetCurrentDirectory(oldDirectory);
					}
				}
			}

			public virtual void Reset()
			{
				LocalReset();
			}

			public string[] FileNames
			{
				get
				{
					if (_dlg != null)
					{
						var fileNames = new string[_dlg.Filenames.Length];
						for (var i = 0; i < _dlg.Filenames.Length; i++)
						{
							fileNames[i] = GetCurrentFileName(_dlg.Filenames[i]);
						}
						return fileNames;
					}
					return new string[0];
				}
			}

			public string Filter
			{
				get => _filter;
				set
				{
					if (value == null)
					{
						throw new ArgumentException();
					}
					var parts = value.Split('|');
					if (parts.Length % 2 != 0)
					{
						throw new ArgumentException();
					}
					_filter = value;
				}
			}

			/// <summary>Always returns true with Gtk dialog</summary>
			public bool CheckPathExists
			{
				get => true;
				set { }
			}

			public bool AddExtension { get; set; }
			public bool CheckFileExists { get; set; }
			public string DefaultExt { get; set; }
			public string FileName { get; set; }
			public int FilterIndex { get; set; }
			public string InitialDirectory { get; set; }
			public bool RestoreDirectory { get; set; }
			public bool ShowHelp { get; set; }
			public bool SupportMultiDottedExtensions { get; set; }
			public string Title { get; set; }
			public bool ValidateNames { get; set; }
			#endregion
		}

		private sealed class SaveFileDialogLinux : FileDialogLinux, ISaveFileDialog
		{
			internal SaveFileDialogLinux()
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

			protected override void OnFileOk(CancelEventArgs e)
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

		private sealed class OpenFileDialogLinux : FileDialogLinux, IOpenFileDialog
		{
			internal OpenFileDialogLinux()
			{
				Action = FileChooserAction.Open;
				LocalReset();
			}

			private IOpenFileDialog AsIOpenFileDialog => this;

			protected override bool SelectMultiple
			{
				get => AsIOpenFileDialog.Multiselect;
				set => AsIOpenFileDialog.Multiselect = value;
			}

			#region IOpenFileDialog implementation

			public bool Multiselect { get; set; }

			public Stream OpenFile()
			{
				return new FileStream(FileName, FileMode.Open);
			}
			#endregion

			protected override void ReportFileNotFound(string fileName)
			{
				ShowMessageBox(string.Format(FileDialogStrings.FileNotFoundOpen, Environment.NewLine), ButtonsType.Ok, MessageType.Warning, fileName);
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

		private sealed class FolderBrowserDialogLinux : FileDialogLinux, IFolderBrowserDialog
		{
			internal FolderBrowserDialogLinux()
			{
				Action = FileChooserAction.SelectFolder;
				LocalReset();
			}

			private Environment.SpecialFolder? InternalRootFolder { get; set; }

			#region IFolderBrowserDialog implementation
			public string Description
			{
				get => Title;
				set => Title = value;
			}

			public Environment.SpecialFolder RootFolder
			{
				get => InternalRootFolder.GetValueOrDefault();
				set => InternalRootFolder = value;
			}

			public string SelectedPath { get; set; }

			// TODO: currently we always show the Create Folder button regardless of the
			// ShowNewFolderButton property.
			public bool ShowNewFolderButton { get; set; }
			#endregion

			public object Tag { get; set; }

			protected override void ReportFileNotFound(string fileName)
			{
			}

			private void LocalReset()
			{
				InternalRootFolder = null;
				SelectedPath = null;
				ShowNewFolderButton = true;
				SelectMultiple = false;
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
				{
					dlg.SetCurrentFolder(Environment.GetFolderPath(RootFolder));
				}
				if (!string.IsNullOrEmpty(SelectedPath))
				{
					dlg.SetFilename(SelectedPath);
				}
				return dlg;
			}

			protected override bool OnOk()
			{
				// Don't call base class
				SelectedPath = _dlg.Filename;
				return true;
			}
		}
		#endregion

		#region Windows dlgs
		private sealed class FolderBrowserDialogWindows : IFolderBrowserDialog, IDisposable
		{
			private FolderBrowserDialog m_dlg;

			internal FolderBrowserDialogWindows()
			{
				m_dlg = new FolderBrowserDialog();
			}

			#region IFolderBrowserDialog implementation
			public event EventHandler Disposed
			{
				add => m_dlg.Disposed += value;
				remove => m_dlg.Disposed -= value;
			}

			public void Reset()
			{
				m_dlg.Reset();
			}

			public DialogResult ShowDialog()
			{
				return m_dlg.ShowDialog();
			}

			public DialogResult ShowDialog(IWin32Window owner)
			{
				return m_dlg.ShowDialog(owner);
			}

			public string Description
			{
				get => m_dlg.Description;
				set => m_dlg.Description = value;
			}

			public Environment.SpecialFolder RootFolder
			{
				get => m_dlg.RootFolder;
				set => m_dlg.RootFolder = value;
			}

			public string SelectedPath
			{
				get => m_dlg.SelectedPath;
				set => m_dlg.SelectedPath = value;
			}

			public bool ShowNewFolderButton
			{
				get => m_dlg.ShowNewFolderButton;
				set => m_dlg.ShowNewFolderButton = value;
			}

			public object Tag
			{
				get => m_dlg.Tag;
				set => m_dlg.Tag = value;
			}
			#endregion

			#region Disposable stuff

			/// <summary />
			~FolderBrowserDialogWindows()
			{
				Dispose(false);
			}

			/// <summary/>
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			private void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". *******");

				if (disposing)
				{
					// dispose managed objects
					m_dlg?.Dispose();
				}
				m_dlg = null;
			}
			#endregion
		}

		private abstract class FileDialogWindows : IFileDialog, IDisposable
		{
			protected System.Windows.Forms.FileDialog _dlg;

			#region IFileDialog implementation
			public event EventHandler Disposed
			{
				add => _dlg.Disposed += value;
				remove => _dlg.Disposed -= value;
			}

			public event CancelEventHandler FileOk
			{
				add => _dlg.FileOk += value;
				remove => _dlg.FileOk -= value;
			}

			public event EventHandler HelpRequest
			{
				add => _dlg.HelpRequest += value;
				remove => _dlg.HelpRequest -= value;
			}

			public void Reset()
			{
				_dlg.Reset();
			}

			public DialogResult ShowDialog()
			{
				return _dlg.ShowDialog();
			}

			public DialogResult ShowDialog(IWin32Window owner)
			{
				return _dlg.ShowDialog(owner);
			}

			public bool AddExtension
			{
				get => _dlg.AddExtension;
				set => _dlg.AddExtension = value;
			}

			public bool CheckFileExists
			{
				get => _dlg.CheckFileExists;
				set => _dlg.CheckFileExists = value;
			}

			public bool CheckPathExists
			{
				get => _dlg.CheckPathExists;
				set => _dlg.CheckPathExists = value;
			}

			public string DefaultExt
			{
				get => _dlg.DefaultExt;
				set => _dlg.DefaultExt = value;
			}

			public string FileName
			{
				get => _dlg.FileName;
				set => _dlg.FileName = value;
			}

			public string[] FileNames => _dlg.FileNames;

			public string Filter
			{
				get => _dlg.Filter;
				set => _dlg.Filter = value;
			}

			public int FilterIndex
			{
				get => _dlg.FilterIndex;
				set => _dlg.FilterIndex = value;
			}

			public string InitialDirectory
			{
				get => _dlg.InitialDirectory;
				set => _dlg.InitialDirectory = value;
			}

			public bool RestoreDirectory
			{
				get => _dlg.RestoreDirectory;
				set => _dlg.RestoreDirectory = value;
			}

			public bool ShowHelp
			{
				get => _dlg.ShowHelp;
				set => _dlg.ShowHelp = value;
			}

			public bool SupportMultiDottedExtensions
			{
				get => _dlg.SupportMultiDottedExtensions;
				set => _dlg.SupportMultiDottedExtensions = value;
			}

			public string Title
			{
				get => _dlg.Title;
				set => _dlg.Title = value;
			}

			public bool ValidateNames
			{
				get => _dlg.ValidateNames;
				set => _dlg.ValidateNames = value;
			}
			#endregion

			#region Disposable stuff

			/// <summary />
			~FileDialogWindows()
			{
				Dispose(false);
			}

			/// <inheritdoc />
			public void Dispose()
			{
				Dispose(true);
				GC.SuppressFinalize(this);
			}

			/// <summary/>
			protected virtual void Dispose(bool disposing)
			{
				Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". *******");

				if (disposing)
				{
					_dlg?.Dispose();
				}
				_dlg = null;
			}
			#endregion
		}

		private sealed class OpenFileDialogWindows : FileDialogWindows, IOpenFileDialog
		{
			internal OpenFileDialogWindows()
			{
				_dlg = new OpenFileDialog();
			}

			#region IOpenFileDialog implementation
			public Stream OpenFile()
			{
				return ((OpenFileDialog)_dlg).OpenFile();
			}

			public bool Multiselect
			{
				get => ((OpenFileDialog)_dlg).Multiselect;
				set => ((OpenFileDialog)_dlg).Multiselect = value;
			}
			#endregion
		}

		private sealed class SaveFileDialogWindows : FileDialogWindows, ISaveFileDialog
		{
			internal SaveFileDialogWindows()
			{
				_dlg = new SaveFileDialog();
			}

			public bool CreatePrompt
			{
				get => ((SaveFileDialog)_dlg).CreatePrompt;
				set => ((SaveFileDialog)_dlg).CreatePrompt = value;
			}

			public bool OverwritePrompt
			{
				get => ((SaveFileDialog)_dlg).OverwritePrompt;
				set => ((SaveFileDialog)_dlg).OverwritePrompt = value;
			}

			public Stream OpenFile()
			{
				return ((SaveFileDialog)_dlg).OpenFile();
			}
		}
		#endregion
	}
}