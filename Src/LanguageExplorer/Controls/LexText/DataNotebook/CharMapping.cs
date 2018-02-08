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
		public CharMapping()
		{
		}

		public CharMapping(XmlNode xn)
		{
			BeginMarker = XmlUtils.GetMandatoryAttributeValue(xn, "begin");
			EndMarker = XmlUtils.GetMandatoryAttributeValue(xn, "end");
			EndWithWord = XmlUtils.GetOptionalBooleanAttributeValue(xn, "endWithWord", false);
			IgnoreMarkerOnImport = XmlUtils.GetOptionalBooleanAttributeValue(xn, "ignore", false);
			DestinationStyle = XmlUtils.GetOptionalAttributeValue(xn, "style", null);
			DestinationWritingSystemId = XmlUtils.GetOptionalAttributeValue(xn, "ws", null);
		}

		public string BeginMarker { get; set; }

		public string EndMarker { get; set; }

		public bool EndWithWord { get; set; }

		public string DestinationWritingSystemId { get; set; }

		public CoreWritingSystemDefinition DestinationWritingSystem { get; set; }

		public string DestinationStyle { get; set; }

		public bool IgnoreMarkerOnImport { get; set; }
	}
}