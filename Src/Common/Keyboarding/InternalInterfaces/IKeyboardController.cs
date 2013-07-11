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
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;
using SIL.FieldWorks.Common.Keyboarding.Types;

namespace SIL.FieldWorks.Common.Keyboarding.InternalInterfaces
{
	/// <summary>
	/// Internal interface for the implementation of the keyboard controller. Implement this
	/// interface if you want to provide a double for unit testing. Otherwise the default
	/// implementation is sufficient.
	/// </summary>
	internal interface IKeyboardController: IDisposable
	{
		/// <summary>
		/// Tries to get the keyboard with the specified <paramref name="layoutName"/>.
		/// </summary>
		/// <returns>
		/// Returns <c>KeyboardDescription.Zero</c> if no keyboard can be found.
		/// </returns>
		IKeyboardDescription GetKeyboard(string layoutName);

		IKeyboardDescription GetKeyboard(string layoutName, string locale);

		/// <summary>
		/// Tries to get the keyboard for the specified <paramref name="writingSystem"/>.
		/// </summary>
		/// <returns>
		/// Returns <c>KeyboardDescription.Zero</c> if no keyboard can be found.
		/// </returns>
		IKeyboardDescription GetKeyboard(IWritingSystem writingSystem);

		/// <summary>
		/// Sets the keyboard
		/// </summary>
		void SetKeyboard(IKeyboardDescription keyboard);

		void SetKeyboard(string layoutName);

		void SetKeyboard(string layoutName, string locale);

		// TODO: Change param type to IWritingSystemDefinition
		void SetKeyboard(IWritingSystem writingSystem);

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
		KeyboardCollection Keyboards { get; }
		/// <summary>
		/// Gets or sets the implementation of the internal event handlers. Can be set
		/// in unit tests to replace parts of the implementation.
		/// </summary>
		IKeyboardEventHandler InternalEventHandler { get; set; }
		/// <summary>
		/// Gets or sets the implementation of the internal methods. Can be set
		/// in unit tests to replace parts of the implementation.
		/// </summary>
		IKeyboardMethods InternalMethods { get; set; }

		/// <summary>
		/// Gets or sets the currently active keyboard
		/// </summary>
		IKeyboardDescription ActiveKeyboard { get; set; }
	}
}
