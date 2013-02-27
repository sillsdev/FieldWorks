// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, 2012, SIL International. All Rights Reserved.
// <copyright from='2003' to='2012' company='SIL International'>
//		Copyright (c) 2003, 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlUtils.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// This makes available some utilities for handling XML Nodes
// </remarks>
// --------------------------------------------------------------------------------------------
//We're changing to using libxslt (wrapped in the LibXslt class) on Linux/Mono.
//#if __MonoCS__
//#define UsingDotNetTransforms
//#endif

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Xml.Serialization;
using System.Windows.Forms;
#if __MonoCS__
using System.Xml.Xsl;
#endif

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for XmlUtils.
	/// </summary>
	public class XmlUtils
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public XmlUtils()
		{
		}

		/// <summary>
		/// Returns true if value of attrName is 'true' or 'yes' (case ignored)
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="attrName">The optional attribute to find.</param>
		/// <returns></returns>
		public static bool GetBooleanAttributeValue(XmlNode node, string attrName)
		{
			return GetBooleanAttributeValue(GetOptionalAttributeValue(node, attrName));
		}

		/// <summary>
		/// Returns true if sValue is 'true' or 'yes' (case ignored)
		/// </summary>
		public static bool GetBooleanAttributeValue(string sValue)
		{
			return (sValue != null
				&& (sValue.ToLowerInvariant().Equals("true")
				|| sValue.ToLowerInvariant().Equals("yes")));
		}

		/// <summary>
		/// Returns a integer obtained from the (mandatory) attribute named.
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="attrName">The mandatory attribute to find.</param>
		/// <returns>The value, or 0 if attr is missing.</returns>
		public static int GetMandatoryIntegerAttributeValue(XmlNode node, string attrName)
		{
			return Int32.Parse(GetManditoryAttributeValue(node, attrName), CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Return an optional integer attribute value, or if not found, the default value.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultVal"></param>
		/// <returns></returns>
		public static int GetOptionalIntegerValue(XmlNode node, string attrName, int defaultVal)
		{
			string val = GetOptionalAttributeValue(node, attrName);
			if (string.IsNullOrEmpty(val))
				return defaultVal;
			return Int32.Parse(val, CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Retrieve an array, given an attribute consisting of a comma-separated list of integers
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <returns></returns>
		public static int[] GetMandatoryIntegerListAttributeValue(XmlNode node, string attrName)
		{
			string input = GetManditoryAttributeValue(node, attrName);
			string[] vals = input.Split(',');
			int[] result = new int[vals.Length];
			for (int i = 0; i < vals.Length; i++)
				result[i] = Int32.Parse(vals[i], CultureInfo.InvariantCulture);
			return result;
		}

		/// <summary>
		/// Retrieve an array, given an attribute consisting of a comma-separated list of integers
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <returns></returns>
		public static uint[] GetMandatoryUIntegerListAttributeValue(XmlNode node, string attrName)
		{
			string input = GetManditoryAttributeValue(node, attrName);
			string[] vals = input.Split(',');
			uint[] result = new uint[vals.Length];
			for (int i = 0; i < vals.Length; i++)
				result[i] = UInt32.Parse(vals[i]);
			return result;
		}

		/// <summary>
		/// Make a value suitable for GetMandatoryIntegerListAttributeValue to parse.
		/// </summary>
		/// <param name="vals"></param>
		/// <returns></returns>
		public static string MakeIntegerListValue(int[] vals)
		{
			StringBuilder builder = new StringBuilder(vals.Length * 7); // enough unless VERY big numbers
			for (int i = 0; i < vals.Length; i++)
			{
				if (i != 0)
					builder.Append(",");
				builder.Append(vals[i].ToString());
			}
			return builder.ToString();
		}

		/// <summary>
		/// Make a comma-separated list of the ToStrings of the values in the list.
		/// </summary>
		/// <param name="vals"></param>
		/// <returns></returns>
		public static string MakeListValue(List<int> vals)
		{
			StringBuilder builder = new StringBuilder(vals.Count * 7); // enough unless VERY big numbers
			for (int i = 0; i < vals.Count; i++)
			{
				if (i != 0)
					builder.Append(",");
				builder.Append(vals[i].ToString());
			}
			return builder.ToString();
		}

		/// <summary>
		/// Make a comma-separated list of the ToStrings of the values in the list.
		/// </summary>
		/// <param name="vals"></param>
		/// <returns></returns>
		public static string MakeListValue(List<uint> vals)
		{
			StringBuilder builder = new StringBuilder(vals.Count * 7); // enough unless VERY big numbers
			for (int i = 0; i < vals.Count; i++)
			{
				if (i != 0)
					builder.Append(",");
				builder.Append(vals[i].ToString());
			}
			return builder.ToString();
		}

		/// <summary>
		/// Get an optional attribute value from an XmlNode.
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="attrName">The attribute to find.</param>
		/// <param name="defaultValue"></param>
		/// <returns>The value of the attribute, or the default value, if the attribute dismissing</returns>
		public static bool GetOptionalBooleanAttributeValue(XmlNode node, string attrName, bool defaultValue)
		{
			return GetBooleanAttributeValue(GetOptionalAttributeValue(node, attrName, defaultValue?"true":"false"));
		}

		/// <summary>
		/// Deprecated: use GetOptionalAttributeValue instead.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultValue"></param>
		/// <returns></returns>
		public static string GetAttributeValue(XmlNode node, string attrName, string defaultValue)
		{
			return GetOptionalAttributeValue(node, attrName, defaultValue);
		}

		/// <summary>
		/// Get an optional attribute value from an XmlNode.
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="attrName">The attribute to find.</param>
		/// <returns>The value of the attribute, or null, if not found.</returns>
		public static string GetAttributeValue(XmlNode node, string attrName)
		{
			return XmlUtils.GetOptionalAttributeValue(node, attrName);
		}

		/// <summary>
		/// Get an optional attribute value from an XmlNode.
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="attrName">The attribute to find.</param>
		/// <returns>The value of the attribute, or null, if not found.</returns>
		public static string GetOptionalAttributeValue(XmlNode node, string attrName)
		{
			return GetOptionalAttributeValue(node, attrName, null);
		}

		/// <summary>
		/// Get an optional attribute value from an XmlNode.
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="attrName">The attribute to find.</param>
		/// <returns>The value of the attribute, or null, if not found.</returns>
		public static string GetOptionalAttributeValue(XmlNode node, string attrName, string defaultString)
		{
			if (node != null && node.Attributes != null)
			{
				XmlAttribute xa = node.Attributes[attrName];
				if (xa != null)
					return xa.Value;
			}
			return defaultString;
		}

		/// <summary>
		/// Get an optional attribute value from an XmlNode, and look up its localized value in the
		/// given StringTable.
		/// </summary>
		/// <param name="tbl"></param>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultString"></param>
		/// <returns></returns>
		public static string GetLocalizedAttributeValue(StringTable tbl, XmlNode node,
			string attrName, string defaultString)
		{
			string sValue = GetOptionalAttributeValue(node, attrName, defaultString);
			if (tbl == null)
				return sValue;
			else
				return tbl.LocalizeAttributeValue(sValue);
		}

		/// <summary>
		/// Return the node that has the desired 'name', either the input node or a decendent.
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="name">The XmlNode name to find.</param>
		/// <returns></returns>
		public static XmlNode FindNode(XmlNode node, string name)
		{
			if (node.Name == name)
				return node;
			foreach (XmlNode childNode in node.ChildNodes)
			{
				if (childNode.Name == name)
					return childNode;
				XmlNode n = FindNode(childNode, name);
				if (n != null)
					return n;
			}
			return null;
		}

		/// <summary>
		/// Get an obligatory attribute value.
		/// </summary>
		/// <param name="node">The XmlNode to look in.</param>
		/// <param name="attrName">The required attribute to find.</param>
		/// <returns>The value of the attribute.</returns>
		/// <exception cref="ApplicationException">
		/// Thrown when the value is not found in the node.
		/// </exception>
		public static string GetManditoryAttributeValue(XmlNode node, string attrName)
		{
			string retval = XmlUtils.GetOptionalAttributeValue(node, attrName, null);
			if (retval == null)
			{
				throw new ApplicationException("The attribute'"
					+ attrName
					+ "' is mandatory, but was missing. "
					+ node.OuterXml);
			}
			return retval;
		}

		/// <summary>
		/// Append an attribute with the specified name and value to parent.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="attrName"></param>
		/// <param name="attrVal"></param>
		public static void AppendAttribute(XmlNode parent, string attrName, string attrVal)
		{
			XmlAttribute xa = parent.OwnerDocument.CreateAttribute(attrName);
			xa.Value = attrVal;
			parent.Attributes.Append(xa);
		}

		/// <summary>
		/// Change the value of the specified attribute, appending it if not already present.
		/// </summary>
		public static void SetAttribute(XmlNode parent, string attrName, string attrVal)
		{
			XmlAttribute xa = parent.Attributes[attrName];
			if (xa != null)
				xa.Value = attrVal;
			else
				AppendAttribute(parent, attrName, attrVal);
		}

		/// <summary>
		/// Append an attribute with the specified name and value to parent.
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="attrName"></param>
		/// <param name="attrVal"></param>
		public static XmlElement AppendElement(XmlNode parent, string elementName)
		{
			XmlElement xe = parent.OwnerDocument.CreateElement(elementName);
			parent.AppendChild(xe);
			return xe;
		}
		/// <summary>
		/// Return true if the two nodes match. Corresponding children should match, and
		/// corresponding attributes (though not necessarily in the same order).
		/// The nodes are expected to be actually XmlElements; not tested for other cases.
		/// Comments do not affect equality.
		/// </summary>
		/// <param name="node1"></param>
		/// <param name="node2"></param>
		/// <returns></returns>
		static public bool NodesMatch(XmlNode node1, XmlNode node2)
		{
			if (node1 == null && node2 == null)
				return true;
			if (node1 == null || node2 == null)
				return false;
			if (node1.Name != node2.Name)
				return false;
			if (node1.InnerText != node2.InnerText)
				return false;
			if (node1.Attributes == null && node2.Attributes != null)
				return false;
			if (node1.Attributes != null && node2.Attributes == null)
				return false;
			if (node1.Attributes != null)
			{
				if (node1.Attributes.Count != node2.Attributes.Count)
					return false;
				for (int i = 0; i < node1.Attributes.Count; i++)
				{
					XmlAttribute xa1 = node1.Attributes[i];
					XmlAttribute xa2 = node2.Attributes[xa1.Name];
					if (xa2 == null || xa1.Value != xa2.Value)
						return false;
				}
			}
			if (node1.ChildNodes == null && node2.ChildNodes != null)
				return false;
			if (node1.ChildNodes != null && node2.ChildNodes == null)
				return false;
			if (node1.ChildNodes != null)
			{
				int ichild1 = 0; // index node1.ChildNodes
				int ichild2 = 0; // index node2.ChildNodes
				while (ichild1 < node1.ChildNodes.Count && ichild2 < node1.ChildNodes.Count)
				{
					XmlNode child1 = node1.ChildNodes[ichild1];

					// Note that we must defer doing the 'continue' until after we have checked to see if both children are comments
					// If we continue immediately and the last node of both elements is a comment, the second node will not have
					// ichild2 incremented and the final test will fail.
					bool foundComment = false;

					if (child1 is XmlComment)
					{
						ichild1++;
						foundComment = true;
					}
					XmlNode child2 = node2.ChildNodes[ichild2];
					if (child2 is XmlComment)
					{
						ichild2++;
						foundComment = true;
					}

					if (foundComment)
						continue;

					if (!NodesMatch(child1, child2))
						return false;
					ichild1++;
					ichild2++;
				}
				// If we finished both lists we got a match.
				return ichild1 == node1.ChildNodes.Count && ichild2 == node2.ChildNodes.Count;
			}
			else
			{
				// both lists are null
				return true;
			}
		}

		/// <summary>
		/// Return the first child of the node that is not a comment (or null).
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static XmlNode GetFirstNonCommentChild(XmlNode node)
		{
			if (node == null)
				return null;
			foreach(XmlNode child in node.ChildNodes)
				if (!(child is XmlComment))
					return child;
			return null;
		}
		/// <summary>
		/// Apply an XSLT transform on a DOM to produce a resulting file
		/// </summary>
		/// <param name="sTransformName">full path name of the XSLT transform</param>
		/// <param name="inputDOM">XmlDocument DOM containing input to be transformed</param>
		/// <param name="sOutputName">full path of the resulting output file</param>
		public static void TransformDomToFile(string sTransformName, XmlDocument inputDOM, string sOutputName)
		{
			string sTempInput = FileUtils.GetTempFile("xml");
			try
			{
				inputDOM.Save(sTempInput);
				TransformFileToFile(sTransformName, sTempInput, sOutputName);
			}
			finally
			{
				if (File.Exists(sTempInput))
					File.Delete(sTempInput);
			}
		}
		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="sTransformName">full path name of the XSLT transform</param>
		/// <param name="sInputPath">full path of the input file</param>
		/// <param name="sOutputName">full path of the resulting output file</param>
		public static void TransformFileToFile(string sTransformName, string sInputPath, string sOutputName)
		{
			TransformFileToFile(sTransformName, null, sInputPath, sOutputName);
		}

		/// <summary>
		/// Convert an encoded string (safe XML) into plain text.
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>
		public static string DecodeXml(string sInput)
		{
			string sOutput = sInput;

			if (!String.IsNullOrEmpty(sOutput))
			{
				sOutput = sOutput.Replace("&amp;", "&");
				sOutput = sOutput.Replace("&lt;", "<");
				sOutput = sOutput.Replace("&gt;", ">");
			}
			return sOutput;
		}

		/// <summary>
		/// Fix the string to be safe in a text region of XML.
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>
		public static string MakeSafeXml(string sInput)
		{
			string sOutput = sInput;

			if (!String.IsNullOrEmpty(sOutput))
			{
				sOutput = sOutput.Replace("&", "&amp;");
				sOutput = sOutput.Replace("<", "&lt;");
				sOutput = sOutput.Replace(">", "&gt;");
			}
			return sOutput;
		}

		[SuppressMessage("Gendarme.Rules.Portability", "NewLineLiteralRule",
			Justification="Replacing new line characters")]
		public static string ConvertMultiparagraphToSafeXml(string sInput)
		{
			string sOutput = sInput;

			if (!String.IsNullOrEmpty(sOutput))
			{
				sOutput = sOutput.Replace(Environment.NewLine, "\u2028");
				sOutput = sOutput.Replace("\n", "\u2028");
				sOutput = sOutput.Replace("\r", "\u2028");
				sOutput = MakeSafeXml(sOutput);
			}
			return sOutput;
		}

		/// <summary>
		/// Fix the string to be safe in an attribute value of XML.
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>
		public static string MakeSafeXmlAttribute(string sInput)
		{
			string sOutput = sInput;

			if (!String.IsNullOrEmpty(sOutput))
			{
				sOutput = sOutput.Replace("&", "&amp;");
				sOutput = sOutput.Replace("\"", "&quot;");
				sOutput = sOutput.Replace("'", "&apos;");
				sOutput = sOutput.Replace("<", "&lt;");
				sOutput = sOutput.Replace(">", "&gt;");
				for (int i = 0; i < sOutput.Length; ++i)
				{
					if (Char.IsControl(sOutput, i))
					{
						char c = sOutput[i];
						string sReplace = String.Format("&#x{0:X};", (int)c);
						sOutput = sOutput.Replace(c.ToString(), sReplace);
						i += (sReplace.Length - 1);		// skip over the replacement string.
					}
				}
			}
			return sOutput;
		}

		/// <summary>
		/// Convert an encoded attribute string into plain text.
		/// </summary>
		/// <param name="sInput"></param>
		/// <returns></returns>
		public static string DecodeXmlAttribute(string sInput)
		{
			string sOutput = sInput;
			if (!String.IsNullOrEmpty(sOutput) && sOutput.Contains("&"))
			{
				sOutput = sOutput.Replace("&gt;", ">");
				sOutput = sOutput.Replace("&lt;", "<");
				sOutput = sOutput.Replace("&apos;", "'");
				sOutput = sOutput.Replace("&quot;", "\"");
				sOutput = sOutput.Replace("&amp;", "&");
			}
			for (int idx = sOutput.IndexOf("&#"); idx >= 0; idx = sOutput.IndexOf("&#"))
			{
				int idxEnd = sOutput.IndexOf(';', idx);
				if (idxEnd < 0)
					break;
				string sOrig = sOutput.Substring(idx, (idxEnd - idx) + 1);
				string sNum = sOutput.Substring(idx + 2, idxEnd - (idx + 2));
				string sReplace = null;
				int chNum = 0;
				if (sNum[0] == 'x' || sNum[0] == 'X')
				{
					if (Int32.TryParse(sNum.Substring(1), NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out chNum))
						sReplace = Char.ConvertFromUtf32(chNum);
				}
				else
				{
					if (Int32.TryParse(sNum, out chNum))
						sReplace = Char.ConvertFromUtf32(chNum);
				}
				if (sReplace == null)
					sReplace = sNum;
				sOutput = sOutput.Replace(sOrig, sReplace);
			}
			return sOutput;
		}

		/// <summary>
		/// build an xpath to the given node in its document.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static string GetXPathInDocument(XmlNode node)
		{
			if (node == null || node.NodeType != XmlNodeType.Element)
				return "";
			//XmlNode parent = node.ParentNode;
			// start with the name of the node, and tentatively guess it to be the root element.
			string xpath = String.Format("/{0}", node.LocalName);
			// append the index of the node amongst any preceding siblings.
			int index = GetIndexAmongSiblings(node);
			if (index != -1)
			{
				index = index + 1; // add one for an xpath index.
				xpath += String.Format("[{0}]", index);
			}
			return String.Concat(GetXPathInDocument(node.ParentNode), xpath);
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="node"></param>
		/// <returns>Zero-based Index of Node in ParentNode.ChildNodes. -1, if node has no parent.</returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static int GetIndexAmongSiblings(XmlNode node)
		{
			XmlNode parent = node.ParentNode;
			if (parent != null)
			{
				return node.SelectNodes("./preceding-sibling::" + node.LocalName).Count;
			}
			return -1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the index of the node in nodes that 'matches' the target node.
		/// Return -1 if not found.
		/// </summary>
		/// <param name="nodes">The nodes.</param>
		/// <param name="target">The target.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int FindIndexOfMatchingNode(IEnumerable<XmlNode> nodes, XmlNode target)
		{
			int index = 0;
			foreach (XmlNode node in nodes)
			{
				if (XmlUtils.NodesMatch(node, target))
					return index;
				index++;
			}
			return -1;
		}

		/// <summary>
		/// return the deep clone of the given node, in a clone of its document context.
		/// </summary>
		/// <param name="node"></param>
		/// <returns></returns>
		public static XmlNode CloneNodeWithDocument(XmlNode node)
		{
			if (node == null)
				return null;
			// get the xpath of the node in its document
			if (node.NodeType != XmlNodeType.Document)
			{
				string xpath = GetXPathInDocument(node);
				XmlNode clonedOwner = node.OwnerDocument.CloneNode(true);
				return clonedOwner.SelectSingleNode(xpath);
			}
			else
			{
				return node.CloneNode(true);
			}
		}

		#region Serialize/Deserialize

		/// <summary>
		/// Try to serialize the given object into an xml string
		/// </summary>
		/// <param name="objToSerialize"></param>
		/// <returns>empty string if couldn't serialize object</returns>
		public static string SerializeObjectToXmlString(object objToSerialize)
		{
			string settingsXml = "";
			using (MemoryStream stream = new MemoryStream())
			{
				using (XmlTextWriter textWriter = new XmlTextWriter(stream, Encoding.UTF8))
				{
					XmlSerializer xmlSerializer = new XmlSerializer(objToSerialize.GetType());
					xmlSerializer.Serialize(textWriter, objToSerialize);
					textWriter.Flush();
					stream.Seek(0, SeekOrigin.Begin);
					XmlDocument doc = new XmlDocument();
					doc.Load(stream);
					settingsXml = doc.OuterXml;
				}
			}
			return settingsXml;
		}

		/// <summary>
		/// Deserialize the given xml string into an object of targetType class
		/// </summary>
		/// <param name="xml"></param>
		/// <param name="targetType"></param>
		/// <returns>null if we didn't deserialize the object</returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		static public object DeserializeXmlString(string xml, Type targetType)
		{
			// TODO-Linux: System.Boolean System.Type::op_{Ine,E}quality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			if (String.IsNullOrEmpty(xml) || targetType == null)
				return null;
			using (MemoryStream stream = new MemoryStream())
			{
				using (XmlTextWriter textWriter = new XmlTextWriter(stream, Encoding.UTF8))
				{
					XmlDocument doc = new XmlDocument();
					doc.LoadXml(xml);
					// get the type from the xml itself.
					// if we can find an existing class/type, we can try to deserialize to it.
					if (targetType != null)
					{
						doc.WriteContentTo(textWriter);
						textWriter.Flush();
						stream.Seek(0, SeekOrigin.Begin);
						XmlSerializer xmlSerializer = new XmlSerializer(targetType);
						try
						{
							object settings = xmlSerializer.Deserialize(stream);
							return settings;
						}
						catch
						{
							// something went wrong trying to deserialize the xml
							// perhaps the structure of the stored data no longer matches the class
						}
					}
				}
			}
			return null;
		}

		#endregion Serialize/Deserialize


		/// <summary>
		/// Utility function to find a methodInfo for the named method.
		/// It is a static method of the class specified in the EditRowClass of the EditRowAssembly.
		/// </summary>
		/// <param name="methodName"></param>
		/// <returns></returns>
		public static System.Reflection.MethodInfo GetStaticMethod(XmlNode node, string sAssemblyAttr, string sClassAttr,
			string sMethodName, out System.Type typeFound)
		{
			string sAssemblyName = XmlUtils.GetAttributeValue(node, sAssemblyAttr);
			string sClassName = XmlUtils.GetAttributeValue(node, sClassAttr);
			System.Reflection.MethodInfo mi = GetStaticMethod(sAssemblyName, sClassName, sMethodName,
				"node " + node.OuterXml, out typeFound);
			return mi;
		}
		/// <summary>
		/// Utility function to find a methodInfo for the named method.
		/// It is a static method of the class specified in the EditRowClass of the EditRowAssembly.
		/// </summary>
		/// <param name="methodName"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Portability", "MonoCompatibilityReviewRule",
			Justification="See TODO-Linux comment")]
		public static System.Reflection.MethodInfo GetStaticMethod(string sAssemblyName, string sClassName,
			string sMethodName, string sContext, out System.Type typeFound)
		{
			typeFound = null;
			System.Reflection.Assembly assemblyFound = null;
			try
			{
				string baseDir = System.IO.Path.GetDirectoryName(
					System.Reflection.Assembly.GetExecutingAssembly().CodeBase).
					Substring(MiscUtils.IsUnix ? 5 : 6);
				assemblyFound = System.Reflection.Assembly.LoadFrom(
					System.IO.Path.Combine(baseDir, sAssemblyName));
			}
			catch (Exception error)
			{
				string sMainMsg = "DLL at " + sAssemblyName;
				string sMsg = MakeGetStaticMethodErrorMessage(sMainMsg, sContext);
				throw new RuntimeConfigurationException(sMsg, error);
			}
			Debug.Assert(assemblyFound != null);
			try
			{
				typeFound = assemblyFound.GetType(sClassName);
			}
			catch (Exception error)
			{
				string sMainMsg = "class called " + sClassName;
				string sMsg = MakeGetStaticMethodErrorMessage(sMainMsg, sContext);
				throw new RuntimeConfigurationException(sMsg, error);
			}
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type)
			// is marked with [MonoTODO] and might not work as expected in 4.0.
			Debug.Assert(typeFound != null);
			System.Reflection.MethodInfo mi = null;
			try
			{
				mi = typeFound.GetMethod(sMethodName);
			}
			catch (Exception error)
			{
				string sMainMsg = "method called " + sMethodName + " of class " + sClassName +
					" in assembly " + sAssemblyName;
				string sMsg = MakeGetStaticMethodErrorMessage(sMainMsg, sContext);
				throw new RuntimeConfigurationException(sMsg, error);
			}
			return mi;
		}
		static protected string MakeGetStaticMethodErrorMessage(string sMainMsg, string sContext)
		{
			string sResult = "GetStaticMethod() could not find the " + sMainMsg +
				" while processing " + sContext;
			return sResult;
		}

		/// <summary>
		/// Apply an XSLT transform on a file to produce a resulting file
		/// </summary>
		/// <param name="sTransformName">full path name of the XSLT transform</param>
		/// <param name="parameterList">list of parameters to pass to the transform</param>
		/// <param name="sInputPath">full path of the input file</param>
		/// <param name="sOutputName">full path of the resulting output file</param>
		public static void TransformFileToFile(string sTransformName, XSLParameter[] parameterList, string sInputPath, string sOutputName)
		{
#if DEBUG
			Debug.WriteLine("Transform: " + sTransformName + " input file: " + sInputPath);
			DateTime start = DateTime.Now;
			Debug.WriteLine("\tStarting at: " + start.TimeOfDay.ToString());
#endif
#if UsingDotNetTransforms
			// set up transform
			XslCompiledTransform transformer = new XslCompiledTransform();
			transformer.Load(sTransformName);

			// add any parameters
			XsltArgumentList args;
			AddParameters(out args, parameterList);

			// setup output file
			using (var writer = File.CreateText(sOutputName))
			{
				// load input file
				using (var reader = new XmlTextReader(sInputPath))
				{
#if !__MonoCS__
					reader.DtdProcessing = DtdProcessing.Parse;
#else
					reader.ProhibitDtd = false;
#endif
					reader.EntityHandling = EntityHandling.ExpandEntities;

					// Apply transform
					transformer.Transform(reader, args, writer);
				}
			}
#else // not UsingDotNetTransforms
#if __MonoCS__
			if (parameterList != null)
			{
				foreach(XSLParameter rParam in parameterList)
				{
					// Following is a specially recognized parameter name
					if (rParam.Name == "prmSDateTime")
					{
						rParam.Value = GetCurrentDateTime();
					}
				}
			}
			SIL.Utils.LibXslt.TransformFileToFile(sTransformName, parameterList, sInputPath, sOutputName);
#else
			//.Net framework XML transform is still slower than something like MSXML2
			// (this is so especially for transforms using xsl:key).
			MSXML2.XSLTemplate60Class xslt = new MSXML2.XSLTemplate60Class();
			MSXML2.FreeThreadedDOMDocument60Class xslDoc = new
				MSXML2.FreeThreadedDOMDocument60Class();
			MSXML2.DOMDocument60Class xmlDoc = new MSXML2.DOMDocument60Class();
			MSXML2.IXSLProcessor xslProc;

			xslDoc.async = false;
			xslDoc.setProperty("ResolveExternals", true);
			xslDoc.setProperty("ProhibitDTD", false);
			xslDoc.load(sTransformName);
			xslt.stylesheet = xslDoc;
			xmlDoc.setProperty("ResolveExternals", true);
			xmlDoc.setProperty("ProhibitDTD", false);
			xmlDoc.async = false;
			var fOk = xmlDoc.load(sInputPath);
			if (!fOk)
			{
				var msg = String.Format(XmlUtilsStrings.ksXmlFileIsInvalid, sInputPath);
				MessageBox.Show(msg, XmlUtilsStrings.ksWarning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
			}
			xslProc = xslt.createProcessor();
			xslProc.input = xmlDoc;
			AddParameters(parameterList, xslProc);
			xslProc.transform();
			using (StreamWriter sr = File.CreateText(sOutputName))
			{
				sr.Write(xslProc.output);
				sr.Close();
			}
#endif // __MonoCS__
#endif // UsingDotNetTransforms
#if DEBUG
			DateTime end = DateTime.Now;
			Debug.WriteLine("\tEnding at: " + end.TimeOfDay.ToString());
			System.TimeSpan diff = end.Subtract(start);
			Debug.WriteLine("\tProcess took: " + diff.ToString() + " " + sOutputName);
#endif
		}

#if UsingDotNetTransforms
		static private void AddParameters(out XsltArgumentList args, XSLParameter[] parameterList)
		{
			args = new XsltArgumentList();
			if (parameterList != null)
			{
				foreach(XSLParameter rParam in parameterList)
				{
					// Following is a specially recognized parameter name
					if (rParam.Name == "prmSDateTime")
					{
						args.AddParam(rParam.Name, "", GetCurrentDateTime());
					}
					else
						args.AddParam(rParam.Name, "", rParam.Value);
				}
			}
		}
#else
#if !__MonoCS__
		/// <summary>
		/// Add parameters to a transform
		/// </summary>
		/// <param name="parameterList"></param>
		/// <param name="xslProc"></param>
		private static void AddParameters(XSLParameter[] parameterList, MSXML2.IXSLProcessor xslProc)
		{
			if (parameterList != null)
			{
				foreach(XSLParameter rParam in parameterList)
				{
					// Following is a specially recognized parameter name
					if (rParam.Name == "prmSDateTime")
					{
						xslProc.addParameter(rParam.Name, GetCurrentDateTime(), "");
					}
					else
						xslProc.addParameter(rParam.Name, rParam.Value, "");
				}
			}
		}
#endif
#endif // UsingDotNetTransforms
		/// <summary>
		/// Are we using the .Net XSLT transforms?
		/// </summary>
		/// <returns>true if we're using .Net XSLT transforms
		/// false if we're using MSXML2 or LibXslt</returns>
		public static bool UsingDotNetTransforms()
		{
#if UsingDotNetTransforms
			return true;
#else
			return false;
#endif
		}

		/// <summary>
		/// Are we using the Microsoft's MSXML2 XSLT transforms?
		/// </summary>
		/// <returns>true if we're using MSXML2 XSLT transforms
		/// false if we're using .Net or LibXslt</returns>
		public static bool UsingMSXML2Transforms()
		{
#if UsingDotNetTransforms
			return false;
#else
#if __MonoCS__
			return false;
#else
			return true;
#endif
#endif
		}

		/// <summary>
		/// Are we using the libxslt.so XSLT transforms?
		/// </summary>
		/// <returns>true if we're using libxslt.so transforms
		/// false if we're using MSXML2 or .Net</returns>
		public static bool UsingLibXsltTransforms()
		{
#if UsingDotNetTransforms
			return false;
#else
#if __MonoCS__
			return true;
#else
			return false;
#endif
#endif
		}

		private static string GetCurrentDateTime()
		{
			DateTime now;
			now = DateTime.Now;
			return (now.ToShortDateString() + " " + now.ToLongTimeString());
		}
		/// <summary>
		/// A class that represents a parameter of an XSL stylesheet.
		/// </summary>
		public class XSLParameter
		{
			/// <summary>
			/// Parameter name.
			/// </summary>
			private string m_name;

			/// <summary>
			/// Parameter value.
			/// </summary>
			private string m_value;

			public XSLParameter(string sName, string sValue)
			{
				m_name = sName;
				m_value = sValue;
			}

			public string Name
			{
				get { return m_name; }
				set { m_name = value; }
			}

			public string Value
			{
				get { return m_value; }
				set { m_value = value; }
			}
		}

		/// <summary>
		/// Allow the visitor to 'visit' each attribute in the input XmlNode.
		/// </summary>
		/// <param name="input"></param>
		/// <param name="result"></param>
		/// <returns>true if any Visit call returns true</returns>
		public static bool VisitAttributes(XmlNode input, IAttributeVisitor visitor)
		{
			bool fSuccessfulVisit = false;
			if (input.Attributes != null) // can be, e.g, if Input is a XmlTextNode
			{
				foreach (XmlAttribute xa in input.Attributes)
				{
					if (visitor.Visit(xa))
						fSuccessfulVisit = true;
				}
			}
			if (input.ChildNodes != null) // not sure whether this can happen.
			{
				foreach (XmlNode child in input.ChildNodes)
				{
					if (VisitAttributes(child, visitor))
						fSuccessfulVisit = true;
				}
			}
			return fSuccessfulVisit;
		}
	}

	/// <summary>
	/// Superclass for operations we can apply to attributes.
	/// </summary>
	public interface IAttributeVisitor
	{
		bool Visit(XmlAttribute xa);
	}

	public class ReplaceSubstringInAttr : IAttributeVisitor
	{
		string m_pattern;
		string m_replacement;
		public ReplaceSubstringInAttr(string pattern, string replacement)
		{
			m_pattern = pattern;
			m_replacement = replacement;
		}
		public virtual bool Visit(XmlAttribute xa)
		{
			string old = xa.Value;
			int index = old.IndexOf(m_pattern);
			if (index < 0)
				return false;
			xa.Value = old.Replace(m_pattern, m_replacement);
			return false;
		}
	}
}
