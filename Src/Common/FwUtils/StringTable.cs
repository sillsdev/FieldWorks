// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.Xml;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Get strings according to the current culture from one or more XML files
	/// </summary>
	public class StringTable
	{
		private static StringTable s_singletonStringTable;

		private readonly string m_baseDirectory;
		private StringTable m_parent;
		private XDocument m_document;
		private string m_sWsLoaded;
		/// <summary>
		/// This table is keyed by the groupXPathFragment passed to GetStringWithXPath.
		/// The value is another Dictioanry, from the id string to the string value we want.
		/// </summary>
		private readonly Dictionary<string, Dictionary<string, string>> m_pathsToStrings = new Dictionary<string, Dictionary<string, string>>();

		/// <summary>
		/// Return the singleton StringTable instance.
		/// </summary>
		public static StringTable Table => s_singletonStringTable ?? (s_singletonStringTable = new StringTable(Path.Combine(FwDirectoryFinder.FlexFolder, "Configuration")));

		/// <summary>
		/// Create an instance of the StringTable.
		/// </summary>
		/// <param name="baseDirectory"></param>
		internal StringTable(string baseDirectory)
		{
			m_parent = null;
			m_baseDirectory = baseDirectory;
			string sWs = System.Globalization.CultureInfo.CurrentUICulture.Name;
			Load(sWs);
		}

		/// <summary>
		/// Load the strings for the given language/writing system/locale.
		/// </summary>
		/// <param name="sWs"></param>
		private void Load(string sWs)
		{
			try
			{
				// Always load the neutral English strings first so that every string has a
				// fallback definition.
				string path;
				if (m_sWsLoaded != "en")
				{
					path = ChooseStringFile(m_baseDirectory, "en");
					if (path == null)
						throw new FileNotFoundException("strings-en.xml does not exist in " + m_baseDirectory);
					m_document = XDocument.Load(path);
				}
				if (sWs != m_sWsLoaded)
				{
					path = ChooseStringFile(m_baseDirectory, sWs);
					if (path != null)
					{
						var doc2 = XDocument.Load(path);
						MergeCustomTable(doc2);
					}
				}
			}
			catch (FileNotFoundException ex)
			{
				throw;
			}
			catch (Exception error)
			{
				m_sWsLoaded = "";
				throw new ApplicationException("Problem loading the strings table file in " + m_baseDirectory, error);
			}
			FindParent(m_baseDirectory);
		}

		/// <summary>
		/// Reload the string table for a new language (writing system).
		/// </summary>
		/// <param name="sWs"></param>
		public void Reload(string sWs)
		{
			if (sWs == m_sWsLoaded)
				return;
			Load(sWs);
		}

		/// <summary>
		/// Merge some custom set of strings into this table.
		/// </summary>
		/// <param name="customTableDocument">XML Document that contains the custom strings.</param>
		public void MergeCustomTable(XDocument customTableDocument)
		{
			var customRoot = customTableDocument.Root;
			if (customRoot.Name != "strings")
				throw new ArgumentException("Invalid custom strings table.", "customTableDocument");

			// Loop through each group node in custom table document, and merge into main table.
			var root = m_document.Root;
			MergeCustomGroups(root, customRoot.Elements("group"));
			m_sWsLoaded += "+";		// Flag that we've modified the original set of strings.
		}

		private void MergeCustomGroups(XElement parentNode, IEnumerable<XElement> customGroupElementList)
		{
			if (customGroupElementList == null || !customGroupElementList.Any())
				return; // Stop recursing in this method.

			foreach (var customGroupNode in customGroupElementList)
			{
				string customGroupId = XmlUtils.GetManditoryAttributeValue(customGroupNode, "id");
				var srcMatchingGroupNode = parentNode.XPathSelectElement("group[@id='" + customGroupId + "']");
				if (srcMatchingGroupNode == null)
				{
					// Import the entire custom node.
					m_document.Root.Add(customGroupNode.Clone());
				}
				else
				{
					// 1. Import new strings, or override extant strings with custom strings.
					foreach (var customStringNode in customGroupNode.Elements("string"))
					{
						var customId = XmlUtils.GetManditoryAttributeValue(customStringNode, "id");
						var customTxt = GetTxtAtributeValue(customStringNode);
						var srcMatchingStringNode = srcMatchingGroupNode.XPathSelectElement("string[@id='" + customId + "']");
						if (srcMatchingStringNode == null)
						{
							// Import the new string into the extant group.
							srcMatchingGroupNode.Add(customStringNode.Clone());
						}
						else
						{
							// Replace the original value with the new value.
							// The 'txt' attribute is optional, but it will be added as a cpoy of the 'id' here, if needed.
							string srcTxt = XmlUtils.GetOptionalAttributeValue(srcMatchingStringNode, "txt");
							if (srcTxt == null)
								XmlUtils.SetAttribute(srcMatchingStringNode, "txt", customTxt);
							else
								srcMatchingStringNode.Attribute("txt").SetValue(customTxt);
						}
					}

					// 2. Group elements can be nested, so merge them, too.
					MergeCustomGroups(srcMatchingGroupNode, customGroupNode.Elements("group"));
				}
			}
		}

		private string GetTxtAtributeValue(XElement element)
		{
			return XmlUtils.GetOptionalAttributeValue(
				element,
				"txt",
				XmlUtils.GetManditoryAttributeValue(element, "id")); // 'id' is default, if no 'txt' attribute is present.
		}

		/// <summary>
		/// choose the best string file we can find for the given writing system/language.
		/// </summary>
		protected string ChooseStringFile(string baseDirectory, string sWs)
		{
			string path = Path.Combine(baseDirectory, String.Format("strings-{0}.xml", sWs));
			if (File.Exists(path))
			{
				m_sWsLoaded = sWs;
				return path;
			}
			else
			{
				// Try for the parent culture if the given one doesn't exist.  This is the
				// standard fallback ploy for localization...
				int idx = sWs.LastIndexOf('-');
				while (idx >= 0)
				{
					sWs = sWs.Substring(0, idx);
					path = Path.Combine(baseDirectory, String.Format("strings-{0}.xml", sWs));
					if (File.Exists(path))
					{
						m_sWsLoaded = sWs;
						return path;
					}
					idx = sWs.LastIndexOf('-');
				}
				return null;
			}
		}

		private void FindParent(string baseDirectory)
		{
			var node = m_document.Elements("strings").FirstOrDefault();
			if (node == null)
				throw new ApplicationException("Could not find the root node, <strings> in " + baseDirectory);


			string inheritPath = XmlUtils.GetOptionalAttributeValue(node, "inheritPath");
			if (!string.IsNullOrEmpty(inheritPath))
			{
				string path = Path.Combine(baseDirectory, inheritPath);
				m_parent = new StringTable(path);
			}
		}

		/// <summary>
		/// get a string out of the root node of the string table
		/// </summary>
		/// <param name="id"></param>
		/// <returns></returns>
		public string GetString(string id)
		{
			return GetString(id, "");
		}

		/// <summary>
		/// This is similar to GetStringWithXPath, but the path argument identifies a
		/// single root node, which contains string elemenents (and optionally others,
		/// which are ignored), each containing id and optionally txt elements.
		/// This is much more efficient than GetStringWithXPath when multiple items
		/// are wanted from the same root node, but only reliable when the path
		/// identifies a single root.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="rootXPathFragment"></param>
		/// <returns></returns>
		public string GetStringWithRootXPath(string id, string rootXPathFragment)
		{
			return GetStringWithRootXPath(id, rootXPathFragment, true);
		}

		/// <summary>
		/// This is similar to GetStringWithXPath, but the path argument identifies a
		/// single root node, which contains string elemenents (and optionally others,
		/// which are ignored), each containing id and optionally txt elements.
		/// This is much more efficient than GetStringWithXPath when multiple items
		/// are wanted from the same root node, but only reliable when the path
		/// identifies a single root.
		/// </summary>
		/// <param name="id"></param>
		/// <param name="rootXPathFragment"></param>
		/// <param name="fTrim"></param>
		/// <returns></returns>
		private string GetStringWithRootXPath(string id, string rootXPathFragment, bool fTrim)
		{
			if (fTrim)
				id = id.Trim();
			Dictionary<string, string> items;
			if (m_pathsToStrings.ContainsKey(rootXPathFragment))
			{
				items = m_pathsToStrings[rootXPathFragment];
			}
			else
			{
				string path = "strings/" + rootXPathFragment;
				if (path[path.Length - 1] == '/')
					path = path.Substring(0, path.Length - 1); // strip closing slash.
				var parent = m_document.XPathSelectElements(path).FirstOrDefault();
				items = new Dictionary<string, string>();
				m_pathsToStrings[rootXPathFragment] = items;
				if (parent != null)
				{
					foreach(var child in parent.Elements())
					{
						string idChild = XmlUtils.GetOptionalAttributeValue(child, "id");
						//if the txt attribute is missing, use the id attr,
						//as this is unacceptable shorthand to use for English entries.
						//e.g.: <string id="Anywhere"/> is equivalent to <string id="Anywhere" txt="Anywhere"/>
						string txt = XmlUtils.GetOptionalAttributeValue(child, "txt", idChild);
						if (child.Name == "string" && idChild != null)
							items[idChild] = txt;
					}
				}
			}
			if (items.ContainsKey(id))
				return items[id];

			if (m_parent != null)
				return m_parent.GetStringWithXPath(id, rootXPathFragment);
			return "*" + id + "*";
		}

		/// <summary>
		/// get a string out of the table, specifying an XML path to the group which contains the string
		/// </summary>
		/// <param name="id"></param>
		/// <param name="groupXPathFragment">this path should start *underneath* the root <strings/> node.
		///					e.g. "group[@id='linguistics']/group[@id='phonology']"
		///	</param>
		/// <returns></returns>
		public string GetStringWithXPath(string id, string groupXPathFragment)
		{
			id = id.Trim();
			var node = m_document.XPathSelectElement("strings/" + groupXPathFragment + "string[@id='" + id + "']");
			if (node == null)
			{
				if (m_parent != null)
				{
					return m_parent.GetStringWithXPath(id, groupXPathFragment);
				}
				//not found
				return "*" + id + "*";
			}
			return GetTxtAtributeValue(node);
		}

		/// <summary>
		/// get a string which is embedded in a group (or in a group inside of a group...)
		/// </summary>
		/// <example>
		///		GetStringInGroup("LexMajorEntry", "ClassNames");
		/// </example>
		/// <example>
		///		get one out of a group within a group:
		///		GetString("FooLexMajorEntry", "ClassNames/FooNames");
		/// </example>
		/// <param name="id">the ID parameter of the string you want</param>
		/// <param name="groupPath"> e.g. linguistics/morphology
		///	</param>
		/// <returns></returns>
		public string GetString(string id, string groupPath)
		{
			return GetStringWithRootXPath(id, GetXPathFragmentFromSimpleNotation(groupPath));
		}

		/// <summary>
		/// look up a list of string IDs and return an array of strings
		/// </summary>
		/// <example>
		///		here, the strings will be looked up at the root level
		///		<stringList ids="anywhere, somewhere to left, somewhere to right, adjacent to left, adjacent to right"/>
		/// </example>
		/// <example>
		///		here, the strings will be looked up under a nested group
		///		<stringList group="MoMorphAdhocProhib/adjacency" ids="anywhere, somewhere to left, somewhere to right, adjacent to left, adjacent to right"/>
		/// </example>
		/// <param name="node">the name of the node is ignored, only the attributes are read</param>
		/// <returns></returns>
		public string[] GetStringsFromStringListNode(XElement node)
		{
			string ids = XmlUtils.GetManditoryAttributeValue(node, "ids");
			string[] idList = ids.Split(',');
			string[] strings = new string[idList.Length];
			string groupPath = "";
			string simplePath = XmlUtils.GetOptionalAttributeValue(node, "group");
			if (simplePath != null)
			{
				groupPath = GetXPathFragmentFromSimpleNotation(simplePath);
			}
			int i = 0;
			foreach (string id in idList)
			{
				strings[i++] = GetStringWithXPath(id, groupPath);
			}
			return strings;
		}

		/// <summary>
		/// creat an XPATH for use with GetString, based on a simpler notation
		/// </summary>
		/// <param name="simplePath">e.g. "linguistics/morphology" </param>
		/// <returns></returns>
		protected string GetXPathFragmentFromSimpleNotation(string simplePath)
		{
			if(simplePath == "")
				return "";
			string path = "";
			string[] names = simplePath.Split('/');
			foreach(string name in names)
			{
				path += "group[@id = '" + name.Trim() + "']/";
			}
			return path;
		}

		/// <summary>
		/// Retrieve a localized version of the input string if possible, otherwise
		/// return the input string.
		/// </summary>
		/// <param name="sValue"></param>
		/// <returns></returns>
		public string LocalizeAttributeValue(string sValue)
		{
			if (String.IsNullOrEmpty(sValue))
				return sValue;
			string sLocValue = GetStringWithRootXPath(sValue,
				GetXPathFragmentFromSimpleNotation("LocalizedAttributes"), false);
			if (String.IsNullOrEmpty(sLocValue))
				return sValue;
			return sLocValue == "*" + sValue + "*" ? sValue : sLocValue;
		}

		/// <summary>
		/// Retrieve a localized version of the input string if possible, otherwise
		/// return the input string.
		/// </summary>
		/// <param name="sValue"></param>
		/// <returns></returns>
		public string LocalizeLiteralValue(string sValue)
		{
			if (String.IsNullOrEmpty(sValue))
				return sValue;
			string sLocValue = GetStringWithRootXPath(sValue,
				GetXPathFragmentFromSimpleNotation("LocalizedLiterals"), false);
			if (String.IsNullOrEmpty(sLocValue))
				return sValue;
			return sLocValue == "*" + sValue + "*" ? sValue : sLocValue;
		}

		/// <summary>
		/// Retrieve a localized version of the specified ContextHelp string if
		/// possible, otherwise return null.
		/// </summary>
		public string LocalizeContextHelpString(string sId)
		{
			if (String.IsNullOrEmpty(sId))
				return null;
			string sLocValue = GetStringWithRootXPath(sId,
				GetXPathFragmentFromSimpleNotation("LocalizedContextHelp"), false);
			if (String.IsNullOrEmpty(sLocValue))
				return null;
			return sLocValue == "*" + sId + "*" ? null : sLocValue;
		}
	}
}
