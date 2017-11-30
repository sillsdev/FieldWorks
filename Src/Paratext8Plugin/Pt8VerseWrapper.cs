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