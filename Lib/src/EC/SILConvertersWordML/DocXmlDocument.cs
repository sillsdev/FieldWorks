#define DefineWord07MLDocument	// turn this off until I implement it

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.XPath;
using System.Drawing;
using System.Xml.Xsl;

namespace SILConvertersWordML
{
	public abstract class DocXmlDocument : XmlDocument
	{
		public Dictionary<string, string> mapStyleId2Name = new Dictionary<string, string>();
		public Dictionary<string, string> mapStyleName2FontName = new Dictionary<string, string>();

		public List<string> lstFontNamesCustom = new List<string>();
		public List<string> lstFontNamesSymbolText = new List<string>();
		public List<string> lstFontNamesPStyle = new List<string>();
		public List<string> lstFontNamesCStyle = new List<string>();
		public List<string> lstPStyleIdList = new List<string>();
		public List<string> lstCStyleIdList = new List<string>();
		public string m_strDefaultStyleFontName;

		protected const string cstrDefaultNameSpaceAbrev = "n";

		protected Dictionary<string, string> m_mapPrefix2NamespaceURI = new Dictionary<string, string>();

		public abstract void GetFullNameLists(string strFilename);
		public abstract void ReplaceTextFontNameGetFontText(string strOldFontName, string strNewFontName);
		public abstract void ReplaceSymbolTextFontNameGetFontText(string strOldFontName, string strNewFontName);
		public abstract void ReplaceTextFontNameGetPStyleFontText(string strOldFontName, string strNewFontName);
		public abstract void ReplaceTextFontNameGetCStyleFontText(string strOldFontName, string strNewFontName);
		public abstract void ReplaceTextFontNameGetStyleText(string strStyleName, string strNewFontName);
		public abstract string XPathFormatGetFontText { get; }
		public abstract string XPathFormatGetSymbolFontText { get; }
		public abstract string XPathFormatGetDefaultPStyleFontText { get; }
		public abstract string XPathFormatGetPStyleFontText { get; }
		public abstract string XPathFormatGetCStyleFontText { get; }
		public abstract string XPathFormatGetPStyleText { get; }
		public abstract string XPathFormatGetCStyleText { get; }

		protected XPathNodeIterator GetIterator(string strXPath)
		{
			XPathNavigator navigator = CreateNavigator();

			XPathNodeIterator xpIterator = null;
			if (IsNamespaceRequired)
			{
				XmlNamespaceManager manager;
				GetNamespaceManager(navigator, out manager);
				xpIterator = navigator.Select(strXPath, manager);
			}
			else
			{
				xpIterator = navigator.Select(strXPath);
			}

			return xpIterator;
		}

		public void GetFullNameList(string strXPath2Names, ref List<string> lstDocNameList)
		{
			XPathNodeIterator xpIteratorName = GetIterator(strXPath2Names);
			while (xpIteratorName.MoveNext())
			{
				string strName = xpIteratorName.Current.Value;
				if (!lstDocNameList.Contains(strName))
					lstDocNameList.Add(strName);
			}
		}

		protected bool IsNamespaceRequired
		{
			get { return (m_mapPrefix2NamespaceURI.Count > 0); }
		}

		protected void GetNameSpaceURIs(XmlNode nodeParent)
		{
			foreach (System.Xml.XmlNode node in nodeParent.ChildNodes)
			{
				string strPrefix = String.IsNullOrEmpty(node.Prefix) ? cstrDefaultNameSpaceAbrev : node.Prefix;
				if (!m_mapPrefix2NamespaceURI.ContainsKey(strPrefix) && !String.IsNullOrEmpty(node.NamespaceURI))
					m_mapPrefix2NamespaceURI.Add(strPrefix, node.NamespaceURI);

				// recurse children
				GetNameSpaceURIs(node);
			}
		}

		protected void GetNamedItem(string strXPathFormat, string strName, out string strNamedItem)
		{
			string strXPath2NamedItem = String.Format(strXPathFormat, strName);
			GetNamedItem(strXPath2NamedItem, out strNamedItem);
		}

		protected void GetNamedItem(string strXPathExpress, out string strNamedItem)
		{
			strNamedItem = null;
			XPathNodeIterator xpIteratorFontName = GetIterator(strXPathExpress);
			if (xpIteratorFontName.MoveNext())
				strNamedItem = xpIteratorFontName.Current.Value;
		}

		public bool GetTextIteratorForName(string strFontNameOrStyleId, string strXPathFormat,
			ref IteratorMap mapNames2Iterator, bool bConvertAsCharValue)
		{
			bool bAdded = false;
#if !rde1001
			// before creating and adding this one, make sure it isn't a duplicate (which
			//	can now only happen if the same font is found in two different documents).
			//	It's okay that we'll ignore it in subsequent documents, because before we
			//	convert the files, we re-query for all of them
			if (!mapNames2Iterator.ContainsKey(strFontNameOrStyleId))
			{
				// get an iterator to see if there's any actual data in this font in this document
				//  (and don't add it here if not)
				string strXPathText = String.Format(strXPathFormat, strFontNameOrStyleId);
				XPathNodeIterator xpIteratorFontText = GetIterator(strXPathText);
				if ((bAdded = xpIteratorFontText.MoveNext()))
					mapNames2Iterator.Add(strFontNameOrStyleId, new XPathIterator(xpIteratorFontText, bConvertAsCharValue));
			}
			else
				System.Diagnostics.Debug.Assert(!Program.IsOnlyOneDoc, "Bad assumption: multiple fonts found for the same type of text and *not* because it's multiple documents! Send this document to silconverters_support@sil.org for help");
#else
			if (!mapNames2Iterator.ContainsKey(strFontNameOrStyleId))
			{
				// get an iterator to see if there's any actual data in this font in this document
				//  (and don't add it here if not)
				string strXPathText = String.Format(strXPathFormat, strFontNameOrStyleId);
				XPathNodeIterator xpIteratorFontText = GetIterator(strXPathText);
				if ((bAdded = xpIteratorFontText.MoveNext()))
				{
					mapNames2Iterator.Add(strFontNameOrStyleId, xpIteratorFontText);
				}
			}
#endif

			return bAdded;
		}

