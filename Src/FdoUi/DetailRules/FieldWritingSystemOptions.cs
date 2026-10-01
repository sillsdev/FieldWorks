// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// Which writing-system sets a multi-writing-system field draws on: the set it shows, the
	/// set it offers on request only, and whether English joins them regardless. Build one
	/// with <see cref="FromLayout"/> or <see cref="FromMagicIds"/>, whichever the caller holds.
	/// </summary>
	public sealed class WritingSystemFieldSpec
	{
		private WritingSystemFieldSpec(int objectHvo, int writingSystems, int optionalWritingSystems,
			bool forceIncludeEnglish)
		{
			ObjectHvo = objectHvo;
			WritingSystems = writingSystems;
			OptionalWritingSystems = optionalWritingSystems;
			ForceIncludeEnglish = forceIncludeEnglish;
		}

		/// <summary>The field's object; some magic sets resolve against it.</summary>
		public int ObjectHvo { get; }

		/// <summary>The magic id of the field's own writing-system set, or 0 for none.</summary>
		public int WritingSystems { get; }

		/// <summary>
		/// The magic id of the set the field offers on request only, or 0 for none. The
		/// Pronunciation form is the only shipped part that carries one.
		/// </summary>
		public int OptionalWritingSystems { get; }

		/// <summary>
		/// Whether English joins the options even when the project has not checked it.
		/// </summary>
		public bool ForceIncludeEnglish { get; }

		/// <summary>
		/// The spec a layout describes: its <c>ws</c> and <c>optionalWs</c> names and its
		/// <c>forceIncludeEnglish</c> flag. A name no magic set answers to resolves to none, so
		/// the field offers nothing rather than the analysis writing systems the writing-system
		/// query falls back to.
		/// </summary>
		public static WritingSystemFieldSpec FromLayout(int objectHvo, string writingSystems,
			string optionalWritingSystems, bool forceIncludeEnglish)
			=> new WritingSystemFieldSpec(objectHvo,
				Resolve(WritingSystemServices.GetMagicWsIdFromName(writingSystems)),
				Resolve(WritingSystemServices.GetMagicWsIdFromName(optionalWritingSystems)),
				forceIncludeEnglish);

		/// <summary>The same spec, for a caller that already holds its magic ids.</summary>
		public static WritingSystemFieldSpec FromMagicIds(int objectHvo, int writingSystems,
			int optionalWritingSystems, bool forceIncludeEnglish)
			=> new WritingSystemFieldSpec(objectHvo, Resolve(writingSystems),
				Resolve(optionalWritingSystems), forceIncludeEnglish);

		// Pronunciation sets resolve through the plural id. Seeding the project's list is the
		// caller's business: these rules read the model and never write to it.
		private static int Resolve(int magicId)
		{
			switch (magicId)
			{
				case WritingSystemServices.kwsPronunciation:
				case WritingSystemServices.kwsFirstPronunciation:
				case WritingSystemServices.kwsPronunciations:
					return WritingSystemServices.kwsPronunciations;
			}
			return magicId;
		}
	}

	/// <summary>
	/// One entry of a field's Writing Systems menu: the writing system, whether the field
	/// shows it now, and whether the user may switch it off.
	/// </summary>
	public sealed class WritingSystemMenuOption
	{
		/// <param name="writingSystem">The writing system this entry offers.</param>
		/// <param name="isChecked">Whether the field shows it now.</param>
		/// <param name="canUncheck">False for the last shown entry.</param>
		public WritingSystemMenuOption(CoreWritingSystemDefinition writingSystem, bool isChecked,
			bool canUncheck)
		{
			WritingSystem = writingSystem ?? throw new ArgumentNullException(nameof(writingSystem));
			IsChecked = isChecked;
			CanUncheck = canUncheck;
		}

		/// <summary>The writing system this entry offers.</summary>
		public CoreWritingSystemDefinition WritingSystem { get; }

		/// <summary>The id the stored selection holds.</summary>
		public string Id => WritingSystem.Id;

		/// <summary>The menu text.</summary>
		public string Label => WritingSystem.DisplayLabel;

		/// <summary>Whether the field shows this writing system now.</summary>
		public bool IsChecked { get; }

		/// <summary>False for the last shown entry, so the set can never be emptied.</summary>
		public bool CanUncheck { get; }
	}

	/// <summary>
	/// Which writing systems a multi-writing-system field offers, which of them it shows, and
	/// which may be switched off. Both detail views answer their Writing Systems menu from
	/// here, so neither can drift from the other.
	/// </summary>
	public static class FieldWritingSystemOptions
	{
		/// <summary>
		/// Every writing system the field can be told to show: its own set, including ones the
		/// project has not checked, then the set it offers on request.
		/// </summary>
		public static IReadOnlyList<CoreWritingSystemDefinition> Options(LcmCache cache,
			WritingSystemFieldSpec spec)
		{
			Require(cache, spec);
			// Deduplicated by ID, not by instance: one writing system must never become two
			// menu entries, whichever query produced it.
			var options = new List<CoreWritingSystemDefinition>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var ws in ListFor(cache, spec.WritingSystems, spec, includeUnchecked: true))
			{
				if (seen.Add(ws.Id))
					options.Add(ws);
			}
			foreach (var ws in ListFor(cache, spec.OptionalWritingSystems, spec, includeUnchecked: true))
			{
				if (seen.Add(ws.Id))
					options.Add(ws);
			}
			return options;
		}

		/// <summary>
		/// What the field shows when nothing usable is stored: its own set, restricted to the
		/// writing systems the project has checked. The set offered on request is absent, since
		/// it is never shown unasked.
		/// </summary>
		public static IReadOnlyList<CoreWritingSystemDefinition> DefaultShown(LcmCache cache,
			WritingSystemFieldSpec spec)
		{
			Require(cache, spec);
			return ListFor(cache, spec.WritingSystems, spec, includeUnchecked: false);
		}

		/// <summary>
		/// The writing systems the field shows: the stored selection, restricted to the ids
		/// that are still options and in the options' order; <see cref="DefaultShown"/> when
		/// none of them is.
		/// </summary>
		/// <param name="storedSelection">The field's persisted selection, or null for
		/// none.</param>
		public static IReadOnlyList<CoreWritingSystemDefinition> Shown(LcmCache cache,
			WritingSystemFieldSpec spec, IReadOnlyList<string> storedSelection)
		{
			var stored = Select(Options(cache, spec), storedSelection);
			return stored.Count > 0 ? stored : DefaultShown(cache, spec);
		}

		/// <summary>
		/// The field's Writing Systems menu: every option in order, each marked with whether
		/// the field shows it and whether it may be switched off.
		/// </summary>
		/// <param name="storedSelection">The field's persisted selection, or null for
		/// none.</param>
		public static IReadOnlyList<WritingSystemMenuOption> Menu(LcmCache cache,
			WritingSystemFieldSpec spec, IReadOnlyList<string> storedSelection)
		{
			// Options once, then the shown set from it: recomputing would query the writing
			// systems a second time.
			var options = Options(cache, spec);
			var stored = Select(options, storedSelection);
			var shown = new HashSet<string>(
				(stored.Count > 0 ? stored : DefaultShown(cache, spec)).Select(ws => ws.Id),
				StringComparer.OrdinalIgnoreCase);
			// The last shown writing system cannot be switched off, so a field is never blank.
			var last = shown.Count == 1;
			return options
				.Select(ws =>
				{
					var isChecked = shown.Contains(ws.Id);
					return new WritingSystemMenuOption(ws, isChecked, !(isChecked && last));
				})
				.ToList();
		}

		// The options the stored selection names, in OPTION order: a re-checked writing system
		// lands at the end of the stored list, and the rows must not reorder on a rebuild.
		private static IReadOnlyList<CoreWritingSystemDefinition> Select(
			IReadOnlyList<CoreWritingSystemDefinition> options, IReadOnlyList<string> stored)
		{
			if (stored == null || stored.Count == 0)
				return Array.Empty<CoreWritingSystemDefinition>();
			var wanted = new HashSet<string>(stored.Where(id => !string.IsNullOrEmpty(id)),
				StringComparer.OrdinalIgnoreCase);
			var result = new List<CoreWritingSystemDefinition>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var ws in options)
			{
				if (wanted.Contains(ws.Id) && seen.Add(ws.Id))
					result.Add(ws);
			}
			return result;
		}

		private static IReadOnlyList<CoreWritingSystemDefinition> ListFor(LcmCache cache, int magicId,
			WritingSystemFieldSpec spec, bool includeUnchecked)
		{
			if (magicId == 0)
				return Array.Empty<CoreWritingSystemDefinition>();
			return WritingSystemServices.GetWritingSystemList(cache, magicId, spec.ObjectHvo,
				spec.ForceIncludeEnglish, includeUnchecked);
		}

		private static void Require(LcmCache cache, WritingSystemFieldSpec spec)
		{
			if (cache == null)
				throw new ArgumentNullException(nameof(cache));
			if (spec == null)
				throw new ArgumentNullException(nameof(spec));
		}
	}
}
