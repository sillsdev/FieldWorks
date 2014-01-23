// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Attribute.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
//   This is a simple attribute class - name and value.
// </remarks>

using System;

namespace GuiTestDriver
{
	public class Attribute
	{
		private string m_name = null;
		private string m_value = null;
		private string m_type = null;

		public Attribute(string name, string value)
		{
			m_name = name;
			m_value = value;
		}

		public Attribute(string name, string value, string type)
		{
			m_name = name;
			m_value = value;
			m_type = type;
		}

		public string Name
		{
			get { return m_name; }
		}

		public string Type
		{
			get { return m_type; }
		}

		public string Value
		{
			get { return m_value; }
			set { m_value = value; }
		}

	}
}
