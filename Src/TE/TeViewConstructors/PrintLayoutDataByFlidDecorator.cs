// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PrintLayoutDataByFlidDecorator.cs
// Responsibility: Lothers
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.FDO;
using System.Diagnostics;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PrintLayoutDataByFlidDecorator : ScrBookFilterDecorator
	{
		private bool m_isIntro;
		private Dictionary<int, int[]> m_footnotesOnPage = new Dictionary<int, int[]>();
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="PrintLayoutDataByFlidDecorator"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="isIntro">if set to <c>true</c> is print layout for an intro division.
		/// </param>
		/// ------------------------------------------------------------------------------------
		public PrintLayoutDataByFlidDecorator(FdoCache cache, int filterInstance, bool isIntro) :
			base(cache, filterInstance)
		{
			m_isIntro = isIntro;
		}

		#region ScrBookFilterDecorator overrides
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the sections for the print layout division.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		/// ------------------------------------------------------------------------------------
		public override int[] VecProp(int hvo, int tag)
		{
			if (tag == ScrBookTags.kflidSections)
				return GetSectionHvos(hvo).ToArray();
			return base.VecProp(hvo, tag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain one section from either the intro or Scripture division of the book.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="index">Indicates the item of interest. &lt;b&gt;Zero based&lt;/b&gt;.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int get_VecItem(int hvo, int tag, int index)
		{
			if (tag == ScrBookTags.kflidSections)
			{
				List<int> sectionHvos = GetSectionHvos(hvo);
				Debug.Assert(index >= 0 && index < sectionHvos.Count);
				return sectionHvos[index];
			}
			return base.get_VecItem(hvo, tag, index);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the number of sections in the intro or Scripture division of this book.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int get_VecSize(int hvo, int tag)
		{
			if (tag == ScrBookTags.kflidSections)
				return GetSectionHvos(hvo).Count;
			return base.get_VecSize(hvo, tag);
		}
		#endregion

		#region Private methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the section hvos for either intro sections or Scripture sections.
		/// </summary>
		/// <param name="bookHvo">The book hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private List<int> GetSectionHvos(int bookHvo)
		{
			List<int> hvoSections = new List<int>();
			IScrBook book = m_cache.ServiceLocator.GetInstance<IScrBookRepository>().GetObject(bookHvo);
			foreach (IScrSection section in book.SectionsOS)
			{
				if (m_isIntro == section.IsIntro)
					hvoSections.Add(section.Hvo);
				else if (m_isIntro)
					break;
			}

			return hvoSections;
		}
		#endregion
	}
}
