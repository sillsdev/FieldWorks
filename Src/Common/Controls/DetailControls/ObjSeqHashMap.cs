using System;
using System.Collections;


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
		public ObjSeqHashMap()
		{
			m_table = new Hashtable(new ListHashCodeProvider());
		}
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
		/// Return a collection of the lists that are the values stored in the collection.
		/// </summary>
		public ICollection Values
		{
			get { return m_table.Values; }
		}
		/// <summary>
		/// Add the item to the list associated with this key.
		/// </summary>
		/// <param name="keyList"></param>
		/// <param name="obj"></param>
		public void Add(IList keyList, object obj)
		{
			ArrayList list = (ArrayList)(m_table[keyList]);
			if (list == null)
			{
				list = new ArrayList(1);
				m_table[keyList] = list;
			}
			list.Add(obj);
		}
		/// <summary>
		/// Remove the argument object from the indicated collection.
		/// Currently it is not considered an error if the object is not found, just nothing happens.
		/// </summary>
		/// <param name="keyList"></param>
		/// <param name="obj"></param>
		public void Remove(IList keyList, object obj)
		{
			ArrayList list = (ArrayList)(m_table[keyList]);
			if (list == null)
				return;
			list.Remove(obj);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ListHashCodeProvider : IEqualityComparer
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the hash code.
		/// </summary>
		/// <param name="objList">The obj list.</param>
		/// <returns></returns>
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
