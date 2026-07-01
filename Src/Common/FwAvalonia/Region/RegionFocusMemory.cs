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

			return new Memento(null, -1, verticalOffset);
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

			// First pass: the exact stable id (the common case — the same field survived the re-show).
			foreach (var visual in root.GetVisualDescendants())
			{
				if (!(visual is Control control)
					|| AutomationProperties.GetAutomationId(control) != memento.AutomationId)
				{
					continue;
				}

				FocusMatch(control, memento);
				return true;
			}

			// Post-ghost-commit continuity (legacy RestoreSelection): when a ghost add-prompt commits,
			// the host recomposes and the new REAL editor's id differs from the "/ghost" id — the ghost
			// id carries the OWNER's hvo and the "/ghost" marker, the successor carries the newly created
			// object's hvo and no marker. So the exact match above misses and focus would land on the
			// floor. When the captured id is a ghost id, fall back to its successor matcher so focus
			// continues into the new real field/ws editor.
			var successorMatch = GhostSuccessorMatcher(memento.AutomationId);
			if (successorMatch != null)
			{
				foreach (var visual in root.GetVisualDescendants())
				{
					if (visual is Control control
						&& successorMatch(AutomationProperties.GetAutomationId(control)))
					{
						FocusMatch(control, memento);
						return true;
					}
				}
			}

			return false;
		}

		private static void FocusMatch(Control control, Memento memento)
		{
			control.Focus();
			if (control is TextBox box && memento.CaretIndex >= 0)
				box.CaretIndex = Math.Min(memento.CaretIndex, box.Text?.Length ?? 0);
		}

		// Maps a "/ghost" editor automation id to a predicate that recognizes the real successor editor
		// the ghost commit produced. The ghost id has the shape "{node}@{ownerHvo}/ghost.{wsKey}" (the
		// owner hvo because the object did not exist yet); the successor has "{node}@{newHvo}.{wsKey}" —
		// same node-stable prefix and same writing-system suffix, only the owned object's hvo (and the
		// "/ghost" marker) change. We therefore match on the prefix up to and including "@" plus the WS
		// suffix after "/ghost", tolerating the hvo difference. Returns null when the id is not a ghost id
		// (so non-ghost re-shows keep exact-id matching only).
		private static Func<string, bool> GhostSuccessorMatcher(string ghostAutomationId)
		{
			if (string.IsNullOrEmpty(ghostAutomationId))
				return null;

			const string marker = "/ghost";
			var markerIndex = ghostAutomationId.IndexOf(marker, StringComparison.Ordinal);
			if (markerIndex < 0)
				return null;

			// Everything after "/ghost" (e.g. ".vern") is the writing-system suffix and must match exactly.
			var wsSuffix = ghostAutomationId.Substring(markerIndex + marker.Length);

			// The node-stable prefix up to the owner-hvo separator. Composer stable ids are
			// "{node.StableId}@{hvo}"; match on the part through "@" so the differing hvo does not block
			// the successor. When there is no "@" (ids built outside the composer), fall back to the whole
			// pre-marker text as a literal prefix.
			var beforeMarker = ghostAutomationId.Substring(0, markerIndex);
			var atIndex = beforeMarker.LastIndexOf('@');
			var prefix = atIndex >= 0 ? beforeMarker.Substring(0, atIndex + 1) : beforeMarker;

			return candidate =>
				!string.IsNullOrEmpty(candidate)
				&& candidate.IndexOf(marker, StringComparison.Ordinal) < 0 // never re-match another ghost
				&& candidate.StartsWith(prefix, StringComparison.Ordinal)
				&& candidate.EndsWith(wsSuffix, StringComparison.Ordinal)
				// Guard the degenerate empty-suffix/empty-prefix case from matching unrelated rows: the
				// candidate must be strictly longer than the parts it must contain.
				&& candidate.Length >= prefix.Length + wsSuffix.Length;
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
