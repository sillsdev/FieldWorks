// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using FwAvaloniaDialogs;
using SIL.LCModel;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Shared wiring for the inline create-feature / add-value affordances of the inflection-feature editor hosted
	/// inside <see cref="FwMsaGroupBox"/> (Phase-1 §19b Stage 3) — the single place the three MSA-section launchers
	/// (Insert Entry, Add New Sense, MSA Creator) wire <c>MsaGroupBox.CreateNewFeatureRequested</c> /
	/// <c>CreateNewValueRequested</c> to the <see cref="LcmCreateFeatureLauncher"/>, replacing the Stage-2 deferred
	/// no-op. On a created feature/value it feeds the new <see cref="FwFeatureNode"/> back to the box's editor
	/// (<c>AcceptCreatedInflectionFeature</c> / <c>AcceptCreatedInflectionFeatureValue</c>), so the new item appears +
	/// (for a value) becomes the feature's pick. The MSA box's editor edits the INFLECTION feature system, so both
	/// flows target <see cref="FeatureSystemKind.Inflection"/>.
	/// </summary>
	internal static class LcmInflectionFeatureCreateWiring
	{
		/// <summary>Runs the create-feature flow and, on success, adds the new feature to the box's editor.</summary>
		public static void CreateFeature(LcmCache cache, IWin32Window owner, FwMsaGroupBox box)
		{
			if (cache == null || box == null)
				return;
			var node = LcmCreateFeatureLauncher.CreateFeature(cache, FeatureSystemKind.Inflection, owner,
				out var children);
			if (node == null)
				return;
			box.AcceptCreatedInflectionFeature(node, children);
		}

		/// <summary>Runs the add-value flow for the given closed feature and, on success, adds + selects the value.</summary>
		public static void AddValue(LcmCache cache, IWin32Window owner, string closedFeatureId, FwMsaGroupBox box)
		{
			if (cache == null || box == null || string.IsNullOrEmpty(closedFeatureId))
				return;
			var node = LcmCreateFeatureLauncher.AddValue(cache, FeatureSystemKind.Inflection, closedFeatureId, owner);
			if (node == null)
				return;
			box.AcceptCreatedInflectionFeatureValue(closedFeatureId, node);
		}
	}
}
