// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// WinForms wrapper that hosts the Avalonia lexical-edit region inside the product app. The in-process
	/// net48 host plumbing (Avalonia bootstrap, companion strip, keyboard interop, context menus, message
	/// states) lives in the reusable <see cref="AvaloniaRegionHostControl"/> base (Stage 2.1); this class
	/// adds only the lexical-edit region projection and its per-host splitter memory.
	/// </summary>
	public sealed class LexicalEditHostControl : AvaloniaRegionHostControl
	{
		// The splitter (label/value column) width the user dragged, remembered across re-shows for
		// THIS host only (11.15) — replaces the former process-global static on the view. Used only
		// as the in-process fallback when the host (RecordEditView) supplies no session-persistence
		// hooks; the product host routes a PropertyTable LocalSetting through ShowRegion so the width
		// also survives across SESSIONS, mirroring legacy slice-splitter persistence (Task C2).
		private double? _rememberedLabelColumnWidth;

		public LexicalEditHostControl()
		{
			Name = "LexicalEditHostControl";
			AccessibleName = "RecordEditView.AvaloniaHost";
			AccessibleDescription = FwAvaloniaStrings.AvaloniaHostName;
		}

		public void ShowRegion(LexicalEditRegionModel region, IRegionEditContext editContext = null,
			Action<string> writingSystemFocused = null,
			Func<string, bool?> getExpansionState = null,
			Action<string, bool> expansionChanged = null,
			Action<RegionMenuRequest> menuRequested = null,
			Action<RegionLinkRequest> linkRequested = null,
			IFwClipboard clipboard = null,
			Func<double?> getLabelColumnWidth = null,
			Action<double> labelColumnWidthChanged = null,
			IRegionMediaServices mediaServices = null)
		{
			if (region == null) throw new ArgumentNullException(nameof(region));
			// Splitter position persists per-HOST across re-shows (11.15): this long-lived host owns
			// the in-process remembered width, so each window/preview keeps its own — no process-global
			// field. When the product host supplies persistence hooks (Task C2) the read/write chains
			// through them too, so a width dragged in one session is restored in the next; otherwise it
			// falls back to the process-only field (e.g. the preview host / headless tests).
			var view = new LexicalEditRegionView(region, editContext, writingSystemFocused,
				getExpansionState, expansionChanged, menuRequested, linkRequested, clipboard,
				() => getLabelColumnWidth?.Invoke() ?? _rememberedLabelColumnWidth,
				w =>
				{
					_rememberedLabelColumnWidth = w;
					labelColumnWidthChanged?.Invoke(w);
				},
				mediaServices);
			view.EditCompleted += (s, e) => RaiseRegionEditCompleted();

			var focusMemento = RegionFocusMemory.Capture(CurrentContent);
			if (focusMemento != null)
				RegionFocusMemory.TryRestoreScroll(view, focusMemento);
			SetHostContent(view);
			if (!string.IsNullOrEmpty(focusMemento?.AutomationId))
			{
				Avalonia.Threading.Dispatcher.UIThread.Post(
					() => RegionFocusMemory.TryRestoreFocus(view, focusMemento),
					Avalonia.Threading.DispatcherPriority.Input);
			}
		}
	}
}
