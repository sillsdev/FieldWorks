// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Drawing;
using System.Windows.Forms;
using System.Xml.Linq;

namespace LanguageExplorer.Areas.Grammar.Tools.PosEdit
{
	/// <summary />
	public delegate void InflAffixTemplateEventHandler (object sender, InflAffixTemplateEventArgs e);

	/// <summary />
	public class InflAffixTemplateEventArgs : EventArgs
	{
		/// <summary>
		/// Constructor
		/// </summary>
		public InflAffixTemplateEventArgs(Control context, XElement node, Point location, int tag)
		{
			Location = location;
			ConfigurationNode = node;
			Context = context;
			Tag = tag;
		}
		public Control Context { get; }

		public int Tag { get; }

		public XElement ConfigurationNode { get; }

		public Point Location { get; }
	}
}