//-------------------------------------------------------------------------------------------------
// <copyright file="CloneableCollection.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Abstract base class for a cloneable collection.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudioInfrastructure
{
	using System;
	using System.Collections;

	/// <summary>
	/// Abstract base class for a cloneable collection, which can take on the behavior of either
	/// a list or a dictionary.
	/// </summary>
	public abstract class CloneableCollection : DirtyableObject, ICollection, ICloneable
	{
		#region Member Variables
		//==========================================================================================
		// Member Variables
		//==========================================================================================

		private static readonly Type classType = typeof(CloneableCollection);

		private IList innerList;
		private IDictionary innerDictionary;
		#endregion

		#region Constructors
		//==========================================================================================
		// Constructors
		//==========================================================================================

		/// <summary>
		/// Initializes a new instance of the <see cref="CloneableCollection"/> class, specifying
		/// that it should take on the behavior of a list.
		/// </summary>
		/// <param name="innerList">The data container for the items.</param>
		protected CloneableCollection(IList innerList)
		{
			Tracer.VerifyNonNullArgument(innerList, "innerList");
			this.innerList = innerList;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CloneableCollection"/> class, specifying
		/// that it should take on the behavior of a dictionary.
		/// </summary>
		/// <param name="innerDictionary">The data container for the items.</param>
		protected CloneableCollection(IDictionary innerDictionary)
		{
			Tracer.VerifyNonNullArgument(innerDictionary, "innerDictionary");
			this.innerDictionary = innerDictionary;
		}
		#endregion

		#region Properties
		//==========================================================================================
		// Properties
		//==========================================================================================

		/// <summary>
		/// Gets the number of items in the collection.
		/// </summary>
		public int Count
		{
			get { return this.InnerCollection.Count; }
		}

		/// <summary>
		/// Gets a value indicating whether this collection is synchronized (thread-safe).
		/// </summary>
		public bool IsSynchronized
		{
			get { return this.InnerCollection.IsSynchronized; }
		}

		/// <summary>
		/// Gets an object that can be used for synchronizing access to this instance.
		/// </summary>
		public object SyncRoot
		{
			get { return this.InnerCollection.SyncRoot; }
		}

		/// <summary>
		/// Returns a value indicating whether one or more contained <see cref="IDirtyable"/> objects
		/// are dirty.
		/// </summary>
		protected override bool AreContainedObjectsDirty
		{
			get
			{
				foreach (object obj in this.InnerCollection)
				{
					IDirtyable dirtyableObj = obj as IDirtyable;
					if (dirtyableObj != null && dirtyableObj.IsDirty)
					{
						return true;
					}
				}

				return false;
			}
		}

		/// <summary>
		/// Gets the inner <see cref="ICollection"/> interface.
		/// </summary>
		protected ICollection InnerCollection
		{
			get
			{
				if (this.innerList != null)
				{
					return this.innerList;
				}
				return this.innerDictionary;
			}
		}
		#endregion

		#region Methods
		//==========================================================================================
		// Methods
		//==========================================================================================

		/// <summary>
		/// Clones the collection by performing a deep copy if the elements implement <see cref="ICloneable"/>,
		/// otherwise a shallow copy is performed.
		/// </summary>
		/// <returns>A clone of this object.</returns>
		public abstract object Clone();

		/// <summary>
		/// Copies this collection to the specified array starting at the specified index.
		/// Performs a shallow copy of the elements.
		/// </summary>
		/// <param name="array">The destination array, which will contain a copy of this collection.</param>
		/// <param name="index">The index from which to start copying.</param>
		public virtual void CopyTo(Array array, int index)
		{
			this.InnerCollection.CopyTo(array, index);
		}

		/// <summary>
		/// Gets the enumerator for this collection.
		/// </summary>
		/// <returns>An <see cref="IEnumerator"/> object that can be used for iterating over the collection.</returns>
		public virtual IEnumerator GetEnumerator()
		{
			return this.InnerCollection.GetEnumerator();
		}

		/// <summary>
		/// Copies the elements of the <see cref="CloneableCollection"/> to a new <see cref="Object"/> array.
		/// Performs a shallow copy of the elements.
		/// </summary>
		/// <returns>An <see cref="Object"/> array containing shallow copies of the elements of the <see cref="CloneableCollection"/>.</returns>
		public virtual object[] ToArray()
		{
			object[] copy = new object[this.Count];
			this.CopyTo(copy, 0);
			return copy;
		}

		/// <summary>
		/// Copies the elements of the <see cref="CloneableCollection"/> to a new array of the specified type.
		/// Performs a shallow copy of the elements.
		/// </summary>
		/// <param name="type">The <see cref="Type"/> of the array to create and copy elements to.</param>
		/// <returns>An array of the specified type containing shallow copies of the elements of the <see cref="CloneableCollection"/>.</returns>
		public virtual Array ToArray(Type type)
		{
			Tracer.VerifyNonNullArgument(type, "type");
			Array copy = Array.CreateInstance(type, this.Count);
			this.CopyTo(copy, 0);
			return copy;
		}

		/// <summary>
		/// Clears the dirty flag for any contained <see cref="IDirtyable"/> objects.
		/// </summary>
		protected override void ClearDirtyOnContainedObjects()
		{
			foreach (object obj in this.InnerCollection)
			{
				IDirtyable dirtyableObj = obj as IDirtyable;
				if (dirtyableObj != null)
				{
					dirtyableObj.ClearDirty();
				}
			}
		}

		/// <summary>
		/// Clones this object into the specified object by performing deep copy if the elements implement
		/// <see cref="ICloneable"/>, otherwise a shallow copy is performed.
		/// </summary>
		/// <param name="clone">The object to clone this object into.</param>
		protected void CloneInto(CloneableCollection clone)
		{
			if (this.innerList != null)
			{
				this.CloneIntoList(clone);
			}
			else if (this.innerDictionary != null)
			{
				this.CloneIntoDictionary(clone);
			}
			else
			{
				Tracer.Fail("The CloneableCollection is in an illegal state.");
			}
		}

		/// <summary>
		/// Clones this collection (acting as a dictionary) into the specified object by performing a deep copy
		/// if the elements implement <see cref="ICloneable"/>, otherwise a shallow copy is performed.
		/// </summary>
		/// <param name="clone">The object to clone this object into.</param>
		private void CloneIntoDictionary(CloneableCollection clone)
		{
			foreach (DictionaryEntry entry in this.innerDictionary)
			{
				object clonedKey = entry.Key;
				object clonedValue = entry.Value;
				if (clonedKey is ICloneable)
				{
					clonedKey = ((ICloneable)clonedKey).Clone();
				}
				if (clonedValue is ICloneable)
				{
					clonedValue = ((ICloneable)clonedValue).Clone();
				}
				clone.innerDictionary.Add(clonedKey, clonedValue);
			}
		}

		/// <summary>
		/// Clones this collection (acting as a list) into the specified object by performing a deep copy
		/// if the elements implement <see cref="ICloneable"/>, otherwise a shallow copy is performed.
		/// </summary>
		/// <param name="clone">The object to clone this object into.</param>
		private void CloneIntoList(CloneableCollection clone)
		{
			foreach (object element in this.innerList)
			{
				object clonedElement = element;
				if (element is ICloneable)
				{
					clonedElement = ((ICloneable)element).Clone();
				}
				clone.innerList.Add(clonedElement);
			}
		}
		#endregion
	}
}