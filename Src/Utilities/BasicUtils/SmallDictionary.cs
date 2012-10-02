using System;
using System.Collections;
using System.Collections.Generic;

namespace SIL.Utils
{
	/// <summary>
	/// Small dictionary implements (most of?) IDictionary, using much less memory than a regular Dictionary.
	/// However, it's performance will be poor if there are frequent changes in the set of keys or a more than
	/// a few keys. It uses linear search and is optimized for a handful of items.
	/// </summary>
	public class SmallDictionary<Tkey, TValue> : IDictionary<Tkey, TValue>
	{
		private KeyValuePair<Tkey, TValue> m_first;
		private KeyValuePair<Tkey, TValue>[] m_others;

		#region IDictionary<Tkey,TValue> Members

		/// <summary>
		/// Put one in the dictionary; throws if already present.
		/// </summary>
		public void Add(Tkey key, TValue value)
		{
			if (key.Equals(default(Tkey)))
				throw new ArgumentException("SmallDictionary does not allow the default value of the key type to be used as a key");
			if (indexOfKey(key) != -2)
				throw new ArgumentException("Key " + key + " already present.");
			var newItem = new KeyValuePair<Tkey, TValue>(key, value);
			if (m_first.Key.Equals(default(Tkey)))
			{
				m_first = newItem;
				return;
			}
			if (m_others == null)
			{
				m_others = new KeyValuePair<Tkey, TValue>[1];
			}
			else
			{
				var temp = m_others;
				m_others = new KeyValuePair<Tkey, TValue>[temp.Length + 1];
				Array.Copy(temp, m_others, temp.Length);
			}
			m_others[m_others.Length - 1] = newItem;
		}
		/// <summary>
		/// Return the index at which the specified key was found in m_others, or -1 if found in m_first, or -2 if not found
		/// at all.
		/// </summary>
		/// <param name="key"></param>
		/// <returns></returns>
		int indexOfKey(Tkey key)
		{
			if (m_first.Key.Equals(key))
				return -1;
			if (m_others == null)
				return -2;
			for (int i = 0; i < m_others.Length; i++)
				if (m_others[i].Key.Equals(key))
					return i;
			return -2;
		}

		/// <summary>
		/// Return true if the dictionary contains the key.
		/// </summary>
		public bool ContainsKey(Tkey key)
		{
			return indexOfKey(key) != -2;
		}

		/// <summary>
		/// Get a collection of the keys. In this implementation it is a copy and will NOT
		/// change if the collection changes.
		/// </summary>
		public ICollection<Tkey> Keys
		{
			get
			{
				var result = new Tkey[Count];
				if (result.Length > 0)
					result[0] = m_first.Key;
				if (m_others != null)
					for (int i = 0; i < m_others.Length; i++ )
						result[i+1] = m_others[i].Key;
				return result;
			}
		}

		/// <summary>
		/// Remove the key (and value).
		/// Review JohnT: throw if not found??
		/// </summary>
		public bool Remove(Tkey key)
		{
			int index = indexOfKey(key);
			if (index == -2)
				return false;
			if (index == -1)
			{
				// Removing the first one.
				if (m_others == null)
				{
					// No others: make the special-case so it is empty.
					m_first = new KeyValuePair<Tkey, TValue>(default(Tkey), default(TValue));
					return true;
				}
				// Removing the first one, but there are others: copy the first thing in m_others to first
				m_first = m_others[0];
				// Now we've saved the first one from m_others in m_first, treat as if removing that first one.
				index = 0;
			}
			if (m_others.Length == 1)
			{
				m_others = null;
				return true;
			}
			var temp = m_others;
			m_others = new KeyValuePair<Tkey, TValue>[m_others.Length - 1];
			Array.Copy(temp, 0, m_others, 0, index); // copy ones before removed
			Array.Copy(temp, index + 1, m_others, index, m_others.Length - index); // copy ones after removed
			return true;
		}

		/// <summary>
		/// Retrieve value and return true, or retrieve null and return false if key not present.
		/// </summary>
		public bool TryGetValue(Tkey key, out TValue value)
		{
			int index = indexOfKey(key);
			if (index >= 0)
			{
				value = m_others[index].Value;
				return true;
			}
			if (index == -2)
			{
				value = default(TValue);
				return false;
			}
			value = m_first.Value;
			return true;
		}

