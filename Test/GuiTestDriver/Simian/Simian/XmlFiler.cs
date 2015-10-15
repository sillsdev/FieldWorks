// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using System.IO;
using System.Reflection;

namespace Simian
{
	/// <summary>
	/// Contains static methods to simplify access to Xml files and their contents.
	/// </summary>
	class XmlFiler
	{
		/// <summary>
		/// Gets a document from the file name supplied.
		/// When the folder is the one where this program executes from,
		/// set onExePath to true.
		/// </summary>
		/// <param name="FileName">The xml file name.</param>
		/// <param name="onExePath">True if the file is in the same folder as this program.</param>
		/// <returns>The document of XML nodes from the file.</returns>
		static public XmlDocument getDocument(string FileName, bool onExePath)
		{
			XmlDocument doc = new XmlDocument();
			doc.PreserveWhitespace = true; // allows <insert> </insert>
			string path = FileName;
			if (onExePath) path = getExecPath() + @"\" + FileName;
			try { doc.Load(path); }
			catch (XmlException xe)
			{
				Log log = Log.getOnly();
				Console.Out.WriteLine("Simian can't find " + path + ". " + xe.Message);
				log.writeElt("fail");
				log.writeAttr("document", path);
				log.writeAttr("exception", xe.Message);
				log.endElt();
			}
			return doc;
		}

		/// <summary>
		/// Gets the document element from the specified xml file.
		/// The document is checked for the expected node name unless
		/// the expected document element name is the empty string.
		/// Then the document element is returned unchecked.
		/// When the folder is the one where this program executes from,
		/// set onExePath to true.
		/// </summary>
		/// <param name="FileName">The name of the xml file to parse.</param>
		/// <param name="docNodeName">Name of the document node expected or empty string.</param>
		/// <param name="onExePath">True if the file is in the same folder as this program.</param>
		/// <returns>The document element or null if it didn't match expectations.</returns>
		static public XmlElement getDocumentElement(string FileName, string docNodeName, bool onExePath)
		{
			XmlElement root = null;
			XmlDocument doc = getDocument(FileName, onExePath);
			if (doc != null)
			{
				if (docNodeName == "") root = doc.DocumentElement;
				else root = doc[docNodeName];
			}
			return root;
		}

		/**
		 * Parses an XML file if the document element matches docElName and
		 * returns an element selected via xPath.
		 * @param fileName the name of the file to parse.
		 * @param docElName the name of the document element - for verification.
		 * @param xPath the xpath query for an element in the named file.
		 * Its context is the document element.
		 * @return the specified element or null.
		 */
		public static XmlElement readXmlFile(string fileName, string docElName, string xPath)
		{
			XmlNodeList els = null;
			XmlElement elDoc = getDocumentElement(fileName, docElName, false);
			if (elDoc != null)
			{ // the name is docElName
				els = selectNodes(elDoc, xPath);
				for (int e = 0; e < els.Count; e++)
				{
					XmlNode n = els.Item(e);
					if (n.NodeType == XmlNodeType.Element) return (XmlElement)n;
				}
			}
			return null;
		}

		/// <summary>
		/// The best way to parse an attribute from an XML element node.
		/// </summary>
		/// <param name="xn">An element node</param>
		/// <param name="name">Name of the attribute</param>
		/// <returns>The value of the attribute or null if it doesn't exist</returns>
		static public string getAttribute(XmlNode xn, string name)
		{
			XmlAttribute xa = xn.Attributes[name];
			if (xa == null) return null;
			xa.Value.Replace("&quote;", "\"");
			return xa.Value;
		}

		/**
		 * Get a string valued attribute from an elemtent by name.
		 * If it is abscent or empty, the default value is returned.
		 * <param name="el"> The element that has the attribute</param>
		 * <param name="atName"> The attribute name</param>
		 * <param name="def"> The default value (default is a keyword)</param>
		 */
		public static string getStringAttr(XmlElement el, String atName, String def)
		{
			string val = el.GetAttribute(atName); // never returns null
			if (val.Equals("")) return def;
			return val;
		}

		/// <summary>
		/// Select document nodes using xPath starting from the supplied root node.
		/// Note, the xPath must not lead to an attribute or string value.
		/// Just nodes.
		/// If it not a node or the root is null, null is returned.
		/// Uses current namespaces.
		/// </summary>
		/// <param name="root">The root of the subtree to search.</param>
		/// <param name="xPath">string of xPath text to locate nodes.</param>
		/// <returns>A list of model nodes matching the query or null</returns>
		private static XmlNodeList selectNodes(XmlElement root, string xPath)
		{
			if (root != null)
			{
				XmlNamespaceManager nsmgr = new XmlNamespaceManager(root.OwnerDocument.NameTable);
				return root.SelectNodes(xPath, nsmgr);
			}
			return null;
		}

		/// <summary>
		/// Gets the first element matching the XPath pattern.
		/// Do not use XPath that results in other nodes, like text.
		/// </summary>
		/// <param name="element">The element to base the query on.</param>
		/// <param name="Xpath">The query expression.</param>
		/// <returns></returns>
		public static XmlElement getElement(XmlElement element, string Xpath)
		{
			XmlNodeList nodes = selectNodes(element, Xpath);
			if (nodes == null) return null;
			for (int n = 0; n < nodes.Count; n++)
			{
				XmlNode node = nodes.Item(n);
				if (node.NodeType != XmlNodeType.Element) continue;
				return (XmlElement)node;
			}
			return null;
		}

		/// <summary>
		/// Gets the file system path to this program's executable.
		/// </summary>
		/// <returns>The path to the executable.</returns>
		static public string getExecPath()
		{
			return Path.GetDirectoryName(Assembly.GetExecutingAssembly().CodeBase).Substring(6);
		}

		/// <summary>
		/// Get the value of an attribute or something it is refering to.
		/// If the attribute has a reference, "attrName"-ref, it is interpreted
		/// as XPATH to the node with the desired value. Only the first such
		/// node value is used.
		/// </summary>
		/// <param name="node">The node who's attribute value is returned</param>
		/// <param name="attrName">The name of an attribute of the node</param>
		/// <returns>The value of the attribute or its @-ref XPATH target value</returns>
		static public string GetAttrOrRef(XmlNode node, string attrName)
		{  // try the direct approach
			string value = null;
			XmlAttribute attr = node.Attributes[attrName];
			if (attr != null && attr.Value != "")
				value = attr.Value;
			else
			{  // is there a @-ref attribute?
				attr = node.Attributes[attrName + "-ref"];
				if (attr != null && attr.Value != "")
				{ // this is xpath, go find the target and its value
					XmlNode first = node.SelectSingleNode(attr.Value);
					if (first != null && first.Value != "")
						value = first.Value; // the text value of the node
				}
			}
			return value;
		}

	}
}
