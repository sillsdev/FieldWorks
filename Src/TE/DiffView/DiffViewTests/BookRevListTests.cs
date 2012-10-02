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
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
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
		private IScrBook m_genesis;
		private IScrBook m_genesisRevision;
		private BookRevList m_booksToMerge;

		#region Setup
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create the BookRevList.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			base.Initialize();
			m_booksToMerge = new BookRevList();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_booksToMerge.Dispose();
			m_booksToMerge = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows subclasses to do other stuff to initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void InitializeCache()
		{
			Cache.MapType(typeof(StTxtPara), typeof(ScrTxtPara));
			Cache.MapType(typeof(StFootnote), typeof(ScrFootnote));
			base.InitializeCache();
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
				if (m_booksToMerge != null)
					m_booksToMerge.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_booksToMerge = null;
			m_genesis = null;
			m_genesisRevision = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates test data needed by all tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			m_genesis = m_scrInMemoryCache.AddBookToMockedScripture(1, "Genesis");
			m_genesisRevision = m_scrInMemoryCache.AddArchiveBookToMockedScripture(1, "Genesis");
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
			CheckDisposed();

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
			CheckDisposed();

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
