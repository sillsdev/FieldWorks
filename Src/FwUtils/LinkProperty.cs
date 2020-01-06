// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Class used by FwLinkArgs and Friends.
	/// </summary>
	[Serializable]
	//TODO: we can't very well change this source code every time someone adds a new value type!!!
	[XmlInclude(typeof(System.Drawing.Point))]
	[XmlInclude(typeof(System.Drawing.Size))]
	[XmlInclude(typeof(FormWindowState))]
	public sealed class LinkProperty
	{
		public LinkProperty(string name, object value)
		{
			Name = name;
			Value = value;
		}

		/// <summary>
		/// Get the property name.
		/// </summary>
		public string Name { get; }

		/// <summary>
		/// Get property value.
		/// </summary>
		public object Value { get; }
	}
}