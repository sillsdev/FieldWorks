using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// FdoInvertSet wraps another FdoSet and a hashset in such a way that the items in the wrapper are the items in the hashset
	/// that are NOT in the wrapped FdoSet.
	/// It is used to implement the various PublishIn properties, where the hashset is the LexDb.PublicationTypesOA.PossibilitiesOS.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public class FdoInvertSet<T> : IFdoSet<T> where T : class, ICmObject
	{
		// The set we are inverting.
		private IFdoSet<T> m_inverse;
		// The universe of objects which we are the other half of.
		private IEnumerable<T> m_universe;

		/// <summary>
		/// make one which wraps the given inverse set based on the given universe of objects which are in
		/// this set if not in the other one.
		/// </summary>
		public FdoInvertSet(IFdoSet<T> inverse, IEnumerable<T> universe)
		{
			m_inverse = inverse;
			m_universe = universe;
		}

		/// <summary>
		/// Returns an enumerator that iterates through the collection.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.IEnumerator`1"/> that can be used to iterate through the collection.
		/// </returns>
		/// <filterpriority>1</filterpriority>
		public IEnumerator<T> GetEnumerator()
		{
			var inverse = new HashSet<T>(m_inverse);
			return (m_universe.Where(obj => !inverse.Contains(obj))).GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through a collection.
		/// </summary>
		/// <returns>
		/// An <see cref="T:System.Collections.IEnumerator"/> object that can be used to iterate through the collection.
		/// </returns>
		/// <filterpriority>2</filterpriority>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		/// <summary>
		/// Adds an item to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <param name="item">The object to add to the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		///                 </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		///                 </exception>
		public void Add(T item)
		{
			m_inverse.Remove(item);
		}

		/// <summary>
		/// Removes all items from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		///                 </exception>
		public void Clear()
		{
			var items = ToArray(); // current value.
			Replace(items, new ICmObject[0]);
		}

		/// <summary>
		/// Determines whether the <see cref="T:System.Collections.Generic.ICollection`1"/> contains a specific value.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> is found in the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false.
		/// </returns>
		/// <param name="item">The object to locate in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		///                 </param>
		public bool Contains(T item)
		{
			foreach (var obj in this)
				if (obj == item)
					return true;
			return false;
		}

		/// <summary>
		/// Copies the elements of the <see cref="T:System.Collections.Generic.ICollection`1"/> to an <see cref="T:System.Array"/>, starting at a particular <see cref="T:System.Array"/> index.
		/// </summary>
		/// <param name="array">
		/// The one-dimensional <see cref="T:System.Array"/> that is the destination of the elements copied from <see cref="T:System.Collections.Generic.ICollection`1"/>. The <see cref="T:System.Array"/> must have zero-based indexing.
		/// </param>
		/// <param name="arrayIndex">
		/// The zero-based index in <paramref name="array"/> at which copying begins.
		/// </param>
		/// <exception cref="T:System.ArgumentNullException"><paramref name="array"/> is null.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="arrayIndex"/> is less than 0.</exception>
		/// <exception cref="T:System.ArgumentException">
		/// <paramref name="array"/> is multidimensional.
		/// -or-
		/// <paramref name="arrayIndex"/> is equal to or greater than the length of <paramref name="array"/>.
		/// -or-
		/// The number of elements in the source <see cref="T:System.Collections.Generic.ICollection`1"/> is greater than the available space from <paramref name="arrayIndex"/> to the end of the destination <paramref name="array"/>.
		/// -or-
		/// Type <paramref type="T"/> cannot be cast automatically to the type of the destination
		/// <paramref name="array"/>.
		/// </exception>
		public void CopyTo(T[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException();
			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException();
			int count = Count;
			// TODO: Check for multidimensional 'array' and throw ArgumentException, if it is.
			if (array.Length == 0)
				return;
			if (arrayIndex + count > array.Length)
				throw new ArgumentException();

			int index = arrayIndex;
			foreach (var obj in this)
			{
				array.SetValue(obj, index++);
			}
		}

		/// <summary>
		/// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// true if <paramref name="item"/> was successfully removed from the <see cref="T:System.Collections.Generic.ICollection`1"/>; otherwise, false. This method also returns false if <paramref name="item"/> is not found in the original <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		/// <param name="item">The object to remove from the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		///                 </param><exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		///                 </exception>
		public bool Remove(T item)
		{
			var result = Contains(item);
			if (result)
				m_inverse.Add(item);
			return result;
		}

		/// <summary>
		/// Gets the number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </summary>
		/// <returns>
		/// The number of elements contained in the <see cref="T:System.Collections.Generic.ICollection`1"/>.
		/// </returns>
		public int Count
		{
			get { return m_universe.Count() - m_inverse.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only.
		/// </summary>
		/// <returns>
		/// true if the <see cref="T:System.Collections.Generic.ICollection`1"/> is read-only; otherwise, false.
		/// </returns>
		public bool IsReadOnly
		{
			get { return m_inverse.IsReadOnly; }
		}

		/// <summary>
		/// Get an array of all the hvos.
		/// </summary>
		/// <returns></returns>
		public int[] ToHvoArray()
		{
			return (from obj in ToArray() select obj.Hvo).ToArray();
		}

		/// <summary>
		/// Get an array of all the Guids.
		/// </summary>
		/// <returns></returns>
		public Guid[] ToGuidArray()
		{
			return (from obj in ToArray() select obj.Guid).ToArray();
		}

		/// <summary>
		/// Allows getting the actual objects, without knowing the type parameter of the collection.
		/// </summary>
		public IEnumerable<ICmObject> Objects
		{
			get { return ToArray(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an array of all CmObjects.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public T[] ToArray()
		{
			return ((IEnumerable<T>)this).ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Replace the indicated objects (possibly none) with the new objects (possibly none).
		/// In the case of owning properties, the removed objects are really deleted; this code does
		/// not handle the possibility that some of them are included in thingsToAdd.
		/// The code will handle both newly created objects and ones being moved from elsewhere
		/// (but not from the same set, because that doesn't make sense).
		/// </summary>
		/// <param name="thingsToRemove">The things to remove.</param>
		/// <param name="thingsToAdd">The things to add.</param>
		/// ------------------------------------------------------------------------------------
		public void Replace(IEnumerable<ICmObject> thingsToRemove, IEnumerable<ICmObject> thingsToAdd)
		{
			// If there should be any overlap between thingsToRemove and thingsToAdd, we must remove it.
			// The normal semantics of this method is that both removing and adding an item leaves it in place.
			// In the real set, this is achieved by doing the remove fist; it has no effect on items already present.
			// With the inversion we perform here, we will first remove the item from the underlying set (no
			// effect, because it isn't there), then add it (which removes it from the inverted property).
			var thingsToReallyRemove =new HashSet<ICmObject>(thingsToRemove);
			thingsToReallyRemove.ExceptWith(thingsToAdd);
			var thingsToReallyAdd = new HashSet<ICmObject>(thingsToAdd);
			thingsToReallyAdd.ExceptWith(thingsToRemove);
			m_inverse.Replace(thingsToReallyAdd, thingsToReallyRemove);
		}
	}
}
