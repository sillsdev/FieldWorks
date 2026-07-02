// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// Which implementation renders the Lexical Edit surface. WinForms is the safe default;
	/// Avalonia is the proof-of-concept path selected only when the feature flag is enabled.
	/// </summary>
	public enum LexicalEditSurface
	{
		/// <summary>The existing WinForms DataTree/Slice surface (default).</summary>
		WinForms,

		/// <summary>The Avalonia surface (flag-gated).</summary>
		Avalonia
	}

	/// <summary>
	/// Pure-logic resolver for the two-adapter feature flag that selects the active lexical-edit
	/// surface. Default is WinForms; Avalonia is selected by a persisted `UIMode = New` preference
	/// or by an explicit override used in tests.
	/// This type has no Avalonia dependency so it can be unit tested without a UI runtime.
	/// </summary>
	public static class LexicalEditSurfaceResolver
	{
		// Stage 2.2: tool support now comes from an app-wide registry rather than a hardcoded array. The
		// default registry is seeded with the tools that ship with Avalonia support, so the static
		// convenience methods below keep their exact original behavior.
		private static readonly LexicalEditSurfaceRegistry DefaultRegistry =
			LexicalEditSurfaceRegistry.CreateDefault();

		// Tools whose BROWSE/table surface can render on the Avalonia owned table (Stage 3 product
		// wiring). Kept separate from the edit-surface registry because the browse table is a distinct
		// surface; both are gated by the same `UIMode = New` preference.
		// - "lexiconEdit": the Lexicon Edit tool's left Entries pane (the primary requested target;
		//   its currentContentControl is the tool value "lexiconEdit", same as the right edit pane).
		// - "lexiconBrowse": the standalone Lexicon > Browse tool.
		// §20.2: the BROWSE/list surface is class-agnostic (ClerkBrowseRowSource keys on the clerk's columns,
		// not LexEntry), and the bulk-delete orphan sweep is now gated to lexicon list-items (§20.2.3), so
		// flat-list non-lexicon tools opt their LIST pane into the Avalonia table here. (Their EDIT detail
		// stays WinForms until registered in the separate LexicalEditSurfaceRegistry — §20.3.) Lists tools are
		// NOT here: they navigate via a hierarchical tree bar, which needs the §20.2.6 owned tree first.
		// PHASE-1: the Avalonia browse TABLE is a FOLLOW-UP surface and is INERT in the base PR — no tool is
		// registered, so every list pane falls back to the legacy WinForms BrowseViewer even under UIMode=New.
		// The table's view-layer code (LexicalBrowseView etc.) ships in base but stays dormant. The browse
		// follow-up PR ACTIVATES it by moving its tool name(s) from Phase1FollowUpBrowseTools into this list.
		// Verified by InertFollowUpSurfacesFallBackToLegacy in the resolver tests. The gate this array feeds is
		// consulted at Src/xWorks/RecordBrowseView.cs:TryActivateAvaloniaBrowse (via ResolveBrowse below).
		private static readonly string[] SupportedAvaloniaBrowseToolNames =
		{
		};

		// The browse tools the table follow-up PR will re-activate (the one-line "flip" — move into the array above).
		public static readonly string[] Phase1FollowUpBrowseTools =
		{
			"lexiconEdit", "lexiconBrowse",
			"notebookEdit", "notebookBrowse",       // §20.2.1 Notebook (RnGenericRec flat record list)
			"Analyses", "toolBulkEditWordforms",    // §20.2.4 Words (wordform analyses + bulk edit)
			"featureTypesAdvancedEdit", "reversalToolReversalIndexPOS" // §20.2.7 Grammar flat-table editors
		};

		/// <summary>Property/app-setting key storing the preferred lexical-edit UI mode.</summary>
		public const string UIModePropertyName = "UIMode";
		public const string LegacyUIMode = "Legacy";
		public const string NewUIMode = "New";

		/// <summary>
		/// Resolves the surface to use. Resolution order: an explicit <paramref name="overrideEnabled"/>
		/// wins; otherwise the persisted <paramref name="uiMode"/> user preference is used.
		/// </summary>
		/// <param name="overrideEnabled">Optional strong override (PropertyTable/registry).</param>
		/// <param name="uiMode">Persisted user preference (`Legacy` or `New`).</param>
		public static LexicalEditSurface Resolve(
			bool? overrideEnabled = null,
			string uiMode = null,
			string currentToolName = null)
			=> Resolve(DefaultRegistry, overrideEnabled, uiMode, currentToolName);

		/// <summary>
		/// Registry-aware resolution (Stage 2.2): tool support comes from <paramref name="registry"/> rather
		/// than a hardcoded list, so a host can register additional tools without editing this type. A null
		/// registry uses the shipped default. Same precedence as the static overload: tool gate first, then
		/// explicit override, then the persisted UI-mode preference.
		/// </summary>
		public static LexicalEditSurface Resolve(
			LexicalEditSurfaceRegistry registry,
			bool? overrideEnabled = null,
			string uiMode = null,
			string currentToolName = null)
		{
			registry = registry ?? DefaultRegistry;
			return ResolveFromPreference(registry.SupportsAvalonia(currentToolName), overrideEnabled, uiMode);
		}

		// The single surface-precedence implementation shared by the edit (Resolve) and browse
		// (ResolveBrowse) gates: a closed tool gate is always WinForms; otherwise an explicit override
		// wins; otherwise the persisted UI-mode preference decides.
		private static LexicalEditSurface ResolveFromPreference(bool toolGateOpen, bool? overrideEnabled, string uiMode)
		{
			if (!toolGateOpen)
				return LexicalEditSurface.WinForms;

			if (overrideEnabled.HasValue)
				return overrideEnabled.Value ? LexicalEditSurface.Avalonia : LexicalEditSurface.WinForms;

			return string.Equals(uiMode, NewUIMode, StringComparison.OrdinalIgnoreCase)
				? LexicalEditSurface.Avalonia
				: LexicalEditSurface.WinForms;
		}

		public static string ToUIModeValue(LexicalEditSurface surface)
			=> surface == LexicalEditSurface.Avalonia ? NewUIMode : LegacyUIMode;

		public static bool SupportsAvaloniaForTool(string currentToolName)
			=> DefaultRegistry.SupportsAvalonia(currentToolName);

		/// <summary>
		/// Resolves the surface for a BROWSE/table tool (Stage 3). Unlike <see cref="SupportsAvaloniaForTool"/>
		/// a blank/unknown tool does NOT opt in — the Avalonia browse table is enabled only for explicitly
		/// listed browse tools, so unrelated browse surfaces keep the legacy <c>BrowseViewer</c>.
		/// </summary>
		public static LexicalEditSurface ResolveBrowse(
			bool? overrideEnabled = null,
			string uiMode = null,
			string currentToolName = null)
			=> ResolveFromPreference(SupportsAvaloniaBrowseForTool(currentToolName), overrideEnabled, uiMode);

		/// <summary>Whether the named browse/table tool is approved for the Avalonia owned table.</summary>
		public static bool SupportsAvaloniaBrowseForTool(string currentToolName)
		{
			if (string.IsNullOrWhiteSpace(currentToolName))
				return false;

			foreach (var toolName in SupportedAvaloniaBrowseToolNames)
			{
				if (string.Equals(toolName, currentToolName, StringComparison.OrdinalIgnoreCase))
					return true;
			}

			return false;
		}
	}
}
