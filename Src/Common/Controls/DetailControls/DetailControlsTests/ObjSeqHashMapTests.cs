// Copyright (c) 2025 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	[TestFixture]
	public class ObjSeqHashMapTests
	{
		private List<Slice> m_slices;

		[SetUp]
		public void SetUp()
		{
			m_slices = new List<Slice>();
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var slice in m_slices)
				slice.Dispose();
			m_slices.Clear();
		}

		private Slice MakeSlice(params object[] key)
		{
			var slice = new Slice();
			slice.Key = key;
			m_slices.Add(slice);
			return slice;
		}

		[Test]
		public void Add_And_Retrieve()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 1, 2, 3 };
			var slice = MakeSlice(1, 2, 3);

			map.Add(key, slice);

			var result = map[key];
			Assert.That(result.Count, Is.EqualTo(1));
			Assert.That(result[0], Is.SameAs(slice));
		}

		[Test]
		public void Remove_ReturnsCorrectSlice()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 10, 20 };
			var slice = MakeSlice(10, 20);

			map.Add(key, slice);
			map.Remove(key, slice);

			var result = map[key];
			Assert.That(result.Count, Is.EqualTo(0));
		}

		[Test]
		public void ClearUnwantedPart_DifferentObjectTrue_ClearsTable()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 5 };
			var slice = MakeSlice(5);

			map.Add(key, slice);
			map.ClearUnwantedPart(true);

			var result = map[key];
			Assert.That(result.Count, Is.EqualTo(0));
		}

		[Test]
		public void ClearUnwantedPart_DifferentObjectFalse_ClearsReuse()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 5 };
			var slice = MakeSlice(5);

			map.Add(key, slice);
			map.ClearUnwantedPart(false);

			var reused = map.GetSliceToReuse(nameof(Slice));
			Assert.That(reused, Is.Null);
		}

		[Test]
		public void DuplicateKeys_FIFO()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 42 };
			var slice1 = MakeSlice(42);
			var slice2 = MakeSlice(42);
			var slice3 = MakeSlice(42);

			map.Add(key, slice1);
			map.Add(key, slice2);
			map.Add(key, slice3);

			var result = map[key];
			Assert.That(result.Count, Is.EqualTo(3));
			Assert.That(result[0], Is.SameAs(slice1));
			Assert.That(result[1], Is.SameAs(slice2));
			Assert.That(result[2], Is.SameAs(slice3));
		}

		[Test]
		public void MissingKey_ReturnsEmptyList()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 99, 100 };

			var result = map[key];
			Assert.That(result.Count, Is.EqualTo(0));
		}

		[Test]
		public void GetSliceToReuse_ReturnsAndRemoves()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 1 };
			var slice1 = MakeSlice(1);
			var slice2 = MakeSlice(1);

			map.Add(key, slice1);
			map.Add(key, slice2);

			var reused = map.GetSliceToReuse(nameof(Slice));
			Assert.That(reused, Is.SameAs(slice1));

			// After retrieving, the slice should be removed from both the key list and reuse list
			var remaining = map[key];
			Assert.That(remaining.Count, Is.EqualTo(1));
			Assert.That(remaining[0], Is.SameAs(slice2));
		}

		[Test]
		public void GetSliceToReuse_MissingType_ReturnsNull()
		{
			var map = new ObjSeqHashMap();

			var result = map.GetSliceToReuse("NonExistentSliceType");
			Assert.That(result, Is.Null);
		}

		[Test]
		public void Values_ReturnsAllSlices()
		{
			var map = new ObjSeqHashMap();
			var key1 = new ArrayList { 1 };
			var key2 = new ArrayList { 2 };
			var slice1 = MakeSlice(1);
			var slice2 = MakeSlice(2);
			var slice3 = MakeSlice(1);

			map.Add(key1, slice1);
			map.Add(key2, slice2);
			map.Add(key1, slice3);

			var values = map.Values.ToList();
			// Values iterates both m_table and m_slicesToReuse, so each slice appears twice
			Assert.That(values, Has.Member(slice1));
			Assert.That(values, Has.Member(slice2));
			Assert.That(values, Has.Member(slice3));
		}

		[Test]
		public void Values_DuplicatesSliceAfterReuse_CurrentBehavior()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 7 };
			var slice = MakeSlice(7);

			map.Add(key, slice);

			var values = map.Values.ToList();
			Assert.That(values.Count(v => ReferenceEquals(v, slice)), Is.GreaterThan(1),
				"Current behavior: Values can include duplicate references from table + reuse map");
		}

		[Test]
		[Explicit("Expected to fail until ObjSeqHashMap.Values is deduplicated.")]
		public void Values_AreDeduplicated_ExpectedAfterFix()
		{
			var map = new ObjSeqHashMap();
			var key = new ArrayList { 8 };
			var slice = MakeSlice(8);

			map.Add(key, slice);

			var values = map.Values.ToList();
			Assert.That(values.Count(v => ReferenceEquals(v, slice)), Is.EqualTo(1),
				"Expected future behavior: each slice appears once in Values");
		}
	}

	[TestFixture]
	public class ListHashCodeProviderTests
	{
		[Test]
		public void ListHashCodeProvider_BoxedIntEquality()
		{
			IEqualityComparer comparer = new ListHashCodeProvider();
			var list1 = new ArrayList { 1, 2, 3 };
			var list2 = new ArrayList { 1, 2, 3 };

			Assert.That(comparer.Equals(list1, list2), Is.True);
			Assert.That(comparer.GetHashCode(list1), Is.EqualTo(comparer.GetHashCode(list2)));
		}

		[Test]
		public void ListHashCodeProvider_DifferentLengths_NotEqual()
		{
			IEqualityComparer comparer = new ListHashCodeProvider();
			var list1 = new ArrayList { 1, 2 };
			var list2 = new ArrayList { 1, 2, 3 };

			Assert.That(comparer.Equals(list1, list2), Is.False);
		}

		[Test]
		public void ListHashCodeProvider_XmlNodeEquality()
		{
			IEqualityComparer comparer = new ListHashCodeProvider();
			var doc = new XmlDocument();
			var node = doc.CreateElement("test");

			// Same reference → equal
			var list1 = new ArrayList { node };
			var list2 = new ArrayList { node };
			Assert.That(comparer.Equals(list1, list2), Is.True);

			// Different node instances with same content → not equal (reference equality)
			var node2 = doc.CreateElement("test");
			var list3 = new ArrayList { node2 };
			Assert.That(comparer.Equals(list1, list3), Is.False);
		}
	}
}
