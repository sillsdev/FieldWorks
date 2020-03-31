// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using LanguageExplorer;
using NUnit.Framework;

namespace LanguageExplorerTests
{
	[TestFixture]
	public class ManagedLgIcuCollatorTests
	{
		private static ManagedLgIcuCollator ManagedLgIcuCollatorInitializerHelper()
		{
			var icuCollator = new ManagedLgIcuCollator();
			icuCollator.Open("en");
			return icuCollator;
		}

		[Test]
		public void OpenTest()
		{
			using (var icuCollator = (IDisposable)ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);
			}
		}

		[Test]
		public void GeneralTestWithTwoDifferentWs()
		{
			using (var icuCollator1 = new ManagedLgIcuCollator())
			{
				icuCollator1.Open("en");
				icuCollator1.SortKeyVariant("test");
				using (var icuCollator2 = new ManagedLgIcuCollator())
				{
					icuCollator2.Open("fr");
					icuCollator2.SortKeyVariant("test");
				}
			}
		}

		[Test]
		public void CloseTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);
				icuCollator.Close();
			}
		}

		[Test]
		public void GetSortKeyVariantTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);
				var obj = icuCollator.SortKeyVariant("abc");
				Assert.IsNotNull(obj);

				icuCollator.Close();
			}
		}

		[Test]
		public void SortKeyVariantTestWithValues()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);
				var bytes = (byte[])icuCollator.SortKeyVariant("action");
				Assert.That(bytes[0], Is.EqualTo(41));
				Assert.That(bytes[1], Is.EqualTo(45));
				Assert.That(bytes[2], Is.EqualTo(79));
				Assert.That(bytes[3], Is.EqualTo(57));
				Assert.That(bytes[4], Is.EqualTo(69));
				Assert.That(bytes[5], Is.EqualTo(67));
				Assert.That(bytes[6], Is.EqualTo(1));
				Assert.That(bytes[7], Is.EqualTo(10));
				Assert.That(bytes[8], Is.EqualTo(1));
				Assert.That(bytes[9], Is.EqualTo(10));
				Assert.That(bytes[10], Is.EqualTo(0));
			}
		}


		[Test]
		public void CompareVariantTest1()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);
				var obj1 = icuCollator.SortKeyVariant("abc");
				var obj2 = obj1;
				var obj3 = icuCollator.SortKeyVariant("def");

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2) == 0, " obj1 == obj2");
				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj3) != 0, " obj1 != obj3");

				icuCollator.Close();
			}
		}

		[Test]
		public void CompareVariantTest2()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);
				var obj1 = icuCollator.SortKeyVariant("action");
				var obj2 = icuCollator.SortKeyVariant("actiom");
				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2) != 0, " action != actionm");

				obj1 = icuCollator.SortKeyVariant("tenepa");
				obj2 = icuCollator.SortKeyVariant("tenepo");
				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2) != 0, " tenepa != tenepo");

				obj1 = icuCollator.SortKeyVariant("hello");
				obj2 = icuCollator.SortKeyVariant("hello");

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2) == 0, " hello == hello");

				obj1 = icuCollator.SortKeyVariant("tenepaa");
				obj2 = icuCollator.SortKeyVariant("tenepa");

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2) > 0, " tenepaa > tenepa");

				obj1 = icuCollator.SortKeyVariant("tenepa");
				obj2 = icuCollator.SortKeyVariant("tenepaa");

				Assert.IsTrue(icuCollator.CompareVariant(obj1, obj2) < 0, " tenepaa < tenepa");

				icuCollator.Close();
			}
		}

		[Test]
		public void CompareTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.IsNotNull(icuCollator);
				Assert.IsTrue(icuCollator.Compare(string.Empty, string.Empty) == 0);
				Assert.IsTrue(icuCollator.Compare("abc", "abc") == 0);
				Assert.IsTrue(icuCollator.Compare("abc", "def") != 0);
			}
		}
	}
}