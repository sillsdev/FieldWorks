// Copyright (c) 2003-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// <remarks>
// This makes available some utilities for handling XML Nodes
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Xml;
using System.Globalization;
using System.Xml.Serialization;
using System.Xml.Xsl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Utils;

namespace SIL.Utils
{
	/// <summary>
	/// Summary description for XmlUtils.
	/// </summary>
	public static class XmlUtils
	{
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
			return Int32.Parse(GetMandatoryAttributeValue(node, attrName), CultureInfo.InvariantCulture);
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
			string input = GetMandatoryAttributeValue(node, attrName);
			string[] vals = input.Split(',');
			var result = new int[vals.Length];
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
			string input = GetMandatoryAttributeValue(node, attrName);
			string[] vals = input.Split(',');
			var result = new uint[vals.Length];
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
			var builder = new StringBuilder(vals.Length * 7); // enough unless VERY big numbers
			for (int i = 0; i < vals.Length; i++)
			{
				if (i != 0)
					builder.Append(",");
				builder.Append(vals[i].ToString(CultureInfo.InvariantCulture));
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
			var builder = new StringBuilder(vals.Count * 7); // enough unless VERY big numbers
			for (int i = 0; i < vals.Count; i++)
			{
				if (i != 0)
					builder.Append(",");
				builder.Append(vals[i].ToString(CultureInfo.InvariantCulture));
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
			var builder = new StringBuilder(vals.Count * 7); // enough unless VERY big numbers
			for (int i = 0; i < vals.Count; i++)
			{
				if (i != 0)
					builder.Append(",");
				builder.Append(vals[i].ToString(CultureInfo.InvariantCulture));
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
			return GetOptionalAttributeValue(node, attrName);
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
		/// <param name="defaultString"></param>
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
			return tbl.LocalizeAttributeValue(sValue);
		}

		/// <summary>
		/// Get an optional attribute value from an XmlNode, and look up its localized value in the
		/// standard StringTable.
		/// </summary>
		/// <param name="node"></param>
		/// <param name="attrName"></param>
		/// <param name="defaultString"></param>
		/// <returns></returns>
		public static string GetLocalizedAttributeValue(XmlNode node,
			string attrName, string defaultString)
		{
			return GetLocalizedAttributeValue(StringTable.Table, node, attrName, defaultString);
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
		public static string GetMandatoryAttributeValue(XmlNode node, string attrName)
		{
			string retval = GetOptionalAttributeValue(node, attrName, null);
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
		/// <param name="elementName"></param>
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

			// both lists are null
			return true;
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

		/// <summary>
		/// Convert a possibly multiparagraph string to a form that is safe to store both in an XML file. Also deals
		/// with some characters that are not safe to put in an FDO field, which is more to the point since that's
		/// where most of this data is heading.
		/// </summary>
		/// <remarks>JohnT: escaping the XML characters is slightly bizarre, since LiftMerger is an IMPORT function and for the most part,
		/// we are not creating XML files. Most of the places we want to put this text, in FDO objects, we end up
		/// Decoding it again. Worth considering refactoring so that this method (renamed) just deals with characters
		/// we don't want in FDO objects, like tab and newline, and leaves the XML reserved characters alone. Then
		/// we could get rid of a lot of Decode statements also.
		/// Steve says one place we do need to make encoded XML is in the content of Residue fields.</remarks>
		/// <param name="sInput"></param>
		/// <returns></returns>
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
						sOutput = sOutput.Replace(c.ToString(CultureInfo.InvariantCulture), sReplace);
						i += (sReplace.Length - 1);		// skip over the replacement string.
					}
				}
			}
			return sOutput;
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
				if (NodesMatch(node, target))
					return index;
				index++;
			}
			return -1;
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
		public static MethodInfo GetStaticMethod(XmlNode node, string sAssemblyAttr, string sClassAttr,
			string sMethodName, out Type typeFound)
		{
			string sAssemblyName = GetAttributeValue(node, sAssemblyAttr);
			string sClassName = GetAttributeValue(node, sClassAttr);
			MethodInfo mi = GetStaticMethod(sAssemblyName, sClassName, sMethodName,
				"node " + node.OuterXml, out typeFound);
			return mi;
		}
		/// <summary>
		/// Utility function to find a methodInfo for the named method.
		/// It is a static method of the class specified in the EditRowClass of the EditRowAssembly.
		/// </summary>
		/// <param name="methodName"></param>
		/// <returns></returns>
		public static MethodInfo GetStaticMethod(string sAssemblyName, string sClassName,
			string sMethodName, string sContext, out Type typeFound)
		{
			typeFound = null;
			Assembly assemblyFound;
			try
			{
				string baseDir = Path.GetDirectoryName(
					Assembly.GetExecutingAssembly().CodeBase).
					Substring(MiscUtils.IsUnix ? 5 : 6);
				assemblyFound = Assembly.LoadFrom(
					Path.Combine(baseDir, sAssemblyName));
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
			MethodInfo mi;
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

		private static string MakeGetStaticMethodErrorMessage(string sMainMsg, string sContext)
		{
			string sResult = "GetStaticMethod() could not find the " + sMainMsg +
				" while processing " + sContext;
			return sResult;
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

		public static XslCompiledTransform CreateTransform(string xslName, string assemblyName)
		{
			var transform = new XslCompiledTransform();
			if (MiscUtils.IsDotNet)
			{
				// Assumes the XSL has been precompiled.  xslName is the name of the precompiled class
				Type type = Type.GetType(xslName + "," + assemblyName);
				Debug.Assert(type != null);
				transform.Load(type);
			}
			else
			{
				string libPath = Path.GetDirectoryName(FileUtils.StripFilePrefix(Assembly.GetExecutingAssembly().CodeBase));
				Assembly transformAssembly = Assembly.LoadFrom(Path.Combine(libPath, assemblyName + ".dll"));
				using (Stream stream = transformAssembly.GetManifestResourceStream(xslName + ".xsl"))
				{
					Debug.Assert(stream != null);
					using (XmlReader reader = XmlReader.Create(stream))
						transform.Load(reader, new XsltSettings(true, false), new XmlResourceResolver(transformAssembly));
				}
			}
			return transform;
		}

		private class XmlResourceResolver : XmlUrlResolver
		{
			private readonly Assembly m_assembly;

			public XmlResourceResolver(Assembly assembly)
			{
				m_assembly = assembly;
			}

			public override Uri ResolveUri(Uri baseUri, string relativeUri)
			{
				if (baseUri == null)
					return new Uri(string.Format("res://{0}", relativeUri));
				return base.ResolveUri(baseUri, relativeUri);
			}

			public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
			{
				switch (absoluteUri.Scheme)
				{
				case "res":
					return m_assembly.GetManifestResourceStream(absoluteUri.OriginalString.Substring(6));

				default:
					// Handle file:// and http://
					// requests from the XmlUrlResolver base class
					return base.GetEntity(absoluteUri, role, ofObjectToReturn);
				}
			}
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
