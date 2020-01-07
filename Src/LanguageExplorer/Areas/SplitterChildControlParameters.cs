// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Class that contains the control and related information used to set up a main (left/top or right/bottom) control in a splitter control
	/// </summary>
	internal class SplitterChildControlParameters
	{
		/// <summary />
		internal Control Control { get; set; }
		/// <summary />
		internal string Label { get; set; }
	}
}