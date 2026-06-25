// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// App-wide registry of which tools support the Avalonia lexical-edit surface (Stage 2.2: generalizes
	/// the previously-hardcoded supported-tool list so new tools opt into the Avalonia surface by
	/// registration rather than by editing <see cref="LexicalEditSurfaceResolver"/>).
	///
	/// Contract (matching the resolver's safety property): a null/blank tool name means "no tool context",
	/// which is NOT a tool gate — it defers to the UIMode/override preference. An **unregistered** tool
	/// never advertises Avalonia support, so an unknown tool can never silently resolve to Avalonia.
	/// </summary>
	public sealed class LexicalEditSurfaceRegistry
	{
		// The tools that shipped supporting the Avalonia surface (the former hardcoded list).
		// §20.3: tools whose record EDIT/detail surface is approved for the Avalonia composer. The class-general
		// composer + 4-key layout resolution (§20.1.4) make these compose; per-tool editor gaps (e.g. Notebook
		// participants/subrecords NB-4/NB-5) degrade to read-only/unsupported rows, never a crash (20.1.3 guard),
		// and are tracked in §20.3. All gated behind UIMode=New (off by default). The many Lists CmPossibility
		// editors register via an area/persistContext predicate (F-4 follow-on), not enumerated here.
		// PHASE-1 BASE (active now): the composed detail-editor tools. These ship ON in the base PR — all still
		// gated behind UIMode=New (off by default), so no visible change to existing users.
		private static readonly string[] DefaultSupportedTools =
		{
			"lexiconEdit", "lexiconEditPopup",
			"notebookEdit", // §20.3.2 Notebook RnGenericRec (layout resolved via layoutChoiceField="Type")
			"posEdit",      // §20.3.3 Grammar PartOfSpeech (MSA/feature launchers already plugin-claimed)
			"Analyses",     // avalonia-interlinear-editor follow-up: Words Analyses morph-bundle editor (flipped on)
			"PhonologicalRuleEdit", // avalonia-rule-formula-editor: regular (editable) + metathesis (editable, non-middle)
			"EnvironmentEdit",      // §3.2: PhEnvironment composes Name/Description (Text) + the validated environment editor — full parity
			"compoundRuleAdvancedEdit", // §2.5: MoEndoCompound/MoExoCompound compose Name/Description + Active + category Choosers editably
			                            // (after the multi-child-part importer fix). // PARITY: MoAffixProcess (affix-process) stays read-only.
			"naturalClassedit",         // §3.3: PhNCSegments (editable Segments phoneme vector) + PhNCFeatures (feature launcher) compose fully editably
			"phonemeEdit",              // §3.1: Basic IPA symbol editor (with derive-on-commit) + Features launcher + editable Codes vector + Name
			"AdhocCoprohibEdit"         // §3.4: leaf co-prohibitions editable (Key chooser + Adjacency + Others vector + Active);
			                            // group Name/Desc/Active editable. // PARITY: nested MoAdhocProhibGr.Members recursive editing read-only.
		};

		// PHASE-1 FOLLOW-UP edit surfaces — now EMPTY: both follow-up edit surfaces (avalonia-interlinear-editor
		// "Analyses" and the avalonia-rule-formula-editor family) have been flipped into DefaultSupportedTools above
		// by their follow-up PRs. The browse/table follow-up is gated separately by
		// LexicalEditSurfaceResolver.Phase1FollowUpBrowseTools. Verified by InertFollowUpSurfacesFallBackToLegacy.
		public static readonly string[] Phase1FollowUpSurfaceTools =
		{
		};

		private readonly HashSet<string> _supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>A registry seeded with the tools that ship with Avalonia support.</summary>
		public static LexicalEditSurfaceRegistry CreateDefault()
		{
			var registry = new LexicalEditSurfaceRegistry();
			foreach (var tool in DefaultSupportedTools)
				registry._supported.Add(tool);
			return registry;
		}

		/// <summary>Opt a tool into the Avalonia surface.</summary>
		public void RegisterSupportedTool(string toolName)
		{
			if (string.IsNullOrWhiteSpace(toolName))
				throw new ArgumentException("A tool name is required.", nameof(toolName));
			_supported.Add(toolName);
		}

		/// <summary>
		/// True when the tool may use the Avalonia surface. Null/blank defers to the preference (not a gate);
		/// an unregistered tool returns false so it can never silently resolve to Avalonia.
		/// </summary>
		public bool SupportsAvalonia(string currentToolName)
		{
			if (string.IsNullOrWhiteSpace(currentToolName))
				return true;
			return _supported.Contains(currentToolName);
		}

		/// <summary>The registered tool names (for diagnostics/inspection).</summary>
		public IReadOnlyCollection<string> SupportedTools => _supported;
	}
}
