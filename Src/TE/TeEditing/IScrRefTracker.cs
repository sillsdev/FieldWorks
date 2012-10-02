// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IScrRefTracker.cs
// Responsibility: DavidO
// ---------------------------------------------------------------------------------------------
using SILUBS.SharedScrUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IScrRefTracker
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the current reference in the usfm browser if it is active.
		/// </summary>
		/// <param name="reference">The target reference in the current project's versification</param>
		/// ------------------------------------------------------------------------------------
		void SetCurrentReference(ScrReference reference);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Syncs to the current Scripture reference location of the given editing helper.
		/// </summary>
		/// <param name="editingHelper">The editing helper.</param>
		/// <param name="fSendInternalOnly">if set to <c>true</c> does not send reference to
		/// third-party listeners.</param>
		/// ------------------------------------------------------------------------------------
		void SyncToScrLocation(TeEditingHelper editingHelper, bool fSendInternalOnly);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to ignore any sync messages.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		bool IgnoreAnySyncMessages { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Provides simple access to the reference at the current insertion point.
		/// </summary>
		/// <remarks>
		/// This property is not guaranteed to return a ScrReference containing the book, chapter,
		/// AND verse.  It will return as much as it can, but not neccessarily all of it. It
		/// will not search back into a previous section if it can't find the verse number in
		/// the current section. This means that if a verse crosses a section break, the verse
		/// number will be inferred from the section start ref.
		/// </remarks>
		/// ------------------------------------------------------------------------------------
		ScrReference CurrentRef { get; }
	}
}
