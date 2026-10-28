// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia;
using Avalonia.VisualTree;

namespace SIL.FieldWorks.Common.FwAvalonia.Detail
{
	/// <summary>
	/// Keeps keyboard focus stable across detail-view re-shows. The host re-resolves and REPLACES the
	/// whole detail view after every committed edit and every delivered external refresh; without
	/// this, tabbing out of a field (which auto-commits) would tear down the editor the user
	/// just moved into and dump focus on the floor. Capture reads the focused editor's stable
	/// automation id (and caret) from the outgoing view; restore finds the same id in the incoming
	/// view and gives it focus -- automation ids are stable per field/writing system by design,
	/// so
	/// they are the right cross-rebuild identity.
	/// </summary>
	public static class DetailFocusMemory
	{
		/// <summary>
		/// What to restore: the focused editor's automation id/caret, the detail view scroll
		/// offset, and the current item of a reference-vector row.
		/// </summary>
		public sealed class Memento
		{
			public Memento(string automationId, int caretIndex, double verticalOffset = 0,
				string vectorAutomationId = null, string vectorItemKey = null, int vectorItemIndex = -1,
				bool focusVectorItem = false, IReadOnlyList<string> rowAutomationIds = null,
				int objectHvo = 0)
			{
				AutomationId = automationId;
				CaretIndex = caretIndex;
				VerticalOffset = verticalOffset;
				VectorAutomationId = vectorAutomationId;
				VectorItemKey = vectorItemKey;
				VectorItemIndex = vectorItemIndex;
				FocusVectorItem = focusVectorItem;
				RowAutomationIds = rowAutomationIds;
				ObjectHvo = objectHvo;
			}

			/// <summary>
			/// The object whose first row the outgoing view asked to focus after the rebuild
			/// (a gesture replaced the focused row's object); 0 when none.
			/// </summary>
			public int ObjectHvo { get; }

			/// <summary>
			/// The automation ids of the rows the outgoing view asked to try focusing after
			/// the rebuild, in order (a gesture removed the focused row and maybe its
			/// neighbours); null when none.
			/// </summary>
			public IReadOnlyList<string> RowAutomationIds { get; }

			/// <summary>
			/// True when the outgoing view asked for the vector row's current item to take
			/// keyboard focus after the rebuild (a gesture made from a menu, with no editor
			/// focused). A remembered focus on one of that row's items asks for the same.
			/// </summary>
			public bool FocusVectorItem { get; }

			public string AutomationId { get; }
			public int CaretIndex { get; }
			public double VerticalOffset { get; }

			/// <summary>The automation id of the reference-vector row that had a current item;
			/// null when none did.</summary>
			public string VectorAutomationId { get; }

			/// <summary>The option key of that row's current item.</summary>
			public string VectorItemKey { get; }

			/// <summary>The index of that item, the fallback when no item has its key; -1 when
			/// none.</summary>
			public int VectorItemIndex { get; }
		}

		/// <summary>
		/// Captures the detail view's current scroll offset plus, when focus is inside <paramref name="root"/>,
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
			// A reference-vector row's current item survives the rebuild too, so a move made
			// from a menu (no focused editor) leaves the moved item current.
			var tree = root as DataTree;
			var selectedVector = tree?.SelectedVector ?? FindSelectedVector(root);
			var vectorId = selectedVector == null ? null : AutomationProperties.GetAutomationId(selectedVector);
			var vectorKey = selectedVector?.SelectedItemKey;
			var vectorIndex = selectedVector?.SelectedItemIndex ?? -1;
			var focusVector = tree?.VectorFocusRequested ?? false;
			var focusRow = tree?.RowFocusRequested;
			var focusObject = tree?.ObjectFocusRequested ?? 0;

			var focusManager = TopLevel.GetTopLevel(root)?.FocusManager;
			if (!(focusManager?.GetFocusedElement() is Control focused) || !root.IsVisualAncestorOf(focused))
				return new Memento(null, -1, verticalOffset, vectorId, vectorKey, vectorIndex, focusVector, focusRow, focusObject);

			// The editor itself carries the stable id (e.g. "LexemeFormEditor.vern"); walk up in
			// case focus landed on an inner template part.
			for (var control = focused; control != null && control != root; control = control.GetVisualParent() as Control)
			{
				var id = AutomationProperties.GetAutomationId(control);
				if (!string.IsNullOrEmpty(id))
				{
					return new Memento(id, (focused as TextBox)?.CaretIndex ?? -1, verticalOffset,
						vectorId, vectorKey, vectorIndex, focusVector, focusRow, focusObject);
				}
			}

