// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: BookRevListTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Diagnostics;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Controls;

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
