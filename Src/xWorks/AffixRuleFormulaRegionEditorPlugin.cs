// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using Avalonia.Controls;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.LCModel;
using SIL.Reporting;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-rule-formula-editor (task 2.5, read-only) — the Avalonia compound / affix-process rule
	/// editor: claims the legacy <c>SIL.FieldWorks.XWorks.MorphologyEditor.AffixRuleFormulaSlice</c> layout
	/// identity and renders the <c>IMoAffixProcess</c>'s input columns ⇒ output mappings via
	/// <see cref="RuleFormulaProjector.ProjectCompoundRule"/>.
	///
	/// <para>READ-ONLY this increment (the input/output editing — copy-from-input index references, modify
	/// features, phoneme inserts — is a follow-up); the slice's object is the affix process itself (legacy
	/// <c>AffixRuleFormulaControl</c> edits the <c>IMoAffixProcess</c>). LCModel reads live here.</para>
	/// </summary>
	public sealed class AffixRuleFormulaRegionEditorPlugin : IRegionEditorPlugin
	{
		/// <summary>The legacy slice class this plugin claims (MorphologyParts.xml MoAffixProcess-Detail-RuleFormula).</summary>
		public const string AffixRuleFormulaSliceClassName =
			"SIL.FieldWorks.XWorks.MorphologyEditor.AffixRuleFormulaSlice";

		public string LegacyClassName => AffixRuleFormulaSliceClassName;

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var rule = context?.Target as IMoAffixProcess;
			if (rule == null)
				return null;
			try
			{
				return new RuleFormulaRegionEditor(RuleFormulaProjector.ProjectCompoundRule(rule));
			}
			catch (Exception e)
			{
				Logger.WriteEvent($"AffixRuleFormulaRegionEditorPlugin: compound editor unavailable for '{rule.Guid}': {e}");
				return null;
			}
		}
	}
}