		protected void InitXPathExpression(string strXPathExpr, ref XPathExpression xpe)
		{
			XPathNavigator navigator = CreateNavigator();
			xpe = navigator.Compile(strXPathExpr);
			XmlNamespaceManager manager;
			GetNamespaceManager(navigator, out manager);
			xpe.SetContext(manager);
		}

		protected void GetNamespaceManager(XPathNavigator navigator, out XmlNamespaceManager manager)
		{
			manager = new XmlNamespaceManager(navigator.NameTable);
			foreach (KeyValuePair<string, string> kvp in m_mapPrefix2NamespaceURI)
				manager.AddNamespace(String.IsNullOrEmpty(kvp.Key) ? String.Empty : kvp.Key, kvp.Value);
		}

		protected void GetExpressionValue(XPathNodeIterator xpIterator, XPathExpression xpe, out string str)
		{
			XPathNodeIterator xpIteratorName = xpIterator.Current.Select(xpe);
			if (xpIteratorName.MoveNext())
				str = xpIteratorName.Current.Value;
			else
			{
				str = null;
				System.Diagnostics.Debug.Assert(false); // not expecting this
			}
		}

		protected void RemoveElement(XPathNodeIterator xpIterator, XPathExpression xpe)
		{
			XPathNodeIterator xpIteratorName = xpIterator.Current.Select(xpe);
			if (xpIteratorName.MoveNext())
				xpIteratorName.Current.DeleteSelf();
		}

		/// <summary>
		/// InsureFontNameAttributes
		/// </summary>
		/// <param name="xpIterator"></param>
		/// <param name="xpe"></param>
		/// <param name="strNewFontName"></param>
		/// <param name="bCreateIfNotPresent">indicates whether to create the attribute/element if it doesn't already exist</param>
		/// <returns>true if the attribute was already present; false if not (whether it was created or not)</returns>
		protected bool InsureFontNameAttributes(XPathNodeIterator xpIterator, XPathExpression xpe,
			string strNewFontName, bool bCreateIfNotPresent)
		{
			XPathNodeIterator xpIteratorAttrib = xpIterator.Current.Select(xpe);
			if (xpIteratorAttrib.MoveNext())
			{
				xpIteratorAttrib.Current.SetValue(strNewFontName);
				return true;
			}
			else if (bCreateIfNotPresent)
			{
				xpIteratorAttrib = xpIterator.Clone();
				string strExpr = xpe.Expression;

				// this code only handles expressions of the form "w:x/y:z" so there should be 4 "parts")
				string[] astrSplit = strExpr.Split(new char[] { '/', ':', '@' }, StringSplitOptions.RemoveEmptyEntries);
				System.Diagnostics.Debug.Assert(astrSplit.Length == 6);

				string strChildElementPrefix = astrSplit[0];
				string strNameSpace = m_mapPrefix2NamespaceURI[strChildElementPrefix];
				string strChildElementName = astrSplit[1];
				int nOffset = 0;
				if (astrSplit.Length == 6)
				{
					if (!xpIteratorAttrib.Current.MoveToChild(strChildElementName, strNameSpace))
					{
						xpIteratorAttrib.Current.PrependChildElement(strChildElementPrefix, strChildElementName, strNameSpace, null);
						bool bMoveRes = xpIteratorAttrib.Current.MoveToChild(strChildElementName, strNameSpace);
						System.Diagnostics.Debug.Assert(bMoveRes);
					}
					nOffset = 2;
				}

				strChildElementPrefix = astrSplit[0 + nOffset];
				strNameSpace = m_mapPrefix2NamespaceURI[strChildElementPrefix];
				strChildElementName = astrSplit[1 + nOffset];
				string strAttribPrefix = astrSplit[2 + nOffset];
				string strAttribName = astrSplit[3 + nOffset];

				if (!xpIteratorAttrib.Current.MoveToChild(strChildElementName, strNameSpace))
				{
					xpIteratorAttrib.Current.PrependChildElement(strChildElementPrefix, strChildElementName, strNameSpace, null);
					bool bMoveRes = xpIteratorAttrib.Current.MoveToChild(strChildElementName, strNameSpace);
					System.Diagnostics.Debug.Assert(bMoveRes);
				}

				xpIteratorAttrib.Current.CreateAttribute(strAttribPrefix, strAttribName, strNameSpace, strNewFontName);
			}

			return false;
		}

		protected abstract string GetFindFontXPathExpression(List<string> astrFontsToSearchFor);

		public bool HasFonts(List<string> astrFontsToSearchFor)
		{
			// see if this document has an instance of the font
			XPathNodeIterator xpIteratorFontName = GetIterator(GetFindFontXPathExpression(astrFontsToSearchFor));
			return xpIteratorFontName.MoveNext();
		}
	}

	// common stuff for Word 2003 and 2007
	public abstract class WordMLDocument : DocXmlDocument
	{
		// XPath expressions for insuring these font names exist
		//  These are used after conversion, where we rewrite the font names listed by writing all the possible
		//  permutations (because changing the 'ascii' fontname doesn't help if the result of the conversion,
		//  is a complex script font).
		public const string cstrXPathExprFontAscii = "w:rPr/w:rFonts/@w:ascii";
		public const string cstrXPathExprFontCS = "w:rPr/w:rFonts/@w:cs";
		public const string cstrXPathExprFont = "w:rPr/wx:font/@wx:val";
		public const string cstrXPathExprSymFont = "w:sym/@w:font";

