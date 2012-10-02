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
// File: ILocationTracker.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implemented by a view to keep track of the current book and section. In a trivial case
	/// (as implemented in DraftView) this corresponds directly to the rootbox. In more
	/// complicated cases (as in TePrintLayout) the books and sections are split accross
	/// multiple rootboxes and the root site has to figure out the correct one.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ILocationTracker
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the current book (relative to book filter), or -1 if there is no
		/// current book (e.g. no selection or empty view).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>Index of the current book, or -1 if there is no current book.</returns>
		/// <remarks>The returned value is suitable for making a selection.</remarks>
		/// ------------------------------------------------------------------------------------
		int GetBookIndex(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the current book at the anchor of the selection, or -1 if there is
		/// no current book (e.g. no selection or empty view).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The book hvo.</returns>
		/// ------------------------------------------------------------------------------------
		int GetBookHvo(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the index of the section (relative to RootBox) at the anchor of the selection,
		/// or -1 if we're not in a section (e.g. the IP is in a title).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>Index of the section, or -1 if we're not in a section.</returns>
		/// <remarks>The returned value is suitable for making a selection.</remarks>
		/// ------------------------------------------------------------------------------------
		int GetSectionIndexInView(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the section at the anchor of the selection relative to the book,
		/// or -1 if we're not in a section.
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The section index in book.</returns>
		/// ------------------------------------------------------------------------------------
		int GetSectionIndexInBook(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the HVO of the current section at the anchor of the selection, or -1 if we're
		/// not in a section (e.g. the IP is in a title).
		/// </summary>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <returns>The section hvo.</returns>
		/// ------------------------------------------------------------------------------------
		int GetSectionHvo(SelectionHelper selHelper, SelectionHelper.SelLimitType selLimitType);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set both book and section at the anchor of the selection. Don't make a selection;
		/// typically the caller will proceed to do that.
		/// </summary>
		/// <remarks>This method should change only the book and section levels of the
		/// selection, but not any other level.</remarks>
		/// <param name="selHelper">The selection helper.</param>
		/// <param name="selLimitType">Which end of the selection</param>
		/// <param name="iBook">The index of the book (in the book filter).</param>
		/// <param name="iSection">The index of the section (relative to
		/// <paramref name="iBook"/>), or -1 for a selection that is not in a section (e.g.
		/// title).</param>
		/// ------------------------------------------------------------------------------------
		void SetBookAndSection(SelectionHelper selHelper,
			SelectionHelper.SelLimitType selLimitType, int iBook, int iSection);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of levels for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>Number of levels</returns>
		/// ------------------------------------------------------------------------------------
		int GetLevelCount(int tag);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the level for the given tag.
		/// </summary>
		/// <param name="tag">The tag.</param>
		/// <returns>Index of the level</returns>
		/// ------------------------------------------------------------------------------------
		int GetLevelIndex(int tag);
	}
}
