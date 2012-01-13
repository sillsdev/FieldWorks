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
using System.Collections.Generic;
using SIL.FieldWorks.Common.Keyboarding;
using SIL.FieldWorks.Views;

namespace SIL.FieldWorks.Common.Keyboarding.Linux
{
	/// <summary>
	/// Class for handling ibus keyboards on Linux. Currently just a wrapper for KeyboardSwitcher.
	/// </summary>
	/// <remarks>TODO: Move functionality from KeyboardSwitcher to here.</remarks>
	public class IbusKeyboardAdaptor: IKeyboardAdaptor
	{
		private KeyboardSwitcher m_KeyboardSwitcher;

		/// <summary>
		/// Initializes a new instance of the
		/// <see cref="T:SIL.FieldWorks.Common.Keyboarding.Linux.IbusKeyboardAdaptor"/> class.
		/// </summary>
		public IbusKeyboardAdaptor()
		{
		}

		private void InitKeyboards()
		{
			var nKeyboards = m_KeyboardSwitcher.IMEKeyboardsCount;
			for (int i = 0; i < nKeyboards; i++)
			{
				var name = m_KeyboardSwitcher.GetKeyboardName(i);
				var id = name.GetHashCode();
				var keyboard = new KeyboardDescription(id, name, this, KeyboardType.OtherIm);
				KeyboardController.Manager.RegisterKeyboard(id, keyboard);
			}
		}

		#region IKeyboardAdaptor implementation
		/// <summary>
		/// Initialize the installed keyboards
		/// </summary>
		public void Initialize()
		{
			m_KeyboardSwitcher = new KeyboardSwitcher();
			InitKeyboards();
		}

		/// <summary/>
		public void Close()
		{
			if (m_KeyboardSwitcher == null)
				return;

			m_KeyboardSwitcher.Dispose();
			m_KeyboardSwitcher = null;
		}

		/// <summary>
		/// Activates the keyboard
		/// </summary>
		public void ActivateKeyboard(IKeyboardDescription keyboard,
			IKeyboardDescription systemKeyboard)
		{
			m_KeyboardSwitcher.IMEKeyboard = keyboard.Name;

			if (systemKeyboard != null)
				systemKeyboard.Activate();
		}

		/// <summary>
		/// Deactivates the specified keyboard.
		/// </summary>
		public void DeactivateKeyboard(IKeyboardDescription keyboard)
		{
			m_KeyboardSwitcher.IMEKeyboard = null;
		}

		/// <summary>
		/// List of keyboard layouts that either gave an exception or other error trying to
		/// get more information. We don't have enough information for these keyboard layouts
		/// to include them in the list of installed keyboards.
		/// </summary>
		public List<IKeyboardErrorDescription> ErrorKeyboards
		{
			get
			{
				return new List<IKeyboardErrorDescription>();
			}
		}
		#endregion
	}
}
#endif
