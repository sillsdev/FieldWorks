// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Text;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// LaTeX text-cleaning rules, ported character-for-character from the gb4e export's
	/// xml2LaTeX.xsl (LT-22645) so the new template pipeline keeps its already-validated
	/// behavior: zero-width spaces (unrepresentable under inputenc) are dropped, a pre-existing
	/// U+FFFD replacement character in the source data becomes '?' (not something this export
	/// loses -- the source project's data was already corrupt there), and LaTeX special
	/// characters are escaped one character at a time so a character's own replacement text is
	/// never rescanned.
	/// </summary>
	public static class InterlinearTemplateEscaping
	{
		public static string CleanSource(string text)
		{
			if (string.IsNullOrEmpty(text))
				return text ?? string.Empty;
			var sb = new StringBuilder(text.Length);
			foreach (var ch in text)
			{
				if (ch == ReplacementChar)
					sb.Append('?');
				else if (ch != ZeroWidthSpace)
					sb.Append(ch);
			}
			return sb.ToString();
		}

		// Written as numeric code points, not char literals, so this file never carries a raw
		// zero-width space or replacement character in its own bytes.
		private static readonly char ReplacementChar = (char)0xFFFD;
		private static readonly char ZeroWidthSpace = (char)0x200B;

		public static string EscapeLatex(string text)
		{
			var sb = new StringBuilder(text.Length);
			foreach (var ch in text)
			{
				switch (ch)
				{
					case '\\': sb.Append("\\textbackslash{}"); break;
					case '{': sb.Append("\\{"); break;
					case '}': sb.Append("\\}"); break;
					case '&': sb.Append("\\&"); break;
					case '%': sb.Append("\\%"); break;
					case '$': sb.Append("\\$"); break;
					case '#': sb.Append("\\#"); break;
					case '_': sb.Append("\\_"); break;
					case '~': sb.Append("\\textasciitilde{}"); break;
					case '^': sb.Append("\\textasciicircum{}"); break;
					default: sb.Append(ch); break;
				}
			}
			return sb.ToString();
		}

		/// <summary>Prose text that stands on its own line (a free/literal translation, a note):
		/// escaped, but never brace-grouped.</summary>
		public static string EscapeProse(string text)
		{
			return EscapeLatex(CleanSource(text));
		}

		/// <summary>
		/// A value that becomes (part of) a gll-line token: escaped, then brace-grouped if it
		/// still contains a space, since gb4e's \getwords splits on spaces and a braced group is
		/// opaque to that split (cgloss4e.sty) -- so "{go up}" survives as one token where a bare
		/// "go up" would not.
		/// </summary>
		public static string EscapeToken(string text)
		{
			var escaped = EscapeLatex(CleanSource(text));
			return escaped.Contains(" ") ? "{" + escaped + "}" : escaped;
		}
	}
}
