// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;

namespace SIL.FieldWorks.Common.Keyboarding
{
	/// <summary>
	/// The different types of a selection change
	/// </summary>
	public enum SelChangeType
	{
		/// <summary>Selection did not change</summary>
		NoChange = -1,
		/// <summary>Selection changed but stayed in same paragraph</summary>
		SamePara = 1,
		/// <summary>Selection moved to a different paragraph</summary>
		DiffPara = 2,
		/// <summary>Selection changed, it is not known whether it moved paragraph... maybe no
		/// previous sel.</summary>
		Unknown = 3,
		/// <summary>Selection removed altogether, there is now no current selection.</summary>
		Deleted = 4,
	}

	/// <summary>
	/// Event handler that the keyboard controller or the keyboard implements to get notified
	/// about changes and events in the view/document.
	/// </summary>
	/// <remarks>The keyboard controller creates an object that implements IKeyboardEventHandler.
	/// If different keyboard types on a system require different treatment of events, an
	/// event handler on each keyboard type should be implemented together with a wrapper class
	/// that calls the handler on the keyboard. If all keyboard types can use the same event
	/// handling code, only one class needs to be implemented. The reason it is done this way is
	/// that in the second case we save some time if we don't have to determine the current
	/// keyboard based on the current selection in the text.</remarks>
	public interface IKeyboardEventHandler
	{
		/// <summary>
		/// Called before a property gets updated.
		/// </summary>
		/// <returns>Returns <c>true</c> if the property should be updated without normalization
		/// (i.e. not updated in the database) in order to avoid messing up compositions;
		/// <c>false</c> if property can be processed regularly.</returns>
		/// <remarks>The event handler should do what was done in C++ with the following two
		/// lines and return the value of ptxs->IsCompositionActive:
		/// if (ptxs->IsCompositionActive())
		///    ptxs->NoteCommitDuringComposition();
		/// </remarks>
		bool OnUpdateProp(IKeyboardCallback callback);

		/// <summary>
		/// Called when a mouse event happened.
		/// </summary>
		/// <returns>Returns <c>true</c> if the mouse event was handled, otherwise <c>false</c>.
		/// </returns>
		/// <remarks>Corresponding C++ method is VwTextStore::MouseEvent.</remarks>
		bool OnMouseEvent(IKeyboardCallback callback, int xd, int yd, Rectangle rcSrc,
			Rectangle rcDst, MouseEvent mouseEvent);

		/// <summary>
		/// Called when the layout of the view changes.
		/// </summary>
		void OnLayoutChange(IKeyboardCallback callback);

		/// <summary>
		/// Called when the selection changes.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::OnSelChange.</remarks>
		void OnSelectionChange(IKeyboardCallback callback, SelChangeType how);

		/// <summary>
		/// Called when the text changes.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::OnDocChange.</remarks>
		void OnTextChange(IKeyboardCallback callback);
	}
}
