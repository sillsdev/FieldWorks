// Copyright (c) 2010-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel.Core.WritingSystems;
using SIL.Xml;

namespace LanguageExplorer.Controls.LexText.DataNotebook
{
	public class CharMapping
	{
		private string m_sBeginMkr;
		private string m_sEndMkr;
		private bool m_fEndWithWord;
		private string m_sDestWsId;
		private CoreWritingSystemDefinition m_ws;
		private string m_sDestStyle;
		private bool m_fIgnoreMarker;

		public CharMapping()
		{
		}

		public CharMapping(XmlNode xn)
		{
			m_sBeginMkr = XmlUtils.GetMandatoryAttributeValue(xn, "begin");
			m_sEndMkr = XmlUtils.GetMandatoryAttributeValue(xn, "end");
			m_fEndWithWord = XmlUtils.GetOptionalBooleanAttributeValue(xn, "endWithWord", false);
			m_fIgnoreMarker = XmlUtils.GetOptionalBooleanAttributeValue(xn, "ignore", false);
			m_sDestStyle = XmlUtils.GetOptionalAttributeValue(xn, "style", null);
			m_sDestWsId = XmlUtils.GetOptionalAttributeValue(xn, "ws", null);
		}

		public string BeginMarker
		{
			get { return m_sBeginMkr; }
			set { m_sBeginMkr = value; }
		}

		public string EndMarker
		{
			get { return m_sEndMkr; }
			set { m_sEndMkr = value; }
		}

		public bool EndWithWord
		{
			get { return m_fEndWithWord; }
			set { m_fEndWithWord = value; }
		}

		public string DestinationWritingSystemId
		{
			get { return m_sDestWsId; }
			set { m_sDestWsId = value; }
		}

		public CoreWritingSystemDefinition DestinationWritingSystem
		{
			get { return m_ws; }
			set { m_ws = value; }
		}

		public string DestinationStyle
		{
			get { return m_sDestStyle; }
			set { m_sDestStyle = value; }
		}

		public bool IgnoreMarkerOnImport
		{
			get { return m_fIgnoreMarker; }
			set { m_fIgnoreMarker = value; }
		}
	}
}