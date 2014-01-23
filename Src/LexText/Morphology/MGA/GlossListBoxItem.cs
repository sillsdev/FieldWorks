// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: GLossListBoxItem.cs
// Responsibility: Andy Black
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.LexText.Controls.MGA
{
	// NB: I'd prefer to subclass XmlNode and override its ToString() class.
	//     When I tried that, however, it appears that XmlNode is protected and one
	//     cannot inherit from it.
	public class GlossListBoxItem
	{
		private string m_sAbbrev;
		private string m_sTerm;
		private string m_sAfterSeparator;
		private string m_sComplexNameSeparator;
		private bool m_fComplexNameFirst;
		private bool m_fIsComplex;
		private bool m_fIsValue;
		private readonly FdoCache m_cache;
		private XmlNode m_xmlNode;
		private IMoGlossItem m_glossItem;

		#region Construction
		public GlossListBoxItem(FdoCache cache, XmlNode node, string sAfterSeparator, string sComplexNameSeparator, bool fComplexNameFirst)
		{
			if (cache == null) throw new ArgumentNullException("cache");

			m_cache = cache;
			m_xmlNode = node;
			SetValues(node, sAfterSeparator, sComplexNameSeparator, fComplexNameFirst);
			m_glossItem = m_cache.ServiceLocator.GetInstance<IMoGlossItemFactory>().Create();
		}

		private void SetValues(XmlNode node, string sAfterSeparator, string sComplexNameSeparator, bool fComplexNameFirst)
		{
			XmlNode xn = node.SelectSingleNode("term");
			if (xn == null)
				m_sTerm = MGAStrings.ksUnknownTerm;
			else
				m_sTerm = xn.InnerText;
			xn = node.SelectSingleNode("abbrev");
			if (xn == null)
				m_sAbbrev = MGAStrings.ksUnknownTerm;
			else
				m_sAbbrev = xn.InnerText;
			XmlNode attr = m_xmlNode.Attributes.GetNamedItem("afterSeparator");
			if (attr == null)
				m_sAfterSeparator = sAfterSeparator;
			else
				m_sAfterSeparator = attr.Value;
			attr = m_xmlNode.Attributes.GetNamedItem("complexNameSeparator");
			if (attr == null)
				m_sComplexNameSeparator = sComplexNameSeparator;
			else
				m_sComplexNameSeparator= attr.Value;
			attr = m_xmlNode.Attributes.GetNamedItem("complexNameFirst");
			if (attr == null)
				m_fComplexNameFirst = fComplexNameFirst;
			else
				m_fComplexNameFirst = XmlUtils.GetBooleanAttributeValue(attr.Value);
			SetType();
		}

		private void SetType()
		{
			XmlNode attr;
			attr = m_xmlNode.Attributes.GetNamedItem("type");
			if (attr != null)
			{
				switch (attr.Value)
				{
					case "complex":
						m_fIsComplex = true;
						m_fIsValue = false;
						break;
					case "value":
						m_fIsComplex = false;
						m_fIsValue = true;
						break;
					default:
						m_fIsComplex = false;
						m_fIsValue = false;
						break;
				}
			}
			else
			{
				XmlNode itemDaughter = m_xmlNode.SelectSingleNode("item");
				if (itemDaughter == null)
				{
					m_fIsComplex = false;
					m_fIsValue = true;
				}
				else
				{
					m_fIsComplex = false;
					m_fIsValue = false;
				}
			}
		}
		#endregion
		#region properties
		/// <summary>
		/// Gets the abbreviation of the item.
		/// </summary>
		public string Abbrev
		{
			get
			{
				return m_sAbbrev;
			}
		}
		/// <summary>
		/// Gets default after separator character for glossing.
		/// </summary>
		public string AfterSeparator
		{
			get
			{
				return m_sAfterSeparator;
			}
		}
		/// <summary>
		/// Gets flag whether the name of the complex item comes first or not.
		/// </summary>
		public bool ComplexNameFirst
		{
			get
			{
				return m_fComplexNameFirst;
			}
		}
		/// <summary>
		/// Gets default separator character to occur after a complex name in glossing.
		/// </summary>
		public string ComplexNameSeparator
		{
			get
			{
				return m_sComplexNameSeparator;
			}
		}
		/// <summary>
		/// Gets flag whether the item is complex or not.
		/// </summary>
		public bool IsComplex
		{
			get
			{
				return m_fIsComplex;
			}
		}
		/// <summary>
		/// Gets flag whether the item is a feature value or not.
		/// </summary>
		public bool IsValue
		{
			get
			{
				return m_fIsValue;
			}
		}
		/// <summary>
		/// Gets the MoGlossItem of the item.
		/// </summary>
		public IMoGlossItem MoGlossItem
		{
			get
			{
				return m_glossItem;
			}
		}
		/// <summary>
		/// Gets the term definition of the item.
		/// </summary>
		public string Term
		{
			get
			{
				return m_sTerm;
			}
		}
		/// <summary>
		/// Gets/sets the XmlNode of the item.
		/// </summary>
		public XmlNode XmlNode
		{
			get
			{
				return m_xmlNode;
			}
			set
			{
				m_xmlNode = value;
			}
		}
		#endregion
		public override string ToString()
		{
			return String.Format(MGAStrings.ksX_Y, m_sTerm, m_sAbbrev);
		}
#if UsingGlossSystem
		/// <summary>
		/// Add the item to the language database
		/// </summary>
		/// <param name="cache">FDO cache to use</param>
		public void AddToDataBase(FdoCache cache)
		{
			ILangProject lp=cache.LangProject;
			IMoMorphData md = lp.MorphologicalDataOA;
			IMoGlossSystem gs = md.GlossSystemOA;

			XmlNode parent = m_xmlNode.ParentNode;
			if (parent.Name != "item")
			{ // is a top level item; find it or add it
				IMoGlossItem giFound = gs.FindEmbeddedItem(Term, Abbrev, false);
				if (giFound == null)
				{ // not found, so add it
					gs.GlossesOC.Add(m_glossItem);
				}
			}
			else
			{ // not at top level; get parent and add it to parent;
				// also create any missing items between this node and the top
				IMoGlossItem giParent = GetMyParentGlossItem(cache, parent);
				giParent.GlossItemsOS.Append(m_glossItem);
			}
			FillInGlossItemBasedOnXmlNode(m_glossItem, m_xmlNode, this);
			CreateFeatStructFrag();
		}
		/// <summary>
		/// Get parent MoGlossItem and fill in any missing items between the parent and the top level.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="node"></param>
		/// <returns>The MoGlossItem object which is or is to be the parent of this item.</returns>
		private IMoGlossItem GetMyParentGlossItem(FdoCache cache, XmlNode node)
		{
			ILangProject lp=cache.LangProject;
			IMoMorphData md = lp.MorphologicalDataOA;
			IMoGlossSystem gs = md.GlossSystemOA;

			System.Xml.XmlNode parent = node.ParentNode;
#if NUnitDebug
			Console.WriteLine("gmpgi: working on " + XmlUtils.GetAttributeValue(node, "id"));
#endif
			if (parent.Name != "item")
			{ // is a top level item; find it or add it
#if NUnitDebug
				Console.WriteLine("gmpgi: found top");
#endif
				MIoGlossItem giFound = gs.FindEmbeddedItem(XmlUtils.GetAttributeValue(node, "term"),
					XmlUtils.GetAttributeValue(node, "abbrev"), false);
				if (giFound == null)
				{ // not found; so add it
					IMoGlossItem gi = new MoGlossItem();
					gs.GlossesOC.Add(gi);
					FillInGlossItemBasedOnXmlNode(gi, node, this);
#if NUnitDebug
					Console.WriteLine("gmpgi, found top, made new, returning ", gi.Name.AnalysisDefaultWritingSystem);
#endif
					return gi;
				}
				else
				{ //found, so return it
#if NUnitDebug
					Console.WriteLine("gmpgi, found top, exists, returning ", giFound.Name.AnalysisDefaultWritingSystem);
#endif
					return giFound;
				}
			}
			else
			{  // not a top level item; get its parent and add it, if need be
#if NUnitDebug
				Console.WriteLine("gmpgi: calling parent of " + XmlUtils.GetAttributeValue(node, "id"));
#endif
				IMoGlossItem giParent = GetMyParentGlossItem(cache, parent);
				IMoGlossItem giFound = giParent.FindEmbeddedItem(XmlUtils.GetAttributeValue(node, "term"),
					XmlUtils.GetAttributeValue(node, "abbrev"), false);
				if (giFound == null)
				{ // not there, add it
#if NUnitDebug
					Console.WriteLine("gmpgi: adding a node");
#endif
					giFound = new MoGlossItem();
					giParent.GlossItemsOS.Append(giFound);
					FillInGlossItemBasedOnXmlNode(giFound, node, this);
				}
#if NUnitDebug
				Console.WriteLine("gmpgi, in middle, returning " + giFound.Name.AnalysisDefaultWritingSystem + " for node " + XmlUtils.GetAttributeValue(node, "id"));
#endif
				return giFound;
			}
		}
		/// <summary>
		/// Fill in the attributes of a MoGlossItem object based on its corresponding XML node in the etic gloss list tree
		/// </summary>
		/// <param name="gi"></param>
		/// <param name="xn"></param>
		/// <param name="glbi"></param>
		private void FillInGlossItemBasedOnXmlNode(MoGlossItem gi, XmlNode xn, GlossListBoxItem glbi)
		{
			XmlNode attr = xn.Attributes.GetNamedItem("term");
			if (attr == null)
				gi.Name.AnalysisDefaultWritingSystem = MGAStrings.ksUnknownTerm;
			else
				gi.Name.AnalysisDefaultWritingSystem = attr.Value;
			attr = xn.Attributes.GetNamedItem("abbrev");
			if (attr == null)
				gi.Abbreviation.AnalysisDefaultWritingSystem = MGAStrings.ksUnknownAbbreviation;
			else
				gi.Abbreviation.AnalysisDefaultWritingSystem = attr.Value;
			attr = xn.Attributes.GetNamedItem("afterSeparator");
			if (attr == null)
				gi.AfterSeparator = this.AfterSeparator;
			else
				gi.AfterSeparator = attr.Value;
			attr = xn.Attributes.GetNamedItem("complexNameSeparator");
			if (attr == null)
				gi.ComplexNameSeparator = this.ComplexNameSeparator;
			else
				gi.ComplexNameSeparator= attr.Value;
			attr = xn.Attributes.GetNamedItem("complexNameFirst");
			if (attr == null)
				gi.ComplexNameFirst = this.ComplexNameFirst;
			else
				gi.ComplexNameFirst = XmlUtils.GetBooleanAttributeValue(attr.Value);
			attr = xn.Attributes.GetNamedItem("type");
			attr = xn.Attributes.GetNamedItem("status");
			if (attr == null)
				gi.Status = true;
			else
			{
				if (attr.Value == "visible")
					gi.Status = true;
				else
					gi.Status = false;
			}
			attr = xn.Attributes.GetNamedItem("type");
			if (attr == null)
				gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.unknown;
			else
			{
				switch(attr.Value)
				{
					case "complex":
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.complex;
						break;
					case "deriv":
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.deriv;
						break;
					case "feature":
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.feature;
						break;
					case "fsType":
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.fsType;
						break;
					case "group":
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.group;
						break;
					case "value":
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.inflValue;
						break;
					case "xref":
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.xref;
						break;
					default:
						gi.Type = (int)SIL.FieldWorks.FDO.Ling.MoGlossItem.ItemType.unknown;
						break;
				}
			}
		}
		private void CreateFeatStructFrag()
		{

		}
#endif
	}
}
