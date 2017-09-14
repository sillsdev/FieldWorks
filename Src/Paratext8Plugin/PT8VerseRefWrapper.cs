using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.Scripture;

namespace Paratext8Plugin
{
	internal class PT8VerseRefWrapper : IVerseRef
	{
		private VerseRef ptVerseRef;

		/// <summary/>
		public object CoreVerseRef { get { return ptVerseRef; } }

		/// <summary/>
		public PT8VerseRefWrapper(VerseRef verseRef)
		{
			ptVerseRef = verseRef;
		}

		public int BookNum { get { return ptVerseRef.BookNum; } set { ptVerseRef.BookNum = value; } }

		public int ChapterNum { get { return ptVerseRef.ChapterNum; } }

		public int VerseNum { get { return ptVerseRef.VerseNum; } }

		public string Segment()
		{
			return ptVerseRef.Segment();
		}

		public IEnumerable<IVerseRef> AllVerses(bool v)
		{
			return ScriptureProvider.WrapPtCollection(ptVerseRef.AllVerses(v),
				new Func<VerseRef, IVerseRef>(verseRef => new PT8VerseRefWrapper(verseRef)));
		}
	}
}