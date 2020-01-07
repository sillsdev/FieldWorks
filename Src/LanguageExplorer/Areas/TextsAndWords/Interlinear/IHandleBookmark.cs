// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public interface IHandleBookmark
	{
		/// <summary>
		/// makes a selection in a view given the bookmark location.
		/// </summary>
		void SelectBookmark(IStTextBookmark bookmark);
	}
}