// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Class that enables the context grid to get the text before and after the character
	/// being displayed.
	/// </summary>
	public class ContextInfo
	{
		private ContextPosition m_position = ContextPosition.Undefined;

		/// <summary />
		internal ContextInfo(PuncPattern pattern, TextTokenSubstring tts)
			: this(pattern, tts.Offset, tts.FullTokenText, tts.FirstToken.ScrRefString)
		{
		}

		/// <summary />
		internal ContextInfo(string chr, TextTokenSubstring tts)
			: this(chr, tts.Offset, tts.FullTokenText, tts.FirstToken.ScrRefString)
		{
		}

		/// <summary />
		internal ContextInfo(PuncPattern pattern, int offset, string context, string reference)
		{
			m_position = pattern.ContextPos;
			var chr = pattern.Pattern;
			// For punctuation patterns the position indicated by offset refers to the place where
			// the first punctuation character occurs. There can be a leading character indicating
			// that the pattern was found preceded by a space or at the start of a paragraph.
			if (pattern.Pattern.Length > 1)
			{
				if (m_position == ContextPosition.WordInitial || m_position == ContextPosition.Isolated)
				{
					Debug.Assert(context[offset] == chr[1]);
					// Adjust offset to account for leading space which is actually in the data
					offset--;
				}
			}
			Initialize(chr, offset, context, reference);
		}

		/// <summary />
		internal ContextInfo(string chr, int offset, string context, string reference)
		{
			Initialize(chr, offset, context, reference);
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ContextInfo"/> class.
		/// </summary>
		/// <param name="chr">The character or pattern to which this context applies.</param>
		/// <param name="offset">The offset (can be negative!).</param>
		/// <param name="context">The context (a string with the line contents).</param>
		/// <param name="reference">The reference (line number).</param>
		private void Initialize(string chr, int offset, string context, string reference)
		{
			Character = chr;
			Reference = reference;

			var startPos = Math.Max(0, offset - 50);
			var length = Math.Max(0, (startPos == 0 ? offset : offset - startPos));
			Before = context.Substring(startPos, length);

			// Since the pattern may come from multiple contiguous tokens, it's still possible
			// that the context isn't long enough to account for all the characters in the
			// pattern, in which case we will not be able to display any context after.
			startPos = Math.Min(context.Length, offset + chr.Length);
			After = startPos + 50 >= context.Length ? context.Substring(startPos) : context.Substring(startPos, 50);
		}

		/// <summary>
		/// Gets the reference (line number).
		/// </summary>
		public string Reference { get; private set; }

		/// <summary>
		/// Gets the character to which this context applies.
		/// </summary>
		public string Character { get; private set; }

		/// <summary>
		/// Gets the context before the offset.
		/// </summary>
		public string Before { get; private set; }

		/// <summary>
		/// Gets the context after the offset.
		/// </summary>
		public string After { get; private set; }

		/// <summary>
		/// Serves as a lookup key for the Context Info type. Note that this will return an
		/// identical hash code for different ContextInfo objects whose Character and Position
		/// are the same.
		/// </summary>
		public string Key => Character + (m_position == ContextPosition.Undefined ? string.Empty : m_position.ToString());
	}
}