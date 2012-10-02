// Copyright (c) 2004, Rüdiger Klaehn
// All rights reserved.
//
// Redistribution and use in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//
// * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
// * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
// * Neither the name of lambda computing nor the names of its contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
#region Using directives

using System;
using System.Collections;
using System.Collections.Generic;
//using System.Text;

#endregion

namespace SIL.FieldWorks.Common.Utils
{
	/// <summary>
	///
	/// </summary>
	/// <typeparam name="T"></typeparam>
	[Serializable]
	public sealed class Set<T> : ICollection<T>, IEnumerable<T>, IEnumerable // , ICollection
	{
		#region Data members

		private Dictionary<T, bool> m_data;

		#endregion Data members

		#region Construction

		/// <summary>
		///
		/// </summary>
		public Set()
		{
			m_data = new Dictionary<T, bool>();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="capacity"></param>
		public Set(int capacity)
		{
			m_data = new Dictionary<T, bool>(capacity);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="original"></param>
		public Set(Set<T> original)
		{
			m_data = new Dictionary<T, bool>(original.m_data);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="original"></param>
		public Set(IEnumerable<T> original)
		{
			m_data = new Dictionary<T, bool>();
			AddRange(original);
		}

		/// <summary>
		///
		/// </summary>
		public static Set<T> Empty
		{
			get { return new Set<T>(0); }
		}

		#endregion Construction

		#region Other Methods

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public T[] ToArray()
		{
			T[] result = new T[m_data.Count];
			m_data.Keys.CopyTo(result, 0);
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="range"></param>
		public void AddRange(IEnumerable<T> range)
		{
			foreach (T a in range)
				Add(a);
		}

#if MAYBEADDLATER
		/// <summary>
		///
		/// </summary>
		/// <typeparam name="U"></typeparam>
		/// <param name="converter"></param>
		/// <returns></returns>
		public Set<U> ConvertAll<U>(Converter<T, U> converter)
		{
			Set<U> result = new Set<U>(this.Count);
			foreach (T element in this)
				result.Add(converter(element));
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public bool TrueForAll(Predicate<T> predicate)
		{
			foreach (T element in this)
				if (!predicate(element))
					return false;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="predicate"></param>
		/// <returns></returns>
		public Set<T> FindAll(Predicate<T> predicate)
		{
			Set<T> result = new Set<T>();
			foreach (T element in this)
				if (predicate(element))
					result.Add(element);
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="action"></param>
		public void ForEach(Action<T> action)
		{
			foreach (T element in this)
				action(element);
		}
#endif

		#endregion Other Methods

		#region static set operators and matching methods

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Set<T> operator |(Set<T> a, Set<T> b)
		{
			Set<T> result = new Set<T>(a);
			result.AddRange(b);
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public Set<T> Union(IEnumerable<T> b)
		{
			return this | new Set<T>(b);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Set<T> operator &(Set<T> a, Set<T> b)
		{
			Set<T> result = new Set<T>();
			foreach (T element in a)
				if (b.Contains(element))
					result.Add(element);
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public Set<T> Intersection(IEnumerable<T> b)
		{
			return this & new Set<T>(b);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Set<T> operator -(Set<T> a, Set<T> b)
		{
			Set<T> result = new Set<T>();
			foreach (T element in a)
				if (!b.Contains(element))
					result.Add(element);
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public Set<T> Difference(IEnumerable<T> b)
		{
			return this - new Set<T>(b);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static Set<T> operator ^(Set<T> a, Set<T> b)
		{
			Set<T> result = new Set<T>();
			foreach (T element in a)
				if (!b.Contains(element))
					result.Add(element);
			foreach (T element in b)
				if (!a.Contains(element))
					result.Add(element);
			return result;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="b"></param>
		/// <returns></returns>
		public Set<T> SymmetricDifference(IEnumerable<T> b)
		{
			return this ^ new Set<T>(b);
		}

		#endregion static set operators and matching methods

		#region Relational operators

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator <=(Set<T> a, Set<T> b)
		{
			if ((a == null) || (b == null))
				return false;

			foreach (T element in a)
				if (!b.Contains(element))
					return false;
			return true;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator <(Set<T> a,Set<T> b)
		{
			return (a != null) && (b != null) && (a.Count < b.Count) && (a <= b);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator ==(Set<T> a, Set<T> b)
		{
			if ((Object)a == null && (Object)b == null)
				return true;
			if ((Object)a == null || (Object)b == null)
				return false;
			return (a.Count == b.Count) && (a <= b);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator >(Set<T> a, Set<T> b)
		{
			return b < a;
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator >=(Set<T> a, Set<T> b)
		{
			return (b <= a);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static bool operator !=(Set<T> a, Set<T> b)
		{
			if ((Object)a == null && (Object)b == null)
				return false;
			if ((Object)a == null || (Object)b == null)
				return true;
			return !(a == b);
		}

		#endregion Relational operators

		#region Object overrides

		/// <summary>
		///
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public override bool Equals(object obj)
		{
			Set<T> a = this;
			Set<T> b = obj as Set<T>;
			if (b == null)
				return false;
			return a == b;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override int GetHashCode()
		{
			int hashcode = 0;
			foreach (T element in this)
				hashcode ^= element.GetHashCode();
			return hashcode;
		}

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			return m_data.ToString();
		}

		#endregion Object overrides

		#region ICollection<T> implementation

		/// <summary>
		///
		/// </summary>
		public int Count
		{
			get { return m_data.Count; }
		}

		/// <summary>
		///
		/// </summary>
		public bool IsReadOnly
		{
			get { return false; }
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		public void Add(T a)
		{
			m_data[a] = true;
		}

		/// <summary>
		///
		/// </summary>
		public void Clear()
		{
			m_data.Clear();
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public bool Contains(T a)
		{
			return m_data.ContainsKey(a);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="array"></param>
		/// <param name="index"></param>
		public void CopyTo(T[] array, int index)
		{
			m_data.Keys.CopyTo(array, index);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="a"></param>
		/// <returns></returns>
		public bool Remove(T a)
		{
			return m_data.Remove(a);
		}

		#endregion ICollection<T> implementation

		#region IEnumerable<T> implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		public IEnumerator<T> GetEnumerator()
		{
			return m_data.Keys.GetEnumerator();
		}

		#endregion IEnumerable<T> implementation

		#region IEnumerable explicit implementation

		/// <summary>
		///
		/// </summary>
		/// <returns></returns>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable)m_data.Keys).GetEnumerator();
		}

		#endregion IEnumerable explicit implementation

#if ImplementICollection
		#region ICollection implementation

			/// <summary>
			///
			/// </summary>
			/// <param name="array"></param>
			/// <param name="index"></param>
			void ICollection.CopyTo(Array array, int index)
			{
				((ICollection)m_data.Keys).CopyTo(array, index);
			}

			/// <summary>
			///
			/// </summary>
			object ICollection.SyncRoot
			{
				get { return ((ICollection)m_data.Keys).SyncRoot; }
			}

			/// <summary>
			///
			/// </summary>
			bool ICollection.IsSynchronized
			{
				get { return ((ICollection)m_data.Keys).IsSynchronized; }
			}

		#endregion ICollection implementation
#endif
	}
}
