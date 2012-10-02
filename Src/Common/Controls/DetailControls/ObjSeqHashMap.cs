using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// An object sequence hash map represents a mapping from sequences of objects to sequences of objects.
	/// The key may be anything that implements IList. The value is also a list.
	/// It is expected that very commonly only one value will be stored for a given key.
	/// The implementation is optimized for this by storing the object directly rather than a sequence
	/// holding the one object. Two objects are typically stored as an array, three or more as an ArrayList.
	/// (This means lists of lists don't work.)
	/// </summary>
	public class ObjSeqHashMap
	{
		Hashtable m_table;
		// These are slices looked up by type name. The same ones as m_table. This supports reuse for different objects.
		private Dictionary<string, List<Slice>> m_slicesToReuse;

		/// <summary></summary>
		public ObjSeqHashMap()
		{
			m_table = new Hashtable(new ListHashCodeProvider());
			m_slicesToReuse = new Dictionary<string, List<Slice>>();
		}

		/// <summary></summary>
		public IList this[IList keyList]
		{
			get
			{
				object result = m_table[keyList];
				if (result == null)
					return new object[0];
				else return (IList) result;
			}
		}

		/// <summary>
		/// Return a collection of the slices in the collection
		/// </summary>
		public IEnumerable<Slice> Values
		{
			get
			{
				foreach (ICollection list in m_table.Values)
					foreach (Slice item in list)
						yield return item;
				foreach (var list in m_slicesToReuse.Values)
					foreach (var item in list)
						yield return item;
			}
		}
		/// <summary>
		/// Add the item to the list associated with this key.
		/// </summary>
		public void Add(IList keyList, Slice obj)
		{
			ArrayList list = (ArrayList)(m_table[keyList]);
			if (list == null)
			{
				list = new ArrayList(1);
				m_table[keyList] = list;
			}
			list.Add(obj);
			List<Slice> reusableSlices;
			var key = obj.GetType().Name;
			if (!m_slicesToReuse.TryGetValue(key, out reusableSlices))
			{
				reusableSlices = new List<Slice>();
				m_slicesToReuse[key] = reusableSlices;
			}
			reusableSlices.Add(obj);
		}

		/// <summary></summary>
		public void ClearUnwantedPart(bool differentObject)
		{
			if (differentObject)
				m_table.Clear(); // no slice is safe to reuse without resetting it for a different root object.
			else
				m_slicesToReuse.Clear(); // ONLY want strict reuse (otherwise we lose closed/open states).
		}
		/// <summary>
		/// Remove the argument object from the indicated collection.
		/// Currently it is not considered an error if the object is not found, just nothing happens.
		/// </summary>
		public void Remove(IList keyList, Slice obj)
		{
			ArrayList list = (ArrayList)(m_table[keyList]);
			if (list != null)
				list.Remove(obj);
			List<Slice> reusableSlices;
			var key = obj.GetType().Name;
			if (m_slicesToReuse.TryGetValue(key, out reusableSlices))
			{
				reusableSlices.Remove(obj);
			}
		}

		/// <summary></summary>
		public Slice GetSliceToReuse(string className)
		{
			List<Slice> reusableSlices;
			if (m_slicesToReuse.TryGetValue(className, out reusableSlices))
			{
				if (reusableSlices.Count > 0) // may have used all that are available.
				{
					var result = reusableSlices[0];
					Remove(result.Key, result);
					return result;
				}
			}
			return null;
		}

		internal void Report()
		{
			int total = 0;
			foreach (var kvp in m_slicesToReuse)
			{
				Debug.WriteLine("  " + kvp.Key + ": " + kvp.Value.Count);
				total += kvp.Value.Count;
			}
			Debug.WriteLine("    total slices not reused: " + total);
		}
	}

	/// <summary></summary>
	public class ListHashCodeProvider : IEqualityComparer
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hash code.
		/// </summary>
		/// <param name="objList">The obj list.</param>
		/// ------------------------------------------------------------------------------------
		int IEqualityComparer.GetHashCode(object objList)
		{
			IList list = (IList) objList;
			int hash = 0;
			foreach (object obj in list)
			{
				// This ensures that two sequences containing the same boxed integer produce the same hash value.
				if (obj is int)
					hash += (int) obj;
				else
					hash += obj.GetHashCode();
			}
			return hash;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This comparer is only suitable for hash tables; it doesn't provide a valid
		/// (commutative) ordering of items.
		/// </summary>
		/// <param name="xArg">The x arg.</param>
		/// <param name="yArg">The y arg.</param>
		/// <returns><c>false</c> for any non-equal items.</returns>
		/// <remarks>Note that in general, boxed values are not equal, even if the unboxed
		/// values would be. The current code makes a special case for ints, which behave
		/// as expected.
		/// This used to be class ListComparer</remarks>
		/// ------------------------------------------------------------------------------------
		bool IEqualityComparer.Equals(object xArg, object yArg)
		{
			IList listX = (IList)xArg;
			IList listY = (IList)yArg;
			if (listX.Count != listY.Count)
				return false;
			for (int i = 0; i < listX.Count; i++)
			{
				object x = listX[i];
				object y = listY[i];
				if (x != y && !(x is int && y is int && ((int)x) == ((int)y)))
					return false;
			}
			return true;
		}
	}
}