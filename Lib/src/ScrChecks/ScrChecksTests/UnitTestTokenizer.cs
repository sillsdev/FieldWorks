using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using SILUBS.SharedScrUtils;

namespace SILUBS.ScriptureChecks
{
	/// <summary>
	/// This class allows tokenizing usfm text snippets without using any Paratext specific
	/// code.
	/// Markers supported: id, rem, s, p, q1, q2, io, f, f*, f?, x, x*, x?, nd, nd*
	/// </summary>
	public class UnitTestUSFMTextToken : ITextToken
	{
		// Set by NextToken(...)
		string paraStyleName;
		public string ParaStyleName
		{
			set { paraStyleName = value; }
			get { return paraStyleName; }
		}

		string charStyleName = string.Empty;
		public string CharStyleName
		{
			set { charStyleName = value;  }
			get { return charStyleName; }
		}

		public string BookText;
		public int Offset;

		// Set by DivideText(...)
		public int Length;
		public string Chapter;
		public string Verse;

		// Set by CategorizeToken(...)
		public bool IsPublishableText;
		public bool IsNoteText;
		public bool IsVerseText;

		// Virtual
		public bool IsPublishable
		{
			get
			{
				if (ParaStyleName == "id") return false;
				if (ParaStyleName == "rem") return false;
				return true;
			}
		}

		public bool IsVerse { get { return CharStyleName == "v"; } }
		public bool IsChapter { get { return CharStyleName == "c"; } }

		public bool IsParagraphStart
		{
			get
			{
				if (ParaStyleName == "id") return true;
				if (ParaStyleName == "rem") return true;
				if (ParaStyleName == "p") return true;
				if (paraStyleName == "b") return true;
				if (paraStyleName == "d") return true;
				if (ParaStyleName == "q1") return true;
				if (ParaStyleName == "q2") return true;
				if (ParaStyleName == "io") return true;
				if (ParaStyleName == "s") return true;
				return false;
			}
		}

		public bool IsNoteStart
		{
			get
			{
				if (CharStyleName == "f") return true;
				if (CharStyleName == "x") return true;
				return false;
			}
		}

		public bool IsCharacterStyle
		{
			get
			{
				//if (CharStyleName == "nd") return true;
				//if (ParaStyleName.StartsWith("x") && ParaStyleName != "x") return true;
				//if (ParaStyleName.StartsWith("f") && ParaStyleName != "f") return true;
				//return false;

				//return string.IsNullOrEmpty(CharStyleName);

				return CharStyleName != null;
			}
		}

		public bool IsVerseTextStyle
		{
			get
			{
				if (ParaStyleName == "p") return true;
				if (paraStyleName == "b") return true;
				if (ParaStyleName == "q1") return true;
				if (ParaStyleName == "q2") return true;
				if (ParaStyleName == "nd") return true;
				return false;
			}
		}

