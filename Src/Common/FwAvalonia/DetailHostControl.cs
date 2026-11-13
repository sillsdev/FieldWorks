// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// WinForms wrapper that hosts the Avalonia detail view inside the product app, adding the detail-view
	/// projection and per-host splitter memory over the in-process host plumbing
	/// <see cref="AvaloniaHostControlBase"/> supplies.
	/// </summary>
	public sealed class DetailHostControl : AvaloniaHostControlBase
	{
		// The splitter (label/value column) width the user dragged, remembered across re-shows for
		// THIS host only -- deliberately per-instance, never a process-global static. Used only
		// as the in-process fallback when the host (RecordEditView) supplies no session-persistence
		// hooks; the product host routes a PropertyTable LocalSetting through ShowDetail so the width
		// also survives across SESSIONS, mirroring legacy slice-splitter persistence.
		private double? _rememberedLabelColumnWidth;

		// The root object of the detail last shown, or 0 when none was given.
		private int _shownRootHvo;

		private readonly DetailCurrentRow _currentRow = new DetailCurrentRow();

		// The last selection a structured-text paragraph of the shown record reported.
		private DetailTextSelectionSnapshot _textSelection;

		/// <inheritdoc />
		public override bool IsDetailInteractionInFlight
			=> CurrentContent is DataTree tree
				&& (tree.IsInteractionInFlight || tree.HasUnsubmittedText);

		public DetailHostControl()
		{
			Name = "DetailHostControl";
			AccessibleName = "RecordEditView.AvaloniaHost";
			AccessibleDescription = FwAvaloniaStrings.AvaloniaHostName;
		}

		/// <summary>
		/// Replaces the shown detail view with one for <paramref name="detail"/>, carrying focus
		/// and the label column width across. When <paramref name="rootHvo"/> names the same
		/// object as the previous show, any collapsed section hiding an object that was not in
		/// the previous view is expanded, so an object inserted into a collapsed section is
		/// shown, except a section the user collapsed after the previous view's own edit. A
		/// <paramref name="rootHvo"/> of 0 never matches. <paramref name="hotlinkItems"/>
		/// supplies a section header's hotlink commands; without it no hotlinks are shown.
		/// </summary>
		public void ShowDetail(DetailModel detail, IDetailEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<DetailMenuRequest> menuRequested = null,
			Action<DetailLinkRequest> linkRequested = null,
			IFwClipboard clipboard = null,
			Func<double?> getLabelColumnWidth = null,
			Action<double> labelColumnWidthChanged = null,
			int rootHvo = 0,
			Func<DetailField, IReadOnlyList<DetailMenuItem>> hotlinkItems = null)
		{
			if (detail == null) throw new ArgumentNullException(nameof(detail));
			// Splitter position persists per-HOST across re-shows: this long-lived host owns
			// the in-process remembered width, so each window/preview keeps its own -- no
			// process-global
			// field. When the product host supplies persistence hooks, the read/write chains
			// through them too, so a width dragged in one session is restored in the next; otherwise it
			// falls back to the process-only field (e.g. the preview host / headless tests).
			var view = new DataTree(detail, editContext, writingSystemFocused,
				getExpansionState, expansionChanged, menuRequested, linkRequested, clipboard,
				() => getLabelColumnWidth?.Invoke() ?? _rememberedLabelColumnWidth,
				w =>
				{
					_rememberedLabelColumnWidth = w;
					labelColumnWidthChanged?.Invoke(w);
				},
				hotlinkItems);
			view.EditCompleted += (s, e) => RaiseDetailEditCompleted();
			view.InteractionCompleted += (s, e) => RaiseDetailInteractionCompleted();
			view.UndoRedoKeyUnhandled += (s, e) =>
				e.Handled = RaiseUndoRedoKeyUnhandled(e.Key, e.KeyModifiers);
			view.RowFocused += _currentRow.Remember;
			view.TextSelectionChanged += snapshot => _textSelection = snapshot;

			if (rootHvo != 0 && rootHvo == _shownRootHvo && CurrentContent is DataTree previous)
				view.RevealObjectsAddedSince(previous);
			else
			{
				_currentRow.Forget();
				_textSelection = null;
			}
			_shownRootHvo = rootHvo;

			// Continuity across the re-show, applied once the new view lays out. Focus leaves the
			// old view first: detaching a focused editor makes Avalonia refocus and scroll away.
			var focusMemento = DetailFocusMemory.Capture(CurrentContent);
			DetailFocusMemory.ReleaseFocus(CurrentContent);
			SetHostContent(view);
			DetailFocusMemory.RestoreAfterLayout(view, focusMemento);
		}

		/// <summary>
		/// Asks the next re-show to give keyboard focus to the vector row's current item, for a
		/// gesture (a menu-driven move) made while no editor had focus.
		/// </summary>
		public void FocusVectorItemOnNextShow() => (CurrentContent as DataTree)?.RequestVectorFocusOnRebuild();

		/// <summary>
		/// Asks the next re-show to give keyboard focus to the first of
		/// <paramref name="rowAutomationIds"/> it still shows, for a gesture that removed the
		/// focused row.
		/// </summary>
		public void FocusRowOnNextShow(IReadOnlyList<string> rowAutomationIds)
			=> (CurrentContent as DataTree)?.RequestRowFocusOnRebuild(rowAutomationIds);

		/// <summary>
		/// Asks the next re-show to give keyboard focus to the first row showing the object
		/// with <paramref name="hvo"/>, for a gesture that replaced the focused row's object.
		/// </summary>
		/// <param name="fieldName">The field whose row of that object to focus first; null
		/// for none.</param>
		public void FocusObjectOnNextShow(int hvo, string fieldName = null)
			=> (CurrentContent as DataTree)?.RequestObjectFocusOnRebuild(hvo, fieldName);

		/// <summary>The detail model shown now; null before the first show.</summary>
		public DetailModel ShownDetail => (CurrentContent as DataTree)?.Model;

		/// <summary>
		/// The row of the shown record that last had keyboard focus, as the shown detail has
		/// it; null when no row of the record has had focus since it was shown, or the row is
		/// gone.
		/// </summary>
		public DetailField CurrentRow => _currentRow.ResolveIn(ShownDetail);

		/// <summary>
		/// The selection the current row's structured-text editor last reported while it had
		/// focus; null when the current row is another row or has reported none.
		/// </summary>
		public DetailTextSelectionSnapshot CurrentTextSelection
		{
			get
			{
				var selection = _textSelection;
				var row = CurrentRow;
				return selection != null && row != null
					&& string.Equals(selection.Field.RowAutomationId, row.RowAutomationId, StringComparison.Ordinal)
					? selection
					: null;
			}
		}
	}
}
