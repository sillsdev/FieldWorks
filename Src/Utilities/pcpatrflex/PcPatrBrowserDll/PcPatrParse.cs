using System;
using System.Xml;

namespace SIL.PcPatrBrowser
{
	/// <summary>
	/// Summary description for PcPatrParse.
	/// </summary>
	public class PcPatrParse
	{
		protected XmlNode m_node;

		public PcPatrParse(XmlNode node)
		{
			m_node = node;
		}

		/// <summary>
		/// Get XML node
		/// </summary>
		public XmlNode Node
		{
			get { return m_node; }
		}
	}
}
