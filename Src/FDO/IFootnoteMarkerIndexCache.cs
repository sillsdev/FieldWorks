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
// File: IFootnoteMarkerIndexCache.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.FDO
{
	#region FootnoteIndexCacheInfo class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Information for the index cache values
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FootnoteIndexCacheInfo
	{
		/// <summary>The index into the list of auto-lettered footnotes</summary>
		public int m_index = 0;
		/// <summary>True if the index was changed, false otherwise</summary>
		public bool m_dirty = false;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FootnoteIndexCacheInfo"/> class.
		/// </summary>
		/// <param name="index">The index into the list of auto-lettered footnotes</param>
		/// <param name="dirty">True if the index was changed, false otherwise</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteIndexCacheInfo(int index, bool dirty)
		{
			m_index = index;
			m_dirty = dirty;
		}
	}
	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for Footnote Marker Index Cache.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IFootnoteMarkerIndexCache
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the cache.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void ClearCache();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index for the footnote.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		int GetIndexForFootnote(int footnoteHvo);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the dirty flag for a footnote.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		bool GetDirtyFlagForFootnote(int footnoteHvo);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the dirty flag for a footnote.
		/// </summary>
		/// <param name="footnoteHvo">The footnote hvo.</param>
		/// ------------------------------------------------------------------------------------
		void ClearDirtyFlagForFootnote(int footnoteHvo);
	}
}
