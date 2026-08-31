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

		public DetailHostControl()
		{
			Name = "DetailHostControl";
			AccessibleName = "RecordEditView.AvaloniaHost";
			AccessibleDescription = FwAvaloniaStrings.AvaloniaHostName;
		}

		public void ShowDetail(DetailModel detail, IDetailEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<DetailMenuRequest> menuRequested = null,
			Action<DetailLinkRequest> linkRequested = null,
			IFwClipboard clipboard = null,
			Func<double?> getLabelColumnWidth = null,
			Action<double> labelColumnWidthChanged = null,
			Action<DetailField> fieldFocused = null)
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
				}, fieldFocused);
			view.EditCompleted += (s, e) => RaiseDetailEditCompleted();

			var focusMemento = DetailFocusMemory.Capture(CurrentContent);
			if (focusMemento != null)
				DetailFocusMemory.TryRestoreScroll(view, focusMemento);
			SetHostContent(view);
			if (!string.IsNullOrEmpty(focusMemento?.AutomationId))
			{
				Avalonia.Threading.Dispatcher.UIThread.Post(
					() => DetailFocusMemory.TryRestoreFocus(view, focusMemento),
					Avalonia.Threading.DispatcherPriority.Input);
			}
		}
	}
}
