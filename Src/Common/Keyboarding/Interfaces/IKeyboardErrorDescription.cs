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
	/// Describes a keyboard layout that either gave an exception or other error trying to
	/// get more information. We don't have enough information for these keyboard layouts
	/// to include them in the list of installed keyboards.
	/// </summary>
	public interface IKeyboardErrorDescription
	{
		/// <summary>
		/// Gets the type of this keyboard (system or other)
		/// </summary>
		KeyboardType Type { get; }

		/// <summary>
		/// Gets the details about the error, e.g. layout name.
		/// </summary>
		object Details { get; }
	}
}
