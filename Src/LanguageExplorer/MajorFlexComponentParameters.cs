// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer
{
	/// <summary>
	/// Parameter object used by IMajorFlexComponent
	/// </summary>
	internal sealed class MajorFlexComponentParameters
	{
		/// <summary />
		internal ICollapsingSplitContainer MainCollapsingSplitContainer { get; private set; }
		/// <summary />
		internal MenuStrip MenuStrip { get; private set; }
		/// <summary />
		internal ToolStripContainer ToolStripContainer { get; private set; }
		/// <summary />
		internal StatusBar Statusbar { get; private set; }
		/// <summary />
		internal ParserMenuManager ParserMenuManager { get; private set; }
		/// <summary />
		internal DataNavigationManager DataNavigationManager { get; private set; }
		/// <summary />
		internal FlexComponentParameters FlexComponentParameters { get; private set; }

		/// <summary />
		internal MajorFlexComponentParameters(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar, ParserMenuManager parserMenuManager, DataNavigationManager dataNavigationManager, FlexComponentParameters flexComponentParameters)
		{
			MainCollapsingSplitContainer = mainCollapsingSplitContainer;
			MenuStrip = menuStrip;
			ToolStripContainer = toolStripContainer;
			Statusbar = statusbar;
			ParserMenuManager = parserMenuManager;
			DataNavigationManager = dataNavigationManager;
			FlexComponentParameters = flexComponentParameters;
		}
	}
}