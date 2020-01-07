// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for helping to deal with visibility of the ToolStripSeparator instances on menus.
	/// </summary>
	internal interface ISeparatorMenuBundle
	{
		ToolStripSeparator Separator { get; }
		List<ToolStripMenuItem> PrecedingItems { get; }
		List<ToolStripMenuItem> FollowingItems { get; }
	}
}