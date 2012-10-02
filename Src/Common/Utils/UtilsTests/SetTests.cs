using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;

using NUnit.Framework;

namespace SIL.FieldWorks.Common.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests generic Set class uisng ints.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SetTests
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public SetTests()
		{
		}

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
			IEnumerator<int> ie = set.GetEnumerator();
			ie.Reset();
			while (ie.MoveNext())
			{
				Assert.IsTrue(set.Contains(ie.Current), "Set does not contain 'Current'.");
				count++;
			}
			Assert.AreEqual(2, count, "Wrong number of items in enumerator.");
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
		public void AddRangeTests()
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
	}
}