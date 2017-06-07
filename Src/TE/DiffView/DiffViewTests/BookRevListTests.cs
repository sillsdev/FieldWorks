// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BookRevListTests.cs
// Responsibility: TE Team

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the <see cref="BookRevListTests"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class BookRevListTests : ScrInMemoryFdoTestBase
	{
		// member variables for testing
		private IScrBook m_genesisRevision;
		private BookRevList m_booksToMerge;

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the BookRevList.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void TestSetup()
		{
			base.TestSetup();
			m_booksToMerge = new BookRevList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void TestTearDown()
		{
			m_booksToMerge.Dispose();
			m_booksToMerge = null;
			m_genesisRevision = null;

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates test data needed by all tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = AddArchiveBookToMockedScripture(1, "Genesis");
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the AddVersion method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddVersion()
		{
			m_booksToMerge.AddVersion(m_genesisRevision);
			Assert.AreEqual(1, m_booksToMerge.Count);
			Assert.AreEqual(m_genesisRevision.Hvo, m_booksToMerge.BookRevs[0].Hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the RemoveVersion method
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveVersion()
		{
			m_booksToMerge.AddVersion(m_genesisRevision);
			Assert.AreEqual(1, m_booksToMerge.Count);
			m_booksToMerge.RemoveVersion(2);
			Assert.AreEqual(1, m_booksToMerge.Count,
				"Shouldn't have removed anything because Exodus isn't in the list");
			m_booksToMerge.RemoveVersion(1);
			Assert.AreEqual(0, m_booksToMerge.Count, "Should have removed Genesis");
		}
	}
}
