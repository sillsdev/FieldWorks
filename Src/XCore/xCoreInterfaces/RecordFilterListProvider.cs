// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections;
using System.Xml;
using SIL.Utils;

namespace XCore
{
	/// <summary>
	/// concrete implementations of this provide a list of RecordFilters to offer to the user.
	/// </summary>
	public abstract class RecordFilterListProvider
	{
		protected PropertyTable m_propertyTable;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// a factory method for RecordFilterListProvider
		/// </summary>
		/// <param name="propertyTable">The property table</param>
		/// <param name="configuration">The configuration.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		static public RecordFilterListProvider Create(PropertyTable propertyTable, XmlNode configuration)
		{
			RecordFilterListProvider p = (RecordFilterListProvider)DynamicLoader.CreateObject(configuration);
			if (p != null)
				p.Init(propertyTable);
			return p;
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter list. Called by Create.
		/// </summary>
		/// <param name="propertyTable">The PropertyTable</param>
		/// ------------------------------------------------------------------------------------
		public virtual void Init(PropertyTable propertyTable)
		{
			m_propertyTable = propertyTable;
		}

		/// <summary>
		/// reload the data items
		/// </summary>
		public virtual void ReLoad()
		{
		}


		/// <summary>
		/// the list of filters.
		/// </summary>
		public abstract ArrayList Filters
		{
			get;
		}

		//this has a signature of object just because is confined to XCore, so does not know about FDO RecordFilters
		public abstract object GetFilter(string id);

		/// <summary>
		/// Updates the offered filters to match the owning clerk's current filter.
		/// </summary>
		/// <param name="currentFilter">the clerk's current filter; may be null</param>
		public virtual void AdjustFilterSelection(object currentFilter)
		{
		}
	}
}
