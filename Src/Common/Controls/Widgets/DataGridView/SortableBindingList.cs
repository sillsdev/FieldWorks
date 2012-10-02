// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SortableBindingList.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace SIL.FieldWorks.Common.Widgets
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A sortable binding list
	/// </summary>
	/// <typeparam name="T"></typeparam>
	/// <remarks><para>See article "Behind the Scenes: Improvements to Windows Forms Data
	/// Binding in the .NET Framework 2.0" on MSDN
	/// (http://msdn2.microsoft.com/en-us/library/aa480736.aspx).</para>
	/// <para>Enhances the BindingList class by adding support for sorting. </para>
	/// <para>How to use this class:</para>
	/// <code>
	/// SortableBindingList&lt;MyType&gt; list = ...
	/// dataGridView.DataSource = list;
	/// // MyComment property can be sorted by the TsStringComparer
	/// list.AddComparer("MyComment", new TsStringComparer("en"));
	/// </code>
	/// </remarks>
	/// ----------------------------------------------------------------------------------------
	public class SortableBindingList<T> : BindingList<T>
	{
		#region Comparer class
		private class Comparer : IComparer<T>
		{
			private PropertyInfo m_PropInfo;
			private IComparer m_InnerComparer;
			private ListSortDirection m_SortDirection;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the
			/// <see cref="SortableBindingList&lt;T&gt;.Comparer"/> class.
			/// </summary>
			/// <param name="propertyName">Name of the property.</param>
			/// <param name="innerComparer">The inner comparer.</param>
			/// <param name="sortDirection">The sort direction.</param>
			/// --------------------------------------------------------------------------------
			public Comparer(string propertyName, IComparer innerComparer,
				ListSortDirection sortDirection)
			{
				m_PropInfo = typeof(T).GetProperty(propertyName);
				m_InnerComparer = innerComparer;
				m_SortDirection = sortDirection;

				if (m_InnerComparer == null)
				{
					Debug.Assert(m_PropInfo.PropertyType.GetInterface("IComparable") != null,
						"If no innerComparer is specified the property type has to implement IComparable");
				}
			}

			#region IComparer<T> Members

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Compares two objects and returns a value indicating whether one is less than,
			/// equal to, or greater than the other.
			/// </summary>
			/// <param name="x">The first object to compare.</param>
			/// <param name="y">The second object to compare.</param>
			/// <returns>
			/// Value				Condition
			/// Less than zero		<paramref name="x"/> is less than <paramref name="y"/>.
			/// Zero				<paramref name="x"/> equals <paramref name="y"/>.
			/// Greater than zero	<paramref name="x"/> is greater than <paramref name="y"/>.
			/// </returns>
			/// --------------------------------------------------------------------------------
			public int Compare(T x, T y)
			{
				object objX = m_PropInfo.GetValue(x, null);
				object objY = m_PropInfo.GetValue(y, null);
				if (m_InnerComparer == null)
				{
					return m_SortDirection == ListSortDirection.Ascending ?
						((IComparable)objX).CompareTo(objY) : ((IComparable)objY).CompareTo(objX);
				}

				return m_SortDirection == ListSortDirection.Ascending ?
					m_InnerComparer.Compare(objX, objY) :
					m_InnerComparer.Compare(objY, objX);
			}

			#endregion
		}
		#endregion

		#region Data members
		private bool m_fSorted;
		private ListSortDirection m_sortDirection;
		private PropertyDescriptor m_sortProperty;
		private string m_sortPropertyName;
		private Dictionary<string, IComparer> m_PropertyIcuLocale =
			new Dictionary<string, IComparer>();
		#endregion

		#region Constructors

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SortableBindingList&lt;T&gt;"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SortableBindingList(): base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SortableBindingList&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="list">An <see cref="T:System.Collections.Generic.IList`1"/> of items
		/// to be contained in the <see cref="T:System.ComponentModel.BindingList`1"/>.</param>
		/// ------------------------------------------------------------------------------------
		public SortableBindingList(IList<T> list)
			: base(list)
		{
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a comparer for property <paramref name="propName"/>.
		/// </summary>
		/// <param name="propName">Name of the property.</param>
		/// <param name="comparer">The comparer object</param>
		/// <remarks>Adding a comparer for a property allows that property to be sorted by the
		/// specified comparer (e.g. when the user clicks on the corresponding column header in
		/// a datagrid view.</remarks>
		/// ------------------------------------------------------------------------------------
		public void AddComparer(string propName, IComparer comparer)
		{
			m_PropertyIcuLocale[propName] = comparer;
		}

		#region Overridden methods and properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the list supports sorting.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool SupportsSortingCore
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether the list is sorted.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		protected override bool IsSortedCore
		{
			get { return m_fSorted; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property descriptor that is used for sorting the list.
		/// </summary>
		/// <returns>The <see cref="T:System.ComponentModel.PropertyDescriptor"/> used for
		/// sorting the list.</returns>
		/// ------------------------------------------------------------------------------------
		protected override PropertyDescriptor SortPropertyCore
		{
			get { return m_sortProperty; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the direction the list is sorted.
		/// </summary>
		/// <returns>One of the <see cref="T:System.ComponentModel.ListSortDirection"/> values.
		/// The default is <see cref="F:System.ComponentModel.ListSortDirection.Ascending"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override ListSortDirection SortDirectionCore
		{
			get { return m_sortDirection; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for the index of the item that has the specified property descriptor with
		/// the specified value.
		/// </summary>
		/// <param name="prop">The <see cref="T:System.ComponentModel.PropertyDescriptor"/> to
		/// search for.</param>
		/// <param name="key">The property to match.</param>
		/// <returns>
		/// The zero-based index of the item that matches the property descriptor and contains
		/// the specified value.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected override int FindCore(PropertyDescriptor prop, object key)
		{
			return FindCore(prop, key, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Searches for the index of the item that has the specified property descriptor with
		/// the specified value.
		/// </summary>
		/// <param name="prop">The <see cref="T:System.ComponentModel.PropertyDescriptor"/> to
		/// search for.</param>
		/// <param name="key">The property to match.</param>
		/// <param name="startIndex">The index to start searching for the item.</param>
		/// <returns>
		/// The zero-based index of the item that matches the property descriptor and contains
		/// the specified value.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		protected int FindCore(PropertyDescriptor prop, object key, int startIndex)
		{
			// Get the property info for the specified property.
			PropertyInfo propInfo = typeof(T).GetProperty(prop.Name);

			if (key != null)
			{
				// Loop through the items to see if the key
				// value matches the property value.
				T item;
				for (int i = startIndex; i < Count; ++i)
				{
					item = Items[i];
					if (propInfo.GetValue(item, null).Equals(key))
						return i;
				}
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the items.
		/// </summary>
		/// <param name="prop">A <see cref="T:System.ComponentModel.PropertyDescriptor"/> that
		/// specifies the property to sort on.</param>
		/// <param name="direction">One of the <see cref="T:System.ComponentModel.ListSortDirection"/>
		/// values.</param>
		/// ------------------------------------------------------------------------------------
		protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
		{
			Sort(prop.Name, direction);

			if (m_fSorted)
			{
				m_sortProperty = prop;
				m_sortPropertyName = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts the specified property name.
		/// </summary>
		/// <param name="propertyName">Name of the property.</param>
		/// <param name="direction">The direction.</param>
		/// ------------------------------------------------------------------------------------
		public void Sort(string propertyName, ListSortDirection direction)
		{
			// Check to see if the property type we are sorting by implements
			// the IComparable interface.
			PropertyInfo propInfo = typeof(T).GetProperty(propertyName);
			Type interfaceType = propInfo.PropertyType.GetInterface("IComparable");

			IComparer comparer = null;
			if (m_PropertyIcuLocale.TryGetValue(propertyName, out comparer) || interfaceType != null)
			{
				RaiseListChangedEvents = false;

				try
				{
					// If so, set the SortPropertyValue and SortDirectionValue.
					m_sortProperty = null;
					m_sortPropertyName = propertyName;
					m_sortDirection = direction;

					Comparer outerComparer =
						new SortableBindingList<T>.Comparer(propertyName, comparer, direction);
					T[] array = new T[Count];
					CopyTo(array, 0);
					Array.Sort<T>(array, outerComparer);
					ClearItems();

					foreach (T item in array)
						Add(item);

					m_fSorted = true;
				}
				finally
				{
					RaiseListChangedEvents = true;
				}

				// Raise the ListChanged event so bound controls refresh their
				// values.
				OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
			}
			else
			{
				// If the property type does not implement IComparable, let the user
				// know.
				throw new NotSupportedException(
					string.Format("Cannot sort by {0}. This {1} does not implement IComparable",
					propertyName, propInfo.GetType()));
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the index of the object with the specified property.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <param name="key">The key.</param>
		/// <returns>The zero-based index of the item that matches the property descriptor and
		/// contains the specified value.</returns>
		/// ------------------------------------------------------------------------------------
		public int Find(string property, object key)
		{
			// Check the properties for a property with the specified name.
			PropertyDescriptorCollection properties =
				TypeDescriptor.GetProperties(typeof(T));
			PropertyDescriptor prop = properties.Find(property, true);

			// If there is not a match, return -1 otherwise pass search to
			// FindCore method.
			if (prop == null)
				return -1;
			else
				return FindCore(prop, key);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a range of items and raises one ListChanged event at the end.
		/// </summary>
		/// <param name="items">The items.</param>
		/// ------------------------------------------------------------------------------------
		public void AddRange(IList<T> items)
		{
			bool fOldRaiseListChanged = RaiseListChangedEvents;
			int nOldCount = Count;
			try
			{
				RaiseListChangedEvents = false;
				foreach (T item in items)
					InsertItem(Count, item);

				if (IsSortedCore)
				{
					if (SortPropertyCore != null)
						ApplySortCore(SortPropertyCore, SortDirectionCore);
					else
						Sort(m_sortPropertyName, SortDirectionCore);
				}
			}
			finally
			{
				RaiseListChangedEvents = fOldRaiseListChanged;
				if (RaiseListChangedEvents)
					OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, nOldCount));
			}
		}
	}
}
