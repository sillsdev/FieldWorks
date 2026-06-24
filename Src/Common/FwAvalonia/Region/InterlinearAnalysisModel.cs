// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.Region
{
	/// <summary>
	/// avalonia-interlinear-editor (task 1.2) — one morpheme column of an interlinear analysis, as an
	/// LCModel-free DTO (morph form, lex-gloss, grammatical-info abbreviation, plus the GUIDs the adapter
	/// maps edits back to). The Avalonia interlinear control binds to these; it never touches LCModel or a
	/// Sandbox (design Decision 1).
	/// </summary>
	public sealed class InterlinearBundle
	{
		public InterlinearBundle(string morph, string gloss, string grammaticalInfo,
			Guid? morphGuid = null, Guid? senseGuid = null, Guid? msaGuid = null, string lexEntry = null)
		{
			Morph = morph ?? string.Empty;
			LexEntry = lexEntry ?? string.Empty;
			Gloss = gloss ?? string.Empty;
			GrammaticalInfo = grammaticalInfo ?? string.Empty;
			MorphGuid = morphGuid;
			SenseGuid = senseGuid;
			MsaGuid = msaGuid;
		}

		/// <summary>The morph form text (vernacular) — the legacy interlinear "Morphemes" line.</summary>
		public string Morph { get; }

		/// <summary>The owning lex-entry headword (vernacular) — the legacy interlinear "Lex. Entries" line
		/// (rendered by LexEntryVc), distinct from the morpheme form: the headword/citation form of the entry
		/// the morph belongs to. Empty when the bundle has no chosen morph/entry.</summary>
		public string LexEntry { get; }

		/// <summary>The lexical gloss text (analysis).</summary>
		public string Gloss { get; }

		/// <summary>The grammatical-info (MSA) abbreviation shown under the gloss.</summary>
		public string GrammaticalInfo { get; }

		/// <summary>Referenced MoForm GUID (or null when the bundle has no chosen morph).</summary>
		public Guid? MorphGuid { get; }

		/// <summary>Referenced LexSense GUID (or null).</summary>
		public Guid? SenseGuid { get; }

		/// <summary>Referenced MoMorphSynAnalysis GUID (or null). The adapter prunes an MSA no surviving
		/// sense uses on commit (write-back parity with the legacy Sandbox).</summary>
		public Guid? MsaGuid { get; }
	}

	/// <summary>
	/// avalonia-interlinear-editor (task 1.2) — one analysis line of a wordform: the wordform form plus its
	/// ordered morpheme bundles. LCModel-free.
	/// </summary>
	public sealed class InterlinearLine
	{
		public InterlinearLine(string wordform, IEnumerable<InterlinearBundle> bundles, Guid? analysisGuid = null)
		{
			Wordform = wordform ?? string.Empty;
			Bundles = (bundles ?? Enumerable.Empty<InterlinearBundle>()).ToList();
			AnalysisGuid = analysisGuid;
		}

		/// <summary>The wordform line text (vernacular).</summary>
		public string Wordform { get; }

		/// <summary>The morpheme bundles in document order (left-to-right).</summary>
		public IReadOnlyList<InterlinearBundle> Bundles { get; }

		/// <summary>The WfiAnalysis GUID this line projects (or null for the bare wordform).</summary>
		public Guid? AnalysisGuid { get; }
	}

	/// <summary>
	/// avalonia-interlinear-editor (task 1.2) — the LCModel-free projection of a <c>WfiWordform</c>'s
	/// analyses the Avalonia interlinear control renders: the wordform plus its analysis lines. The
	/// Morphology plugin builds this (read) and applies per-bundle edits back to the real analysis with the
	/// Sandbox-parity MSA prune (write) inside one fenced UOW. NO Sandbox in the view (design Decision 1).
	/// </summary>
	public sealed class InterlinearAnalysisModel
	{
		public InterlinearAnalysisModel(string wordform, IEnumerable<InterlinearLine> lines, Guid? wordformGuid = null)
		{
			Wordform = wordform ?? string.Empty;
			Lines = (lines ?? Enumerable.Empty<InterlinearLine>()).ToList();
			WordformGuid = wordformGuid;
		}

		/// <summary>The wordform form text (vernacular).</summary>
		public string Wordform { get; }

		/// <summary>The wordform's analysis lines (one per WfiAnalysis); empty for a wordform with no analyses.</summary>
		public IReadOnlyList<InterlinearLine> Lines { get; }

		/// <summary>The WfiWordform GUID this model projects (or null).</summary>
		public Guid? WordformGuid { get; }

		/// <summary>True when the wordform has at least one analysis line with at least one bundle —
		/// the read-only renderer uses this to choose between the interlinear grid and the bare-wordform state.</summary>
		public bool HasAnalysis => Lines.Any(l => l.Bundles.Count > 0);
	}
}
