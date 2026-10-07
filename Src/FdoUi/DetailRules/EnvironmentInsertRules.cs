// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel.Core.Phonology;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// When each of the five environment inserts (slash, bar, natural class, optional item,
	/// word boundary) is allowed in an environment string at a selection. The text and the
	/// selection come from whichever editor shows the string; a selection end below zero means
	/// the editor has no selection, which allows nothing that acts at a caret.
	/// </summary>
	public static class EnvironmentInsertRules
	{
		/// <summary>The text Insert Environment slash types.</summary>
		public const string Slash = "/";
		/// <summary>The text Insert Environment bar types.</summary>
		public const string Bar = "_";
		/// <summary>The text Insert Word Boundary types.</summary>
		public const string HashMark = "#";
		/// <summary>The text Insert Optional Item types; the caret belongs between the
		/// parentheses.</summary>
		public const string OptionalItem = "()";

		/// <summary>The text Insert Natural Class types for a class abbreviation.</summary>
		public static string NaturalClass(string abbreviation) => "[" + abbreviation + "]";

		/// <summary>Allowed while the string has no slash; an empty string takes one.</summary>
		public static bool CanInsertSlash(string text)
			=> string.IsNullOrEmpty(text) || text.IndexOf('/') < 0;

		/// <summary>
		/// Allowed once the string has a slash and no bar yet, with the whole selection after
		/// the slash.
		/// </summary>
		/// <param name="text">The environment string.</param>
		/// <param name="selectionAnchor">The selection's anchor offset; below zero when there is
		/// no selection.</param>
		/// <param name="selectionEnd">The selection's end offset; below zero when there is no
		/// selection.</param>
		public static bool CanInsertBar(string text, int selectionAnchor, int selectionEnd)
		{
			if (string.IsNullOrEmpty(text) || selectionAnchor < 0 || selectionEnd < 0)
				return false;
			var slash = text.IndexOf('/');
			return slash >= 0 && selectionEnd > slash && selectionAnchor > slash && text.IndexOf('_') < 0;
		}

		/// <summary>
		/// Allowed where the recognizer accepts an item (a natural class or an optional item):
		/// after the slash, in a string that has one.
		/// </summary>
		/// <param name="text">The environment string.</param>
		/// <param name="selectionAnchor">The selection's anchor offset; below zero when there is
		/// no selection.</param>
		/// <param name="selectionEnd">The selection's end offset; below zero when there is no
		/// selection.</param>
		public static bool CanInsertItem(string text, int selectionAnchor, int selectionEnd)
			=> HasSelection(text, selectionAnchor, selectionEnd)
				&& PhonEnvRecognizer.CanInsertItem(text, selectionEnd, selectionAnchor);

		/// <summary>
		/// Allowed where the recognizer accepts a word boundary: right after the slash or at the
		/// end of the string.
		/// </summary>
		/// <param name="text">The environment string.</param>
		/// <param name="selectionAnchor">The selection's anchor offset; below zero when there is
		/// no selection.</param>
		/// <param name="selectionEnd">The selection's end offset; below zero when there is no
		/// selection.</param>
		public static bool CanInsertHashMark(string text, int selectionAnchor, int selectionEnd)
			=> HasSelection(text, selectionAnchor, selectionEnd)
				&& PhonEnvRecognizer.CanInsertHashMark(text, selectionEnd, selectionAnchor);

		private static bool HasSelection(string text, int selectionAnchor, int selectionEnd)
			=> !string.IsNullOrEmpty(text) && selectionAnchor >= 0 && selectionEnd >= 0;
	}
}
