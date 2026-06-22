// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.VisualTree;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// Keeps keyboard focus stable across region re-shows. The host re-resolves and REPLACES the
	/// whole region view after every committed edit and every delivered external refresh; without
	/// this, tabbing out of a field (which auto-commits, 14.4) would tear down the editor the user
	/// just moved into and dump focus on the floor. Capture reads the focused editor's stable
	/// automation id (and caret) from the outgoing view; restore finds the same id in the incoming
	/// view and gives it focus — automation ids are stable per field/writing system by design, so
	/// they are the right cross-rebuild identity.
	/// </summary>
	public static class RegionFocusMemory
	{
		/// <summary>What to restore: the focused editor's automation id and caret position.</summary>
		public sealed class Memento
		{
			public Memento(string automationId, int caretIndex)
			{
				AutomationId = automationId;
				CaretIndex = caretIndex;
			}

			public string AutomationId { get; }
			public int CaretIndex { get; }
		}

		/// <summary>
		/// Captures the focus state of the editor currently focused INSIDE <paramref name="root"/>,
		/// or null when focus is elsewhere (or nothing identifies the editor).
		/// </summary>
		public static Memento Capture(Control root)
		{
			if (root == null)
				return null;
			var focusManager = TopLevel.GetTopLevel(root)?.FocusManager;
			if (!(focusManager?.GetFocusedElement() is Control focused) || !root.IsVisualAncestorOf(focused))
				return null;

			// The editor itself carries the stable id (e.g. "LexemeFormEditor.vern"); walk up in
			// case focus landed on an inner template part.
			for (var control = focused; control != null && control != root; control = control.GetVisualParent() as Control)
			{
				var id = AutomationProperties.GetAutomationId(control);
				if (!string.IsNullOrEmpty(id))
					return new Memento(id, (focused as TextBox)?.CaretIndex ?? -1);
			}

			return null;
		}

		/// <summary>
		/// Focuses the control with the memento's automation id inside <paramref name="root"/>
		/// (which must already be attached to a TopLevel). Returns false when no match exists —
		/// e.g. the field disappeared in the re-show.
		/// </summary>
		public static bool TryRestore(Control root, Memento memento)
		{
			if (root == null || memento?.AutomationId == null)
				return false;

			foreach (var visual in root.GetVisualDescendants())
			{
				if (!(visual is Control control)
					|| AutomationProperties.GetAutomationId(control) != memento.AutomationId)
				{
					continue;
				}

				control.Focus();
				if (control is TextBox box && memento.CaretIndex >= 0)
					box.CaretIndex = Math.Min(memento.CaretIndex, box.Text?.Length ?? 0);
				return true;
			}

			return false;
		}
	}
}
