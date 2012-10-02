// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: MiscTests.cs
// Responsibility: JohnH, RandyR
// Last reviewed:
//
// <remarks>
// Implements MiscTests.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.InteropServices; // needed for Marshal

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region Tests with real database - DON'T ADD TESTS HERE!
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Miscelaneous tests that require a real database - Don't add new tests here because
	/// they are much slower then using the in-memory cache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class MiscTestsWithRealDb_DONTADDTESTSHERE : InDatabaseFdoTestBase
	{
		#region NewStPara and DerivedStTxtParas
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy class for testing PopulateCsBasic
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class NewStPara : StTxtPara
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="NewStPara"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public NewStPara()
			{
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="NewStPara"/> class.
			/// </summary>
			/// <param name="fcCache">The FDO cache object</param>
			/// <param name="hvo">HVO of the new object</param>
			/// ------------------------------------------------------------------------------------
			public NewStPara(FdoCache fcCache, int hvo)
				: base(fcCache, hvo)
			{
			}

			/// <summary><c>true</c> if PopulateCsBasic was called in this class</summary>
			public static bool s_fPopCalledInNewStPara = false;

			/// <summary></summary>
			protected new static void PopulateCsBasic(IDbColSpec cs)
			{
				s_fPopCalledInNewStPara = true;
				StTxtPara.PopulateCsBasic(cs);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Derived dummy class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DerivedStTxtPara : NewStPara
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DerivedStTxtPara"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DerivedStTxtPara()
			{
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DerivedStTxtPara"/> class.
			/// </summary>
			/// <param name="fcCache">The FDO cache object</param>
			/// <param name="hvo">HVO of the new object</param>
			/// ------------------------------------------------------------------------------------
			public DerivedStTxtPara(FdoCache fcCache, int hvo)
				: base(fcCache, hvo)
			{
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Derived dummy class
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class DerivedStTxtPara2 : NewStPara
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DerivedStTxtPara2"/> class.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public DerivedStTxtPara2()
			{
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="DerivedStTxtPara2"/> class.
			/// </summary>
			/// <param name="fcCache">The FDO cache object</param>
			/// <param name="hvo">HVO of the new object</param>
			/// ------------------------------------------------------------------------------------
			public DerivedStTxtPara2(FdoCache fcCache, int hvo)
				: base(fcCache, hvo)
			{
			}

			/// <summary><c>true</c> if PopulateCsBasic was called in this class</summary>
			public static bool s_fPopCalledInDerived = false;

			/// <summary></summary>
			protected new static void PopulateCsBasic(IDbColSpec cs)
			{
				s_fPopCalledInDerived = true;
				StTxtPara.PopulateCsBasic(cs);
			}
		}
		#endregion NewStPara and DerivedStTxtPara

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the correct PopulateCsBasic method is called
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadBasicData()
		{
			CheckDisposed();

			int hvoPhm_17_21 = 0;

			IOleDbCommand odc = null;
			try
			{
				m_fdoCache.DatabaseAccessor.CreateCommand(out odc);
				string sSql = @"select id from StTxtPara_ " +
					@"where Owner$ = (select dst from scrsection_content c " +
					@"join scrSection s on c.src = s.id " +
					@"where s.VerseRefStart = 57001001) " +
					@"and substring(contents, 1, 2) = '17'";
				odc.ExecCommand(sSql,
					(int)SqlStmtType.knSqlStmtSelectWithOneRowset);
				odc.GetRowset(0);
				bool fMoreRows;
				odc.NextRow(out fMoreRows);
				if (fMoreRows)
					odc.GetInt(1, out hvoPhm_17_21);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
			//			SqlConnection sqlConMaster = new SqlConnection(
			//				string.Format("Server={0}; Database={1};" +
			//				"User ID = sa; Password=inscrutable; Pooling=false;",
			//				m_fdoCache.ServerName, m_fdoCache.DatabaseName));
			//			sqlConMaster.Open();
			//			SqlCommand sqlComm = sqlConMaster.CreateCommand();
			//			// Select the hvo of the paragraph containing Philemon 17-21.
			//			string sSql = @"select id from StTxtPara_ " +
			//				@"where Owner$ = (select dst from scrsection_content c " +
			//				@"join scrSection s on c.src = s.id " +
			//				@"where s.VerseRefStart = 57001001) " +
			//				@"and substring(contents, 1, 2) = '17'";
			//			sqlComm.CommandText = sSql;
			//			SqlDataReader sqlreader =
			//				sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult);
			//			if (sqlreader.Read())
			//				hvoPhm_17_21 = sqlreader.GetInt32(0);
			//
			//			sqlreader.Close();
			//			sqlreader = null;
			//			sqlComm.Dispose();
			//			sqlComm = null;
			//			sqlConMaster.Close();
			//			sqlConMaster.Dispose();
			//			sqlConMaster = null;

			Assert.IsTrue(hvoPhm_17_21 > 0);

			NewStPara.s_fPopCalledInNewStPara = false;
			m_fdoCache.VwCacheDaAccessor.ClearInfoAbout(hvoPhm_17_21,
				VwClearInfoAction.kciaRemoveObjectInfoOnly);
			NewStPara para = new NewStPara(m_fdoCache, hvoPhm_17_21);

			Assert.IsTrue(NewStPara.s_fPopCalledInNewStPara,
				"PopulateCsBasic wasn't called in NewStPara");
			Assert.IsNotNull(para.Contents, "Contents of NewStPara is null");

			DerivedStTxtPara2.s_fPopCalledInNewStPara = false;
			DerivedStTxtPara2.s_fPopCalledInDerived = false;
			m_fdoCache.VwCacheDaAccessor.ClearInfoAbout(hvoPhm_17_21,
				VwClearInfoAction.kciaRemoveObjectInfoOnly);
			DerivedStTxtPara2 para2 = new DerivedStTxtPara2(m_fdoCache, hvoPhm_17_21);
			Assert.IsFalse(DerivedStTxtPara2.s_fPopCalledInNewStPara,
				"PopulateCsBasic was called in NewStPara instead of DerivedStTxtPara2");
			Assert.IsTrue(DerivedStTxtPara2.s_fPopCalledInDerived,
				"PoplulateCsBasic wasn't called in DerviedStTxtPara2");
			Assert.IsNotNull(para.Contents, "Contents of DerivedStTxtPara2 is null");

			DerivedStTxtPara.s_fPopCalledInNewStPara = false;
			m_fdoCache.VwCacheDaAccessor.ClearInfoAbout(hvoPhm_17_21,
				VwClearInfoAction.kciaRemoveObjectInfoOnly);
			DerivedStTxtPara para3 = new DerivedStTxtPara(m_fdoCache, hvoPhm_17_21);
			Assert.IsTrue(DerivedStTxtPara.s_fPopCalledInNewStPara,
				"PoplulateCsBasic wasn't called in DerviedStTxtPara");
			Assert.IsNotNull(para.Contents, "Contents of DerivedStTxtPara is null");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the OleDbEncap::BeginTrans and RollbackTrans methods
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		//[Ignore("Has problems with new restore database approach")]
		public void Transactions()
		{
			CheckDisposed();

			if (!m_fdoCache.DatabaseAccessor.IsTransactionOpen())
				m_fdoCache.DatabaseAccessor.BeginTrans();

			StStyle newStyle = new StStyle();
			m_fdoCache.LangProject.StylesOC.Add(newStyle);

			int newStyleHvo = newStyle.Hvo;
			m_fdoCache.DatabaseAccessor.RollbackTrans();

			// A simple rollback will not clear the object from the cache,
			// so do it here.
			m_fdoCache.VwCacheDaAccessor.ClearInfoAbout(newStyleHvo, VwClearInfoAction.kciaRemoveAllObjectInfo);

			if (newStyle.IsValidObject())
			{
				// Should not get here, as the create call should throw the exception.
				// Just in case, however, delete the new object.
				newStyle.DeleteUnderlyingObject();
				Assert.Fail("Object is still valid.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the FdoCache.Undo method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		//[Ignore("Has problems with new restore database approach")]
		public void Undo()
		{
			IText newInterlinearText = m_fdoCache.LangProject.TextsOC.Add(new Text());

			int newInterlinearTextHvo = newInterlinearText.Hvo;
			SqlConnection sqlConMaster = new SqlConnection(string.Format(@"Server=.\SILFW; " +
				"Database={0}; User ID = sa; Password=inscrutable;Connect Timeout = 30; Pooling=false;",
				DatabaseName));
			sqlConMaster.Open();

			SqlCommand sqlComm = sqlConMaster.CreateCommand();
			string sSql = string.Format(
				@"select id from Text_ where id={0}", newInterlinearTextHvo);
			sqlComm.CommandText = sSql;
			SqlDataReader sqlreader =
				sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult);
			bool fAnyRowsBeforeRollback = sqlreader.Read();
			sqlreader.Close();

			int nFdoRowsBeforeRollback = m_fdoCache.LangProject.TextsOC.Count;

			m_fdoCache.Undo();

			sqlreader = sqlComm.ExecuteReader(System.Data.CommandBehavior.SingleResult);
			bool fAnyRowsAfterRollback = sqlreader.Read();
			sqlreader.Close();
			sqlConMaster.Close();

			int nFdoRowsAfterRollback = m_fdoCache.LangProject.TextsOC.Count;
			if (fAnyRowsAfterRollback)
				m_fdoCache.DeleteObject(newInterlinearTextHvo);

			Assert.AreEqual(false, fAnyRowsAfterRollback);
			Assert.AreEqual(nFdoRowsBeforeRollback - 1, nFdoRowsAfterRollback);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the CmObject.DeleteObjects method (with more than one object deleted)
		/// </summary>
		/// ------------------------------------------------------------------------------------

		[Test]
		public void DeleteObjects()
		{
			CheckDisposed();

			m_fdoCache.BeginUndoTask("make texts", "undo make texts");
			// One text with 4 paras
			SIL.FieldWorks.FDO.Ling.Text text1 = new Text();
			m_fdoCache.LangProject.TextsOC.Add(text1);
			StText body1 = new StText();
			text1.ContentsOA = body1;
			StTxtPara para1A = new StTxtPara();
			body1.ParagraphsOS.Append(para1A);
			StTxtPara para1B = new StTxtPara();
			body1.ParagraphsOS.Append(para1B);
			StTxtPara para1C = new StTxtPara();
			body1.ParagraphsOS.Append(para1C);
			StTxtPara para1D = new StTxtPara();
			body1.ParagraphsOS.Append(para1D);
			para1A.Contents.Text = "Contents of para1A";
			para1B.Contents.Text = "Para 1B stuff";
			para1C.Contents.Text = "Rubbish in 1C";
			para1D.Contents.Text = "Things in 1D";

			// Second text, 2 paras
			SIL.FieldWorks.FDO.Ling.Text text2 = new Text();
			m_fdoCache.LangProject.TextsOC.Add(text2);
			StText body2 = new StText();
			text2.ContentsOA = body2;
			StTxtPara para2A = new StTxtPara();
			body2.ParagraphsOS.Append(para2A);
			StTxtPara para2B = new StTxtPara();
			body2.ParagraphsOS.Append(para2B);
			para2A.Contents.Text = "2A contents";
			para2B.Contents.Text = "2B stuff";

			m_fdoCache.EndUndoTask();

			Set<int> idsToDelete = new Set<int>();
			idsToDelete.Add(para1A.Hvo);
			idsToDelete.Add(para1B.Hvo);
			idsToDelete.Add(para1D.Hvo);
			idsToDelete.Add(para2A.Hvo);
			m_fdoCache.BeginUndoTask("delete paras", "undo delete paras");
			CmObject.DeleteObjects(idsToDelete, m_fdoCache);
			m_fdoCache.EndUndoTask();
			Assert.AreEqual(1, body1.ParagraphsOS.Count, "paras were deleted1");
			Assert.AreEqual(1, body2.ParagraphsOS.Count, "paras were deleted2");
			m_fdoCache.Undo();
			m_fdoCache.VwCacheDaAccessor.ClearAllData(); // as in Refresh.
			Assert.AreEqual(4, body1.ParagraphsOS.Count, "paras were restored1");
			Assert.AreEqual(2, body2.ParagraphsOS.Count, "paras were restored1");
			Assert.AreEqual("Contents of para1A", para1A.Contents.Text, "para 1A contents restored");
			Assert.AreEqual("Things in 1D", para1D.Contents.Text, "para 1D contents restored");
			Assert.AreEqual("2A contents", para2A.Contents.Text, "para 2A contents restored");
			m_fdoCache.Redo();
			m_fdoCache.VwCacheDaAccessor.ClearAllData(); // as in Refresh.
			Assert.AreEqual(1, body1.ParagraphsOS.Count, "paras were deleted1");
			Assert.AreEqual(1, body2.ParagraphsOS.Count, "paras were deleted2");
			m_fdoCache.Undo();
			m_fdoCache.VwCacheDaAccessor.ClearAllData(); // as in Refresh.
			Assert.AreEqual(4, body1.ParagraphsOS.Count, "paras were restored1");
			Assert.AreEqual(2, body2.ParagraphsOS.Count, "paras were restored1");
			Assert.AreEqual("Contents of para1A", para1A.Contents.Text, "para 1A contents restored");
			Assert.AreEqual("Things in 1D", para1D.Contents.Text, "para 1D contents restored");
			Assert.AreEqual("2A contents", para2A.Contents.Text, "para 2A contents restored");
			m_fdoCache.Undo(); // cleans up the last of what we added.
		}
		/// <summary>
		/// Make sure an object gets deleted from the database, and the cache.
		/// </summary>
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		//TODO: (JDH) fix this so that only an exception *on the last line* will produce a PASS

		// Validity checking in DEBUG mode has been turned off.  It complicates performance testing too much.
		//        [ExpectedException(typeof(ArgumentException))]
		//#if !DEBUG
		//        [Ignore("This test doesn't get an exception in release mode due to CmObject c'tor")]
		//#endif

		//TODO (Steve Miller): The above test fails if the followeng lines are remarked
		// out in CmObject.cs, like below. I have frequently remarked out lines there
		// like this, because they foul up SQL performance tests:
		//
		// (InitExisting method)
		//  //#if DEBUG
		//  //	if (!fcCache.IsDummyObject(hvo))
		//  //		fCheckValidity = true;
		//  //#endif
		//
		// (CreateFromDBObject method)
		// //#if DEBUG
		// //     true/*validity check*/,
		// //#else
		//		false,
		// //#endif

		public void DeleteUnderlyingObject()
		{
			CheckDisposed();

			LexEntry se = new LexEntry();
			m_fdoCache.LangProject.LexDbOA.EntriesOC.Add(se);
			LexSense ls = new LexSense();
			se.SensesOS.Append(ls);
			int khvoLexSense = ls.Hvo;

			ls.DeleteUnderlyingObject();

			Assert.IsTrue(m_fdoCache.VwOleDbDaAccessor.get_ObjOwner(khvoLexSense) <= 0); // khvoLexSense not removed from cache.

			// See if FDO object is valid. It should not be.
			Assert.IsFalse(ls.IsValidObject());

			// Test to see if it is really gone from DB.
			// An exception should be thrown by CmObject.InitExisting,
			// since khvoLexSense isn't in DB now.
			ls = new LexSense(m_fdoCache, khvoLexSense, true, true);
			Assert.IsFalse(ls.IsValidObject());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test creating a particular (fixed-Guid) FDO object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFromGuid()
		{
			CheckDisposed();

			ICmAnnotationDefn cad = CmAnnotationDefn.TextSegment(Cache);
			Assert.AreEqual("Text Segment", cad.Name.UserDefaultWritingSystem);
		}
	}
	#endregion

	/// <summary>
	/// Implements miscellaneous tests which will be performed by NUnit
	/// </summary>
	[TestFixture]
	public class MiscTests : InMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_inMemoryCache.InitializeLexDb();
		}

		/// <summary>
		/// Tests the failure of an instantiation of an object
		/// </summary>
		[Test]
		[ExpectedException(typeof(Exception))]
		public void DBObjectInstantiation()
		{
			CheckDisposed();

			// Putting persons in the UsageTypes doesn't really make sense, but for testing purposes
			// it doesn't matter what we store there as long as it is derived from CmPossibility
			CmPerson person = new CmPerson();
			CmPossibility pos = new CmPossibility();
			Cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Append(person);
			Cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Append(pos);

			int khvoAPossibiltyObject = pos.Hvo;
			int khvoAPersonObject = person.Hvo;
			Assert.AreEqual("CmPerson", CmPossibility.CreateFromDBObject(Cache, khvoAPersonObject).GetType().Name);
			Assert.AreEqual("CmPossibility", CmPossibility.CreateFromDBObject(Cache, khvoAPossibiltyObject).GetType().Name);

			// Now try it not assuming anything about the class type (use the method on CmObject)
			// CmObject uses a different, less efficient method.
			Assert.AreEqual("CmPerson", CmObject.CreateFromDBObject(Cache, khvoAPersonObject).GetType().Name);

			// trying to turn a possibility into a person should throw an exception
			CmPerson.CreateFromDBObject(Cache, khvoAPossibiltyObject);
		}


		/// <summary>
		/// Test setting an atomic owning property.
		/// </summary>
		[Test]
		public void SetNewOwning()
		{
			CheckDisposed();

			int hvoOld = Cache.LangProject.MorphologicalDataOAHvo;
			IMoMorphData mdOriginal = Cache.LangProject.MorphologicalDataOA;
			Cache.LangProject.MorphologicalDataOA = (new MoMorphData() as IMoMorphData);
			Assert.IsTrue(hvoOld != Cache.LangProject.MorphologicalDataOAHvo);
			Assert.IsTrue(Cache.LangProject.MorphologicalDataOA.IsValidObject());
		}


		/// <summary>
		/// Test setting an atomic reference value.
		/// </summary>
		[Test]
		public void SetAtomicReference()
		{
			CheckDisposed();

			ILangProject lp = Cache.LangProject;
			ICmPossibility confidence = (ICmPossibility)lp.ConfidenceLevelsOA.PossibilitiesOS.Append(new CmPossibility());
			IPartOfSpeech pos = (IPartOfSpeech)lp.PartsOfSpeechOA.PossibilitiesOS.Append(new PartOfSpeech());

			pos.ConfidenceRA = confidence;
			Assert.IsTrue(confidence.Equals(pos.ConfidenceRA));
			Assert.AreEqual(pos.ConfidenceRA, confidence);
			Assert.AreEqual(pos.ConfidenceRAHvo, confidence.Hvo);

			pos.ConfidenceRA = null;	// Excersizes RemoveReference on cache.
			Assert.IsNull(pos.ConfidenceRA);
		}

		/// <summary>
		/// Tests accessing properties
		/// </summary>
		[Test]
		public void GetProperties()
		{
			CheckDisposed();

			// Putting AnthroItems in the UsageTypes doesn't really make sense, but for testing purposes
			// it doesn't matter what we store there as long as it is derived from CmPossibility
			CmAnthroItem p = new CmAnthroItem();
			Cache.LangProject.LexDbOA.UsageTypesOA.PossibilitiesOS.Append(p);
			Cache.MainCacheAccessor.SetInt(p.Hvo,
				(int)CmPossibility.CmPossibilityTags.kflidBackColor, 0xffc000);

			Assert.AreEqual(0xffc000, p.BackColor);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CopyObject method by creating a new draft and copying the first available
		/// book to it.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyObject()
		{
			CheckDisposed();

			IStStyle styleToCopy = AddTestStyle("MyStyle", ContextValues.General,
				StructureValues.Undefined, FunctionValues.Prose, false, Cache.LangProject.StylesOC);

			int originalNumberOfStyles = Cache.LangProject.LexDbOA.StylesOC.Count;
			int hvoCopiedStyle = Cache.CopyObject(styleToCopy.Hvo, Cache.LangProject.LexDbOA.Hvo,
				(int)LexDb.LexDbTags.kflidStyles);

			IStStyle copiedStyle = CmObject.CreateFromDBObject(Cache, hvoCopiedStyle) as IStStyle;

			Assert.AreEqual(originalNumberOfStyles + 1, Cache.LangProject.LexDbOA.StylesOC.Count);
			Assert.AreNotEqual(styleToCopy.Hvo, hvoCopiedStyle);
			Assert.AreEqual(styleToCopy.Name, copiedStyle.Name);
		}
	}
}
