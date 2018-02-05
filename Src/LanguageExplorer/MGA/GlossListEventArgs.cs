// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using SIL.LCModel;

namespace LanguageExplorer.MGA
{
	public delegate void GlossListEventHandler(object sender, GlossListEventArgs e);


	/// <summary>
	/// Summary description for GlossListEventArgs.
	/// </summary>
	public class GlossListEventArgs : EventArgs
	{
		public GlossListEventArgs(GlossListBoxItem glbi)
		{
			GlossListBoxItem = glbi;
		}
		public GlossListEventArgs(LcmCache cache, XmlNode node, string sAfterSeparator, string sComplexNameSeparator, bool fComplexNameFirst)
		{
			GlossListBoxItem = new GlossListBoxItem(cache, node, sAfterSeparator, sComplexNameSeparator, fComplexNameFirst);
		}
		/// <summary>
		/// Gets the item.
		/// </summary>
		public GlossListBoxItem GlossListBoxItem { get; }
	}
}
