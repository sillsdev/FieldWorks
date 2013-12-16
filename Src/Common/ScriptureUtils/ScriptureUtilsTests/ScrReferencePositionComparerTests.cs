// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrReferencePositionComparerTests.cs
// Responsibility: TE Team

using System;
using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.Common.ScriptureUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests the ScrReferencePositionComparer class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrReferencePositionComparerTests : BaseTest
	{
		private ScrReferencePositionComparer m_comparer;
		private DummyScrProjMetaDataProvider m_mdProvider = new DummyScrProjMetaDataProvider();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Setup the test fixture.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_comparer = new ScrReferencePositionComparer(m_mdProvider, true);
			ScrReferenceTests.InitializeScrReferenceForTests();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing differing verses in the same book and chapter.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareRefPos_DifferentVerse()
		{
			// Verse references are different.
			Assert.Greater(m_comparer.Compare(new ReferencePositionType(01001010, 0, 0, 15),
				new ReferencePositionType(01001002, 0, 0, 0)), 0);
			Assert.Less(m_comparer.Compare(new ReferencePositionType(01001002, 0, 0, 0),
				new ReferencePositionType(01001010, 0, 0, 15)), 0);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing the same verse in the same book and chapter when the character offset
		/// is different.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareRefPosStrings_SameVerseDiffIch()
		{
			// Verse references are the same, but the character offset is different.
			Assert.Greater(m_comparer.Compare(new ReferencePositionType(01001010, 0, 0, 15),
				new ReferencePositionType(01001010, 0, 0, 0)), 0);
			Assert.Less(m_comparer.Compare(new ReferencePositionType(01001010, 0, 0, 0),
				new ReferencePositionType(01001010, 0, 0, 15)), 0);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing the same verse in the same book and chapter when the paragraph index
		/// is different.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareRefPosStrings_SameVerseDiffPara()
		{
			// Verse references are the same but different paragraph indices.
			Assert.Greater(m_comparer.Compare(new ReferencePositionType(01001010, 0, 1, 0),
				new ReferencePositionType(01001010, 0, 0, 15)), 0);
			Assert.Less(m_comparer.Compare(new ReferencePositionType(01001010, 0, 0, 15),
				new ReferencePositionType(01001010, 0, 1, 0)), 0);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing the same verse in the same book and chapter when the section index
		/// is different.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareRefPosStrings_SameVerseDiffSection()
		{
			// Verse references are the same but different section indices.
			Assert.Greater(m_comparer.Compare(new ReferencePositionType(01001010, 1, 0, 0),
				new ReferencePositionType(01001010, 0, 2, 15)), 0);
			Assert.Less(m_comparer.Compare(new ReferencePositionType(01001010, 0, 2, 15),
				new ReferencePositionType(01001010, 1, 0, 0)), 0);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests comparing the same verse in the same book and chapter in the same section and
		/// paragraph and at the same character index.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CompareRefPosStrings_Same()
		{
			Assert.AreEqual(m_comparer.Compare(new ReferencePositionType(01001010, 3, 2, 15),
				new ReferencePositionType(01001010, 3, 2, 15)), 0);
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
			// This was using new CmObject, which made the whole assembly depend on FDO.
			// There are cheaper ways to test some non reference object (I, RBR, hope).
			m_comparer.Compare(01001001, new ArgumentException());
		}
	}
}
