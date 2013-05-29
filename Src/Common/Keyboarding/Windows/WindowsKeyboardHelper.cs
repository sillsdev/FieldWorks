// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2013, SIL International. All Rights Reserved.
// <copyright from='2013' to='2013' company='SIL International'>
//		Copyright (c) 2013, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
// ---------------------------------------------------------------------------------------------
#if !__MonoCS__
using System;
using System.Drawing;

namespace SIL.FieldWorks.Common.Keyboarding.Windows
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Common keyboard event handling class for Windows (system and keyman) keyboards.
	/// </summary>
	/// <remarks>Most functionality is implemented in C++ in VwTextStore and doesn't make use
	/// of the IKeyboardEventHandler and IKeyboardMethods interfaces.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class WindowsKeyboardHelper: IKeyboardEventHandler, IKeyboardMethods
	{
		private IKeyboardDescription m_ActiveKeyboard = new KeyboardDescriptionNull();

		#region IKeyboardEventHandler Members

		public bool OnUpdateProp(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		public bool OnMouseEvent(IKeyboardCallback callback, int xd, int yd, Rectangle rcSrc,
			Rectangle rcDst, MouseEvent mouseEvent)
		{
			throw new NotImplementedException();
		}

		public void OnLayoutChange(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		public void OnSelectionChange(IKeyboardCallback callback, SelChangeType how)
		{
			throw new NotImplementedException();
		}

		public void OnTextChange(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		#endregion

		#region IKeyboardMethods Members

		public void TerminateAllCompositions(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		public void SetFocus(IKeyboardCallback callback)
		{
			var keyboard = callback.Keyboard;
			keyboard.Activate();
			m_ActiveKeyboard = keyboard;
		}

		public void KillFocus(IKeyboardCallback callback)
		{
			if (m_ActiveKeyboard != null)
			{
				m_ActiveKeyboard.Deactivate();
				m_ActiveKeyboard = new KeyboardDescriptionNull();
			}
		}

		public bool IsCompositionActive(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		public bool IsEndingComposition(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		public void EnableInput(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		public void DisableInput(IKeyboardCallback callback)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the active keyboard.
		/// </summary>
		public IKeyboardDescription ActiveKeyboard
		{
			get
			{
				foreach (var adaptor in KeyboardController.Adaptors)
				{
					var keymanAdaptor = adaptor as KeymanKeyboardAdapter;
					if (keymanAdaptor != null)
						return keymanAdaptor.ActiveKeyboard;
				}
				return m_ActiveKeyboard;
			}
		}
		#endregion
	}
}
#endif
