// Copyright (c) 2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// Interface for UI widgets, such as menus or toolbars, that allows for managing what is added to a main component and the related events.
	/// </summary>
	internal interface IToolUiWidgetManager : IDisposable
	{
		/// <summary>
		/// Initialize with the main FW parameter object
		/// </summary>
		/// <param name="majorFlexComponentParameters">The main parameter object for a given IFwMainWnd instance.</param>
		/// <param name="recordList">Current record list for menus/toobars.</param>
		/// <param name="sharedEventHandlers">Event handlers that are shared with other implementations.</param>
		/// <param name="randomParameters">A list of zero, or more, parameters the implementation needs, where the client knows of the need</param>
		void Initialize(MajorFlexComponentParameters majorFlexComponentParameters, IRecordList recordList, IReadOnlyDictionary<string, EventHandler> sharedEventHandlers = null, IReadOnlyList<object> randomParameters = null);

		/// <summary>
		/// Get a list of 0, or more, event handlers that are known to be shared between several interface implementations.
		/// </summary>
		/// <remarks>
		/// Implementors should return an empty Dictionary, when they have no event handlers that are to be shared.
		/// </remarks>
		IReadOnlyDictionary<string, EventHandler> SharedEventHandlers { get; }
	}
}