		public const string cstrXPathExprCS = "w:rPr/w:cs";
		public const string cstrXPathExprStyleName = "w:name/@w:val";
		public const string cstrXPathExprStyleId = "@w:styleId";
		public const string cstrXPathExprPStyleFont = "w:rPr/wx:font/@wx:val";
		public const string cstrXPathExprCStyleFont = "w:rPr/w:rFonts/@w:ascii";

		protected XPathExpression m_xpeWFontValAscii = null;
		protected XPathExpression m_xpeWFontValFareast = null;
		protected XPathExpression m_xpeWFontValHAnsi = null;
		protected XPathExpression m_xpeWFontValCS = null;

		protected XPathExpression m_xpeStyleName = null;
		protected XPathExpression m_xpeStyleId = null;
		protected XPathExpression m_xpePStyleFont = null;
		protected XPathExpression m_xpeCStyleFont = null;

		protected abstract string XPathExprFontFareast { get; }
		protected abstract string XPathExprFontHAnsi { get; }
		protected abstract string XPathGetDefPStyleFontName { get; }
		protected abstract string XPathGetSymbolFontName { get; }
		protected abstract string XPathGetPStyleFontNames { get; }
		protected abstract string XPathGetCStyleFontNames { get; }
		protected abstract string XPathGetPStyle { get; }
		protected abstract string XPathGetCStyle { get; }

		protected abstract void GetCustomFontLists();

		protected virtual void InitXPathExpressions()
		{
			InitXPathExpression(cstrXPathExprFontAscii, ref m_xpeWFontValAscii);
			InitXPathExpression(XPathExprFontFareast, ref m_xpeWFontValFareast);
			InitXPathExpression(XPathExprFontHAnsi, ref m_xpeWFontValHAnsi);
			InitXPathExpression(cstrXPathExprFontCS, ref m_xpeWFontValCS);

			InitXPathExpression(cstrXPathExprStyleName, ref m_xpeStyleName);
			InitXPathExpression(cstrXPathExprStyleId, ref m_xpeStyleId);
			InitXPathExpression(cstrXPathExprPStyleFont, ref m_xpePStyleFont);
			InitXPathExpression(cstrXPathExprCStyleFont, ref m_xpeCStyleFont);
		}

		// get the full list of potential font and style names when we first open the XML documents so we don't
		//  need to do this again (e.g. when the user selects a different radio button)
		public override void GetFullNameLists(string strFilename)
		{
			// get all the fonts associated with custom formatting (into doc.lstFontNamesCustom)
			GetCustomFontLists();

			// get all inserted symbol items
			GetFullNameList(XPathGetSymbolFontName, ref lstFontNamesSymbolText);

			// get all the fonts associated with style-based formatting (into m_astrFullFontNamesStyle)
			GetNamedItem(XPathGetDefPStyleFontName, out m_strDefaultStyleFontName);
			GetFullNameList(XPathGetPStyleFontNames, ref lstFontNamesPStyle);
			GetFullNameList(XPathGetCStyleFontNames, ref lstFontNamesCStyle);

			// get all the style names (into m_astrFullStyleNameList)
			GetStyleNameIdLists(strFilename, XPathGetPStyle, m_xpePStyleFont, ref lstPStyleIdList);
			GetStyleNameIdLists(strFilename, XPathGetCStyle, m_xpeCStyleFont, ref lstCStyleIdList);
		}

		protected void GetStyleNameIdLists(string strFilename, string strXPath2Style, XPathExpression xpeFont,
			ref List<string> lstDocStyleIdList)
		{
			XPathNodeIterator xpIterator = GetIterator(strXPath2Style);
			while (xpIterator.MoveNext())
			{
				string strName, strId, strFont;
				GetExpressionValue(xpIterator, m_xpeStyleName, out strName);
				GetExpressionValue(xpIterator, m_xpeStyleId, out strId);
				GetExpressionValue(xpIterator, xpeFont, out strFont);

				if (!lstDocStyleIdList.Contains(strId))
					lstDocStyleIdList.Add(strId);

				// each document could have a completely different mapping of style to font... so keep a map on
				//  a per document basis
				System.Diagnostics.Debug.Assert(!mapStyleId2Name.ContainsKey(strId));
				mapStyleId2Name.Add(strId, strName);

				// Apparently, it isn't impossible to have multiple styles with the same name...
				//  see c:\temp\Buku\Buku Latihan Fnlg_06.doc
				// System.Diagnostics.Debug.Assert(!mapStyleName2FontName.ContainsKey(strName));
				if (mapStyleName2FontName.ContainsKey(strName))
					MessageBox.Show(String.Format("The Word document '{0}' contains two styles with the same name '{1}'. After the conversion, you should check the converted file carefully to see if the data in that style name was converted correctly. You may need to combine the segments into a single style for this to work properly.",
						strFilename, strName), FontsStylesForm.cstrCaption);
				else
					mapStyleName2FontName.Add(strName, strFont);
			}
		}

		protected void ReplaceTextNameFormatAttribs(string strXPathFormat, string strOldFontName, string strNewFontName)
		{
			string strXPathReplaceFontName = String.Format(strXPathFormat, strOldFontName);
			XPathNodeIterator xpIterator = GetIterator(strXPathReplaceFontName);
			while (xpIterator.MoveNext())
				InsureFontNameAttributes(xpIterator, strNewFontName, false);
		}

		protected virtual void InsureFontNameAttributes(XPathNodeIterator xpIterator, string strNewFontName, bool bCreateIfNotPresent)
		{
			bool bAsciiPresent = InsureFontNameAttributes(xpIterator, m_xpeWFontValAscii, strNewFontName, bCreateIfNotPresent) | bCreateIfNotPresent;

			// I have no idea whether this is going to work or not, but the fareast attribute causes Devanagari to screw
			//  up, so don't create it if it isn't already present
			InsureFontNameAttributes(xpIterator, m_xpeWFontValFareast, strNewFontName, false);
			InsureFontNameAttributes(xpIterator, m_xpeWFontValHAnsi, strNewFontName, bAsciiPresent);
			InsureFontNameAttributes(xpIterator, m_xpeWFontValCS, strNewFontName, bAsciiPresent);
		}
	}

