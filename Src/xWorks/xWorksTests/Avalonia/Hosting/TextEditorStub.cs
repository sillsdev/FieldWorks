// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A row editor for menu tests: the current item, the editor's text with a collapsed caret,
	/// and a record of what the menu's inserts typed into it.
	/// </summary>
	internal sealed class TextEditorStub : IDetailItemSelection, IDetailTextSelection
	{
		public string SelectedItemKey { get; set; }
		public int SelectedItemIndex { get; set; } = -1;

		/// <summary>The editor's text; null means the row has no editor.</summary>
		public string EditorText { get; set; }

		/// <summary>The collapsed caret, reported as both selection ends.</summary>
		public int Caret { get; set; }

		/// <summary>Each insert, as the span it replaced and the text and caret it
		/// typed.</summary>
		public readonly List<(int Anchor, int End, string Text, int CaretBack)> Typed =
			new List<(int, int, string, int)>();

		public bool HasTextEditor => EditorText != null;
		public int EditorSelectionAnchor => Caret;
		public int EditorSelectionEnd => Caret;

		public bool ReplaceEditorText(int selectionAnchor, int selectionEnd, string text, int caretBack)
		{
			Typed.Add((selectionAnchor, selectionEnd, text, caretBack));
			return true;
		}

		public void BeginMenuGesture() { }
		public void EndMenuGesture() { }
	}
}
