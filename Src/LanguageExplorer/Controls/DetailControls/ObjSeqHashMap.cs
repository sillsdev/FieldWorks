// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// An object sequence hash map represents a mapping from sequences of objects to sequences of objects.
	/// The key may be anything that implements IList. The value is also a list.
	/// It is expected that very commonly only one value will be stored for a given key.
	/// The implementation is optimized for this by storing the object directly rather than a sequence
	/// holding the one object. Two objects are typically stored as an array, three or more as an ArrayList.
	/// (This means lists of lists don't work.)
	/// </summary>
	internal class ObjSeqHashMap
	{
		private readonly Hashtable m_table;
		// These are slices looked up by type name. The same ones as m_table. This supports reuse for different objects.
		private Dictionary<string, List<Slice>> m_slicesToReuse;

		/// <summary />
		public ObjSeqHashMap()
		{
			m_table = new Hashtable(new ListHashCodeProvider());
			m_slicesToReuse = new Dictionary<string, List<Slice>>();
		}

		/// <summary />
		public IList this[IList keyList]
		{
			get
			{
				var result = m_table[keyList];
				if (result == null)
				{
					return new object[0];
				}
				return (IList)result;
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
				{
					foreach (Slice item in list)
					{
						yield return item;
					}
				}
				foreach (var list in m_slicesToReuse.Values)
				{
					foreach (var item in list)
					{
						yield return item;
					}
				}
			}
		}

		/// <summary>
		/// Add the item to the list associated with this key.
		/// </summary>
		public void Add(IList keyList, Slice obj)
		{
			var list = (ArrayList)m_table[keyList];
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

		/// <summary />
		public void ClearUnwantedPart(bool differentObject)
		{
			if (differentObject)
			{
				// no slice is safe to reuse without resetting it for a different root object.
				m_table.Clear();
			}
			else
			{
				// ONLY want strict reuse (otherwise we lose closed/open states).
				m_slicesToReuse.Clear();
			}
		}

		/// <summary>
		/// Remove the argument object from the indicated collection.
		/// Currently it is not considered an error if the object is not found, just nothing happens.
		/// </summary>
		public void Remove(IList keyList, Slice obj)
		{
			var list = (ArrayList)m_table[keyList];
			list?.Remove(obj);
			List<Slice> reusableSlices;
			var key = obj.GetType().Name;
			if (m_slicesToReuse.TryGetValue(key, out reusableSlices))
			{
				reusableSlices.Remove(obj);
			}
		}

		/// <summary />
		public Slice GetSliceToReuse(string className)
		{
			List<Slice> reusableSlices;
			if (!m_slicesToReuse.TryGetValue(className, out reusableSlices))
			{
				return null;
			}
			if (!reusableSlices.Any())
			{
				return null;
			}
			var result = reusableSlices[0];
			Remove(result.Key, result);
			return result;
		}

		/// <summary>
		/// Debugging code for investigating slice reuse bugs.
		/// </summary>
		internal void Report()
		{
			var total = 0;
			foreach (var kvp in m_slicesToReuse)
			{
				Debug.WriteLine("  " + kvp.Key + ": " + kvp.Value.Count);
				total += kvp.Value.Count;
			}
			Debug.WriteLine("    total slices not reused: " + total);
		}
	}
}