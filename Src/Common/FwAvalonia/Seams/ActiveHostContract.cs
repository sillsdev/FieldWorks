// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.Common.FwAvalonia;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>
	/// The active-host contract for a migrated detail view: the visible Avalonia path SHALL NOT
	/// instantiate or drive hidden legacy <c>DataTree</c>/menu infrastructure, except through an
	/// explicitly approved baseline adapter used only for comparison or fallback. This type makes the
	/// rule data so a host can ask "may I drive the legacy DataTree right now?" and an audit test can
	/// assert the answer. Adapter ids are the manifest's <c>allowedAdapters</c> entries.
	/// </summary>
	public sealed class ActiveHostContract
	{
		private readonly HashSet<string> _allowedBaselineAdapters;

		public ActiveHostContract(UIFramework activeFramework, IEnumerable<string> allowedBaselineAdapters = null)
		{
			ActiveUIFramework = activeFramework;
			_allowedBaselineAdapters = new HashSet<string>(
				allowedBaselineAdapters ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
		}

		/// <summary>The UI framework that is currently visible/active.</summary>
		public UIFramework ActiveUIFramework { get; }

		/// <summary>Baseline-only adapter ids that are permitted to touch legacy infrastructure even when Avalonia is active.</summary>
		public IReadOnlyCollection<string> AllowedBaselineAdapters => _allowedBaselineAdapters;

		/// <summary>
		/// Whether legacy <c>DataTree</c> initialization/driving is permitted in the current state. Always
		/// true when the Legacy framework is active; when Avalonia is active it is permitted only for an
		/// approved baseline adapter id.
		/// </summary>
		public bool PermitsLegacyDataTreeDrive(string adapterId = null)
		{
			if (ActiveUIFramework == UIFramework.Legacy)
				return true;

			return adapterId != null && _allowedBaselineAdapters.Contains(adapterId);
		}

		/// <summary>Throws if legacy <c>DataTree</c> driving is not permitted in the current state.</summary>
		public void AssertLegacyDataTreeDriveAllowed(string adapterId = null)
		{
			if (!PermitsLegacyDataTreeDrive(adapterId))
			{
				throw new InvalidOperationException(
					$"Active-host contract violation: the Avalonia framework is active and may not drive the legacy " +
					$"DataTree (adapter id '{adapterId ?? "<none>"}' is not an approved baseline adapter).");
			}
		}

		/// <summary>A contract for a legacy-active host (everything permitted).</summary>
		public static ActiveHostContract ForLegacy() => new ActiveHostContract(UIFramework.Legacy);

		/// <summary>A contract for an Avalonia-active host with the given approved baseline adapters (none by default).</summary>
		public static ActiveHostContract ForAvalonia(params string[] allowedBaselineAdapters)
			=> new ActiveHostContract(UIFramework.Avalonia, allowedBaselineAdapters);
	}
}