		public bool IsEndStyle
		{
			get
			{
				Debug.Assert(ParaStyleName == null || !ParaStyleName.EndsWith("*"),
					"Paragraph styles should never end with an asterisk.");

				return CharStyleName != null && CharStyleName.EndsWith("*");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clones this instance.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITextToken Clone()
		{
			UnitTestUSFMTextToken tok = new UnitTestUSFMTextToken();
			tok.ParaStyleName = this.ParaStyleName;
			tok.CharStyleName = this.CharStyleName;
			tok.BookText = this.BookText;
			tok.Offset = this.Offset;
			tok.Length = this.Length;
			tok.Chapter = this.Chapter;
			tok.Verse = this.Verse;
			tok.IsPublishableText = this.IsPublishableText;
			tok.IsNoteText = this.IsNoteText;
			tok.IsVerseText = this.IsVerseText;

			return tok;
		}

		public override string ToString()
		{
			return "ParaStyle: " + ((!string.IsNullOrEmpty(ParaStyleName)) ? ParaStyleName : "-") +
				", CharStyle: " + ((!string.IsNullOrEmpty(CharStyleName)) ? CharStyleName : "-") +
				": " + Text + "(length=" + Text.Length.ToString() + ")";
		}

		public string Locale
		{
			get { return string.Empty; }
		}

		public TextType TextType
		{
			get {
				if (IsVerse) return TextType.VerseNumber;
				if (IsChapter) return TextType.ChapterNumber;
				if (IsNoteText) return TextType.Note;
				if (IsVerseText) return TextType.Verse;
				return TextType.Other;
			}
		}

		public string Text
		{
			get { return BookText.Substring(Offset, Length); }
		}

		public string ScrRefString
		{
			get { return Chapter + ":" + Verse; }
			set { ; }
		}

		public BCVRef MissingEndRef
		{
			get { return null; }
			set { ; }
		}

		public BCVRef MissingStartRef
		{
			get { return null; }
			set { ; }
		}
	}

	class UnitTestTokenizer
	{
		/// <summary>
		/// Split text for book into TextTokens. Populate Tokens and chapters.
		/// </summary>
		public List<UnitTestUSFMTextToken> Tokenize(string text)
		{
			List<UnitTestUSFMTextToken> tokens = DivideText(text);
			CategorizeTokens(tokens);
			return tokens;
		}

		/// <summary>
		/// Divide text for book into TextTokens.
		/// Set Offset, Length, BookText, AnnotationOffset, Chapter, Verse
		/// Tricky things needing done:
		/// 1) Split \v N abc... into two tokens, first containing just verse number
		/// 2) \f X abc... don't return caller as part of the token
		/// </summary>
		private List<UnitTestUSFMTextToken> DivideText(string text)
		{
			UnitTestUSFMTextToken tok = null;
			List<UnitTestUSFMTextToken> tokens = new List<UnitTestUSFMTextToken>();
			string chapter = "1";
			string verse = "0";
			bool inPublishable = false;

			for (int i = 0; i < text.Length; )
			{
				int ind = text.IndexOf("\\", i);
				if (tok != null)  // if token in progress, set its length
				{
					int last = (ind == -1) ? text.Length : ind;
					tok.Length = last - tok.Offset;
				}

				if (ind == -1) break;  // quit if not more markers

				tok = NextToken(text, ind);  // start new token

				if (tok.IsParagraphStart)
					inPublishable = tok.IsPublishable ||
						tok.IsChapter;

				if (inPublishable)
					tokens.Add(tok);

				if (tok.IsChapter)
				{
					chapter = GetCVNumber(text, tok.Offset);
					// Everything after \c is verse '0'.
					// This allows the title of Psalms (\d) which are present in the Hebrew
					// text to be considered verse text.
					verse = "0";
				}
				else if (tok.IsVerse)
				{
					// Add a token with just the verse number
					verse = GetCVNumber(text, tok.Offset);
					tok.Length = verse.Length;

					// Make another token to contain the verse text
					tok = tok.Clone() as UnitTestUSFMTextToken;
					tok.CharStyleName = "";
					tok.Offset += verse.Length;
					tokens.Add(tok);

					// If number followed by a space, skip this
					if (char.IsWhiteSpace(text[tok.Offset]))
						tok.Offset += 1;
				}

				tok.Chapter = chapter;
				tok.Verse = verse;

				if (tok.IsNoteStart)
				{
					// Skip over the footnote caller
					while (tok.Offset < text.Length)
					{
						char cc = text[tok.Offset];
						if (cc == '\\')
							break;
						if (char.IsWhiteSpace(cc))
						{
							++tok.Offset;
							break;
						}

						++tok.Offset;
					}
				}

				i = tok.Offset;
			}

			return tokens;
		}

		// Scan tokens. Return publishable tokens.
		// Set StyleName, IsPublishableText, IsVerseText, IsNoteText.
		// Character style can override the IsVerseText feature of the pargraph styles.
		private void CategorizeTokens(List<UnitTestUSFMTextToken> tokens)
		{
			List<string> noteEndMarkers = new List<string>();

			bool inNote = false;
			bool inPublishable = false;
			bool paragraphStyleIsVerseText = false;
			bool characterStyleIsVerseText = false;

			foreach (UnitTestUSFMTextToken tok in tokens)
			{
				if (tok.ParaStyleName == "")
				{
					// This is the second token created form splitting the verse number from \v N abc...
					characterStyleIsVerseText = true;
					paragraphStyleIsVerseText = true;
					inPublishable = true;
					inNote = false;
				}

				else if (tok.IsChapter || tok.IsVerse)
				{
					if (tok.IsChapter)
						paragraphStyleIsVerseText = false;
					characterStyleIsVerseText = false;
					inPublishable = true;
					inNote = false;
				}

				else if (tok.IsParagraphStart)
				{
					inPublishable = tok.IsPublishable;
					paragraphStyleIsVerseText = tok.IsVerseTextStyle;
					characterStyleIsVerseText = paragraphStyleIsVerseText;
					inNote = false;
				}

				else if (tok.IsNoteStart)
				{
					inNote = true;

					// We have to build a list of note end markers. Otherwise there
					// is no way to distinguish between character end markers and
					// note ending markers. Sigh.
					if (!noteEndMarkers.Contains("f*"))
						noteEndMarkers.Add("f*");
					if (!noteEndMarkers.Contains("x*"))
						noteEndMarkers.Add("x*");
				}

				else if (tok.IsEndStyle)
				{
					if (noteEndMarkers.Contains(tok.CharStyleName))
						inNote = false;

					characterStyleIsVerseText = paragraphStyleIsVerseText;
					tok.ParaStyleName = "";
				}

				else if (tok.IsCharacterStyle)
				{
					characterStyleIsVerseText = tok.IsVerseTextStyle;
				}

				else { Debug.Assert(false); }

				tok.IsNoteText = inNote;
				tok.IsPublishableText = inPublishable;
				tok.IsVerseText = !inNote && characterStyleIsVerseText && paragraphStyleIsVerseText;
			}
		}

		// Create a new token. Set its Offset, BookText, StyleName.
		private UnitTestUSFMTextToken NextToken(string text, int ind)
		{
			// When this loop is done j points to the first character that is not
			// part of the marker. Note that the space in '\\p ' is considered part
			// of the marker (it terminates the marker). The space in '\\nd* ' is not
			// considered part of the marker.
			int j;
			string marker = "";
			for (j = ind + 1; j < text.Length; ++j)
			{
				if (text[j] <= 32)
				{
					marker = text.Substring(ind + 1, j - (ind + 1));
					j = j + 1;
					if (j < text.Length && text[j] == '\n')
						j = j + 1;
					break;
				}
				if (text[j] == '*')
				{
					j = j + 1;
					marker = text.Substring(ind + 1, j - (ind + 1));
					break;
				}
			}

			UnitTestUSFMTextToken tok = new UnitTestUSFMTextToken();
			tok.Offset = j;
			tok.BookText = text;
			if (IsParagraphStart(marker))
				tok.ParaStyleName = marker;
			else
				tok.CharStyleName = marker;

			return tok;
		}

		/// <summary>
		/// Determines whether the specified marker is a paragraph start marker.
		/// </summary>
		/// <param name="marker">The specified marker.</param>
		/// <returns>
		/// 	<c>true</c> if the specified marker is a paragraph start marker; otherwise, <c>false</c>.
		/// </returns>
		private bool IsParagraphStart(string marker)
		{
			if (marker == "id") return true;
			if (marker == "rem") return true;
			if (marker == "p") return true;
			if (marker == "b") return true;
			if (marker == "d") return true;
			if (marker == "q1") return true;
			if (marker == "q2") return true;
			if (marker == "io") return true;
			if (marker == "s") return true;
			return false;
		}

		/// <summary>
		/// Return the text of a chapter or verse number starting at the
		/// specified offset.
		/// </summary>
		/// <param name="text"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		private string GetCVNumber(string text, int offset)
		{
			while (offset < text.Length && char.IsWhiteSpace(text[offset]))
				++offset;

			int start = offset;

			while (offset < text.Length && !char.IsWhiteSpace(text[offset]) &&
					text[offset] != '\\')
				++offset;

			string num = text.Substring(start, offset - start);
			return num;
		}
	}
}
