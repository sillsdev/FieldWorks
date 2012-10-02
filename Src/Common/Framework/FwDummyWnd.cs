// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwDummyWnd.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// This wraps a dummy Window in a form that FwApp can swallow as a main window.  The only use
// of this window is to provide a message handler for dealing with operations such as Restore
// that may delete all the existing main windows as part of their processing.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;
using Microsoft.Win32;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Installing FwDummyWnd will set FwApp.App.SuppressCloseApp = true in order to prevent
	/// the application from exiting during radical database operations (such as backup/restore).
	/// </summary>
	public class FwDummyWnd : Form, IFWDisposable, IFwMainWnd
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		private System.Windows.Forms.Label label1;

		/// <summary>
		/// We need a dummy cache that is not attached to any particular database.
		/// </summary>
		private FdoCache m_cache = new FdoCache();

		private bool m_fCanDie = false;

		/// <summary>
		///
		/// </summary>
		public FwDummyWnd()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
			//
			// Additional constructor code.
			//
			this.Visible = false;

			m_fCanDie = false;
			FwApp.App.SuppressCloseApp = true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="title">title for the window, null for default</param>
		/// <param name="message">message for the window, null for default</param>
		public FwDummyWnd(string title, string message)
			: this()
		{
			if (title != null)
				this.Text = title;
			if (message != null)
				label1.Text = message;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}


		/// <summary>
		/// when closing this window, we want to set FwApp.App.SuppressCloseApp = false in order to allow exiting the app.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnClosing(CancelEventArgs e)
		{
			base.OnClosing(e);
			m_fCanDie = true;
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (!m_fCanDie)
				return;
			if (disposing)
			{
				if (components != null)
					components.Dispose();
				if (FwApp.App != null)
				{
					FwApp.App.SuppressCloseApp = false;
					FwApp.App.OkToCloseApp = false;
					FwApp.App.RemoveWindow(this);
					FwApp.App.OkToCloseApp = true;
				}
			}

			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FwDummyWnd));
			this.label1 = new System.Windows.Forms.Label();
			this.SuspendLayout();
			//
			// label1
			//
			resources.ApplyResources(this.label1, "label1");
			this.label1.Name = "label1";
			//
			// FwDummyWnd
			//
			resources.ApplyResources(this, "$this");
			this.ControlBox = false;
			this.Controls.Add(this.label1);
			this.Cursor = System.Windows.Forms.Cursors.WaitCursor;
			this.Name = "FwDummyWnd";
			this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
			this.TopMost = true;
			this.Load += new System.EventHandler(this.FwXDummyWnd_Load);
			this.ResumeLayout(false);

		}
		#endregion

		private void FwXDummyWnd_Load(object sender, System.EventArgs e)
		{

		}

		/*
		 * (EricP) not sure why it was necessary to do the backup in our own WndProc handling.
		 * As far as I can tell, all we need to do is make sure we have a dummy window
		 * installed to ensure we don't exit the application while closing all the other windows.

		/// <summary>
		/// Registers a custom message for handling backup and restore dialog
		/// </summary>
		protected static uint s_wm_kmBackupRestore =
			Win32.RegisterWindowMessage("WM_KMBACKUPRESTORE");

		/// <summary>
		/// a custom message for handling backup and restore dialog
		/// </summary>
		public static uint WM_BackupRestore
		{
			get
			{
				return s_wm_kmBackupRestore;
			}
		}

		/// <summary>
		/// This is the critical piece for the dummy window: an event loop processor for
		/// handling commands on behalf of the application that may destroy all existing
		/// main windows.
		/// </summary>
		/// <param name="m"></param>
		protected override void WndProc(ref Message m)
		{
			Debug.WriteLine("Dummy.WndProc(Msg = " + m.Msg + ",  LParam = " + m.LParam +
				", WParam = " + m.WParam + ")");
			if (m.Msg == s_wm_kmBackupRestore)
			{
				m_fCanDie = false;
				FwApp.App.SuppressCloseApp = true;
				try
				{
					DIFwBackupDb backupSystem = FwBackupClass.Create();
					backupSystem.Init(FwApp.App, Handle.ToInt32());
					backupSystem.UserConfigure(FwApp.App, false);
					backupSystem.Close();
				}
				finally
				{
					FwApp.App.SuppressCloseApp = false;
				}
				// now, remove the invisible dummy window.
				m_fCanDie = true;
				//this.Close();
				Win32.PostMessage(Handle, 16, 0, 0);		// WM_CLOSE
				return;
			}
			base.WndProc(ref m);
		}
		 */

		#region IFwMainWnd implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshAllViews()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window is finished being created and completely initialized.
		/// </summary>
		/// <returns>True if successful; false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool OnFinishedInit()
		{
			CheckDisposed();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual string ApplicationName
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the data objects cache.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return m_cache;		// dummy cache, but needed elsewhere.
			}
			set
			{
				CheckDisposed();

				// Shouldn't reset the cache.
				Debug.Assert(false);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Returns the NormalStateDesktopBounds property from the persistence object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public Rectangle NormalStateDesktopBounds
		{
			get
			{
				CheckDisposed();

				System.Drawing.Point loc = new System.Drawing.Point(0, 0);
				System.Drawing.Size size = new System.Drawing.Size(400,400);
				return new Rectangle(loc, size);
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add corresponding stuff to the sidebar, View menu,
		/// etc.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public virtual void InitAndShowClient()
		{
			CheckDisposed();

		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Enable or disable this window.
		/// </summary>
		///
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		/// -----------------------------------------------------------------------------------
		public void EnableWindow(bool fEnable)
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save all data in this window, ending the current transaction.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SaveData()
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called just before a window syncronizes it's views with DB changes (e.g. when an
		/// undo or redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization information record</param>
		/// ------------------------------------------------------------------------------------
		public virtual bool PreSynchronize(SyncInfo sync)
		{
			CheckDisposed();

			return true;
		}

		/// <summary>
		/// If a property requests it, do a db sync.
		/// </summary>
		public virtual void OnIdle(object sender)
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization information record</param>
		/// <returns>True if the sync message was handled; false, indicating that the
		/// application should refresh all windows. </returns>
		/// ------------------------------------------------------------------------------------
		public virtual bool Synchronize(SyncInfo sync)
		{
			CheckDisposed();

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the currently active view (client window).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IRootSite ActiveView
		{
			get
			{
				CheckDisposed();
				return (IRootSite)null;
			}
		}
		#endregion // IFwMainWnd implementation
	}
}
