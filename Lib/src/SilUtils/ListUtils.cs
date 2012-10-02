// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ListUtils.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for Utils.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ListUtils
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the enumeration, formatted as a string that contains the items, separated
		/// by the specified separator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">The enumeration.</param>
		/// <param name="separator">The separator.</param>
		/// ------------------------------------------------------------------------------------
		public static string ToString<T>(this IEnumerable<T> list, string separator)
		{
			return list.ToString(separator, item => item.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the enumeration, formatted as a string that contains the items, separated
		/// by the specified separator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">The enumeration.</param>
		/// <param name="ignoreEmptyItems">True to ignore any items in the list that are empty
		/// or null, false to include them in the returned string.</param>
		/// <param name="separator">The separator.</param>
		/// ------------------------------------------------------------------------------------
		public static string ToString<T>(this IEnumerable<T> list, bool ignoreEmptyItems,
			string separator)
		{
			return list.ToString(ignoreEmptyItems, separator, item => item == null ? string.Empty : item.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the enumeration, formatted as a string that contains the items, separated
		/// by the specified separator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">The enumeration.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="itemToStringFunction">A function that is applied to each item to turn
		/// it into a string.</param>
		/// ------------------------------------------------------------------------------------
		public static string ToString<T>(this IEnumerable<T> list, string separator,
			Func<T, string> itemToStringFunction)
		{
			return list.ToString(true, separator, itemToStringFunction);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the enumeration, formatted as a string that contains the items, separated
		/// by the specified separator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">The enumeration.</param>
		/// <param name="ignoreEmptyItems">True to ignore any items in the list that are empty
		/// or null, false to include them in the returned string.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="itemToStringFunction">A function that is applied to each item to turn
		/// it into a string.</param>
		/// ------------------------------------------------------------------------------------
		public static string ToString<T>(this IEnumerable<T> list, bool ignoreEmptyItems,
			string separator, Func<T, string> itemToStringFunction)
		{
			var bldr = new StringBuilder();
			bool fFirstTime = true;
			foreach (T item in list)
			{
				if (!ignoreEmptyItems || item != null)
				{
					string sItem = itemToStringFunction(item);
					if (!fFirstTime && (!string.IsNullOrEmpty(sItem) || !ignoreEmptyItems))
						bldr.Append(separator);
					bldr.Append(sItem);
				}
				fFirstTime = ignoreEmptyItems && bldr.Length == 0;
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the enumeration, formatted as a string that contains the items, separated
		/// by the specified separator.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="list">The enumeration.</param>
		/// <param name="separator">The separator.</param>
		/// <param name="addToBuilderDelegate">Delegate to add the item to the string builder.</param>
		/// ------------------------------------------------------------------------------------
		public static string ToString<T>(this IEnumerable<T> list, string separator,
			Action<T, StringBuilder> addToBuilderDelegate)
		{
			var bldr = new StringBuilder();
			foreach (T item in list)
			{
				if (item == null)
					continue;
				int cchBefore = bldr.Length;
				addToBuilderDelegate(item, bldr);
				if (cchBefore > 0 && bldr.Length > cchBefore)
					bldr.Insert(cchBefore, separator);
			}
			return bldr.ToString();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines whether a list contains the sequence represented by another list.
		/// </summary>
		/// <typeparam name="T">The type of item contained in the lists</typeparam>
		/// <param name="list1">The potential super-sequence.</param>
		/// <param name="list2">The potential sub-sequence.</param>
		/// ------------------------------------------------------------------------------------
		public static bool ContainsSequence<T>(this IList<T> list1, IList<T> list2)
		{
			for (int i1 = 0; i1 <= list1.Count - list2.Count; i1++)
			{
				bool foundAtThisPos = true;
				for (int i2 = 0; i2 < list2.Count; i2++)
				{
					if (!EqualityComparer<T>.Default.Equals(list1[i1 + i2], list2[i2])) // See http://stackoverflow.com/questions/390900/cant-operator-be-applied-to-generic-types-in-c/390974#390974
					{
						foundAtThisPos = false;
						break;
					}
				}
				if (foundAtThisPos)
					return true;
			}
			return false;
		}
	}
}
