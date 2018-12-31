// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.Controls
{
	/// <summary>
	/// Special event args class for Html Control
	/// </summary>
	public class HtmlControlEventArgs : EventArgs
	{
		/// <summary />
		public HtmlControlEventArgs(string sUrl)
		{
			URL = sUrl;
		}

		/// <summary>
		/// Get the event's URL.
		/// </summary>
		public string URL { get; }
	}

	/// <summary>
	/// Delegate declaration.
	/// </summary>
	public delegate void HtmlControlEventHandler(object sender, HtmlControlEventArgs e);
}