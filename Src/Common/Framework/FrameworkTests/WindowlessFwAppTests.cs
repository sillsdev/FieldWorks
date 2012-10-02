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
// File: WindowlessFwAppTests.cs
// Responsibility: TomB
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Data.SqlClient;
using System.Diagnostics;

using NUnit.Framework;

using NMock;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.UIAdapters;
namespace SIL.FieldWorks.Common.Framework
{
	#region InvisibleFwMainWnd
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy override of FwMainWnd that never gets shown
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InvisibleFwMainWnd : FwMainWnd
	{
		#region data members
		/// <summary>Mocked Editing Helper</summary>
		public DynamicMock m_mockedEditingHelper = new DynamicMock(typeof(EditingHelper));
		/// <summary>Set this to true to test condition where there's no editing helper</summary>
		public bool m_fSimulateNoEditingHelper;
		#endregion

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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mockedEditingHelper = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the mocked editing helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
				CheckDisposed();

				if (m_fSimulateNoEditingHelper)
					return null;
				return (EditingHelper)m_mockedEditingHelper.MockInstance;
			}
		}
	}
	#endregion

	#region WindowlessDummyFwApp class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Derive our own FwApp to allow access to certain methods for testing purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class WindowlessDummyFwApp : DummyFwApp
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new instance of WindowlessDummyFwApp.
		/// </summary>
		/// <param name="rgArgs"></param>
		/// ------------------------------------------------------------------------------------
		public WindowlessDummyFwApp(string[] rgArgs)
			: base(rgArgs)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The HTML help file (.chm) for the app.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string HelpFile
		{
			get
			{
				CheckDisposed();
				return string.Empty;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Bogus implementation
		/// </summary>
		///
		/// <param name="cache">Instance of the FW Data Objects cache that the new main window
		/// will use for accessing the database.</param>
		/// <param name="fNewCache">Flag indicating whether one-time, application-specific
		/// initialization should be done for this cache.</param>
		/// <param name="wndCopyFrom"> Must be null for creating the original app window.
		/// Otherwise, a reference to the main window whose settings we are copying.</param>
		/// <param name="fOpeningNewProject"><c>true</c> if opening a brand spankin' new
		/// project</param>
		/// <returns>New instance of DummyFwMainWnd</returns>
		/// -----------------------------------------------------------------------------------
		protected override Form NewMainAppWnd(FdoCache cache, bool fNewCache, Form wndCopyFrom,
			bool fOpeningNewProject)
		{
			throw new Exception("NewMainAppWnd Not implemented");
		}
	}
	#endregion

	#region WindowslessFwAppTests class
	/// <summary>
	/// Summary description for WindowlessFwAppTests.
	/// </summary>
	[TestFixture]
	public class WindowlessFwAppTests : BaseTest
	{
		private readonly string m_sSvrName = MiscUtils.LocalServerName;
		private readonly string m_sDbName = "TestLangProj";
		private DummyFwApp m_DummyFwApp;
		private InvisibleFwMainWnd m_mainWnd;
		private readonly TMItemProperties m_itemProps = new TMItemProperties();

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="WindowlessFwAppTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public WindowlessFwAppTests()
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
			Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_DummyFwApp != null)
					m_DummyFwApp.Dispose();
				if (m_mainWnd != null)
					m_mainWnd.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_DummyFwApp = null;
			m_mainWnd = null;
			m_itemProps.ParentForm = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_DummyFwApp = new WindowlessDummyFwApp(new string[] { "-c", m_sSvrName, "-db", m_sDbName });

			m_mainWnd = new InvisibleFwMainWnd();
			m_itemProps.ParentForm = m_mainWnd;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();

			m_mainWnd.m_fSimulateNoEditingHelper = false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure GetDbVersion works correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetDbVersionTest()
		{
			CheckDisposed();

			m_DummyFwApp.m_internalDbVersion = -1;

			Assert.AreEqual((int)DbVersion.kdbAppVersion,
				m_DummyFwApp.GetDbVersion(m_sSvrName, m_sDbName), "Test with explicit server name");
			Assert.AreEqual((int)DbVersion.kdbAppVersion,
				m_DummyFwApp.GetDbVersion(string.Empty, m_sDbName), "Test with default (local) server");

			// Make sure version number 3 is translated to 5000
			m_DummyFwApp.m_internalDbVersion = 3;
			Assert.AreEqual(5000, m_DummyFwApp.GetDbVersion(m_sSvrName, m_sDbName));

			// Set the version to a random number between 5001 and 1000000.
			Random r = new Random();
			int n = r.Next(5001, 1000000);
			m_DummyFwApp.m_internalDbVersion = n;
			Assert.AreEqual(n, m_DummyFwApp.GetDbVersion(m_sSvrName, m_sDbName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure CheckDbVerCompatibility_Bkupd works correctly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckDbVerCompatibilityTest()
		{
			CheckDisposed();

			m_DummyFwApp.m_internalDbVersion = -1;

			Assert.IsTrue(m_DummyFwApp.CheckDbVerCompatibility_Bkupd(m_sSvrName, m_sDbName),
				"Test with explicit server name");
			Assert.IsTrue(m_DummyFwApp.CheckDbVerCompatibility_Bkupd(string.Empty, m_sDbName),
				"Test with default (local) server");

			m_DummyFwApp.m_internalDbVersion = 3;
			Assert.IsFalse(m_DummyFwApp.CheckDbVerCompatibility_Bkupd(m_sSvrName, m_sDbName),
				"Test having made version number too low (3)");

			m_DummyFwApp.m_internalDbVersion = 5000;
			Assert.IsFalse(m_DummyFwApp.CheckDbVerCompatibility_Bkupd(m_sSvrName, m_sDbName),
				"Test having made version number too low (5000)");

			// Let's make the database version number bigger than the current version
			m_DummyFwApp.m_internalDbVersion = (int)DbVersion.kdbAppVersion + 1;
			// CheckDbVerCompatibility_Bkupd should now return false and a message box saying
			// the database is newer than the application
			Assert.IsFalse(m_DummyFwApp.CheckDbVerCompatibility_Bkupd(m_sSvrName, m_sDbName),
				"Test having made version number greater than the current version");

			m_DummyFwApp.m_internalDbVersion = 100001;
			// CheckDbVerCompatibility_Bkupd should now return false and a message box saying
			// you can't upgrade
			Assert.IsFalse(m_DummyFwApp.CheckDbVerCompatibility_Bkupd(m_sSvrName, m_sDbName),
				"Test having made version number too low (100001)");

			// Let's make the database version number an even thousand lower than the current
			// version
			int evenThousand = ((int)DbVersion.kdbAppVersion / 1000) * 1000 - 1000;
			m_DummyFwApp.m_internalDbVersion = evenThousand;
			// CheckDbVerCompatibility_Bkupd should show a message box asking if the user wishes
			// to upgrade.  If the user clicks yes and the upgrade is successful then returns
			// true, false in all other cases.
			Assert.IsFalse(m_DummyFwApp.CheckDbVerCompatibility_Bkupd(m_sSvrName, m_sDbName),
				"Test having made version number to an upgradable version");
		}

		#region tests for enabling/disabling Format Apply Style menu
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is enabled when there is an active editing
		/// helper and a current selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_EnabledWhenCurrentSelection()
		{
			CheckDisposed();

			this.m_mainWnd.m_fSimulateNoEditingHelper = false;
			DynamicMock mockedSelectionHelper = new DynamicMock(typeof(SelectionHelper));
			m_mainWnd.m_mockedEditingHelper.ExpectAndReturn("CurrentSelection",
				mockedSelectionHelper.MockInstance);
			DynamicMock mockedSelection = new DynamicMock(typeof(IVwSelection));
			mockedSelectionHelper.ExpectAndReturn("Selection", mockedSelection.MockInstance);
			mockedSelection.ExpectAndReturn("IsEditable", true);
			mockedSelection.SetupResult("CanFormatChar", true);
			mockedSelection.SetupResult("CanFormatPara", true);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsTrue(m_itemProps.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is no active editing helper
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenNoEditingHelper()
		{
			CheckDisposed();

			this.m_mainWnd.m_fSimulateNoEditingHelper = true;

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is no current selection
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenNoSelection()
		{
			CheckDisposed();

			this.m_mainWnd.m_fSimulateNoEditingHelper = false;
			m_mainWnd.m_mockedEditingHelper.SetupResult("CurrentSelection", null);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is an active editing
		/// helper and a current selection but the current selection doesn't allow formatting
		/// of either paragraph or character styles.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenNeitherParaNorCharCanBeFormatted()
		{
			CheckDisposed();

			this.m_mainWnd.m_fSimulateNoEditingHelper = false;
			DynamicMock mockedSelectionHelper = new DynamicMock(typeof(SelectionHelper));
			m_mainWnd.m_mockedEditingHelper.ExpectAndReturn("CurrentSelection",
				mockedSelectionHelper.MockInstance);
			DynamicMock mockedSelection = new DynamicMock(typeof(IVwSelection));
			mockedSelectionHelper.ExpectAndReturn("Selection", mockedSelection.MockInstance);
			mockedSelection.ExpectAndReturn("IsEditable", true);
			mockedSelection.SetupResult("CanFormatPara", false);
			mockedSelection.SetupResult("CanFormatChar", false);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the Format Apply Style menu is disabled when there is an active editing
		/// helper and a current selection but the current selection isn't editable.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FormatApplyStyle_DisabledWhenSelectionIsUneditable()
		{
			CheckDisposed();

			this.m_mainWnd.m_fSimulateNoEditingHelper = false;
			DynamicMock mockedSelectionHelper = new DynamicMock(typeof(SelectionHelper));
			m_mainWnd.m_mockedEditingHelper.ExpectAndReturn("CurrentSelection",
				mockedSelectionHelper.MockInstance);
			DynamicMock mockedSelection = new DynamicMock(typeof(IVwSelection));
			mockedSelectionHelper.ExpectAndReturn("Selection", mockedSelection.MockInstance);
			mockedSelection.ExpectAndReturn("IsEditable", false);

			Assert.IsTrue(m_mainWnd.OnUpdateFormatApplyStyle(m_itemProps));
			Assert.IsFalse(m_itemProps.Enabled);
		}
		#endregion
	}
	#endregion
}