	// specifics for Word 2003
	public class Word03MLDocument : WordMLDocument
	{
		// XPath expressions
		// get the total list of fonts in the document
		// these are done when the xml file is first opened to get a huristic of the things that we might need to
		//  look for. If something doesn't show up in the list that ought to, it could be a problem from this list
		//  (e.g. if the user is looking for a character style that is based on a complex script, then the last
		//  entry here won't find it (since it's only looking for 'ascii')).
		public const string cstrXPathWordMLDefFontAscii = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:ascii";
		public const string cstrXPathWordMLDefFontFareast = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:fareast";
		public const string cstrXPathWordMLDefFontHAnsi = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:h-ansi";
		public const string cstrXPathWordMLDefFontCS = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:cs";

		// get the total list of fonts in the document
		// these are done when the xml file is first opened to get a huristic of the things that we might need to
		//  look for. If something doesn't show up in the list that ought to, it could be a problem from this list
		//  (e.g. if the user is looking for a character style that is based on a complex script, then the last
		//  entry here won't find it (since it's only looking for 'ascii')).
		public const string cstrXPathWordMLGetFontNames = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/wx:font/@wx:val";
		public const string cstrXPathWordMLGetSymbolTextFontNames = "/w:wordDocument/w:body//w:p/w:r/w:sym/@w:font";

		// rde: 2010-07-01: the default paragraph style font isn't the w:fonts/w:defaultFonts, but rather the font
		//  of the Normal style...
		// public const string cstrXPathWordMLGetDefaultPStyleFontName = "/w:wordDocument/w:fonts/w:defaultFonts/@w:ascii";    // this only handles the lhs legacy case (but having a default complex doesn't seem to depend on this key)
		public const string cstrXPathWordMLGetDefaultPStyleFontName = "/w:wordDocument/w:styles/w:style[@w:styleId = 'Normal']/w:rPr/w:rFonts/@w:ascii";
		public const string cstrXPathWordMLGetPStyleFontNames = "/w:wordDocument/w:styles/w:style[@w:type = 'paragraph']/w:rPr/wx:font/@wx:val";
		public const string cstrXPathWordMLGetCStyleFontNames = "/w:wordDocument/w:styles/w:style[@w:type = 'character']/w:rPr/w:rFonts/@w:ascii";    // this is only one of the 4 possibles, but at least it should cover the legacy to unicode conversion case

		public const string cstrXPathExprFontFareast = "w:rPr/w:rFonts/@w:fareast";
		public const string cstrXPathExprFontHAnsi = "w:rPr/w:rFonts/@w:h-ansi";

		protected XPathExpression m_xpeWxFontVal = null;
		protected XPathExpression m_xpeSymWFontVal = null;
		protected XPathExpression m_xpeCSVal = null;

		// get the total list of style names and ids in the document (that have an associated font)
		//  (these are used to fill the m_astrFull{P,C}StyleNameList collections which are
		//  then iterated over during the searching of the file and also the mapping of style name to id
		public const string cstrXPathWordMLGetPStyle = "/w:wordDocument/w:styles/w:style[@w:type = 'paragraph'][w:rPr/wx:font/@wx:val]";
		public const string cstrXPathWordMLGetCStyle = "/w:wordDocument/w:styles/w:style[@w:type = 'character'][w:rPr/w:rFonts/@w:ascii]";

		// format to get the style id of a particular style name (as part of a complex search)
		public const string cstrXPathWordMLFormatGetPStyleId = "/w:wordDocument/w:styles/w:style[@w:type = 'paragraph'][w:name/@w:val = '{0}']/@w:styleId";
		public const string cstrXPathWordMLFormatGetCStyleId = "/w:wordDocument/w:styles/w:style[@w:type = 'character'][w:name/@w:val = '{0}']/@w:styleId";

		// formats for building XPath statements to get text entries for text
		//  the first one (for custom formatting) definitely needs to have "/w:r" because in some docs
		//  (e.g. the MTT manual) that node is embedded in different things. The style based ones (i.e. the latter two)
		//  might also should be "/w:r", but it really blows the time of search out greatly, so I'm taking it out until
		//  I know for sure whether it can happen or not
		// public const string cstrXPathWordMLFormatGetFontText = "/w:wordDocument/w:body//w:p//w:r[w:rPr/wx:font/@wx:val = '{0}']/w:t";
		// rde: 5/10/10 adding "[not(w:rPr/w:rStyle/@w:val)]", so we can prevent the case where it has
		//  both style and (what appears to be) custom formatting.
		/* e.g. in:
		<w:r>
		  <w:rPr>
			<w:rStyle w:val="GWGreekWord" />
			<wx:font wx:val="SIL Galatia" />
			<wx:sym wx:font="SIL Galatia" wx:char="F06D" />
		  </w:rPr>
		  <w:t>x</w:t>
		* the presence of w:rStyle/@w:val suggests style-based formatting (which gets picked up elsewhere, so we have to prevent it here)
		* and the presence of wx:font/@wx:val suggests custom formatting (which gets picked here)
		*/
		public const string cstrXPathWordMLFormatGetFontText = "/w:wordDocument/w:body//w:p//w:r[not(w:rPr/w:rStyle/@w:val)][w:rPr/wx:font/@wx:val = '{0}']/w:t";


		public const string cstrXPathWordMLFormatGetSymbolFontChar = "/w:wordDocument/w:body//w:p//w:r/w:sym[@w:font = '{0}']/@w:char";
																	// "/w:wordDocument/w:body//w:p//w:r/w:sym[@w:font = '{0}']";

