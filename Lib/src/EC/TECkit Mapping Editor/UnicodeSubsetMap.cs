using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using System.IO;

namespace TECkit_Mapping_Editor
{
	public class UnicodeSubset : System.Collections.Generic.SortedDictionary<int, int>
	{
		public UnicodeSubset(string strSubsetName)
		{
			Name = strSubsetName;
		}

		public new void Add(int nRangeStart, int nRangeEnd)
		{
			int nLength = nRangeEnd - nRangeStart + 1;
			base.Add(nRangeStart, nLength);
		}

		public string Name = null;
	}

	public class UnicodeSubsetMap : System.Collections.Generic.Dictionary<string, UnicodeSubset>
	{
		public const string cstrDefSubsetName = "C0 Controls and Basic Latin";
		public const string cstrUnicodeRangeXmlFilename = "UnicodeRanges.xml";

		protected string m_strSubsetName = null;
		protected int[] m_nRangeBeginning = null;
		protected int[] m_nRangeLength = null;

		public UnicodeSubsetMap()
		{
			InitUnicodeSubsets();
		}

		public string PathToXmlFile
		{
			get
			{
				// try the same folder as we're executing out of
				string strCurrentFolder = System.Reflection.Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName; // e.g. C:\src\SEC\Lib\release\TECkitMappingEditorU.exe
				strCurrentFolder = Path.GetDirectoryName(strCurrentFolder); // e.g. C:\src\SEC\Lib\release
				string strFileToCheck = String.Format(@"{0}\{1}", strCurrentFolder, cstrUnicodeRangeXmlFilename);
				System.Diagnostics.Debug.Assert(File.Exists(strFileToCheck), String.Format("Can't find: {0}", strFileToCheck));
				if (!File.Exists(strFileToCheck))
				{
					// on dev machines, this file is in the "..\..\src\EC\TECkit Mapping Editor" folder
					strCurrentFolder = Path.GetDirectoryName(strCurrentFolder); // e.g. C:\src\SEC\Lib
					strFileToCheck = strCurrentFolder + @"\src\EC\TECkit Mapping Editor\" + cstrUnicodeRangeXmlFilename;
				}

				return strFileToCheck;
			}
		}

		protected void GetXmlDocument(out XmlDocument doc, out XPathNavigator navigator, out XmlNamespaceManager manager)
		{
			doc = new XmlDocument();
			doc.Load(PathToXmlFile);
			navigator = doc.CreateNavigator();
			manager = new XmlNamespaceManager(navigator.NameTable);
		}

		protected void InitUnicodeSubsets()
		{
			try
			{
				XmlDocument doc;
				XPathNavigator navigator;
				XmlNamespaceManager manager;
				GetXmlDocument(out doc, out navigator, out manager);

				XPathNodeIterator xpUnicodeRange = navigator.Select("/UnicodeRanges/UnicodeRange", manager);
				while (xpUnicodeRange.MoveNext())
				{
					string strSubsetName = xpUnicodeRange.Current.GetAttribute("name", navigator.NamespaceURI);
					UnicodeSubset aUS = new UnicodeSubset(strSubsetName);

					XPathNodeIterator xpUnicodeRanges = xpUnicodeRange.Current.Select("Range", manager);
					while (xpUnicodeRanges.MoveNext())
					{
						int nRangeStart = Convert.ToInt32(xpUnicodeRanges.Current.GetAttribute("begin", navigator.NamespaceURI), 16);
						int nRangeEnd = Convert.ToInt32(xpUnicodeRanges.Current.GetAttribute("end", navigator.NamespaceURI), 16);
						aUS.Add(nRangeStart, nRangeEnd);
					}

					this.Add(strSubsetName, aUS);
				}
			}
			catch (Exception ex)
			{
				throw new ApplicationException("Unable to process the xml file containing the Unicode Ranges... Reinstall.", ex);
			}
		}
	}
}
