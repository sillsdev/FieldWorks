// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Scripture;

namespace Paratext8Plugin
{
	internal class Pt8VerseWrapper : IScrVerse
	{
		private ScrVers pt8Versification;
		public Pt8VerseWrapper(ScrVers versification)
		{
			pt8Versification = versification;
		}

		public int LastChapter(int bookNum)
		{
			return pt8Versification.GetLastChapter(bookNum);
		}

		public int LastVerse(int bookNum, int chapter)
		{
			return pt8Versification.GetLastVerse(bookNum, chapter);
		}
	}
}