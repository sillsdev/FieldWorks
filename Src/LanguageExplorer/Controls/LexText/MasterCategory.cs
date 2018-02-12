// Copyright (c) 2005-2018 SIL International
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
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Controls.LexText
{
	internal class MasterCategory
	{
		private string m_id;
		private string m_abbrev;
		private string m_abbrevWs;
		private string m_term;
		private string m_termWs;
		private string m_def;
		private string m_defWs;
		private List<MasterCategoryCitation> m_citations;
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

			var mc = new MasterCategory
			{
				IsGroup = node.SelectNodes("item") != null,
				m_id = XmlUtils.GetMandatoryAttributeValue(node, "id")
			};

			foreach (var pos in posSet)
			{
				if (pos.CatalogSourceId == mc.m_id)
				{
					mc.POS = pos;
					break;
				}
			}

			mc.m_node = node; // remember node, too, so can put info for all WSes in database

			var sDefaultWS = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Id;
			string sContent;
			mc.m_abbrevWs = GetBestWritingSystemForNamedNode(node, "abbrev", sDefaultWS, cache, out sContent);
			mc.m_abbrev = sContent;

			mc.m_termWs = GetBestWritingSystemForNamedNode(node, "term", sDefaultWS, cache, out sContent);
			mc.m_term = NameFixer(sContent);

			mc.m_defWs = GetBestWritingSystemForNamedNode(node, "def", sDefaultWS, cache, out sContent);
			mc.m_def = sContent;

			foreach (XmlNode citNode in node.SelectNodes("citation"))
			{
				mc.m_citations.Add(new MasterCategoryCitation(XmlUtils.GetMandatoryAttributeValue(citNode, "ws"), citNode.InnerText));
			}
			return mc;
		}
		private static string GetBestWritingSystemForNamedNode(XmlNode node, string sNodeName, string sDefaultWS, LcmCache cache, out string sNodeContent)
		{
			string sWS;
			var nd = node.SelectSingleNode(sNodeName + "[@ws='" + sDefaultWS + "']");
			if (nd == null || nd.InnerText.Length == 0)
			{
				foreach (var ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
				{
					sWS = ws.Id;
					if (sWS == sDefaultWS)
					{
						continue;
					}
					nd = node.SelectSingleNode(sNodeName + "[@ws='" + sWS + "']");
					if (nd != null && nd.InnerText.Length > 0)
					{
						break;
					}
				}
			}
			if (nd == null)
			{
				sNodeContent = string.Empty;
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
			if (POS != null)
			{
				return; // It's already in the database, so nothing more can be done.
			}

			UndoableUnitOfWorkHelper.Do(LexTextControls.ksUndoCreateCategory, LexTextControls.ksRedoCreateCategory,
				cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					int newOwningFlid;
					int insertLocation;
					DeterminePOSLocationInfo(cache, subItemOwner, parent, posList, out newOwningFlid, out insertLocation);
					var wsf = cache.WritingSystemFactory;
					Debug.Assert(POS != null);

					var termWs = wsf.GetWsFromStr(m_termWs);
					var abbrevWs = wsf.GetWsFromStr(m_abbrevWs);
					var defWs = wsf.GetWsFromStr(m_defWs);
					if (m_node == null)
					{
						// should not happen, but just in case... we still get something useful
						POS.Name.set_String(termWs, TsStringUtils.MakeString(m_term, termWs));
						POS.Abbreviation.set_String(abbrevWs, TsStringUtils.MakeString(m_abbrev, abbrevWs));
						POS.Description.set_String(defWs, TsStringUtils.MakeString(m_def, defWs));
					}
					else
					{
						SetContentFromNode(cache, "abbrev", false, POS.Abbreviation);
						SetContentFromNode(cache, "term", true, POS.Name);
						SetContentFromNode(cache, "def", false, POS.Description);
					}

					POS.CatalogSourceId = m_id;
				});
		}

		private void SetContentFromNode(LcmCache cache, string sNodeName, bool fFixName, ITsMultiString item)
		{
			var wsf = cache.WritingSystemFactory;
			int iWS;
			var fContentFound = false; // be pessimistic
			foreach (var ws in cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems)
			{
				var sWS = ws.Id;
				var nd = m_node.SelectSingleNode(sNodeName + "[@ws='" + sWS + "']");
				if (nd == null || nd.InnerText.Length == 0)
				{
					continue;
				}
				fContentFound = true;
				var sNodeContent = fFixName ? NameFixer(nd.InnerText) : nd.InnerText;
				iWS = wsf.GetWsFromStr(sWS);
				item.set_String(iWS, TsStringUtils.MakeString(sNodeContent, iWS));
			}
			if (!fContentFound)
			{
				iWS = cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
				item.set_String(iWS, TsStringUtils.EmptyString(iWS));
			}
		}

		private int DeterminePOSLocationInfo(LcmCache cache, IPartOfSpeech subItemOwner, MasterCategory parent, ICmPossibilityList posList, out int newOwningFlid, out int insertLocation)
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
			var guid = posList.Owner is IReversalIndex ? Guid.NewGuid() : new Guid(XmlUtils.GetOptionalAttributeValue(m_node, "guid"));
			var posFactory = cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
			if (subItemOwner != null)
			{
				newOwner = subItemOwner.Hvo;
				newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
				insertLocation = subItemOwner.SubPossibilitiesOS.Count;
				POS = posFactory.Create(guid, subItemOwner);
			}
			else if (parent?.POS != null)
			{
				newOwner = parent.POS.Hvo;
				newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
				insertLocation = parent.POS.SubPossibilitiesOS.Count;
				POS = posFactory.Create(guid, parent.POS);
			}
			else
			{
				newOwner = posList.Hvo;
				newOwningFlid = CmPossibilityListTags.kflidPossibilities;
				insertLocation = posList.PossibilitiesOS.Count;
				POS = posFactory.Create(guid, posList); // automatically adds to parent.
			}
			return newOwner;
		}

		/// <summary>
		/// Ensures the first letter is uppercase, and that uppercase letters after the first letter, start a new word.
		/// </summary>
		private static string NameFixer(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return name;
			}

			var c = name[0];
			if (char.IsLetter(c) && char.IsLower(c))
			{
				name = char.ToUpper(c) + name.Substring(1, name.Length - 1);
			}

			// Add space before each upper case letter, after the first one.
			for (var i = name.Length - 1; i > 0; --i)
			{
				c = name[i];
				if (char.IsLetter(c) && char.IsUpper(c))
				{
					name = name.Insert(i, " ");
				}
			}

			return name;
		}

		public MasterCategory()
		{
			m_citations = new List<MasterCategoryCitation>();
		}

		public bool IsGroup { get; private set; }

		public IPartOfSpeech POS { get; private set; }

		public bool InDatabase => POS != null;

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

			rtbDescription.SelectionFont = string.IsNullOrEmpty(m_def) ? fntItalic : original;
			rtbDescription.AppendText(string.IsNullOrEmpty(m_def) ? LexTextControls.ksUndefinedItem : m_def);
			rtbDescription.AppendText(doubleNewLine);

			if (m_citations.Count > 0)
			{
				rtbDescription.SelectionFont = fntItalic;
				rtbDescription.AppendText(LexTextControls.ksReferences);
				rtbDescription.AppendText(doubleNewLine);

				rtbDescription.SelectionFont = original;
				foreach (var mcc in m_citations)
				{
					mcc.ResetDescription(rtbDescription);
				}
			}
#if __MonoCS__
			// Ensure that the top of the description is showing (FWNX-521).
			rtbDescription.Select(0,0);
			rtbDescription.ScrollToCaret();
#endif
		}

		public override string ToString()
		{
			return InDatabase ? string.Format(LexTextControls.ksXInFwProject, m_term) : m_term;
		}
	}
}