// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	public class ListHashCodeProvider : IEqualityComparer
	{
		/// <summary>
		/// Gets the hash code.
		/// </summary>
		int IEqualityComparer.GetHashCode(object objList)
		{
			var list = (IList)objList;
			var hash = 0;
			foreach (var obj in list)
			{
				// This ensures that two sequences containing the same boxed integer produce the same hash value.
				if (obj is int)
				{
					hash += (int)obj;
				}
				else
				{
					hash += obj.GetHashCode();
				}
			}
			return hash;
		}

		/// <summary>
		/// This comparer is only suitable for hash tables; it doesn't provide a valid
		/// (commutative) ordering of items.
		/// </summary>
		/// <remarks>Note that in general, boxed values are not equal, even if the unboxed
		/// values would be. The current code makes a special case for ints, which behave
		/// as expected.</remarks>
		bool IEqualityComparer.Equals(object xArg, object yArg)
		{
			var listX = (IList)xArg;
			var listY = (IList)yArg;
			if (listX.Count != listY.Count)
			{
				return false;
			}
			for (var i = 0; i < listX.Count; i++)
			{
				var x = listX[i];
				var y = listY[i];
				if (x != y && !(x is int && y is int && (int)x == (int)y))
				{
					return false;
				}
			}
			return true;
		}
	}
}