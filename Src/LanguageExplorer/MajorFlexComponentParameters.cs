// Copyright (c) 2016-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords;
using LanguageExplorer.Controls.SilSidePane;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace LanguageExplorer
{
	/// <summary>
	/// Parameter object used by IMajorFlexComponent
	/// </summary>
	internal sealed class MajorFlexComponentParameters
	{
		/// <summary />
		internal ICollapsingSplitContainer MainCollapsingSplitContainer { get; }
		/// <summary />
		internal MenuStrip MenuStrip { get; }
		/// <summary />
		internal ToolStripContainer ToolStripContainer { get; }
		/// <summary />
		internal StatusBar Statusbar { get; }
		/// <summary />
		internal ParserMenuManager ParserMenuManager { get; }
		/// <summary />
		internal DataNavigationManager DataNavigationManager { get; }
		/// <summary />
		internal FlexComponentParameters FlexComponentParameters { get; }
		/// <summary />
		internal IRecordListRepositoryForTools RecordListRepositoryForTools { get; }
		/// <summary />
		internal LcmCache LcmCache { get; }
		/// <summary />
		internal IFlexApp FlexApp { get; }
		/// <summary />
		internal IFwMainWnd MainWindow { get; }
		/// <summary />
		internal ISharedEventHandlers SharedEventHandlers { get; }
		/// <summary />
		internal SidePane SidePane { get; }

		internal MajorFlexComponentParameters(ICollapsingSplitContainer mainCollapsingSplitContainer, MenuStrip menuStrip, ToolStripContainer toolStripContainer, StatusBar statusbar,
			ParserMenuManager parserMenuManager, DataNavigationManager dataNavigationManager,
			IRecordListRepositoryForTools recordListRepositoryForTools,
			FlexComponentParameters flexComponentParameters, LcmCache lcmCache, IFlexApp flexApp, IFwMainWnd mainWindow, ISharedEventHandlers sharedEventHandlers, SidePane sidePane)
		{
			MainCollapsingSplitContainer = mainCollapsingSplitContainer;
			MenuStrip = menuStrip;
			ToolStripContainer = toolStripContainer;
			Statusbar = statusbar;
			ParserMenuManager = parserMenuManager;
			DataNavigationManager = dataNavigationManager;
			RecordListRepositoryForTools = recordListRepositoryForTools;
			FlexComponentParameters = flexComponentParameters;
			LcmCache = lcmCache;
			FlexApp = flexApp;
			MainWindow = mainWindow;
			SharedEventHandlers = sharedEventHandlers;
			SidePane = sidePane;
		}
	}
}