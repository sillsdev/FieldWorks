// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// A managed, owned snapshot of one browse column's metadata (rendering-cutover F1, owned column
	/// model). Captures the per-column identity the Avalonia table needs — label, a stable field token,
	/// writing system, the legacy <c>transduce</c> target, and editability — into a plain value the table
	/// builds its <see cref="ViewDefinitionModel"/> from, instead of the table fabricating <c>"col{i}"</c>
	/// field tokens off the live viewer. Today the snapshot is read once from the still-live
	/// <see cref="BrowseViewer"/> (the legacy viewer stays underneath in F1); in F2 the same snapshot will
	/// be sourced without a live viewer, so this type is the seam that decouples the table's column model
	/// from the legacy control.
	/// </summary>
	internal sealed class BrowseColumnSpec
	{
		public BrowseColumnSpec(int index, string label, string field, string writingSystem,
			string transduce, bool isEditable)
		{
			Index = index;
			Label = label;
			Field = field;
			WritingSystem = writingSystem;
			Transduce = transduce;
			IsEditable = isEditable;
		}

		public int Index { get; }
		public string Label { get; }
		public string Field { get; }
		public string WritingSystem { get; }
		public string Transduce { get; }
		public bool IsEditable { get; }

		/// <summary>
		/// A stable per-column identity for automation ids and the view-node field token. Lexicon browse
		/// columns are layout/transduce-driven (no plain <c>field</c>), so fall back to the transduce
		/// target, then the positional <c>col{index}</c> — never an empty token (which would collide
		/// across columns in <c>BrowseHeader.{field}</c>/<c>BrowseFilter.{field}</c> automation ids).
		/// </summary>
		public string StableField =>
			!string.IsNullOrEmpty(Field) ? Field
			: !string.IsNullOrEmpty(Transduce) ? Transduce
			: "col" + Index;

		/// <summary>Projects the column into the typed view-definition field node the owned table renders.</summary>
		public ViewNode ToFieldNode() => new ViewNode(
			$"browse/#{Index}", ViewNodeKind.Field, Label ?? ("Column " + (Index + 1)), null,
			StableField, "string", EditorClassification.Known, WritingSystem,
			ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null);

		/// <summary>
		/// Snapshots every column's metadata from the (F1: still-live) browse viewer via its public,
		/// read-only column accessors — touching none of the viewer's edit/filter internals.
		/// </summary>
		public static IReadOnlyList<BrowseColumnSpec> Snapshot(BrowseViewer viewer)
		{
			var specs = new List<BrowseColumnSpec>(viewer.ColumnCount);
			for (var i = 0; i < viewer.ColumnCount; i++)
			{
				viewer.GetColumnEditAttributes(i, out var field, out var ws, out var transduce);
				specs.Add(new BrowseColumnSpec(i, viewer.GetColumnName(i), field, ws, transduce,
					viewer.IsColumnEditable(i)));
			}
			return specs;
		}

		/// <summary>Builds the table's column view-definition from a column snapshot.</summary>
		public static ViewDefinitionModel ToViewDefinition(IReadOnlyList<BrowseColumnSpec> columns)
		{
			var roots = new List<ViewNode>(columns.Count);
			foreach (var column in columns)
				roots.Add(column.ToFieldNode());
			return new ViewDefinitionModel("LexEntry", "browse", "browse", roots, new List<ViewDiagnostic>());
		}
	}
}
