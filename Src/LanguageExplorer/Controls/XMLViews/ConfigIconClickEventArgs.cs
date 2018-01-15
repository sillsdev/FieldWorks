// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Arguments for clicking on ConfigIcon
	/// </summary>
	public class ConfigIconClickEventArgs : EventArgs
	{
		private Rectangle m_buttonLocation;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="location">The location.</param>
		public ConfigIconClickEventArgs(Rectangle location)
		{
			m_buttonLocation = location;
		}

		/// <summary>
		/// This is the location of the button relative to the DhListView's client area.
		/// It provides a good idea of where to display a popup.
		/// </summary>
		/// <value>The location.</value>
		public Rectangle Location => m_buttonLocation;
	}

	/// <summary>
	/// Handles clicking on ConfigIcon
	/// </summary>
	public delegate void ConfigIconClickHandler(object sender, ConfigIconClickEventArgs e);
}