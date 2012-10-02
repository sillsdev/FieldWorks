// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlHelper.cs
// Responsibility: TE team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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
	}
}
