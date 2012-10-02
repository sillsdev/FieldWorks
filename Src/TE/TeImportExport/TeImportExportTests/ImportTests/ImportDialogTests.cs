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
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SILUBS.SharedScrUtils;

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
			ScrImportSet settings) :
			base(styleSheet, cache, settings, string.Empty)
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
	public class ImportDialogTests: ScrInMemoryFdoTestBase
	{
		#region Member Data
		private RegistryData m_regData;
		private DummyImportDialog m_dlg;
		private ScrImportSet m_settings;
		#endregion

		#region IDisposable override
		/// ------------------------------------------------------------------------------------
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
		/// ------------------------------------------------------------------------------------
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_dlg != null)
					m_dlg.Dispose();
				if (m_regData != null)
				{
					m_regData.RestoreRegistryData();
				}
				Unpacker.RemoveParatextTestProjects();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_dlg = null;
			m_regData = null;
			m_settings = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		#region Setup/Teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create temp registry settings and versification files
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
		}

		#region CreateTestData
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates test data for a Paratext 6 import.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_scrInMemoryCache.InitializeScripture();
			m_scrInMemoryCache.InitializeAnnotationDefs();
			m_settings = new ScrImportSet();
			Cache.LangProject.TranslatedScriptureOA.ImportSettingsOC.Add(m_settings);
			m_settings.ImportTypeEnum = TypeOfImport.Paratext6;
			m_settings.ParatextScrProj = "TEV";
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_dlg = new DummyImportDialog(null, Cache, m_settings);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			try
			{
				m_dlg.Close();
				m_dlg = null;
				m_settings = null;
			}
			finally
			{
				base.Exit();
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
			CheckDisposed();

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
		public void CheckScrPsgRefPersistence()
		{
			CheckDisposed();

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
		public void CheckScrPsgRanges_FromCtrl()
		{
			CheckDisposed();

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
		public void CheckScrPsgRanges_ToCtrl()
		{
			CheckDisposed();

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
