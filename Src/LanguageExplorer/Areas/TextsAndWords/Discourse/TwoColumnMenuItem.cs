// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FwCoreDlgControls;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	internal class TwoColumnMenuItem : DisposableToolStripMenuItem
	{
		/// <summary>
		/// Make one that doesn't care about row.
		/// </summary>
		public TwoColumnMenuItem(string label, int colDst, int colSrc)
			: this(label, colDst, colSrc, null)
		{
		}

		/// <summary>
		/// Make one that knows about row.
		/// </summary>
		public TwoColumnMenuItem(string label, int colDst, int colSrc, IConstChartRow row)
			:base(label)
		{
			Destination = colDst;
			Source = colSrc;
			Row = row;
		}

		/// <summary>
		/// The source (other) column.
		/// </summary>
		public int Source { get; }

		/// <summary>
		/// The Destination column (where the action will occur; where the menu appears).
		/// </summary>
		public int Destination { get; }

		/// <summary>
		/// The row in which everything takes place.
		/// </summary>
		public IConstChartRow Row { get; }
	}
}