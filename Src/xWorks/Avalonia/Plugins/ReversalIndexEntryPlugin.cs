// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// The Avalonia Reversal Entries editor for a sense: claims the
	/// <c>SIL.FieldWorks.XWorks.LexEd.ReversalIndexEntrySlice</c> layout identity and renders an
	/// <see cref="FwReversalEntriesField"/>. Each reversal index whose writing system the row
	/// shows is a group listing the sense's entries in it, then an add row; an index that does
	/// not exist is never created just to show a group (LT-4480). Typing links, relinks, or
	/// unlinks entries, with colons marking subentries (LT-4665), and a row can jump to its
	/// entry in the Reversal Index tool. A build failure degrades to the Unsupported row.
	/// </summary>
	public sealed class ReversalIndexEntryPlugin : ISlicePlugin
	{
		/// <summary>The layout class this plugin claims: the sense's Reversal Entries.</summary>
		public const string ReversalIndexEntrySliceClassName =
			"SIL.FieldWorks.XWorks.LexEd.ReversalIndexEntrySlice";

		/// <summary>The editor's automation id when the layout node declares none.</summary>
		public const string DefaultAutomationId = "ReversalEntriesEditor";

		/// <summary>The tool a row's jump opens, showing the entry.</summary>
		public const string ReversalIndexTool = "reversalToolEditComplete";

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
				var label = string.IsNullOrEmpty(node?.Label)
					? node?.Field ?? DefaultAutomationId
					: StringTable.Table.LocalizeAttributeValue(node.Label);
				var automationId = node?.AutomationId ?? DefaultAutomationId;
				var host = context.EditContext;
				var editing = new ReversalDetailEditContext(cache, host, sense, label);
				var groups = editing.CreateGroups(context.VisibleWritingSystems);

				Action<string> navigate = null;
				var linkRequested = context.Render?.LinkRequested;
				if (linkRequested != null)
				{
					var field = new DetailField(
						stableId: "reversal/" + sense.Hvo,
						label: label,
						field: node?.Field,
						writingSystem: node?.WritingSystem,
						kind: DetailFieldKind.Custom,
						editorClassification: node?.EditorClassification ?? EditorClassification.Known,
						automationId: automationId,
						localizationKey: node?.LocalizationKey,
						routing: node?.Routing ?? HostRouting.Product,
						values: null,
						options: null,
						selectedOptionKey: null,
						isEditable: true,
						objectHvo: sense.Hvo);
					navigate = rowKey =>
					{
						var target = editing.TryResolveMainEntryGuid(rowKey);
						if (target.HasValue)
							RequestShowInReversalIndex(linkRequested, field, target.Value);
					};
				}

				var control = new FwReversalEntriesField(label, automationId, groups,
					host == null ? null : editing, context.WritingSystemFocused, navigate,
					context.Render?.WsAbbrevColumnWidth);
				// The field stages only when focus leaves it, so the host's save must ask for its
				// edits when it runs with focus still inside.
				(host as DetailEditContextBase)?.AddPendingEditFlush(control.CommitPendingEdits);
				return control;
			}
			catch (Exception e)
			{
				Logger.WriteEvent($"ReversalIndexEntryPlugin: reversal editor unavailable for sense '{sense.Guid}': {e}");
				return null;
			}
		}

		/// <summary>Asks the host to show the entry with guid <paramref name="target"/> in the
		/// Reversal Index tool.</summary>
		internal static void RequestShowInReversalIndex(Action<DetailLinkRequest> linkRequested,
			DetailField field, Guid target)
		{
			linkRequested(new DetailLinkRequest(field, new DetailChooserLink(
				FwAvaloniaStrings.ReversalShowInReversalIndex, ReversalIndexTool, target.ToString())));
		}
	}

	/// <summary>
	/// The Reversal Entries edit context: projects a sense's reversal entries into
	/// <see cref="DetailReversalGroup"/> rows and applies row commits to them. Every write
	/// stages on the host's shared fenced session, so a commit is one undo step with the view's
	/// other edits; session lifecycle and validation delegate to the host.
	/// </summary>
	internal sealed class ReversalDetailEditContext : IDetailEditContext, IReversalEntryEditing
	{
		private readonly LcmCache _cache;
		private readonly IDetailEditContext _host;
		private readonly ILexSense _sense;
		private readonly string _fieldLabel;
		private readonly Dictionary<string, RowBinding> _rows =
			new Dictionary<string, RowBinding>(StringComparer.Ordinal);
		private int _nextRowKey;

		public ReversalDetailEditContext(LcmCache cache, IDetailEditContext host, ILexSense sense,
			string fieldLabel)
		{
			_cache = cache ?? throw new ArgumentNullException(nameof(cache));
			_sense = sense ?? throw new ArgumentNullException(nameof(sense));
			_host = host;
			_fieldLabel = fieldLabel;
		}

		// The reversal index a row belongs to and the entry it shows; null is an add row.
		private sealed class RowBinding
		{
			public RowBinding(IReversalIndex index, IReversalIndexEntry entry)
			{
				Index = index;
				Entry = entry;
			}

			public IReversalIndex Index { get; }

			public IReversalIndexEntry Entry { get; set; }
		}

		/// <summary>
		/// The groups to show, one per analysis writing system allowed by
		/// <paramref name="visibleWritingSystems"/> (null or empty allows all) that has a
		/// reversal index. A writing system that is not a current analysis one shows only when
		/// the sense has entries in it. Issues fresh row keys and forgets the previous ones.
		/// </summary>
		internal IReadOnlyList<DetailReversalGroup> CreateGroups(IReadOnlyList<string> visibleWritingSystems)
		{
			_rows.Clear();
			var linked = _sense.ReferringReversalIndexEntries.ToList();
			var current = new HashSet<string>(
				_cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Select(ws => ws.Id));
			var systems = DetailComposer.ApplyVisibleWritingSystems(
				_cache.LanguageProject.AnalysisWritingSystems.ToList(), visibleWritingSystems);

			var groups = new List<DetailReversalGroup>();
			foreach (var ws in systems)
			{
				var index = _cache.LanguageProject.LexDbOA.ReversalIndexesOC
					.FirstOrDefault(ri => ri.WritingSystem == ws.Id);
				if (index == null)
					continue;
				var entries = index.EntriesForSense(linked).ToList();
				if (entries.Count == 0 && !current.Contains(ws.Id))
					continue;

				var rows = new List<DetailReversalRow>();
				foreach (var entry in entries)
				{
					rows.Add(new DetailReversalRow(Bind(index, entry), ChainText(entry, ws.Handle), false,
						OtherWsForms(entry, ws.Handle)));
				}
				rows.Add(new DetailReversalRow(Bind(index, null), string.Empty, true));
				groups.Add(new DetailReversalGroup(ws.Id, ws.Abbreviation, ws.DefaultFontName,
					ws.RightToLeftScript, rows));
			}
			return groups;
		}

		private string Bind(IReversalIndex index, IReversalIndexEntry entry)
		{
			var key = "row" + _nextRowKey++;
			_rows[key] = new RowBinding(index, entry);
			return key;
		}

		// A subentry shows its ancestors' forms before its own, joined by ": ".
		private static string ChainText(IReversalIndexEntry entry, int ws)
		{
			var forms = new List<string>();
			for (var level = entry; level != null; level = level.OwningEntry)
				forms.Insert(0, level.ReversalForm.get_String(ws).Text ?? string.Empty);
			return string.Join(": ", forms);
		}

		private IReadOnlyList<DetailReversalAlternative> OtherWsForms(IReversalIndexEntry entry, int indexWs)
		{
			var result = new List<DetailReversalAlternative>();
			foreach (var ws in WritingSystemServices.GetReversalIndexWritingSystems(_cache, entry.Hvo, false))
			{
				if (ws.Handle == indexWs)
					continue;
				var text = entry.ReversalForm.get_String(ws.Handle).Text;
				if (!string.IsNullOrEmpty(text))
					result.Add(new DetailReversalAlternative(ws.Abbreviation, text, ws.DefaultFontName));
			}
			return result;
		}

		// One row's staged change: its binding, its new chain of forms, and its index's ws.
		private sealed class RowChange
		{
			public RowChange(RowBinding binding, IList<string> forms, int ws)
			{
				Binding = binding;
				Forms = forms;
				Ws = ws;
			}

			public RowBinding Binding { get; }

			public IList<string> Forms { get; }

			public int Ws { get; }
		}

		/// <inheritdoc />
		public bool TryCommitRow(string rowKey, string typedText)
			=> TryCommitRows(new[] { new KeyValuePair<string, string>(rowKey, typedText) });

		/// <inheritdoc />
		public bool TryCommitRows(IReadOnlyList<KeyValuePair<string, string>> edits)
		{
			if (edits == null || !_sense.IsValidObject)
				return false;

			var changes = new List<RowChange>();
			foreach (var edit in edits)
			{
				RowBinding binding;
				if (string.IsNullOrEmpty(edit.Key) || !_rows.TryGetValue(edit.Key, out binding))
					continue;
				var ws = _cache.ServiceLocator.WritingSystemManager.GetWsFromStr(binding.Index.WritingSystem);
				if (ws <= 0)
					continue;
				if (binding.Entry != null && !binding.Entry.IsValidObject)
					binding.Entry = null;
				var forms = SplitForms(edit.Value);
				if (binding.Entry == null ? forms.Count == 0 : ChainMatches(binding.Entry, forms, ws))
					continue;
				changes.Add(new RowChange(binding, forms, ws));
			}
			if (changes.Count == 0)
				return false;

			var previous = changes.Select(change => change.Binding.Entry).ToList();
			try
			{
				return StageOnHost(() =>
				{
					// Every row takes its new entry before any entry is let go, so an entry
					// one row gives up and another takes over is never deleted in between.
					var released = new List<IReversalIndexEntry>();
					foreach (var change in changes)
					{
						IReversalIndexEntry target = null;
						if (change.Forms.Count > 0)
						{
							target = FindOrCreateEntry(change.Binding.Index, change.Forms, change.Ws);
							if (!target.SensesRS.Contains(_sense))
								target.SensesRS.Add(_sense);
						}
						if (change.Binding.Entry != null && change.Binding.Entry != target)
							released.Add(change.Binding.Entry);
						change.Binding.Entry = target;
					}

					var stillWanted = new HashSet<IReversalIndexEntry>(
						_rows.Values.Select(binding => binding.Entry).Where(entry => entry != null));
					foreach (var entry in released.Distinct())
					{
						if (entry.IsValidObject && !stillWanted.Contains(entry))
							Unlink(entry);
					}
					return true;
				});
			}
			catch (Exception e)
			{
				// The write rolled back or never ran, so the rows keep the entries they showed.
				for (var i = 0; i < changes.Count; i++)
					changes[i].Binding.Entry = previous[i];
				Logger.WriteError(e);
				return false;
			}
		}

		/// <inheritdoc />
		public string IssueAddRowKey(string rowKey)
		{
			RowBinding binding;
			if (string.IsNullOrEmpty(rowKey) || !_rows.TryGetValue(rowKey, out binding))
				return null;
			return Bind(binding.Index, null);
		}

		/// <inheritdoc />
		public Guid? TryResolveMainEntryGuid(string rowKey)
		{
			RowBinding binding;
			if (!_sense.IsValidObject || string.IsNullOrEmpty(rowKey) || !_rows.TryGetValue(rowKey, out binding))
				return null;
			var entry = binding.Entry;
			if (entry == null || !entry.IsValidObject)
				return null;
			return entry.MainEntry.Guid;
		}

		/// <summary>
		/// The forms of an entry chain, top level first: the text split on colons, each part
		/// trimmed and decomposed (NFD, as stored forms are), and empty parts dropped (LT-4665).
		/// </summary>
		internal static IList<string> SplitForms(string text)
		{
			return (text ?? string.Empty)
				.Split(new[] { ':' }, StringSplitOptions.RemoveEmptyEntries)
				.Select(part => part.Trim())
				.Where(part => part.Length > 0)
				.Select(Decomposed)
				.ToList();
		}

		// Typed text arrives precomposed while stored forms are decomposed, so both sides of a
		// comparison go through NFD first.
		private static string Decomposed(string text)
			=> CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFD).Normalize(text ?? string.Empty);

		private static string StoredForm(IReversalIndexEntry entry, int ws)
			=> Decomposed(entry.ReversalForm.get_String(ws).Text);

		// True when the entry and its ancestors, top level first, are exactly the given forms.
		private static bool ChainMatches(IReversalIndexEntry entry, IList<string> forms, int ws)
		{
			var level = entry;
			for (var i = forms.Count - 1; i >= 0; i--)
			{
				if (level == null || StoredForm(level, ws) != forms[i])
					return false;
				level = level.OwningEntry;
			}
			return level == null;
		}

		// Reuses the deepest existing entry matching a prefix of the chain and creates the
		// levels below it. An existing entry is never renamed.
		private IReversalIndexEntry FindOrCreateEntry(IReversalIndex index, IList<string> forms, int ws)
		{
			IReversalIndexEntry deepest = null;
			var depth = 0;
			FindDeepest(index.EntriesOC, forms, 0, ws, ref deepest, ref depth);
			if (depth == forms.Count)
				return deepest;

			var factory = _cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			var owner = deepest;
			for (var level = depth; level < forms.Count; level++)
			{
				var created = factory.Create();
				if (owner == null)
					index.EntriesOC.Add(created);
				else
					owner.SubentriesOS.Add(created);
				created.ReversalForm.set_String(ws, forms[level]);
				owner = created;
			}
			return owner;
		}

		// Depth-first, so a chain is found under whichever same-form homograph has it. The
		// first entry to reach a new depth is kept; a full match ends the search.
		private static void FindDeepest(IEnumerable<IReversalIndexEntry> candidates, IList<string> forms,
			int level, int ws, ref IReversalIndexEntry deepest, ref int depth)
		{
			foreach (var candidate in candidates)
			{
				if (StoredForm(candidate, ws) != forms[level])
					continue;
				if (level + 1 > depth)
				{
					deepest = candidate;
					depth = level + 1;
				}
				if (depth == forms.Count)
					return;
				if (level + 1 < forms.Count)
				{
					FindDeepest(candidate.SubentriesOS, forms, level + 1, ws, ref deepest, ref depth);
					if (depth == forms.Count)
						return;
				}
			}
		}

		// An entry left with no senses and no subentries is deleted, and so is each ancestor the
		// deletion leaves with neither.
		private void Unlink(IReversalIndexEntry entry)
		{
			entry.SensesRS.Remove(_sense);
			var level = entry;
			while (level != null && level.SensesRS.Count == 0 && level.SubentriesOS.Count == 0)
			{
				var parent = level.OwningEntry;
				level.Delete();
				level = parent;
			}
		}

		// A host that is not the fenced detail context (a test fake) applies the write directly.
		private bool StageOnHost(Func<bool> setter)
		{
			var fenced = _host as DetailEditContextBase;
			return fenced != null ? fenced.Stage(setter, _fieldLabel) : setter();
		}

		public bool IsOpen => _host != null && _host.IsOpen;

		public bool TrySetText(DetailField field, string ws, string value) => false;

		public bool TrySetRichText(DetailField field, string ws, DetailRichTextValue value) => false;

		public bool TrySetOption(DetailField field, string optionKey) => false;

		public bool TryAddReferenceItem(DetailField field, string optionKey) => false;

		public bool TryRemoveReferenceItem(DetailField field, string optionKey) => false;

		public bool TryMoveReferenceItem(DetailField field, string optionKey, bool forward) => false;

		public bool TryResetReferenceOrder(DetailField field) => false;

		public IReadOnlyList<string> Validate() => _host?.Validate() ?? Array.Empty<string>();

		public void Commit() => _host?.Commit();

		public void Cancel() => _host?.Cancel();
	}
}
