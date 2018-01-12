// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Controls.LexText
{
	internal class MasterCategory
	{
		private bool m_isGroup = false;
		private string m_id;
		private string m_abbrev;
		private string m_abbrevWs;
		private string m_term;
		private string m_termWs;
		private string m_def;
		private string m_defWs;
		private List<MasterCategoryCitation> m_citations;
		private IPartOfSpeech m_pos;
		private XmlNode m_node; // need to remember the node so can put info for *all* writing systems into databas

		public static MasterCategory Create(ISet<IPartOfSpeech> posSet, XmlNode node, LcmCache cache)
		{
			/*
			<item type="category" id="Adjective" guid="30d07580-5052-4d91-bc24-469b8b2d7df9">
				<abbrev ws="en">adj</abbrev>
				<term ws="en">adjective</term>
				<def ws="en">An adjective is a part of speech whose members modify nouns. An adjective specifies the attributes of a noun referent. Note: this is one case among many. Adjectives are a class of modifiers.</def>
				<citation ws="en">Crystal 1997:8</citation>
				<citation ws="en">Mish et al. 1990:56</citation>
				<citation ws="en">Payne 1997:63</citation>
			</item>
			*/

			MasterCategory mc = new MasterCategory();
			mc.m_isGroup = node.SelectNodes("item") != null;
			mc.m_id = XmlUtils.GetMandatoryAttributeValue(node, "id");

			foreach (var pos in posSet)
			{
				if (pos.CatalogSourceId == mc.m_id)
				{
					mc.m_pos = pos;
					break;
				}
			}

			mc.m_node = node; // remember node, too, so can put info for all WSes in database

			string sDefaultWS = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			string sContent;
			mc.m_abbrevWs = GetBestWritingSystemForNamedNode(node, "abbrev", sDefaultWS, cache, out sContent);
			mc.m_abbrev = sContent;

			mc.m_termWs = GetBestWritingSystemForNamedNode(node, "term", sDefaultWS, cache, out sContent);
			mc.m_term = NameFixer(sContent);

			mc.m_defWs = GetBestWritingSystemForNamedNode(node, "def", sDefaultWS, cache, out sContent);
			mc.m_def = sContent;

			foreach (XmlNode citNode in node.SelectNodes("citation"))
				mc.m_citations.Add(new MasterCategoryCitation(XmlUtils.GetMandatoryAttributeValue(citNode, "ws"), citNode.InnerText));
			return mc;
		}
		private static string GetBestWritingSystemForNamedNode(XmlNode node, string sNodeName, string sDefaultWS, LcmCache cache, out string sNodeContent)
		{
			string sWS;
			XmlNode nd = node.SelectSingleNode(sNodeName + "[@ws='" + sDefaultWS + "']");
			if (nd == null || nd.InnerText.Length == 0)
			{
				foreach (CoreWritingSystemDefinition ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
				{
					sWS = ws.Id;
					if (sWS == sDefaultWS)
						continue;
					nd = node.SelectSingleNode(sNodeName + "[@ws='" + sWS + "']");
					if (nd != null && nd.InnerText.Length > 0)
						break;
				}
			}
			if (nd == null)
			{
				sNodeContent = "";
				sWS = sDefaultWS;
			}
			else
			{
				sNodeContent = nd.InnerText;
				sWS = XmlUtils.GetMandatoryAttributeValue(nd, "ws");
			}
			return sWS;
		}

		public void AddToDatabase(LcmCache cache, ICmPossibilityList posList, MasterCategory parent, IPartOfSpeech subItemOwner)
		{
			if (m_pos != null)
				return; // It's already in the database, so nothing more can be done.

			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateCategory, LexTextControls.ksRedoCreateCategory,
				cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					int newOwningFlid;
					int insertLocation;
					int newOwner =
						DeterminePOSLocationInfo(cache, subItemOwner, parent, posList, out newOwningFlid, out insertLocation);
					ILgWritingSystemFactory wsf = cache.WritingSystemFactory;
					Debug.Assert(m_pos != null);

					int termWs = wsf.GetWsFromStr(m_termWs);
					int abbrevWs = wsf.GetWsFromStr(m_abbrevWs);
					int defWs = wsf.GetWsFromStr(m_defWs);
					if (m_node == null)
					{ // should not happen, but just in case... we still get something useful
						m_pos.Name.set_String(termWs, TsStringUtils.MakeString(m_term, termWs));
						m_pos.Abbreviation.set_String(abbrevWs, TsStringUtils.MakeString(m_abbrev, abbrevWs));
						m_pos.Description.set_String(defWs, TsStringUtils.MakeString(m_def, defWs));
					}
					else
					{
						SetContentFromNode(cache, "abbrev", false, m_pos.Abbreviation);
						SetContentFromNode(cache, "term", true, m_pos.Name);
						SetContentFromNode(cache, "def", false, m_pos.Description);
					}

					m_pos.CatalogSourceId = m_id;
				});
		}

		private void SetContentFromNode(LcmCache cache, string sNodeName, bool fFixName, ITsMultiString item)
		{
			ILgWritingSystemFactory wsf = cache.WritingSystemFactory;
			int iWS;
			XmlNode nd;
			bool fContentFound = false; // be pessimistic
			foreach (CoreWritingSystemDefinition ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
			{
				string sWS = ws.Id;
				nd = m_node.SelectSingleNode(sNodeName + "[@ws='" + sWS + "']");
				if (nd == null || nd.InnerText.Length == 0)
					continue;
				fContentFound = true;
				string sNodeContent;
				if (fFixName)
					sNodeContent = NameFixer(nd.InnerText);
				else
					sNodeContent = nd.InnerText;
				iWS = wsf.GetWsFromStr(sWS);
				item.set_String(iWS, TsStringUtils.MakeString(sNodeContent, iWS));
			}
			if (!fContentFound)
			{
				iWS = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				item.set_String(iWS, TsStringUtils.EmptyString(iWS));
			}
		}

		private int DeterminePOSLocationInfo(LcmCache cache, IPartOfSpeech subItemOwner, MasterCategory parent, ICmPossibilityList posList,
			out int newOwningFlid, out int insertLocation)
		{
			int newOwner;
			// The XML node is from a file shipped with FieldWorks. It is quite likely multiple users
			// of a project could independently add the same items, so we create them with fixed guids
			// so merge will recognize them as the same objects.
			//// LT-14511 However, if the partOfSpeech is being added to a reversal index, a different guid needs to be used
			//// than the ones in the file shipped with FieldWorks. In this case if two users add the same POS to the
			//// reversal index at the same time and then do a Send/Receive operation, then a merge conflict report
			//// will probably be created for this. This scenario is not likely to occur very often at all so having
			//// a conflict report created for when this happens is something we can live with.
			Guid guid;
			if (posList.Owner is IReversalIndex)
				guid = Guid.NewGuid();
			else
				guid = new Guid(XmlUtils.GetOptionalAttributeValue(m_node, "guid"));
			var posFactory = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			if (subItemOwner != null)
			{
				newOwner = subItemOwner.Hvo;
				newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
				insertLocation = subItemOwner.SubPossibilitiesOS.Count;
				m_pos = posFactory.Create(guid, subItemOwner);
			}
			else if (parent != null && parent.m_pos != null)
			{
				newOwner = parent.m_pos.Hvo;
				newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
				insertLocation = parent.m_pos.SubPossibilitiesOS.Count;
				m_pos = posFactory.Create(guid, parent.m_pos);
			}
			else
			{
				newOwner = posList.Hvo;
				newOwningFlid = CmPossibilityListTags.kflidPossibilities;
				insertLocation = posList.PossibilitiesOS.Count;
				m_pos = posFactory.Create(guid, posList); // automatically adds to parent.
			}
			return newOwner;
		}

		/// <summary>
		/// Ensures the first letter is uppercase, and that uppercase letters after the first letter, start a new word.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		private static string NameFixer(string name)
		{
			if (name == null || name.Length == 0)
				return name;

			char c = name[0];
			if (char.IsLetter(c) && char.IsLower(c))
				name = char.ToUpper(c).ToString() + name.Substring(1, name.Length - 1);

			// Add space before each upper case letter, after the first one.
			for (int i = name.Length - 1; i > 0; --i)
			{
				c = name[i];
				if (char.IsLetter(c) && char.IsUpper(c))
					name = name.Insert(i, " ");
			}

			return name;
		}

		public MasterCategory()
		{
			m_citations = new List<MasterCategoryCitation>();
		}

		public bool IsGroup
		{
			get
			{
				return m_isGroup;
			}
		}

		public IPartOfSpeech POS
		{
			get
			{
				return m_pos;
			}
		}

		public bool InDatabase
		{
			get
			{
				return m_pos != null;
			}
		}

		public void ResetDescription(RichTextBox rtbDescription)
		{

			rtbDescription.Clear();

			var doubleNewLine = Environment.NewLine + Environment.NewLine;
			Font original = rtbDescription.SelectionFont;
			Font fntBold = new Font(original.FontFamily, original.Size, FontStyle.Bold);
			Font fntItalic = new Font(original.FontFamily, original.Size, FontStyle.Italic);
			rtbDescription.SelectionFont = fntBold;
			rtbDescription.AppendText(m_term);
			rtbDescription.AppendText(doubleNewLine);

			rtbDescription.SelectionFont = (m_def == null || m_def == String.Empty) ? fntItalic : original;
			rtbDescription.AppendText((m_def == null || m_def == String.Empty) ? LexTextControls.ksUndefinedItem : m_def);
			rtbDescription.AppendText(doubleNewLine);

			if (m_citations.Count > 0)
			{
				rtbDescription.SelectionFont = fntItalic;
				rtbDescription.AppendText(LexTextControls.ksReferences);
				rtbDescription.AppendText(doubleNewLine);

				rtbDescription.SelectionFont = original;
				foreach (MasterCategoryCitation mcc in m_citations)
					mcc.ResetDescription(rtbDescription);
			}
#if __MonoCS__
// Ensure that the top of the description is showing (FWNX-521).
				rtbDescription.Select(0,0);
				rtbDescription.ScrollToCaret();
#endif
		}

		public override string ToString()
		{
			if (InDatabase)
				return String.Format(LexTextControls.ksXInFwProject, m_term);
			else
				return m_term;
		}
	}
}