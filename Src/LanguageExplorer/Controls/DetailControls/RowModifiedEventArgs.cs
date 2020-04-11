// Copyright (c) 2008-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This is used for a slice to ask the data tree to display a context menu.
	/// </summary>
	internal delegate void RowModifiedEventHandler(object sender, RowModifiedEventArgs e);

	internal sealed class RowModifiedEventArgs : EventArgs
	{
		internal RowModifiedEventArgs(IConstChartRow row)
		{
			Row = row;
		}

		internal IConstChartRow Row { get; }
	}
}