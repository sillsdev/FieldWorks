// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwAvalonia;

namespace FwAvaloniaDialogs
{
	/// <summary>
	/// Avalonia analog of the old WinForms <c>LexicalEditFeatureManagerDlg</c>: call sites read like a plain
	/// <c>MessageBox.Show</c>-style helper (mirrors <see cref="FwMessageBox"/>'s convention) but the dialog renders
	/// in Avalonia (<see cref="LexicalEditFeatureManagerDialogView"/> + <see cref="LexicalEditFeatureManagerDialogViewModel"/>)
	/// hosted in a WinForms-owned modal window via <see cref="AvaloniaDialogHost.ShowModal"/>.
	/// </summary>
	public static class LexicalEditFeatureManagerDialog
	{
		/// <summary>
		/// Shows the dialog modally over <paramref name="owner"/>, seeded from <paramref name="features"/>
		/// (the full catalog, in display order) with everything checked except <paramref name="disabledToolNames"/>.
		/// </summary>
		/// <returns>
		/// The edited disabled-tool-name set on OK, or null on Cancel/close (the caller should keep its
		/// existing set unchanged in that case).
		/// </returns>
		public static IReadOnlyList<string> Show(IWin32Window owner,
			IEnumerable<LexicalEditFeatureDescriptor> features, IEnumerable<string> disabledToolNames)
		{
			var state = new LexicalEditFeatureManagerState { Groups = BuildGroups(features, disabledToolNames) };

			var viewModel = new LexicalEditFeatureManagerDialogViewModel(state);
			var view = new LexicalEditFeatureManagerDialogView { DataContext = viewModel };

			var accepted = AvaloniaDialogHost.ShowModal(owner, view, viewModel,
				FwAvaloniaDialogsStrings.FeatureManagerTitle, 460, 440, resizable: true, minWidth: 380, minHeight: 340);
			if (accepted != true)
				return null;

			return ExtractDisabledToolNames(state.Groups);
		}

		/// <summary>
		/// Builds the dialog's grouped rows from the full <paramref name="features"/> catalog, checked except
		/// for <paramref name="disabledToolNames"/> (matched case-insensitively). Factored out of <see cref="Show"/>
		/// so the catalog/CSV bridging is unit-testable without spinning a real modal window. A disabled-tool
		/// name that doesn't match any catalog entry (e.g. stale from a prior app version, or a renamed/removed
		/// tool) seeds nothing -- it does not create a phantom row, and correspondingly cannot reappear in
		/// <see cref="ExtractDisabledToolNames"/>'s output, so a round trip through this dialog silently drops it.
		/// </summary>
		public static IReadOnlyList<FeatureGroupOption> BuildGroups(
			IEnumerable<LexicalEditFeatureDescriptor> features, IEnumerable<string> disabledToolNames)
		{
			if (features == null) throw new ArgumentNullException(nameof(features));

			var disabled = new HashSet<string>(disabledToolNames ?? Enumerable.Empty<string>(), StringComparer.OrdinalIgnoreCase);
			return features
				.GroupBy(f => f.GroupName)
				.Select(g => new FeatureGroupOption(g.Key,
					g.Select(f => new FeatureOption(f.ToolName, f.DisplayName, f.Description, !disabled.Contains(f.ToolName)))
						.ToList()))
				.ToList();
		}

		/// <summary>
		/// The tool names left unchecked across every group, in group/catalog order (the inverse of the
		/// enabled-state seeding <see cref="BuildGroups"/> does). Factored out of <see cref="Show"/> for the
		/// same reason as <see cref="BuildGroups"/>.
		/// </summary>
		public static IReadOnlyList<string> ExtractDisabledToolNames(IEnumerable<FeatureGroupOption> groups) =>
			(groups ?? Enumerable.Empty<FeatureGroupOption>())
				.SelectMany(g => g.Features)
				.Where(f => !f.Enabled)
				.Select(f => f.ToolName)
				.ToArray();
	}
}
