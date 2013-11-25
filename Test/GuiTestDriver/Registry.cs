// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: Registry.cs
// Responsibility:HintonD
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using NUnit.Framework;
using System.Xml;

/*
 * Example of the xml test script entry.
 *
<!--  Force the email dlg to show up -->
<registry key="HKEY_CURRENT_USER\Software\SIL\Language Explorer\launches" data="0"/>

*/

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Registry.
	/// </summary>
	public class Registry : Instruction
	{
		private string m_key;
		private string m_data;

		public Registry(string key, string data)
		{
			m_key = key;
			m_data = data;
			m_tag = "registry";
		}

		public Registry() : this(null, null) { }


		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public override bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			m_log.isTrue(m_key != null && m_data != null, "Registry instruction must have a key and data.");
			return true;
		}

		public string Key
		{
			get { return m_key; }
			set { m_key = value; }
		}

		public string Data
		{
			get { return m_data; }
			set { m_data = value; }
		}

		public override void Execute()
		{
			base.Execute ();
			string key;
			Microsoft.Win32.RegistryKey regkey = Utilities.parseRegKey(m_key, out key);
			if (regkey != null)
			{
				regkey.SetValue(key, m_data);
				regkey.Close();
			}
			else
				m_log.fail("Invalid Reisitry path: " + m_key);
			Finished = true; // tell do-once it's done
		}

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage (string name)
		{
			if (name == null) name = "key";
			switch (name)
			{
				case "key":	 return m_key;
				case "data": return m_data;
				default:	 return "[Registry does not have data for '"+name+"']";
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_key != null) image += @" key="""+Utilities.attrText(m_key)+@"""";
			if (m_data != null) image += @" data="""+Utilities.attrText(m_data)+@"""";
			return image;
		}
	}
}
