// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.Keyboarding.Interfaces;

namespace SIL.FieldWorks.Common.Keyboarding.InternalInterfaces
{
	/// <summary>
	/// Methods and properties for dealing with keyboards
	/// </summary>
	internal interface IKeyboardAdaptor
	{
		/// <summary>
		/// Initialize the installed keyboards
		/// </summary>
		void Initialize();

		/// <summary/>
		void Close();

		/// <summary>
		/// List of keyboard layouts that either gave an exception or other error trying to
		/// get more information. We don't have enough information for these keyboard layouts
		/// to include them in the list of installed keyboards.
		/// </summary>
		/// <returns>List of IKeyboardErrorDescription objects, or an empty list.</returns>
		List<IKeyboardErrorDescription> ErrorKeyboards { get; }

		/// <summary>
		/// Activates the keyboard
		/// </summary>
		/// <param name="keyboard">The keyboard to activate</param>
		void ActivateKeyboard(IKeyboardDescription keyboard);

		/// <summary>
		/// Deactivates the keyboard
		/// </summary>
		/// <param name="keyboard">The keyboard to deactivate</param>
		void DeactivateKeyboard(IKeyboardDescription keyboard);
	}
}
