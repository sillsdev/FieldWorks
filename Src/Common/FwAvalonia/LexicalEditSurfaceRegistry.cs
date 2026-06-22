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
		private static readonly string[] DefaultSupportedTools =
		{
			"lexiconEdit", "lexiconEditPopup",
			"notebookEdit", // §20.3.2 Notebook RnGenericRec (layout resolved via layoutChoiceField="Type")
			"posEdit",      // §20.3.3 Grammar PartOfSpeech (MSA/feature launchers already plugin-claimed)
			"Analyses"      // avalonia-interlinear-editor (W-4/W-5): Words Analyses wordform interlinear editor
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
