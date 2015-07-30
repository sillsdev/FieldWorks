// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;

namespace Simian
{
	class Variables
	{
		private static Variables m_Variables = null;
		private Hashtable m_vars = null;

		public static Variables getOnly()
		{
			if (m_Variables == null) m_Variables = new Variables();
			return m_Variables;
		}

		private Variables() {m_vars = new Hashtable(); }

		/// <summary>
		/// Retrieves a named variable by name from the pool.
		/// </summary>
		/// <param name="name">The id of the variable.</param>
		/// <returns>The named variable.</returns>
		public string get(string name)
		{
			EmptyElement ee = (EmptyElement)m_vars[name];
			if (ee != null) return ee.getValue(name);
			return null;
		}

		/// <summary>
		/// Retrieves a dotted value by name and data name from the pool.
		/// </summary>
		/// <param name="name">The id of the variable.</param>
		/// <param name="data">The id of the variable data.</param>
		/// <returns>The named variable.</returns>
		public string getDotted(string name, string data)
		{
			EmptyElement ee = (EmptyElement)m_vars[name];
			if (data == null || data.Equals("")) return ee.getValue(name);
			return ee.getValue(data);
		}

		/// <summary>
		/// Add a named variable to the pool to be retrieved by calling get.
		/// </summary>
		/// <param name="name">The id of the variable.</param>
		/// <param name="value">The variable called by name.</param>
		public void add(string name, string value)
		{
			EmptyElement ee = new EmptyElement(name);
			ee.addAttribute(name, value);
			m_vars.Add(name, ee);
		}

		/// <summary>
		/// Add a dotted named variable to the pool to be retrieved by calling getDotted.
		/// If it exists but the new value is null or empty, then the dotted variable
		/// is removed.
		/// </summary>
		/// <param name="name">The id of the variable.</param>
		/// <param name="value">The variable called by name.</param>
		public void add(string name, string dotted, string value)
		{
			if (name == null || name == "") return;
			EmptyElement ee = (EmptyElement)m_vars[name];
			if (ee != null)
			{   // the variable is defined
				if (dotted != null && dotted != "")
				{
					if (value != null && value != "")
						ee.addAttribute(dotted, value);
					else // remove the dotted value
						ee.removeAttribute(dotted);
				}
			}
			else if (dotted != null && dotted != "" && value != null && value != "")
			{   // the dotted variable is NOT defined
				ee = new EmptyElement(name);
				ee.addAttribute(dotted, value);
				m_vars.Add(name, ee);
			}
		}

		/// <summary>
		/// Removes an variable from the hash table.
		/// </summary>
		/// <param name="name">of the variable to remove.</param>
		public void remove(string name)
		{
			m_vars.Remove(name);
		}
	}
}
