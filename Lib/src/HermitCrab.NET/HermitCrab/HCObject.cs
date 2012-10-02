using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace SIL.HermitCrab
{
	public class HCObject : IComparable<HCObject>, IComparable
	{
		public static bool operator ==(HCObject o1, HCObject o2)
		{
			if (object.ReferenceEquals(o1, o2))
				return true;
			if ((object) o1 == null || (object) o2 == null)
				return false;
			return o1.Equals(o2);
		}

		public static bool operator !=(HCObject o1, HCObject o2)
		{
			return !(o1 == o2);
		}

		string m_id;
		string m_desc;
		Morpher m_morpher;

		public HCObject(string id, string desc, Morpher morpher)
		{
			m_id = id;
			m_desc = desc;
			m_morpher = morpher;
		}

		public HCObject(HCObject obj)
		{
			m_id = obj.m_id;
			m_desc = obj.m_desc;
			m_morpher = obj.m_morpher;
		}

		public string ID
		{
			get
			{
				return m_id;
			}
		}

		public string Description
		{
			get
			{
				return m_desc;
			}
		}

		public Morpher Morpher
		{
			get
			{
				return m_morpher;
			}
		}

		public int CompareTo(HCObject other)
		{
			if (other == null)
				return 1;
			return m_id.CompareTo(other.m_id);
		}

		public int CompareTo(object other)
		{
			if (!(other is HCObject))
				throw new ArgumentException();
			return CompareTo(other as HCObject);
		}

		public override int GetHashCode()
		{
			return m_id.GetHashCode();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as HCObject);
		}

		public bool Equals(HCObject other)
		{
			if (other == null)
				return false;
			return m_id == other.m_id;
		}

		public override string ToString()
		{
			return m_desc;
		}
	}

	public class HCObjectSet<T> : KeyedCollection<string, T> where T : HCObject
	{
		public HCObjectSet()
			: base()
		{
		}

		public HCObjectSet(IEnumerable<T> items)
			: base()
		{
			AddMany(items);
		}

		public void AddMany(IEnumerable<T> items)
		{
			foreach (T item in items)
				Add(item);
		}

		public bool TryGetValue(string id, out T value)
		{
			if (Dictionary == null)
			{
				value = null;
				return false;
			}

			return Dictionary.TryGetValue(id, out value);
		}

		public HCObjectSet<T> Intersection(IEnumerable<T> items)
		{
			HCObjectSet<T> result = new HCObjectSet<T>();
			foreach (T item in items)
			{
				if (Contains(item))
					result.Add(item);
			}
			return result;
		}

		public HCObjectSet<T> Union(IEnumerable<T> items)
		{
			HCObjectSet<T> result = new HCObjectSet<T>(this);
			result.AddMany(items);
			return result;
		}

		public HCObjectSet<T> Difference(IEnumerable<T> items)
		{
			HCObjectSet<T> result = new HCObjectSet<T>();
			foreach (T item in items)
			{
				if (!Contains(item))
					result.Add(item);
			}
			return result;
		}

		protected override string GetKeyForItem(T item)
		{
			return item.ID;
		}

		protected override void InsertItem(int index, T item)
		{
			if (Contains(item.ID))
			{
				int oldIndex = IndexOf(item);
				Remove(item.ID);
				if (oldIndex < index)
					index--;
			}
			base.InsertItem(index, item);
		}

		protected override void SetItem(int index, T item)
		{
			if (Contains(item.ID))
			{
				int oldIndex = IndexOf(item);
				if (oldIndex != index)
				{
					Remove(item.ID);
					if (oldIndex < index)
						index--;
				}
			}
			base.SetItem(index, item);
		}

		public override string ToString()
		{
			bool firstItem = true;
			StringBuilder sb = new StringBuilder();
			foreach (T item in this)
			{
				if (!firstItem)
					sb.Append(", ");
				sb.Append(item.Description);
				firstItem = false;
			}
			return sb.ToString();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as HCObjectSet<T>);
		}

		public bool Equals(HCObjectSet<T> other)
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
	}
}
