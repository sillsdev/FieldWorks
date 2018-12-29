// Copyright (c) 2006-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>Values indicating what changed as a result of the user actions taken in the Styles dialog</summary>
	[Flags]
	internal enum StyleChangeType
	{
		/// <summary>Nothing changed</summary>
		None = 0,
		/// <summary>Definition of at least one style changed</summary>
		DefChanged = 1,
		/// <summary>At least one style got renamed or deleted</summary>
		RenOrDel = 2,
		/// <summary>At least one style got added</summary>
		Added = 4,
	}
}