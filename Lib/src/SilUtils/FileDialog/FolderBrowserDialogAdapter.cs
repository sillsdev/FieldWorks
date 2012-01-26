// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// 	Copyright (c) 2012, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

namespace SIL.Utils.FileDialog
{
	/// <summary>
	/// Cross-platform FolderBrowser dialog. On Windows it displays .NET's WinForms
	/// FolderBrowserDialog, on Linux the GTK FileChooserDialog (limited to selecting folders).
	/// </summary>
	public class FolderBrowserDialogAdapter: IFolderBrowserDialog, IDisposable
	{
		/// <summary/>
		protected IFolderBrowserDialog m_dlg;

		/// <summary/>
		public FolderBrowserDialogAdapter()
		{
			m_dlg = Manager.CreateFolderBrowserDialog();
		}

		#region IFolderBrowserDialog implementation
		/// <summary/>
		public event EventHandler Disposed
		{
			add { m_dlg.Disposed += value; }
			remove { m_dlg.Disposed -= value; }
		}

		/// <summary/>
		public void Reset()
		{
			m_dlg.Reset();
		}

		/// <summary/>
		public DialogResult ShowDialog()
		{
			return m_dlg.ShowDialog();
		}

		/// <summary/>
		public DialogResult ShowDialog(IWin32Window owner)
		{
			return m_dlg.ShowDialog(owner);
		}

		/// <summary/>
		public string Description
		{
			get { return m_dlg.Description; }
			set { m_dlg.Description = value; }
		}

		/// <summary/>
		public Environment.SpecialFolder RootFolder
		{
			get { return m_dlg.RootFolder; }
			set { m_dlg.RootFolder = value; }
		}

		/// <summary/>
		public string SelectedPath
		{
			get { return m_dlg.SelectedPath; }
			set { m_dlg.SelectedPath = value; }
		}

		/// <summary/>
		public bool ShowNewFolderButton
		{
			get { return m_dlg.ShowNewFolderButton; }
			set { m_dlg.ShowNewFolderButton = value; }
		}

		/// <summary/>
		public object Tag
		{
			get { return m_dlg.Tag; }
			set { m_dlg.Tag = value; }
		}
		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~FolderBrowserDialogAdapter()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

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
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_dlg as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_dlg = null;
			IsDisposed = true;
		}
		#endregion
	}
}