		/// <summary>
		/// Get a collection of all the values. This is currently a copy.
		/// </summary>
		public ICollection<TValue> Values
		{
			get
			{
				var result = new TValue[Count];
				if (result.Length > 0)
					result[0] = m_first.Value;
				if (m_others != null)
					for (int i = 0; i < m_others.Length; i++)
						result[i + 1] = m_others[i].Value;
				return result;
			}
		}

		/// <summary>
		/// Set the value for the specified key (overwrite if present).
		/// </summary>
		public TValue this[Tkey key]
		{
			get
			{
				if (m_first.Key.Equals(key) && !key.Equals(default(Tkey)))
					return m_first.Value;
				if (m_others != null)
					foreach (var item in m_others)
						if (item.Key.Equals(key))
							return item.Value;
				if (key.Equals(default(Tkey)))
					throw new ArgumentException("Cannot use default type for Key in SmallDictionary");
				throw new KeyNotFoundException("Key " + key.ToString() + " not found.");
			}
			set
			{
				if (key.Equals(default(Tkey)))
					throw new ArgumentException("SmallDictionary does not allow the default value of the key type to be used as a key");
				var newItem = new KeyValuePair<Tkey, TValue>(key, value);
				if (m_first.Key.Equals(key) || m_first.Key.Equals(default(Tkey)))
				{
					m_first = newItem;
					return;
				}
				if (m_others == null)
				{
					m_others = new KeyValuePair<Tkey, TValue>[1];
				}
				else
				{
					for (int i = 0; i < m_others.Length; i++)
					{
						if (m_others[i].Key.Equals(key))
						{
							m_others[i] = newItem;
							return;
						}
					}
					var temp = m_others;
					m_others = new KeyValuePair<Tkey, TValue>[temp.Length + 1];
					Array.Copy(temp, m_others, temp.Length);
				}
				m_others[m_others.Length - 1] = newItem;
			}
		}

		#endregion

		#region ICollection<KeyValuePair<Tkey,TValue>> Members

		/// <summary>
		/// Another way to add an item.
		/// </summary>
		/// <param name="item"></param>
		public void Add(KeyValuePair<Tkey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// Remove all items.
		/// </summary>
		public void Clear()
		{
			m_others = null;
			m_first = new KeyValuePair<Tkey, TValue>(default(Tkey), default(TValue));
		}

		/// <summary>
		/// Another way to test presence of item
		/// </summary>
		public bool Contains(KeyValuePair<Tkey, TValue> item)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Copy it.
		/// </summary>
		/// <param name="array"></param>
		/// <param name="arrayIndex"></param>
		public void CopyTo(KeyValuePair<Tkey, TValue>[] array, int arrayIndex)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Number of items.
		/// </summary>
		public int Count
		{
			get
			{
				if (m_first.Key.Equals(default(Tkey)))
					return 0;
				if (m_others == null)
					return 1;
				return m_others.Length + 1;
			}
		}

		/// <summary>
		/// Can it be modified? Yes.
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		/// Cannot find any documentation that this is part of ICollection, and not sure what it should do
		/// if the key matches but the value does not. Leave unimplemented.
		/// </summary>
		public bool Remove(KeyValuePair<Tkey, TValue> item)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IEnumerable<KeyValuePair<Tkey,TValue>> Members

		/// <summary>
		/// Enumerate the keys and values.
		/// </summary>
		/// <returns></returns>

		public IEnumerator<KeyValuePair<Tkey, TValue>> GetEnumerator()
		{
			if (!m_first.Key.Equals(default(Tkey)))
				yield return m_first;
			if (m_others != null)
				foreach (var item in m_others)
					yield return item;
		}


		#endregion

		#region IEnumerable Members


		/// <summary>
		/// Another variation on enumerable. Doesn't seem to be useful.
		/// (Useful for FDOBrowser, though
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			if (m_others != null)
				foreach (var m in m_others)
					yield return m;

		}

		#endregion
	}
}
