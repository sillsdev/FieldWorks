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
// File: ArrayUtils.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Linq;

namespace SIL.Utils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public static class ArrayUtils
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the given enumarables have the same contents
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static bool AreEqual(IEnumerable e1, IEnumerable e2)
		{
			// both empty is equivalent
			if (e1 == null && e2 == null)
				return true;

			if (e1 != null && e2 != null)
			{
				IEnumerator e1Enum = e1.GetEnumerator();
				IEnumerator e2Enum = e2.GetEnumerator();
				// Be very careful about changing this. Remember MoveNext() changes the state.
				// You can't test it twice and have it mean the same thing.
				while (e1Enum.MoveNext())
				{
					if (e2Enum.MoveNext())
					{
						if (!e1Enum.Current.Equals(e2Enum.Current))
							return false; // items at current position are not equal
					}
					else
					{
						return false; // collection 2 is shorter
					}
				}
				// Come to the end of e1Enum and all elements so far matched.
				return !e2Enum.MoveNext(); // if there are more in e2 they are not equal
			}
			return false; // one (only) is null
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the specified input array to the output type.
		/// </summary>
		/// <typeparam name="T1">The type of the output.</typeparam>
		/// <typeparam name="T2">The type of the input.</typeparam>
		/// <param name="input">The input array.</param>
		/// <returns>The output in the output type</returns>
		/// ------------------------------------------------------------------------------------
		public static T1[] Convert<T1, T2>(T2[] input) where T1 : T2
		{
			T1[] output = new T1[input.Length];
			for(int i = 0; i < input.Length; i++)
				output[i] = (T1)input[i];
			return output;
		}
	}
}
