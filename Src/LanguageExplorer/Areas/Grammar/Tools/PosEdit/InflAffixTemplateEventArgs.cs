// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
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
		private XElement m_node;
		private Point m_location;
		private Control m_contextControl;
		private int m_tag;

		/// <summary>
		/// Constructor
		/// </summary>
		public InflAffixTemplateEventArgs(Control context, XElement node, Point location, int tag)
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
		public XElement ConfigurationNode
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
