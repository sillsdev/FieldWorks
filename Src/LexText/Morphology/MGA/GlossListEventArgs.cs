// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: GLossListEventArgs.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	public delegate void GlossListEventHandler(object sender, GlossListEventArgs e);


	/// <summary>
	/// Summary description for GlossListEventArgs.
	/// </summary>
	public class GlossListEventArgs : EventArgs
	{
		private readonly GlossListBoxItem m_glossListBoxItem;

		public GlossListEventArgs(GlossListBoxItem glbi)
		{
			m_glossListBoxItem = glbi;
		}
		public GlossListEventArgs(FdoCache cache, XmlNode node, string sAfterSeparator, string sComplexNameSeparator, bool fComplexNameFirst)
		{
			m_glossListBoxItem = new GlossListBoxItem(cache, node, sAfterSeparator, sComplexNameSeparator, fComplexNameFirst);
		}
		/// <summary>
		/// Gets the item.
		/// </summary>
		public GlossListBoxItem GlossListBoxItem
		{
			get
			{
				return m_glossListBoxItem;
			}
		}
	}
}
