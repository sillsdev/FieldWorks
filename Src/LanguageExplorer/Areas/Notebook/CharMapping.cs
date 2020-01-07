// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.LCModel.Core.WritingSystems;
using SIL.Xml;

namespace LanguageExplorer.Areas.Notebook
{
	internal sealed class CharMapping
	{
		internal CharMapping()
		{
		}

		internal CharMapping(XmlNode xn)
		{
			BeginMarker = XmlUtils.GetMandatoryAttributeValue(xn, "begin");
			EndMarker = XmlUtils.GetMandatoryAttributeValue(xn, "end");
			EndWithWord = XmlUtils.GetOptionalBooleanAttributeValue(xn, "endWithWord", false);
			IgnoreMarkerOnImport = XmlUtils.GetOptionalBooleanAttributeValue(xn, "ignore", false);
			DestinationStyle = XmlUtils.GetOptionalAttributeValue(xn, "style", null);
			DestinationWritingSystemId = XmlUtils.GetOptionalAttributeValue(xn, "ws", null);
		}

		internal string BeginMarker { get; set; }

		internal string EndMarker { get; set; }

		internal bool EndWithWord { get; set; }

		internal string DestinationWritingSystemId { get; set; }

		internal CoreWritingSystemDefinition DestinationWritingSystem { get; set; }

		internal string DestinationStyle { get; set; }

		internal bool IgnoreMarkerOnImport { get; set; }
	}
}