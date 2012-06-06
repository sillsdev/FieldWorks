// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.Common.Keyboarding
{
	/// <summary>
	/// This interface provides additional methods for keyboards
	/// </summary>
	public interface IKeyboardMethods
	{
		/// <summary>
		/// End all active compositions.
		/// </summary>
		void TerminateAllCompositions(IKeyboardCallback callback);

		/// <summary>
		/// Activate the input method
		/// </summary>
		void SetFocus(IKeyboardCallback callback);

		/// <summary>
		/// Deactivate the input method
		/// </summary>
		void KillFocus(IKeyboardCallback callback);

		/// <summary>
		/// Gets a value indicating whether a composition window is active.
		/// </summary>
		bool IsCompositionActive(IKeyboardCallback callback);

		/// <summary>
		/// Gets a value indicating if the input method is in the process of closing a composition
		/// window.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::IsDoingRecommit.</remarks>
		bool IsEndingComposition(IKeyboardCallback callback);

		/// <summary>
		/// Enables the input method. This gets called as part of VwRootBox::HandleActivate when
		/// enabling a selection.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::SetFocus.</remarks>
		void EnableInput(IKeyboardCallback callback);

		/// <summary>
		/// Disables the input method. This gets called as part of VwRootBox::HandleActivate when
		/// disabling a selection.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::OnLoseFocus.</remarks>
		void DisableInput(IKeyboardCallback callback);

		/// <summary>
		/// Gets or sets the active keyboard.
		/// </summary>
		IKeyboardDescription ActiveKeyboard { get; set; }
	}
}
