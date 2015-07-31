// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Xml;
using System.IO;
using System.Reflection;

namespace GuiTestDriver
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
				Console.Out.WriteLine("GUI Test Driver can't find " + path + ". " + xe.Message);
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
			return xa.Value;
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
		static public XmlNodeList selectNodes(XmlElement root, string xPath)
		{
			if (root != null)
			{
				XmlNamespaceManager nsmgr = new XmlNamespaceManager(root.OwnerDocument.NameTable);
				return root.SelectNodes(xPath, nsmgr);
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
