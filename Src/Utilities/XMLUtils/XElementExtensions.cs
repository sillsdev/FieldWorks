// Copyright (c) 2016 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;

namespace SIL.Utils
{
	/// <summary>
	/// Extensions for the XElement class.
	/// </summary>
	public static class XElementExtensions
	{
		/// <summary>
		/// Clone the XElement.
		/// </summary>
		/// <returns>A copy of the XElement</returns>
		public static XElement Clone(this XElement me)
		{
			return XElement.Parse(me.ToString());
		}

		/// <summary>
		/// Get an xml string for the given XElement.
		/// </summary>
		/// <returns>Equivalent of "OuterXml" for an XmlNode</returns>
		public static string GetOuterXml(this XElement me)
		{
			return me.ToString();
		}

		/// <summary>
		/// Get an xml string for all children.
		/// </summary>
		/// <returns>Equivalent of "InnerText" for an XmlNode</returns>
		public static string GetInnerText(this XElement me)
		{
			return me.Value;
		}

		/// <summary>
		/// Get an xml string for all children.
		/// </summary>
		/// <returns>Equivalent of "InnerText" for an XmlNode</returns>
		public static string GetInnerXml(this XElement me)
		{
			return string.Concat(me.Elements());
		}
	}
}
