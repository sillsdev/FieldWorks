// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// Copyright (c) 2012-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html).
// </copyright>
// --------------------------------------------------------------------------------------------
#if !__MonoCS__
using System;
using System.Windows.Forms;
using SIL.Utils.FileDialog;

namespace SIL.Utils.FileDialog.Windows
{
	internal class FolderBrowserDialogWindows: IFolderBrowserDialog, IDisposable
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
#if DEBUG
		/// <summary>Finalizer</summary>
		~FolderBrowserDialogWindows()
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
				// dispose managed and unmanaged objects
				m_dlg.Dispose();
			}
			m_dlg = null;
		}
		#endregion
	}
}
#endif
