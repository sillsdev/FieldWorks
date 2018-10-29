// Copyright (c) 2011-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.Controls.FileDialog.Windows
{
	internal class FolderBrowserDialogWindows : IFolderBrowserDialog, IDisposable
	{
		protected FolderBrowserDialog m_dlg;

		public FolderBrowserDialogWindows()
		{
			m_dlg = new FolderBrowserDialog();
		}

		#region IFolderBrowserDialog implementation
		public event EventHandler Disposed
		{
			add { m_dlg.Disposed += value; }
			remove { m_dlg.Disposed -= value; }
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
			get { return m_dlg.Description; }
			set { m_dlg.Description = value; }
		}

		public Environment.SpecialFolder RootFolder
		{
			get { return m_dlg.RootFolder; }
			set { m_dlg.RootFolder = value; }
		}

		public string SelectedPath
		{
			get { return m_dlg.SelectedPath; }
			set { m_dlg.SelectedPath = value; }
		}

		public bool ShowNewFolderButton
		{
			get { return m_dlg.ShowNewFolderButton; }
			set { m_dlg.ShowNewFolderButton = value; }
		}

		public object Tag
		{
			get { return m_dlg.Tag; }
			set { m_dlg.Tag = value; }
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
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType() + ". *******");

			if (fDisposing)
			{
				// dispose managed and unmanaged objects
				m_dlg?.Dispose();
			}
			m_dlg = null;
		}
		#endregion
	}
}