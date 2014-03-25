using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Code for converting between unicode characters and their codepoints.
	/// </summary>
	static class UnicodeCharacterEditingHelper
	{
		/// <summary>
		/// If input ends in one of more hex digits, replace them with a UTF-16 character at that codepoint. Only up to 4 hex digits are supported.
		/// Hex digits can optionally start with "U+" or "u+".
		/// See LT-13359.
		/// </summary>
		internal static string ConvertFinalDigitsToCharacter(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			// Matches 1 to 4 hex digits at the end of input, and replaces them with the result of the lambda.
			// U+ or u+ is optional, and will be omitted from the output.
			return Regex.Replace(input, @"(?:[Uu]\+)?([0-9A-Fa-f]{1,4})$", (match) =>
			{
				if (!match.Success)
					return match.Value;

				var hexdigits = match.Groups[1].Value;

				int codepoint;
				if (!int.TryParse(hexdigits, NumberStyles.AllowHexSpecifier, null, out codepoint))
				{
					// Failed somehow.
					return match.Value;
				}

				var character = (char) codepoint;
				if (char.IsSurrogate(character))
					return match.Value;
				return character.ToString();
			});
		}

		/// <summary>
		/// Replace the last character of input with four hex digits, representing the code point of the character replaced. See LT-13359.
		/// Does not convert surrogates.
		/// </summary>
		internal static string ConvertFinalCharacterToCodepoint(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var lastCharacter = input.Last();

			if (char.IsSurrogate(lastCharacter))
				return input;

			var hexCodepoint = ((int)lastCharacter).ToString("X4");
			return input.Remove(input.Length - 1) + hexCodepoint;
		}

		/// <summary>
		/// If input ends in hex digits, replace them with the corresponding character from that codepoint. Otherwise replace the last character with its four hex-digit codepoint.
		/// </summary>
		public static string ConvertFinal(string input)
		{
			// Does input end in a hex digit?
			if (Regex.IsMatch(input, "[0-9A-Fa-f]$"))
				return ConvertFinalDigitsToCharacter(input);
			return ConvertFinalCharacterToCodepoint(input);
		}

		/// <summary>
		/// Attach to a TextBox's KeyDown event. When user presses ALT-X, replace
		/// the preceeding hex-digits with a corresponding character from that codepoint, or the preceeding
		/// non-hex-digit with the hex-digits of the character's codepoint.
		/// See LT-13359.
		/// Preserves insertion point location, and current selection.
		/// </summary>
		public static void HandleKeyDown(object sender, KeyEventArgs keyEventArgs)
		{
			if (keyEventArgs.Alt && keyEventArgs.KeyCode == Keys.X)
			{
				var textbox = ((TextBoxBase)sender);
				var insertionPointLocation = textbox.SelectionStart;
				var originalSelectionLength = textbox.SelectionLength;
				var beginningText = textbox.Text.Substring(0, insertionPointLocation);
				var endingText = textbox.Text.Substring(insertionPointLocation);

				beginningText = ConvertFinal(beginningText);

				textbox.Text = beginningText + endingText;
				textbox.SelectionStart = beginningText.Length;
				textbox.SelectionLength = originalSelectionLength;
			}
		}
	}
}
