// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using System;
using SIL.FieldWorks.Common.ViewsInterfaces;

namespace SIL.FieldWorks.Language
{
	[TestFixture()]
	public class ManagedLgIcuCollatorTests
	{
		protected ManagedLgIcuCollator ManagedLgIcuCollatorInitializerHelper()
		{
			var icuCollator = new ManagedLgIcuCollator();
			icuCollator.Open("en");
			return icuCollator;
		}

		[Test()]
		[Category("ByHand")]
		public void OpenTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
				Assert.That(icuCollator, Is.Not.Null);
		}

		[Test()]
		[Category("ByHand")]
		public void GeneralTestWithTwoDifferentWs()
		{
			using (var icuCollator1 = new ManagedLgIcuCollator())
			{
				icuCollator1.Open("en");
				var options = new LgCollatingOptions();
				object result1 = icuCollator1.get_SortKeyVariant("test", options);
				using (var icuCollator2 = new ManagedLgIcuCollator())
				{
					icuCollator2.Open("fr");
					object result2 = icuCollator2.get_SortKeyVariant("test", options);
				}
			}
		}

		[Test()]
		[Category("ByHand")]
		public void CloseTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		public void GetSortKeyTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				var options = new LgCollatingOptions();

				string result = icuCollator.get_SortKey("abc", options);
				Assert.That(result, Is.Not.Empty);

				Assert.That(() => icuCollator.Close(), Throws.Nothing);
			}
		}

		[Test()]
		[Category("ByHand")]
		public void GetSortKeyVariantTest()
		{
			using (var icuCollator= ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				var options = new LgCollatingOptions();

				object obj = icuCollator.get_SortKeyVariant("abc", options);
				Assert.That(obj, Is.Not.Null);

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		public void GetSortKeyRgchTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				int cchOut;
				Assert.That(() => icuCollator.SortKeyRgch(null, 0, new LgCollatingOptions(), 0, null, out cchOut),
					Throws.TypeOf<NotImplementedException>());
			}
		}

		[Test()]
		[Category("ByHand")]
		public void SortKeyVariantTestWithValues()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				var options = new LgCollatingOptions();

				// ICU sort keys can vary across ICU versions; verify basic invariants instead of exact bytes.
				var key1 = icuCollator.get_SortKeyVariant("action", options) as byte[];
				Assert.That(key1, Is.Not.Null);
				Assert.That(key1.Length, Is.GreaterThan(0));
				Assert.That(key1[key1.Length - 1], Is.EqualTo(0));

				var key2 = icuCollator.get_SortKeyVariant("action", options) as byte[];
				Assert.That(key2, Is.EqualTo(key1));
			}
		}


		[Test()]
		[Category("ByHand")]
		public void CompareVariantTest1()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				var options = new LgCollatingOptions();

				object obj1 = icuCollator.get_SortKeyVariant("abc", options);
				object obj2 = obj1;
				object obj3 = icuCollator.get_SortKeyVariant("def", options);

				Assert.That(icuCollator.CompareVariant(obj1, obj2, options) == 0, Is.True, " obj1 == obj2");
				Assert.That(icuCollator.CompareVariant(obj1, obj3, options) != 0, Is.True, " obj1 != obj3");

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		public void CompareVariantTest2()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				var options = new LgCollatingOptions();

				object obj1 = icuCollator.get_SortKeyVariant("action", options);
				object obj2 = icuCollator.get_SortKeyVariant("actiom", options);

				Assert.That(icuCollator.CompareVariant(obj1, obj2, options) != 0, Is.True, " action != actionm");

				obj1 = icuCollator.get_SortKeyVariant("tenepa", options);
				obj2 = icuCollator.get_SortKeyVariant("tenepo", options);
				Assert.That(icuCollator.CompareVariant(obj1, obj2, options) != 0, Is.True, " tenepa != tenepo");

				obj1 = icuCollator.get_SortKeyVariant("hello", options);
				obj2 = icuCollator.get_SortKeyVariant("hello", options);

				Assert.That(icuCollator.CompareVariant(obj1, obj2, options) == 0, Is.True, " hello == hello");


				obj1 = icuCollator.get_SortKeyVariant("tenepaa", options);
				obj2 = icuCollator.get_SortKeyVariant("tenepa", options);

				Assert.That(icuCollator.CompareVariant(obj1, obj2, options) > 0, Is.True, " tenepaa > tenepa");

				obj1 = icuCollator.get_SortKeyVariant("tenepa", options);
				obj2 = icuCollator.get_SortKeyVariant("tenepaa", options);

				Assert.That(icuCollator.CompareVariant(obj1, obj2, options) < 0, Is.True, " tenepaa < tenepa");

				icuCollator.Close();
			}
		}

		[Test()]
		[Category("ByHand")]
		public void CompareTest()
		{
			using (var icuCollator = ManagedLgIcuCollatorInitializerHelper())
			{
				Assert.That(icuCollator, Is.Not.Null);

				var options = new LgCollatingOptions();

				Assert.That(icuCollator.Compare(string.Empty, String.Empty, options) == 0, Is.True);
				Assert.That(icuCollator.Compare("abc", "abc", options) == 0, Is.True);
				Assert.That(icuCollator.Compare("abc", "def", options) != 0, Is.True);
			}
		}
	}
}
