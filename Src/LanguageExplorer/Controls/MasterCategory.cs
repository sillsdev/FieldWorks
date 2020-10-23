// Copyright (c) 2015-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.PlatformUtilities;
using SIL.Xml;

namespace LanguageExplorer.Controls
{
	public class MasterCategory
	{
		private string m_id;
		private string m_abbrev;
		private string m_abbrevWs;
		private string m_term;
		private string m_termWs;
		private string m_def;
		private string m_defWs;
		private readonly List<MasterCategoryCitation> m_citations;
		private XmlNode m_node; // need to remember the node so can put info for *all* writing systems into database

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
			mc.m_abbrevWs = GetBestWritingSystemForNamedNode(node, "abbrev", sDefaultWS, cache, out var sContent);
			mc.m_abbrev = sContent;

			mc.m_termWs = GetBestWritingSystemForNamedNode(node, "term", sDefaultWS, cache, out sContent);
			mc.m_term = NameFixer(sContent);

			mc.m_defWs = GetBestWritingSystemForNamedNode(node, "def", sDefaultWS, cache, out sContent);
			mc.m_def = sContent;

			// ReSharper disable once PossibleNullReferenceException
			foreach (XmlNode citNode in node.SelectNodes("citation"))
				mc.m_citations.Add(new MasterCategoryCitation(XmlUtils.GetMandatoryAttributeValue(citNode, "ws"), citNode.InnerText));
			return mc;
		}

		internal static string GetBestWritingSystemForNamedNode(XmlNode node, string sNodeName, string sDefaultWS, LcmCache cache, out string sNodeContent)
		{
			string sWS;
			var nd = node.SelectSingleNode(sNodeName + "[@ws='" + sDefaultWS + "']");
			if (nd == null || nd.InnerText.Length == 0)
			{
				foreach (var ws in cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
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
			// It's already in the database, so nothing more can be done.
			if (InDatabase)
			{
				return;
			}

			UndoableUnitOfWorkHelper.Do(LanguageExplorerControls.ksUndoCreateCategory, LanguageExplorerControls.ksRedoCreateCategory,
				cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					DeterminePOSLocationInfo(cache, subItemOwner, parent, posList, out _, out _);
					Debug.Assert(POS != null);
					if (m_node == null)
					{
						// should not happen, but just in case... we still get something useful
						var wsf = cache.WritingSystemFactory;
						var termWs = wsf.GetWsFromStr(m_termWs);
						var abbrevWs = wsf.GetWsFromStr(m_abbrevWs);
						var defWs = wsf.GetWsFromStr(m_defWs);
						POS.Name.set_String(termWs, TsStringUtils.MakeString(m_term, termWs));
						POS.Abbreviation.set_String(abbrevWs, TsStringUtils.MakeString(m_abbrev, abbrevWs));
						POS.Description.set_String(defWs, TsStringUtils.MakeString(m_def, defWs));
					}
					else
					{
						UpdatePOSStrings(cache, m_node, POS);
					}

					POS.CatalogSourceId = m_id;
				});
		}

		/// <summary>
		/// Updates the text of the projects' grammatical categories with text from provided goldEticDoc
		/// </summary>
		public static void UpdatePOSStrings(LcmCache cache, XmlDocument goldEticDoc)
		{
			var posDict = cache.LangProject.PartsOfSpeechOA.ReallyReallyAllPossibilities.Cast<IPartOfSpeech>()
				.Where(pos => !string.IsNullOrEmpty(pos.CatalogSourceId)).ToDictionary(pos => pos.CatalogSourceId);
			var posNodes = goldEticDoc.DocumentElement?.SelectNodes("/eticPOSList/item");
			Debug.Assert(posNodes != null);

			UndoableUnitOfWorkHelper.Do(LanguageExplorerControls.ksUndoImportTranslateGrammaticalCategories,
				LanguageExplorerControls.ksRedoImportTranslateGrammaticalCategories,
				cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					UpdatePOSStrings(cache, goldEticDoc.DocumentElement, posDict);
				});
		}

