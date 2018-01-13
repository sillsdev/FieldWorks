// Copyright (c) 2003-2018 SIL International
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
		private readonly string m_sUrl;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="sUrl"></param>
		public HtmlControlEventArgs(string sUrl)
		{
			m_sUrl = sUrl;
		}

		/// <summary>
		/// Get the event's URL.
		/// </summary>
		public string URL
		{
			get { return m_sUrl;}
		}
	}

	/// <summary>
	/// Delegate declaration.
	/// </summary>
	public delegate void HtmlControlEventHandler(object sender, HtmlControlEventArgs e);
}