// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FwCoreDlgControls;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class DepClauseMenuItem : DisposableToolStripMenuItem
	{
		public DepClauseMenuItem(string label, ChartLocation srcCell, IConstChartRow[] depClauses)
			: base(label)
		{
			DepClauses = depClauses;
			SrcCell = srcCell;
		}

		public IConstChartRow RowSource => SrcCell.Row;

		public int HvoRow => SrcCell.HvoRow;

		public IConstChartRow[] DepClauses { get; }

		public int Column => SrcCell.ColIndex;

		public ChartLocation SrcCell { get; }

		public ClauseTypes DepType { get; set; }
	}
}