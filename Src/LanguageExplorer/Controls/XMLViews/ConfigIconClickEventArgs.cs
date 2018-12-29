// Copyright (c) 2005-2019 SIL International
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
		/// <summary>
		/// Constructor.
		/// </summary>
		public ConfigIconClickEventArgs(Rectangle location)
		{
			Location = location;
		}

		/// <summary>
		/// This is the location of the button relative to the DhListView's client area.
		/// It provides a good idea of where to display a popup.
		/// </summary>
		public Rectangle Location { get; }
	}
}