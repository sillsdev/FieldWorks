// Copyright (c) 2015-2018 SIL International
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
		private readonly XElement m_item;
		public OptionListItem(XElement item)
		{
			m_item = item;
		}

		public XElement Item => m_item;

		public override string ToString()
		{
			return XmlUtils.GetMandatoryAttributeValue(m_item, "label");
		}

	}
}