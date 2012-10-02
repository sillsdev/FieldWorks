using System;
using System.Collections;
using System.Xml;
using System.Xml.XPath;

namespace Simian
{
	class GuiModel
	{
		private XmlElement m_guiModel = null;
		private string m_title = null;
		private Log m_log;

		/// <summary>
		/// Reads the model from the path and root file given.
		/// All model files refered to from the root file must be in the path folder.
		/// If a section of the model can't be found or read, it is not grafted onto the root.
		/// </summary>
		/// <param name="path">The path to the folder containing the root file.</param>
		/// <param name="root">The name of the file that refers to all the others.</param>
		public GuiModel(string path, string root)
		{
			m_log = Log.getOnly();
			XmlDocument doc = XmlFiler.getDocument(path + @"\" + root, false);
			if (doc == null) return;
			// read the sections and various other files into a single node tree
			m_guiModel = doc.DocumentElement;
			if (m_guiModel == null) return;
			XmlNodeList sections = m_guiModel.SelectNodes("section");
			if (sections == null) return;
			foreach (XmlNode sec in sections)
			{
				string name = sec.Attributes["file"].Value;
				AppendNodesToTree(m_guiModel, path + @"\" + name, doc);
			}
			ReadModelVars();
		}

		/// <summary>
		/// Gets the model title.
		/// If it is null, a failure is logged.
		/// </summary>
		/// <returns>The model title or null.</returns>
		public string getTitle()
		{
			if (m_title == null)
			{   // read it from the gui model
				m_title = m_guiModel.GetAttribute("title");
				if (m_title == null)
				{
					m_log.writeElt("fail");
					m_log.writeAttr("model", "no title");
					m_log.endElt();
				}
			}
			return m_title;
		}

		/// <summary>
		/// Selects a single node for the appPath from the context node.
		/// if the context is null, the model document element is used.
		/// If the appPath (XPath) returns a list, the first is selected.
		/// If it returns a text node with XPath, that XPath is used to
		/// select nodes if possible.
		/// If it is invalid, null is returned.
		/// </summary>
		/// <param name="context">XmlPath to start from or null.</param>
		/// <param name="appPath">XPath to a single model node.</param>
		/// <returns>The first node represented by the appPath or null</returns>
		public XmlPath selectToXmlPath(XmlNode context, string appPath)
		{
			if (context == null && !Utilities.isGoodStr(appPath))
				return null;
			if (context != null && !Utilities.isGoodStr(appPath))
				return new XmlPath(context);
			// logically, appPath has to be good at this point!
			if (context == null) context = m_guiModel; // start from root node
			string expandedAppPath = Utilities.evalExpr(appPath);
			//XmlNamespaceManager nsmgr = new XmlNamespaceManager(context.OwnerDocument.NameTable);
			XmlNode node = context.SelectSingleNode(expandedAppPath); //, nsmgr);
			if (node == null) return null;
			if ((node is XmlText || node is XmlAttribute) && Utilities.isGoodStr(node.Value))
			{ // this text node might be an xpath statement
				string xPathImage = node.Value; // separate line for debugging
				XmlNode xn = null;
				try { xn = context.SelectSingleNode(xPathImage);}
				catch (XPathException) { } // it's not XPath
				if (xn != null) node = xn;
			}
			if (node == null) return null;
			return new XmlPath(node);
		}

		/// <summary>
		/// Selects all the nodes from the context node that are
		/// represented by the appPath, placing each in a XmlPath
		/// in an ArrayList.
		/// if the context is null, the model document element is used.
		/// If it returns a text node with XPath, that XPath is used to
		/// select nodes if possible.
		/// If the appPath (XPath) is invalid, null is returned.
		/// </summary>
		/// <param name="context">XmlPath to start from or null.</param>
		/// <param name="appPath">XPath representing model nodes.</param>
		/// <returns>A list of nodes represented by the appPath or null</returns>
		public ArrayList selectToXmlNodes(XmlNode context, string appPath)
		{
			if (context == null) context = m_guiModel; // start from root node
			if (appPath == null || appPath == "") return null;
			string expandedAppPath = Utilities.evalExpr(appPath);
			XmlNamespaceManager nsmgr = new XmlNamespaceManager(context.OwnerDocument.NameTable);
			XmlNodeList nodes = context.SelectNodes(expandedAppPath, nsmgr);
			if (nodes == null) return null;
			if (nodes.Count == 1 && (nodes.Item(0) is XmlText || nodes.Item(0) is XmlAttribute) && Utilities.isGoodStr(nodes.Item(0).Value))
			{ // this text node should be an xpath statement
				string xPathImage = nodes.Item(0).Value; // separate line for debugging
				nodes = context.SelectNodes(xPathImage);
			}
			if (nodes == null) return null;
			ArrayList nodeList = new ArrayList(nodes.Count);
			foreach (XmlNode xn in nodes) nodeList.Add(xn);
			return nodeList;
		}

		/// <summary>
		/// Selects a string from the model.
		/// If xpath and attName are null, the value of the context is returned.
		/// </summary>
		/// <param name="context">A model node. null is the document node</param>
		/// <param name="xpath">specifies another node in context. If null, context is used.</param>
		/// <param name="attName">An attribute to get a value from. If null, xpath should resolve to a string.</param>
		/// <returns>A string value or null based on the parameters.</returns>
		public string selectToString(XmlNode context, string xpath, string attName)
		{
			if (context == null) context = m_guiModel;
			XmlNode textNode = null;
			if (Utilities.isGoodStr(xpath))
				textNode = context.SelectSingleNode(xpath);
			else textNode = context;
			if (Utilities.isGoodStr(attName))
			{
				if (textNode is XmlElement)
				{
					if (textNode.Attributes == null) return null; // attr not in model element
					XmlAttribute xat = textNode.Attributes[attName];
					if (xat == null) return null; // node didn't have the attribute
					return xat.Value;
				}
			}
			else
			{
				if (textNode is XmlAttribute) return textNode.Value;
				if (textNode is XmlText) return textNode.Value;
			}
			return null;
		}

		/// <summary>
		/// Attaches nodes from a file to the specifiec root from the given document.
		/// The root doesn't have to be the root element of the document.
		/// It can be any element in it.
		/// If the filePath is invalid, it's nodes are not added to the root.
		/// </summary>
		/// <param name="root">The xml element to attach nodes to from the file.</param>
		/// <param name="filePath">The path to the file to append to the root.</param>
		/// <param name="doc">The document to make a fragment from.</param>
		private void AppendNodesToTree(XmlElement root, string filePath, XmlDocument doc)
		{
			XmlDocument secDoc = new XmlDocument();
			try { secDoc.Load(filePath); }
			catch (Exception e)
			{
				Console.Out.WriteLine("Failed to read " + filePath + " because: " + e.Message);
			}
			if (secDoc == null) return;
			XmlDocumentFragment secFrag = doc.CreateDocumentFragment();
			XmlNode top = secDoc.SelectSingleNode("*").Clone();
			// since the node is from a different doc, it must be converted to text,
			// then inserted into a fragment and finally appended to the root.
			secFrag.InnerXml = top.OuterXml;
			XmlNode child = secFrag.FirstChild;
			if (child == null) return;
			root.AppendChild(child);
		}

		/// <summary>
		/// Read the var nodes from the model and add them as vars.
		/// </summary>
		private void ReadModelVars()
		{
			XmlNodeList vars = m_guiModel.SelectNodes("var");
			if (vars != null)
			{
				Variables varlist = Variables.getOnly();
				foreach (XmlNode varNode in vars)
				{
					XmlNode id = varNode.Attributes["id"];
					XmlNode set = varNode.Attributes["set"];
					varlist.add(id.Value, set.Value);
				}
			}
		}


	}
}
