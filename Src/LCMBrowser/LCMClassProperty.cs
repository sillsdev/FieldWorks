// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Serialization;

namespace LCMBrowser
{
	/// <summary />
	public class LCMClassProperty
	{
		/// <summary>
		/// Gets or sets the name.
		/// </summary>
		[XmlAttribute]
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets a value indicating whether this <see cref="T:LCMClassProperty"/> is displayed.
		/// </summary>
		[XmlAttribute]
		public bool Displayed { get; set; }

		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents this instance.
		/// </summary>
		public override string ToString()
		{
			return Name;
		}
	}
}