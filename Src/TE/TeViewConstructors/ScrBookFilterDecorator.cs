// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrBookFilterDecorator.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using System.Diagnostics;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// ScrBookFilterDecorator decorates the main SDA for handling the book filter.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class ScrBookFilterDecorator : DomainDataByFlidDecoratorBase
	{
		/// <summary>database cache</summary>
		protected readonly FdoCache m_cache;
		private readonly FilteredScrBooks m_filter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScrBookFilterDecorator"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// ------------------------------------------------------------------------------------
		public ScrBookFilterDecorator(FdoCache cache, int filterInstance)
			: base(cache.DomainDataByFlid as ISilDataAccessManaged)
		{
			m_cache = cache;
			m_filter = cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(filterInstance);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the book ids contained in the book filter.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns>The Ids of entire vector property</returns>
		/// ------------------------------------------------------------------------------------
		public override int[] VecProp(int hvo, int tag)
		{
			if (tag != ScriptureTags.kflidScriptureBooks)
				return base.VecProp(hvo, tag);
			return m_filter.BookHvos.ToArray();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Obtain one book from the book filter.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="index">Indicates the item of interest. &lt;b&gt;Zero based&lt;/b&gt;.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int get_VecItem(int hvo, int tag, int index)
		{
			if (tag != ScriptureTags.kflidScriptureBooks)
				return base.get_VecItem(hvo, tag, index);
			Debug.Assert(index >= 0 && index < m_filter.BookHvos.Count);
			return m_filter.GetBook(index).Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the number of books in the book filter.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public override int get_VecSize(int hvo, int tag)
		{
			if (tag != ScriptureTags.kflidScriptureBooks)
				return base.get_VecSize(hvo, tag);
			return m_filter.BookCount;
		}

		/// <summary>
		/// Get the display index according to the filteded collection.
		/// </summary>
		public override int GetDisplayIndex(int hvoOwn, int flid, int ihvo)
		{
			if (flid != ScriptureTags.kflidScriptureBooks)
				return ihvo;
			if (ihvo >= 0 && ihvo < m_cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS.Count)
			{
				IScrBook book = m_cache.LanguageProject.TranslatedScriptureOA.ScriptureBooksOS[ihvo];
				return m_filter.GetBookIndex(book);
			}
			return -1;
		}
	}
}
