// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#if __MonoCS__
using System;
using System.Drawing;

namespace SIL.FieldWorks.Common.Keyboarding.Linux
{
	/// <summary>
	/// Common keyboard event handling class for Linux (xkb and ibus) keyboards.
	/// </summary>
	public class LinuxKeyboardHelper: IKeyboardEventHandler, IKeyboardMethods
	{
		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="SIL.FieldWorks.Common.Keyboarding.Linux.LinuxKeyboardHelper"/> class.
		/// </summary>
		public LinuxKeyboardHelper()
		{
			ActiveKeyboard = KeyboardDescription.Zero;
		}

		#region IKeyboardEventHandler implementation
		/// <summary>
		/// Called before a property gets updated.
		/// </summary>
		public bool OnUpdateProp(IKeyboardCallback callback)
		{
			return false;
		}

		/// <summary>
		/// Called when a mouse event happened.
		/// </summary>
		/// <returns>Returns <c>true</c> if the mouse event was handled, otherwise <c>false</c>.
		/// </returns>
		public bool OnMouseEvent(IKeyboardCallback callback, int xd, int yd, Rectangle rcSrc,
			Rectangle rcDst, MouseEvent mouseEvent)
		{
			if (mouseEvent == MouseEvent.kmeDown)
			{
				SetFocus(callback);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Called when the layout of the view changes.
		/// </summary>
		public void OnLayoutChange(IKeyboardCallback callback)
		{
		}

		/// <summary>
		/// Called when the selection changes.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::OnSelChange.</remarks>
		public void OnSelectionChange(IKeyboardCallback callback, SelChangeType how)
		{
		}

		/// <summary>
		/// Called when the text changes.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::OnDocChange.</remarks>
		public void OnTextChange(IKeyboardCallback callback)
		{
		}
		#endregion

		#region IKeyboardMethods implementation
		/// <summary>
		/// End all active compositions.
		/// </summary>
		public void TerminateAllCompositions(IKeyboardCallback callback)
		{
		}

		/// <summary>
		/// Activate the input method
		/// </summary>
		public void SetFocus(IKeyboardCallback callback)
		{
			var keyboard = callback.Keyboard;
			KeyboardController.ActivateKeyboard(keyboard);
		}

		/// <summary>
		/// Deactivate the input method
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::OnLoseFocus.</remarks>
		public void KillFocus(IKeyboardCallback callback)
		{
			ActiveKeyboard.Deactivate();
			ActiveKeyboard = KeyboardDescription.Zero;
		}

		/// <summary>
		/// Enables the input method. This gets called as part of VwRootBox::HandleActivate when
		/// enabling a selection.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::SetFocus.</remarks>
		public void EnableInput(IKeyboardCallback callback)
		{
		}

		/// <summary>
		/// Disables the input method. This gets called as part of VwRootBox::HandleActivate when
		/// disabling a selection.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::OnLoseFocus.</remarks>
		public void DisableInput(IKeyboardCallback callback)
		{
		}

		/// <summary>
		/// Gets a value indicating whether a composition window is active.
		/// </summary>
		public bool IsCompositionActive(IKeyboardCallback callback)
		{
			return false;
		}

		/// <summary>
		/// Gets a value indicating if the input method is in the process of closing a composition
		/// window.
		/// </summary>
		/// <remarks>Corresponding C++ method is VwTextStore::IsDoingRecommit.</remarks>
		public bool IsEndingComposition(IKeyboardCallback callback)
		{
			return false;
		}

		/// <summary>
		/// Gets or sets the active keyboard.
		/// </summary>
		public IKeyboardDescription ActiveKeyboard { get; set; }
		#endregion
	}
}
#endif
