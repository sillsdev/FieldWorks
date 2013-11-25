// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AndIFilter.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// AndFilter is an implementation of IFilter that allows an arbitrary number of IFilters
	/// to be "anded" together. (Sorry, Dave.)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class AndIFilter: List<IFilter>, IFilter
	{
		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AndIFilter"/> class that has an empty
		/// list of filters with the default initial capacity.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AndIFilter() : base()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AndIFilter"/> class that is empty and
		/// has the specified initial capacity.
		/// </summary>
		/// <param name="capacity">The capacity.</param>
		/// ------------------------------------------------------------------------------------
		public AndIFilter(int capacity) : base(capacity)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AndIFilter"/> class whose list of
		/// filters contains the two specified filter.
		/// </summary>
		/// <param name="filter1">First filter to add (can be null)</param>
		/// <param name="filter2">Second filter to add (can be null)</param>
		/// ------------------------------------------------------------------------------------
		public AndIFilter(IFilter filter1, IFilter filter2) : this(2)
		{
			if (filter1 != null)
				Add(filter1);
			if (filter2 != null)
				Add(filter2);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="AndIFilter"/> class whose list of
		/// filters contains elements copied from the specified collection and has sufficient
		/// capacity to accommodate the number of elements copied.
		/// </summary>
		/// <param name="collection">The collection whose elements are copied to the new list.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="collection"/> is null.</exception>
		/// ------------------------------------------------------------------------------------
		public AndIFilter(IEnumerable<IFilter> collection) : base(collection)
		{
		}
		#endregion

		#region IFilter Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public string FilterName
		{
			get { return Name; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="MatchesCriteria"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void InitCriteria()
		{
			if (Count == 0)
				throw new InvalidOperationException("Call to InitCriteria requires at least one filter to be added to the collection");
			foreach (IFilter filter in this)
			{
				filter.InitCriteria();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <remarks>currently only handles basic filters (single cell)</remarks>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns><c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool MatchesCriteria(int hvoObj)
		{
			foreach (IFilter filter in this)
			{
				if (!filter.MatchesCriteria(hvoObj))
					return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get { return this.ToString(" & ", item => item.FilterName); }
		}
		#endregion
	}
}
