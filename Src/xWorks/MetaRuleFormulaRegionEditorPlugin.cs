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
	/// avalonia-rule-formula-editor (task 2.3) — the Avalonia metathesis-rule formula editor: claims the
	/// legacy <c>SIL.FieldWorks.XWorks.MorphologyEditor.MetaRuleFormulaSlice</c> layout identity and renders
	/// the <c>IPhMetathesisRule</c>'s four index-partitioned cell groups (left env | left switch ↔ right
	/// switch | right env) via <see cref="RuleFormulaProjector.ProjectMetathesisRule"/>.
	///
	/// <para>Editable when the region supplies its fenced context (via <see cref="MetaRuleFormulaEditSink"/>,
	/// non-middle cells); the slice's object is the rule itself (legacy <c>MetaRuleFormulaControl</c> casts
	/// its object to <c>IPhMetathesisRule</c>). LCModel reads/writes live here (design Decision 1). // PARITY:
	/// the result/swap-row display echo and middle-context editing are deferred.</para>
	/// </summary>
	public sealed class MetaRuleFormulaRegionEditorPlugin : IRegionEditorPlugin
	{
		/// <summary>The legacy slice class this plugin claims (MorphologyParts.xml PhMetathesisRule-Detail-RuleFormula).</summary>
		public const string MetaRuleFormulaSliceClassName =
			"SIL.FieldWorks.XWorks.MorphologyEditor.MetaRuleFormulaSlice";

		public string LegacyClassName => MetaRuleFormulaSliceClassName;

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var rule = context?.Target as IPhMetathesisRule;
			if (rule == null)
				return null;
			try
			{
				var editor = new RuleFormulaRegionEditor(RuleFormulaProjector.ProjectMetathesisRule(rule));
				var host = context.EditContext;
				if (host != null)
				{
					editor.Sink = new MetaRuleFormulaEditSink(rule, context.Cache, host, editor.SetModel);
					editor.Options = RuleFormulaOptions.BuildCellOptions(context.Cache);
				}
				return editor;
			}
			catch (Exception e)
			{
				Logger.WriteEvent($"MetaRuleFormulaRegionEditorPlugin: metathesis editor unavailable for '{rule.Guid}': {e}");
				return null;
			}
		}
	}
}
