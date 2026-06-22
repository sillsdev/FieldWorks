// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.FieldWorks.Common.FwAvalonia.Seams
{
	/// <summary>Which framework renders a lexical-edit surface.</summary>
	public enum LexicalEditSurfaceKind
	{
		/// <summary>The legacy WinForms DataTree/Slice surface.</summary>
		Legacy,

		/// <summary>The Avalonia surface.</summary>
		Avalonia
	}

	/// <summary>
	/// The active-host contract for a migrated region (task 3.10): the visible Avalonia path SHALL NOT
	/// instantiate or drive hidden legacy <c>DataTree</c>/menu infrastructure, except through an
	/// explicitly approved baseline adapter used only for comparison or fallback. This type makes the
	/// rule data so a host can ask "may I drive the legacy DataTree right now?" and an audit test can
	/// assert the answer. Adapter ids are the manifest's <c>allowedAdapters</c> entries.
	/// </summary>
	public sealed class ActiveHostContract
	{
		private readonly HashSet<string> _allowedBaselineAdapters;

		public ActiveHostContract(LexicalEditSurfaceKind activeSurface, IEnumerable<string> allowedBaselineAdapters = null)
		{
			ActiveSurface = activeSurface;
			_allowedBaselineAdapters = new HashSet<string>(
				allowedBaselineAdapters ?? Enumerable.Empty<string>(), StringComparer.Ordinal);
		}

		/// <summary>The surface that is currently visible/active.</summary>
		public LexicalEditSurfaceKind ActiveSurface { get; }

		/// <summary>Baseline-only adapter ids that are permitted to touch legacy infrastructure even when Avalonia is active.</summary>
		public IReadOnlyCollection<string> AllowedBaselineAdapters => _allowedBaselineAdapters;

		/// <summary>
		/// Whether legacy <c>DataTree</c> initialization/driving is permitted in the current state. Always
		/// true when the legacy surface is active; when Avalonia is active it is permitted only for an
		/// approved baseline adapter id.
		/// </summary>
		public bool PermitsLegacyDataTreeDrive(string adapterId = null)
		{
			if (ActiveSurface == LexicalEditSurfaceKind.Legacy)
				return true;

			return adapterId != null && _allowedBaselineAdapters.Contains(adapterId);
		}

		/// <summary>Throws if legacy <c>DataTree</c> driving is not permitted in the current state.</summary>
		public void AssertLegacyDataTreeDriveAllowed(string adapterId = null)
		{
			if (!PermitsLegacyDataTreeDrive(adapterId))
			{
				throw new InvalidOperationException(
					$"Active-host contract violation: the Avalonia surface is active and may not drive the legacy " +
					$"DataTree (adapter id '{adapterId ?? "<none>"}' is not an approved baseline adapter).");
			}
		}

		/// <summary>A contract for a legacy-active host (everything permitted).</summary>
		public static ActiveHostContract ForLegacy() => new ActiveHostContract(LexicalEditSurfaceKind.Legacy);

		/// <summary>A contract for an Avalonia-active host with the given approved baseline adapters (none by default).</summary>
		public static ActiveHostContract ForAvalonia(params string[] allowedBaselineAdapters)
			=> new ActiveHostContract(LexicalEditSurfaceKind.Avalonia, allowedBaselineAdapters);
	}
}
