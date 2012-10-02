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

namespace SIL.FieldWorks.Common.Keyboarding
{
	/// <summary>
	/// Methods and properties for dealing with keyboards
	/// </summary>
	public interface IKeyboardAdaptor
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
		/// <param name="systemKeyboard">A second keyboard (usually a system keyboard) that,
		/// depending on the implementation, might also get activated when this keyboard gets
		/// activated, or <c>null</c>.</param>
		void ActivateKeyboard(IKeyboardDescription keyboard, IKeyboardDescription systemKeyboard);

		/// <summary>
		/// Deactivates the keyboard
		/// </summary>
		/// <param name="keyboard">The keyboard to deactivate</param>
		void DeactivateKeyboard(IKeyboardDescription keyboard);
	}
}
