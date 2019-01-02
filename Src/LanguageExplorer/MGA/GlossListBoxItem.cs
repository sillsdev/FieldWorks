// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml;
using SIL.Code;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.MGA
{
	// NB: I'd prefer to subclass XmlNode and override its ToString() class.
	//     When I tried that, however, it appears that XmlNode is protected and one
	//     cannot inherit from it.
	public class GlossListBoxItem
	{
		private readonly LcmCache m_cache;

		#region Construction
		public GlossListBoxItem(LcmCache cache, XmlNode node, string sAfterSeparator, string sComplexNameSeparator, bool fComplexNameFirst)
		{
			Guard.AgainstNull(cache, nameof(cache));

			m_cache = cache;
			XmlNode = node;
			SetValues(node, sAfterSeparator, sComplexNameSeparator, fComplexNameFirst);
			MoGlossItem = m_cache.ServiceLocator.GetInstance<IMoGlossItemFactory>().Create();
		}

		private void SetValues(XmlNode node, string sAfterSeparator, string sComplexNameSeparator, bool fComplexNameFirst)
		{
			var xn = node.SelectSingleNode("term");
			Term = xn?.InnerText ?? MGAStrings.ksUnknownTerm;
			xn = node.SelectSingleNode("abbrev");
			Abbrev = xn?.InnerText ?? MGAStrings.ksUnknownTerm;
			var attr = XmlNode.Attributes.GetNamedItem("afterSeparator");
			AfterSeparator = attr == null ? sAfterSeparator : attr.Value;
			attr = XmlNode.Attributes.GetNamedItem("complexNameSeparator");
			ComplexNameSeparator = attr == null ? sComplexNameSeparator : attr.Value;
			attr = XmlNode.Attributes.GetNamedItem("complexNameFirst");
			ComplexNameFirst = attr == null ? fComplexNameFirst : XmlUtils.GetBooleanAttributeValue(attr.Value);
			SetType();
		}

		private void SetType()
		{
			var attr = XmlNode.Attributes.GetNamedItem("type");
			if (attr != null)
			{
				switch (attr.Value)
				{
					case "complex":
						IsComplex = true;
						IsValue = false;
						break;
					case "value":
						IsComplex = false;
						IsValue = true;
						break;
					default:
						IsComplex = false;
						IsValue = false;
						break;
				}
			}
			else
			{
				var itemDaughter = XmlNode.SelectSingleNode("item");
				if (itemDaughter == null)
				{
					IsComplex = false;
					IsValue = true;
				}
				else
				{
					IsComplex = false;
					IsValue = false;
				}
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// Gets the abbreviation of the item.
		/// </summary>
		public string Abbrev { get; private set; }

		/// <summary>
		/// Gets default after separator character for glossing.
		/// </summary>
		public string AfterSeparator { get; private set; }

		/// <summary>
		/// Gets flag whether the name of the complex item comes first or not.
		/// </summary>
		public bool ComplexNameFirst { get; private set; }

		/// <summary>
		/// Gets default separator character to occur after a complex name in glossing.
		/// </summary>
		public string ComplexNameSeparator { get; private set; }

		/// <summary>
		/// Gets flag whether the item is complex or not.
		/// </summary>
		public bool IsComplex { get; private set; }

		/// <summary>
		/// Gets flag whether the item is a feature value or not.
		/// </summary>
		public bool IsValue { get; private set; }

		/// <summary>
		/// Gets the MoGlossItem of the item.
		/// </summary>
		public IMoGlossItem MoGlossItem { get; }

		/// <summary>
		/// Gets the term definition of the item.
		/// </summary>
		public string Term { get; private set; }

		/// <summary>
		/// Gets/sets the XmlNode of the item.
		/// </summary>
		public XmlNode XmlNode { get; set; }

		#endregion
		public override string ToString()
		{
			return string.Format(MGAStrings.ksX_Y, Term, Abbrev);
		}
	}
}