// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyFwApp.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
#if WANTTESTPORT // (Common) Need to see if we really still need this. Have to change a lot of stuff related to the app because we're combining TE anf Flex into a single app.
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// FwApp class used for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwApp : FwApp
	{
		#region Data members for testing
		/// <summary></summary>
		public DummyMainWnd[] m_mainWnd = new DummyMainWnd[10];
		/// <summary></summary>
		public int m_nMainWnd = 0;
		/// <summary></summary>
		public int m_nSplashScreenShown = 0;
		/// <summary></summary>
		public int m_nSplashScreenClosed = 0;
		/// <summary></summary>
		public string m_SplashScreenMessages;
		/// <summary></summary>
		public int m_appVersion = (int)DbVersion.kdbAppVersion;
		/// <summary></summary>
		public bool m_oldAppWarningCalled = false;
		/// <summary></summary>
		public bool m_noUpgradeWarningCalled = false;
		/// <summary></summary>
		public bool m_shouldUpgradeDatabaseCalled = false;
		/// <summary>The value to return from GetDbVersion_Internal, or -1 to call the base class</summary>
		public int m_internalDbVersion = -1;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// C'tor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public DummyFwApp() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of WindowlessDummyFwApp.
		/// </summary>
		/// <param name="rgArgs"></param>
		/// ------------------------------------------------------------------------------------
		public DummyFwApp(string[] rgArgs) : base(rgArgs)
		{
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				foreach (DummyMainWnd wnd in m_mainWnd)
				{
					if (wnd != null)
						wnd.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mainWnd = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="fNewCache"></param>
		/// <param name="wndCopyFrom"></param>
		/// <param name="fOpeningNewProject"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override Form NewMainAppWnd(bool fNewCache, Form wndCopyFrom, bool fOpeningNewProject)
		{
			int nMainWnd = m_nMainWnd;
			m_nMainWnd++;
			m_mainWnd[nMainWnd] = new DummyMainWnd();
			return m_mainWnd[nMainWnd];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The name of the sample DB for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string SampleDatabase
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void ShowSplashScreen()
		{
			CheckDisposed();

			m_nSplashScreenShown++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void CloseSplashScreen()
		{
			CheckDisposed();

			m_nSplashScreenClosed++;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="msg"></param>
		/// ------------------------------------------------------------------------------------
		public override void WriteSplashScreen(string msg)
		{
			CheckDisposed();

			if (m_SplashScreenMessages == null)
				m_SplashScreenMessages = msg;
			else
				m_SplashScreenMessages += "\n" + msg;
		}

		#region Special methods for testing
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the FDO cache for testing
		/// </summary>
		/// <param name="serverName"></param>
		/// <param name="dbName"></param>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public void SetFdoCache(string serverName, string dbName, FdoCache cache)
		{
			CheckDisposed();

			m_caches[MakeKey(serverName, dbName)] = cache;
		}

		#endregion
	}

	#region Other Test classes
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy rootsite for testing
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyRootSite : IRootSite
	{
		/// <summary></summary>
		public static DynamicMock s_mockVwRootSite;

		#region IRootSite Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Commit()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the width available for laying things out in the view.
		/// Return the layout width for the window, depending on whether or not there is a
		/// scroll bar. If there is no scroll bar, we pretend that there is, so we don't have
		/// to keep adjusting the width back and forth based on the toggling on and off of
		/// vertical and horizontal scroll bars and their interaction.
		/// The return result is in pixels.
		/// The only common reason to override this is to answer instead a very large integer,
		/// which has the effect of turning off line wrap, as everything apparently fits on
		/// a line.
		/// </summary>
		/// <param name="prootb">The root box</param>
		/// <returns>Width available for layout</returns>
		/// ------------------------------------------------------------------------------------
		public int GetAvailWidth(IVwRootBox prootb)
		{
			return 640;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public IVwRootSite CastAsIVwRootSite()
		{
			return (IVwRootSite) s_mockVwRootSite.MockInstance;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void RefreshDisplay()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CloseRootBox()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public EditingHelper EditingHelper
		{
			get { return null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The list of zero or more internal rootboxes.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public virtual List<IVwRootBox> AllRootBoxes()
		{
			return new List<IVwRootBox>();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the selection in view and set the IP at the given client position.
		/// </summary>
		/// <param name="sel">The selection</param>
		/// <param name="dyPos">Position from top of client window where IP should be set</param>
		/// <returns>True if the selection was scrolled into view, false if this function did
		/// nothing</returns>
		/// ------------------------------------------------------------------------------------
		public bool ScrollSelectionToLocation(IVwSelection sel, int dyPos)
		{
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool AllowPainting
		{
			get { return true; }
			set { }
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy main window
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyMainWnd : Form, IFWDisposable, IFwMainWnd
	{
		#region Data members for testing

		/// <summary></summary>
		public bool m_fInitCalled = false;
		/// <summary></summary>
		public bool m_fClosed = false;
		/// <summary></summary>
		public DummyRootSite m_rootSite;
		private FdoCache m_fdoCache;

		#endregion

		#region IDisposable override

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
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fdoCache = null; // Client will dispose it.
			m_rootSite = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		#region IFwMainWnd Members
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
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite ActiveView
		{
			get
			{
				CheckDisposed();

				if (m_rootSite == null)
					m_rootSite = new DummyRootSite();
				return m_rootSite;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public System.Drawing.Rectangle NormalStateDesktopBounds
		{
			get
			{
				CheckDisposed();
				return new System.Drawing.Rectangle();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ApplicationName
		{
			get
			{
				CheckDisposed();
				return null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitAndShowClient()
		{
			CheckDisposed();

			m_fInitCalled = true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="sync"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public void PreSynchronize(SyncMsg sync)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
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
		/// <param name="sync"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool Synchronize(SyncMsg sync)
		{
			CheckDisposed();

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return m_fdoCache;
			}
			set
			{
				CheckDisposed();

				m_fdoCache = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="fEnable"></param>
		/// ------------------------------------------------------------------------------------
		public void EnableWindow(bool fEnable)
		{
			CheckDisposed();

		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosed(EventArgs e)
		{
			base.OnClosed (e);
			m_fClosed = true;
		}
	}
	#endregion
}
#endif
