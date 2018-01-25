// Copyright (c) 2006-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	internal class IhWordPos : IhMissingWordPos
	{
		internal override int WasReal()
		{
			CheckDisposed();

			return 1;
		}
	}
}