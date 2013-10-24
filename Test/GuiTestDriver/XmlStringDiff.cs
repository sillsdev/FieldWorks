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
// File: XmlStringDiff.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// XmlStringDiff wraps Microsoft.XmlDiffPatch for easier use with XML strings.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Xml;
using System.IO;
using Microsoft.XmlDiffPatch; // Does the XML comparison and outputs a diff XML file.

namespace GuiTestDriver
{
	/// <summary>
	/// XmlStringDiff wraps Microsoft.XmlDiffPatch for easier use with XML strings.
	/// The diffgram XML is simplified and a comparison with expected differences is available.
	/// All the intricacies of converting the strings to XML nodes, streaming the results
	/// and converting them back into a useable XML string is hidden here.
	/// </summary>
	public class XmlStringDiff
	{
		string     m_base         = null;
		string     m_target       = null;
		XmlReader  m_baseReader   = null;
		XmlReader  m_targetReader = null;
		byte []    m_bDiffs       = null;
		string     m_sDiffs       = null;
		bool       m_Same         = false;
		bool       m_Compared     = false;

		/// <summary>
		/// XmlStringDiff requires a base and target string to diff.
		/// They may be any XML dialect, but we are most concerned with WorldPad XML.
		/// </summary>
		/// <param name="Base">The string used as the base for comparison.</param>
		/// <param name="Target">The string examined for diffs from the base.</param>
		public XmlStringDiff(string Base, string Target)
		{
			m_base   = Base;
			m_target = Target;
			//convert base and target to XML nodes.
			m_baseReader   = StringToXmlReader(m_base);
			m_targetReader = StringToXmlReader(m_target);
			m_bDiffs       = null;
			m_Same         = false;
			m_Compared     = false;
		}

		/// <summary>
		/// An XML reader is needed to access the XML strings as nodes by the XmlDiff Compare method.
		/// </summary>
		/// <param name="xmlElement">An XML string that is a single element, most usefull when containing other nodes.</param>
		/// <returns>An XML reader for this string.</returns>
		private XmlReader StringToXmlReader(string xmlElement)
		{
			XmlNodeType fragType = XmlNodeType.Element;
			string xmlLang = "";
			System.Xml.XmlSpace xmlSpace = XmlSpace.None;
			XmlNameTable nt = new NameTable();
			System.Xml.XmlNamespaceManager nsMgr = new XmlNamespaceManager(nt);
			XmlParserContext context = new XmlParserContext(nt, nsMgr, xmlLang, xmlSpace);
			return new XmlTextReader(xmlElement, fragType, context);
		}

		/// <summary>
		/// Invokes the XmlDiff Compare method. The strings attribute order and whitespace are ignored.
		/// </summary>
		/// <param name="diffs">The raw byte array of "diffgram" XML returned.</param>
		/// <param name="same">True if the two xml strings are identical.</param>
		/// <returns>Number of bytes read.</returns>
		private int Compare(out byte [] diffs, out bool same)
		{
			Encoding enc = Encoding.UTF8; //.Unicode;
			System.IO.Stream stream = new System.IO.MemoryStream();
			XmlDiff xDiff = new XmlDiff(XmlDiffOptions.IgnoreWhitespace);
			XmlWriter writer = new XmlTextWriter(stream, enc);
			same = xDiff.Compare(m_baseReader, m_targetReader, writer);
			stream.Position = 0;
			diffs = new Byte[stream.Length];
			int bytesRead = stream.Read(diffs,0,(int)stream.Length);
			writer.Close(); // closes stream too.
			m_Compared = true;
			return bytesRead;
		}

		/// <summary>
		/// True if the two strings are equal up to attribute order and whitespace.
		/// </summary>
		/// <returns>True if the class base and target strings are equal.</returns>
		public bool AreEqual()
		{
			int bytesRead = isSame();
			return m_Same;
		}

		/// <summary>
		/// Gets the diffgram XML as a string.
		/// </summary>
		/// <returns>The diffgram XML.</returns>
		public string getDiffString()
		{
			int bytesRead = isSame();
			if (m_sDiffs == null)
			{
				char [] cDiffs = new Char[m_bDiffs.Length];
				int ind = 0;
				foreach ( byte b in m_bDiffs) cDiffs[ind++] = Convert.ToChar(b);
				m_sDiffs = new String(cDiffs);
				m_sDiffs = ReformDiffgram();
			}
			return m_sDiffs;
		}

		/// <summary>
		/// The MS Diffgrams are xml documents and have a lot of "junk" in them we don't need.
		/// This changes it to one we can use more easily and makes them "elements" that
		/// can be used to compare (note the XmlReaders are set to expect "Elements".
		/// </summary>
		/// <returns>The reformed XML string.</returns>
		private string ReformDiffgram()
		{
			// tear off the xml pi and xd: namespace labels
			string sDiffs = m_sDiffs.Replace("\ufeff<?xml version=\"1.0\" encoding=\"utf-8\"?>","").Replace("xd:","");
			// Find the end of the xmldiff start tag. Delete the whole thing. Use the tag w/protoInstruction atts.
			int pos = sDiffs.IndexOf("xmldiff\"",0,sDiffs.Length);
			pos = sDiffs.IndexOf(">",pos,sDiffs.Length-pos);
			if (sDiffs.Length == pos+1) sDiffs = "<equal/>";
			else if (pos > 0)           sDiffs = "<xmldiff>" + sDiffs.Remove(0,pos+1);
			return sDiffs;
		}

		/// <summary>
		/// This comparison is needed to insure the out parameter "same" is set.
		/// Previously, 'save' was m_Save, but the compiler failed to create code for it
		/// as it wasn't used in the block. So, even when the result was true,
		/// the time m_Same was accessed in another method, it was false again!!!
		/// </summary>
		/// <returns>The number of bytes read.</returns>
		private int isSame()
		{
			int bytesRead = 0;
			if (!m_Compared)
			{
				bool same;
				bytesRead = Compare(out m_bDiffs, out same);
				m_Same = same;
			}
			return bytesRead;
		}
	}
}