		private static void UpdatePOSStrings(LcmCache cache, XmlElement goldEticElt, Dictionary<string, IPartOfSpeech> posDict)
		{
			foreach (XmlElement posElt in goldEticElt.GetElementsByTagName("item"))
			{
				if (posDict.TryGetValue(XmlUtils.GetMandatoryAttributeValue(posElt, "id"), out var pos))
				{
					UpdatePOSStrings(cache, posElt, pos);
					UpdatePOSStrings(cache, posElt, posDict);
				}
			}
		}

		/// <summary>
		/// Updates the text of the POS from the XmlNode. Must be called inside a UOW.
		/// </summary>
		internal static void UpdatePOSStrings(LcmCache cache, XmlNode posNode, IPartOfSpeech pos)
		{
			SetContentFromNode(cache, posNode, "abbrev", false, pos.Abbreviation);
			SetContentFromNode(cache, posNode, "term", true, pos.Name);
			SetContentFromNode(cache, posNode, "def", false, pos.Description);
		}

		private static void SetContentFromNode(LcmCache cache, XmlNode posNode, string sNodeName, bool fFixName, ITsMultiString item)
		{
			var wsf = cache.WritingSystemFactory;
			int iWS;
			var fContentFound = false; // be pessimistic
			foreach (var ws in cache.ServiceLocator.WritingSystems.AnalysisWritingSystems)
			{
				var sWS = ws.Id;
				var nd = posNode.SelectSingleNode(sNodeName + "[@ws='" + sWS + "']");
				if (nd == null || nd.InnerText.Length == 0)
					continue;
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

		private void DeterminePOSLocationInfo(LcmCache cache, IPartOfSpeech subItemOwner,
			MasterCategory parent, ICmPossibilityList posList,
			out int newOwningFlid, out int insertLocation)
		{
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
				newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
				insertLocation = subItemOwner.SubPossibilitiesOS.Count;
				POS = posFactory.Create(guid, subItemOwner);
			}
			else if (parent?.POS != null)
			{
				newOwningFlid = CmPossibilityTags.kflidSubPossibilities;
				insertLocation = parent.POS.SubPossibilitiesOS.Count;
				POS = posFactory.Create(guid, parent.POS);
			}
			else
			{
				newOwningFlid = CmPossibilityListTags.kflidPossibilities;
				insertLocation = posList.PossibilitiesOS.Count;
				POS = posFactory.Create(guid,posList); // automatically adds to parent.
			}
		}

		/// <summary>
		/// Ensures the first letter is uppercase, and that uppercase letters after the first letter, start a new word.
		/// </summary>
		private static string NameFixer(string name)
		{
			if (string.IsNullOrEmpty(name))
				return name;

			var c = name[0];
			if (char.IsLetter(c) && char.IsLower(c))
				name = char.ToUpper(c) + name.Substring(1, name.Length - 1);

			// Add space before each upper case letter, after the first one.
			for (var i = name.Length - 1; i > 0; --i)
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
			rtbDescription.AppendText(string.IsNullOrEmpty(m_def) ? LanguageExplorerControls.ksUndefinedItem : m_def);
			rtbDescription.AppendText(doubleNewLine);

			if (m_citations.Count > 0)
			{
				rtbDescription.SelectionFont = fntItalic;
				rtbDescription.AppendText(LanguageExplorerControls.ksReferences);
				rtbDescription.AppendText(doubleNewLine);

				rtbDescription.SelectionFont = original;
				foreach (var mcc in m_citations)
					mcc.ResetDescription(rtbDescription);
			}

			if (Platform.IsMono)
			{
				// Ensure that the top of the description is showing (FWNX-521).
				rtbDescription.Select(0,0);
				rtbDescription.ScrollToCaret();
			}
		}

		public override string ToString()
		{
			return InDatabase ? string.Format(LanguageExplorerControls.ksXInFwProject, m_term) : m_term;
		}

		internal class MasterCategoryCitation
		{
			public string WS { get; }

			public string Citation { get; }

			public MasterCategoryCitation(string ws, string citation)
			{
				WS = ws;
				Citation = citation;
			}

			public void ResetDescription(RichTextBox rtbDescription)
			{
				rtbDescription.AppendText(string.Format(LanguageExplorerControls.ksBullettedItem, Citation, Environment.NewLine));
			}
		}
	}
}