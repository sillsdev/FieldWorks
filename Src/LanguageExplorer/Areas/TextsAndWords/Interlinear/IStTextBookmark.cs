// Copyright (c) 2009-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// indicates a position in an StText
	/// </summary>
	public interface IStTextBookmark
	{
		int IndexOfParagraph { get; }
		int BeginCharOffset { get; }
		int EndCharOffset { get; }
	}
}