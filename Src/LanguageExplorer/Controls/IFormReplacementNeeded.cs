// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// A mostly do nothing interface that lets the AreaServices class replace the main window
	/// without requiring a dependency on the actual implementations.
	/// </summary>
	internal interface IFormReplacementNeeded
	{
	}
}