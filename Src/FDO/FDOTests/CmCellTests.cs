// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CmCellTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region DummyCmCell class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Exposes protected members of the <see cref="CmCell"/> class for testing.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyCmCell : CmCell
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the comparison type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CmCell.ComparisionTypes ComparisionType
		{
			get { return m_comparisonType; }
			set { m_comparisonType = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the match sub-items flag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IncludeSubitems
		{
			get { return m_matchSubitems; }
			set { m_matchSubitems = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the match empty flag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool MatchEmpty
		{
			get { return m_matchEmpty; }
			set { m_matchEmpty = value; }
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the <see cref="CmCell"/> class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CmCellTests : InMemoryFdoTestBase
	{
		#region Data members
		private DummyCmCell m_cell;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement CreateTestData, called by InMemoryFdoTestBase set up.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			// Set up a filter, with a CmCell we can test on.
			m_inMemoryCache.InitializeAnnotationDefs();
			m_inMemoryCache.InitializeAnnotationCategories();

			CmFilter filter = new CmFilter();
			Cache.LangProject.FiltersOC.Add(filter);
			CmRow row = new CmRow();
			filter.RowsOS.Append(row);
			m_cell = new DummyCmCell();
			row.CellsOS.Append(m_cell);
		}

		#region ParseIntegerMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseIntegerMatchCriteria method when ???
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseIntegerMatchCriteria_Equality()
		{
			CheckDisposed();

			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents.UnderlyingTsString = factory.MakeString("= 0", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kEquals, m_cell.ComparisionType);
			Assert.AreEqual(0, m_cell.MatchValue);

			// repeat test with a different value
			m_cell.Contents.UnderlyingTsString = factory.MakeString("= 1", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kEquals, m_cell.ComparisionType);
			Assert.AreEqual(1, m_cell.MatchValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseIntegerMatchCriteria method when ???
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseIntegerMatchCriteria_GreaterThanEqual()
		{
			CheckDisposed();

			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents.UnderlyingTsString = factory.MakeString(">= 5", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kGreaterThanEqual, m_cell.ComparisionType);
			Assert.AreEqual(5, m_cell.MatchValue);

			// repeat test with a different value
			m_cell.Contents.UnderlyingTsString = factory.MakeString(">= 1", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kGreaterThanEqual, m_cell.ComparisionType);
			Assert.AreEqual(1, m_cell.MatchValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseIntegerMatchCriteria method when ???
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseIntegerMatchCriteria_LessThanEqual()
		{
			CheckDisposed();

			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents.UnderlyingTsString = factory.MakeString("<= 5", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kLessThanEqual, m_cell.ComparisionType);
			Assert.AreEqual(5, m_cell.MatchValue);

			// repeat test with a different value
			m_cell.Contents.UnderlyingTsString = factory.MakeString("<= 1", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kLessThanEqual, m_cell.ComparisionType);
			Assert.AreEqual(1, m_cell.MatchValue);
		}
		#endregion

		#region BuildIntegerMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildIntegerMatchCriteria method for equality match
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildIntegerMatchCriteria_Equality()
		{
			CheckDisposed();

			m_cell.BuildIntegerMatchCriteria(CmCell.ComparisionTypes.kEquals, 0, 0);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString("= 0", Cache.DefaultUserWs),
				m_cell.Contents.UnderlyingTsString);

			// repeat test with a different value
			m_cell.BuildIntegerMatchCriteria(CmCell.ComparisionTypes.kEquals, 1, 0);

			//verify the result
			AssertEx.AreTsStringsEqual(factory.MakeString("= 1", Cache.DefaultUserWs),
				m_cell.Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildIntegerMatchCriteria method for "greater than or equal to" match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildIntegerMatchCriteria_GreaterThanOrEqual()
		{
			CheckDisposed();

			m_cell.BuildIntegerMatchCriteria(CmCell.ComparisionTypes.kGreaterThanEqual, 5, 0);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString(">= 5", Cache.DefaultUserWs),
				m_cell.Contents.UnderlyingTsString);

			// repeat test with a different value
			m_cell.BuildIntegerMatchCriteria(CmCell.ComparisionTypes.kGreaterThanEqual, 10, 0);

			//verify the result
			AssertEx.AreTsStringsEqual(factory.MakeString(">= 10", Cache.DefaultUserWs),
				m_cell.Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildIntegerMatchCriteria method for "less than or equal to" match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildIntegerMatchCriteria_LessThanOrEqual()
		{
			CheckDisposed();

			m_cell.BuildIntegerMatchCriteria(CmCell.ComparisionTypes.kLessThanEqual, 5, 0);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString("<= 5", Cache.DefaultUserWs),
				m_cell.Contents.UnderlyingTsString);

			// repeat test with a different value
			m_cell.BuildIntegerMatchCriteria(CmCell.ComparisionTypes.kLessThanEqual, 10, 0);

			//verify the result
			AssertEx.AreTsStringsEqual(factory.MakeString("<= 10", Cache.DefaultUserWs),
				m_cell.Contents.UnderlyingTsString);
		}
		#endregion

		#region BuildObjectMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildObjectMatchCriteria method for match with no default object specified,
		/// excluding subitems, excluding empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_NoDefaultExcludeSubitemsExcludeEmpty()
		{
			CheckDisposed();

			m_cell.BuildObjectMatchCriteria(0, false, false);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString("Matches ", Cache.DefaultUserWs),
				m_cell.Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildObjectMatchCriteria method for match with default object specified,
		/// excluding subitems, excluding empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_WithDefaultExcludeSubitemsExcludeEmpty()
		{
			CheckDisposed();

			m_cell.BuildObjectMatchCriteria(m_inMemoryCache.m_categoryGrammar.Hvo, false, false);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildObjectMatchCriteria method for match with default object specified,
		/// excluding subitems, including empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_WithDefaultExcludeSubitemsIncludeEmpty()
		{
			CheckDisposed();

			m_cell.BuildObjectMatchCriteria(m_inMemoryCache.m_categoryGrammar.Hvo, false, true);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Empty or Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents.UnderlyingTsString);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildObjectMatchCriteria method for match with default object specified,
		/// including subitems, including empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_WithDefaultIncludeSubitemsIncludeEmpty()
		{
			CheckDisposed();

			m_cell.BuildObjectMatchCriteria(m_inMemoryCache.m_categoryGrammar.Hvo, true, true);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Empty or Matches  +subitems",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, 17, 17, Cache.DefaultUserWs);

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents.UnderlyingTsString);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildObjectMatchCriteria method for match with no default object specified,
		/// including subitems, excluding empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_NoDefaultIncludeSubitemsExcludeEmpty()
		{
			CheckDisposed();

			m_cell.BuildObjectMatchCriteria(0, true, false);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches +subitems", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents.UnderlyingTsString);
		}
		#endregion

		#region MatchesCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the MatchesCriteria method for an equality comparison.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MatchesCriteria_Int_Equals()
		{
			CheckDisposed();

			// Specify the matching criteria for this filter cell
			m_cell.ComparisionType = CmCell.ComparisionTypes.kEquals;
			m_cell.MatchValue = 9;

			Assert.IsTrue(m_cell.MatchesCriteria(9));
			Assert.IsFalse(m_cell.MatchesCriteria(8));
			Assert.IsFalse(m_cell.MatchesCriteria(10));
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the MatchesCriteria method when matching against an object with no subitem
		/// checking.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MatchesCriteria_AtomicObjectMatch_NotEmptyNoSubitems()
		{
			CheckDisposed();

			// Specify the matching criteria for this filter cell
			m_cell.ComparisionType = CmCell.ComparisionTypes.kMatches;
			m_cell.IncludeSubitems = false;
			m_cell.MatchEmpty = false;
			m_cell.MatchValue = m_inMemoryCache.m_consultantNoteDefn.Hvo;

			Assert.IsTrue(m_cell.MatchesCriteria(m_inMemoryCache.m_consultantNoteDefn.Hvo));
			Assert.IsFalse(m_cell.MatchesCriteria(m_inMemoryCache.m_translatorNoteDefn.Hvo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the MatchesCriteria method when matching against a vector of objects with no
		/// subitem checking.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MatchesCriteria_VectorObjectMatch_NotEmptyExcludeSubitems()
		{
			CheckDisposed();

			// Specify the matching criteria for this filter cell
			m_cell.ComparisionType = CmCell.ComparisionTypes.kMatches;
			m_cell.IncludeSubitems = false;
			m_cell.MatchEmpty = false;
			m_cell.MatchValue = m_inMemoryCache.m_categoryGrammar.Hvo;

			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryGrammar_PronominalRef.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {
				m_inMemoryCache.m_categoryGrammar_PronominalRef.Hvo,
				m_inMemoryCache.m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryDiscourse.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {}));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the MatchesCriteria method when matching against a vector of objects with
		/// subitem checking.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MatchesCriteria_VectorObjectMatch_NotEmptyIncludeSubitems()
		{
			CheckDisposed();

			// Specify the matching criteria for this filter cell
			m_cell.ComparisionType = CmCell.ComparisionTypes.kMatches;
			m_cell.IncludeSubitems = true;
			m_cell.MatchEmpty = false;
			m_cell.MatchValue = m_inMemoryCache.m_categoryGrammar.Hvo;

			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryGrammar.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryGrammar_PronominalRef.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {
				m_inMemoryCache.m_categoryGrammar_PronominalRef.Hvo,
				m_inMemoryCache.m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryDiscourse.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {}));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the MatchesCriteria method when matching against a vector of objects with no
		/// subitem checking and allowing empty vector to count as a match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void MatchesCriteria_VectorObjectMatch_EmptyExcludeSubitems()
		{
			CheckDisposed();

			// Specify the matching criteria for this filter cell
			m_cell.ComparisionType = CmCell.ComparisionTypes.kMatches;
			m_cell.IncludeSubitems = false;
			m_cell.MatchEmpty = true;
			m_cell.MatchValue = m_inMemoryCache.m_categoryGrammar.Hvo;

			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryGrammar_PronominalRef.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {
				m_inMemoryCache.m_categoryGrammar_PronominalRef.Hvo,
				m_inMemoryCache.m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_inMemoryCache.m_categoryDiscourse.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {}));
		}
		#endregion

		#region ParseObjectMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseObjectMatchCriteria method when no default value is specified and we don't
		/// want to match sub-items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseObjectMatchCriteria_MatchWithoutSubItemsNoDefault()
		{
			CheckDisposed();

			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents.UnderlyingTsString = factory.MakeString("Matches ", Cache.DefaultUserWs);

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kMatches, m_cell.ComparisionType);
			Assert.AreEqual(0, m_cell.MatchValue);
			Assert.IsFalse(m_cell.IncludeSubitems);
			Assert.IsFalse(m_cell.MatchEmpty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseObjectMatchCriteria method when a default value is specified and we don't
		/// want to match sub-items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseObjectMatchCriteria_MatchWithoutSubItemsDefaultSet()
		{
			CheckDisposed();

			// Set the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);
			m_cell.Contents.UnderlyingTsString = bldr.GetString();

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kMatches, m_cell.ComparisionType);
			Assert.AreEqual(m_inMemoryCache.m_categoryGrammar.Hvo, m_cell.MatchValue);
			Assert.IsFalse(m_cell.IncludeSubitems);
			Assert.IsFalse(m_cell.MatchEmpty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseObjectMatchCriteria method when a default value is specified and we want
		/// to match sub-items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseObjectMatchCriteria_MatchWithSubItemsDefaultSet()
		{
			CheckDisposed();

			// Set the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches  +subitems", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, 8, 8, Cache.DefaultUserWs);
			m_cell.Contents.UnderlyingTsString = bldr.GetString();

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kMatches, m_cell.ComparisionType);
			Assert.AreEqual(m_inMemoryCache.m_categoryGrammar.Hvo, m_cell.MatchValue);
			Assert.IsTrue(m_cell.IncludeSubitems);
			Assert.IsFalse(m_cell.MatchEmpty);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseObjectMatchCriteria method when matching empty or a default value and we
		/// don't want to match sub-items
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseObjectMatchCriteria_MatchEmptyOrObjectWithoutSubitems()
		{
			CheckDisposed();

			// Set the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Empty or Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_inMemoryCache.m_categoryDiscourse.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);
			m_cell.Contents.UnderlyingTsString = bldr.GetString();

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(CmCell.ComparisionTypes.kMatches, m_cell.ComparisionType);
			Assert.AreEqual(m_inMemoryCache.m_categoryDiscourse.Hvo, m_cell.MatchValue);
			Assert.IsFalse(m_cell.IncludeSubitems);
			Assert.IsTrue(m_cell.MatchEmpty);
		}
		#endregion

		#region BuildObjectMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test BuildObjectMatchCriteria method when ???
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This test has no content.")]
		public void BuildObjectMatchCriteria_()
		{
			CheckDisposed();

		}
		#endregion
	}
}
