// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ImportDialogTests.cs
// --------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using SILUBS.SharedScrControls;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE.ImportTests
{
	#region DummyImportDialog
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyImportDialog : ImportDialog
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyImportDialog"/> class.
		/// </summary>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="settings">The settings.</param>
		/// ------------------------------------------------------------------------------------
		public DummyImportDialog(FwStyleSheet styleSheet, FdoCache cache,
			IScrImportSet settings) : base(styleSheet, cache, settings, null, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ScrPassageControl FromRefCtrl
		{
			get
			{
				CheckDisposed();
				return scrPsgFrom;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal ScrPassageControl ToRefCtrl
		{
			get
			{
				CheckDisposed();
				return scrPsgTo;
			}
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ImportDialogTests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ImportDialogTests: ScrInMemoryFdoTestBase
	{
		#region Member Data
		private DummyImportDialog m_dlg;
		private IScrImportSet m_settings;
		private MockFileOS m_mockFileOs;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add import settings for the import dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_mockFileOs = new MockFileOS();
			FileUtils.Manager.SetFileAdapter(m_mockFileOs);
			string fileGen = m_mockFileOs.MakeSfFile("GEN", @"\c 1");
			string fileRev = m_mockFileOs.MakeSfFile("REV", @"\c 1");
			m_settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Other);
			m_settings.AddFile(fileGen, ImportDomain.Main, null, null);
			m_settings.AddFile(fileRev, ImportDomain.Main, null, null);
			m_dlg = new DummyImportDialog(null, Cache, m_settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			try
			{
				m_dlg.Dispose();
				m_dlg = null;
				m_settings = null;
			}
			finally
			{
				base.TestTearDown();
			}
		}

		#endregion

		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure the reference range includes the whole cannon when importing entire
		/// scripture project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CheckEntireProjReferences()
		{
			m_dlg.ImportEntireProject = true;
			Assert.AreEqual("GEN 1:1", m_dlg.StartRef.AsString);

			Assert.AreEqual("REV 1:1", m_dlg.EndRef.AsString);
			// Use following assert when we support partial import of books.
			// Assert.AreEqual("REV 22:21", m_dlg.EndRef.AsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setting and persistence of range references
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency: ScrFDO:ScrImportSet in BooksForProject property called via new DummyImportDialog(null, Cache, m_settings) in Initialize()")]
		public void CheckScrPsgRefPersistence()
		{
			m_dlg.ImportEntireProject = false;

			m_dlg.StartRef = new BCVRef(2, 8, 20);
			m_dlg.EndRef = new BCVRef(8, 2, 4);

			m_dlg.Show();

			try
			{
				Assert.AreEqual("EXO 8:20", m_dlg.StartRef.AsString);
				Assert.AreEqual("RUT 2:4", m_dlg.EndRef.AsString);

				// This needs to happen to force the current control content to be saved in
				// static variables.
				m_dlg.DialogResult = System.Windows.Forms.DialogResult.OK;
			}
			finally
			{
				if (m_dlg != null && m_dlg.Visible)
					m_dlg.Close();
			}

			// Now dispose of the dialog and reinstantiate it. The reinstantiated version
			// should still contain the Exodus and Ruth references.
			m_dlg = new DummyImportDialog(null, Cache, m_settings);
			m_dlg.ImportEntireProject = false;

			Assert.AreEqual("EXO 1:1", m_dlg.StartRef.AsString);
			Assert.AreEqual("RUT 1:1", m_dlg.EndRef.AsString);

			// If we ever support partial-book imports, use these assertions instead:
			//Assert.AreEqual("EXO 8:20", m_dlg.StartRef.AsString);
			//Assert.AreEqual("RUT 2:4", m_dlg.EndRef.AsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency: ScrFDO:ScrImportSet in BooksForProject property called via new DummyImportDialog(null, Cache, m_settings) in Initialize()")]
		[Category("DesktopRequired")]
		public void CheckScrPsgRanges_FromCtrl()
		{
			m_dlg.Show();
			m_dlg.ImportEntireProject = false;

			m_dlg.StartRef = new BCVRef(1, 1, 1);
			m_dlg.EndRef = new BCVRef(1, 1, 1);

			Assert.AreEqual("GEN 1:1", m_dlg.FromRefCtrl.ScReference.AsString);
			Assert.AreEqual("GEN 1:1", m_dlg.ToRefCtrl.ScReference.AsString);

			// Change the From ref. to MAT 2:5
			m_dlg.FromRefCtrl.Focus();
			m_dlg.FromRefCtrl.Reference = "REV 2:5";
			m_dlg.ToRefCtrl.Focus();

			Assert.AreEqual(66, m_dlg.FromRefCtrl.ScReference.Book);
			Assert.AreEqual(66, m_dlg.ToRefCtrl.ScReference.Book);

			// Use following asserts when we support partial import of books.
//			Assert.AreEqual("MAT 2:5", m_dlg.FromRefCtrl.ScReference.AsString);
//			Assert.AreEqual("MAT 28:20", m_dlg.ToRefCtrl.ScReference.AsString);
//			Assert.AreEqual("MAT 2:5", m_dlg.StartRef.AsString);
//			Assert.AreEqual("MAT 28:20", m_dlg.EndRef.AsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency: ScrFDO:ScrImportSet in BooksForProject property called via new DummyImportDialog(null, Cache, m_settings) in Initialize()")]
		[Category("DesktopRequired")]
		public void CheckScrPsgRanges_ToCtrl()
		{
			m_dlg.Show();
			m_dlg.ImportEntireProject = false;

			m_dlg.StartRef = new BCVRef(40, 1, 1);
			m_dlg.EndRef = new BCVRef(40, 1, 1);

			Assert.AreEqual("MAT 1:1", m_dlg.FromRefCtrl.ScReference.AsString);
			Assert.AreEqual("MAT 1:1", m_dlg.ToRefCtrl.ScReference.AsString);

			// Change the From ref. to GEN 5:2
			m_dlg.ToRefCtrl.Focus();
			m_dlg.ToRefCtrl.Reference = "GEN 5:2";
			m_dlg.FromRefCtrl.Focus();

			Assert.AreEqual(1, m_dlg.FromRefCtrl.ScReference.Book);
			Assert.AreEqual(1, m_dlg.ToRefCtrl.ScReference.Book);

			// Use following asserts when we support partial import of books.
//			Assert.AreEqual("GEN 5:2", m_dlg.FromRefCtrl.ScReference.AsString);
//			Assert.AreEqual("GEN 5:2", m_dlg.ToRefCtrl.ScReference.AsString);
//			Assert.AreEqual("GEN 5:2", m_dlg.StartRef.AsString);
//			Assert.AreEqual("GEN 5:2", m_dlg.EndRef.AsString);
		}
		#endregion
	}
}
