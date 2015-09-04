// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Interface for major FLEx components
	/// </summary>
	public interface IMajorFlexUiComponent : IMajorFlexComponent
	{
		/// <summary>
		/// Get the internal name of the component.
		/// </summary>
		/// <remarks>NB: This is the machine friendly name, not the user friendly name.</remarks>
		string MachineName { get; }

		/// <summary>
		/// User-visible localizable component name.
		/// </summary>
		string UiName { get; }
	}
}