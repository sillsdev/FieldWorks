// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ListUtilsTests.cs
// Responsibility: Bogle
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ListUtilsTests
	{
		#region ToString extension method tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of integers with no special
		/// function to convert them to strings and a null separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_ValueType_NoFuncNullSeparator()
		{
			IEnumerable<int> list = new[] { 5, 6, 2, 3 };
			Assert.AreEqual("5623", list.ToString(null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of integers with no special
		/// function to convert them to strings and a specified separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_ValueType_NoFunc_IgnoreEmpty()
		{
			IEnumerable<int> list = new[] { 5, 0, 2, 3 };
			Assert.AreEqual("5,0,2,3", list.ToString(true, ","));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of chars with no special
		/// function to convert them to strings and a comma and space as the separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_NoFuncCommaSeparator()
		{
			IEnumerable<char> list = new[] { '#', 'r', 'p', '3' };
			Assert.AreEqual("#, r, p, 3", list.ToString(", "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of strings with a special
		/// function to convert the strings to lowercase and the newline as the separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_Func()
		{
			IEnumerable<string> list = new[] { "ABC", "XYz", "p", "3w", "ml" };
			Assert.AreEqual("abc" + Environment.NewLine + "xyz" + Environment.NewLine + "p" + Environment.NewLine + "3w" + Environment.NewLine + "ml",
				list.ToString(Environment.NewLine, item => item.ToLowerInvariant()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for an empty dictionary.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_EmptyList()
		{
			Dictionary<string, int> list = new Dictionary<string, int>();
			Assert.AreEqual(string.Empty, list.ToString(Environment.NewLine, item => item.Key));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of strings that has nulls
		/// and empty strings which must be excluded, with a special function to convert the
		/// strings to lowercase and a space-padded ampersand as the separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_ExcludeNullAndEmptyItems()
		{
			IEnumerable<string> list = new[] { "ABC", null, "p", string.Empty };
			Assert.AreEqual("abc & p", list.ToString(" & ", item => item.ToLowerInvariant()));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of strings that has nulls
		/// and empty strings whose positions must be preserved in the list, with a special
		/// function to convert the strings to lowercase and a space-padded ampersand as the
		/// separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_IncludeNullAndEmptyItems()
		{
			IEnumerable<string> list = new[] { string.Empty, "ABC", null, "p", string.Empty };
			Assert.AreEqual("|ABC||p|", list.ToString(false, "|"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of strings that has nulls
		/// and empty strings whose positions must be preserved in the list, with a special
		/// function to convert the strings to lowercase and a space-padded ampersand as the
		/// separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_IgnoreNullAndEmptyItems()
		{
			IEnumerable<string> list = new[] { string.Empty, "ABC", null, "p", string.Empty };
			Assert.AreEqual("ABC|p", list.ToString(true, "|"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of objects of both class and
		/// value types that includes nulls and empty strings, with no special function to
		/// convert them to strings and a space-padded ampersand as the separator string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_ObjectsOfValueAndClassTypes()
		{
			IEnumerable<object> list = new object[] { "ABC", null, 0, string.Empty, 'r' };
			Assert.AreEqual("ABC & 0 & r", list.ToString(" & "));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ToString extension method for a collection of objects of both class and
		/// value types that includes nulls and empty strings, with a method that adds the
		/// strings plus the accumulated length of the builder and a comma as the separator
		/// string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ListToString_FuncThatTakesStrBuilder()
		{
			IEnumerable<string> list = new[] { "A", "BC", "XYZ" };
			const string kSep = ",";
			int kcchSep = kSep.Length;
			Assert.AreEqual("A1,BC5,XYZ10", list.ToString(kSep, (item, bldr) =>
			{
				int cch = bldr.Length > 0 ? kcchSep : 0;
				bldr.Append(item);
				bldr.Append(bldr.Length + cch);
			}));
		}
		#endregion

		#region ContainsSequence extension method tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ContainsSequence extension method when the super-sequence contains the
		/// sub-sequence at the beginning.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsSequence_AtBeginning()
		{
			Assert.IsTrue(new List<int>(new [] {1, 23, 4}).ContainsSequence(new List<int>(new [] {1, 23})));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ContainsSequence extension method when the super-sequence contains the
		/// sub-sequence at the end.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsSequence_AtEnd()
		{
			Assert.IsTrue(new List<int>(new[] { 1, 2, 3, 5 }).ContainsSequence(new List<int>(new[] { 2, 3, 5 })));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ContainsSequence extension method when the super-sequence contains the
		/// sub-sequence in the middle.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsSequence_InMiddle()
		{
			Assert.IsTrue(new List<object>(new object[] { 34, "A", "C", true, "34" }).ContainsSequence(
				new List<object>(new object[] { "A", "C", true })));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ContainsSequence extension method when list 1 is shorter than list 2.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsSequence_2LongerThan1()
		{
			Assert.IsFalse(new List<object>(new object[] { 34 }).ContainsSequence(new List<object>(new object[] { 34, true })));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ContainsSequence extension method when list 1 and list 2 are the same
		/// length and end the same but begin differently.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsSequence_DifferenceAtBeginning()
		{
			Assert.IsFalse(new List<object>(new object[] { 56, false, "A", 34 }).ContainsSequence(
				new List<object>(new object[] { 34, true, "A", 34 })));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ContainsSequence extension method when list 1 is empty.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsSequence_List1Empty()
		{
			Assert.IsFalse(new List<object>().ContainsSequence(new List<object>(new object[] { 34, true, "A", 34 })));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the ContainsSequence extension method when list 2 is empty (in which case we
		/// expect true).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void ContainsSequence_TrueIfList2IsEmpty()
		{
			Assert.IsTrue(new List<object>(new object[] { 34, true, "A", 34 }).ContainsSequence(new List<object>()));
			Assert.IsTrue(new List<object>().ContainsSequence(new List<object>()));
		}
		#endregion
	}
}
