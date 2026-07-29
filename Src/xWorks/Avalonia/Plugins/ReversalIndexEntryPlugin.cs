// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.Seams;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The native Avalonia Reversal Entries editor: claims the legacy
	/// <c>SIL.FieldWorks.XWorks.LexEd.ReversalIndexEntrySlice</c> layout identity through the plugin
	/// contract and renders the sense's reversal-entry forms as an editable multi-writing-system text
	/// field (<see cref="FwMultiWsTextField"/>) at the slice's real in-tree position, rather than an
	/// "Unsupported" placeholder row.
	/// <para>A sense's reversal entries (<c>ILexSense.ReferringReversalIndexEntries</c>) are a set of
	/// <c>IReversalIndexEntry</c>, each storing its form (<c>ReversalForm</c>, a multi-unicode string)
	/// under its owning reversal index's writing system. The editor renders one row per EXISTING
	/// reversal entry — the entry's form in its index's writing system — reusing the same
	/// plain-text-over-preserved-runs <c>TrySetRichText</c> staging every other text row uses, so the
	/// edit rides the region's SAME fenced undo step.</para>
	/// <para>DATA-SAFE SCOPE: this editor edits the form text of EXISTING reversal entries only.
	/// Creating a new reversal index entry (typing a new form on an empty row) and deleting one
	/// (clearing a form) are the legacy slice's risky parses-of-semicolon-separated-lists +
	/// find-or-create path (ReversalIndexEntrySlice.ReplaceReversalIndexEntries) and are not supported here:
	/// a sense with no reversal entry for a given index simply shows no row for it, and clearing a
	/// form to empty stores an empty form (it does not delete the entry).</para>
	/// </summary>
	public sealed class ReversalIndexEntryPlugin : ISlicePlugin
	{
		/// <summary>The legacy slice class this plugin claims (LexSenseParts.xml reversal entries slice).</summary>
		public const string ReversalIndexEntrySliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.ReversalIndexEntrySlice";

		public string LegacyClassName => ReversalIndexEntrySliceClassName;

		public Control BuildControl(SlicePluginBuildContext context)
		{
			var sense = context?.Target as ILexSense;
			var cache = context?.Cache;
			if (sense == null || cache == null)
				return null;

			try
			{
				var node = context.Node;
				var rows = BuildReversalRows(sense, cache, out var entryByWsKey);
				if (rows.Count == 0)
					return null; // no existing reversal entry: nothing editable (creation is not supported)

				var field = new DetailField(
					stableId: "reversal/" + sense.Hvo,
					label: node?.Label ?? "Reversal Entries",
					field: node?.Field ?? "ReferringReversalIndexEntries",
					writingSystem: node?.WritingSystem,
					kind: DetailFieldKind.Text,
					editorClassification: node?.EditorClassification
						?? SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.EditorClassification.Known,
					automationId: node?.AutomationId ?? "ReversalEntriesEditor",
					localizationKey: node?.LocalizationKey,
					routing: node?.Routing ?? SIL.FieldWorks.Common.FwAvalonia.ViewDefinition.SurfaceRouting.Product,
					values: rows,
					options: null,
					selectedOptionKey: null,
					isEditable: true,
					objectHvo: sense.Hvo);

				var reversalContext = new ReversalDetailEditContext(cache, context.EditContext, entryByWsKey);
				var automationId = node?.AutomationId ?? "ReversalEntriesEditor";
				return new FwMultiWsTextField(field, automationId, reversalContext,
					writingSystemFocused: wsTag => WritingSystemKeyboards.Activate(cache, wsTag));
			}
			catch (Exception e)
			{
				// Graceful degradation, same policy as the other plugins: a broken reversal read/build
				// degrades to the unsupported row (the view's null-factory guard), never the whole pane.
				Logger.WriteEvent($"ReversalIndexEntryPlugin: reversal editor unavailable for sense '{sense.Guid}': {e}");
				return null;
			}
		}

		// One editable row per EXISTING reversal entry: the entry's ReversalForm in its index's writing
		// system. The row's WsTag (and the wsKey edits route on) is the reversal index's writing-system
		// tag, which is unique per index — a sense has at most one reversal entry per index.
		private static IReadOnlyList<DetailWsValue> BuildReversalRows(ILexSense sense, LcmCache cache,
			out IReadOnlyDictionary<string, IReversalIndexEntry> entryByWsKey)
		{
			var values = new List<DetailWsValue>();
			var map = new Dictionary<string, IReversalIndexEntry>(StringComparer.Ordinal);
			var wsManager = cache.ServiceLocator.WritingSystemManager;
			var factory = cache.WritingSystemFactory;

			foreach (var entry in sense.ReferringReversalIndexEntries)
			{
				var wsTag = entry.ReversalIndex?.WritingSystem;
				if (string.IsNullOrEmpty(wsTag) || map.ContainsKey(wsTag))
					continue;
				var wsHandle = wsManager.GetWsFromStr(wsTag);
				if (wsHandle <= 0)
					continue;

				var ws = wsManager.Get(wsHandle);
				var tss = entry.ReversalForm.get_String(wsHandle);
				var richText = DetailRichTextAdapter.FromTsString(tss, factory);
				values.Add(new DetailWsValue(ws.Abbreviation, tss?.Text ?? string.Empty,
					ws.DefaultFontName, 0, ws.RightToLeftScript, ws.Id, false, richText));
				map[ws.Id] = entry;
			}

			entryByWsKey = map;
			return values;
		}
	}

	/// <summary>
	/// The Reversal Entries plugin's edit context: routes <see cref="TrySetText"/>/<see cref="TrySetRichText"/>
	/// to the matching reversal entry's <c>ReversalForm</c> (data-safe: edits existing forms only), staging
	/// through the region's SHARED <see cref="DetailEditContextBase"/> session so a reversal edit lands as
	/// ONE step on the same undoable fence as every other row. Session lifecycle (IsOpen/Commit/Cancel) and
	/// validation delegate to the host context, so the host view's Save/Cancel commit reversal edits too.
	/// </summary>
	internal sealed class ReversalDetailEditContext : IDetailEditContext
	{
		private readonly LcmCache _cache;
		private readonly IDetailEditContext _host;
		private readonly IReadOnlyDictionary<string, IReversalIndexEntry> _entryByWsKey;

		public ReversalDetailEditContext(LcmCache cache, IDetailEditContext host,
			IReadOnlyDictionary<string, IReversalIndexEntry> entryByWsKey)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_host = host;
			_entryByWsKey = entryByWsKey ?? new Dictionary<string, IReversalIndexEntry>();
		}

		public bool IsOpen => _host != null && _host.IsOpen;

		public bool TrySetText(DetailField field, string ws, string value)
		{
			if (string.IsNullOrEmpty(ws) || !_entryByWsKey.TryGetValue(ws, out var entry))
				return false;
			var wsHandle = _cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ws);
			if (wsHandle <= 0)
				return false;
			return StageOnHost(() =>
			{
				entry.ReversalForm.set_String(wsHandle,
					TsStringUtils.MakeString(value ?? string.Empty, wsHandle));
				return true;
			});
		}

		public bool TrySetRichText(DetailField field, string ws, DetailRichTextValue value)
		{
			if (value == null || string.IsNullOrEmpty(ws) || !_entryByWsKey.TryGetValue(ws, out var entry))
				return false;
			var wsHandle = _cache.ServiceLocator.WritingSystemManager.GetWsFromStr(ws);
			if (wsHandle <= 0)
				return false;
			return StageOnHost(() =>
			{
				// ReversalForm is multi-unicode (plain text): re-emit the run-replay as a plain string in
				// the row's writing system. The per-run rich projection still drives display/formatting,
				// but the stored property carries no run structure.
				var tss = DetailRichTextAdapter.ToTsString(value, _cache.WritingSystemFactory, wsHandle);
				entry.ReversalForm.set_String(wsHandle,
					TsStringUtils.MakeString(tss?.Text ?? string.Empty, wsHandle));
				return true;
			});
		}

		// Stage on the host's shared fenced session when present (the region's own context); fall back to
		// a self-contained non-undoable write only when no host context exists (defensive — the composer
		// always supplies one).
		private bool StageOnHost(Func<bool> setter)
		{
			if (_host is DetailEditContextBase fenced)
				return fenced.Stage(setter);
			if (_host != null)
				return setter(); // a non-fenced host (a test fake): apply directly
			return setter();
		}

		// Chooser / reference-vector / validation are not part of the reversal text editor; delegate
		// the session boundary to the host so the view's Save/Cancel still drive commit/rollback.
		public bool TrySetOption(DetailField field, string optionKey) => false;

		public bool TryAddReferenceItem(DetailField field, string optionKey) => false;

		public bool TryRemoveReferenceItem(DetailField field, string optionKey) => false;

		// The Reversal Entries plugin edits multi-unicode reversal forms only; it implements neither
		// IStructuredTextEditing (no StText rows are composed for it) nor picture editing.

		public IReadOnlyList<string> Validate() => _host?.Validate() ?? Array.Empty<string>();

		public void Commit() => _host?.Commit();

		public void Cancel() => _host?.Cancel();
	}
}