			return new Memento(null, -1, verticalOffset, vectorId, vectorKey, vectorIndex, focusVector, focusRow, focusObject);
		}

		// Whether the rebuilt view should focus the vector row's current item: the outgoing view
		// asked for it, or the remembered focused control was one of that row's items.
		private static bool WantsVectorFocus(Memento memento)
		{
			if (memento.FocusVectorItem)
				return true;
			return FwReferenceVectorField.IsItemAutomationId(memento.VectorAutomationId, memento.AutomationId);
		}

		/// <summary>
		/// Clears keyboard focus when it sits inside <paramref name="root"/>, so replacing the
		/// view detaches no focused element and the swap carries no focus-change side effects
		/// into the new view. Capture first: this forgets which editor had focus.
		/// </summary>
		public static void ReleaseFocus(Control root)
		{
			if (root == null)
				return;
			var focusManager = TopLevel.GetTopLevel(root)?.FocusManager;
			if (focusManager?.GetFocusedElement() is Control focused && root.IsVisualAncestorOf(focused))
				focusManager.ClearFocus();
		}

		// The visual-tree fallback for a root that is not a DataTree.
		private static FwReferenceVectorField FindSelectedVector(Control root)
		{
			foreach (var visual in root.GetVisualDescendants())
			{
				if (visual is FwReferenceVectorField vector && vector.SelectedItemKey != null)
					return vector;
			}
			return null;
		}

		private static FwReferenceVectorField FindVector(Control root, string automationId)
		{
			var rows = (root as DataTree)?.VectorRows;
			if (rows != null)
			{
				foreach (var row in rows)
				{
					if (AutomationProperties.GetAutomationId(row) == automationId)
						return row;
				}
				return null;
			}
			foreach (var visual in root.GetVisualDescendants())
			{
				if (visual is FwReferenceVectorField vector
					&& AutomationProperties.GetAutomationId(vector) == automationId)
				{
					return vector;
				}
			}
			return null;
		}

		/// <summary>
		/// Makes the memento's vector item current again in the row with the same automation id,
		/// or, when no item has that key, the item now at its index. Returns false when the
		/// memento carries none or the row is not shown or is empty.
		/// </summary>
		public static bool TryRestoreVectorSelection(Control root, Memento memento)
			=> RestoreVectorSelection(root, memento) != null;

		// The row whose selection was restored, or null.
		private static FwReferenceVectorField RestoreVectorSelection(Control root, Memento memento)
		{
			if (root == null || string.IsNullOrEmpty(memento?.VectorAutomationId))
				return null;
			var vector = FindVector(root, memento.VectorAutomationId);
			if (vector == null)
				return null;
			if (vector.SelectItem(memento.VectorItemKey))
				return vector;
			return memento.VectorItemIndex >= 0 && vector.SelectItemAt(memento.VectorItemIndex)
				? vector
				: null;
		}

		/// <summary>
		/// Restores the detail view's vertical scroll offset. Safe to call before the view is attached to a
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

		// How many layout passes the restore waits for the scroller to measure before giving
		// up on the offset, so the handler never outlives a view that never shows content.
		private const int MaxLayoutPassesToWait = 10;

