// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlHelper.cs
// Responsibility: TE team
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// XmlHelper contains methods to help when testing with XmlDocuments.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the XML documents and reports where the actual XML document differs from
		/// the expected.
		/// </summary>
		/// <param name="expected">The expected state of the actual.</param>
		/// <param name="actual">The actual XML document.</param>
		/// <param name="strDifference">A string describing the difference between the expected
		/// and actual nodes</param>
		/// <returns><c>true</c> if nodes are the same; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool CompareXmlNodes(XmlNodeList expected, XmlNodeList actual,
			out string strDifference)
		{
			if (expected.Count != actual.Count)
			{
				// Number of nodes in the expected and actual nodes are different.
				// Describe difference and fail assertion.
				StringBuilder strBldr = new StringBuilder();
				strBldr.Append(" " + Environment.NewLine + "Number of XML nodes is different: expected (" +
					expected.Count + "), actual (" + actual.Count + ")" + Environment.NewLine + "Expected: ");
				foreach (XmlNode expectedNode in expected)
					strBldr.Append(expectedNode.Name + " ");

				strBldr.Append(Environment.NewLine + "Actual: ");
				foreach (XmlNode actualNode in actual)
					strBldr.Append(actualNode.Name + " ");
				strDifference = strBldr.ToString();
				return false;
			}

			// Compare the expected and actual nodes
			for (int i = 0; i < expected.Count; i++)
			{
				XmlNode expectedNode = expected[i];
				XmlNode actualNode = actual[i];

				if (expectedNode.Name == actualNode.Name)
				{
					// Compare the attributes of the nodes.
					if (!CompareXmlAttributes(expectedNode.Name, expectedNode.Attributes,
						actualNode.Attributes, out strDifference))
					{
						return false;
					}

					// Compare the content of the nodes.
					if (expectedNode.Value != actualNode.Value)
					{
						StringBuilder strBldr = new StringBuilder();
						strBldr.Append("Value in node " + expectedNode.Name + " are not equal:" +
							Environment.NewLine);
						strBldr.Append("  Expected: " + expectedNode.Value + Environment.NewLine);
						strBldr.Append("  Actual:   " + actualNode.Value);
						strDifference = strBldr.ToString();
						return false;
					}

					// Recursively check child nodes of expected and actual
					if (!CompareXmlNodes(expectedNode.ChildNodes, actualNode.ChildNodes, out strDifference))
					{
						strDifference = expectedNode.Name + " " + strDifference;
						return false;
					}
				}
				else
				{
					StringBuilder strBldr = new StringBuilder();
					strBldr.Append("Unexpected node. Expected: " + expectedNode.Name + ", Actual: " + actualNode.Name);
					strDifference = strBldr.ToString();
					return false;
				}
			}

			strDifference = string.Empty;
			return true;
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the name of the node by.
		///// </summary>
		///// <param name="nodeList">The node list.</param>
		///// <param name="name">The name.</param>
		///// <returns></returns>
		///// ------------------------------------------------------------------------------------
		//private XmlNode GetNodeByName(XmlNodeList nodeList, string name)
		//{
		//    foreach (XmlNode node in nodeList)
		//    {
		//        if (node.Name = name)
		//            return node;
		//    }

		//    return null;
		//}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the XML attributes.
		/// </summary>
		/// <param name="expected">The expected attributes.</param>
		/// <param name="actual">The actual attributes.</param>
		/// <param name="strDifference">out: A string describing the difference in attributes.
		/// </param>
		/// <returns><c>true</c> if the attributes are the same; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public static bool CompareXmlAttributes(XmlAttributeCollection expected,
			XmlAttributeCollection actual, out string strDifference)
		{
			return CompareXmlAttributes(string.Empty, expected, actual, out strDifference);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the XML attributes.
		/// </summary>
		/// <param name="owningNodeName">The name of the owning node.</param>
		/// <param name="expected">The expected attributes.</param>
		/// <param name="actual">The actual attributes.</param>
		/// <param name="strDifference">out: A string describing the difference in attributes.</param>
		/// <returns>
		/// 	<c>true</c> if the attributes are the same; <c>false</c> otherwise
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static bool CompareXmlAttributes(string owningNodeName, XmlAttributeCollection expected,
			XmlAttributeCollection actual, out string strDifference)
		{
			strDifference = string.Empty;

			int expectedCount = (expected == null ? 0 : expected.Count);
			int actualCount = (actual == null ? 0 : actual.Count);
			if (actualCount > expectedCount)
			{
				strDifference = string.Format("Count of attributes different in node {0}. Expected {1} but was {2}",
					owningNodeName, expectedCount, actualCount);
				return false;
			}

			if (expectedCount == 0)
				return true;

			foreach (XmlAttribute expectedAttrib in expected)
			{
				XmlNode actualAttrib =
					(actual != null ? actual.GetNamedItem(expectedAttrib.Name) : null);

				if (actualAttrib == null)
				{
					strDifference = string.Format("Attribute {0} is missing.", expectedAttrib.Name);
					return false;
				}

				if (expectedAttrib.InnerText != actualAttrib.InnerText)
				{
					strDifference = "Attributes are different for attribute " +
						expectedAttrib.Name + ": " + Environment.NewLine +
						" Expected: " + expectedAttrib.InnerText + Environment.NewLine +
						" Actual:   " + actualAttrib.InnerText;
					return false;
				}
			}

			strDifference = string.Empty;
			return true;
		}

		/// <summary>
		/// Compare two XML elements for equality.  Take into account element names, attributes
		/// (in any order), child elements (in order), and text content for elements that have
		/// no child elements.
		/// </summary>
		/// <returns>
		/// true iff the two XML elements are essentially equal.
		/// </returns>
		public static bool EqualXml(XElement xeExpected, XElement xeActual)
		{
			return EqualXml(xeExpected, xeActual, null);
		}

		/// <summary>
		/// Compare two XML elements for equality.  Take into account element names, attributes
		/// (in any order), child elements (in order), and text content for elements that have
		/// no child elements.
		/// </summary>
		/// <returns>
		/// true iff the two XML elements are essentially equal.
		/// </returns>
		/// <remarks>
		/// The StringBuilder argument provides useful information for test output when the
		/// first difference is discovered.
		/// </remarks>
		public static bool EqualXml(XElement xeExpected, XElement xeActual, StringBuilder sb)
		{
			if (xeExpected.Name != xeActual.Name)
			{
				if (sb != null)
				{
					sb.AppendFormat("Element names are different: expected {0}, but have {1}.",
						xeExpected.Name, xeActual.Name);
					sb.AppendLine();
				}
				return false;
			}
			if (!SameAttributes(xeExpected, xeActual, sb))
				return false;
			if (!xeExpected.HasElements && !xeActual.HasElements)
			{
				if (xeExpected.Value != xeActual.Value)
				{
					if (sb != null)
					{
						sb.AppendFormat("Contents of an {0} element are different: expected '{1}', but have '{2}'",
							xeExpected.Name, xeExpected.Value, xeActual.Value);
						sb.AppendLine();
					}
					return false;
				}
				else
				{
					return true;
				}
			}
			List<XElement> expectedChildren = new List<XElement>(xeExpected.Elements());
			List<XElement> actualChildren = new List<XElement>(xeActual.Elements());
			if (expectedChildren.Count != actualChildren.Count)
			{
				if (sb != null)
				{
					sb.AppendFormat("A {0} element has varying numbers of children: expected {1}, but have {2}",
						xeExpected.Name, expectedChildren.Count, actualChildren.Count);
					sb.AppendLine();
				}
				return false;
			}
			for (int i = 0; i < expectedChildren.Count; ++i)
			{
				if (!EqualXml(expectedChildren[i], actualChildren[i], sb))
					return false;
			}
			return true;
		}

		/// <summary>
		/// Compare the attributes of the two XML elements, handling Namespace declarations
		/// carefully (by ignoring those that are not expected).
		/// </summary>
		/// <returns>
		/// true iff the attributes of the two XML elements are the same, regardless of order.
		/// </returns>
		private static bool SameAttributes(XElement xeExpected, XElement xeActual, StringBuilder sb)
		{
			List<XAttribute> expected = new List<XAttribute>();
			List<XAttribute> actual = new List<XAttribute>();
			int cNamespaceExpected = 0;
			foreach (var attr in xeExpected.Attributes())
			{
				if (attr.IsNamespaceDeclaration)
					++cNamespaceExpected;
				expected.Add(attr);
			}
			foreach (var attr in xeActual.Attributes())
			{
				if (attr.IsNamespaceDeclaration && cNamespaceExpected == 0)
					continue;					// ignore unexpected namespace declarations
				actual.Add(attr);
			}
			if (expected.Count != actual.Count)
			{
				if (sb != null)
				{
					sb.AppendFormat("An {0} element has varying numbers of attributes:  expected {1}, but have {2}",
						xeExpected.Name, expected.Count, actual.Count);
					sb.AppendLine();
					sb.Append("    Expected attributes: ");
					foreach (var attr in expected)
						sb.AppendFormat(" {0}", attr.Name);
					sb.AppendLine();
					sb.Append("    Actual attributes: ");
					foreach (var attr in actual)
						sb.AppendFormat(" {0}", attr.Name);
					sb.AppendLine();
				}
				return false;
			}
			foreach (var attrExpect in expected)
			{
				bool match = false;
				string badmatch = null;
				foreach (var attrActual in actual)
				{
					if (attrActual.Name == attrExpect.Name)
					{
						match = (attrActual.Value == attrExpect.Value) ||
								(attrActual.IsNamespaceDeclaration && attrExpect.IsNamespaceDeclaration);
						if (!match && sb != null)
							badmatch = String.Format("    but actual {1}=\"{2}\"", attrActual.Name, attrActual.Value);
						break;
					}
				}
				if (!match)
				{
					if (sb != null)
					{
						sb.AppendFormat("An {0} element does not have a matching attribute:  expected {1}=\"{2}\"",
							xeExpected.Name, attrExpect.Name, attrExpect.Value);
						sb.AppendLine();
						if (!String.IsNullOrEmpty(badmatch))
							sb.AppendLine(badmatch);
					}
					return false;
				}
			}
			return true;
		}
	}
}
