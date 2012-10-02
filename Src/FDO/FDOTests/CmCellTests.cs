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

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for the CmCell class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CmCellTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		#region Data members
		private ICmCell m_cell;
		private ICmPossibility m_categoryDiscourse;
		private ICmPossibility m_categoryGrammar;
		private ICmPossibility m_categoryGrammar_PronominalRef;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implement CreateTestData, called by InMemoryFdoTestBase set up.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void CreateTestData()
		{
			var servloc = Cache.ServiceLocator;

			ICmPossibilityFactory possibilityFactory = servloc.GetInstance<ICmPossibilityFactory>();
			// Add an annotation category (for Discourse)
			m_categoryDiscourse = possibilityFactory.Create();
			var affixCatList = servloc.GetInstance<ICmPossibilityListFactory>().Create();
			Cache.LangProject.AffixCategoriesOA = affixCatList;
			affixCatList.PossibilitiesOS.Add(m_categoryDiscourse);

			// Add an annotation category (for Grammar)
			m_categoryGrammar = possibilityFactory.Create();
			affixCatList.PossibilitiesOS.Add(m_categoryGrammar);

			// add a sub-annotation category (for "Pronominal reference")
			m_categoryGrammar_PronominalRef = possibilityFactory.Create();
			m_categoryGrammar.SubPossibilitiesOS.Add(m_categoryGrammar_PronominalRef);

			// Set up a filter, with a CmCell we can test on.
			ICmFilter filter = servloc.GetInstance<ICmFilterFactory>().Create();
			Cache.LangProject.FiltersOC.Add(filter);
			ICmRow row = servloc.GetInstance<ICmRowFactory>().Create();
			filter.RowsOS.Add(row);
			m_cell = servloc.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(m_cell);
		}

		#region ParseIntegerMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseIntegerMatchCriteria method when testing for equality.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseIntegerMatchCriteria_Equality()
		{
			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents = factory.MakeString("= 0", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kEquals, m_cell.ComparisonType);
			Assert.AreEqual(0, m_cell.MatchValue);

			// repeat test with a different value
			m_cell.Contents = factory.MakeString("= 1", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kEquals, m_cell.ComparisonType);
			Assert.AreEqual(1, m_cell.MatchValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseIntegerMatchCriteria method when testing for greater-than or equal
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseIntegerMatchCriteria_GreaterThanEqual()
		{
			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents = factory.MakeString(">= 5", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kGreaterThanEqual, m_cell.ComparisonType);
			Assert.AreEqual(5, m_cell.MatchValue);

			// repeat test with a different value
			m_cell.Contents = factory.MakeString(">= 1", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kGreaterThanEqual, m_cell.ComparisonType);
			Assert.AreEqual(1, m_cell.MatchValue);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test ParseIntegerMatchCriteria method when testing for less-than or equal
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ParseIntegerMatchCriteria_LessThanEqual()
		{
			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents = factory.MakeString("<= 5", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kLessThanEqual, m_cell.ComparisonType);
			Assert.AreEqual(5, m_cell.MatchValue);

			// repeat test with a different value
			m_cell.Contents = factory.MakeString("<= 1", Cache.DefaultUserWs);

			m_cell.ParseIntegerMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kLessThanEqual, m_cell.ComparisonType);
			Assert.AreEqual(1, m_cell.MatchValue);
		}
		#endregion

		#region SetIntegerMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetIntegerMatchCriteria method for equality match
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildIntegerMatchCriteria_Equality()
		{
			m_cell.SetIntegerMatchCriteria(ComparisonTypes.kEquals, 0);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString("= 0", Cache.DefaultUserWs),
				m_cell.Contents);

			// repeat test with a different value
			m_cell.SetIntegerMatchCriteria(ComparisonTypes.kEquals, 1);

			//verify the result
			AssertEx.AreTsStringsEqual(factory.MakeString("= 1", Cache.DefaultUserWs),
				m_cell.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetIntegerMatchCriteria method for "greater than or equal to" match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildIntegerMatchCriteria_GreaterThanOrEqual()
		{
			m_cell.SetIntegerMatchCriteria(ComparisonTypes.kGreaterThanEqual, 5);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString(">= 5", Cache.DefaultUserWs),
				m_cell.Contents);

			// repeat test with a different value
			m_cell.SetIntegerMatchCriteria(ComparisonTypes.kGreaterThanEqual, 10);

			//verify the result
			AssertEx.AreTsStringsEqual(factory.MakeString(">= 10", Cache.DefaultUserWs),
				m_cell.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetIntegerMatchCriteria method for "less than or equal to" match.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildIntegerMatchCriteria_LessThanOrEqual()
		{
			m_cell.SetIntegerMatchCriteria(ComparisonTypes.kLessThanEqual, 5);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString("<= 5", Cache.DefaultUserWs),
				m_cell.Contents);

			// repeat test with a different value
			m_cell.SetIntegerMatchCriteria(ComparisonTypes.kLessThanEqual, 10);

			//verify the result
			AssertEx.AreTsStringsEqual(factory.MakeString("<= 10", Cache.DefaultUserWs),
				m_cell.Contents);
		}
		#endregion

		#region SetObjectMatchCriteria Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetObjectMatchCriteria method for match with no default object specified,
		/// excluding subitems, excluding empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_NoDefaultExcludeSubitemsExcludeEmpty()
		{
			m_cell.SetObjectMatchCriteria(null, false, false);

			//verify the result
			ITsStrFactory factory = TsStrFactoryClass.Create();
			AssertEx.AreTsStringsEqual(factory.MakeString("Matches ", Cache.DefaultUserWs),
				m_cell.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetObjectMatchCriteria method for match with default object specified,
		/// excluding subitems, excluding empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_WithDefaultExcludeSubitemsExcludeEmpty()
		{
			m_cell.SetObjectMatchCriteria(m_categoryGrammar, false, false);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetObjectMatchCriteria method for match with default object specified,
		/// excluding subitems, including empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_WithDefaultExcludeSubitemsIncludeEmpty()
		{
			m_cell.SetObjectMatchCriteria(m_categoryGrammar, false, true);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Empty or Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetObjectMatchCriteria method for match with default object specified,
		/// including subitems, including empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_WithDefaultIncludeSubitemsIncludeEmpty()
		{
			m_cell.SetObjectMatchCriteria(m_categoryGrammar, true, true);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Empty or Matches  +subitems",
				StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, 17, 17, Cache.DefaultUserWs);

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test SetObjectMatchCriteria method for match with no default object specified,
		/// including subitems, excluding empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildObjectMatchCriteria_NoDefaultIncludeSubitemsExcludeEmpty()
		{
			m_cell.SetObjectMatchCriteria(null, true, false);

			//verify the result
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches +subitems", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));

			AssertEx.AreTsStringsEqual(bldr.GetString(), m_cell.Contents);
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
			m_cell.SetIntegerMatchCriteria(ComparisonTypes.kEquals, 9);

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
			ICmAnnotationDefnRepository annDefnRepo =
				Cache.ServiceLocator.GetInstance<ICmAnnotationDefnRepository>();
			ICmAnnotationDefn consultantNoteDefn = annDefnRepo.ConsultantAnnotationDefn;
			ICmAnnotationDefn translatorNoteDefn = annDefnRepo.TranslatorAnnotationDefn;

			m_cell.SetObjectMatchCriteria(consultantNoteDefn, false, false);

			Assert.IsTrue(m_cell.MatchesCriteria(consultantNoteDefn.Hvo));
			Assert.IsFalse(m_cell.MatchesCriteria(translatorNoteDefn.Hvo));
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
			m_cell.SetObjectMatchCriteria(m_categoryGrammar, false, false);

			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_categoryGrammar_PronominalRef.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {
				m_categoryGrammar_PronominalRef.Hvo,
				m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_categoryDiscourse.Hvo}));
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
			m_cell.SetObjectMatchCriteria(m_categoryGrammar, true, false);

			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_categoryGrammar.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_categoryGrammar_PronominalRef.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {
				m_categoryGrammar_PronominalRef.Hvo,
				m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_categoryDiscourse.Hvo}));
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
			m_cell.SetObjectMatchCriteria(m_categoryGrammar, false, true);

			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_categoryGrammar_PronominalRef.Hvo}));
			Assert.IsTrue(m_cell.MatchesCriteria(new int[] {
				m_categoryGrammar_PronominalRef.Hvo,
				m_categoryGrammar.Hvo}));
			Assert.IsFalse(m_cell.MatchesCriteria(new int[] {m_categoryDiscourse.Hvo}));
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
			// Set the matching criteria for this filter cell
			ITsStrFactory factory = TsStrFactoryClass.Create();
			m_cell.Contents = factory.MakeString("Matches ", Cache.DefaultUserWs);

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kMatches, m_cell.ComparisonType);
			Assert.AreEqual(0, m_cell.MatchValue);
			Assert.IsFalse((bool)ReflectionHelper.GetField(m_cell, "m_matchSubitems"));
			Assert.IsFalse((bool)ReflectionHelper.GetField(m_cell, "m_matchEmpty"));
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
			// Set the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);
			m_cell.Contents = bldr.GetString();

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kMatches, m_cell.ComparisonType);
			Assert.AreEqual(m_categoryGrammar.Hvo, m_cell.MatchValue);
			Assert.IsFalse((bool)ReflectionHelper.GetField(m_cell, "m_matchSubitems"));
			Assert.IsFalse((bool)ReflectionHelper.GetField(m_cell, "m_matchEmpty"));
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
			// Set the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Matches  +subitems", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_categoryGrammar.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, 8, 8, Cache.DefaultUserWs);
			m_cell.Contents = bldr.GetString();

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kMatches, m_cell.ComparisonType);
			Assert.AreEqual(m_categoryGrammar.Hvo, m_cell.MatchValue);
			Assert.IsTrue((bool)ReflectionHelper.GetField(m_cell, "m_matchSubitems"));
			Assert.IsFalse((bool)ReflectionHelper.GetField(m_cell, "m_matchEmpty"));
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
			// Set the matching criteria for this filter cell
			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, "Empty or Matches ", StyleUtils.CharStyleTextProps(null, Cache.DefaultUserWs));
			StringUtils.InsertOrcIntoPara(m_categoryDiscourse.Guid,
				FwObjDataTypes.kodtNameGuidHot, bldr, bldr.Length,
				bldr.Length, Cache.DefaultUserWs);
			m_cell.Contents = bldr.GetString();

			m_cell.ParseObjectMatchCriteria();

			Assert.AreEqual(ComparisonTypes.kMatches, m_cell.ComparisonType);
			Assert.AreEqual(m_categoryDiscourse.Hvo, m_cell.MatchValue);
			Assert.IsFalse((bool)ReflectionHelper.GetField(m_cell, "m_matchSubitems"));
			Assert.IsTrue((bool)ReflectionHelper.GetField(m_cell, "m_matchEmpty"));
		}
		#endregion
	}
}
