// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrNotesDataByFlidDecorator.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FilteredDomainDataByFlidDecorator : DomainDataByFlidDecoratorBase
	{
		/// <summary>database cache</summary>
		protected readonly FdoCache m_cache;
		private readonly int m_objectTag;
		private IFilter m_filter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilteredDomainDataByFlidDecorator"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filter">The filter.</param>
		/// <param name="objectTag">The tag of the objects to filter.</param>
		/// ------------------------------------------------------------------------------------
		public FilteredDomainDataByFlidDecorator(FdoCache cache, IFilter filter, int objectTag)
			: base(cache.DomainDataByFlid as ISilDataAccessManaged)
		{
			m_cache = cache;
			m_filter = filter;
			m_objectTag = objectTag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IFilter Filter
		{
			get { return m_filter; }
			set { m_filter = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the filtered index of item.
		/// </summary>
		/// <param name="hvoOwner">The HVO of the owning object</param>
		/// <param name="unfilteredIndex">Index of the unfiltered.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public int GetFilteredIndexOfItem(int hvoOwner, int unfilteredIndex)
		{
			if (base.get_VecSize(hvoOwner, m_objectTag) > unfilteredIndex)
			{
				int objHvo = base.get_VecItem(hvoOwner, m_objectTag, unfilteredIndex);
				return Array.IndexOf(VecProp(hvoOwner, m_objectTag), objHvo);
			}
			// This method gets used on prop changes and on deletes, the object at the unfiltered
			// index may no longer exist
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the note ids contained in the note filter.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		/// ------------------------------------------------------------------------------------
		public override int[] VecProp(int hvo, int tag)
		{
			if (tag != m_objectTag || m_filter == null)
				return base.VecProp(hvo, tag);
			List<int> monkey = new List<int>(base.get_VecSize(hvo, tag));
			foreach (int hvo2 in base.VecProp(hvo, tag))
			{
				if (m_filter.MatchesCriteria(hvo2))
					monkey.Add(hvo2);
			}
			return monkey.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain one note from the note filter.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="index">Indicates the item of interest. &lt;b&gt;Zero based&lt;/b&gt;.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int get_VecItem(int hvo, int tag, int index)
		{
			if (tag != m_objectTag || m_filter == null)
				return base.get_VecItem(hvo, tag, index);
			int[] items = VecProp(hvo, tag);
			Debug.Assert(index >= 0 && index < items.Length);
			return items[index];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the number of notes in the note filter.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int get_VecSize(int hvo, int tag)
		{
			return (tag != m_objectTag || m_filter == null) ?
				base.get_VecSize(hvo, tag) : VecProp(hvo, tag).Length;
		}

		/// <summary>Return the index of hvo in the flid vector of hvoOwn.</summary>
		/// <param name='hvoOwn'>The object ID of the owner.</param>
		/// <param name='flid'>The parameter on hvoOwn that owns hvo.</param>
		/// <param name='hvo'>The target object ID we are looking for.</param>
		/// <returns>
		/// The index, or -1 if not found.
		/// </returns>
		public override int GetObjIndex(int hvoOwn, int flid, int hvo)
		{
			int idx = base.GetObjIndex(hvoOwn, flid, hvo);
			if (flid == m_objectTag && m_filter != null)
				idx = GetFilteredIndexOfItem(hvoOwn, idx);
			return idx;
		}

		/// <summary>
		/// Get the display index according to the filteded collection.
		/// </summary>
		public override int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			if (flid != m_objectTag || m_filter == null)
				return ihvo;
			return GetFilteredIndexOfItem(hvoOwn, ihvo);
		}
	}
}
