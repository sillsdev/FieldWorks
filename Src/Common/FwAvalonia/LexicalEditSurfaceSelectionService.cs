// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// The deliberate product behavior of a host under the app-wide UI mode (task 3.9 / manifest
	/// <c>uiModeBehavior</c>). Every host must resolve to one of these — there is no ambiguous
	/// "best effort" routing.
	/// </summary>
	public enum HostUiBehavior
	{
		/// <summary>Legacy UI mode is selected; this host renders the legacy surface.</summary>
		LegacyActive,

		/// <summary>New UI mode and this host has a migrated Avalonia surface.</summary>
		SupportedAvalonia,

		/// <summary>New UI mode but this host is not migrated, so it explicitly falls back to legacy.</summary>
		ExplicitLegacyFallback,

		/// <summary>New UI mode and this host is neither migrated nor has a legacy fallback (reserved).</summary>
		Blocked
	}

	/// <summary>The resolved routing decision for a host: the concrete surface plus why it was chosen.</summary>
	public sealed class SurfaceDecision
	{
		public SurfaceDecision(LexicalEditSurface surface, HostUiBehavior behavior, string reason)
		{
			Surface = surface;
			Behavior = behavior;
			Reason = reason;
		}

		/// <summary>The concrete surface to render.</summary>
		public LexicalEditSurface Surface { get; }

		/// <summary>The deliberate behavior classification behind the surface choice.</summary>
		public HostUiBehavior Behavior { get; }

		/// <summary>Human-readable reason (for diagnostics/manifest evidence, not for control flow).</summary>
		public string Reason { get; }
	}

	/// <summary>
	/// Explicit, central mapping from the app-wide UI mode to per-host behavior (task 3.9). Hosts such as
	/// <c>RecordEditView</c> consume this instead of inferring product routing ad hoc from settings and
	/// <c>PropertyTable</c> state. Pure logic over <see cref="LexicalEditSurfaceResolver"/>, with no
	/// Avalonia dependency, so it is unit-testable without a UI runtime.
	/// </summary>
	public sealed class LexicalEditSurfaceSelectionService
	{
		/// <summary>
		/// Resolves the surface decision for a host from the persisted UI mode and the current tool.
		/// </summary>
		/// <param name="uiMode">Persisted user preference (<c>Legacy</c> or <c>New</c>).</param>
		/// <param name="toolName">The current content-control/tool name.</param>
		/// <param name="overrideEnabled">Optional strong override (PropertyTable/registry).</param>
		public SurfaceDecision Decide(string uiMode, string toolName, bool? overrideEnabled = null)
		{
			var supportsAvalonia = LexicalEditSurfaceResolver.SupportsAvaloniaForTool(toolName);
			var surface = LexicalEditSurfaceResolver.Resolve(overrideEnabled, uiMode, toolName);

			if (surface == LexicalEditSurface.Avalonia)
			{
				return new SurfaceDecision(LexicalEditSurface.Avalonia, HostUiBehavior.SupportedAvalonia,
					$"Avalonia is supported for tool '{toolName}' and the UI mode selects it.");
			}

			// Surface resolved to WinForms. Distinguish "legacy mode" from "new mode, tool not migrated".
			var isNewMode = overrideEnabled == true
				|| (!overrideEnabled.HasValue && string.Equals(uiMode, LexicalEditSurfaceResolver.NewUIMode,
					System.StringComparison.OrdinalIgnoreCase));

			if (isNewMode && !supportsAvalonia)
			{
				return new SurfaceDecision(LexicalEditSurface.WinForms, HostUiBehavior.ExplicitLegacyFallback,
					$"Tool '{toolName}' is not migrated; it explicitly falls back to legacy under the New UI mode.");
			}

			return new SurfaceDecision(LexicalEditSurface.WinForms, HostUiBehavior.LegacyActive,
				"Legacy UI mode is selected.");
		}
	}
}
