// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using SIL.LCModel.Core.Scripture;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class that represents a sub-string of one or more consecutive source tokens
	/// </summary>
	public class TextTokenSubstring
	{
		private List<ITextToken> m_tokens;
		private string m_inventoryString;

		/// <summary />
		public TextTokenSubstring(ITextToken token, int offset, int length)
			: this(token, offset, length, null)
		{
		}

		/// <summary />
		public TextTokenSubstring(ITextToken token, int offset, int length, string msg)
		{
			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset), "Offset must be 0 or greater.");
			}
			if (offset > token.Text.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}
			if (length < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(length), "Length must be 0 or greater.");
			}
			if (offset + length > token.Text.Length)
			{
				throw new ArgumentOutOfRangeException(nameof(length));
			}
			m_tokens = new List<ITextToken>(new[] { token });
			Offset = offset;
			Length = length;
			Message = msg;
		}

		/// <summary />
		public TextTokenSubstring(TextTokenSubstring tts, string msg)
		{
			m_tokens = tts.m_tokens;
			Offset = tts.Offset;
			Length = tts.Length;
			m_inventoryString = tts.m_inventoryString;
			Message = msg;
		}

		/// <summary>
		/// Implements the operator ++.
		/// </summary>
		/// <param name="tts">The TextTokenSubstring.</param>
		public static TextTokenSubstring operator ++(TextTokenSubstring tts)
		{
			if (tts.Offset + tts.Length >= tts.m_tokens[0].Text.Length)
			{
				var availableLength = tts.m_tokens[0].Text.Length - tts.Offset;
				for (var iTok = 1; iTok < tts.m_tokens.Count; iTok++)
				{
					availableLength += tts.m_tokens[iTok].Text.Length;
				}
				if (tts.Length >= availableLength)
				{
					Debug.Fail("TextTokenSubstring ++ operator tried to increment past end of available length.");
					return tts;
				}
			}
			tts.Length++;
			return tts;
		}

		/// <summary>
		/// Adds a token.
		/// </summary>
		public void AddToken(ITextToken token)
		{
			if (token.IsParagraphStart)
			{
				throw new ArgumentException("A substring must be wholly contained within a single paragraph.");
			}
			m_tokens.Add(token);
		}

		/// <summary>
		/// Gets or sets the inventory text, which is often the same as the Text.
		/// </summary>
		public string InventoryText
		{
			get { return (m_inventoryString ?? Text); }
			set { m_inventoryString = value; }
		}

		/// <summary>
		/// Gets the text representing the substring (which can come from more than one source
		/// token).
		/// </summary>
		public string Text
		{
			get
			{
				if (m_tokens.Count == 1)
				{
					return m_tokens[0].Text.Substring(Offset, Length);
				}
				var offsetTemp = Offset;
				var lengthTemp = Length;
				var bldr = new StringBuilder();
				foreach (var tok in m_tokens)
				{
					if (tok.TextType == TextType.VerseNumber || tok.TextType == TextType.ChapterNumber || tok.TextType == TextType.Note)
					{
						lengthTemp -= tok.Text.Length;
						continue;
					}
					if (offsetTemp + lengthTemp > tok.Text.Length)
					{
						var substring = tok.Text.Substring(offsetTemp);
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

		/// <summary>
		/// Gets the full text of all text tokens covered (in part or whole) by this substring.
		/// </summary>
		public string FullTokenText
		{
			get
			{
				Debug.Assert(FirstToken != null);
				var bldr = new StringBuilder();
				foreach (var tok in m_tokens)
				{
					bldr.Append(tok.Text);
				}
				return bldr.ToString();
			}
		}

		/// <summary>
		/// Gets or sets an error message associated with this substring (can be empty).
		/// </summary>
		public string Message { get; set; } = string.Empty;

		/// <summary>
		/// Gets the character offset into the first token.
		/// </summary>
		public int Offset { get; }

		/// <summary>
		/// Gets the total length of the substring, from the start offset in the first token
		/// through the ending position in the last token. Note that this can be longer than the
		/// Text length (because intermediate verse and chapter numbers are excluded from the
		/// Text property. Unless FirstToken and LastToken are the same, it is NOT safe to
		/// calculate a substring using the String.Substring method, applied to the FirstToken
		/// using the Offset and this Length.
		/// </summary>
		public int Length { get; private set; }

		/// <summary>
		/// Gets the paragraph style name.
		/// </summary>
		public string ParagraphStyle => m_tokens[0].ParaStyleName;

		/// <summary>
		/// Gets the first text token.
		/// </summary>
		public ITextToken FirstToken => m_tokens.Count == 0 ? null : m_tokens[0];

		/// <summary>
		/// Gets the last text token.
		/// </summary>
		public ITextToken LastToken => m_tokens.Count == 0 ? null : m_tokens[m_tokens.Count - 1];

		/// <summary>
		/// Gets the missing start reference, if any.
		/// </summary>
		public BCVRef MissingStartRef => m_tokens.Count != 1 ? null : m_tokens[0].MissingStartRef;

		/// <summary>
		/// Gets the missing and reference, if any.
		/// </summary>
		public BCVRef MissingEndRef => m_tokens.Count != 1 ? null : m_tokens[0].MissingEndRef;

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </returns>
		public override string ToString()
		{
			return Text;
		}
	}
}