		// public const string cstrXPathWordMLFormatGetDefaultPStyleFontText = "/w:wordDocument/w:body//w:p/w:r[not(w:rPr)]/w:t";
		// The above version was changed to the following to better distinguish between default paragraph style and regular
		//  custom formatted text (i.e. cstrXPathWordMLFormatGetFontText)--as you can see, the rules are about opposites of each other.
		//  This was added to fix the file: C:\temp\SC for Word\Obadiah\Obadiah.Kalam.doc
		// public const string cstrXPathWordMLFormatGetDefaultPStyleFontText = "/w:wordDocument/w:body//w:p//w:r[not(w:rPr/wx:font/@wx:val)]/w:t";
		// unfortunately, this then causes a problem with the non-default paragraph style (i.e. cstrXPathWordMLFormatGetPStyleFontText below)
		// so now we need to prevent this from finding the text twice
		//  The "not(w:pPr/w:pStyle/@w:val)" part says it isn't a regular paragraph style (which should get picked up by
		//  cstrXPathWordMLFormatGetPStyleFontText) and the "not(w:rPr/wx:font/@wx:val)" part says it isn't custom formatting
		//  which gets picked up by cstrXPathWordMLFormatGetFontText
		// The test file for this is L:\Kangri\Texts\Copy of Indian cult.doc
		public const string cstrXPathWordMLFormatGetDefaultPStyleFontText = "/w:wordDocument/w:body//w:p[not(w:pPr/w:pStyle/@w:val)]//w:r[not(w:rPr/wx:font/@wx:val)]/w:t";

		public const string cstrXPathWordMLFormatGetPStyleFontText = "/w:wordDocument/w:body//w:p[w:pPr/w:pStyle/@w:val = //w:styles/w:style[@w:type = 'paragraph'][w:rPr/wx:font/@wx:val = '{0}']/@w:styleId]/w:r[not(w:rPr/wx:font)]/w:t";
		public const string cstrXPathWordMLFormatReplaceFontNameGetPStyleFontName = "/w:wordDocument/w:styles/w:style[@w:type = 'paragraph'][w:rPr/wx:font/@wx:val = '{0}']";

		public const string cstrXPathWordMLFormatGetCStyleFontText =                "/w:wordDocument/w:body//w:p//w:r[w:rPr/w:rStyle/@w:val = //w:styles/w:style[@w:type = 'character']/@w:styleId][w:rPr/wx:font/@wx:val = '{0}']/w:t";
		public const string cstrXPathWordMLFormatReplaceFontNameGetCStyleFontName = "/w:wordDocument/w:styles/w:style[@w:type = 'character'][w:rPr/w:rFonts/@w:ascii = '{0}']";

		public const string cstrXPathWordMLFormatGetPStyleText = "/w:wordDocument/w:body//w:p[w:pPr/w:pStyle/@w:val = '{0}']/w:r[not(w:rPr/wx:font)]/w:t";

		public const string cstrXPathWordMLFormatGetCStyleText = "/w:wordDocument/w:body//w:p/w:r[w:rPr/w:rStyle/@w:val = '{0}']/w:t";

		protected const string cstrXPathHasSingleCharacterRun = "/w:wordDocument/w:body//w:p/w:r[w:rPr/wx:sym/@wx:char]/w:t";

		// public const string cstrXPathWordMLFormatReplaceFontNameGetFontText = "/w:wordDocument/w:body//w:p//w:r[w:rPr/wx:font/@wx:val = '{0}']";
		public const string cstrXPathWordMLFormatReplaceFontNameGetFontText = "/w:wordDocument/w:body//w:p//w:r[not(w:rPr/w:rStyle/@w:val)][w:rPr/wx:font/@wx:val = '{0}']";
		public const string cstrXPathWordMLFormatReplaceFontNameNoRunParagraphs = "/w:wordDocument/w:body//w:p[w:pPr/w:rPr/wx:font/@wx:val = '{0}']/w:pPr";
		public const string cstrXPathWordMLFormatReplaceFontNameNoRunCsParagraphs = "/w:wordDocument/w:body//w:p[w:pPr/w:rPr/w:rFonts/@w:cs = '{0}']/w:pPr";

		public const string cstrXPathWordMLFormatReplaceFontNameGetSymbolFontText = "/w:wordDocument/w:body//w:p//w:r[w:sym/@w:font = '{0}']";

		public const string cstrXPathWordMLFormatReplaceFontNameGetStyleText = "/w:wordDocument/w:styles/w:style[w:name/@w:val = '{0}']";

		public const string cstrXPathFindFontFormatExpression = "/w:wordDocument/w:fonts/w:font[{0}]";
		protected const string cstrXPathFindFontFormat = "@w:name = '{0}'";

		public override string XPathFormatGetFontText
		{
			get { return cstrXPathWordMLFormatGetFontText; }
		}
		public override string XPathFormatGetSymbolFontText
		{
			get { return cstrXPathWordMLFormatGetSymbolFontChar; }
		}
		public override string XPathFormatGetDefaultPStyleFontText
		{
			get { return cstrXPathWordMLFormatGetDefaultPStyleFontText; }
		}
		public override string XPathFormatGetPStyleFontText
		{
			get { return cstrXPathWordMLFormatGetPStyleFontText; }
		}
		public override string XPathFormatGetCStyleFontText
		{
			get { return cstrXPathWordMLFormatGetCStyleFontText; }
		}
		public override string XPathFormatGetPStyleText
		{
			get { return cstrXPathWordMLFormatGetPStyleText;  }
		}
		public override string XPathFormatGetCStyleText
		{
			get { return cstrXPathWordMLFormatGetCStyleText; }
		}
		protected override string XPathGetDefPStyleFontName
		{
			get { return cstrXPathWordMLGetDefaultPStyleFontName; }
		}
		protected override string XPathGetSymbolFontName
		{
			get { return cstrXPathWordMLGetSymbolTextFontNames; }
		}
		protected override string XPathGetPStyleFontNames
		{
			get { return cstrXPathWordMLGetPStyleFontNames; }
		}
		protected override string XPathGetCStyleFontNames
		{
			get { return cstrXPathWordMLGetCStyleFontNames;  }
		}
		protected override string XPathGetPStyle
		{
			get { return cstrXPathWordMLGetPStyle; }
		}
		protected override string XPathGetCStyle
		{
			get { return cstrXPathWordMLGetCStyle; }
		}
		protected override string XPathExprFontFareast
		{
			get { return cstrXPathExprFontFareast; }
		}
		protected override string XPathExprFontHAnsi
		{
			get { return cstrXPathExprFontHAnsi; }
		}

