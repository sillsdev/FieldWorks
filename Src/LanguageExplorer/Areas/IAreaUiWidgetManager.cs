// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Interface for an area-wide UI manager system, that can add menus/toolbars, etc that are used by most tools in an area.
	/// </summary>
	internal interface IAreaUiWidgetManager : IDisposable
	{
		/// <summary>
		/// Initialize the implementation.
		/// </summary>
		/// <param name="majorFlexComponentParameters">The main parameter object for a given IFwMainWnd instance.</param>
		/// <param name="area">The area used by the implementation.</param>
		/// <param name="toolUiWidgetManager">Optional tool-specific menu manager.</param>
		/// <param name="recordList">Optional record list that provides the current main CmObject being displayed.</param>
		void Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IArea area, IToolUiWidgetManager toolUiWidgetManager = null, IRecordList recordList = null);

		/// <summary>
		/// Get the active tool in the current area.
		/// </summary>
		ITool ActiveTool { get; }

		/// <summary>
		/// Get the current IToolUiWidgetManager for the area.
		/// </summary>
		IToolUiWidgetManager ActiveToolUiManager { get; }

		/// <summary>
		/// In preparation for disposal, unwire any shared event handlers.
		/// </summary>
		void UnwireSharedEventHandlers();
	}
}