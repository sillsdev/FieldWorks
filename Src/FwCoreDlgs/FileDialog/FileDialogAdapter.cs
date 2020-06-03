// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs.FileDialog
{
	/// <summary>
	/// File dialog adapter.
	/// </summary>
	public abstract class FileDialogAdapter : IFileDialog
	{
		/// <summary/>
		protected IFileDialog _dlg;

		#region IFileDialog implementation

		/// <inheritdoc />
		event EventHandler IFileDialog.Disposed
		{
			add => _dlg.Disposed += value;
			remove => _dlg.Disposed -= value;
		}

		/// <inheritdoc />
		event CancelEventHandler IFileDialog.FileOk
		{
			add => _dlg.FileOk += value;
			remove => _dlg.FileOk -= value;
		}

		/// <inheritdoc />
		event EventHandler IFileDialog.HelpRequest
		{
			add => _dlg.HelpRequest += value;
			remove => _dlg.HelpRequest -= value;
		}

		/// <inheritdoc />
		void IFileDialog.Reset()
		{
			_dlg.Reset();
		}

		/// <summary>
		/// Runs the dialog box with a default owner.
		/// </summary>
		DialogResult IFileDialog.ShowDialog()
		{
			return _dlg.ShowDialog();
		}

		/// <inheritdoc />
		DialogResult IFileDialog.ShowDialog(IWin32Window owner)
		{
			return _dlg.ShowDialog(owner);
		}

		/// <inheritdoc />
		bool IFileDialog.AddExtension
		{
			get => _dlg.AddExtension;
			set => _dlg.AddExtension = value;
		}

		/// <inheritdoc />
		bool IFileDialog.CheckFileExists
		{
			get => _dlg.CheckFileExists;
			set => _dlg.CheckFileExists = value;
		}

		/// <inheritdoc />
		bool IFileDialog.CheckPathExists
		{
			get => _dlg.CheckPathExists;
			set => _dlg.CheckPathExists = value;
		}

		/// <inheritdoc />
		string IFileDialog.DefaultExt
		{
			get => _dlg.DefaultExt;
			set => _dlg.DefaultExt = value;
		}

		/// <inheritdoc />
		string IFileDialog.FileName
		{
			get => _dlg.FileName;
			set => _dlg.FileName = value;
		}

		/// <inheritdoc />
		string[] IFileDialog.FileNames => _dlg.FileNames;

		/// <inheritdoc />
		string IFileDialog.Filter
		{
			get => _dlg.Filter;
			set => _dlg.Filter = value;
		}

		/// <inheritdoc />
		int IFileDialog.FilterIndex
		{
			get => _dlg.FilterIndex;
			set => _dlg.FilterIndex = value;
		}

		/// <inheritdoc />
		string IFileDialog.InitialDirectory
		{
			get => _dlg.InitialDirectory;
			set => _dlg.InitialDirectory = value;
		}

		/// <inheritdoc />
		bool IFileDialog.RestoreDirectory
		{
			get => _dlg.RestoreDirectory;
			set => _dlg.RestoreDirectory = value;
		}

		/// <inheritdoc />
		bool IFileDialog.ShowHelp
		{
			get => _dlg.ShowHelp;
			set => _dlg.ShowHelp = value;
		}

		/// <inheritdoc />
		bool IFileDialog.SupportMultiDottedExtensions
		{
			get => _dlg.SupportMultiDottedExtensions;
			set => _dlg.SupportMultiDottedExtensions = value;
		}

		/// <inheritdoc />
		string IFileDialog.Title
		{
			get => _dlg.Title;
			set => _dlg.Title = value;
		}

		/// <inheritdoc />
		bool IFileDialog.ValidateNames
		{
			get => _dlg.ValidateNames;
			set => _dlg.ValidateNames = value;
		}
		#endregion

		#region Disposable stuff

		/// <summary />
		~FileDialogAdapter()
		{
			Dispose(false);
		}

		/// <summary/>
		private bool IsDisposed { get; set; }

		/// <inheritdoc />
		void IDisposable.Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				_dlg?.Dispose();
			}
			_dlg = null;
			IsDisposed = true;
		}
		#endregion
	}
}