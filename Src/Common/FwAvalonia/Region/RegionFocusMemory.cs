// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia;
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
		/// <summary>What to restore: the focused editor's automation id/caret and the region scroll offset.</summary>
		public sealed class Memento
		{
			public Memento(string automationId, int caretIndex, double verticalOffset = 0)
			{
				AutomationId = automationId;
				CaretIndex = caretIndex;
				VerticalOffset = verticalOffset;
			}

			public string AutomationId { get; }
			public int CaretIndex { get; }
			public double VerticalOffset { get; }
		}

		/// <summary>
		/// Captures the region's current scroll offset plus, when focus is inside <paramref name="root"/>,
		/// the focused editor's stable automation id and caret. Scroll continuity matters even when
		/// focus lives in a transient popup/context menu (e.g. removing a vector item from its
		/// flyout), so the memento remains useful even with no focused editor identity.
		/// </summary>
		public static Memento Capture(Control root)
		{
			if (root == null)
				return null;
			var scroller = FindScroller(root);
			var verticalOffset = scroller?.Offset.Y ?? 0;
			var focusManager = TopLevel.GetTopLevel(root)?.FocusManager;
			if (!(focusManager?.GetFocusedElement() is Control focused) || !root.IsVisualAncestorOf(focused))
				return new Memento(null, -1, verticalOffset);

			// The editor itself carries the stable id (e.g. "LexemeFormEditor.vern"); walk up in
			// case focus landed on an inner template part.
			for (var control = focused; control != null && control != root; control = control.GetVisualParent() as Control)
			{
				var id = AutomationProperties.GetAutomationId(control);
				if (!string.IsNullOrEmpty(id))
					return new Memento(id, (focused as TextBox)?.CaretIndex ?? -1, verticalOffset);
			}

			return null;
		}

		/// <summary>
		/// Restores the region's vertical scroll offset. Safe to call before the view is attached to a
		/// TopLevel; the ScrollViewer is part of the constructed control tree already.
		/// </summary>
		public static bool TryRestoreScroll(Control root, Memento memento)
		{
			if (root == null || memento == null)
				return false;

			var scroller = FindScroller(root);
			if (scroller == null)
				return false;
			scroller.Offset = new Vector(scroller.Offset.X, memento.VerticalOffset);
			return true;
		}

		/// <summary>
		/// Focuses the control with the memento's automation id inside <paramref name="root"/>
		/// (which must already be attached to a TopLevel). Returns false when no match exists —
		/// e.g. the field disappeared in the re-show, or the memento had scroll-only state.
		/// </summary>
		public static bool TryRestoreFocus(Control root, Memento memento)
		{
			if (root == null || string.IsNullOrEmpty(memento?.AutomationId))
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

		/// <summary>
		/// Restores both scroll and, when present, focus/caret state.
		/// </summary>
		public static bool TryRestore(Control root, Memento memento)
		{
			var restoredScroll = TryRestoreScroll(root, memento);
			var restoredFocus = TryRestoreFocus(root, memento);
			return restoredScroll || restoredFocus;
		}

		private static ScrollViewer FindScroller(Control root)
		{
			if (root is ScrollViewer selfScroller
				&& AutomationProperties.GetAutomationId(selfScroller) == "LexicalEditRegionView.Scroll")
			{
				return selfScroller;
			}

			if (root is ContentControl contentControl
				&& contentControl.Content is ScrollViewer directScroller
				&& AutomationProperties.GetAutomationId(directScroller) == "LexicalEditRegionView.Scroll")
			{
				return directScroller;
			}

			foreach (var visual in root.GetVisualDescendants())
			{
				if (visual is ScrollViewer scroller
					&& AutomationProperties.GetAutomationId(scroller) == "LexicalEditRegionView.Scroll")
				{
					return scroller;
				}
			}

			return null;
		}
	}
}
