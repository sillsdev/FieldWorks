// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FwAvaloniaDialogs;
using SIL.FieldWorks.Common.Controls;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Shared LCModel-aware helpers for the reusable Avalonia entry-search ("go") dialog consumers
	/// (<see cref="LcmLinkMsaDialogLauncher"/>, <see cref="LcmLinkAllomorphDialogLauncher"/>,
	/// <see cref="LcmLinkEntryOrSenseDialogLauncher"/>, <see cref="LcmAddAllomorphDialogLauncher"/>). It factors the
	/// common search wiring (the shared <see cref="EntryGoSearchEngine"/> with the starting entry excluded, the
	/// legacy <c>EntryGoDlg.GetFields</c> vernacular field set, and the entry->row mapping) so
	/// each launcher only
	/// supplies its own title/OK/prompt text, an optional extra result filter, and its on-OK resolution. Mirrors the
	/// search semantics already established by <see cref="LcmMergeEntryDialogLauncher"/>. Internal so the shared
	/// search is unit-testable against a real cache via InternalsVisibleTo("LexTextControlsTests").
	/// </summary>
	internal static class EntryGoLauncherShared
	{
		/// <summary>
		/// The writing system an entry search matches against. A search delegate reads it on
		/// every query, so changing <see cref="Handle"/> applies to the next search.
		/// </summary>
		internal sealed class SearchWritingSystem
		{
			/// <summary>The writing system's handle.</summary>
			public int Handle { get; set; }
		}

		/// <summary>
		/// Builds the search delegate the dialog drives: it reuses the shared <see cref="EntryGoSearchEngine"/> (the
		/// SAME matching the legacy EntryGoDlg uses, cached on the property table when one is supplied) wrapped to
		/// EXCLUDE <paramref name="excludedEntryHvo"/> (the legacy starting entry that cannot be a match) and an
		/// optional caller <paramref name="filter"/> (e.g. LinkAllomorph's "drop entries whose
		/// forms are all
		/// abstract"). Each surviving hvo is mapped to a row carrying the matching list's
		/// per-column values --
		/// headword, lexeme form, and gloss(es), sourced the way the legacy matchingEntries browser columns are.
		/// </summary>
		internal static Func<string, IReadOnlyList<EntryGoSearchResult>> BuildEntrySearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable, int excludedEntryHvo, string engineCacheKey,
			Func<ILexEntry, bool> filter, SearchWritingSystem searchWs = null)
		{
			var engine = GetSearchEngine(cache, mediator, propertyTable, excludedEntryHvo, engineCacheKey);
			var repo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			return query =>
			{
				var fields = GetSearchFields(cache, query ?? string.Empty, SelectedWs(cache, searchWs));
				var hvos = engine.Search(fields);
				var results = new List<EntryGoSearchResult>();
				foreach (var hvo in hvos)
				{
					if (hvo == excludedEntryHvo)
						continue; // belt-and-braces on top of the engine's FilterResults
					if (!repo.TryGetObject(hvo, out var entry))
						continue;
					if (filter != null && !filter(entry))
						continue;
					results.Add(BuildEntryRow(entry));
				}
				return results.OrderBy(r => r.Text, StringComparer.CurrentCulture).ToList();
			};
		}

		/// <summary>
		/// The fields an entry search matches in <paramref name="ws"/>: sense glosses when it
		/// is a current analysis writing system, and citation, lexeme, and alternate forms
		/// otherwise. A writing system that is both analysis and vernacular matches both sets.
		/// </summary>
		internal static IEnumerable<SearchField> GetSearchFields(LcmCache cache, string str, int ws)
		{
			var tssKey = TsStringUtils.MakeString(str, ws);
			var writingSystems = cache.ServiceLocator.WritingSystems;
			var isAnalysis = writingSystems.CurrentAnalysisWritingSystems.Any(w => w.Handle == ws);
			var isVernacular = writingSystems.CurrentVernacularWritingSystems.Any(w => w.Handle == ws);
			if (isVernacular || !isAnalysis)
			{
				yield return new SearchField(LexEntryTags.kflidCitationForm, tssKey);
				yield return new SearchField(LexEntryTags.kflidLexemeForm, tssKey);
				yield return new SearchField(LexEntryTags.kflidAlternateForms, tssKey);
			}
			if (isAnalysis)
				yield return new SearchField(LexSenseTags.kflidGloss, tssKey);
		}

		// The selected writing system, or the default vernacular one when none is selected.
		private static int SelectedWs(LcmCache cache, SearchWritingSystem searchWs)
			=> searchWs?.Handle > 0 ? searchWs.Handle : cache.DefaultVernWs;

		/// <summary>
		/// Builds the mode-aware search the Link-Entry-or-Sense dialog drives: in ENTRY mode it returns the same
		/// entry rows as <see cref="BuildEntrySearch"/>; in SENSE mode it expands each matching entry into one row
		/// per sense (the legacy LinkEntryOrSenseDlg "Specific Sense" path that lists <c>entry.AllSenses</c> with
		/// <c>ChooserNameTS</c>). The starting entry is excluded in both modes. Each sense row carries the sense's
		/// hvo as its id and is flagged <see cref="EntryGoSearchResult.IsSense"/> so the launcher resolves it as a
		/// sense. Internal so it is unit-testable against a real cache.
		/// </summary>
		internal static Func<string, bool, IReadOnlyList<EntryGoSearchResult>> BuildEntryOrSenseSearch(LcmCache cache,
			Mediator mediator, PropertyTable propertyTable, int excludedEntryHvo, string engineCacheKey,
			SearchWritingSystem searchWs = null)
		{
			var engine = GetSearchEngine(cache, mediator, propertyTable, excludedEntryHvo, engineCacheKey);
			var repo = cache.ServiceLocator.GetInstance<ILexEntryRepository>();

			return (query, senseMode) =>
			{
				var fields = GetSearchFields(cache, query ?? string.Empty, SelectedWs(cache, searchWs));
				var hvos = engine.Search(fields);
				var results = new List<EntryGoSearchResult>();
				foreach (var hvo in hvos)
				{
					if (hvo == excludedEntryHvo)
						continue; // belt-and-braces on top of the engine's FilterResults
					if (!repo.TryGetObject(hvo, out var entry))
						continue;
					if (!senseMode)
					{
						results.Add(BuildEntryRow(entry));
						continue;
					}
					// Sense mode: one row per sense (the legacy m_fwcbSenses populated from entry.AllSenses). The
					// row's gloss column shows the SENSE's gloss (via the SubText fallback), not the entry's list.
					var head = HeadwordText(entry);
					var lexemeForm = LexemeFormText(entry);
					foreach (var sense in entry.AllSenses)
					{
						results.Add(new EntryGoSearchResult(
							sense.Hvo.ToString(CultureInfo.InvariantCulture),
							head,
							isSense: true,
							subText: SenseGloss(sense),
							description: $"{head} : {SenseGloss(sense)}")
						{
							LexemeForm = lexemeForm
						});
					}
				}
				return senseMode
					? results // already grouped by entry (headword) then sense order
					: results.OrderBy(r => r.Text, StringComparer.CurrentCulture).ToList();
			};
		}

		/// <summary>
		/// Builds the search-box spec for a search that follows <paramref name="searchWs"/>. It
		/// offers every current analysis and vernacular writing system once, sorted by display
		/// name and each with its own font and direction, and selects the one
		/// <paramref name="searchWs"/> holds. When it holds none, or one that is not current, it
		/// is pointed at the default vernacular writing system, which is selected instead.
		/// Selecting an option points <paramref name="searchWs"/> at it, and focusing the
		/// search box activates the selected writing system's keyboard. The spec's own typography
		/// is the default vernacular writing system's.
		/// </summary>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="searchWs"/> is null.
		/// </exception>
		internal static EntryGoSearchFieldSpec BuildSearchFieldSpec(LcmCache cache,
			SearchWritingSystem searchWs)
		{
			if (searchWs == null)
				throw new ArgumentNullException(nameof(searchWs));
			var writingSystems = cache.ServiceLocator.WritingSystems;
			var available = writingSystems.CurrentAnalysisWritingSystems
				.Concat(writingSystems.CurrentVernacularWritingSystems)
				.GroupBy(ws => ws.Id)
				.Select(group => group.First())
				.OrderBy(ws => ws.DisplayLabel, StringComparer.CurrentCulture)
				.ToList();
			if (available.All(ws => ws.Handle != searchWs.Handle))
				searchWs.Handle = cache.DefaultVernWs;

			var spec = FieldSpecFor(writingSystems.DefaultVernacularWritingSystem);
			spec.WritingSystems = available.Select(ws => new EntryGoWritingSystemOption
			{
				Id = ws.Id,
				DisplayName = ws.DisplayLabel,
				Typography = FieldSpecFor(ws)
			}).ToList();
			spec.SelectedWritingSystemId = available.FirstOrDefault(ws => ws.Handle == searchWs.Handle)?.Id;
			spec.WritingSystemChanged = id =>
			{
				var chosen = available.FirstOrDefault(ws => ws.Id == id);
				if (chosen != null)
					searchWs.Handle = chosen.Handle;
			};
			spec.Focused = () => ActivateKeyboardForWritingSystem(cache,
				available.FirstOrDefault(ws => ws.Handle == searchWs.Handle)?.Id);
			return spec;
		}

		// The per-ws typography spec (font family + right-to-left) shared by the search box and the matching
		// list's column specs -- the same DefaultFontName derivation the detail view's value rows
		// use.
		private static EntryGoSearchFieldSpec FieldSpecFor(CoreWritingSystemDefinition ws)
		{
			return new EntryGoSearchFieldSpec
			{
				FontFamily = ws.DefaultFontName,
				RightToLeft = ws.RightToLeftScript
			};
		}

		/// <summary>
		/// Builds the matching list's default column spec -- the legacy matchingEntries browser's
		/// default-visible
		/// columns (areaConfiguration.xml: "Headword" ws=best vernoranal and "Glosses" ws=best analorvern; the
		/// other columns are visibility="menu"): a Headword column in the default vernacular ws's typography and a
		/// Glosses column in the default analysis ws's typography, headers from the shared localized strings.
		/// Internal so the derivation is unit-testable against a real cache.
		/// </summary>
		internal static IReadOnlyList<EntryGoResultColumn> BuildDefaultResultColumns(LcmCache cache)
		{
			var vernacular = FieldSpecFor(cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem);
			var analysis = FieldSpecFor(cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem);
			return new[]
			{
				new EntryGoResultColumn
				{
					Header = FwAvaloniaDialogsStrings.EntryGoHeadwordColumnHeader,
					Field = EntryGoResultField.Headword,
					Typography = vernacular
				},
				new EntryGoResultColumn
				{
					Header = FwAvaloniaDialogsStrings.EntryGoGlossesColumnHeader,
					Field = EntryGoResultField.Gloss,
					Typography = analysis
				}
			};
		}

		/// <summary>
		/// Activates the writing system's configured keyboard (Keyman/Windows IME) when the search box gains
		/// focus -- the behavior the legacy vernacular FwTextBox gets from
		/// <c>EditingHelper.SetKeyboardForWs</c>.
		/// Unknown tags fall back to the default keyboard, and every failure is swallowed: keyboard switching must
		/// never take down the dialog.
		/// </summary>
		internal static void ActivateKeyboardForWritingSystem(LcmCache cache, string wsTag)
		{
			try
			{
				foreach (var ws in cache.ServiceLocator.WritingSystems.AllWritingSystems)
				{
					if (ws.Id == wsTag)
					{
						ws.LocalKeyboard?.Activate();
						return;
					}
				}

				SIL.Keyboarding.Keyboard.Controller.ActivateDefaultKeyboard();
			}
			catch (Exception)
			{
				// Keyboard switching must never take down the dialog; legacy swallows comparable failures.
			}
		}

		// The display gloss for a sense row (the best-analysis gloss; fall back to the chooser name's text).
		private static string SenseGloss(ILexSense sense)
		{
			var gloss = FirstText(sense.Cache, sense.Gloss, analysisFirst: true);
			return string.IsNullOrEmpty(gloss) ? sense.ChooserNameTS?.Text ?? string.Empty : gloss;
		}

		/// <summary>Resolves a sense-id (legacy hvo string) back to the live <c>ILexSense</c>, or null.</summary>
		internal static ILexSense ResolveSense(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || cache == null)
				return null;
			if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hvo))
				return null;
			return cache.ServiceLocator.GetInstance<ILexSenseRepository>().TryGetObject(hvo, out var sense)
				? sense
				: null;
		}

		/// <summary>Resolves an entry-id (legacy hvo string) back to the live <c>ILexEntry</c>, or null.</summary>
		internal static ILexEntry ResolveEntry(LcmCache cache, string id)
		{
			if (string.IsNullOrEmpty(id) || cache == null)
				return null;
			if (!int.TryParse(id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var hvo))
				return null;
			return cache.ServiceLocator.GetInstance<ILexEntryRepository>().TryGetObject(hvo, out var entry)
				? entry
				: null;
		}

		// One matching-list row for an entry, carrying the per-column values the legacy matchingEntries browser
		// columns show: the headword, the lexeme form, and the senses' glosses.
		private static EntryGoSearchResult BuildEntryRow(ILexEntry entry)
		{
			return new EntryGoSearchResult(
				entry.Hvo.ToString(CultureInfo.InvariantCulture),
				HeadwordText(entry),
				DescriptionText(entry))
			{
				LexemeForm = LexemeFormText(entry),
				Gloss = GlossesText(entry)
			};
		}

		/// <summary>
		/// The entry's headword in the first current vernacular writing system that has a form,
		/// default first, then the first current analysis one; empty when no writing system has
		/// a form.
		/// </summary>
		internal static string HeadwordText(ILexEntry entry)
		{
			var writingSystems = entry.Cache.ServiceLocator.WritingSystems;
			var candidates = writingSystems.CurrentVernacularWritingSystems
				.Concat(writingSystems.CurrentAnalysisWritingSystems);
			foreach (var ws in candidates)
			{
				var text = entry.HeadWordForWs(ws.Handle)?.Text;
				if (!string.IsNullOrEmpty(text))
					return text;
			}
			return string.Empty;
		}

		// The Lexeme Form column's value (the legacy LexemeFormForFindEntry part: LexemeForm.Form in the best
		// vernacular-or-analysis ws).
		internal static string LexemeFormText(ILexEntry entry)
			=> FirstText(entry.Cache, entry.LexemeFormOA?.Form, analysisFirst: false);

		// The Glosses column's value (the legacy GlossesForFindEntry part: the entry's senses' glosses in the
		// best analysis-or-vernacular ws, comma-separated).
		internal static string GlossesText(ILexEntry entry)
		{
			var glosses = entry.SensesOS
				.Select(sense => FirstText(entry.Cache, sense.Gloss, analysisFirst: true))
				.Where(gloss => !string.IsNullOrEmpty(gloss));
			return string.Join(", ", glosses);
		}

		// A short gloss preview for the description pane: the best-analysis gloss of the first sense, if any.
		private static string DescriptionText(ILexEntry entry)
		{
			var firstSense = entry.SensesOS.FirstOrDefault();
			var gloss = firstSense == null ? string.Empty : FirstText(entry.Cache, firstSense.Gloss, analysisFirst: true);
			return string.IsNullOrEmpty(gloss) ? HeadwordText(entry) : $"{HeadwordText(entry)} : {gloss}";
		}

		/// <summary>
		/// The first non-empty value of <paramref name="multi"/> across the current analysis and
		/// vernacular writing systems: analysis first when <paramref name="analysisFirst"/> is
		/// true, vernacular first otherwise. Empty when none has a value or
		/// <paramref name="multi"/> is null; unlike LCM's best-alternative properties it never
		/// returns a placeholder.
		/// </summary>
		internal static string FirstText(LcmCache cache, IMultiAccessorBase multi, bool analysisFirst)
		{
			if (multi == null)
				return string.Empty;
			var writingSystems = cache.ServiceLocator.WritingSystems;
			var analysis = writingSystems.CurrentAnalysisWritingSystems;
			var vernacular = writingSystems.CurrentVernacularWritingSystems;
			var candidates = analysisFirst ? analysis.Concat(vernacular) : vernacular.Concat(analysis);
			foreach (var ws in candidates)
			{
				var text = multi.StringOrNull(ws.Handle)?.Text;
				if (!string.IsNullOrEmpty(text))
					return text;
			}
			return string.Empty;
		}

		// Gets (creating + caching on the property table, like the legacy dialog) a search engine that uses the
		// legacy EntryGoDlg matching but excludes the starting entry hvo. When no property table is supplied
		// (tests) a fresh engine is built.
		private static ExcludingEntryGoSearchEngine GetSearchEngine(LcmCache cache, Mediator mediator,
			PropertyTable propertyTable, int excludedEntryHvo, string engineCacheKey)
		{
			ExcludingEntryGoSearchEngine engine;
			if (propertyTable != null)
			{
				engine = (ExcludingEntryGoSearchEngine)SearchEngine.Get(mediator, propertyTable,
					engineCacheKey, () => new ExcludingEntryGoSearchEngine(cache));
			}
			else
			{
				engine = new ExcludingEntryGoSearchEngine(cache);
			}
			engine.ExcludedEntryHvo = excludedEntryHvo;
			return engine;
		}

		/// <summary>
		/// The legacy EntryGoDlg matching with the starting entry filtered out of the results (parity with the
		/// EntryGoDlg children excluding <c>m_startingEntry</c>). A generalization of
		/// <c>LcmMergeEntryDialogLauncher.MergeEntrySearchEngine</c> so every entry-go consumer shares the identical
		/// search semantics.
		/// </summary>
		private sealed class ExcludingEntryGoSearchEngine : EntryGoSearchEngine
		{
			public int ExcludedEntryHvo { private get; set; }

			public ExcludingEntryGoSearchEngine(LcmCache cache) : base(cache)
			{
			}

			protected override IEnumerable<int> FilterResults(IEnumerable<int> results)
			{
				if (results == null)
					return null;
				return ExcludedEntryHvo == 0 ? results : results.Where(hvo => hvo != ExcludedEntryHvo);
			}
		}
	}
}
