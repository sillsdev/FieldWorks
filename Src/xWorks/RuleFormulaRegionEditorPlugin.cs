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
	/// avalonia-rule-formula-editor (task 1.4) — the native Avalonia regular-rule formula editor: claims the
	/// legacy <c>SIL.FieldWorks.XWorks.MorphologyEditor.RegRuleFormulaSlice</c> layout identity through the D1
	/// plugin contract and renders the rule's formula grid (<see cref="RuleFormulaRegionEditor"/>) at the
	/// slice's real in-tree position, retiring the §20.1.3 "unsupported" row the rule tools showed under New UI.
	///
	/// <para>The slice's object is the rule's <c>IPhSegRuleRHS</c> (legacy <c>RegRuleFormulaControl.Rhs</c>);
	/// this plugin projects it LCModel-side via <see cref="RuleFormulaProjector"/> and hands the LCModel-free
	/// model to the view (design Decision 1/3). This phase is READ-ONLY — the editable phase (task 2.1+) wires
	/// cell-intent events through the region's fenced edit session.</para>
	/// </summary>
	public sealed class RuleFormulaRegionEditorPlugin : IRegionEditorPlugin
	{
		/// <summary>The legacy slice class this plugin claims (MorphologyParts.xml PhSegRuleRHS-Detail-RuleFormula).</summary>
		public const string RegRuleFormulaSliceClassName =
			"SIL.FieldWorks.XWorks.MorphologyEditor.RegRuleFormulaSlice";

		public string LegacyClassName => RegRuleFormulaSliceClassName;

		public Control BuildControl(RegionEditorBuildContext context)
		{
			var rhs = context?.Target as IPhSegRuleRHS;
			if (rhs == null)
				return null;

			try
			{
				var model = RuleFormulaProjector.ProjectRegularRule(rhs);
				var editor = new RuleFormulaRegionEditor(model);

				// task 2.1: when the region supplies its fenced edit context, make the grid editable —
				// cell gestures route to the LCModel handler, which commits one undo step and re-projects.
				var host = context.EditContext;
				if (host != null)
				{
					editor.Sink = new RuleFormulaEditSink(rhs, context.Cache, host, editor.SetModel);
					editor.Options = RuleFormulaOptions.BuildCellOptions(context.Cache);
				}

				return editor;
			}
			catch (Exception e)
			{
				// Graceful degradation (same policy as the other plugins): a broken rule read degrades to the
				// view's null-factory unsupported row, never the whole pane.
				Logger.WriteEvent($"RuleFormulaRegionEditorPlugin: rule editor unavailable for '{rhs.Guid}': {e}");
				return null;
			}
		}
	}
}
