// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002-2008, SIL International. All Rights Reserved.
// <copyright from='2002' to='2008' company='SIL International'>
//		Copyright (c) 2002-2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: VectorTests.cs
// Responsibility: RandyR
//
// <remarks>
// Implements VectorTests.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.FDOTests;
using Rhino.Mocks;

namespace SIL.FieldWorks.FDO.CoreTests.VectorTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test public properties and methods on the abstract FdoVector class.
	/// Some properties and methods are abstract, so the 'test'
	/// will end up testing those subclass implementations.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoMainVectorTests : ScrInMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Count property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CountPropertyTests()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ILangProject lp = Cache.LanguageProject;
			ILexDb lexDb = lp.LexDbOA;
			ILexEntry le = servLoc.GetInstance<ILexEntryFactory>().Create();
			ILexSense sense = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(sense);

			// FdoReferenceCollection
			int originalCount = lexDb.LexicalFormIndexRC.Count;
			lexDb.LexicalFormIndexRC.Add(le);
			Assert.AreEqual(originalCount + 1, lexDb.LexicalFormIndexRC.Count);
			lexDb.LexicalFormIndexRC.Remove(le);
			Assert.AreEqual(originalCount, lexDb.LexicalFormIndexRC.Count);

			// FdoReferenceSequence
			originalCount = le.MainEntriesOrSensesRS.Count;
			le.MainEntriesOrSensesRS.Add(sense);
			Assert.AreEqual(originalCount + 1, le.MainEntriesOrSensesRS.Count);
			le.MainEntriesOrSensesRS.RemoveAt(le.MainEntriesOrSensesRS.Count - 1);
			Assert.AreEqual(originalCount, le.MainEntriesOrSensesRS.Count);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Owning sequence Item ([idx]) method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OwningSequence_Item_Tests()
		{
			var servLoc = Cache.ServiceLocator;
			var entry = servLoc.GetInstance<ILexEntryFactory>().Create();
			var senseFactory = servLoc.GetInstance<ILexSenseFactory>();
			var sense1 = senseFactory.Create();
			entry.SensesOS.Add(sense1);
			var sense2 = senseFactory.Create();
			entry.SensesOS[0] = sense2;

			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, sense1.Hvo, "Sense not deleted.");
			Assert.AreSame(sense2, entry.SensesOS[0], "Sense2 not in the right place.");
			Assert.AreEqual(1, entry.SensesOS.Count, "Wrong number of senses.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that OwnOrd gets updated properly when an Owning sequence is modified.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void OwningSequence_OwnOrd_Tests()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le = leFactory.Create();

			var alloFactory = servLoc.GetInstance<IMoStemAllomorphFactory>();
			var allo1 = alloFactory.Create();
			le.AlternateFormsOS.Add(allo1);
			var allo2 = alloFactory.Create();
			le.AlternateFormsOS.Add(allo2);
			var allo3 = alloFactory.Create();
			le.AlternateFormsOS.Add(allo3);

			le.AlternateFormsOS.Add(allo1);
			Assert.IsTrue(le.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo3, allo1 }));
			Assert.AreEqual(2, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo3.OwnOrd);

			le.AlternateFormsOS.Insert(0, allo1);
			Assert.IsTrue(le.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo2, allo3 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(1, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);

			le.AlternateFormsOS.Insert(1, allo1);
			Assert.IsTrue(le.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo2, allo3 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(1, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);

			le.AlternateFormsOS.Insert(2, allo1);
			Assert.IsTrue(le.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo1, allo3 }));
			Assert.AreEqual(1, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);

			le.AlternateFormsOS.Remove(allo1);
			Assert.IsTrue(le.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo3 }));
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo3.OwnOrd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If an item is in an owning sequence and it is inserted into a different sequence,
		/// the own ord values of all items in both lists should be maintained properly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void InsertUsedToMoveObjBetweenOwningSequences()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFactory.Create();
			var le2 = leFactory.Create();

			var alloFactory = servLoc.GetInstance<IMoStemAllomorphFactory>();
			var allo1 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo1);
			var allo2 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo2);
			var allo3 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo3);
			var allo4 = alloFactory.Create();
			le2.AlternateFormsOS.Add(allo4);
			var allo5 = alloFactory.Create();
			le2.AlternateFormsOS.Add(allo5);
			var allo6 = alloFactory.Create();
			le2.AlternateFormsOS.Add(allo6);

			 // Move allo1 from first entry's list to second entry's list
			le2.AlternateFormsOS.Insert(0, allo1);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo3 }));
			Assert.IsTrue(le2.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo4, allo5, allo6 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo3.OwnOrd);
			Assert.AreEqual(1, allo4.OwnOrd);
			Assert.AreEqual(2, allo5.OwnOrd);
			Assert.AreEqual(3, allo6.OwnOrd);

			le1.AlternateFormsOS.Insert(1, allo4);
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo4, allo3 }));
			Assert.IsTrue(le2.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo5, allo6 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(2, allo3.OwnOrd);
			Assert.AreEqual(1, allo4.OwnOrd);
			Assert.AreEqual(1, allo5.OwnOrd);
			Assert.AreEqual(2, allo6.OwnOrd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If an item is in an owning sequence and the indexed setter (this[]) is used to move
		/// it into a different sequence, the own ord values of all items in both lists should
		/// be maintained properly.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void IndexedSetterUsedToMoveObjBetweenOwningSequences()
		{
			var servLoc = Cache.ServiceLocator;
			var lp = Cache.LanguageProject;
			var leFactory = servLoc.GetInstance<ILexEntryFactory>();
			var le1 = leFactory.Create();
			var le2 = leFactory.Create();

			var alloFactory = servLoc.GetInstance<IMoStemAllomorphFactory>();
			var allo1 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo1);
			var allo2 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo2);
			var allo3 = alloFactory.Create();
			le1.AlternateFormsOS.Add(allo3);
			var allo4 = alloFactory.Create();
			le2.AlternateFormsOS.Add(allo4);
			var allo5 = alloFactory.Create();
			le2.AlternateFormsOS.Add(allo5);
			var allo6 = alloFactory.Create();
			le2.AlternateFormsOS.Add(allo6);

			// Move allo1 from first entry's list to second entry's list
			le2.AlternateFormsOS[0] = allo1;
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo3 }));
			Assert.IsTrue(le2.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo5, allo6 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo3.OwnOrd);
			Assert.AreEqual(1, allo5.OwnOrd);
			Assert.AreEqual(2, allo6.OwnOrd);

			le1.AlternateFormsOS[1] = allo6;
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo6 }));
			Assert.IsTrue(le2.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1, allo5 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo5.OwnOrd);
			Assert.AreEqual(1, allo6.OwnOrd);

			le1.AlternateFormsOS[1] = allo5;
			Assert.IsTrue(le1.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo2, allo5 }));
			Assert.IsTrue(le2.AlternateFormsOS.SequenceEqual(new IMoForm[] { allo1 }));
			Assert.AreEqual(0, allo1.OwnOrd);
			Assert.AreEqual(0, allo2.OwnOrd);
			Assert.AreEqual(1, allo5.OwnOrd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the Contains method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsMethodTests()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ILangProject lp = Cache.LanguageProject;
			ILexDb lexDb = lp.LexDbOA;
			ILexEntry le = servLoc.GetInstance<ILexEntryFactory>().Create();

			// FdoReferenceCollection
			Assert.IsFalse(lexDb.LexicalFormIndexRC.Contains(le));
			lexDb.LexicalFormIndexRC.Add(le);
			Assert.IsTrue(lexDb.LexicalFormIndexRC.Contains(le));
			lexDb.LexicalFormIndexRC.Remove(le);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CopyTo method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyTo_OneItemInLongListTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBookFactory bookFact = servLoc.GetInstance<IScrBookFactory>();

			// Setup the source sequence using the scripture books sequence.
			IScrBook book0 = bookFact.Create(1);

			IScrBook[] bookArray = new IScrBook[5];
			m_scr.ScriptureBooksOS.CopyTo(bookArray, 3);

			Assert.IsNull(bookArray[0]);
			Assert.IsNull(bookArray[1]);
			Assert.IsNull(bookArray[2]);
			Assert.AreEqual(book0, bookArray[3]);
			Assert.IsNull(bookArray[4]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CopyTo method when we are copying into a one-item list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyTo_OneItemInOneItemListTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBookFactory bookFact = servLoc.GetInstance<IScrBookFactory>();

			// Setup the source sequence using the scripture books sequence.
			IScrBook book0 = bookFact.Create(1);

			IScrBook[] bookArray = new IScrBook[1];
			m_scr.ScriptureBooksOS.CopyTo(bookArray, 0);

			Assert.AreEqual(book0, bookArray[0]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CopyTo method when we are copying no items into an empty list.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyTo_NoItemsInEmptyItemListTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBookFactory bookFact = servLoc.GetInstance<IScrBookFactory>();

			IScrBook[] bookArray = new IScrBook[0];
			m_scr.ScriptureBooksOS.CopyTo(bookArray, 0);
			// This test makes sure that an exception is not thrown when the array is empty.
			// This fixes creating a new List<> when giving a FdoVector as the parameter.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CopyTo method when we are copying from one reference sequence to another.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyTo_RefSeqToRefSeq()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ILangProject lp = Cache.LanguageProject;
			ILexEntry le1 = servLoc.GetInstance<ILexEntryFactory>().Create();
			ILexSense sense1 = servLoc.GetInstance<ILexSenseFactory>().Create();
			le1.SensesOS.Add(sense1);
			le1.MainEntriesOrSensesRS.Add(sense1);
			ILexSense sense2 = servLoc.GetInstance<ILexSenseFactory>().Create();
			le1.SensesOS.Add(sense2);
			le1.MainEntriesOrSensesRS.Add(sense2);

			ILexEntry le2 = servLoc.GetInstance<ILexEntryFactory>().Create();

			le1.MainEntriesOrSensesRS.CopyTo(le2.MainEntriesOrSensesRS, 0);

			Assert.AreEqual(2, le2.MainEntriesOrSensesRS.Count);
			Assert.AreEqual(sense1, le2.MainEntriesOrSensesRS[0]);
			Assert.AreEqual(sense2, le2.MainEntriesOrSensesRS[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddTo method when we are copying from one reference collection to another.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddTo_RefColToRefCol()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ILangProject lp = Cache.LanguageProject;
			ILexDb lexDb = lp.LexDbOA;
			ILexAppendix app1 = servLoc.GetInstance<ILexAppendixFactory>().Create();
			lexDb.AppendixesOC.Add(app1);
			ILexAppendix app2 = servLoc.GetInstance<ILexAppendixFactory>().Create();
			lexDb.AppendixesOC.Add(app2);
			ILexEntry le1 = servLoc.GetInstance<ILexEntryFactory>().Create();
			ILexSense sense1 = servLoc.GetInstance<ILexSenseFactory>().Create();
			le1.SensesOS.Add(sense1);
			ILexSense sense2 = servLoc.GetInstance<ILexSenseFactory>().Create();
			le1.SensesOS.Add(sense2);

			sense1.AppendixesRC.Add(app1);
			sense1.AppendixesRC.Add(app2);

			sense1.AppendixesRC.AddTo(sense2.AppendixesRC);

			Assert.AreEqual(2, sense2.AppendixesRC.Count);
			Assert.IsTrue(sense2.AppendixesRC.Contains(app1));
			Assert.IsTrue(sense2.AppendixesRC.Contains(app2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Insert method with a null object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Insert_Null()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;

			IScrBook book0 = servLoc.GetInstance<IScrBookFactory>().Create(1);
			m_scr.ScriptureBooksOS.Insert(0, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Insert method with a null object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(FDOObjectDeletedException),
			ExpectedMessage = "Owned object has been deleted.")]
		public void Insert_Deleted()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;

			IScrBook book0 = servLoc.GetInstance<IScrBookFactory>().Create(1);
			m_scr.ScriptureBooksOS.Remove(book0);
			m_scr.ScriptureBooksOS.Insert(0, book0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Insert method with a null object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(FDOObjectUninitializedException),
			ExpectedMessage = "Object has not been initialized.")]
		public void InsertIntoRefSequence_Uninitialized()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ILangProject lp = Cache.LanguageProject;
			ILexEntry le = servLoc.GetInstance<ILexEntryFactory>().Create();

			var senseUninitialized = MockRepository.GenerateStub<ILexSense>();
			senseUninitialized.Stub(x => x.Hvo).Return((int)SpecialHVOValues.kHvoUninitializedObject);
			le.MainEntriesOrSensesRS.Insert(0, senseUninitialized);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests inserting an object into an owning sequence that can not be owned
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(InvalidOperationException),
			ExpectedMessage = "ScrRefSystem can not be owned!")]
		public void Insert_UnownableObject()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBookFactory bookFact = servLoc.GetInstance<IScrBookFactory>();

			// Setup the source sequence using the scripture books sequence.
			IStText text;
			IScrBook book0 = bookFact.Create(1, out text);
			IStTxtPara para = text.AddNewTextPara(ScrStyleNames.MainBookTitle);
			IScrRefSystem systemToAdd = servLoc.GetInstance<IScrRefSystemRepository>().Singleton;
			para.AnalyzedTextObjectsOS.Insert(0, systemToAdd);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveTo method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveTo_EmptyListTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBookFactory bookFact = servLoc.GetInstance<IScrBookFactory>();

			// Setup the source sequence using the scripture books sequence.
			IScrBook book0 = bookFact.Create(1);
			IScrBook book1 = bookFact.Create(2);
			IScrBook book2 = bookFact.Create(3);

			// Setup the target sequence so it's able to have items moved to it.
			IScrDraft targetSeq = servLoc.GetInstance<IScrDraftFactory>().Create("MoveTo_EmptyListTest");

			m_scr.ScriptureBooksOS.MoveTo(1, 2, targetSeq.BooksOS, 0);

			Assert.AreEqual(2, targetSeq.BooksOS.Count);
			Assert.AreEqual(1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(book1, targetSeq.BooksOS[0]);
			Assert.AreEqual(book2, targetSeq.BooksOS[1]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveTo method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveTo_PopulatedListTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBookFactory bookFact = servLoc.GetInstance<IScrBookFactory>();

			// Setup the source sequence using the scripture books sequence.
			IScrBook book0 = bookFact.Create(1);
			IScrBook book1 = bookFact.Create(2);
			IScrBook book2 = bookFact.Create(3);

			// Setup the target sequence so it's able to have items moved to it.
			IScrDraft targetSeq = servLoc.GetInstance<IScrDraftFactory>().Create("MoveTo_PopulatedListTest");
			IScrBook bookDest0 = bookFact.Create(targetSeq.BooksOS, 1);
			IScrBook bookDest1 = bookFact.Create(targetSeq.BooksOS, 2);

			m_scr.ScriptureBooksOS.MoveTo(1, 2, targetSeq.BooksOS, 1);

			Assert.AreEqual(4, targetSeq.BooksOS.Count);
			Assert.AreEqual(1, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(bookDest0, targetSeq.BooksOS[0]);
			Assert.AreEqual(book1, targetSeq.BooksOS[1]);
			Assert.AreEqual(book2, targetSeq.BooksOS[2]);
			Assert.AreEqual(bookDest1, targetSeq.BooksOS[3]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveTo method where the old position should go through
		/// RemoveObjectSideEffects().
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveTo_RemoveSideEffects()
		{
			// Setup a chart on a text
			var servLoc = Cache.ServiceLocator;
			var tssFact = Cache.TsStrFactory;
			var ws = Cache.DefaultVernWs;
			var chart = SetupChart();
			var dummyPoss = chart.TemplateRA;
			var rowFact = servLoc.GetInstance<IConstChartRowFactory>();
			var chartTagFact = servLoc.GetInstance<IConstChartTagFactory>();

			// Setup the Cell sequence in a row using chart tag objects.
			var row0 = rowFact.Create(chart, 0, tssFact.MakeString("1a", ws));
			var row1 = rowFact.Create(chart, 1, tssFact.MakeString("1b", ws));
			var tag1 = chartTagFact.Create(row0, 0, dummyPoss, dummyPoss);
			var tag2 = chartTagFact.Create(row0, 1, dummyPoss, dummyPoss);
			var tag3 = chartTagFact.Create(row1, 0, dummyPoss, dummyPoss);

			// SUT
			row0.CellsOS.MoveTo(0, 1, row1.CellsOS, 0);

			Assert.AreEqual(3, row1.CellsOS.Count);
			Assert.AreEqual(tag1, row1.CellsOS[0]);
			Assert.AreEqual(tag2, row1.CellsOS[1]);
			Assert.AreEqual(tag3, row1.CellsOS[2]);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, row0.Hvo, "Should delete first row.");
		}

		private IDsConstChart SetupChart()
		{
			var servLoc = Cache.ServiceLocator;
			var tssFact = Cache.TsStrFactory;
			var ws = Cache.DefaultVernWs;
			var text = servLoc.GetInstance<ITextFactory>().Create();
			Cache.LangProject.TextsOC.Add(text);
			var stText = servLoc.GetInstance<IStTextFactory>().Create();
			text.ContentsOA = stText;
			var data = servLoc.GetInstance<IDsDiscourseDataFactory>().Create();
			Cache.LangProject.DiscourseDataOA = data;
			var dummyList = servLoc.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LangProject.DiscourseDataOA.ConstChartTemplOA = dummyList;
			var dummy = servLoc.GetInstance<ICmPossibilityFactory>().Create();
			dummyList.PossibilitiesOS.Add(dummy);
			return servLoc.GetInstance<IDsConstChartFactory>().Create(data, stText, dummy);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the MoveTo method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MoveTo_DestListLargerThenSrcTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			IScrBookFactory bookFact = servLoc.GetInstance<IScrBookFactory>();

			// Setup the source sequence using the scripture books sequence.
			IScrBook book0 = bookFact.Create(1);

			// Setup the target sequence so it's able to have items moved to it.
			IScrDraft targetSeq = servLoc.GetInstance<IScrDraftFactory>().Create("MoveTo_DestListLargerThenSrcTest");
			IScrBook bookD0 = bookFact.Create(targetSeq.BooksOS, 1);
			IScrBook bookD1 = bookFact.Create(targetSeq.BooksOS, 2);

			m_scr.ScriptureBooksOS.MoveTo(0, 0, targetSeq.BooksOS, 2);

			Assert.AreEqual(3, targetSeq.BooksOS.Count);
			Assert.AreEqual(0, m_scr.ScriptureBooksOS.Count);
			Assert.AreEqual(bookD0, targetSeq.BooksOS[0]);
			Assert.AreEqual(bookD1, targetSeq.BooksOS[1]);
			Assert.AreEqual(book0, targetSeq.BooksOS[2]);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test the 'Count' vector method on a collection.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void VectorCount()
		{
			ILexEntryFactory factory = Cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			factory.Create();
			ILexEntry entry = factory.Create();
			Assert.AreEqual(0, entry.AlternateFormsOS.Count);

			entry.Delete();
			Assert.IsTrue(0 < Cache.LanguageProject.LexDbOA.Entries.Count());
		}

		////-------------------------------------------------------------------------------
		/// <summary>
		/// Test re-adding an object to its owning collection. This should work, but it
		/// won't actually be moved anywhere.
		/// </summary>
		////-------------------------------------------------------------------------------
		[Test]
		public void FdoOwningCollection_ReAddToOC()
		{
			var oc = Cache.LanguageProject.MorphologicalDataOA.AdhocCoProhibitionsOC;
			var acp = Cache.ServiceLocator.GetInstance<IMoAlloAdhocProhibFactory>().Create();
			oc.Add(acp);
			int count = oc.Count();
			oc.Add(acp);	// Try adding it again.
			Assert.AreEqual(count, oc.Count);
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Contains method on an owning collection
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void OwningCollectionContainsObject()
		{
			var factory = Cache.ServiceLocator.GetInstance<ICmResourceFactory>();
			var resource = factory.Create();
			Assert.IsFalse(Cache.LanguageProject.LexDbOA.ResourcesOC.Contains(resource));
			Cache.LangProject.LexDbOA.ResourcesOC.Add(resource);
			Assert.IsTrue(Cache.LanguageProject.LexDbOA.ResourcesOC.Contains(resource));
		}

		//-------------------------------------------------------------------------------
		/// <summary>
		/// Test the 'ReallyReallyAllPossibilities' method on a possibility list.
		/// </summary>
		//-------------------------------------------------------------------------------
		[Test]
		public void GetAllPossibilities()
		{
			// We use the following hierarchy:
			// top - 1 -  1.1 - 1.1.1
			//     \ 2 \- 1.2
			//          \ 1.3
			// which are 7 CmPossibility objects
			var factory = Cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
			ICmPossibility top = factory.Create();
			Cache.LanguageProject.PartsOfSpeechOA.PossibilitiesOS.Add(top);
			ICmPossibility one = factory.Create();
			top.SubPossibilitiesOS.Add(one);
			ICmPossibility two = factory.Create();
			top.SubPossibilitiesOS.Add(two);
			ICmPossibility oneone = factory.Create();
			one.SubPossibilitiesOS.Add(oneone);
			ICmPossibility onetwo = factory.Create();
			one.SubPossibilitiesOS.Add(onetwo);
			ICmPossibility onethree = factory.Create();
			one.SubPossibilitiesOS.Add(onethree);
			ICmPossibility oneoneone = factory.Create();
			oneone.SubPossibilitiesOS.Add(oneoneone);

			Assert.AreEqual(7, Cache.LanguageProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.Count);
		}
	}

	#region FdoMinimalVectorTests class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test most basic public properties and methods on the abstract FdoVector class.
	/// Some properties and methods are abstract, so the 'test'
	/// will end up testing those subclass implementations.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FdoMinimalVectorTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to add a null item to a vector.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddNullItemToVectorTest()
		{
			Cache.LanguageProject.AnalyzingAgentsOC.Add(null); // Should throw the exception.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to add a null item to a vector.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NotSupportedException))]
		public void AddNullItem2ToVectorTest()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			ILangProject lp = Cache.LanguageProject;
			ILexEntry le = servLoc.GetInstance<ILexEntryFactory>().Create();
			ILexSense sense = servLoc.GetInstance<ILexSenseFactory>().Create();
			le.SensesOS.Add(sense);

			le.MainEntriesOrSensesRS.Add(sense);
			le.MainEntriesOrSensesRS[0] = null; // Should throw the exception.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to add a deleted item to a vector.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(FDOObjectDeletedException))]
		public void AddDeletedItemToVectorTest()
		{
			// Make new annotation.
			var ann = Cache.ServiceLocator.GetInstance<ICmBaseAnnotationFactory>().Create();
			Cache.LanguageProject.AnnotationsOC.Add(ann);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, ann.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, ann.Hvo);

			Cache.LanguageProject.AnnotationsOC.Remove(ann);
			Assert.AreEqual((int)SpecialHVOValues.kHvoObjectDeleted, ann.Hvo);

			Cache.LanguageProject.AnnotationsOC.Add(ann); // Should throw the exception for being deleted.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Try to add a duplicate item to an owing collection.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDuplicateOwnedItemToVectorTest()
		{
			// Make new annotation.
			var ann = Cache.ServiceLocator.GetInstance<ICmBaseAnnotationFactory>().Create();
			Cache.LanguageProject.AnnotationsOC.Add(ann);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoObjectDeleted, ann.Hvo);
			Assert.AreNotEqual((int)SpecialHVOValues.kHvoUninitializedObject, ann.Hvo);

			Cache.LanguageProject.AnnotationsOC.Add(ann); // Should be happy.
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempting to set the nth (even if n == 0) element of an empty list should throw an
		/// ArgumentOutOfRangeException. If this seems too obvious to need a test, note that the
		/// implementation in FdoList has been wrong for a long time.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void IndexedSetter_EmptyList()
		{
			IFdoServiceLocator servLoc = Cache.ServiceLocator;
			Cache.LangProject.CheckListsOC.Add(servLoc.GetInstance<ICmPossibilityListFactory>().Create());
			Cache.LangProject.CheckListsOC.First().PossibilitiesOS[0] =
				servLoc.GetInstance<ICmPossibilityFactory>().Create();
		}
	}
	#endregion

}
