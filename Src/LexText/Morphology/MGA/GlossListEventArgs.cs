// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: GLossListEventArgs.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
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
