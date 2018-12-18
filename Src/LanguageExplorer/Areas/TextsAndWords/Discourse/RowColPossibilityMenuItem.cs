// Copyright (c) 2008-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	public class RowColPossibilityMenuItem : DisposableToolStripMenuItem
	{
		internal int m_hvoPoss;
		public RowColPossibilityMenuItem(ChartLocation cloc, int hvoPoss)
		{
			SrcCell = cloc;
			m_hvoPoss = hvoPoss;
		}

		public ChartLocation SrcCell { get; }
	}
}