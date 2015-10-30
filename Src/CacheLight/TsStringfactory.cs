// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.CacheLight
{
	/// <summary>
	/// This class helps the RealCacheLoader class by creating ITsStrings from Run XML elements.
	/// </summary>
	internal sealed class TsStringfactory
	{
		private ITsStrFactory m_tsf;
		private readonly Dictionary<string, int> m_wsCache = new Dictionary<string, int>();

		public TsStringfactory(ITsStrFactory tsf, Dictionary<string, int> wsCache)
		{
			m_tsf = tsf;
			m_wsCache = wsCache;
		}

		public ITsString CreateFromAStr(XmlNode aStrNode, out int wsAStr)
		{
			wsAStr = m_wsCache[aStrNode.Attributes["ws"].Value];
			var tisb = m_tsf.GetIncBldr();
			tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, wsAStr);
			ProcessRunElements(aStrNode.ChildNodes, tisb);

			return tisb.GetString();
		}

		public ITsString CreateFromStr(XmlNode strNode)
		{
			var tisb = m_tsf.GetIncBldr();
			ProcessRunElements(strNode.ChildNodes, tisb);

			return tisb.GetString();
		}

		private void ProcessRunElements(XmlNodeList runNodes, ITsIncStrBldr tisb)
		{
			foreach (XmlNode runNode in runNodes)
				ProcessRunElement(runNode, tisb);
		}

		private void ProcessRunElement(XmlNode runNode, ITsIncStrBldr tisb)
		{
			/*
(From DTD, as of 9/18/2006)
<!ELEMENT Run (#PCDATA)>
<!ATTLIST Run
	type (chars | picture) #IMPLIED
	ownlink CDATA #IMPLIED
	contextString CDATA #IMPLIED
	backcolor CDATA #IMPLIED
	bold (invert | off | on) #IMPLIED
	ws CDATA #REQUIRED
	wsBase CDATA #IMPLIED
	externalLink CDATA #IMPLIED
	fontFamily CDATA #IMPLIED
	fontsize CDATA #IMPLIED
	fontsizeUnit CDATA #IMPLIED
	forecolor CDATA #IMPLIED
	italic (invert | off | on) #IMPLIED
	link CDATA #IMPLIED
	namedStyle CDATA #IMPLIED
	offset CDATA #IMPLIED
	offsetUnit CDATA #IMPLIED
	superscript (sub | super) #IMPLIED
	tabList CDATA #IMPLIED
	tags IDREFS #IMPLIED
	undercolor CDATA #IMPLIED
	underline (dashed | dotted | double | none | single | squiggle | strikethrough) #IMPLIED
>
Stephen McCon...	The relevant source file is FwXmlString.cpp in {FW}/Src/Cellar.  Unfortunately, this area is somewhat of a mess.
Stephen McCon...	The attr list is probably found in FwDatabase.dtd, without explanation.
Randy Regnier		If I understand it right, the various attrs will control some properties for the ts string, such as ws, bold, etc, right?
Stephen McCon...	that's correct.
Stephen McCon...	You could look at FwXmlImportData::ProcessStringStartTag(const XML_Char *, const XML_Char **) in FwXmlString.cpp to see how the attributes are scanned and stored when reading in a string in the various C++ XML parsers.
Stephen McCon...	(Of course, that calls all sorts of other methods to do the work, which you may have to look at as well)
			*/
			// Process all of the properties, before adding the string (says JohnT, personal communication).
			foreach (XmlAttribute attr in runNode.Attributes)
			{
				switch (attr.Name)
				{
					case "ws":
						// ws CDATA #REQUIRED
						tisb.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_wsCache[attr.Value]);
						break;
					case "type":
						// type (chars | picture) #IMPLIED
						// <Run ws=\"en\"
						break;
					case "ownlink":
						// ownlink CDATA #IMPLIED
						// <Run ws=\"xkal\" ownlink=\"993DA544-3B38-4AB0-BFDE-9AF884571A2D\"></Run>
						break;
					case "contextString":
						// contextString CDATA #IMPLIED
						// <Run ws=\"en\" contextString=\"85EE15C6-0799-46C6-8769-F9B3CE313AE2\"></Run>
						break;
					case "backcolor":
						// backcolor CDATA #IMPLIED
						break;
					case "bold":
						// bold (invert | off | on) #IMPLIED
						break;
					case "wsBase":
						// wsBase CDATA #IMPLIED
						break;
					case "externalLink":
						// externalLink CDATA #IMPLIED
						break;
					case "fontFamily":
						// fontFamily CDATA #IMPLIED
						// <Run ws=\"en\" fontsize=\"16000\" forecolor=\"blue\" fontFamily=\"SILDoulos PigLatinDemo\">Welcome to WorldPad!</Run>
						break;
					case "fontsize":
						// fontsize CDATA #IMPLIED
						break;
					case "fontsizeUnit":
						// fontsizeUnit CDATA #IMPLIED
						break;
					case "forecolor":
						// forecolor CDATA #IMPLIED
						// <Run ws=\"en\" fontsize=\"20000\" forecolor=\"red\">French IPA: </Run>
						// <Run ws=\"xsta\" fontsize=\"20000\" forecolor=\"007f00\">We</Run>
						break;
					case "italic":
						// italic (invert | off | on) #IMPLIED
						// <Run ws=\"en\" italic=\"on\">man</Run>
						break;
					case "link":
						// link CDATA #IMPLIED
						// <Run ws=\"en\" link=\"6EBC80F8-9CE0-45D9-BB49-3869591FB1FF\"></Run>
						break;
					case "namedStyle":
						// <Run ws=\"en\" namedStyle=\"Emphasized Text\">pirana</Run>
						break;
					case "offset":
						// offset CDATA #IMPLIED
						break;
					case "offsetUnit":
						// offsetUnit CDATA #IMPLIED
						break;
					case "superscript":
						// superscript (sub | super) #IMPLIED
						break;
					case "tabList":
						// tabList CDATA #IMPLIED
						break;
					case "tags":
						// tags IDREFS #IMPLIED
						// <Run ws=\"en\" tags=\"I2BDD0E8D-F9B2-11D3-977B-00C04F186933\">Tiga</Run>
						break;
					case "undercolor":
						// undercolor CDATA #IMPLIED
						break;
					case "underline":
						// underline (dashed | dotted | double | none | single | squiggle | strikethrough) #IMPLIED
						break;
					default:
						// Unrecognized attr, so do nothing.
						Debug.WriteLine(String.Format("Unrecognized <Run> element attribute: {0}", attr.Name));
						break;
				}
			}
			tisb.Append(runNode.InnerText);
		}
	}
}
