// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CmFilterTests : ScrInMemoryFdoTestBase
	{

		private ITsString makeString(string str)
		{
			return Cache.TsStrFactory.MakeString(str, Cache.DefaultVernWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CmFilter when having multiple cells (and'ing the results)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildCmFilter()
		{
			// Setup test values
			Cache.LangProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			ICmPossibility person1 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person1);
			person1.ForeColor = 5;
			person1.BackColor = 10;

			// Setup filter
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(filter);
			filter.ColumnInfo = CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidForeColor
				+ "|" + CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidBackColor;
			ICmRow row = Cache.ServiceLocator.GetInstance<ICmRowFactory>().Create();
			filter.RowsOS.Add(row);
			ICmCell cell1 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell1);
			cell1.Contents = makeString("= 1");
			ICmCell cell2 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell2);
			cell2.Contents = makeString("= 5");

			// Check the result
			filter.InitCriteria();
			Assert.IsFalse(filter.MatchesCriteria(person1.Hvo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CmFilter when having multiple cells (and'ing the results)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildCmFilter2()
		{
			// Setup test values
			Cache.LangProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			ICmPossibility person1 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person1);
			person1.ForeColor = 5;
			person1.BackColor = 10;

			// Setup filter
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(filter);
			filter.ColumnInfo = CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidForeColor
				+ "|" + CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidBackColor;
			ICmRow row = Cache.ServiceLocator.GetInstance<ICmRowFactory>().Create();
			filter.RowsOS.Add(row);
			ICmCell cell1 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell1);
			cell1.Contents =  makeString("= 5");
			ICmCell cell2 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell2);
			cell2.Contents =  makeString("= 10");

			// Check the result
			filter.InitCriteria();
			Assert.IsTrue(filter.MatchesCriteria(person1.Hvo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CmFilter for "greater than or equal to" comparisons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GreaterThanOrEqual()
		{
			// Setup test values
			Cache.LangProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			ICmPossibility person1 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person1);
			person1.ForeColor = 4;
			ICmPossibility person2 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person2);
			person2.ForeColor = 5;
			ICmPossibility person3 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person3);
			person3.ForeColor = 10;

			// Setup filter
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(filter);
			filter.ColumnInfo = CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidForeColor;
			ICmRow row = Cache.ServiceLocator.GetInstance<ICmRowFactory>().Create();
			filter.RowsOS.Add(row);
			ICmCell cell1 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell1);
			cell1.Contents =  makeString(">= 5");

			// Check the result
			filter.InitCriteria();
			Assert.IsFalse(filter.MatchesCriteria(person1.Hvo));
			Assert.IsTrue(filter.MatchesCriteria(person2.Hvo));
			Assert.IsTrue(filter.MatchesCriteria(person3.Hvo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CmFilter for "less than or equal to" comparisons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LessThanOrEqual()
		{
			// Setup test values
			Cache.LangProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			ICmPossibility person1 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person1);
			person1.ForeColor = 4;
			ICmPossibility person2 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person2);
			person2.ForeColor = 5;
			ICmPossibility person3 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person3);
			person3.ForeColor = 10;

			// Setup filter
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(filter);
			filter.ColumnInfo = CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidForeColor;
			ICmRow row = Cache.ServiceLocator.GetInstance<ICmRowFactory>().Create();
			filter.RowsOS.Add(row);
			ICmCell cell1 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell1);
			cell1.Contents=  makeString("<= 5");

			// Check the result
			filter.InitCriteria();
			Assert.IsTrue(filter.MatchesCriteria(person1.Hvo));
			Assert.IsTrue(filter.MatchesCriteria(person2.Hvo));
			Assert.IsFalse(filter.MatchesCriteria(person3.Hvo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CmFilter for "less than or equal to" AND "greater than or equal to"
		/// comparisons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Range()
		{
			// Setup test values
			Cache.LangProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			ICmPossibility person1 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person1);
			person1.ForeColor = 1;
			ICmPossibility person2 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person2);
			person2.ForeColor = 3;
			ICmPossibility person3 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person3);
			person3.ForeColor = 5;
			ICmPossibility person4 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person4);
			person4.ForeColor = 7;

			// Setup filter
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(filter);
			filter.ColumnInfo = CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidForeColor +
				"|" + CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidForeColor;
			ICmRow row = Cache.ServiceLocator.GetInstance<ICmRowFactory>().Create();
			filter.RowsOS.Add(row);
			ICmCell cell1 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell1);
			cell1.Contents = makeString("<= 5");
			ICmCell cell2 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell2);
			cell2.Contents = makeString(">= 3");

			// Check the result
			filter.InitCriteria();
			Assert.IsFalse(filter.MatchesCriteria(person1.Hvo));
			Assert.IsTrue(filter.MatchesCriteria(person2.Hvo));
			Assert.IsTrue(filter.MatchesCriteria(person3.Hvo));
			Assert.IsFalse(filter.MatchesCriteria(person4.Hvo));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CmFilter for equality comparisons.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equality()
		{
			// Setup test values
			Cache.LangProject.PeopleOA = Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().Create();
			ICmPossibility person1 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person1);
			person1.ForeColor = 4;
			ICmPossibility person2 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person2);
			person2.ForeColor = 5;
			ICmPossibility person3 = Cache.ServiceLocator.GetInstance<ICmPersonFactory>().Create();
			Cache.LangProject.PeopleOA.PossibilitiesOS.Add(person3);
			person3.ForeColor = 10;

			// Setup filter
			IFdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = Cache.ServiceLocator.GetInstance<ICmFilterFactory>().Create();
			filtersCol.Add(filter);
			filter.ColumnInfo = CmPossibilityTags.kClassId + "," + CmPossibilityTags.kflidForeColor;
			ICmRow row = Cache.ServiceLocator.GetInstance<ICmRowFactory>().Create();
			filter.RowsOS.Add(row);
			ICmCell cell1 = Cache.ServiceLocator.GetInstance<ICmCellFactory>().Create();
			row.CellsOS.Add(cell1);
			cell1.Contents=  makeString("= 5");

			// Check the result
			filter.InitCriteria();
			Assert.IsFalse(filter.MatchesCriteria(person1.Hvo));
			Assert.IsTrue(filter.MatchesCriteria(person2.Hvo));
			Assert.IsFalse(filter.MatchesCriteria(person3.Hvo));
		}
	}
}
