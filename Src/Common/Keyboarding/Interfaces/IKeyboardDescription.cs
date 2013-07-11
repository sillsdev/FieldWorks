// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
using System;

namespace SIL.FieldWorks.Common.Keyboarding.Interfaces
{
	/// <summary>
	/// The different keyboard types we're supporting.
	/// </summary>
	public enum KeyboardType
	{
		/// <summary>
		/// System keyboard like Windows API or xkb
		/// </summary>
		System,
		/// <summary>
		/// Other input method like Keyman, InKey or ibus
		/// </summary>
		OtherIm
	}

	/// <summary>
	/// Represents an installed keyboard layout/language
	/// </summary>
	public interface IKeyboardDescription
	{
		/// <summary>
		/// Gets an identifier of the language/keyboard layout
		/// </summary>
		string Id { get; }

		/// <summary>
		/// Gets the type of this keyboard (system or other)
		/// </summary>
		KeyboardType Type { get; }

		/// <summary>
		/// Gets a human-readable name of the input language.
		/// </summary>
		string Name { get; }

		/// <summary>
		/// The Locale of the keyboard in the format languagecode2-country/regioncode2.
		/// This is mainly significant on Windows, which distinguishes (for example)
		/// a German keyboard used in Germany, Switzerland, and Holland.
		/// </summary>
		string Locale { get; }

		/// <summary>
		/// Activate this keyboard layout.
		/// </summary>
		void Activate();

		/// <summary>
		/// Deactivate this keyboard layout.
		/// </summary>
		void Deactivate();
	}
}
