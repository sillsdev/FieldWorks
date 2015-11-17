// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Xml;
using Palaso.Code;

namespace Palaso.Xml
{
	public static class XmlNodeExtensions
	{

		/// <summary>
		/// this is safe to use with foreach, unlike SelectNodes
		/// </summary>
		public static XmlNodeList SafeSelectNodes(this XmlNode node, string path, XmlNamespaceManager namespaceManager)
		{
			var x = node.SelectNodes(path, namespaceManager);
			if (x == null)
				return new NullXMlNodeList();
			return x;
		}

		/// <summary>
		/// honors default namespace and will return an empty list rather than null
		/// </summary>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static XmlNodeList SafeSelectNodes(this XmlNode node, string path)
		{
			Guard.AgainstNull(node, "SafeSelectNodes(node,"+path+"): node was null");
			//REVIEW JH(jh): this will put pfx in front of every element in the path, but in html, that actually makes the queries fail.
			const string prefix = "pfx";
			XmlNamespaceManager nsmgr = GetNsmgr(node, prefix);
			string prefixedPath = GetPrefixedPath(path, prefix);
			var x= node.SelectNodes(prefixedPath, nsmgr);

			if (x == null)
				return new NullXMlNodeList();
			return x;
		}

		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static string SelectTextPortion(this XmlNode node, string path, params object[] args)
		{
			var x = node.SelectNodes(string.Format(path, args));
			if (x == null || x.Count ==0)
				return string.Empty;
			return x[0].InnerText;
		}

		public static string GetStringAttribute(this XmlNode node, string attr)
		{
			try
			{
				return node.Attributes[attr].Value;
			}
			catch (NullReferenceException)
			{
				throw new XmlFormatException(string.Format("Expected a '{0}' attribute on {1}.", attr, node.OuterXml));
			}
		}
		public static string GetOptionalStringAttribute(this XmlNode node, string attributeName, string defaultValue)
		{
			XmlAttribute attr = node.Attributes[attributeName];
			if (attr == null)
				return defaultValue;
			return attr.Value;
		}

		#region HonorDefaultNamespace  // from http://stackoverflow.com/questions/585812/using-xpath-with-default-namespace-in-c/2054877#2054877

		public static XmlNode SelectSingleNodeHonoringDefaultNS(this XmlNode node, string path)
		{
			const string prefix = "pfx";
			XmlNamespaceManager nsmgr = GetNsmgr(node, prefix);
			string prefixedPath = GetPrefixedPath(path, prefix);
			return node.SelectSingleNode(prefixedPath, nsmgr);
		}

		/// <summary>
		/// Search through an entire document for "xmlns" attributes that define prefixes,
		/// returns an XmlNamespaceManager able to interpret xpath for those prefixes.
		/// </summary>
		/// <param name="doc"></param>
		/// <returns></returns>
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "In .NET 4.5 XmlNodeList implements IDisposable, but not in 4.0.")]
		public static XmlNamespaceManager LoadNsmgrForDoc(XmlDocument doc)
		{
			var rootNode = doc.DocumentElement;
			var nsmgr = new XmlNamespaceManager(doc.NameTable);
			foreach (XmlNode node in rootNode.SelectNodes("//*"))
			{
				foreach (XmlAttribute attr in node.Attributes)
				{
					if (attr.Prefix != "xmlns")
						continue;
					var prefix = attr.LocalName;
					var urn = attr.Value;
					if (prefix.Length > 0)
					{
						var urnDefined = nsmgr.LookupNamespace(prefix);
						if (String.IsNullOrEmpty(urnDefined))
						{
							nsmgr.AddNamespace(prefix, urn);
						}

					}
				}
			}
			return nsmgr;
		}

		private static XmlNamespaceManager GetNsmgr(XmlNode node, string prefix)
		{
			Guard.AgainstNull(node, "GetNsmgr(node, prefix): node was null");
			string namespaceUri;
			XmlNameTable nameTable;
			try
			{
				if (node is XmlDocument)
				{
					nameTable = ((XmlDocument) node).NameTable;
					Guard.AgainstNull(((XmlDocument) node).DocumentElement, "((XmlDocument) node).DocumentElement");
					namespaceUri = ((XmlDocument) node).DocumentElement.NamespaceURI;
				}
				else
				{
					Guard.AgainstNull(node.OwnerDocument, "node.OwnerDocument");
					nameTable = node.OwnerDocument.NameTable;
					namespaceUri = node.NamespaceURI;
				}
				XmlNamespaceManager nsmgr = new XmlNamespaceManager(nameTable);
				nsmgr.AddNamespace(prefix, namespaceUri);
				return nsmgr;

			}
			catch (Exception error)
			{
				throw new ApplicationException("Could not create a namespace manager for the following node:" +
					Environment.NewLine + node.OuterXml, error);
			}
		}

		private static string GetPrefixedPath(string xPath, string prefix)
		{
			char[] validLeadCharacters = "@/".ToCharArray();
			char[] quoteChars = "\'\"".ToCharArray();

			List<string> pathParts = xPath.Split("/".ToCharArray()).ToList();
			string result = string.Join("/",
										pathParts.Select(
											x =>
											(string.IsNullOrEmpty(x) ||
											 x.IndexOfAny(validLeadCharacters) == 0 ||
											 (x.IndexOf(':') > 0 &&
											  (x.IndexOfAny(quoteChars) < 0 || x.IndexOfAny(quoteChars) > x.IndexOf(':'))))
												? x
												: prefix + ":" + x).ToArray());

			result = result.Replace(prefix + ":text()", "text()");//remove the pfx from the text()
			return result;
		}

		#endregion
	}


	public class NullXMlNodeList : XmlNodeList
	{
		public override XmlNode Item(int index)
		{
			throw new ArgumentOutOfRangeException();
		}

		public override IEnumerator GetEnumerator()
		{
			yield return null;
		}

		public override int Count
		{
			get { return 0; }
		}
	}
	public class XmlFormatException : ApplicationException
	{
		private string _filePath;
		public XmlFormatException(string message)
			: base(message)
		{
		}

		public string FilePath
		{
			get { return _filePath; }
			set { _filePath = value; }
		}
	}
}