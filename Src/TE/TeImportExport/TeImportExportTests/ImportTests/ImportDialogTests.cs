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
// File: ImportDialogTests.cs
// Responsibility: _Aman
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
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
				return this.scrPsgFrom;
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
				return this.scrPsgTo;
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
	[Platform(Exclude = "Linux", Reason = "TODO-Linux: Paratext6 Dependency in TestSetup")]
	public class ImportDialogTests: ScrInMemoryFdoTestBase
	{
		#region Member Data
		private RegistryData m_regData;
		private DummyImportDialog m_dlg;
		private IScrImportSet m_settings;
		#endregion

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create temp registry settings and versification files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create temp registry settings and versification files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureTeardown()
		{
			if (m_regData != null)
			{
				m_regData.RestoreRegistryData();
			}
			Unpacker.RemoveParatextTestProjects();
			base.FixtureTeardown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add import settings for the import dialog.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();

			m_settings = m_scr.FindOrCreateDefaultImportSettings(TypeOfImport.Paratext6);
			m_settings.ParatextScrProj = "TEV";
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

			Assert.AreEqual("EXO 8:20", m_dlg.StartRef.AsString);
			Assert.AreEqual("RUT 2:4", m_dlg.EndRef.AsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency: ScrFDO:ScrImportSet in BooksForProject property called via new DummyImportDialog(null, Cache, m_settings) in Initialize()")]
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
			m_dlg.FromRefCtrl.Reference = "MAT 2:5";
			m_dlg.ToRefCtrl.Focus();

			Assert.AreEqual(40, m_dlg.FromRefCtrl.ScReference.Book);
			Assert.AreEqual(40, m_dlg.ToRefCtrl.ScReference.Book);

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
