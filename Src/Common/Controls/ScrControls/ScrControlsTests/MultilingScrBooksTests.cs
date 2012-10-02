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
// File: MultilingScrBooksTest.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO;

using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Common.Controls.FwControls
{
	/// <summary>
	/// Test the <see cref="MultilingScrBooks"/> class.
	/// </summary>
	[TestFixture]
	public class MultilingScrBooksTest: ScrInMemoryFdoTestBase
	{
		private MultilingScrBooks m_mlscrBook;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialization called once.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			ScrReferenceTests.InitializeScrReferenceForTests();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setups this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			CreateExodusData();

			m_mlscrBook = new MultilingScrBooks((IScrProjMetaDataProvider)m_scr);
			m_mlscrBook.InitializeWritingSystems(Cache.WritingSystemFactory);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="MultilingScrBooks.GetBookAbbrev"/> method. Test with English
		/// (default) encoding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookAbbrev()
		{
			Assert.AreEqual("Gen", m_mlscrBook.GetBookAbbrev(ScriptureTags.kiOtMin));
			Assert.AreEqual("Mat", m_mlscrBook.GetBookAbbrev(ScriptureTags.kiNtMin));
			Assert.AreEqual("Rev", m_mlscrBook.GetBookAbbrev(ScriptureTags.kBibleLim));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the <see cref="MultilingScrBooks.GetBookAbbrev"/> method. Test with
		/// non-existing encoding.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetBookAbbrevDifferentEncoding()
		{
			List<int> array = new List<int>();
			array.Add(99); // some arbitrary should-never-exist value
			m_mlscrBook.RequestedEncodings = array;

			Assert.AreEqual("GEN", m_mlscrBook.GetBookAbbrev(ScriptureTags.kiOtMin));
			Assert.AreEqual("MAT", m_mlscrBook.GetBookAbbrev(ScriptureTags.kiNtMin));
			Assert.AreEqual("REV", m_mlscrBook.GetBookAbbrev(ScriptureTags.kBibleLim));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateReferencesWithoutDB()
		{
			Assert.AreEqual("Genesis".ToLower(), m_mlscrBook.GetBookName(1).ToLower(),
				"Genesis not found");
			Assert.IsTrue(m_mlscrBook.IsReferenceValid("GEN 1:4"),
				"GEN 1:4 said to be an invalid Reference");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ValidateReferencesWithDB()
		{
			m_mlscrBook = new DBMultilingScrBooks(m_scr);
			m_mlscrBook.InitializeWritingSystems(Cache.WritingSystemFactory);

			Assert.IsFalse(m_mlscrBook.IsBookAvailableInDb(1), "Genesis found");
			Assert.IsTrue("genesis" != m_mlscrBook.GetBookName(1).ToLower(), "Genesis found");
			Assert.IsFalse(m_mlscrBook.IsReferenceValid("GEN 1:4"),
				"GEN 1:4 said to be a valid Reference");

			Assert.IsTrue(m_mlscrBook.IsBookAvailableInDb(2), "Exodus not found");
			Assert.AreEqual("Exodus".ToLower(), m_mlscrBook.GetBookName(2).ToLower(),
				"Exodus found");
			Assert.IsTrue(m_mlscrBook.IsReferenceValid("EXO 1:2"),
				"EXO 1:2 said to be an invalid Reference");
			// These were removed because setting a reference to out of range values will
			// cause the values to be set to 1 and then the reference is valid.
//			Assert.IsFalse(mlscrBook.IsReferenceValid("JAS 50:1"),
//				"JAS 50:1 is valid Reference... go figure");
//			Assert.IsFalse(mlscrBook.IsReferenceValid("JAS 1:50"),
//				"JAS 1:50 is valid Reference... go figure");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString()
		{
			Assert.AreEqual(65001001, m_mlscrBook.ParseRefString("jud").BBCCCVVV);
			Assert.AreEqual(7001001, m_mlscrBook.ParseRefString("ju").BBCCCVVV);
			Assert.AreEqual(65001001, m_mlscrBook.ParseRefString("JUD").BBCCCVVV);
			Assert.AreEqual(7001001, m_mlscrBook.ParseRefString("jU").BBCCCVVV);
			Assert.AreEqual(65001001, m_mlscrBook.ParseRefString("Ju", 34).BBCCCVVV);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests parsing the full English name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_FullNameEnglish()
		{
			ScrReference judeRef = m_mlscrBook.ParseRefString("Jude 99:1");
			Assert.AreEqual(65099001, judeRef.BBCCCVVV);
			Assert.IsFalse(judeRef.Valid);
			ScrReference judgesRef = m_mlscrBook.ParseRefString("Judges 3:6");
			Assert.AreEqual(7003006, judgesRef.BBCCCVVV);
			Assert.IsTrue(judgesRef.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests parsing the English abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_AbbrEnglish()
		{
			ScrReference timRef = m_mlscrBook.ParseRefString("2Tim 1:5");
			Assert.AreEqual(55001005, timRef.BBCCCVVV);
			Assert.IsTrue(timRef.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests parsing the full Spanish name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_FullNameSpanish()
		{
			ScrReference timRef = m_mlscrBook.ParseRefString("2 Timoteo 1:5");
			Assert.AreEqual(55001005, timRef.BBCCCVVV);
			Assert.IsTrue(timRef.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests parsing the Spanish abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_AbbrSpanish()
		{
			ScrReference timRef = m_mlscrBook.ParseRefString("2 Tim 1:5");
			Assert.AreEqual(55001005, timRef.BBCCCVVV);
			Assert.IsTrue(timRef.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests parsing the SIL name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_SILName()
		{
			ScrReference timRef = m_mlscrBook.ParseRefString("2TI 1:5");
			Assert.AreEqual(55001005, timRef.BBCCCVVV);
			Assert.IsTrue(timRef.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests parsing the SIL abbreviation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_AbbrSIL()
		{
			ScrReference timRef = m_mlscrBook.ParseRefString("4T 1:5");
			Assert.AreEqual(55001005, timRef.BBCCCVVV);
			Assert.IsTrue(timRef.Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests edge cases when parsing a reference string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_EdgeCases()
		{
			Assert.AreEqual(42005015, m_mlscrBook.ParseRefString("Luk 5,15"));
			Assert.AreEqual(42005015, m_mlscrBook.ParseRefString("luk 5.15"));
			Assert.AreEqual(42005015, m_mlscrBook.ParseRefString("LUK5:15"));
			Assert.AreEqual(55001005, m_mlscrBook.ParseRefString("4T1:5"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test parsing invalid reference strings. We expect to get an invalid ScrReference,
		/// but not an exception.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefString_Invalid()
		{
			Assert.IsFalse(m_mlscrBook.ParseRefString(string.Empty).Valid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that passing in a null value as string to parse throws an NullReferenceException.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(NullReferenceException))]
		public void ParseRefString_NullArgument()
		{
			m_mlscrBook.ParseRefString(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ParseRefString method when called with a verse bridge
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseRefStringWithVerseBridge()
		{
			ScrReference scrRef = m_mlscrBook.ParseRefString("LUK 23:50-51");

			Assert.IsTrue(scrRef.Valid);
			Assert.AreEqual(42023050, scrRef.BBCCCVVV);
		}
	}
}
