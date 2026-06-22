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
		private static readonly string[] DefaultSupportedTools = { "lexiconEdit", "lexiconEditPopup" };

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
