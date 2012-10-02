using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace SIL.Utils
{
	/// <summary>
	/// Try the SimpleBag class.
	/// </summary>
	[TestFixture]
	public class BagTests // can't derive from BaseTest because of dependencies
	{
		/// <summary>
		/// Test the case where several items are added and removed, without making duplicates.
		/// </summary>
		[Test]
		public void NoDuplicates()
		{
			var bag = new SimpleBag<int>();
			VerifyBag(bag, new int[0]);
			bag.Add(5);
			Assert.AreEqual(1, bag.Occurrences(5));
			Assert.AreEqual(0, bag.Occurrences(6));
			VerifyBag(bag, new[] { 5 });
			bag.Add(6);
			Assert.AreEqual(1, bag.Occurrences(5));
			Assert.AreEqual(1, bag.Occurrences(6));
			Assert.AreEqual(0, bag.Occurrences(2));
			VerifyBag(bag, new[] { 5, 6 });
			bag.Add(2);
			Assert.AreEqual(1, bag.Occurrences(5));
			Assert.AreEqual(1, bag.Occurrences(6));
			Assert.AreEqual(1, bag.Occurrences(2));
			Assert.AreEqual(0, bag.Occurrences(10));
			VerifyBag(bag, new[] { 2, 5, 6 });

			Assert.IsTrue(bag.Remove(2));
			VerifyBag(bag, new[] { 5, 6 });
			Assert.IsTrue(bag.Remove(6));
			VerifyBag(bag, new[] { 5 });
			Assert.IsTrue(bag.Remove(5));
			VerifyBag(bag, new int[0]);
		}

		void VerifyBag(SimpleBag<int> bag, int[] expected)
		{
			Assert.AreEqual(expected.Length, bag.Count);
			var values = new List<int>(bag);
			values.Sort();
			Assert.AreEqual(expected.Length, values.Count);
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], values[i]);
			values = new List<int>(bag.Items);
			values.Sort();
			int iex = 0;
			int iv = 0;
			while (iex < expected.Length)
			{
				Assert.AreEqual(expected[iex], values[iv]);
				// Skip duplicates in expected.
				while (iex < expected.Length - 1 && expected[iex] == expected[iex + 1])
					iex++;
				iex++;
				iv++;
			}
		}

		/// <summary>
		/// Verify that if we try to remove something that is not found it throws.
		/// </summary>
		[Test]
		public void RemoveNotFound()
		{
			var bag = new SimpleBag<int>();

			//Check when the bag is empty.
			Assert.IsFalse(bag.Remove(3));

			//Check when just one thing is in the bag.
			bag.Add(2);
			Assert.IsFalse(bag.Remove(3));

			//Check the case where there are multiple different items in the bag.
			bag.Add(4);
			Assert.IsFalse(bag.Remove(3));

			//The case where there are duplicate items.
			bag.Add(2);
			Assert.IsFalse(bag.Remove(3));
		}

		/// <summary>
		/// Try it out in the presence of duplicates.
		/// </summary>
		[Test]
		public void Duplicates()
		{
			var bag = new SimpleBag<int>();
			VerifyBag(bag, new int[0]);
			bag.Add(5);
			bag.Add(5);
			bag.Add(5);
			Assert.AreEqual(3, bag.Occurrences(5));
			Assert.AreEqual(0, bag.Occurrences(10));
			VerifyBag(bag, new[] { 5, 5, 5 });
			Assert.IsTrue(bag.Remove(5));
			VerifyBag(bag, new[] { 5, 5 });
			Assert.IsTrue(bag.Remove(5));
			VerifyBag(bag, new[] { 5 });
			Assert.IsTrue(bag.Remove(5));
			VerifyBag(bag, new int[0]);

			bag = new SimpleBag<int>();
			bag.Add(5);
			bag.Add(5);
			bag.Add(5);
			bag.Add(6);
			bag.Add(6);
			Assert.AreEqual(3, bag.Occurrences(5));
			Assert.AreEqual(2, bag.Occurrences(6));
			Assert.AreEqual(0, bag.Occurrences(10));
			VerifyBag(bag, new[] { 5, 5, 5, 6, 6 });

			// Subtly different: this takes it to a set before a dictionary.
			bag = new SimpleBag<int>();
			bag.Add(5);
			bag.Add(6);
			bag.Add(5);
			bag.Add(5);
			bag.Add(6);
			Assert.AreEqual(3, bag.Occurrences(5));
			Assert.AreEqual(2, bag.Occurrences(6));
			Assert.AreEqual(0, bag.Occurrences(10));
			VerifyBag(bag, new[] { 5, 5, 5, 6, 6 });
		}

		[Test]
		public void BagWrapper()
		{
			var holder = new BagHolder();
			var bag = holder.Bag;
			Assert.AreEqual(0, bag.Count);
			// This is the critical test: the wrapper should see the modifications to the original.
			holder.Add(1);
			Assert.AreEqual(1, bag.Count);
			// Pause to make sure all the interface functions work.
			Assert.AreEqual(1, bag.Occurrences(1));
			Assert.AreEqual(1, bag.Items.Count());
			holder.Add(2); // moves it up to set
			Assert.AreEqual(2, bag.Count);
			var list = new List<int>(bag);
			list.Sort();
			Assert.AreEqual(1, list[0]);
			Assert.AreEqual(2, list[1]);
			holder.Add(1); // moves it to dictionary
			Assert.AreEqual(2, bag.Occurrences(1));
		}
	}

	class BagHolder
	{
		private SimpleBag<int> m_bag;

		public void Add(int item) { m_bag.Add(item); }

		public IBag<int> Bag { get { return new BagWrapper<int>(() => m_bag); } }
	}
}
