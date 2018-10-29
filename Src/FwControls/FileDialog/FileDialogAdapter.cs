// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// File dialog adapter.
	/// </summary>
	public abstract class FileDialogAdapter : IFileDialog, IDisposable
	{
		/// <summary/>
		protected IFileDialog m_dlg;

		#region IFileDialog implementation

		/// <inheritdoc />
		public event EventHandler Disposed
		{
			add { m_dlg.Disposed += value; }
			remove { m_dlg.Disposed -= value; }
		}

		/// <inheritdoc />
		public event CancelEventHandler FileOk
		{
			add { m_dlg.FileOk += value; }
			remove { m_dlg.FileOk -= value; }
		}

		/// <inheritdoc />
		public event EventHandler HelpRequest
		{
			add { m_dlg.HelpRequest += value; }
			remove { m_dlg.HelpRequest -= value; }
		}

		/// <inheritdoc />
		public void Reset()
		{
			m_dlg.Reset();
		}

		/// <summary>
		/// Runs the dialog box with a default owner.
		/// </summary>
		public DialogResult ShowDialog()
		{
			return m_dlg.ShowDialog();
		}

		/// <inheritdoc />
		public DialogResult ShowDialog(IWin32Window owner)
		{
			return m_dlg.ShowDialog(owner);
		}

		/// <inheritdoc />
		public bool AddExtension
		{
			get { return m_dlg.AddExtension; }
			set { m_dlg.AddExtension = value; }
		}

		/// <inheritdoc />
		public bool CheckFileExists
		{
			get { return m_dlg.CheckFileExists; }
			set { m_dlg.CheckFileExists = value; }
		}

		/// <inheritdoc />
		public bool CheckPathExists
		{
			get { return m_dlg.CheckPathExists; }
			set { m_dlg.CheckPathExists = value; }
		}

		/// <inheritdoc />
		public string DefaultExt
		{
			get { return m_dlg.DefaultExt; }
			set { m_dlg.DefaultExt = value; }
		}

		/// <inheritdoc />
		public string FileName
		{
			get { return m_dlg.FileName; }
			set { m_dlg.FileName = value; }
		}

		/// <inheritdoc />
		public string[] FileNames => m_dlg.FileNames;

		/// <inheritdoc />
		public string Filter
		{
			get { return m_dlg.Filter; }
			set { m_dlg.Filter = value; }
		}

		/// <inheritdoc />
		public int FilterIndex
		{
			get { return m_dlg.FilterIndex; }
			set { m_dlg.FilterIndex = value; }
		}

		/// <inheritdoc />
		public string InitialDirectory
		{
			get { return m_dlg.InitialDirectory; }
			set { m_dlg.InitialDirectory = value; }
		}

		/// <inheritdoc />
		public bool RestoreDirectory
		{
			get { return m_dlg.RestoreDirectory; }
			set { m_dlg.RestoreDirectory = value; }
		}

		/// <inheritdoc />
		public bool ShowHelp
		{
			get { return m_dlg.ShowHelp; }
			set { m_dlg.ShowHelp = value; }
		}

		/// <inheritdoc />
		public bool SupportMultiDottedExtensions
		{
			get { return m_dlg.SupportMultiDottedExtensions; }
			set { m_dlg.SupportMultiDottedExtensions = value; }
		}

		/// <inheritdoc />
		public string Title
		{
			get { return m_dlg.Title; }
			set { m_dlg.Title = value; }
		}

		/// <inheritdoc />
		public bool ValidateNames
		{
			get { return m_dlg.ValidateNames; }
			set { m_dlg.ValidateNames = value; }
		}
		#endregion

		#region Disposable stuff

		/// <summary />
		~FileDialogAdapter()
		{
			Dispose(false);
		}

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <inheritdoc />
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary />
		protected virtual void Dispose(bool disposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType() + ". *******");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				var disposable = m_dlg as IDisposable;
				disposable?.Dispose();
			}
			m_dlg = null;
			IsDisposed = true;
		}
		#endregion
	}
}