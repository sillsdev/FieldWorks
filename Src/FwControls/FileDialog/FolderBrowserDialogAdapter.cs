// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog
{
	/// <summary>
	/// Cross-platform FolderBrowser dialog. On Windows it displays .NET's WinForms
	/// FolderBrowserDialog, on Linux the GTK FileChooserDialog (limited to selecting folders).
	/// </summary>
	public class FolderBrowserDialogAdapter : IFolderBrowserDialog, IDisposable
	{
		/// <summary/>
		protected IFolderBrowserDialog m_dlg;

		/// <summary />
		public FolderBrowserDialogAdapter()
		{
			m_dlg = Manager.CreateFolderBrowserDialog();
		}

		#region IFolderBrowserDialog implementation

		/// <inheritdoc />
		public event EventHandler Disposed
		{
			add { m_dlg.Disposed += value; }
			remove { m_dlg.Disposed -= value; }
		}

		/// <inheritdoc />
		public void Reset()
		{
			m_dlg.Reset();
		}

		/// <inheritdoc />
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
		public string Description
		{
			get { return m_dlg.Description; }
			set { m_dlg.Description = value; }
		}

		/// <inheritdoc />
		public Environment.SpecialFolder RootFolder
		{
			get { return m_dlg.RootFolder; }
			set { m_dlg.RootFolder = value; }
		}

		/// <inheritdoc />
		public string SelectedPath
		{
			get { return m_dlg.SelectedPath; }
			set { m_dlg.SelectedPath = value; }
		}

		/// <inheritdoc />
		public bool ShowNewFolderButton
		{
			get { return m_dlg.ShowNewFolderButton; }
			set { m_dlg.ShowNewFolderButton = value; }
		}

		/// <inheritdoc />
		public object Tag
		{
			get { return m_dlg.Tag; }
			set { m_dlg.Tag = value; }
		}
		#endregion

		#region Disposable stuff

		/// <summary />
		~FolderBrowserDialogAdapter()
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

		/// <summary/>
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