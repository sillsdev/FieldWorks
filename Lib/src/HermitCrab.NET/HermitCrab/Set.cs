using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	class Set<T> : ICollection<T>
	{
		Dictionary<T, object> m_dictionary;

		public Set()
			: this(null, null)
		{
		}

		public Set(IEnumerable<T> items)
			: this(items, null)
		{
		}

		public Set(IEqualityComparer<T> comparer)
			: this(null, comparer)
		{
		}

		public Set(IEnumerable<T> items, IEqualityComparer<T> comparer)
		{
			m_dictionary = new Dictionary<T, object>(comparer);
			if (items != null)
				AddMany(items);
		}

		public int Count
		{
			get
			{
				return m_dictionary.Count;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		public void Add(T item)
		{
			m_dictionary[item] = null;
		}

		public void Clear()
		{
			m_dictionary.Clear();
		}

		public bool Contains(T item)
		{
			return m_dictionary.ContainsKey(item);
		}

		public void CopyTo(T[] array, int arrayIndex)
		{
			m_dictionary.Keys.CopyTo(array, arrayIndex);
		}

		public IEnumerator<T> GetEnumerator()
		{
			return m_dictionary.Keys.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		public bool Remove(T item)
		{
			return m_dictionary.Remove(item);
		}

		public void AddMany(IEnumerable<T> items)
		{
			foreach (T item in items)
				Add(item);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Set<T>);
		}

		public bool Equals(Set<T> other)
		{
			if (other == null)
				return false;

			if (Count != other.Count)
				return false;

			foreach (T item in this)
			{
				if (!other.Contains(item))
					return false;
			}

			return true;
		}

		public override int GetHashCode()
		{
			int hashCode = 0;
			foreach (T item in this)
			{
				hashCode ^= item.GetHashCode();
			}
			return hashCode;
		}

		public override string ToString()
		{
			StringBuilder sb = new StringBuilder();
			bool firstItem = true;
			foreach (T item in this)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(item.ToString());
				firstItem = false;
			}
			return sb.ToString();
		}
	}
}
