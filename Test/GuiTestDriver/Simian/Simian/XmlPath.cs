using System;
using System.Xml;
using System.Collections;

namespace Simian
{
	/// <summary>
	/// XmlPath manages a model node assembled GUI path.
	/// It keeps the GUI path and model node together.
	/// A non-modeled GUI path may be assigned to an XmlPath,
	/// but it has no model node. This allows raw GUI paths
	/// and Xml paths to be in the same collection.
	/// </summary>
	public class XmlPath
	{
		bool    m_valid = false;
		string  m_path  = null;
		XmlNode m_node = null;

		/// <summary>
		/// Constructs an XmlPath object that provides a readable path to the target from the root.
		/// This object can also determine if the control should be visible or enabled.
		/// </summary>
		/// <param name="node"></param>
		public XmlPath(XmlNode node)
		{
			m_node = node;
			m_path = AssemblePath(m_node);
			isValid();
		}
		/// <summary>
		/// Creates an XmlPath without a model so the path can be collected with
		/// other XmlPaths.
		/// </summary>
		/// <param name="path">A raw GUI path</param>
		public XmlPath(string path)
		{
			m_node = null;
			m_path = path;
			isValid();
		}

		/// <summary>
		/// After the XmlPath is created, calling isValid verifies that it was built correctly.
		/// </summary>
		/// <returns>true if the XML Path object is well constructed.</returns>
		public bool isValid()
		{
			if (m_path != null && m_path != "") m_valid = true;
			return m_valid;
		}

		public string Path
		{
			get { return m_path; }
			set { m_path = value; isValid(); }
		}

		public XmlNode ModelNode
		{
			get { return m_node; }
		}

		/// <summary>
		/// Get the xPath to this node from the root or role="root" node.
		/// </summary>
		/// <returns>The xPath image of this node</returns>
		public string xPath()
		{ // No validation required, it does not use m_path
			return ImageAncestors(m_node);
		}

		private string ImageAncestors(XmlNode node)
		{
			string image = null;
			if (node != null)
			{
				if (node.NodeType == XmlNodeType.Attribute) image = @"@";
				image += node.Name;
				string ancestors = null;
				if ((node.Attributes["root"]) == null) ancestors = ImageAncestors(node.ParentNode);
				if (ancestors != null) image = ancestors + @"/" + image;
			}
			return image;
		}

