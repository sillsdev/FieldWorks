// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2003' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InflAffixTemplateEventArgs.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using XCore;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	///
	/// </summary>
	public delegate void InflAffixTemplateEventHandler (object sender, InflAffixTemplateEventArgs e);

	/// <summary>
	///
	/// </summary>
	public class InflAffixTemplateEventArgs : EventArgs
	{
		private XmlNode m_node;
		private Point m_location;
		private Control m_contextControl;
		private int m_tag;
		/// <summary>
		/// Constructor
		/// </summary>
		/// <param name="context"/>
		/// <param name="location"/>
		public InflAffixTemplateEventArgs(Control context, XmlNode node, Point location, int tag)
		{
			m_location = location;
			m_node = node;
			m_contextControl = context;
			m_tag = tag;
		}
		public Control Context
		{
			get
			{
				return m_contextControl;
			}
		}
		public int Tag
		{
			get
			{
				return m_tag;
			}
		}
		public XmlNode ConfigurationNode
		{
			get
			{
				return m_node;
			}
		}
		public Point Location
		{
			get
			{
				return m_location;
			}
		}
	}
}
