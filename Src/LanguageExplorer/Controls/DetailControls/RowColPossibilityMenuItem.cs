// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls.DetailControls
{
	internal sealed class RowColPossibilityMenuItem : DisposableToolStripMenuItem
	{
		internal int m_hvoPoss;
		internal RowColPossibilityMenuItem(ChartLocation cloc, int hvoPoss)
		{
			SrcCell = cloc;
			m_hvoPoss = hvoPoss;
		}

		internal ChartLocation SrcCell { get; }
	}
}