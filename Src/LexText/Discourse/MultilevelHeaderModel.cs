// Copyright (c) 2022 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace SIL.FieldWorks.Discourse
{
	public class MultilevelHeaderModel
	{
		public List<List<MultilevelHeaderNode>> Headers { get; } = new List<List<MultilevelHeaderNode>>();

		public MultilevelHeaderModel(ICmPossibility chartTemplateRoot)
		{
			// If the chart is null or has no subpossibilities, it has no columns (it cannot be its own column)
			if (chartTemplateRoot == null || chartTemplateRoot.SubPossibilitiesOS.Count == 0)
			{
				return;
			}

			// make a row of top-level column groups (could be columns, if there are only two levels of hierarchy)
			Headers.Add(new List<MultilevelHeaderNode>(chartTemplateRoot.SubPossibilitiesOS.Count));
			AddSubpossibilities(Headers[0], new MultilevelHeaderNode(chartTemplateRoot, 0, true), true);

			// Make subsequent rows of column groups and columns
			while (Headers.Last().Any(n => n.Item?.SubPossibilitiesOS.Any() ?? false))
			{
				Headers.Add(new List<MultilevelHeaderNode>());
				foreach (var node in Headers[Headers.Count - 2])
				{
					AddSubpossibilities(Headers.Last(), node);
				}
			}
		}

		internal static void AddSubpossibilities(List<MultilevelHeaderNode> row, MultilevelHeaderNode colGroup, bool treatAllAsGroups = false)
		{
			// if no Item, or Item is a leaf, add a placeholder
			if(colGroup.Item == null || !colGroup.Item.SubPossibilitiesOS.Any())
			{
				row.Add(new MultilevelHeaderNode(null, 1, colGroup.IsLastInGroup));
				return;
			}

			// If any of Item's SubPossibilities have their own SubPossibilities, all of Item's SubPossibilities are groups
			treatAllAsGroups = treatAllAsGroups || colGroup.Item.SubPossibilitiesOS.Any(sp => sp.SubPossibilitiesOS.Any());

			// Add subpossibilities to the row
			row.AddRange(colGroup.Item.SubPossibilitiesOS.Select((item, i) => new MultilevelHeaderNode(item,
				// Count item's leaf subpossibilities, or count item as its own leaf node
				item.SubPossibilitiesOS.Any() ? item.ReallyReallyAllPossibilities.Count(sp => !sp.SubPossibilitiesOS.Any()) : 1,
				treatAllAsGroups || i + 1 == colGroup.Item.SubPossibilitiesOS.Count)));
		}
	}

	public struct MultilevelHeaderNode
	{
		public MultilevelHeaderNode(ICmPossibility item, int columnCount, bool isLastInGroup)
		{
			Item = item;
			IsLastInGroup = isLastInGroup;
			ColumnCount = columnCount;
		}

		public ICmPossibility Item { get; }
		/// <summary>
		/// The number of leaf possibilities descended from Item, or 1 if Item is already a leaf
		/// </summary>
		public int ColumnCount { get; }
		/// <remarks>
		/// Direct children of the top-level template node (historically the column group level)
		/// are always considered last in their group
		/// </remarks>
		public bool IsLastInGroup { get; }

		public ITsString Label => Item?.Name.BestAnalysisAlternative;
	}
}