		/// <summary>
		/// Restores the memento once the incoming view has laid out: the scroll offset first (an
		/// offset set before layout is clamped to zero by the still-empty extent), then the
		/// vector row's current item, then focus -- the object the outgoing view asked for (a
		/// gesture added or replaced one) first of all; else the remembered editor; else that
		/// current item when the memento asks for it (a move or removal made from the row keeps
		/// the keyboard in the row); else the first surviving row the outgoing view asked for
		/// (a gesture removed the focused row), which comes before the remembered editor only
		/// when that is a row's menu button. The scroll offset yields to the row of the object
		/// asked for. Safe to call before the view is attached.
		/// </summary>
		public static void RestoreAfterLayout(Control root, Memento memento)
		{
			if (root == null || memento == null)
				return;
			// Wait for the first layout pass: rows join the visual tree only when the form
			// templates, and the offset only takes once the scroller has measured its content.
			var scroller = FindScroller(root);
			var needsScroll = scroller != null && memento.VerticalOffset > 0;
			var passes = 0;
			EventHandler onLayout = null;
			onLayout = (s, e) =>
			{
				if (needsScroll && scroller.Extent.Height <= 0 && ++passes < MaxLayoutPassesToWait)
					return;
				root.LayoutUpdated -= onLayout;
				if (needsScroll)
					TryRestoreScroll(root, memento);
				var vector = RestoreVectorSelection(root, memento);
				// The object a gesture added or replaced takes the keyboard outright. A row
				// request yields to a surviving editor, unless the gesture was made from a
				// row's menu button, which left focus on that button.
				var focused = (memento.ObjectHvo != 0 || RemembersAMenuButton(memento))
					&& RestoreRequestedFocus(root, memento);
				if (!focused && !TryRestoreFocus(root, memento))
				{
					if (vector != null && WantsVectorFocus(memento))
						vector.FocusItemAt(vector.SelectedItemIndex);
					else
						RestoreRequestedFocus(root, memento);
				}
				// After the next arrange: re-assert the remembered offset, or bring the row
				// of the object the gesture chose into view wherever it sits.
				if (needsScroll)
				{
					var showsRequestedObject = focused && memento.ObjectHvo != 0;
					Avalonia.Threading.Dispatcher.UIThread.Post(() =>
					{
						if (showsRequestedObject)
							(TopLevel.GetTopLevel(root)?.FocusManager?.GetFocusedElement() as Control)?.BringIntoView();
						else
							TryRestoreScroll(root, memento);
					}, Avalonia.Threading.DispatcherPriority.Background);
				}
			};
			root.LayoutUpdated += onLayout;
		}

		// Whether the remembered control is a row's field-options button or hotlinks link
		// rather than an editor.
		private static bool RemembersAMenuButton(Memento memento)
			=> memento.AutomationId != null
				&& (memento.AutomationId.EndsWith(".FieldMenu", StringComparison.Ordinal)
					|| memento.AutomationId.EndsWith(".Hotlinks", StringComparison.Ordinal));

		// Focuses what the outgoing view asked for: the first surviving row of those named,
		// else the first row of the object named; false when it asked for nothing or none can.
		private static bool RestoreRequestedFocus(Control root, Memento memento)
		{
			var tree = root as DataTree;
			if (memento.RowAutomationIds != null)
				return tree?.FocusFirstRow(memento.RowAutomationIds) == true;
			return memento.ObjectHvo != 0 && tree?.FocusFirstRowOf(memento.ObjectHvo) == true;
		}

		/// <summary>
		/// Focuses the control with the memento's automation id inside <paramref name="root"/>
		/// (which must already be attached to a TopLevel). Returns false when no match exists --
		/// e.g. the field disappeared in the re-show, or the memento had scroll-only state.
		/// </summary>
		public static bool TryRestoreFocus(Control root, Memento memento)
		{
			if (root == null || string.IsNullOrEmpty(memento?.AutomationId))
				return false;

			// First pass: the exact stable id (the common case -- the same field survived the
			// re-show).
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
			// the host recomposes and the new REAL editor's id differs from the "/ghost" id --
			// the ghost
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
		// owner hvo because the object did not exist yet); the successor has
		// "{node}@{newHvo}.{wsKey}" --
		// same node-stable prefix and same writing-system suffix, only the owned object's hvo (and the
		// "/ghost" marker) change. We therefore match on the prefix up to and including "@" plus the WS
		// suffix after "/ghost", tolerating the hvo difference. Returns null when the id is not a ghost id
		// (so non-ghost re-shows keep exact-id matching only).
		private static Func<string, bool> GhostSuccessorMatcher(string ghostAutomationId)
		{
			if (string.IsNullOrEmpty(ghostAutomationId))
				return null;

			const string marker = DetailField.GhostStableIdSuffix;
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
			var restoredSelection = TryRestoreVectorSelection(root, memento);
			return restoredScroll || restoredFocus || restoredSelection;
		}

		private static ScrollViewer FindScroller(Control root)
		{
			if (root is ScrollViewer selfScroller
				&& AutomationProperties.GetAutomationId(selfScroller) == "DataTree.Scroll")
			{
				return selfScroller;
			}

			if (root is ContentControl contentControl
				&& contentControl.Content is ScrollViewer directScroller
				&& AutomationProperties.GetAutomationId(directScroller) == "DataTree.Scroll")
			{
				return directScroller;
			}

			foreach (var visual in root.GetVisualDescendants())
			{
				if (visual is ScrollViewer scroller
					&& AutomationProperties.GetAutomationId(scroller) == "DataTree.Scroll")
				{
					return scroller;
				}
			}

			return null;
		}
	}
}
