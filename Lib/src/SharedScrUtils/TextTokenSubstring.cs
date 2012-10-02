using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Class that represents a sub-string of one or more consecutive source tokens
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TextTokenSubstring
	{
		private List<ITextToken> m_tokens;
		private int m_offset;
		private int m_length;
		private string m_inventoryString = null;
		private string m_message = String.Empty;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextTokenSubstring"/> class with a
		/// single source token.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		/// ------------------------------------------------------------------------------------
		public TextTokenSubstring(ITextToken token, int offset, int length) :
			this(token, offset, length, null)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextTokenSubstring"/> class with a
		/// single source token.
		/// </summary>
		/// <param name="token">The token.</param>
		/// <param name="offset">The offset.</param>
		/// <param name="length">The length.</param>
		/// <param name="msg">The error message.</param>
		/// ------------------------------------------------------------------------------------
		public TextTokenSubstring(ITextToken token, int offset, int length, string msg) /*:
			this(new List<ITextToken>(new[] { token }), offset, length, msg)*/
		{
			if (offset < 0)
				throw new ArgumentOutOfRangeException("offset", "Offset must be 0 or greater.");
			if (offset > token.Text.Length)
				throw new ArgumentOutOfRangeException("offset");
			if (length < 0)
				throw new ArgumentOutOfRangeException("length", "Length must be 0 or greater.");
			if (offset + length > token.Text.Length)
				throw new ArgumentOutOfRangeException("length");
			m_tokens = new List<ITextToken>(new ITextToken[] { token });
			m_offset = offset;
			m_length = length;
			m_message = msg;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TextTokenSubstring"/> class based on
		/// another TextTokenSubstring, but having a different Message.
		/// </summary>
		/// <param name="tts">The instance to copy from.</param>
		/// <param name="msg">The message.</param>
		/// ------------------------------------------------------------------------------------
		public TextTokenSubstring(TextTokenSubstring tts, string msg)
		{
			m_tokens = tts.m_tokens;
			m_offset = tts.m_offset;
			m_length = tts.m_length;
			m_inventoryString = tts.m_inventoryString;
			m_message = msg;
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Initializes a new instance of the <see cref="TextTokenSubstring"/> class with a list
		///// of source tokens.
		///// </summary>
		///// <param name="tokens">The tokens.</param>
		///// <param name="offset">The offset into the first token.</param>
		///// <param name="length">The total length (across all tokens).</param>
		///// <param name="msg">The error message.</param>
		///// ------------------------------------------------------------------------------------
		//private TextTokenSubstring(List<ITextToken> tokens, int offset, int length, string msg)
		//{
		//    if (offset >
		//    m_tokens = tokens;
		//    m_offset = offset;
		//    m_length = length;
		//    m_message = msg;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Implements the operator ++.
		/// </summary>
		/// <param name="tts">The TextTokenSubstring.</param>
		/// ------------------------------------------------------------------------------------
		public static TextTokenSubstring operator ++(TextTokenSubstring tts)
		{
			if (tts.m_offset + tts.m_length >= tts.m_tokens[0].Text.Length)
			{
				int availableLength = tts.m_tokens[0].Text.Length - tts.m_offset;
				for (int iTok = 1; iTok < tts.m_tokens.Count; iTok++)
					availableLength += tts.m_tokens[iTok].Text.Length;
				if (tts.m_length >= availableLength)
					throw new IndexOutOfRangeException();
			}
			tts.m_length++;
			return tts;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void AddToken(ITextToken token)
		{
			if (token.IsParagraphStart)
				throw new ArgumentException("A substring must be wholly contained within a single paragraph.");
			m_tokens.Add(token);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the inventory text, which is often the same as the Text.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string InventoryText
		{
			get	{ return (m_inventoryString ?? Text); }
			set { m_inventoryString = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text representing the substring (which can come from more than one source
		/// token).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Text
		{
			get
			{
				if (m_tokens.Count == 1)
					return m_tokens[0].Text.Substring(m_offset, m_length);

				int offsetTemp = m_offset;
				int lengthTemp = m_length;
				StringBuilder bldr = new StringBuilder();
				foreach (ITextToken tok in m_tokens)
				{
					if (tok.TextType == TextType.VerseNumber ||
						tok.TextType == TextType.ChapterNumber ||
						tok.TextType == TextType.Note)
					{
						lengthTemp -= tok.Text.Length;
						continue;
					}
					if (offsetTemp + lengthTemp > tok.Text.Length)
					{
						string substring = tok.Text.Substring(offsetTemp);
						bldr.Append(substring);
						offsetTemp = 0;
						lengthTemp -= substring.Length;
					}
					else
					{
						bldr.Append(tok.Text.Substring(offsetTemp, lengthTemp));
					}
				}
				return bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full text of all text tokens covered (in part or whole) by this substring.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string FullTokenText
		{
			get
			{
				Debug.Assert(FirstToken != null);
				StringBuilder bldr = new StringBuilder();
				foreach (ITextToken tok in m_tokens)
					bldr.Append(tok.Text);
				return bldr.ToString();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets an error message associated with this substring (can be empty).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Message
		{
			get { return m_message; }
			set { m_message = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the character offset into the first token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Offset
		{
			get { return m_offset; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the total length of the substring, from the start offset in the first token
		/// through the ending position in the last token. Note that this can be longer than the
		/// Text length (because intermediate verse and chapter numbers are excluded from the
		/// Text property. Unless FirstToken and LastToken are the same, it is NOT safe to
		/// calculate a substring using the String.Substring method, applied to the FirstToken
		/// using the Offset and this Length.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Length
		{
			get { return m_length; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the paragraph style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParagraphStyle
		{
			get { return m_tokens[0].ParaStyleName; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the first text token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITextToken FirstToken
		{
			get { return m_tokens.Count == 0 ? null : m_tokens[0]; }
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the last text token.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITextToken LastToken
		{
			get { return m_tokens.Count == 0 ? null : m_tokens[m_tokens.Count - 1]; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the missing start reference, if any.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef MissingStartRef
		{
			get { return m_tokens.Count != 1 ? null : m_tokens[0].MissingStartRef; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the missing and reference, if any.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BCVRef MissingEndRef
		{
			get { return m_tokens.Count != 1 ? null : m_tokens[0].MissingEndRef; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Text;
		}
	}
}
