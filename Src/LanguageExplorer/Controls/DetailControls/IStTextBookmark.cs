// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// indicates a position in an StText
	/// </summary>
	internal interface IStTextBookmark
	{
		int IndexOfParagraph { get; }
		int BeginCharOffset { get; }
		int EndCharOffset { get; }
	}
}