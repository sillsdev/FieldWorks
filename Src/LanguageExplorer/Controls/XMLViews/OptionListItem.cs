// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Used for items in optionsList
	/// </summary>
	internal class OptionListItem
	{
		public OptionListItem(XElement item)
		{
			Item = item;
		}

		public XElement Item { get; }

		public override string ToString()
		{
			return XmlUtils.GetMandatoryAttributeValue(Item, "label");
		}
	}
}