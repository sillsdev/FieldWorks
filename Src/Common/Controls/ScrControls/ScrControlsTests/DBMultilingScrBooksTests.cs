// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2002' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DBMultilingScrBooksTest.cs
// --------------------------------------------------------------------------------------------
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;

using SILUBS.SharedScrUtils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.Common.Controls.FwControls
{
	/// <summary>
	/// Test the <see cref="DBMultilingScrBooks"/> class.
	/// </summary>
	[TestFixture]
	public class DBMultilingScrBooksTest : ScrInMemoryFdoTestBase
	{
		private DBMultilingScrBooks m_mlscrBook;

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

			Assert.IsFalse(m_mlscrBook.IsBookAvailableInDb(1), "Genesis found");
			Assert.IsTrue("genesis" != m_mlscrBook.GetBookName(1).ToLower(), "Genesis found");
			Assert.IsFalse(m_mlscrBook.IsReferenceValid("GEN 1:4"),
				"GEN 1:4 said to be a valid Reference");

			Assert.IsTrue(m_mlscrBook.IsBookAvailableInDb(2), "Exodus not found");
			Assert.AreEqual("Exodus".ToLower(), m_mlscrBook.GetBookName(2).ToLower(),
				"Exodus found");
			Assert.IsTrue(m_mlscrBook.IsReferenceValid("EXO 1:2"),
				"EXO 1:2 said to be an invalid Reference");
		}
	}
}
