// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;

using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests generic Set class uisng ints.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SetTests // can't derive from BaseTest because of dependencies
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a Set with given integers.
		/// </summary>
		/// <param name="values"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static Set<int> Create(params int[] values)
		{
			return new Set<int>(values);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests all of the Contructors.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ConstructionTests()
		{
			Set<int> set = new Set<int>();
			Assert.AreEqual(0, set.Count, "Set should be empty at first.");

			set = new Set<int>(0);
			Assert.AreEqual(0, set.Count, "Set should be empty at first.");

			set = Set<int>.Empty;
			Assert.AreEqual(0, set.Count, "Set should be empty at first.");

			int[] ints = new int[] { 1, 2 };
			set = new Set<int>(ints);
			Assert.AreEqual(2, set.Count, "Set should two itmes in it.");

			Set<int> set2 = Create(1, 2);
			set = new Set<int>(set2);
			Assert.AreEqual(2, set.Count, "Set should two itmes in it.");

			set = new Set<int>(set2 as IEnumerable<int>);
			Assert.AreEqual(2, set.Count, "Set should two itmes in it.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetEnumerator() method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EnumeratorTests()
		{
			Set<int> set = Create(1, 2);

			int count = 0;
			using (IEnumerator<int> ie = set.GetEnumerator())
			{
				ie.Reset();
				while (ie.MoveNext())
				{
					Assert.IsTrue(set.Contains(ie.Current), "Set does not contain 'Current'.");
					count++;
				}
				Assert.AreEqual(2, count, "Wrong number of items in enumerator.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the integer added, gets put into the set.
		/// This test also effectively tests the Count property and Contains method.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddTests()
		{
			Set<int> set = new Set<int>();
			Assert.AreEqual(0, set.Count, "Set should be empty at first.");

			// Add an integer.
			set.Add(1);
			Assert.AreEqual(1, set.Count, "Set should have one integer by now.");

			Assert.IsTrue(set.Contains(1), "Set should contain the integer 1.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that duplicate integers do not get added to set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddRangeTests1()
		{
			Set<int> set = Create(1);
			Assert.AreEqual(1, set.Count, "Set should have only one item in it.");

			// Add new integers with 1 being a duplicate.
			set.AddRange(new int[2] { 1, 2 });
			Assert.AreEqual(2, set.Count, "Set should have two integer by now.");

			Assert.IsTrue(set.Contains(1), "Set should contain the integer 1.");
			Assert.IsTrue(set.Contains(2), "Set should contain the integer 2.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that duplicate integers do not get added to set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddRangeTests2()
		{
			Set<int> set = Create(1);
			Assert.AreEqual(1, set.Count, "Set should have only one item in it.");

			// Add new integers with 1 being a duplicate.
			set.AddRange(new object[2] { 1, 2 });
			Assert.AreEqual(2, set.Count, "Set should have two integer by now.");

			Assert.IsTrue(set.Contains(1), "Set should contain the integer 1.");
			Assert.IsTrue(set.Contains(2), "Set should contain the integer 2.");

			try
			{
				set.AddRange(new object[] { 5, 56.12f });
				Assert.Fail("Should fail to add in the float value");
			}
			catch (InvalidCastException e)
			{
				if (Environment.OSVersion.Platform == PlatformID.Unix)
					Assert.AreEqual("Cannot cast from source type to destination type.", e.Message);
				else
				Assert.AreEqual("Specified cast is not valid.", e.Message);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ToArray returns correct array.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ToArrayTests()
		{
			int[] starting = new int[3] { 1, 2, 3 };
			Set<int> set = new Set<int>(starting);
			Assert.AreEqual(3, set.Count, "Set should have three items in it.");

			int[] ending = set.ToArray();
			Assert.AreEqual(3, ending.Length, "Set should have three items in it.");

			int i = 0;
			foreach (int startInt in starting)
			{
				Assert.AreEqual(startInt, ending[i++], "Set should have same three items in it.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that ToArray can return arrays of other types.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ToArrayTests2()
		{
			object[] starting = new object[3] { 1, 2, 3 };
			Set<object> set = new Set<object>(starting);
			Assert.AreEqual(3, set.Count, "Set should have three items in it.");

			int[] ending = set.ToArray<int>();
			Assert.AreEqual(3, ending.Length, "Set should have three items in it.");

			int i = 0;
			foreach (int startInt in starting)
			{
				Assert.AreEqual(startInt, ending[i++], "Set should have same three items in it.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that CastAsNewSet returns a set of the needed type
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CastAsNewSet()
		{
			object[] starting = new object[3] { 1, 2, 3 };
			Set<object> set = new Set<object>(starting);
			Assert.AreEqual(3, set.Count, "Set should have three items in it.");

			Set<int> ending = set.CastAsNewSet<int>();
			Assert.AreEqual(3, ending.Count, "Set should have three items in it.");

			foreach (int startInt in starting)
			{
				Assert.IsTrue(ending.Contains(startInt), "Set should have same three items in it.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that CopyTo returns correct array.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CopyToTests()
		{
			int[] starting = new int[3] { 1, 2, 3 };
			Set<int> set = new Set<int>(starting);
			Assert.AreEqual(3, set.Count, "Set should have three items in it.");

			int[] ending = new int[set.Count];
			set.CopyTo(ending, 0);
			Assert.AreEqual(3, ending.Length, "Set should have three items in it.");

			int i = 0;
			foreach (int startInt in starting)
			{
				Assert.AreEqual(startInt, ending[i++], "Set should have same three items in it.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the set gets cleared of all contents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ClearTests()
		{
			Set<int> set = Create(1);
			Assert.AreEqual(1, set.Count, "Set should have only one item in it.");

			set.Clear();
			Assert.AreEqual(0, set.Count, "Set should have no items left in it.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that an item gets removed from the set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void RemoveTests()
		{
			Set<int> set = Create(1, 2);
			Assert.AreEqual(2, set.Count, "Set should have two items in it.");

			set.Remove(1);
			Assert.AreEqual(1, set.Count, "Set should have one item left in it.");

			Assert.IsFalse(set.Contains(1), "Set still has '1' in it.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that an item gets removed from the set.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetOperationsTests()
		{
			// These test essentially came from the author of the Set class, Rüdiger Klaehn.
			// I (RandyR) jsut 'ported' them to nunit.
			Set<int> a = Create(1, 2, 3);
			Set<int> b = Create(3, 4, 5);
			Set<int> c = Create(1, 2, 3, 4);
			Assert.IsTrue((a & b) == Create(3), "Exclusive ORing didn't work.");
			Assert.IsTrue((a | b) == Create(1, 2, 3, 4, 5), "Regular ORing (Union) didn't work.");
			Assert.IsTrue((a ^ b) == Create(1, 2, 4, 5), "SymmetricDifference didn't work.");
			Assert.IsTrue(a - b == Create(1, 2), "Difference 1 didn't work.");
			Assert.IsTrue(b - a == Create(4, 5), "Difference 2 didn't work.");

			Assert.IsTrue(a != c, "a and c are ==.");
			Assert.IsTrue(a <= c, "a is not <= c.");
			Assert.IsTrue(c >= a, "c is not >- a.");
			Assert.IsTrue(a < c, "a is not < c.");
			Assert.IsTrue(c > a, "c is not > a.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ReadonlySet throws an exception when trying to add an item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReadonlySet_Add()
		{
			Set<int> set = new ReadonlySet<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, set.Count);
			Assert.IsTrue(set.IsReadOnly);

			// Use a try/catch so that we know that this call is the one throwing the exception
			try
			{
				set.Add(5);
				Assert.Fail("Should have thrown a NotSupportedException");
			}catch (NotSupportedException){ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ReadonlySet throws an exception when trying to remove an item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReadonlySet_Remove()
		{
			Set<int> set = new ReadonlySet<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, set.Count);
			Assert.IsTrue(set.IsReadOnly);

			// Use a try/catch so that we know that this call is the one throwing the exception
			try
			{
				set.Remove(2);
				Assert.Fail("Should have thrown a NotSupportedException");
			}catch (NotSupportedException){ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ReadonlySet throws an exception when trying to add a list of
		/// type-safe items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReadonlySet_AddRange1()
		{
			Set<int> set = new ReadonlySet<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, set.Count);
			Assert.IsTrue(set.IsReadOnly);

			// Use a try/catch so that we know that this call is the one throwing the exception
			try
			{
				set.AddRange(new int[] { 5, 6, 7 });
				Assert.Fail("Should have thrown a NotSupportedException");
			}catch (NotSupportedException){ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ReadonlySet throws an exception when trying to add a list of
		/// unknown-type items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReadonlySet_AddRange2()
		{
			Set<int> set = new ReadonlySet<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, set.Count);
			Assert.IsTrue(set.IsReadOnly);

			// Use a try/catch so that we know that this call is the one throwing the exception
			try
			{
				set.AddRange(new object[] { 5, 6, 7 });
				Assert.Fail("Should have thrown a NotSupportedException");
			}catch (NotSupportedException){ }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that the ReadonlySet throws an exception when trying to clear all items.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ReadonlySet_Clear()
		{
			Set<int> set = new ReadonlySet<int>(new int[] { 1, 2, 3 });
			Assert.AreEqual(3, set.Count);
			Assert.IsTrue(set.IsReadOnly);

			// Use a try/catch so that we know that this call is the one throwing the exception
			try
			{
				set.Clear();
				Assert.Fail("Should have thrown a NotSupportedException");
			}catch (NotSupportedException){ }
		}
	}
}
