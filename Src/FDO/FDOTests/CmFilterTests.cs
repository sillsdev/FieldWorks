using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CmFilterTests : InMemoryFdoTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the CmFilter when having multiple cells (and'ing the results)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void BuildCmFilter()
		{
			// Setup test values
			Cache.LangProject.PeopleOA = new CmPossibilityList();
			ICmPossibility person1 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person1.ForeColor = 5;
			person1.BackColor = 10;

			// Setup filter
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = filtersCol.Add(new CmFilter());
			filter.ColumnInfo = CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidForeColor
				+ "|" + CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidBackColor;
			ICmRow row = filter.RowsOS.Append(new CmRow());
			ICmCell cell1 = row.CellsOS.Append(new DummyCmCell());
			cell1.Contents.Text = "= 5";
			ICmCell cell2 = row.CellsOS.Append(new DummyCmCell());
			cell2.Contents.Text = "= 1";

			// Check the result
			((CmFilter)filter).InitCriteria();
			Assert.IsFalse(((CmFilter)filter).MatchesCriteria(person1.Hvo));
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
			Cache.LangProject.PeopleOA = new CmPossibilityList();
			ICmPossibility person1 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person1.ForeColor = 5;
			person1.BackColor = 10;

			// Setup filter
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = filtersCol.Add(new CmFilter());
			filter.ColumnInfo = CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidForeColor
				+ "|" + CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidBackColor;
			ICmRow row = filter.RowsOS.Append(new CmRow());
			ICmCell cell1 = row.CellsOS.Append(new DummyCmCell());
			cell1.Contents.Text = "= 5";
			ICmCell cell2 = row.CellsOS.Append(new DummyCmCell());
			cell2.Contents.Text = "= 10";

			// Check the result
			((CmFilter)filter).InitCriteria();
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person1.Hvo));
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
			Cache.LangProject.PeopleOA = new CmPossibilityList();
			ICmPossibility person1 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person1.ForeColor = 4;
			ICmPossibility person2 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person2.ForeColor = 5;
			ICmPossibility person3 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person3.ForeColor = 10;

			// Setup filter
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = filtersCol.Add(new CmFilter());
			filter.ColumnInfo = CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidForeColor;
			ICmRow row = filter.RowsOS.Append(new CmRow());
			ICmCell cell1 = row.CellsOS.Append(new DummyCmCell());
			cell1.Contents.Text = ">= 5";

			// Check the result
			((CmFilter)filter).InitCriteria();
			Assert.IsFalse(((CmFilter)filter).MatchesCriteria(person1.Hvo));
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person2.Hvo));
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person3.Hvo));
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
			Cache.LangProject.PeopleOA = new CmPossibilityList();
			ICmPossibility person1 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person1.ForeColor = 4;
			ICmPossibility person2 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person2.ForeColor = 5;
			ICmPossibility person3 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person3.ForeColor = 10;

			// Setup filter
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = filtersCol.Add(new CmFilter());
			filter.ColumnInfo = CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidForeColor;
			ICmRow row = filter.RowsOS.Append(new CmRow());
			ICmCell cell1 = row.CellsOS.Append(new DummyCmCell());
			cell1.Contents.Text = "<= 5";

			// Check the result
			((CmFilter)filter).InitCriteria();
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person1.Hvo));
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person2.Hvo));
			Assert.IsFalse(((CmFilter)filter).MatchesCriteria(person3.Hvo));
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
			Cache.LangProject.PeopleOA = new CmPossibilityList();
			ICmPossibility person1 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person1.ForeColor = 1;
			ICmPossibility person2 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person2.ForeColor = 3;
			ICmPossibility person3 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person3.ForeColor = 5;
			ICmPossibility person4 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person4.ForeColor = 7;

			// Setup filter
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = filtersCol.Add(new CmFilter());
			filter.ColumnInfo = CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidForeColor +
				"|" + CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidForeColor;
			ICmRow row = filter.RowsOS.Append(new CmRow());
			ICmCell cell1 = row.CellsOS.Append(new DummyCmCell());
			cell1.Contents.Text = "<= 5";
			ICmCell cell2 = row.CellsOS.Append(new DummyCmCell());
			cell2.Contents.Text = ">= 3";

			// Check the result
			((CmFilter)filter).InitCriteria();
			Assert.IsFalse(((CmFilter)filter).MatchesCriteria(person1.Hvo));
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person2.Hvo));
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person3.Hvo));
			Assert.IsFalse(((CmFilter)filter).MatchesCriteria(person4.Hvo));
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
			Cache.LangProject.PeopleOA = new CmPossibilityList();
			ICmPossibility person1 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person1.ForeColor = 4;
			ICmPossibility person2 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person2.ForeColor = 5;
			ICmPossibility person3 = Cache.LangProject.PeopleOA.PossibilitiesOS.Append(new CmPerson());
			person3.ForeColor = 10;

			// Setup filter
			FdoOwningCollection<ICmFilter> filtersCol = Cache.LangProject.FiltersOC;
			ICmFilter filter = filtersCol.Add(new CmFilter());
			filter.ColumnInfo = CmPossibility.kClassId + "," + (int)CmPossibility.CmPossibilityTags.kflidForeColor;
			ICmRow row = filter.RowsOS.Append(new CmRow());
			ICmCell cell1 = row.CellsOS.Append(new DummyCmCell());
			cell1.Contents.Text = "= 5";

			// Check the result
			((CmFilter)filter).InitCriteria();
			Assert.IsFalse(((CmFilter)filter).MatchesCriteria(person1.Hvo));
			Assert.IsTrue(((CmFilter)filter).MatchesCriteria(person2.Hvo));
			Assert.IsFalse(((CmFilter)filter).MatchesCriteria(person3.Hvo));
		}
	}
}
