// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DetailControlsMainWnd.cs
// Responsibility: RegnierR

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	#region Main window
	/// <summary>
	/// Summary description for DetailControlsMainWnd.
	/// </summary>
	public class DetailControlsMainWnd : Form, IFWDisposable
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
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

			m_dataEntryForm.ShowObject(null, "default", null, null, false);
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
			this.AutoScaleMode = AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(504, 422);
			this.Name = "DetailControlsMainWnd";
			this.Text = "DetailControlsMainWnd";

		}
		#endregion

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
			string partDirectory = Path.Combine(FwDirectoryFinder.CodeDirectory,
				@"Language Explorer\Configuration\Parts");
			Dictionary<string, string[]> keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["layout"] = new string[] {"class", "type", "mode", "name" };
			keyAttrs["group"] = new string[] {"label"};
			keyAttrs["part"] = new string[] {"ref"};


			Inventory layouts = new Inventory(new string[] {partDirectory},
				"*Parts.xml", "/layoutInventory/*", keyAttrs, "DetailControlsMainWnd", "ProjectPath");

			keyAttrs = new Dictionary<string, string[]>();
			keyAttrs["part"] = new string[] {"id", "type", "mode"};

			Inventory parts = new Inventory(new string[] {partDirectory},
				"*.fwlayout", "/PartInventory/*", keyAttrs, "DetailControlsMainWnd", "ProjectPath");
			m_dataEntryForm.Initialize(m_cache, true, layouts, parts);
			m_dataEntryForm.Dock = System.Windows.Forms.DockStyle.Fill;
			Controls.Add(m_dataEntryForm);
			ResumeLayout(false);
		}
	}
	#endregion // Main window

	#region Tests

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the Datatree control.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	//[TestFixture]
	public class DataTreeTestsLive: SIL.FieldWorks.Test.TestUtils.BaseTest
	{
		DetailControlsMainWnd m_mainWnd;

		#region Setup, Teardown

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up an initial transaction and undo item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		//[SetUp]
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
		//[TearDown]
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
		//[Test]
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
