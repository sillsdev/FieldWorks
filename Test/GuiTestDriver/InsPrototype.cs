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
// File: InsPrototype.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
//   Almost simple signature class - name and attributes that hold the formal attribute names
//   and a value that represents what kind of info is to be represented - not exactly a type
//   or enumeration of content - at this point.
// </remarks>
// --------------------------------------------------------------------------------------------
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
