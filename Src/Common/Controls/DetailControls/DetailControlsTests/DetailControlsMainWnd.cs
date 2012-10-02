// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DetailControlsMainWnd.cs
// Responsibility: RegnierR
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	#region Main window
	/// <summary>
	/// Summary description for DetailControlsMainWnd.
	/// </summary>
	public class DetailControlsMainWnd : Form, IFWDisposable, IFwMainWnd
	{
		private FdoCache m_cache;
		private DataTree m_dataEntryForm;

		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DetailControlsMainWnd"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DetailControlsMainWnd()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();

			m_cache = FdoCache.Create("TestLangProj");
			InitAndShowClient();
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing"><c>true</c> to release both managed and unmanaged
		/// resources; <c>false</c> to release only unmanaged resources.
		/// </param>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_dataEntryForm != null)
				{
					Controls.Remove(m_dataEntryForm);
					m_dataEntryForm.Dispose();
				}
				if (m_cache != null)
				{
					m_cache.Dispose();
				}
				if(components != null)
				{
					components.Dispose();
				}
			}
			m_dataEntryForm = null;
			m_cache = null;

			base.Dispose(disposing);
		}

		public void ShowObject()
		{
			CheckDisposed();

			int hvoRoot = 0;
			m_dataEntryForm.ShowObject(hvoRoot, "default");
		}

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			//
			// DetailControlsMainWnd
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(504, 422);
			this.Name = "DetailControlsMainWnd";
			this.Text = "DetailControlsMainWnd";

		}
		#endregion

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
		/// Gets the currently active view (client window).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IRootSite ActiveView
		{
			get
			{
				CheckDisposed();

				// TODO WW team: implement this if needed (see FwMainWnd.ActiveView for example)
				return null;
			}
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
		public string ApplicationName
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the data objects cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();
				return m_cache;
			}
			set
			{
				CheckDisposed();

				Debug.Assert(value != null);
				if (m_cache != null)
					m_cache.Dispose();
				m_cache = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the client windows and add correspnding stuff to the sidebar, View menu,
		/// etc. Subclasses must override this.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitAndShowClient()
		{
			CheckDisposed();

			m_dataEntryForm = new DataTree();
			SuspendLayout();
			string partDirectory = Path.Combine(SIL.FieldWorks.Common.Utils.DirectoryFinder.FWCodeDirectory,
				@"Language Explorer\Configuration\Parts");
			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new string[] {"class", "type", "mode", "name" };
			keyAttrs["group"] = new string[] {"label"};
			keyAttrs["part"] = new string[] {"ref"};


			Inventory layouts = new Inventory(new string[] {partDirectory},
				"*Parts.xml", "/layoutInventory/*", keyAttrs);

			keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new string[] {"id", "type", "mode"};

			Inventory parts = new Inventory(new string[] {partDirectory},
				"*Layouts.xml", "/PartInventory/*", keyAttrs);
			m_dataEntryForm.Initialize(m_cache, true, layouts, parts);
			m_dataEntryForm.Dock = System.Windows.Forms.DockStyle.Fill;
			Controls.Add(m_dataEntryForm);
			ResumeLayout(false);
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

				return new Rectangle(Location, Size);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Enable or disable this window.
		/// </summary>
		/// <param name="fEnable">Enable (true) or disable (false).</param>
		/// ------------------------------------------------------------------------------------
		public void EnableWindow(bool fEnable)
		{
			CheckDisposed();

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Save all data in this window.
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
		public bool PreSynchronize(SyncInfo sync)
		{
			CheckDisposed();

			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a window syncronizes it's views with DB changes (e.g. when an undo or
		/// redo command is issued).
		/// </summary>
		/// <param name="sync">syncronization information record</param>
		/// ------------------------------------------------------------------------------------
		public bool Synchronize(SyncInfo sync)
		{
			CheckDisposed();

			return true;
		}
		#endregion
	}
	#endregion // Main window

	#region Tests

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the Datatree control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	//[TestFixture]
	public class DataTreeTestsLive
	{
		DetailControlsMainWnd m_mainWnd;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DataTreeTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DataTreeTestsLive()
		{
		}

		#region Setup, Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up an initial transaction and undo item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void SetUp()
		{
			m_mainWnd = new DetailControlsMainWnd();
			m_mainWnd.Show();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo all DB changes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void TearDown()
		{
			m_mainWnd.Hide();
			m_mainWnd.Dispose();
		}
		#endregion


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check how many templates are in the default system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
			// Note: It appears to only fire the assert, when the GUI for NUnit is used, but
			// not while testing from NAnt.
			//[Ignore("This causes an assert to fire down in 'TsTextProps::~TsTextProps'.")]
			//[Ignore("SteveMc: This causes an assert in VwPropertyStore::put_IntProperty() due to an invalied ws value (1)")]
		public void ShowWindow()
		{
			m_mainWnd.ShowObject();
		}
	}

	#endregion // Tests
}
