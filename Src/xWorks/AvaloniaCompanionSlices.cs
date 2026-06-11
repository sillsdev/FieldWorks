// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.Reporting;
using SIL.Utils;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The identity of a layout slice whose legacy editor is a dynamically loaded custom slice
	/// (<c>editor="Custom" assemblyPath=... class=...</c>) and which composed as a placeholder row
	/// (an unsupported row, or a best-effort read-only rendering — e.g. the Messages slice's
	/// field="Self" renders as read-only text).
	/// <see cref="FieldStableId"/> is exactly the StableId of that row in the composed
	/// <see cref="LexicalEditRegionModel"/>, so promotion to the companion lane can remove the row.
	/// </summary>
	public sealed class ComposedCustomEditorField
	{
		public ComposedCustomEditorField(string fieldStableId, string className, string assemblyPath,
			string label, int objectHvo)
		{
			FieldStableId = fieldStableId;
			ClassName = className;
			AssemblyPath = assemblyPath;
			Label = label;
			ObjectHvo = objectHvo;
		}

		/// <summary>The composed region row this slice identity belongs to (StableId coordination).</summary>
		public string FieldStableId { get; }

		/// <summary>The fully qualified legacy slice class (layout `class=`).</summary>
		public string ClassName { get; }

		/// <summary>The dll the class loads from (layout `assemblyPath=`), e.g. LexEdDll.dll.</summary>
		public string AssemblyPath { get; }

		/// <summary>The localized row label (e.g. "Messages").</summary>
		public string Label { get; }

		/// <summary>The LCModel object the slice binds to (the entry for the Messages slice).</summary>
		public int ObjectHvo { get; }
	}

	/// <summary>
	/// The hybrid "companion strip" lane for designated WinForms-only custom slices: the Avalonia
	/// surface is itself hosted in WinForms (PocWinFormsHostControl inside RecordEditView), so a
	/// legacy slice whose editor cannot be rendered inside Avalonia (today: the Chorus Send/Receive
	/// Messages notes bar, which hosts Chorus.UI.Notes.Bar.NotesBarView) is instantiated for real and
	/// stacked in a WinForms strip above the Avalonia host instead of rendering as a grey
	/// unsupported row. Pure selection/filtering logic lives here so it is unit-testable without a
	/// RecordEditView; the host owns control lifetime.
	/// </summary>
	public static class AvaloniaCompanionSlices
	{
		/// <summary>The Chorus Send/Receive notes bar (LexEntryParts.xml part "LexEntry-Detail-Messages").</summary>
		public const string MessageSliceClassName = "SIL.FieldWorks.XWorks.LexEd.MessageSlice";

		// The designated companion classes. EMPTY since wave 2 (winforms-free-lexeme-editor.md D2):
		// the Messages slice — the lane's only designated class — graduated to the native
		// ChorusNotesPlugin. The mechanism stays: it is the documented coexistence lane for future
		// tools' WinForms-only custom slices (blocker register B11); with an empty set the
		// RecordEditView companion strip simply never shows.
		private static readonly HashSet<string> PromotedClassNames = new HashSet<string>(StringComparer.Ordinal);

		/// <summary>
		/// Read-only view of the designated companion classes for the burn-down governance lane
		/// (winforms-free-lexeme-editor.md D5): this set may only SHRINK — a class graduates
		/// unsupported → companion → plugin, never the other way. Pinned by
		/// LexemeEditorBurnDownTests; wave 2 (ChorusNotesPlugin, D2) emptied it.
		/// </summary>
		public static IReadOnlyCollection<string> DesignatedClassNames => PromotedClassNames;

		/// <summary>
		/// Picks the composed custom-editor fields that are designated for companion-strip promotion.
		/// </summary>
		public static IReadOnlyList<ComposedCustomEditorField> SelectPromotions(
			IReadOnlyList<ComposedCustomEditorField> customEditorFields)
		{
			return SelectPromotions(customEditorFields, PromotedClassNames);
		}

		// Testable seam: the designated set is empty since wave 2, so the mechanism's selection
		// tests inject a fake designated class (MessagesCompanionLaneTests).
		internal static IReadOnlyList<ComposedCustomEditorField> SelectPromotions(
			IReadOnlyList<ComposedCustomEditorField> customEditorFields, ISet<string> designatedClassNames)
		{
			if (customEditorFields == null || customEditorFields.Count == 0)
				return Array.Empty<ComposedCustomEditorField>();
			return customEditorFields
				.Where(f => f != null && designatedClassNames.Contains(f.ClassName))
				.ToList();
		}

		/// <summary>
		/// Returns a model without the promoted rows (by StableId), so the Avalonia region no longer
		/// shows the placeholder row for a slice the companion strip renders (or that
		/// degraded to nothing because Chorus is unavailable). Returns the same instance when nothing
		/// matches.
		/// </summary>
		public static LexicalEditRegionModel RemovePromotedFields(LexicalEditRegionModel model,
			IEnumerable<string> promotedStableIds)
		{
			if (model == null)
				throw new ArgumentNullException(nameof(model));
			var ids = new HashSet<string>(promotedStableIds ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
			if (ids.Count == 0 || !model.Fields.Any(f => ids.Contains(f.StableId)))
				return model;
			return new LexicalEditRegionModel(model.ClassName, model.LayoutName,
				model.Fields.Where(f => !ids.Contains(f.StableId)).ToList(), model.Diagnostics);
		}

		/// <summary>
		/// Instantiates the real legacy slice for a promoted field the way DataTree does for
		/// <c>editor="Custom"</c> (SliceFactory.Create: DynamicLoader by assemblyPath/class, then the
		/// DataTree install recipe Cache → Object → FinishInit; MessageSlice.FinishInit needs only
		/// those and builds the NotesBarView into Slice.Control). Returns null and logs when the
		/// slice cannot be created (e.g. Chorus/Send-Receive is unavailable) — the caller degrades
		/// to showing nothing for the row.
		/// </summary>
		public static Slice CreateCompanionSlice(ComposedCustomEditorField binding, LcmCache cache)
		{
			if (binding == null)
				throw new ArgumentNullException(nameof(binding));
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));

			Slice slice = null;
			try
			{
				if (!cache.ServiceLocator.ObjectRepository.TryGetObject(binding.ObjectHvo, out var obj))
				{
					Logger.WriteEvent($"Companion slice '{binding.ClassName}': object {binding.ObjectHvo} is gone; skipping.");
					return null;
				}

				slice = DynamicLoader.CreateObject(binding.AssemblyPath, binding.ClassName) as Slice;
				if (slice == null)
				{
					Logger.WriteEvent($"Companion slice '{binding.ClassName}' from '{binding.AssemblyPath}' is not a Slice; skipping.");
					return null;
				}

				slice.Cache = cache;
				slice.Object = obj;
				slice.FinishInit();
				return slice;
			}
			catch (Exception e)
			{
				// Graceful degradation: without a working Chorus system the Messages lane simply
				// does not appear (legacy shows an empty bar at best); never take the view down.
				Logger.WriteEvent($"Companion slice '{binding.ClassName}' unavailable; showing nothing for '{binding.Label}': {e}");
				slice?.Dispose();
				return null;
			}
		}
	}
}
