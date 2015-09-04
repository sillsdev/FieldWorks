// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Drawing;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for each Area in the main IFwMainWnd
	/// </summary>
	public interface IArea : IMajorFlexUiComponent
	{
		/// <summary>
		/// Get the most recently persisted tool, or the default tool if
		/// the persisted one is no longer available.
		/// </summary>
		/// <returns>The last persisted tool or the default tool for the area.</returns>
		ITool GetPersistedOrDefaultToolForArea();

		/// <summary>
		/// Get the machine name of the area's default tool.
		/// </summary>
		string DefaultToolMachineName { get; }

		/// <summary>
		/// Get all installed tools for the area.
		/// </summary>
		IList<ITool> AllToolsInOrder { get; }

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		Image Icon { get; }
	}
}