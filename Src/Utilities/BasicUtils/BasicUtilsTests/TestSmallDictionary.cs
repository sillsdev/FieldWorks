// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TestSmallDictionary.cs
// Responsibility: thomson
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TestSmallDictionary // can't derive from BaseTest because of dependencies
	{
		/// <summary>
		/// Try creating a small dictionary and test various properties of an empty one.
		/// </summary>
		[Test]
		public void Empty()
		{
			var dict = new SmallDictionary<int, string>();
			Assert.AreEqual(0, dict.Count);
			string output;
			Assert.IsFalse(dict.TryGetValue(2, out output));
			Assert.IsNull(output);
			Assert.IsFalse(dict.ContainsKey(3));
		}

		/// <summary>
		/// Try various things when it has exactly one item.
		/// Also tests adding the first item.
		/// </summary>
		[Test]
		public void OneItem()
		{
			var dict = new SmallDictionary<int, string>();
			dict[2] = "abc";
			Assert.AreEqual(1, dict.Count);
			Assert.AreEqual("abc", dict[2]);
			string output;
			Assert.IsFalse(dict.TryGetValue(3, out output));
			Assert.IsNull(output);
			Assert.IsTrue(dict.TryGetValue(2, out output));
			Assert.AreEqual("abc", output);
			Assert.IsFalse(dict.ContainsKey(3));
			Assert.IsTrue(dict.ContainsKey(2));
		}

		/// <summary>
		/// Try various things when there are three items.
		/// </summary>
		[Test]
		public void ThreeItems()
		{
			var dict = new SmallDictionary<int, string>();
			dict[2] = "abc";
			dict[5] = "def";
			dict[11] = "third";
			Assert.AreEqual(3, dict.Count);
			Assert.AreEqual("abc", dict[2]);
			Assert.AreEqual("def", dict[5]);
			Assert.AreEqual("third", dict[11]);
			string output;
			Assert.IsFalse(dict.TryGetValue(3, out output));
			Assert.IsNull(output);
			Assert.IsTrue(dict.TryGetValue(2, out output));
			Assert.AreEqual("abc", output);
			Assert.IsTrue(dict.TryGetValue(5, out output));
			Assert.AreEqual("def", output);
			Assert.IsTrue(dict.TryGetValue(11, out output));
			Assert.AreEqual("third", output);

			Assert.IsFalse(dict.ContainsKey(3));
			Assert.IsTrue(dict.ContainsKey(2));
			Assert.IsTrue(dict.ContainsKey(5));
			Assert.IsTrue(dict.ContainsKey(11));
		}

		/// <summary>
		/// Try overwriting existing keys.
		/// </summary>
		[Test]
		public void Overwrite()
		{
			var dict = new SmallDictionary<int, string>();
			dict[2] = "abc";
			Assert.AreEqual("abc", dict[2]);
			dict[2] = "first";
			Assert.AreEqual("first", dict[2]);
			Assert.AreEqual(1, dict.Count);
			dict[5] = "def";
			dict[11] = "third";
			Assert.AreEqual(3, dict.Count);
			Assert.AreEqual("first", dict[2]);
			Assert.AreEqual("def", dict[5]);
			Assert.AreEqual("third", dict[11]);
			dict[5] = "second";
			Assert.AreEqual(3, dict.Count);
			Assert.AreEqual("first", dict[2]);
			Assert.AreEqual("second", dict[5]);
			Assert.AreEqual("third", dict[11]);
			dict[11] = "change";
			Assert.AreEqual(3, dict.Count);
			Assert.AreEqual("first", dict[2]);
			Assert.AreEqual("second", dict[5]);
			Assert.AreEqual("change", dict[11]);
		}

		/// <summary>
		/// Test that we cannot set an item with a key equal to the default value for the key type.
		/// </summary>
		[Test, ExpectedException(typeof(ArgumentException))]
		public void ItemZeroKey()
		{
			var dict = new SmallDictionary<int, string>();
			dict[0] = "abc";
		}
		/// <summary>
		/// Test that we cannot add an item with a key equal to the default value for the key type.
		/// </summary>
		[Test, ExpectedException(typeof(ArgumentException))]
		public void AddZeroKey()
		{
			var dict = new SmallDictionary<int, string>();
			dict.Add(0,"abc");
		}

		/// <summary>
		/// Test that we cannot get an item with a key equal to the default value for the key type.
		/// </summary>
		[Test, ExpectedException(typeof(ArgumentException))]
		public void GetZeroKey()
		{
			var dict = new SmallDictionary<int, string>();
			var temp = dict[0];
		}

		/// <summary>
		/// Test that we get an exception seeking a missing key (on an empty dictionary).
		/// </summary>
		[Test, ExpectedException(typeof(KeyNotFoundException))]
		public void GetMissingKeyEmpty()
		{
			var dict = new SmallDictionary<int, string>();
			var temp = dict[2];
		}

		/// <summary>
		/// Test that we get an exception seeking a missing key (on an dictionary with one key).
		/// </summary>
		[Test, ExpectedException(typeof(KeyNotFoundException))]
		public void GetMissingKeyOne()
		{
			var dict = new SmallDictionary<int, string>();
			dict.Add(2, "abc");
			var temp = dict[3];
		}

		/// <summary>
		/// Test that we get an exception seeking a missing key (on an dictionary with three key).
		/// </summary>
		[Test, ExpectedException(typeof(KeyNotFoundException))]
		public void GetMissingKeyThree()
		{
			var dict = new SmallDictionary<int, string>();
			dict.Add(2, "abc");
			dict.Add(5, "def");
			dict.Add(11, "third");
			var temp = dict[3];
		}

		/// <summary>
		/// Test success cases of Add.
		/// </summary>
		[Test]
		public void Add()
		{
			var dict = new SmallDictionary<int, string>();
			dict.Add(2, "abc");
			Assert.AreEqual(1, dict.Count);
			dict.Add(5,"def");
			Assert.AreEqual(2, dict.Count);
			dict.Add(11,"third");
			Assert.AreEqual(3, dict.Count);
			Assert.AreEqual("abc", dict[2]);
			Assert.AreEqual("def", dict[5]);
			Assert.AreEqual("third", dict[11]);
		}
		/// <summary>
		/// Test that we cannot add an item with a key equal to the default value for the key type.
		/// </summary>
		[Test, ExpectedException(typeof(ArgumentException))]
		public void AddExistingKeyOneKey()
		{
			var dict = new SmallDictionary<int, string>();
			dict.Add(2, "abc");
			dict.Add(2, "def");
		}

		/// <summary>
		/// Test that we cannot add an item with a key equal to the default value for the key type.
		/// </summary>
		[Test, ExpectedException(typeof(ArgumentException))]
		public void AddExistingKeyTwoKeys()
		{
			var dict = new SmallDictionary<int, string>();
			dict.Add(2, "abc");
			dict.Add(3, "def");
			dict.Add(3, "def");
		}

		/// <summary>
		/// Test removing items.
		/// </summary>
		[Test]
		public void Remove()
		{
			var dict = new SmallDictionary<int, string>();
			Assert.IsFalse(dict.Remove(2), "Remove a missing item from an empty dictionary");
			dict.Add(2, "abc");
			Assert.IsFalse(dict.Remove(3), "Remove a missing item from a dictionary with one item");
			Assert.IsTrue(dict.Remove(2), "Remove the only item from a dictionary");
			Assert.AreEqual(0, dict.Count, "Nothing remains after removing the only item");
			dict.Add(2, "abc");
			dict.Add(5, "def");
			dict.Add(11, "third");
			Assert.IsFalse(dict.Remove(7), "Remove a missing item from a dictionary with three items");
			Assert.IsTrue(dict.Remove(2), "Remove the first item from a dictionary with three items");
			Assert.AreEqual(2, dict.Count, "Two items remain after removing the first of three");
			string output;
			Assert.IsFalse(dict.TryGetValue(2, out output), "Removed item (first) should be gone from original three");
			Assert.AreEqual("def", dict[5]);
			Assert.AreEqual("third", dict[11]);
			dict.Add(2, "abc");
			Assert.IsTrue(dict.Remove(5), "Remove first of two items in others");
			Assert.AreEqual(2, dict.Count);
			Assert.IsFalse(dict.TryGetValue(5, out output));
			Assert.AreEqual("abc", dict[2]);
			Assert.AreEqual("third", dict[11]);

			Assert.IsTrue(dict.Remove(2), "Remove only item in others");
			Assert.AreEqual(1, dict.Count);
			Assert.IsFalse(dict.TryGetValue(2, out output));
			Assert.AreEqual("third", dict[11]);

			Assert.IsTrue(dict.Remove(11), "Remove only item in dictionary which previously had three");
			Assert.AreEqual(0, dict.Count);
			Assert.IsFalse(dict.TryGetValue(11, out output));

			dict.Add(2, "abc");
			dict.Add(5, "def");
			dict.Add(11, "third");
			dict.Add(13, "fourth");
			Assert.IsTrue(dict.Remove(11), "Remove middle of three items in others");
			Assert.AreEqual(3, dict.Count);
			Assert.IsFalse(dict.TryGetValue(11, out output));
			Assert.AreEqual("abc", dict[2]);
			Assert.AreEqual("def", dict[5]);
			Assert.AreEqual("fourth", dict[13]);

			Assert.IsTrue(dict.Remove(13), "Remove last of two items in others");
			Assert.AreEqual(2, dict.Count);
			Assert.IsFalse(dict.TryGetValue(13, out output));
			Assert.AreEqual("abc", dict[2]);
			Assert.AreEqual("def", dict[5]);

			Assert.IsTrue(dict.Remove(2), "Remove first item when others contains exactly one");
			Assert.AreEqual(1, dict.Count);
			Assert.IsFalse(dict.TryGetValue(2, out output));
			Assert.AreEqual("def", dict[5]);
		}

		/// <summary>
		/// Test enumerating.
		/// </summary>
		[Test]
		public void Enumerator()
		{
			var dict = new SmallDictionary<int, string>();
			foreach (var kvp in dict)
			{
				Assert.Fail("Should get no iterations looping over empty dictionary");
			}
			dict.Add(2, "abc");
			int count = 0;
			foreach (var kvp in dict)
			{
				count++;
				Assert.AreEqual(2, kvp.Key);
				Assert.AreEqual("abc", kvp.Value);
			}
			Assert.AreEqual(1,count);

			dict.Add(5, "def");
			dict.Add(11, "third");
			count = 0;
			Dictionary<int, string> normalDict = new Dictionary<int, string>(dict);
			Assert.AreEqual("abc", normalDict[2]);
			Assert.AreEqual("def", normalDict[5]);
			Assert.AreEqual("third", normalDict[11]);
			Assert.AreEqual(3, normalDict.Count);
			foreach (var kvp in dict)
			{
				count++;
				Assert.IsTrue(normalDict.ContainsKey(kvp.Key));
				Assert.AreEqual(normalDict[kvp.Key], kvp.Value);
				normalDict.Remove(kvp.Key);
			}
			Assert.AreEqual(3, count);
		}

		void VerifyIntArrays(int[] expected, int[] actual)
		{
			Assert.AreEqual(expected.Length, actual.Length);
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], actual[i]);
		}
		void VerifyStringArrays(string[] expected, string[] actual)
		{
			Assert.AreEqual(expected.Length, actual.Length);
			for (int i = 0; i < expected.Length; i++)
				Assert.AreEqual(expected[i], actual[i]);
		}

		/// <summary>
		/// Test retrieving Keys
		/// </summary>
		[Test]
		public void KeysAndValues()
		{
			var dict = new SmallDictionary<int, string>();
			VerifyIntArrays(new int[0], dict.Keys.ToArray());
			VerifyStringArrays(new string[0], dict.Values.ToArray());

			dict.Add(2, "abc");
			VerifyIntArrays(new int[] {2}, dict.Keys.ToArray());
			VerifyStringArrays(new string[] {"abc"}, dict.Values.ToArray());

			// This test is too strict, it enforces a particular order of the results.
			dict.Add(5, "def");
			dict.Add(11, "third");
			VerifyIntArrays(new int[] { 2, 5, 11 }, dict.Keys.ToArray());
			VerifyStringArrays(new string[] { "abc", "def", "third" }, dict.Values.ToArray());
		}

		/// <summary>
		/// Test the Clear method
		/// </summary>
		[Test]
		public void Clear()
		{
			var dict = new SmallDictionary<int, string>();
			dict.Clear();
			Assert.AreEqual(0, dict.Count);
			dict[2] = "abc";
			dict.Clear();
			Assert.AreEqual(0, dict.Count);
			dict[2] = "abc";
			dict[5] = "def";
			dict[11] = "third";
			dict.Clear();
			Assert.AreEqual(0, dict.Count);

		}
	}
}
