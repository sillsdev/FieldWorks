// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: BidiDictionary.cs

using System;
using System.Collections.Generic;

namespace SIL.Utils
{
	/// <summary>
	/// This is a dictionary guaranteed to have only one of each value and key.
	/// It may be searched either by TFirst or by TSecond, giving a unique answer because it is 1 to 1.
	/// </summary>
	/// <typeparam name="TFirst">The type of the "key"</typeparam>
	/// <typeparam name="TSecond">The type of the "value"</typeparam>
	public class BidiDictionary<TFirst, TSecond>
	{
		readonly IDictionary<TFirst, TSecond> m_firstToSecond = new Dictionary<TFirst, TSecond>();
		readonly IDictionary<TSecond, TFirst> m_secondToFirst = new Dictionary<TSecond, TFirst>();

		#region Exception throwing methods

		/// <summary>
		/// Tries to add the pair to the dictionary.
		/// Throws an exception if either element is already in the dictionary
		/// </summary>
		public void Add(TFirst first, TSecond second)
		{
			if (m_firstToSecond.ContainsKey(first))
				throw new ArgumentException("Duplicate first.");
			if (m_secondToFirst.ContainsKey(second))
				throw new ArgumentException("Duplicate second.");

			m_firstToSecond.Add(first, second);
			m_secondToFirst.Add(second, first);
		}

		/// <summary>
		/// Tries to add the pair to the dictionary.
		/// Throws an exception if either element is already in the dictionary
		/// </summary>
		public void Add(TSecond second, TFirst first)
		{
			if (m_firstToSecond.ContainsKey(first))
				throw new ArgumentException("Duplicate first.");
			if (m_secondToFirst.ContainsKey(second))
				throw new ArgumentException("Duplicate second.");

			m_firstToSecond.Add(first, second);
			m_secondToFirst.Add(second, first);
		}

		/// <summary>
		/// Get by 'first'
		/// </summary>
		/// <param name="first"></param>
		/// <returns></returns>
		public TSecond Get(TFirst first)
		{
			return m_firstToSecond[first];
		}

		/// <summary>
		/// Get by 'second'
		/// </summary>
		/// <param name="second"></param>
		/// <returns></returns>
		public TFirst Get(TSecond second)
		{
			return m_secondToFirst[second];
		}

		/// <summary>
		/// Remove the record containing first.
		/// </summary>
		/// <param name="first">the key of the record to delete</param>
		/// <returns>true, if 'first' was in the dictioanry, otherwise, false.</returns>
		public bool Remove(TFirst first)
		{
			TSecond second;
			if (!m_firstToSecond.TryGetValue(first, out second))
				return false;
			m_firstToSecond.Remove(first);
			m_secondToFirst.Remove(second);
			return true;
		}

		/// <summary>
		/// Remove the record containing second.
		/// </summary>
		/// <param name="second">the key of the record to delete</param>
		/// <returns>true, if 'second' was in the dictioanry, otherwise, false.</returns>
		public bool Remove(TSecond second)
		{
			TFirst first;
			if (!m_secondToFirst.TryGetValue(second, out first))
				return false;
			m_secondToFirst.Remove(second);
			m_firstToSecond.Remove(first);
			return true;
		}

		#endregion

		#region Try methods

		/// <summary>
		/// Find the TSecond corresponding to the TFirst first.
		/// Returns false if first is not in the dictionary.
		/// </summary>
		/// <param name="first">the key to search for</param>
		/// <param name="second">the corresponding value</param>
		/// <returns>true if first is in the dictionary, false otherwise</returns>
		public bool TryGetValue(TFirst first, out TSecond second)
		{
			return m_firstToSecond.TryGetValue(first, out second);
		}

		/// <summary>
		/// Find the TFirst corresponding to the TSecond second.
		/// Returns false if second is not in the dictionary.
		/// </summary>
		/// <param name="second">the key to search for</param>
		/// <param name="first">the corresponding value</param>
		/// <returns>true if second is in the dictionary, false otherwise</returns>
		public bool TryGetValue(TSecond second, out TFirst first)
		{
			return m_secondToFirst.TryGetValue(second, out first);
		}

		#endregion

		/// <summary>
		/// The number of pairs stored in the dictionary
		/// </summary>
		public int Count
		{
			get { return m_firstToSecond.Count; }
		}

		/// <summary>
		/// Removes all items from the dictionary.
		/// </summary>
		public void Clear()
		{
			m_firstToSecond.Clear();
			m_secondToFirst.Clear();
		}
	}

}
