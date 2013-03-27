// --------------------------------------------------------------------------------------------
// <copyright from='2012' to='2012' company='SIL International'>
// 	Copyright (c) 2012, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.Keyboarding.Interfaces
{
	/// <summary>
	/// Internal interface for the implementation of the keyboard controller. Implement this
	/// interface if you want to provide a double for unit testing. Otherwise the default
	/// implementation is sufficient.
	/// </summary>
	interface IKeyboardController: IDisposable
	{
		/// <summary>
		/// Tries to get the keyboard specified by <paramref name="otherImKeyboard"/> or (if
		/// not found) <paramref name="lcid"/>. Returns <c>KeyboardDescription.Zero</c> if
		/// no keyboard can be found.
		/// </summary>
		IKeyboardDescription GetKeyboard(int? lcid, string otherImKeyboard);
		/// <summary/>
		IKeyboardDescription GetKeyboard(int lcid);
		/// <summary/>
		IKeyboardDescription GetKeyboard(string otherImKeyboard);

		/// <summary>
		/// Sets the keyboard.
		/// </summary>
		/// <param name='lcid'>Keyboard identifier of system keyboard</param>
		/// <param name='otherImKeyboard'>Identifier for other input method keyboard (Keyman/ibus)
		/// </param>
		/// <param name='nActiveLangId'>The active keyboard lcid.</param>
		/// <param name='activeOtherImKeyboard'>Active other input method keyboard.</param>
		/// <param name='fSelectLangPending'></param>
		void SetKeyboard(int lcid, string otherImKeyboard, ref int nActiveLangId,
			ref string activeOtherImKeyboard, ref bool fSelectLangPending);

		/// <summary>
		/// Gets the installed keyboard layouts/languages.
		/// </summary>
		List<IKeyboardDescription> InstalledKeyboards { get; }

		/// <summary>
		/// List of keyboard layouts that either gave an exception or other error trying to
		/// get more information. We don't have enough information for these keyboard layouts
		/// to include them in the list of installed keyboards.
		/// </summary>
		List<IKeyboardErrorDescription> ErrorKeyboards { get; }

		/// <summary>
		/// Gets the available keyboards
		/// </summary>
		Dictionary<int, IKeyboardDescription> Keyboards { get; }
		/// <summary>
		/// Gets or sets the implementation of the internal event handlers.
		/// </summary>
		IKeyboardEventHandler InternalEventHandler { get; set; }
		/// <summary>
		/// Gets or sets the implementation of the internal methods.
		/// </summary>
		IKeyboardMethods InternalMethods { get; set; }
	}
}