		/// <summary>
		/// uses the GUI Model node to build an accessible GUI path to the
		/// corresponding GUI element.
		/// If the node is text or an attribute, the parent's path is created.
		/// </summary>
		/// <param name="node">GUI Model XML node corresponding to a GUI element</param>
		/// <returns>GUI path to the element on the screen</returns>
		private string AssemblePath(XmlNode node)
		{ // assemble the path backward, recursively
			if (node == null) return "";
			string typedPath = null;
			string nodePath = null;
			// What kind of node is it?
			if (node is XmlElement)
			{  // descend to the root sewing "path" attributes together
				XmlAttribute rootAtt = null;
				if (node.Attributes != null) rootAtt = node.Attributes["root"];
				if (rootAtt == null || rootAtt.Value != "yes")
					typedPath = AssemblePath(node.ParentNode);
				// find the path attribute for this node (it may be a @-ref)
				nodePath = GetAttrOrRef(node, "path");
				if (nodePath != null)
				{ // look for variables in the path and resolve them against
					// node attributes (model variables)
					string resolvedPath = ResolveModelPath(node, nodePath);
					typedPath += resolvedPath;
				}
			}
			else if (node is XmlText)
			{   // Get the parent element path
				typedPath = AssemblePath(node.ParentNode);
			}
			else if (node is XmlAttribute)
			{   // Get the parent element path
				XmlNode parent = node.SelectSingleNode("..");
				typedPath = AssemblePath(parent);
			}
			return typedPath;
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
		private string GetAttrOrRef(XmlNode node, String attrName)
		{  // try the direct approach
			string value = null;
			XmlAttribute attr = null;
			if (node.Attributes == null) return null; // no attrs
			attr = node.Attributes[attrName];
			if (attr != null && attr.Value != "")
				value = attr.Value;
			else
			{  // is there a @-ref attribute?
				attr = node.Attributes[attrName+"-ref"];
				if (attr != null && attr.Value != "")
				{ // this is xpath, go find the target and its value
					XmlNode first = node.SelectSingleNode(attr.Value);
					if (first != null && first.Value != "")
						value = first.Value; // the text value of the node
				}
			}
			return value;
		}

		static readonly char[] terminal = new char[] { ';', ' ' };

		/// <summary>
		/// Resolve any local model variables in the expr(ession).
		/// These variables begin with $ and end with a space or ";".
		/// Their names are those of attributes in node.
		/// The value of the node attribute matching the name is substituted
		/// for the variable.
		/// When a variable can't be resolved, it is left in the text -
		/// it may be some other kind of variable.
		/// </summary>
		/// <param name="node">The node from which to make variable substitutions</param>
		/// <param name="expr">The string with variables to replace</param>
		/// <returns>expr with the local model variables replaced by
		/// corresponding node attribute values</returns>
		static public string ResolveModelPath(XmlNode node, String expr)
		{   // look for variables in the path and resolve them against
			// node attributes (model variables)
			if (expr == null || expr == "") return expr;
			string result = null;
			ArrayList parts = new ArrayList(5); // the array of parse product strings
			string line = expr;
			string deref = null;
			bool found = false;
			// scan the expression for references
			int loc = line.IndexOf("$");
			while (-1 != loc)
			{ // found a reference - cut it out and expand it
				if (loc > 0) parts.Add(line.Substring(0, loc));
				line = line.Substring(loc); // drop the leading text
				int end = line.IndexOfAny(terminal);
				if (-1 < end)
				{ // other text follows
					string cut = line.Substring(1, end - 1);
					deref = evalRef(node, cut, out found);
					if (line[end] == ';' && found) end++;
					try { line = line.Substring(end); }
					catch (ArgumentOutOfRangeException)
					{ line = null; }
					if (line == "") line = null;
				}
				else
				{ // this ref is last in the expression
					deref = evalRef(node, line.Substring(1), out found);
					line = null; // nothing left to parse
				}
				if (deref != null)
				{
					if (!found) deref = '$' + deref;
					parts.Add(deref);
				}
				if (line != null) loc = line.IndexOf("$");
				else loc = -1;
			}
			if (line != null) parts.Add(line);
			// line up and return all the parts.
			foreach (String seg in parts) result += seg;
			return result;
		}

		/// <summary>
		/// Given the name of a variable, match it to an attribute name in the node,
		/// or a variable in ancestral scope or return null.
		/// Variables match attributes that have simple content, so
		/// there is no component selection as with script variables.
		/// </summary>
		/// <param name="node">The node from which to make variable substitutions</param>
		/// <param name="refer">The name of the local variable</param>
		/// <param name="found">true if the variable name matched an attribute name in node</param>
		/// <returns>The value of the matching attribute</returns>
		static private string evalRef(XmlNode node, string refer, out bool found)
		{
			found = false;
			string value = null;
			// it could refer to one of the model node's attributes
			XmlElement eltContext = null;
			if (node is XmlElement)        eltContext = (XmlElement)node;
			else if (node is XmlAttribute) eltContext = (XmlElement)(node.SelectSingleNode(".."));

			XmlAttribute attr = eltContext.Attributes[refer];
			if (attr != null)
			{
				value = attr.Value;
				found = true;
			}
			else
			{  // it could refer to a var set in an ancestor model node
				XmlNode varNode = node.SelectSingleNode("ancestor::*/var[@id='"+refer+"']");
				if (varNode != null)
				{ // the var has a @set attribute with the value
					XmlAttribute setAttr = varNode.Attributes["set"];
					if (setAttr != null && setAttr.Value != null && setAttr.Value != "")
					{ // good value, found it - it may have more variable refs in it
						value = ResolveModelPath(varNode, setAttr.Value);
						if (value != null && value != "") found = true;
					}
				}
			}
			if (found == false)
			{  // this can be a variable set in the script not the model
				value = Utilities.evalRef(refer, out found);
			}
			return value;
		}
	}
}
