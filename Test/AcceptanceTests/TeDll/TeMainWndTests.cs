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
// File: TeMainWndTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Drawing;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
//using SIL.FieldWorks.FDO.Cellar;
//using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
//using SIL.FieldWorks.ScrImportComponents;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.TE;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.AcceptanceTests.TE
{
#if WANTPORT // (TE) These tests create the whole app and need a real DB to load. We need to figure out what to do about them

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Acceptance tests for TE Main Window.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TeMainWndTests : BaseTest
	{
		private readonly string m_sSvrName = Environment.MachineName + "\\SILFW";
		private const string m_sDbName = "testlangproj";
		private const string m_ProjName = "DEB-Debug";
		private RegistryData m_regData;
		private TestTeApp m_testTeApp;
		private TestTeDraftView m_firstDraftView = null;
		private TestTeMainWnd m_firstMainWnd = null;
		private bool m_fMainWindowOpened = false;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeMainWndTests"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public TeMainWndTests()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Instantiate a TeApp object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();

			// TeApp derives from FwApp
			m_testTeApp = new TestTeApp(new string[] {
														 "-c", m_sSvrName,			// ComputerName (aka the SQL server)
														 "-proj", m_ProjName,		// ProjectName
														 "-db", m_sDbName});			// DatabaseName

			m_fMainWindowOpened = m_testTeApp.OpenMainWindow();
		}

		/// <summary>
		/// Correct way to deal with FixtureTearDown for class that derive from BaseTest.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
				return;

			if (disposing)
			{
				if (m_testTeApp != null)
				{
					m_testTeApp.ExitAppplication();
					m_testTeApp.Dispose();
				}

				if (m_regData != null)
					m_regData.RestoreRegistryData();

				Unpacker.RemoveParatextTestProjects();
			}
			m_testTeApp = null;

			base.Dispose(disposing);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			CheckDisposed();
			if (m_fMainWindowOpened)
			{
				m_firstMainWnd = (TestTeMainWnd)m_testTeApp.MainWindows[0];
				// reload the styles from the database (test may have inserted new styles!)
				m_firstMainWnd.Synchronize(new SyncInfo(SyncMsg.ksyncStyle, 0, 0));
				// Set the view to the DraftView
				m_firstMainWnd.SelectScriptureDraftView();
				Application.DoEvents();

				// insert book tests create filters - turn them off to prevent interaction
				// between tests
				m_firstMainWnd.TurnOffAllFilters();

				m_firstDraftView = (TestTeDraftView)m_firstMainWnd.TheDraftView;
				m_firstDraftView.ActivateView();

				SelectionHelper helper = m_firstDraftView.SetInsertionPoint(0, 0, 0, 0, true);
				// helper.IhvoEndPara = -1;
				helper.SetSelection(m_firstDraftView, true, true);

				// we don't want to open a transaction!
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public void CleanUp()
		{
			CheckDisposed();
			if (m_fMainWindowOpened && m_firstMainWnd.Cache != null)
			{
				try
				{
					// Undo everything that we can undo
					while (m_firstMainWnd.Cache.CanUndo)
						m_firstMainWnd.SimulateEditUndoClick();
					m_firstMainWnd.Cache.ActionHandlerAccessor.Commit();
				}
				catch(Exception e)
				{
					System.Diagnostics.Debug.WriteLine("Got exception in UndoRedoTests.CleanUp: "
						+ e.Message);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that a footnote gets inserted at the right position and that it shows the
		/// right marker in the text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertFootnote()
		{
			CheckDisposed();
			FdoCache cache = m_firstMainWnd.Cache;
			// Set the IP to the second section in the first book at the 10th character in
			// the first paragraph
			m_firstDraftView.SetInsertionPoint(0, 1, 0, 10, false);
			ScrBook book = (ScrBook)cache.LangProject.TranslatedScriptureOA.ScriptureBooksOS[0];
			int nFootnotes = book.FootnotesOS.Count;

			m_firstMainWnd.CallInsertFootnote();

			Assert.AreEqual(nFootnotes + 1, book.FootnotesOS.Count);

			// Test footnote
			string expectedMarker = new String((char)((int)'a' + nFootnotes), 1);
			StFootnote footnote = (StFootnote)book.FootnotesOS[nFootnotes];
			Assert.AreEqual(expectedMarker, footnote.FootnoteMarker.Text,
				"Wrong footnote marker in footnote");
			ITsString tsString = footnote.FootnoteMarker.UnderlyingTsString;
			ITsTextProps ttp = tsString.get_PropertiesAt(0);
			string styleName = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			Assert.AreEqual(ScrStyleNames.FootnoteMarker, styleName,
				"Wrong style for footnote marker in footnote");

			// Test footnote marker in text
			IVwSelection sel = m_firstDraftView.RootBox.Selection;
			ITsString tss;
			int ich, hvoObj, tag, enc;
			bool fAssocPrev;
			sel.TextSelInfo(true, out tss, out ich, out fAssocPrev, out hvoObj, out tag, out enc);
			string strPara = tss.Text;
			Assert.AreEqual(StringUtils.kchObject, strPara[ich]);
			ttp = tss.get_PropertiesAt(ich);
			int nDummy;
			int wsActual = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nDummy);
			Assert.AreEqual(cache.LangProject.DefaultVernacularWritingSystem, wsActual,
				"Wrong writing system for footnote marker in text");
			string objData = ttp.GetStrPropValue((int)FwTextPropType.ktptObjData);
			Assert.AreEqual((char)(int)FwObjDataTypes.kodtOwnNameGuidHot, objData[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the location of the main window is persisted.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void WindowLocationPersistence()
		{
			CheckDisposed();
			Point saveLocation = new Point(14, 23);
			Size saveSize = new Size(452, 399);

			m_firstMainWnd.Location = saveLocation;
			m_firstMainWnd.Size = saveSize;
			m_firstMainWnd.Close();

			// This is really scary, since the assumption of the nunit system
			// is this method ony gets run once.
			FixtureSetup();
			Assert.AreEqual(saveLocation, m_firstMainWnd.Location);
			Assert.AreEqual(saveSize, m_firstMainWnd.Size);
		}
	}
#endif
}
