// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer
{
	/// <summary>
	/// Interface that lets clients access the main pane bar container,
	/// but without need for reflection or circular references.
	/// </summary>
	public interface IPaneBarContainer : IMainContentControl
	{
		/// <summary>
		/// Get the pane bar.
		/// </summary>
		IPaneBar PaneBar { get; }
	}
}