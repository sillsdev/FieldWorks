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
// File: RootsiteBasicViewTestsBase.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>. This class is specific for
	/// Rootsite tests.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RootsiteBasicViewTestsBase : BasicViewTestsBase
	{
		/// <summary>Defines the possible languages</summary>
		[Flags]
		public enum Lng
		{
			/// <summary>No paragraphs</summary>
			None = 0,
			/// <summary>English paragraphs</summary>
			English = 1,
			/// <summary>French paragraphs</summary>
			French = 2,
			/// <summary>UserWs paragraphs</summary>
			UserWs = 4,
			/// <summary>Empty paragraphs</summary>
			Empty = 8,
			/// <summary>Paragraph with 3 writing systems</summary>
			Mixed = 16,
		}

		/// <summary>Text for the first and third test paragraph (French)</summary>
		internal const string kFirstParaFra = "C'est une paragraph en francais.";
		/// <summary>Text for the second and fourth test paragraph (French).</summary>
		/// <remarks>This text needs to be shorter than the text for the first para!</remarks>
		internal const string kSecondParaFra = "C'est une deuxieme paragraph.";

		/// <summary>Writing System Factory (reset for each test since the cache gets re-created</summary>
		protected ILgWritingSystemFactory m_wsf;
		/// <summary>Id of English Writing System(reset for each test since the cache gets re-created</summary>
		protected int m_wsEng;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fixture setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			m_flidContainingTexts = (int)ScrBook.ScrBookTags.kflidFootnotes;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a test
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();

			m_wsf = Cache.LanguageWritingSystemFactoryAccessor;
			m_wsEng = m_wsf.GetWsFromStr("en");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the view
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_wsf = null;

			base.Exit();
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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_wsf = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		/// <param name="lng">Language</param>
		/// <param name="display"></param>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm(Lng lng, DummyBasicViewVc.DisplayType display)
		{
			if ((lng & Lng.English) == Lng.English)
				MakeEnglishParagraphs();
			if ((lng & Lng.French) == Lng.French)
				MakeFrenchParagraphs();
			if ((lng & Lng.UserWs) == Lng.UserWs)
				MakeUserWsParagraphs();
			if ((lng & Lng.Empty) == Lng.Empty)
				MakeEmptyParagraphs();
			if ((lng & Lng.Mixed) == Lng.Mixed)
				MakeMixedWsParagraph();

			base.ShowForm(display);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add English paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeEnglishParagraphs()
		{
			CheckDisposed();
			AddParagraphs(m_wsEng, DummyBasicView.kFirstParaEng, DummyBasicView.kSecondParaEng);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add French paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeFrenchParagraphs()
		{
			CheckDisposed();
			int wsFrn = m_wsf.GetWsFromStr("fr");
			AddParagraphs(wsFrn, kFirstParaFra, kSecondParaFra);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add paragraphs with the user interface writing system
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeUserWsParagraphs()
		{
			CheckDisposed();
			int ws = m_wsf.UserWs;
			AddParagraphs(ws, "blabla", "abc");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a paragraph containing runs, each of which has a different writing system.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeMixedWsParagraph()
		{
			CheckDisposed();
			StTxtPara para = (StTxtPara)AddParagraph();

			m_scrInMemoryCache.AddRunToMockedPara(para, "ws1", m_wsEng);
			m_scrInMemoryCache.AddRunToMockedPara(para, "ws2", m_wsf.GetWsFromStr("de"));
			m_scrInMemoryCache.AddRunToMockedPara(para, "ws3", m_wsf.GetWsFromStr("fr"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add empty paragraphs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void MakeEmptyParagraphs()
		{
			CheckDisposed();
			int ws = m_wsf.UserWs;
			AddParagraphs(ws, "", "");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a paragraph to the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected IStTxtPara AddParagraph()
		{
			IStText text = new StText(Cache, m_inMemoryCache.NewHvo(StFootnote.kClassId));
			m_inMemoryCache.CacheAccessor.AppendToFdoVector(m_hvoRoot, m_flidContainingTexts,
				text.Hvo);

			return m_scrInMemoryCache.AddParaToMockedText(text.Hvo, string.Empty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds paragraphs to the database
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="firstPara"></param>
		/// <param name="secondPara"></param>
		/// ------------------------------------------------------------------------------------
		private void AddParagraphs(int ws, string firstPara, string secondPara)
		{
			StTxtPara para1 = (StTxtPara)AddParagraph();
			StTxtPara para2 = (StTxtPara)AddParagraph();
			m_scrInMemoryCache.AddRunToMockedPara(para1, firstPara, ws);
			m_scrInMemoryCache.AddRunToMockedPara(para2, secondPara, ws);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			CheckDisposed();
			base.CreateTestData();

			IScrBook book = m_scrInMemoryCache.AddArchiveBookToMockedScripture(0, "GEN");
			m_hvoRoot = book.Hvo;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that use <see cref="DummyBasicView"/>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class RootsiteBasicViewTestsBaseRealCache : BaseTest
	{
		/// <summary>The draft form</summary>
		protected DummyBasicView m_basicView;

		/// <summary></summary>
		protected FdoCache m_fdoCache;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the server and name to be used for establishing a database connection. First
		/// try to get this info based on the command-line arguments. If not given, try using
		/// the info in the registry. Otherwise, just get the first FW database on the local
		/// server.
		/// </summary>
		/// <returns>A new FdoCache, based on options, or null, if not found.</returns>
		/// <remarks>This method was originally taken from FwApp.cs</remarks>
		/// -----------------------------------------------------------------------------------
		private FdoCache GetCache()
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("c", MiscUtils.LocalServerName);
			cacheOptions.Add("db", "TestLangProj");
			FdoCache cache = FdoCache.Create(cacheOptions);
			// For these tests we don't need to run InstallLanguage.
			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			wsf.BypassInstall = true;
			return cache;	// After all of this, it may be still be null, so watch out.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[SetUp]
		public virtual void Initialize()
		{
			CheckDisposed();

			Debug.Assert(m_fdoCache == null, "m_fdoCache is not null.");
			//if (m_fdoCache != null)
			//	m_fdoCache.Dispose();

			m_fdoCache = GetCache();
			FwStyleSheet styleSheet = new FwStyleSheet();

			ILangProject lgproj = m_fdoCache.LangProject;
			IScripture scripture = lgproj.TranslatedScriptureOA;
			styleSheet.Init(m_fdoCache, scripture.Hvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			Debug.Assert(m_basicView == null, "m_basicView is not null.");
			//if (m_basicView != null)
			//	m_basicView.Dispose();
			m_basicView = new DummyBasicView();
			m_basicView.Cache = m_fdoCache;
			m_basicView.Visible = false;
			m_basicView.StyleSheet = styleSheet;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Close the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[TearDown]
		public virtual void CleanUp()
		{
			CheckDisposed();

			UndoResult ures = 0;
			while (m_fdoCache.CanUndo)
			{
				m_fdoCache.Undo(out ures);
				if (ures == UndoResult.kuresFailed  || ures == UndoResult.kuresError)
					Assert.Fail("ures should not be == " + ures.ToString());
			}
			// Some tests are not at all happy to have m_basicView be disposed befroe the undoing.
			m_basicView.Dispose();
			m_basicView = null;
			m_fdoCache.Dispose();
			m_fdoCache = null;
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
				if (m_fdoCache != null)
				{
					UndoResult ures = 0;
					while (m_fdoCache.CanUndo)
						m_fdoCache.Undo(out ures);
					m_fdoCache.Dispose();
				}
				if (m_basicView != null)
					m_basicView.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fdoCache = null;
			m_basicView = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert the specified paragraphs and show the dialog
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void ShowForm()
		{
			m_basicView.DisplayType = DummyBasicViewVc.DisplayType.kAll;
			m_basicView.MakeEnglishParagraphs();

			// We don't actually want to show it, but we need to force the view to create the root
			// box and lay it out so that various test stuff can happen properly.
			m_basicView.Width = 300;
			m_basicView.Height = 307-25;
			m_basicView.MakeRoot();
			m_basicView.CallLayout();
		}
	}
}
