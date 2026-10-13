// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
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
		/// <paramref name="rootHvo"/> of 0 never matches.
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
			int rootHvo = 0)
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
				});
			view.EditCompleted += (s, e) => RaiseDetailEditCompleted();
			view.InteractionCompleted += (s, e) => RaiseDetailInteractionCompleted();

			if (rootHvo != 0 && rootHvo == _shownRootHvo && CurrentContent is DataTree previous)
				view.RevealObjectsAddedSince(previous);
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
	}
}
