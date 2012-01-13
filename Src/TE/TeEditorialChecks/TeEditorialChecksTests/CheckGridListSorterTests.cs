using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Windows.Forms;
using SIL.Utils;
using NUnit.Framework;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// <summary></summary>
	internal enum dummyEnum : int
	{
		One = 0,
		Two = 1,
		Three = 2,
	}

	#region DummyEnumComparer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyEnumComparer : IComparer
	{
		#region IComparer Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			int ix = (int)x;
			int iy = (int)y;
			return ix - iy;
		}

		#endregion
	}

	#endregion

	#region DummyListItem class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyListItem : ICheckGridRowObject
	{
		internal string StrProp = null;
		internal int IntProp = 0;
		internal DateTime DateProp = DateTime.MinValue;
		internal dummyEnum EnumProp = dummyEnum.One;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the value for the specified property. I would use reflection but that takes
		/// more overhead since this will be used in sorting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public object GetPropValue(string propName)
		{
			switch (propName)
			{
				case "EnumProp": return EnumProp;
				case "DateProp": return DateProp;
				case "IntProp": return IntProp;
				case "StrProp": return StrProp;
			}

			return null;
		}
	}

	#endregion

	#region GenericComparer class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements a generic comparing class in which the two objects being compared must
	/// derive from IComparable to be of much use.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class GenericComparer : IComparer
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Compare(object x, object y)
		{
			IComparable xc = x as IComparable;
			IComparable yc = y as IComparable;
			return (xc == null || yc == null ? 0 : xc.CompareTo(yc));
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[SetCulture("en-US")]
	public class CheckGridListSorterTests
	{
		private List<ICheckGridRowObject> m_list;
		CheckGridListSorter m_cgSorter;
		GenericComparer m_genericComparer;

		#region Test setup and teardown
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public void Initialize()
		{
			m_genericComparer = new GenericComparer();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public void Exit()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetupTestList()
		{
			m_list = new List<ICheckGridRowObject>();
			m_cgSorter = new CheckGridListSorter(m_list);

			DummyListItem item = new DummyListItem();
			item.StrProp = "item1";
			item.IntProp = 500;
			item.DateProp = new DateTime(2008, 1, 21);
			item.EnumProp = dummyEnum.One;
			m_list.Add(item);

			item = new DummyListItem();
			item.StrProp = "item3";
			item.IntProp = 100;
			item.DateProp = new DateTime(2008, 1, 23);
			item.EnumProp = dummyEnum.Three;
			m_list.Add(item);

			item = new DummyListItem();
			item.StrProp = "item4";
			item.IntProp = 200;
			item.DateProp = new DateTime(2008, 1, 24);
			item.EnumProp = dummyEnum.One;
			m_list.Add(item);

			item = new DummyListItem();
			item.StrProp = "item2";
			item.IntProp = 800;
			item.DateProp = new DateTime(2008, 1, 24);
			item.EnumProp = dummyEnum.Two;
			m_list.Add(item);

			m_cgSorter.AddComparer("StrProp", StringComparer.Ordinal);
			m_cgSorter.AddComparer("IntProp", m_genericComparer);
			m_cgSorter.AddComparer("DateProp", m_genericComparer);
			m_cgSorter.AddComparer("EnumProp", new DummyEnumComparer());
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestSetFirstSortPropNameMethod()
		{
			m_cgSorter = new CheckGridListSorter(new List<ICheckGridRowObject>());

			List<StableSortInfo> ssOrders =
				ReflectionHelper.GetField(m_cgSorter, "m_stableOrder") as List<StableSortInfo>;

			Assert.AreEqual(0, ssOrders.Count);

			m_cgSorter.SetFirstSortPropName("prop3", false);
			m_cgSorter.SetFirstSortPropName("prop2", false);
			m_cgSorter.SetFirstSortPropName("prop1", false);

			Assert.AreEqual(3, ssOrders.Count);
			Assert.AreEqual("prop1", ssOrders[0].PropName);
			Assert.AreEqual(SortOrder.Ascending, ssOrders[0].SortDirection);
			Assert.AreEqual("prop2", ssOrders[1].PropName);
			Assert.AreEqual(SortOrder.Ascending, ssOrders[1].SortDirection);
			Assert.AreEqual("prop3", ssOrders[2].PropName);
			Assert.AreEqual(SortOrder.Ascending, ssOrders[2].SortDirection);

			m_cgSorter.SetFirstSortPropName("prop2", true);
			Assert.AreEqual("prop2", ssOrders[0].PropName);
			Assert.AreEqual(SortOrder.Descending, ssOrders[0].SortDirection);
			Assert.AreEqual("prop1", ssOrders[1].PropName);
			Assert.AreEqual(SortOrder.Ascending, ssOrders[1].SortDirection);
			Assert.AreEqual("prop3", ssOrders[2].PropName);
			Assert.AreEqual(SortOrder.Ascending, ssOrders[2].SortDirection);

			m_cgSorter.SetFirstSortPropName("prop3", true);
			Assert.AreEqual("prop3", ssOrders[0].PropName);
			Assert.AreEqual(SortOrder.Descending, ssOrders[0].SortDirection);
			Assert.AreEqual("prop2", ssOrders[1].PropName);
			Assert.AreEqual(SortOrder.Descending, ssOrders[1].SortDirection);
			Assert.AreEqual("prop1", ssOrders[2].PropName);
			Assert.AreEqual(SortOrder.Ascending, ssOrders[2].SortDirection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SortTest1()
		{
			SetupTestList();

			m_cgSorter.Sort("StrProp", false);

			Assert.AreEqual("item1", ((DummyListItem)m_list[0]).StrProp);
			Assert.AreEqual("item2", ((DummyListItem)m_list[1]).StrProp);
			Assert.AreEqual("item3", ((DummyListItem)m_list[2]).StrProp);
			Assert.AreEqual("item4", ((DummyListItem)m_list[3]).StrProp);

			Assert.AreEqual(500, ((DummyListItem)m_list[0]).IntProp);
			Assert.AreEqual(800, ((DummyListItem)m_list[1]).IntProp);
			Assert.AreEqual(100, ((DummyListItem)m_list[2]).IntProp);
			Assert.AreEqual(200, ((DummyListItem)m_list[3]).IntProp);

			Assert.AreEqual(dummyEnum.One, ((DummyListItem)m_list[0]).EnumProp);
			Assert.AreEqual(dummyEnum.Two, ((DummyListItem)m_list[1]).EnumProp);
			Assert.AreEqual(dummyEnum.Three, ((DummyListItem)m_list[2]).EnumProp);
			Assert.AreEqual(dummyEnum.One, ((DummyListItem)m_list[3]).EnumProp);

			Assert.AreEqual("01/21/2008", ((DummyListItem)m_list[0]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/24/2008", ((DummyListItem)m_list[1]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/23/2008", ((DummyListItem)m_list[2]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/24/2008", ((DummyListItem)m_list[3]).DateProp.ToString("MM/dd/yyyy"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SortTest2()
		{
			SetupTestList();

			m_cgSorter.SetFirstSortPropName("StrProp", false);
			m_cgSorter.Sort("EnumProp", false);

			Assert.AreEqual("item1", ((DummyListItem)m_list[0]).StrProp);
			Assert.AreEqual("item4", ((DummyListItem)m_list[1]).StrProp);
			Assert.AreEqual("item2", ((DummyListItem)m_list[2]).StrProp);
			Assert.AreEqual("item3", ((DummyListItem)m_list[3]).StrProp);

			Assert.AreEqual(500, ((DummyListItem)m_list[0]).IntProp);
			Assert.AreEqual(200, ((DummyListItem)m_list[1]).IntProp);
			Assert.AreEqual(800, ((DummyListItem)m_list[2]).IntProp);
			Assert.AreEqual(100, ((DummyListItem)m_list[3]).IntProp);

			Assert.AreEqual(dummyEnum.One, ((DummyListItem)m_list[0]).EnumProp);
			Assert.AreEqual(dummyEnum.One, ((DummyListItem)m_list[1]).EnumProp);
			Assert.AreEqual(dummyEnum.Two, ((DummyListItem)m_list[2]).EnumProp);
			Assert.AreEqual(dummyEnum.Three, ((DummyListItem)m_list[3]).EnumProp);

			Assert.AreEqual("01/21/2008", ((DummyListItem)m_list[0]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/24/2008", ((DummyListItem)m_list[1]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/24/2008", ((DummyListItem)m_list[2]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/23/2008", ((DummyListItem)m_list[3]).DateProp.ToString("MM/dd/yyyy"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SortTest3()
		{
			SetupTestList();

			m_cgSorter.SetFirstSortPropName("StrProp", false);
			m_cgSorter.SetFirstSortPropName("DateProp", false);
			m_cgSorter.Sort("DateProp", true);

			Assert.AreEqual("item2", ((DummyListItem)m_list[0]).StrProp);
			Assert.AreEqual("item4", ((DummyListItem)m_list[1]).StrProp);
			Assert.AreEqual("item3", ((DummyListItem)m_list[2]).StrProp);
			Assert.AreEqual("item1", ((DummyListItem)m_list[3]).StrProp);

			Assert.AreEqual(800, ((DummyListItem)m_list[0]).IntProp);
			Assert.AreEqual(200, ((DummyListItem)m_list[1]).IntProp);
			Assert.AreEqual(100, ((DummyListItem)m_list[2]).IntProp);
			Assert.AreEqual(500, ((DummyListItem)m_list[3]).IntProp);

			Assert.AreEqual(dummyEnum.Two, ((DummyListItem)m_list[0]).EnumProp);
			Assert.AreEqual(dummyEnum.One, ((DummyListItem)m_list[1]).EnumProp);
			Assert.AreEqual(dummyEnum.Three, ((DummyListItem)m_list[2]).EnumProp);
			Assert.AreEqual(dummyEnum.One, ((DummyListItem)m_list[3]).EnumProp);

			Assert.AreEqual("01/24/2008", ((DummyListItem)m_list[0]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/24/2008", ((DummyListItem)m_list[1]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/23/2008", ((DummyListItem)m_list[2]).DateProp.ToString("MM/dd/yyyy"));
			Assert.AreEqual("01/21/2008", ((DummyListItem)m_list[3]).DateProp.ToString("MM/dd/yyyy"));
		}
	}
}
