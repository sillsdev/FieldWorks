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
	/// Interface that needs to be implemented by the view/document. This interface allows the
	/// keyboard controller to get the correct keyboard based on the current position in the
	/// document/the current selection in the view.
	/// </summary>
	public interface IKeyboardCallback
	{
		/// <summary>
		/// Gets the keyboard associated with the current selection.
		/// </summary>
		IKeyboardDescription Keyboard { get; }

		/// <summary>
		/// Gets or sets the active keyboard.
		/// </summary>
		IKeyboardDescription ActiveKeyboard { get; set; }
	}
}
