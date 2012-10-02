//---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2002' to='2003' company='SIL International'>
//    Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlAssemblyCollection.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// Implementation of strongly-typed collection XmlAssemblyCollection
//
// Automatically created by CollectionGeneratorWizard
// </remarks>
//---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.Build.Tasks
{
	using System;
	using System.Collections;


	/// <summary>
	/// A collection that stores <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> objects.
	/// </summary>
	/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/>
	[Serializable()]
	public class XmlAssemblyCollection : CollectionBase
	{
		private Hashtable m_Hashtable = new Hashtable();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlAssemblyCollection()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> based on another <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/>.
		/// </summary>
		/// <param name='value'>
		/// A <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> from which the contents are copied
		/// </param>
		/// ------------------------------------------------------------------------------------
		public XmlAssemblyCollection(XmlAssemblyCollection value)
		{
			this.AddRange(value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> containing any array of <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> objects.
		/// </summary>
		/// <param name='value'>
		/// A array of <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> objects with which to intialize the collection
		/// </param>
		/// ------------------------------------------------------------------------------------
		public XmlAssemblyCollection(XmlAssembly[] value)
		{
			this.AddRange(value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Represents the entry at the specified index of the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/>.
		/// </summary>
		/// <param name='index'>The zero-based index of the entry to locate in the collection.</param>
		/// <value>
		/// The entry at the specified index of the collection.
		/// </value>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='index'/> is outside the valid range of indexes for the collection.</exception>
		/// ------------------------------------------------------------------------------------
		public XmlAssembly this[int index]
		{
			get
			{
				return ((XmlAssembly)(List[index]));
			}
			set
			{
				List[index] = value;
				m_Hashtable[value.AssemblyName] = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets an assembly
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlAssembly this[string key]
		{
			get
			{
				return (XmlAssembly)m_Hashtable[key];
			}
			set
			{
				Add(value);
			}
		}

		/// <summary>Occurs before a <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> is inserted in the collection.</summary>
		public event CollectionChange BeforeInsert;

		/// <summary>Occurs before a <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> is removed from the collection.</summary>
		public event CollectionChange BeforeRemove;

		/// <summary>Occurs before the collection is cleared.</summary>
		public event CollectionClear BeforeClear;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> with the specified value to the
		/// <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> .
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> to add.</param>
		/// <returns>The index at which the new element was inserted.</returns>
		/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection.AddRange(
		/// SIL.FieldWorks.Build.Tasks.XmlAssembly[])'/>
		/// ------------------------------------------------------------------------------------
		public int Add(XmlAssembly value)
		{
			if (List.Contains(value))
				List.Remove(value);

			m_Hashtable[value.AssemblyName] = value;
			return List.Add(value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the elements of an array to the end of the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/>.
		/// </summary>
		/// <param name='value'>
		/// An array of type <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> containing the objects to add to the collection.
		/// </param>
		/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection.Add'/>
		/// ------------------------------------------------------------------------------------
		public void AddRange(XmlAssembly[] value)
		{
			for (int i = 0; (i < value.Length); i = (i + 1))
			{
				this.Add(value[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the contents of another <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> to the end of the collection.
		/// </summary>
		/// <param name='value'>
		/// A <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> containing the objects to add to the collection.
		/// </param>
		/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection.Add'/>
		/// ------------------------------------------------------------------------------------
		public void AddRange(XmlAssemblyCollection value)
		{
			for (int i = 0; (i < value.Count); i = (i + 1))
			{
				this.Add(value[i]);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the
		/// <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> contains the specified <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/>.
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> to locate.</param>
		/// <returns>
		/// <c>true</c> if the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> is contained in the collection;
		/// otherwise, <c>false</c>.
		/// </returns>
		/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection.IndexOf'/>
		/// ------------------------------------------------------------------------------------
		public bool Contains(XmlAssembly value)
		{
			return List.Contains(value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Copies the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> values to a one-dimensional <see cref='System.Array'/> instance at the
		/// specified index.
		/// </summary>
		/// <param name='array'>The one-dimensional <see cref='System.Array'/> that is the destination of the values copied from <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> .</param>
		/// <param name='index'>The index in <paramref name='array'/> where copying begins.</param>
		/// <exception cref='System.ArgumentException'><para><paramref name='array'/> is multidimensional.</para> <para>-or-</para> <para>The number of elements in the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> is greater than the available space between <paramref name='arrayIndex'/> and the end of <paramref name='array'/>.</para></exception>
		/// <exception cref='System.ArgumentNullException'><paramref name='array'/> is <see langword='null'/>. </exception>
		/// <exception cref='System.ArgumentOutOfRangeException'><paramref name='arrayIndex'/> is less than <paramref name='array'/>'s lowbound. </exception>
		/// <seealso cref='System.Array'/>
		/// ------------------------------------------------------------------------------------
		public void CopyTo(XmlAssembly[] array, int index)
		{
			List.CopyTo(array, index);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the index of a <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> in
		/// the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> .
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> to locate.</param>
		/// <returns>
		/// The index of the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> of <paramref name='value'/> in the
		/// <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/>, if found; otherwise, -1.
		/// </returns>
		/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection.Contains'/>
		/// ------------------------------------------------------------------------------------
		public int IndexOf(XmlAssembly value)
		{
			return List.IndexOf(value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inserts a <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> into the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> at the specified index.
		/// </summary>
		/// <param name='index'>The zero-based index where <paramref name='value'/> should be inserted.</param>
		/// <param name=' value'>The <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> to insert.</param>
		/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection.Add'/>
		/// ------------------------------------------------------------------------------------
		public void Insert(int index, XmlAssembly value)
		{
			if (List.Contains(value))
				List.Remove(value);
			List.Insert(index, value);

			m_Hashtable[value.AssemblyName] = value;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns an enumerator that can iterate through
		/// the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> .
		/// </summary>
		/// <seealso cref='System.Collections.IEnumerator'/>
		/// ------------------------------------------------------------------------------------
		public new XmlAssemblyEnumerator GetEnumerator()
		{
			return new XmlAssemblyEnumerator(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes a specific <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> from the
		/// <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> .
		/// </summary>
		/// <param name='value'>The <see cref='SIL.FieldWorks.Build.Tasks.XmlAssembly'/> to remove from the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> .</param>
		/// <exception cref='System.ArgumentException'><paramref name='value'/> is not found in the Collection. </exception>
		/// ------------------------------------------------------------------------------------
		public void Remove(XmlAssembly value)
		{
			List.Remove(value);

			m_Hashtable.Remove(value.AssemblyName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs additional custom processes before inserting a new element into the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> instance .
		/// </summary>
		/// <param name='index'>The zero-based index at which to insert <paramref name='value'/>.</param>
		/// <param name='value'>The new value of the element at <paramref name='index'/>.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnInsert(int index, object value)
		{
			if ((BeforeInsert != null))
			{
				BeforeInsert(index, value);
			}
			base.OnInsert(index, value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs additional custom processes before removing an element from the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> instance.
		/// </summary>
		/// <param name='index'>The zero-based index at which to insert <paramref name='value'/>.</param>
		/// <param name='value'>The new value of the element at <paramref name='index'/>.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnRemove(int index, object value)
		{
			if ((BeforeRemove != null))
			{
				BeforeRemove(index, value);
			}
			base.OnRemove(index, value);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Performs additional custom processes before clearing the contents of the <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClear()
		{
			if ((BeforeClear != null))
			{
				BeforeClear();
			}
			base.OnClear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Represents the method that will handle the
		/// <see cref='BeforeInsert'/> and <see cref='BeforeRemove'/> events.
		/// </summary>
		/// <param name='index'>Index of item in collection that will change</param>
		/// <param name='value'>New value of object</param>
		/// ------------------------------------------------------------------------------------
		public delegate void CollectionChange(int index, object value);

		/// ------------------------------------------------------------------------------------
		/// <summary>Represents the method that will handle the
		/// <see cref='BeforeClear'/> event.</summary>
		/// ------------------------------------------------------------------------------------
		public delegate void CollectionClear();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The enumerator for <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> objects.
		/// </summary>
		/// <seealso cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/>
		/// ------------------------------------------------------------------------------------
		public class XmlAssemblyEnumerator : object, IEnumerator
		{

			private IEnumerator baseEnumerator;

			private IEnumerable temp;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection.XmlAssemblyEnumerator'/>.
			/// </summary>
			/// <param name='mappings'>The <see cref='SIL.FieldWorks.Build.Tasks.XmlAssemblyCollection'/> that we enumerate</param>
			/// --------------------------------------------------------------------------------
			public XmlAssemblyEnumerator(XmlAssemblyCollection mappings)
			{
				this.temp = ((IEnumerable)(mappings));
				this.baseEnumerator = temp.GetEnumerator();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the current element in the collection.
			/// </summary>
			/// <value>The current element in the collection.</value>
			/// --------------------------------------------------------------------------------
			public XmlAssembly Current
			{
				get
				{
					return ((XmlAssembly)(baseEnumerator.Current));
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Gets the current element in the collection.
			/// </summary>
			/// <value>The current element in the collection.</value>
			/// --------------------------------------------------------------------------------
			object IEnumerator.Current
			{
				get
				{
					return baseEnumerator.Current;
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns><c>true</c> if the enumerator was successfully advanced to the next element;
			/// <c>false</c> if the enumerator has passed the end of the collection.</returns>
			/// --------------------------------------------------------------------------------
			public bool MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Advances the enumerator to the next element of the collection.
			/// </summary>
			/// <returns><c>true</c> if the enumerator was successfully advanced to the next element;
			/// <c>false</c> if the enumerator has passed the end of the collection.</returns>
			/// --------------------------------------------------------------------------------
			bool IEnumerator.MoveNext()
			{
				return baseEnumerator.MoveNext();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public void Reset()
			{
				baseEnumerator.Reset();
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Sets the enumerator to its initial position, which is before the first element in the collection.
			/// </summary>
			/// --------------------------------------------------------------------------------
			void IEnumerator.Reset()
			{
				baseEnumerator.Reset();
			}
		}
	}
}
