// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwAvalonia
{
	/// <summary>
	/// The deliberate product behavior of a host under the app-wide UI mode. Every host must resolve
	/// to one of these -- there is no ambiguous
	/// "best effort" routing.
	/// </summary>
	public enum HostUiBehavior
	{
		/// <summary>Legacy UI mode is selected; this host renders with the Legacy framework.</summary>
		LegacyActive,

		/// <summary>New UI mode and this host has a migrated Avalonia implementation.</summary>
		SupportedAvalonia,

		/// <summary>New UI mode but this host is not migrated, so it explicitly falls back to legacy.</summary>
		ExplicitLegacyFallback,

		/// <summary>New UI mode and this host is neither migrated nor has a legacy fallback (reserved).</summary>
		Blocked
	}

	/// <summary>The resolved routing decision for a host: the concrete UI framework plus why it was chosen.</summary>
	public sealed class UIFrameworkDecision
	{
		public UIFrameworkDecision(UIFramework framework, HostUiBehavior behavior, string reason)
		{
			Framework = framework;
			Behavior = behavior;
			Reason = reason;
		}

		/// <summary>The concrete UI framework to render with.</summary>
		public UIFramework Framework { get; }

		/// <summary>The deliberate behavior classification behind the framework choice.</summary>
		public HostUiBehavior Behavior { get; }

		/// <summary>Human-readable reason (for diagnostics/manifest evidence, not for control flow).</summary>
		public string Reason { get; }
	}

	/// <summary>
	/// Explicit, central mapping from the app-wide UI mode to per-host behavior. Hosts such as
	/// <c>RecordEditView</c> consume this instead of inferring product routing ad hoc from settings and
	/// <c>PropertyTable</c> state. Pure logic over <see cref="UIFrameworkResolver"/>, with no
	/// Avalonia dependency, so it is unit-testable without a UI runtime.
	/// </summary>
	public sealed class UIFrameworkSelectionService
	{
		/// <summary>
		/// Resolves the framework decision for a host from the persisted UI mode and the current tool.
		/// </summary>
		/// <param name="uiMode">Persisted user preference (<c>Legacy</c> or <c>New</c>).</param>
		/// <param name="toolName">The current content-control/tool name.</param>
		/// <param name="overrideEnabled">Optional strong override (PropertyTable/registry).</param>
		public UIFrameworkDecision Decide(string uiMode, string toolName, bool? overrideEnabled = null)
		{
			var supportsAvalonia = UIFrameworkResolver.SupportsAvaloniaForTool(toolName);
			var framework = UIFrameworkResolver.Resolve(overrideEnabled, uiMode, toolName);

			if (framework == UIFramework.Avalonia)
			{
				return new UIFrameworkDecision(UIFramework.Avalonia, HostUiBehavior.SupportedAvalonia,
					$"Avalonia is supported for tool '{toolName}' and the UI mode selects it.");
			}

			// Legacy resolved. Ask the resolver what the preference alone selects, so the
			// override-versus-preference precedence has one home.
			var preferenceSelectsAvalonia =
				UIFrameworkResolver.ResolvePreference(overrideEnabled, uiMode) == UIFramework.Avalonia;

			if (preferenceSelectsAvalonia && !supportsAvalonia)
			{
				return new UIFrameworkDecision(UIFramework.Legacy, HostUiBehavior.ExplicitLegacyFallback,
					$"Tool '{toolName}' is not migrated; it explicitly falls back to legacy under the New UI mode.");
			}

			return new UIFrameworkDecision(UIFramework.Legacy, HostUiBehavior.LegacyActive,
				"Legacy UI mode is selected.");
		}
	}
}
