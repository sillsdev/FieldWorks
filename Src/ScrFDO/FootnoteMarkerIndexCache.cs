// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FootnoteMarkerIndexCache.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Collections;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FDO
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Caches the index for the marker for the list of autolettered footnotes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FootnoteMarkerIndexCache : IFootnoteMarkerIndexCache
	{
		#region Member variables
		private FdoCache m_cache;
		private IScripture m_scr;
		private Dictionary<int, FootnoteIndexCacheInfo> m_htFootnoteIndex =
			new Dictionary<int, FootnoteIndexCacheInfo>();
		private Dictionary<int, int> m_htBookFootnoteCount = new Dictionary<int, int>();
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FootnoteMarkerIndexCache"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteMarkerIndexCache(FdoCache cache)
		{
			m_cache = cache;
			m_scr = m_cache.LangProject.TranslatedScriptureOA;
		}
		#endregion

		#region IFootnoteMarkerIndexCache implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearCache()
		{
			m_htBookFootnoteCount.Clear();
			m_htFootnoteIndex.Clear();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index for footnote.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// <returns>The index of the marker in the auto-lettered footnote list or -1 if it could
		/// not be found </returns>
		/// ------------------------------------------------------------------------------------
		public int GetIndexForFootnote(int footnoteHvo)
		{
			LoadFootnotes(footnoteHvo);
			return GetFootnoteIndex(footnoteHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dirty flag for a footnote.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public bool GetDirtyFlagForFootnote(int footnoteHvo)
		{
			LoadFootnotes(footnoteHvo);
			FootnoteIndexCacheInfo info;
			if (m_htFootnoteIndex.TryGetValue(footnoteHvo, out info))
				return info.m_dirty;
			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the dirty flag for a footnote.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// ------------------------------------------------------------------------------------
		public void ClearDirtyFlagForFootnote(int footnoteHvo)
		{
			LoadFootnotes(footnoteHvo);
			FootnoteIndexCacheInfo info;
			if (m_htFootnoteIndex.TryGetValue(footnoteHvo, out info))
				info.m_dirty = false;
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the footnote.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// <returns>The index of the marker in the auto-lettered footnote list or -1 if it could
		/// not be found </returns>
		/// ------------------------------------------------------------------------------------
		private int GetFootnoteIndex(int footnoteHvo)
		{
			FootnoteIndexCacheInfo info;
			if (m_htFootnoteIndex.TryGetValue(footnoteHvo, out info))
				return info.m_index;
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the book footnote count.
		/// </summary>
		/// <param name="bookHvo">The book hvo.</param>
		/// <returns>The number of footnotes in the specified book, 0 if we don't know</returns>
		/// ------------------------------------------------------------------------------------
		private int GetBookFootnoteCount(int bookHvo)
		{
			int count;
			if (m_htBookFootnoteCount.TryGetValue(bookHvo, out count))
				return count;
			return 0;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads the footnotes.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// ------------------------------------------------------------------------------------
		private void LoadFootnotes(int footnoteHvo)
		{
			Debug.Assert(m_cache.GetClassOfObject(footnoteHvo) == StFootnote.kClassId);
			StFootnote foot = new StFootnote(m_cache, footnoteHvo);
			IScrBook book = new ScrBook(m_cache, foot.OwnerHVO);
			int footnoteCount = GetBookFootnoteCount(book.Hvo);
			FdoOwningSequence<IStFootnote> footnotes = book.FootnotesOS;

			// If the information we want is already in the cache, then do nothing
			if (footnotes.Count == footnoteCount && m_htFootnoteIndex.ContainsKey(footnoteHvo))
				return;

			ScrFootnote footnote = null;
			int index = 0;
			for (int i = 0; i < footnotes.Count; i++)
			{
				footnote = new ScrFootnote(m_cache, footnotes.HvoArray[i]);
				if (footnote.FootnoteType == FootnoteMarkerTypes.AutoFootnoteMarker)
				{
					int oldIndex = GetFootnoteIndex(footnote.Hvo);
					if (oldIndex != index)
						m_htFootnoteIndex[footnote.Hvo] = new FootnoteIndexCacheInfo(index, true);
					index++;
				}
			}
			m_htBookFootnoteCount[book.Hvo] = footnotes.Count;
		}
		#endregion
	}
}