		protected override void InitXPathExpressions()
		{
			base.InitXPathExpressions();
			InitXPathExpression(cstrXPathExprFont, ref m_xpeWxFontVal);
			InitXPathExpression(cstrXPathExprCS, ref m_xpeCSVal);
		}

		protected override void InsureFontNameAttributes(XPathNodeIterator xpIterator, string strNewFontName, bool bCreateIfNotPresent)
		{
			bool bRemoveCs = true;	// this is in case we're going from Unicode to Legacy
			if (bRemoveCs)
				RemoveElement(xpIterator, m_xpeCSVal);
			base.InsureFontNameAttributes(xpIterator, strNewFontName, bCreateIfNotPresent);
			InsureFontNameAttributes(xpIterator, m_xpeWxFontVal, strNewFontName, bCreateIfNotPresent);
		}

		protected override void GetCustomFontLists()
		{
			// first look in these places to get the actual full list of potential font names
			GetFullNameList(cstrXPathWordMLDefFontAscii, ref lstFontNamesCustom);
			GetFullNameList(cstrXPathWordMLDefFontFareast, ref lstFontNamesCustom);
			GetFullNameList(cstrXPathWordMLDefFontHAnsi, ref lstFontNamesCustom);
			GetFullNameList(cstrXPathWordMLDefFontCS, ref lstFontNamesCustom);
			GetFullNameList(cstrXPathWordMLGetFontNames, ref lstFontNamesCustom);
		}

		// if we replace the name of the font (i.e. //wx:font/@wx:val) associated with some text (i.e. [w:t]),
		//  then we must also replace the nearby //w:rFonts/@w:* items or Word crashes
		public override void ReplaceTextFontNameGetFontText(string strOldFontName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetFontText, strOldFontName, strNewFontName);
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameNoRunParagraphs, strOldFontName, strNewFontName);
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameNoRunCsParagraphs, strOldFontName, strNewFontName);
		}

		public override void ReplaceSymbolTextFontNameGetFontText(string strOldFontName, string strNewFontName)
		{
			if (m_xpeSymWFontVal == null)
				InitXPathExpression(cstrXPathExprSymFont, ref m_xpeSymWFontVal);
			ReplaceSymbolTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetSymbolFontText, strOldFontName, strNewFontName);
		}

		protected void ReplaceSymbolTextNameFormatAttribs(string strXPathFormat, string strOldFontName, string strNewFontName)
		{
			string strXPathReplaceFontName = String.Format(strXPathFormat, strOldFontName);
			XPathNodeIterator xpIterator = GetIterator(strXPathReplaceFontName);
			while (xpIterator.MoveNext())
			{
				InsureFontNameAttributes(xpIterator, strNewFontName, true);
				InsureFontNameAttributes(xpIterator, m_xpeSymWFontVal, strNewFontName, true);
			}
		}

		public override void ReplaceTextFontNameGetPStyleFontText(string strOldFontName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetPStyleFontName, strOldFontName, strNewFontName);
		}

		public override void ReplaceTextFontNameGetCStyleFontText(string strOldFontName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetCStyleFontName, strOldFontName, strNewFontName);
		}

		public override void ReplaceTextFontNameGetStyleText(string strStyleName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetStyleText, strStyleName, strNewFontName);
		}

		public static DocXmlDocument GetXmlDocument(ref string strXmlFilename,
			string strDocFilename, bool bSaveXmlOutputInFolder)
		{
			// when opening the xml file, let's do an xslt pass on it so we can
			//  merge all consecutive single-character runs into one. Word builds
			//  these (e.g. for multiple, consecutive Insert Symbol events), but we
			//  don't want them to be separate runs or we won't convert them as a
			//  block. And if we don't convert them as a block, then context effects
			//  (e.g. Greek final sigma) won't behave properly.
			string strXSLT = Properties.Resources.MergeSingleCharacterRunsWordML;
			MemoryStream streamXSLT = new MemoryStream(Encoding.UTF8.GetBytes(strXSLT));
#if DEBUG
			long lStartTime = DateTime.Now.Ticks;
#endif
			XmlReader xslReaderXSLT = XmlReader.Create(streamXSLT);

			XslCompiledTransform myProcessor = new XslCompiledTransform();
			XsltSettings xsltSettings = new XsltSettings { EnableScript = true };
			myProcessor.Load(xslReaderXSLT, xsltSettings, null);

			string strXsltOutputFilename;
			if (bSaveXmlOutputInFolder)
			{
				strXsltOutputFilename = String.Format(@"{0}\{1}{2}",
					Path.GetDirectoryName(strDocFilename),
					Path.GetFileName(strDocFilename),
					FontsStylesForm.cstrLeftXmlFileSuffixAfterXsltTransform);
				if (File.Exists(strXsltOutputFilename))
					File.Delete(strXsltOutputFilename);
			}
			else
			{
				strXsltOutputFilename = FontsStylesForm.GetTempFilename;
			}

			myProcessor.Transform(strXmlFilename, strXsltOutputFilename);
#if DEBUG
			long lDeltaTime = DateTime.Now.Ticks - lStartTime;
			System.Diagnostics.Debug.WriteLine(String.Format("Transform took: '{0}' ticks", lDeltaTime));
#endif

			strXmlFilename = strXsltOutputFilename;

			Word03MLDocument doc = new Word03MLDocument();
			doc.Load(strXmlFilename);
			doc.GetNameSpaceURIs(doc.DocumentElement);
			doc.InitXPathExpressions();

			// get the full list of potential font and style names (these aren't what we'll present
			//  to the user, because we'll only show those that have some text, but just to get a
			//  full list that we won't have to a) requery or b) look beyond)
			doc.GetFullNameLists(strDocFilename);

			return doc;
		}

		protected override string GetFindFontXPathExpression(List<string> astrFontsToSearchFor)
		{
			System.Diagnostics.Debug.Assert(astrFontsToSearchFor.Count > 0);

			// /w:wordDocument/w:fonts/w:font[@w:name = "SAG-IPA Super SILDoulos" or @w:name = "Annapurna"]
			string strFontname = String.Format(cstrXPathFindFontFormat, astrFontsToSearchFor[0]);
			for (int i = 1; i < astrFontsToSearchFor.Count; i++)
				strFontname += String.Format(" or " + cstrXPathFindFontFormat, astrFontsToSearchFor[i]);

			return String.Format(cstrXPathFindFontFormatExpression, strFontname);
		}
	}

