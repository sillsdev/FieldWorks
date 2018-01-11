// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel;
using SIL.Xml;

namespace LanguageExplorer.MGA
{
	internal class MasterItem
	{
		protected ImageKind m_eKind;
		protected string m_abbrev;
		protected string m_abbrevWs;
		protected string m_term;
		protected string m_termWs;
		protected string m_def;
		protected string m_defWs;
		protected List<MasterItemCitation> m_citations;
		protected XmlNode m_node;
		protected bool m_fInDatabase = false;
		protected IFsFeatDefn m_featDefn = null;

		public MasterItem()
		{
			m_citations = new List<MasterItemCitation>();
		}

		internal MasterItem(XmlNode node, ImageKind kind, string sTerm)
		{
			m_node = node;
			m_eKind = kind;
			m_term = sTerm;

			m_citations = new List<MasterItemCitation>();

			var nd = node.SelectSingleNode("abbrev");
			m_abbrevWs = XmlUtils.GetMandatoryAttributeValue(nd, "ws");
			m_abbrev = nd.InnerText;

			nd = node.SelectSingleNode("term");
			m_termWs = XmlUtils.GetMandatoryAttributeValue(nd, "ws");
			m_term = nd.InnerText;

			nd = node.SelectSingleNode("def");
			if (nd != null)
			{
				m_defWs = XmlUtils.GetMandatoryAttributeValue(nd, "ws");
				m_def = nd.InnerText;
			}

			foreach (XmlNode citNode in node.SelectNodes("citation"))
			{
				var sWs = XmlUtils.GetOptionalAttributeValue(citNode, "ws") ?? "en";
				m_citations.Add(new MasterItemCitation(sWs, citNode.InnerText));
			}
		}

		/// <summary>
		/// figure out if the feature represented by the node is already in the database
		/// </summary>
		/// <param name="cache">database cache</param>
		public virtual void DetermineInDatabase(LcmCache cache)
		{
		}
		public virtual bool KindCanBeInDatabase()
		{
			return (m_eKind == ImageKind.radio ||
				m_eKind == ImageKind.radioSelected ||
				m_eKind == ImageKind.checkBox ||
				m_eKind == ImageKind.checkedBox ||
				m_eKind == ImageKind.userChoice ||
				m_eKind == ImageKind.complex);
		}

		public virtual void AddToDatabase(LcmCache cache)
		{
		}

		public IFsFeatDefn FeatureDefn => m_featDefn;

		public XmlNode Node => m_node;

		public bool InDatabase => m_fInDatabase;

		public bool IsChosen => (m_eKind == ImageKind.radioSelected || m_eKind == ImageKind.checkedBox);

		public override string ToString()
		{
			return InDatabase ? string.Format(MGAStrings.ksX_InFwProject, m_term) : m_term;
		}

		public void ResetDescription(RichTextBox rtbDescription)
		{
			rtbDescription.Clear();

			var doubleNewLine = Environment.NewLine + Environment.NewLine;

			var original = rtbDescription.SelectionFont;
			var fntBold = new Font(original.FontFamily, original.Size, FontStyle.Bold);
			var fntItalic = new Font(original.FontFamily, original.Size, FontStyle.Italic);
			rtbDescription.SelectionFont = fntBold;
			rtbDescription.AppendText(m_term);
			rtbDescription.AppendText(doubleNewLine);

			rtbDescription.SelectionFont = (string.IsNullOrEmpty(m_def)) ? fntItalic : original;
			rtbDescription.AppendText((string.IsNullOrEmpty(m_def)) ? MGAStrings.ksNoDefinitionForItem : m_def);
			rtbDescription.AppendText(doubleNewLine);

			if (m_citations.Count <= 0)
			{
				return;
			}
			rtbDescription.SelectionFont = fntItalic;
			rtbDescription.AppendText(MGAStrings.ksReferences);
			rtbDescription.AppendText(doubleNewLine);

			rtbDescription.SelectionFont = original;
			foreach (var mifc in m_citations)
			{
				mifc.ResetDescription(rtbDescription);
			}
		}
	}
}
