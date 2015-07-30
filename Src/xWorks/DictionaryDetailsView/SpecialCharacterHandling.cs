// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.DictionaryDetailsView
{
	/// <summary>
	/// Handles revealing non-printable characters in a View, such as DetailsView or SenseOptionsView.
	/// </summary>
	public static class SpecialCharacterHandling
	{
		private static Dictionary<char, string> s_visibleCharacterSubstitutions;

		private static Dictionary<char, string> VisibleCharacterSubstitutions
		{
			get
			{
				if (s_visibleCharacterSubstitutions==null)
					PopulateCharacterSubstitutions();
				return s_visibleCharacterSubstitutions;
			}
		}

		private static void PopulateCharacterSubstitutions()
		{
			s_visibleCharacterSubstitutions = new Dictionary<char, string>
			{
				// Dot
				{' ', "\u2219"},

				// Right-arrow
				{'\t', "\u279D"},

				// http://en.wikipedia.org/wiki/Bi-directional_text
				{'\u200F', "[RLM]"},
				{'\u200E', "[LRM]"},
				{'\u061C', "[ALM]"},
				{'\u202A', "[LRE]"},
				{'\u202D', "[LRO]"},
				{'\u202B', "[RLE]"},
				{'\u202E', "[RLO]"},
				{'\u202C', "[PDF]"},
				{'\u2066', "[LRI]"},
				{'\u2067', "[RLI]"},
				{'\u2068', "[FSI]"},
				{'\u2069', "[PDI]"},

				{'\u200D', "[ZWJ]"},
				{'\uFEFF', "[ZWNBSP]"},
				{'\u200C', "[ZWNJ]"},
				{'\u200B', "[ZWSP]"}
			};
		}

		/// <summary>
		/// Display visible characters instead of invisible characters in a textbox.
		/// </summary>
		public static void RevealInvisibleCharacters(object sender, EventArgs eventArgs)
		{
			var textBox = ((TextBox)sender);
			var selectionStart = textBox.SelectionStart;

			// Separately handle the parts before and after
			// the insertion point to more robustly handle
			// different substitution situations while still
			// being able to keep the insertion point in the
			// right place.
			string firstPart;
			if (selectionStart == textBox.Text.Length)
				firstPart = textBox.Text;
			else
				firstPart = textBox.Text.Remove(selectionStart);
			var lastPart = textBox.Text.Substring(selectionStart);

			var firstPartAsVisible = InvisibleToVisibleCharacters(firstPart);
			textBox.Text = firstPartAsVisible + InvisibleToVisibleCharacters(lastPart);

			// Keep insertion point in the same place, and even if visible replacement strings are longer than one character.
			textBox.SelectionStart = firstPartAsVisible.Length;
			// Just eliminate the selection, if any.
			textBox.SelectionLength = 0;
		}

		/// <summary>
		/// Substitute visible characters for some invisible ones.
		/// </summary>
		private static string InvisibleToVisibleCharacters(string operand)
		{
			foreach (var replacement in VisibleCharacterSubstitutions)
				operand = operand.Replace(replacement.Key.ToString(), replacement.Value);
			return operand;
		}

		public static string VisibleToInvisibleCharacters(string operand)
		{
			foreach (var replacement in VisibleCharacterSubstitutions)
				operand = operand.Replace(replacement.Value, replacement.Key.ToString());
			return operand;
		}
	}
}