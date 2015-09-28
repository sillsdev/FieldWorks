// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for each Tool in an IArea
	/// </summary>
	internal interface ITool : IMajorFlexUiComponent
	{
		/// <summary>
		/// Get the area machine name the tool is for.
		/// </summary>
		string AreaMachineName { get; }

		/// <summary>
		/// Get the image for the area.
		/// </summary>
		Image Icon { get; }
	}
}