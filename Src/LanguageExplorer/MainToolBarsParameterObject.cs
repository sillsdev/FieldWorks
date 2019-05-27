// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace LanguageExplorer
{
	internal sealed class MainToolBarsParameterObject
	{
		internal IReadOnlyDictionary<ToolBar, ToolStrip> MainToolBar { get; }
		internal IReadOnlyDictionary<ToolBar, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>> SupportedToolBarItems { get; }

		internal MainToolBarsParameterObject(IReadOnlyDictionary<ToolBar, ToolStrip> mainToolBars, IReadOnlyDictionary<ToolBar, Tuple<Dictionary<Command, ToolStripItem>, List<ISeparatorToolStripBundle>>> supportedToolBarItems)
		{
			MainToolBar = mainToolBars;
			SupportedToolBarItems = supportedToolBarItems;
		}
	}
}