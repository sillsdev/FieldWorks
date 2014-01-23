// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: InsPrototype.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
//   Almost simple signature class - name and attributes that hold the formal attribute names
//   and a value that represents what kind of info is to be represented - not exactly a type
//   or enumeration of content - at this point.
// </remarks>

using System;
using System.Xml;
using System.Collections;

namespace GuiTestDriver
{
	public class InsPrototype
	{
		ArrayList m_attributes = null;
		string m_name = null;

		public InsPrototype(XmlNode xn)
		{
			XmlElement el = (XmlElement)xn;
			m_name = el.Name;
			XmlAttributeCollection attributes = el.Attributes;
			foreach (XmlAttribute at in attributes)
			{
				if (m_attributes == null) m_attributes = new ArrayList();
				m_attributes.Add(new Attribute(at.Name, at.Value));
			}
		}

		public string Name
		{
			get { return m_name; }
		}

		public ArrayList Attributes
		{
			get { return m_attributes; }
		}
	}
}
