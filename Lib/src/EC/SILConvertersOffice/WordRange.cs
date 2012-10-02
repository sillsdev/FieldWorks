using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Word = Microsoft.Office.Interop.Word;

namespace SILConvertersOffice
{
	/// <summary>
	/// This class encapsulates a Word.Range object, but in a way that
	/// is more friendly towards normal .Net implementations (e.g. the Start/End references are
	/// zero-based from the Range upon which it is based, the Font returned is a .Net Font, etc).
	/// </summary>
	internal class WordRange : OfficeRange
	{
		protected Word.Range m_theRangeBasedOn = null;

		public Word.Range RangeBasedOn
		{
			get { return m_theRangeBasedOn; }
			set { m_theRangeBasedOn = value; }
		}

		public WordRange(Word.Range basedOnRange)
		{
			m_theRangeBasedOn = basedOnRange.Duplicate;
			m_theRangeBasedOn.TextRetrievalMode.IncludeHiddenText = true;

			// for some unknown reason, the Range sometimes doesn't operate the way one would expect
			//  an array to work. Sometimes (I can't tell why) the first "character" (which is always
			//  available from Text[0]), comes from a Start value of 0 and sometimes from 1.
			//  In the latter case, I think something hidden must be at 0 and the first "character"
			//  is really coming from offset 1.
			// Anyway, the good news is that I think I can detect this case as follows:
			//  The offset to add to the Start and End properties is one less than the End property
			//  value of the first "Character". For the weird case, the Start is 0 and the End 2, but
			//  the length and Text is only 1!?
			// But only if this isn't a zero-length range
			if (basedOnRange.Start != basedOnRange.End)
			{
				Word.Range aRange = m_theRangeBasedOn.Characters.First;
				m_nOffset = aRange.End - aRange.Text.Length;
			}
		}

		public WordRange(WordRange basedOnRange)
		{
			m_theRangeBasedOn = basedOnRange.m_theRangeBasedOn.Duplicate;
			// for some unknown reason, the Range sometimes doesn't operate the way one would expect
			//  an array to work. Sometimes (I can't tell why) the first "character" (which is always
			//  available from Text[0]), comes from a Start value of 0 and sometimes from 1.
			//  In the latter case, I think something hidden must be at 0 and the first "character"
			//  is really coming from offset 1.
			// Anyway, the good news is that I think I can detect this case as follows:
			//  The offset to add to the Start and End properties is one less than the End property
			//  value of the first "Character". For the weird case, the Start is 0 and the End 2, but
			//  the length and Text is only 1!?
			// But only if this isn't a zero-length range
			if (m_theRangeBasedOn.Start != m_theRangeBasedOn.End)
			{
				Word.Range aRange = m_theRangeBasedOn.Characters.First;
				m_nOffset = aRange.End - aRange.Text.Length;
			}
		}

		public override int Start
		{
			get { return m_theRangeBasedOn.Start; }
			set { m_theRangeBasedOn.Start = value; }
		}

		public override int End
		{
			get { return m_theRangeBasedOn.End; }
			set { m_theRangeBasedOn.End = value; }
		}

		public override string Text
		{
			get { return m_theRangeBasedOn.Text; }
			set { m_theRangeBasedOn.Text = value; }
		}

		public override void Select()
		{
			m_theRangeBasedOn.Select();
		}

		public Word.Characters Characters
		{
			get { return m_theRangeBasedOn.Characters; }
		}

		public Word.Paragraphs Paragraphs
		{
			get { return m_theRangeBasedOn.Paragraphs; }
		}

		public override string FontName
		{
			get
			{
				string strFontName = m_theRangeBasedOn.Font.Name;   // base.FontName;

				// might be a complex-only font
				if (strFontName.Length == 0)
				{
					strFontName = m_theRangeBasedOn.Font.NameBi;

					if (strFontName.Length == 0)
					{
						strFontName = m_theRangeBasedOn.Font.NameOther;

						if (strFontName.Length == 0)
							strFontName = m_theRangeBasedOn.Font.NameFarEast;
					}
				}

				return strFontName;
			}
			set
			{
				Word.Font aFontClass = m_theRangeBasedOn.Font;
				if (aFontClass.Name != value)
					aFontClass.Name = value;

				try
				{
					if (aFontClass.NameBi != value)
						aFontClass.NameBi = value;
				}
				catch { }
				/*
				try
				{
					if( aFontClass.NameOther != value )
						aFontClass.NameOther = value;
				}
				catch { }
				try
				{
					if (aFontClass.NameFarEast != value)
						aFontClass.NameFarEast = value;
				}
				catch { }
				*/
				/* if it *still* doesn't have right font, then it's not going to and this slows it
				 * down incredibly
				// if it *still* doesn't have right font, then go thru it character by character
				if (aFontClass.Name.Length == 0)
				{
					foreach (Word.Range aCh in Characters)
					{
						aFontClass = aCh.Font;
						try { aFontClass.NameFarEast = value; }
						catch { }
						try { aFontClass.NameOther = value; }
						catch { }
						try { aFontClass.NameBi = value; }
						catch { }
						aFontClass.Name = value;
					}
				}
				*/
			}
		}

#if DEBUG
		public void TraceOutput()
		{
			System.Diagnostics.Trace.WriteLine(String.Format("Characters count: {0}", Characters.Count));
			foreach(Word.Range aRange in Characters)
				foreach(char ch in aRange.Text)
					System.Diagnostics.Trace.WriteLine(String.Format("weird word: ch: {0},{1:X4} in Text: {2}",
						ch, (int)ch, aRange.Text));
		}
#endif

		public override void DealWithNullText()  // give Word the opportunity to fixup a null text thing
		{
			Reset(Characters.First);
		}

		protected void Reset(Word.Range aRhs)
		{
			m_theRangeBasedOn = aRhs;
		}
	}

	internal class WordParagraphs : List<Word.Range>
	{
		public WordParagraphs(Word.Selection aBasedOnSelection)
		{
			if (aBasedOnSelection.End > aBasedOnSelection.Start)
			{
				Word.Paragraphs aParagraphs = aBasedOnSelection.Paragraphs;
				int nParagraphCount = aParagraphs.Count;
				int i = 1;
				Word.Range aRange = aParagraphs[i].Range.Duplicate;
				aRange.Start = aBasedOnSelection.Start;
				Add(aRange);

				while (i < nParagraphCount)
				{
					aRange = aParagraphs[++i].Range.Duplicate;
					Add(aRange);
				}

				aRange.End = aBasedOnSelection.End;
			}
		}
	}
}
