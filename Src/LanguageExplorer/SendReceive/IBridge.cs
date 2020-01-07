// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.FieldWorks.Common.FwUtils;

namespace LanguageExplorer.SendReceive
{
	/// <summary>
	/// Interface that allows for running different kinds of S/R bridges.
	/// </summary>
	internal interface IBridge : IFlexComponent, IDisposable
	{
		/// <summary>
		/// Get the name of the bridge.
		/// </summary>
		/// <remarks>
		/// Names of all implementations, *must* be unique!
		/// </remarks>
		string Name { get; }

		/// <summary>
		/// Run the bridge.
		/// </summary>
		void RunBridge();

		/// <summary>
		/// Register UI widgets.
		/// </summary>
		void RegisterHandlers(GlobalUiWidgetParameterObject globalParameterObject);
	}
}