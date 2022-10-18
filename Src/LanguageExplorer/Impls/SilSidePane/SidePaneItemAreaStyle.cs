// Copyright (c) 2010 SIL International
// SilOutlookBar is licensed under the MIT license.

namespace LanguageExplorer.Impls.SilSidePane
{
	/// <summary>
	/// Style of the item area
	/// </summary>
	/// <remarks>Must be public, because some test uses the enum as a parameter to a method that must be public.</remarks>
	public enum SidePaneItemAreaStyle
	{
		/// <summary>Strip of buttons with large icons</summary>
		Buttons,
		/// <summary>List with small icons</summary>
		List,
		/// <summary>List with small icons, implemented as a ToolStrip</summary>
		StripList,
	}
}