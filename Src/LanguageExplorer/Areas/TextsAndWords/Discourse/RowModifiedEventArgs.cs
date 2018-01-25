// Copyright (c) 2008-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer.Areas.TextsAndWords.Discourse
{
	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	public delegate void RowModifiedEventHandler(object sender, RowModifiedEventArgs e);

	public class RowModifiedEventArgs : EventArgs
	{
		public RowModifiedEventArgs(IConstChartRow row)
		{
			Row = row;
		}

		public IConstChartRow Row { get; }
	}
}