#if !DefineWord07MLDocument
	// specifics for Word 2007
	public class Word07MLDocument : WordMLDocument
	{
		// XPath expressions
		// get the total list of fonts in the document
		// these are done when the xml file is first opened to get a huristic of the things that we might need to
		//  look for. If something doesn't show up in the list that ought to, it could be a problem from this list
		//  (e.g. if the user is looking for a character style that is based on a complex script, then the last
		//  entry here won't find it (since it's only looking for 'ascii')).
		public const string cstrXPathWordMLDefFontAscii = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:ascii";
		public const string cstrXPathWordMLDefFontFareast = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:eastAsia";
		public const string cstrXPathWordMLDefFontHAnsi = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:hAnsi";
		public const string cstrXPathWordMLDefFontCS = "/w:wordDocument/w:body//w:p/w:r[w:t]/w:rPr/w:rFonts/@w:cs";

		// get the total list of fonts in the document
		// these are done when the xml file is first opened to get a huristic of the things that we might need to
		//  look for. If something doesn't show up in the list that ought to, it could be a problem from this list
		//  (e.g. if the user is looking for a character style that is based on a complex script, then the last
		//  entry here won't find it (since it's only looking for 'ascii')).
		public const string cstrXPathWordMLGetDefaultPStyleFontName = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:docDefaults/w:rPrDefault/w:rPr/w:rFonts/@w:ascii";    // this only handles the lhs legacy case (but having a default complex doesn't seem to depend on this key)
		public const string cstrXPathWordMLGetSymbolTextFontNames = "NOT DETERMINED YET/w:body//w:p/w:r/w:sym/@w:font";
		public const string cstrXPathWordMLGetPStyleFontNames = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'paragraph']/w:rPr/wx:font/@wx:val";
		public const string cstrXPathWordMLGetCStyleFontNames = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'character']/w:rPr/w:rFonts/@w:ascii";    // this is only one of the 4 possibles, but at least it should cover the legacy to unicode conversion case

		public const string cstrXPathExprFontFareast = "w:rFonts/@w:eastAsia";
		public const string cstrXPathExprFontHAnsi = "w:rFonts/@w:hAnsi";

		// get the total list of style names and ids in the document (that have an associated font)
		//  (these are used to fill the m_astrFull{P,C}StyleNameList collections which are
		//  then iterated over during the searching of the file and also the mapping of style name to id
		public const string cstrXPathWordMLGetPStyle = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'paragraph'][w:rPr/wx:font/@wx:val]";
		public const string cstrXPathWordMLGetCStyle = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'character'][w:rPr/w:rFonts/@w:ascii]";

		// format to get the style id of a particular style name (as part of a complex search)
		public const string cstrXPathWordMLFormatGetPStyleId = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'paragraph'][w:name/@w:val = '{0}']/@w:styleId";
		public const string cstrXPathWordMLFormatGetCStyleId = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'character'][w:name/@w:val = '{0}']/@w:styleId";

		// formats for building XPath statements to get text entries for text
		//  the first one (for custom formatting) definitely needs to have "/w:r" because in some docs
		//  (e.g. the MTT manual) that node is embedded in different things. The style based ones (i.e. the latter two)
		//  might also should be "/w:r", but it really blows the time of search out greatly, so I'm taking it out until
		//  I know for sure whether it can happen or not
		public const string cstrXPathWordMLFormatGetFontText = "/w:wordDocument/w:body//w:p//w:r[w:rPr/wx:font/@wx:val = '{0}']/w:t";

		public const string cstrXPathWordMLFormatGetSymbolFontChar = "NOT DETERMINED YET: /w:body//w:p//w:r/w:sym[@w:font = '{0}']";

		public const string cstrXPathWordMLFormatGetDefaultPStyleFontText = "/w:wordDocument/w:body//w:p/w:r[not(w:rPr)]/w:t";
		public const string cstrXPathWordMLFormatGetPStyleFontText = "/w:wordDocument/w:body//w:p[w:pPr/w:pStyle/@w:val = //w:styles/w:style[@w:type = 'paragraph'][w:rPr/wx:font/@wx:val = '{0}']/@w:styleId]/w:r[not(w:rPr/wx:font)]/w:t";
		public const string cstrXPathWordMLFormatGetCStyleFontText = "/w:wordDocument/w:body//w:p//w:r[w:rPr/w:rStyle/@w:val = //w:styles/w:style[@w:type = 'character']/@w:styleId][w:rPr/wx:font/@wx:val = '{0}']/w:t";
		public const string cstrXPathWordMLFormatGetPStyleText = "/w:wordDocument/w:body//w:p[w:pPr/w:pStyle/@w:val = '{0}']/w:r[not(w:rPr/wx:font)]/w:t";
		public const string cstrXPathWordMLFormatGetCStyleText = "/w:wordDocument/w:body//w:p/w:r[w:rPr/w:rStyle/@w:val = '{0}']/w:t";

		public const string cstrXPathWordMLFormatReplaceFontNameGetFontText = "/w:wordDocument/w:body//w:p//w:r[w:rPr/wx:font/@wx:val = '{0}']/w:rPr";
		public const string cstrXPathWordMLFormatReplaceFontNameNoRunParagraphs = "/w:wordDocument/w:body//w:p[w:pPr/w:rPr/wx:font/@wx:val = '{0}']/w:pPr/w:rPr";
		public const string cstrXPathWordMLFormatReplaceFontNameNoRunCsParagraphs = "/w:wordDocument/w:body//w:p[w:pPr/w:rPr/w:rFonts/@w:cs = '{0}']/w:pPr/w:rPr";

		public const string cstrXPathWordMLFormatReplaceFontNameGetSymbolFontText = "NOT DETERMINED YET: /w:body//w:p//w:r[w:sym/@w:font = '{0}']/w:rPr";

		public const string cstrXPathWordMLFormatReplaceFontNameGetPStyleFontName = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'paragraph']/w:rPr[wx:font/@wx:val = '{0}']";
		public const string cstrXPathWordMLFormatReplaceFontNameGetCStyleFontName = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[@w:type = 'character']/w:rPr[w:rFonts/@w:ascii = '{0}']";
		public const string cstrXPathWordMLFormatReplaceFontNameGetStyleText = "/pkg:package/pkg:part[@pkg:name='/word/styles.xml']/pkg:xmlData/w:styles/w:style[w:name/@w:val = '{0}']/w:rPr";

		public override string XPathFormatGetFontText
		{
			get { return cstrXPathWordMLFormatGetFontText; }
		}
		public override string XPathFormatGetSymbolFontText
		{
			get { return cstrXPathWordMLFormatGetSymbolFontChar; }
		}
		public override string XPathFormatGetDefaultPStyleFontText
		{
			get { return cstrXPathWordMLFormatGetDefaultPStyleFontText; }
		}
		public override string XPathFormatGetPStyleFontText
		{
			get { return cstrXPathWordMLFormatGetPStyleFontText; }
		}
		public override string XPathFormatGetCStyleFontText
		{
			get { return cstrXPathWordMLFormatGetCStyleFontText; }
		}
		public override string XPathFormatGetPStyleText
		{
			get { return cstrXPathWordMLFormatGetPStyleText; }
		}
		public override string XPathFormatGetCStyleText
		{
			get { return cstrXPathWordMLFormatGetCStyleText; }
		}
		protected override string XPathGetDefPStyleFontName
		{
			get { return cstrXPathWordMLGetDefaultPStyleFontName; }
		}
		protected override string XPathGetSymbolFontName
		{
			get { return cstrXPathWordMLGetSymbolTextFontNames; }
		}
		protected override string XPathGetPStyleFontNames
		{
			get { return cstrXPathWordMLGetPStyleFontNames; }
		}
		protected override string XPathGetCStyleFontNames
		{
			get { return cstrXPathWordMLGetCStyleFontNames; }
		}
		protected override string XPathGetPStyle
		{
			get { return cstrXPathWordMLGetPStyle; }
		}
		protected override string XPathGetCStyle
		{
			get { return cstrXPathWordMLGetCStyle; }
		}
		protected override string XPathExprFontFareast
		{
			get { return cstrXPathExprFontFareast; }
		}
		protected override string XPathExprFontHAnsi
		{
			get { return cstrXPathExprFontHAnsi; }
		}

		// if we replace the name of the font (i.e. //wx:font/@wx:val) associated with some text (i.e. [w:t]),
		//  then we must also replace the nearby //w:rFonts/@w:* items or Word crashes
		public override void ReplaceTextFontNameGetFontText(string strOldFontName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetFontText, strOldFontName, strNewFontName);
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameNoRunParagraphs, strOldFontName, strNewFontName);
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameNoRunCsParagraphs, strOldFontName, strNewFontName);
		}

		public override void ReplaceSymbolTextFontNameGetFontText(string strOldFontName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetSymbolFontText, strOldFontName, strNewFontName);
		}

		public override void ReplaceTextFontNameGetPStyleFontText(string strOldFontName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetPStyleFontName, strOldFontName, strNewFontName);
		}

		public override void ReplaceTextFontNameGetCStyleFontText(string strOldFontName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetCStyleFontName, strOldFontName, strNewFontName);
		}

		public override void ReplaceTextFontNameGetStyleText(string strStyleName, string strNewFontName)
		{
			ReplaceTextNameFormatAttribs(cstrXPathWordMLFormatReplaceFontNameGetStyleText, strStyleName, strNewFontName);
		}

		protected override void GetCustomFontLists()
		{
			// first look in these places to get the actual full list of potential font names
			GetFullNameList(cstrXPathWordMLDefFontAscii, ref lstFontNamesCustom);
			GetFullNameList(cstrXPathWordMLDefFontFareast, ref lstFontNamesCustom);
			GetFullNameList(cstrXPathWordMLDefFontHAnsi, ref lstFontNamesCustom);
			GetFullNameList(cstrXPathWordMLDefFontCS, ref lstFontNamesCustom);
		}

		public static DocXmlDocument GetXmlDocument(string strXmlFilename)
		{
			Word07MLDocument doc = new Word07MLDocument();
			doc.Load(strXmlFilename);
			doc.GetNameSpaceURIs(doc.DocumentElement);
			doc.InitXPathExpressions();

			// get the full list of potential font and style names (these aren't what we'll present
			//  to the user, because we'll only show those that have some text, but just to get a
			//  full list that we won't have to a) requery or b) look beyond)
			doc.GetFullNameLists();

			return doc;
		}
	}
#endif
}
