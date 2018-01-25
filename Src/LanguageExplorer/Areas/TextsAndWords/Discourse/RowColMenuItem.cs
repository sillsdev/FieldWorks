// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FwCoreDlgControls;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class RowColMenuItem : DisposableToolStripMenuItem
	{
		/// <summary>
		/// Creates a ToolStripMenuItem for a chart cell carrying the row and column information.
		/// Usually represents a cell that the user clicked.
		/// </summary>
		/// <param name="label"></param>
		/// <param name="cloc">The chart cell location</param>
		public RowColMenuItem(string label, ChartLocation cloc)
			: base(label)
		{
			SrcCell = cloc;
		}

		/// <summary>
		/// The source (other) column index.
		/// </summary>
		public int SrcColIndex => SrcCell.ColIndex;

		/// <summary>
		/// The ChartRow object.
		/// </summary>
		public IConstChartRow SrcRow => SrcCell.Row;

		/// <summary>
		/// The cell that was clicked.
		/// </summary>
		public ChartLocation SrcCell { get; }
	}
}