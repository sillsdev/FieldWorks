// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// App-wide registry of which tools have Avalonia support (the
	/// single supported-tool list -- new tools opt in by registration rather than
	/// by editing <see cref="UIFrameworkResolver"/>).
	///
	/// Contract (matching the resolver's safety property): a null/blank tool name means "no tool context",
	/// which is NOT a tool gate -- it defers to the UIMode/override preference. An
	/// **unregistered** tool
	/// never advertises Avalonia support, so an unknown tool can never silently resolve to Avalonia.
	/// </summary>
	public sealed class UIFrameworkRegistry
	{
		// Sourced from LexiconFeatureCatalog.ToolNames, the single list of tools with working
		// Avalonia support today; all gated behind UIMode=New (off by default), so this list has
		// no effect on existing users.
		private static readonly string[] DefaultSupportedTools = ToArray(LexiconFeatureCatalog.ToolNames);

		private static string[] ToArray(IReadOnlyList<string> source)
		{
			var result = new string[source.Count];
			for (var i = 0; i < source.Count; i++)
				result[i] = source[i];
			return result;
		}

		// FOLLOW-UP tools -- currently INERT. The view-layer code for these ships (it lives in the
		// same FwAvalonia/xWorks assemblies) but the tools are deliberately NOT registered, so the resolver returns
		// "not supported" and they fall back to legacy WinForms even under UIMode=New. Activating a
		// tool means moving its tool name(s) from this list into DefaultSupportedTools above (the one-line
		// "flip"). Verified by InertFollowUpToolsFallBackToLegacy in the resolver tests.
		//   avalonia-interlinear-editor : "Analyses"
		//   avalonia-rule-formula-editor: "PhonologicalRuleEdit","EnvironmentEdit","compoundRuleAdvancedEdit",
		//                                 "naturalClassedit","phonemeEdit","AdhocCoprohibEdit"
		public static readonly string[] Phase1FollowUpTools =
		{
			"Analyses",
			"PhonologicalRuleEdit", "EnvironmentEdit", "compoundRuleAdvancedEdit",
			"naturalClassedit", "phonemeEdit", "AdhocCoprohibEdit"
		};

		private readonly HashSet<string> _supported = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		/// <summary>A registry seeded with the tools that ship with Avalonia support.</summary>
		public static UIFrameworkRegistry CreateDefault()
		{
			var registry = new UIFrameworkRegistry();
			foreach (var tool in DefaultSupportedTools)
				registry._supported.Add(tool);
			return registry;
		}

		/// <summary>Opt a tool into Avalonia support.</summary>
		public void RegisterSupportedTool(string toolName)
		{
			if (string.IsNullOrWhiteSpace(toolName))
				throw new ArgumentException("A tool name is required.", nameof(toolName));
			_supported.Add(toolName);
		}

		/// <summary>
		/// True when the tool may render in Avalonia. Null/blank defers to the preference (not a gate);
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
