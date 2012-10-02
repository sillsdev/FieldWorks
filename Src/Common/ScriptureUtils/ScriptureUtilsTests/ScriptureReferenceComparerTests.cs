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
// File: ScriptureReferenceComparerTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy implementation of IScrProjMetaDataProvider that returns a constant Versification
	/// value.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyScrProjMetaDataProvider : IScrProjMetaDataProvider
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current versification scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ScrVers Versification
		{
			get { return ScrVers.English; }
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ScriptureReferenceComparer class (and IComparable implementation of
	/// ScrReference)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScriptureReferenceComparerTests : BaseTest
	{
		private ScriptureReferenceComparer m_comparer;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the test fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_comparer = new ScriptureReferenceComparer(new DummyScrProjMetaDataProvider(), true);
			ScrReferenceTests.InitializeScrReferenceForTests();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing differing verses in the same book and chapter
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareVerseStrings()
		{
			Assert.Greater(m_comparer.Compare("GEN 1:10", "GEN 1:2"), 0);
			Assert.Less(m_comparer.Compare("GEN 1:2", "GEN 1:10"), 0);
			Assert.AreEqual(m_comparer.Compare("GEN 1:10", "GEN 1:10"), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing differing books.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareBookStrings()
		{
			Assert.Less(m_comparer.Compare("MAT 1:1", "MRK 1:1"), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing references in BCV form where the verses differ.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareBCVVerses()
		{
			Assert.Greater(m_comparer.Compare(01001010, 01001002), 0); // "GEN 1:10", "GEN 1:2"
			Assert.Less(m_comparer.Compare(01001002, 01001010), 0); // "GEN 1:2", "GEN 1:10"
			Assert.AreEqual(m_comparer.Compare(01001001, 01001001), 0); // "GEN 1:1", "GEN 1:1"
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing references in BCV form where the books differ.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareBCVBooks()
		{
			Assert.Less(m_comparer.Compare(01001001, 02001001), 0); // GEN 1:1, EXO 1:1
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing references in ScrReference form where the verses differ.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareScrRefVerses()
		{
			ScrReference gen1_10 = new ScrReference(1, 1, 10, ScrVers.English);
			ScrReference gen1_2 = new ScrReference(1, 1, 2, ScrVers.English);

			Assert.Greater(m_comparer.Compare(gen1_10, gen1_2), 0);
			Assert.Less(m_comparer.Compare(gen1_2, gen1_10), 0);
			Assert.AreEqual(m_comparer.Compare(gen1_10, new ScrReference(01001010, ScrVers.English)), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing references in ScrReference form where books differ.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CompareScrRefBooks()
		{
			ScrReference gen = new ScrReference(1, 1, 1, ScrVers.English);
			ScrReference exo = new ScrReference(2, 1, 1, ScrVers.English);

			Assert.Less(m_comparer.Compare(gen, exo), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that mixing different reference types work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MixedReferences()
		{
			ScrReference genesis = new ScrReference(1, 1, 1, ScrVers.English);

			Assert.Less(m_comparer.Compare("GEN 1:1", 02001001), 0);
			Assert.Greater(m_comparer.Compare("EXO 1:1", genesis), 0);
			Assert.AreEqual(m_comparer.Compare(01001001, genesis), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that comparing two references that represent titles works.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BookTitles()
		{
			ScrReference genesis = new ScrReference(1, 0, 0, ScrVers.English);
			ScrReference exodus = new ScrReference(2, 0, 0, ScrVers.English);

			Assert.AreEqual(m_comparer.Compare(genesis, 1000000), 0);
			Assert.Greater(m_comparer.Compare(exodus, genesis), 0);
			Assert.Less(m_comparer.Compare(exodus, 2001001), 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests trying to compare with a type that is not a reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void InvalidReferenceX()
		{
			m_comparer.Compare(067000999, 001001001);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests trying to compare with a type that is not a reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void InvalidReferenceY()
		{
			m_comparer.Compare(001001001, 067000999);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests trying to compare with a type that is not a reference
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void InvalidType()
		{
			m_comparer.Compare(01001001, Guid.NewGuid());
		}
	}
}
