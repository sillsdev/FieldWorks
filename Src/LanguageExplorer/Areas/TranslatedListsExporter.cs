// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;
using SIL.Xml;

namespace LanguageExplorer.Areas
{
	/// <summary>
	/// This encapsulates exporting one or more CmPossibilityLists in one or more writing
	/// systems.
	/// </summary>
	internal class TranslatedListsExporter
	{
		private LcmCache m_cache;
		private List<ICmPossibilityList> m_lists;
		private Dictionary<int, string> m_mapWsCode = new Dictionary<int, string>();
		private IProgress m_progress;
		private int m_wsEn;
		private Dictionary<int, bool> m_flidsForGuids = new Dictionary<int, bool>();

		/// <summary />
		public TranslatedListsExporter(List<ICmPossibilityList> lists, List<int> writingSystems, IProgress progress)
		{
			m_lists = lists;
			if (m_lists.Count > 0)
			{
				m_cache = m_lists[0].Cache;
			}
			if (m_cache != null)
			{
				foreach (var ws in writingSystems)
				{
					m_mapWsCode.Add(ws, m_cache.WritingSystemFactory.GetStrFromWs(ws));
				}
				m_wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
				Debug.Assert(m_wsEn != 0);
			}
			m_progress = progress;
			// These flids for List fields indicate lists that use fixed guids for their (predefined) items.
			m_flidsForGuids.Add(LangProjectTags.kflidTranslationTags, true);
			m_flidsForGuids.Add(LangProjectTags.kflidAnthroList, true);
			m_flidsForGuids.Add(LangProjectTags.kflidSemanticDomainList, true);
			m_flidsForGuids.Add(LexDbTags.kflidComplexEntryTypes, true);
			m_flidsForGuids.Add(LexDbTags.kflidVariantEntryTypes, true);
			m_flidsForGuids.Add(LexDbTags.kflidMorphTypes, true);
			m_flidsForGuids.Add(RnResearchNbkTags.kflidRecTypes, true);
		}

		/// <summary>
		/// Export the list(s) to the given file.
		/// </summary>
		public void ExportLists(string outputFile)
		{
			if (m_wsEn == 0)
			{
				return;
			}
			using (TextWriter w = new StreamWriter(outputFile))
			{
				ExportTranslatedLists(w);
			}
		}

		/// <summary>
		/// Export the list(s) to the given TextWriter.
		/// </summary>
		public void ExportTranslatedLists(TextWriter w)
		{
			w.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			w.WriteLine("<Lists date=\"{0}\">", DateTime.Now);
			foreach (var list in m_lists)
			{
				ExportTranslatedList(w, list);
			}
			w.WriteLine("</Lists>");
		}

		private void ExportTranslatedList(TextWriter w, ICmPossibilityList list)
		{
			var owner = list.Owner.ClassName;
			var field = m_cache.MetaDataCacheAccessor.GetFieldName(list.OwningFlid);
			var itemClass = m_cache.MetaDataCacheAccessor.GetClassName(list.ItemClsid);
			w.WriteLine("<List owner=\"{0}\" field=\"{1}\" itemClass=\"{2}\">", owner, field, itemClass);
			ExportMultiUnicode(w, list.Name);
			ExportMultiUnicode(w, list.Abbreviation);
			ExportMultiString(w, list.Description);
			w.WriteLine("<Possibilities>");
			foreach (var item in list.PossibilitiesOS)
			{
				ExportTranslatedItem(w, item, list.OwningFlid);
			}
			w.WriteLine("</Possibilities>");
			w.WriteLine("</List>");
		}

		private void ExportMultiUnicode(TextWriter w, IMultiUnicode mu)
		{
			var sEnglish = mu.get_String(m_wsEn).Text;
			if (string.IsNullOrEmpty(sEnglish))
			{
				return;
			}
			var sField = m_cache.MetaDataCacheAccessor.GetFieldName(mu.Flid);
			w.WriteLine("<{0}>", sField);
			w.WriteLine("<AUni ws=\"en\">{0}</AUni>", XmlUtils.MakeSafeXml(sEnglish));
			foreach (var ws in m_mapWsCode.Keys)
			{
				var sValue = mu.get_String(ws).Text;
				sValue = sValue == null ? string.Empty : CustomIcu.GetIcuNormalizer(FwNormalizationMode.knmNFC).Normalize(sValue);
				w.WriteLine("<AUni ws=\"{0}\">{1}</AUni>", m_mapWsCode[ws], XmlUtils.MakeSafeXml(sValue));
			}
			w.WriteLine("</{0}>", sField);
		}

		private void ExportMultiString(TextWriter w, IMultiString ms)
		{
			var tssEnglish = ms.get_String(m_wsEn);
			if (tssEnglish.Length == 0)
			{
				return;
			}
			var sField = m_cache.MetaDataCacheAccessor.GetFieldName(ms.Flid);
			w.WriteLine("<{0}>", sField);
			w.WriteLine(TsStringSerializer.SerializeTsStringToXml(tssEnglish, m_cache.WritingSystemFactory, m_wsEn, false));
			foreach (var ws in m_mapWsCode.Keys)
			{
				var tssValue = ms.get_String(ws);
				w.WriteLine(TsStringSerializer.SerializeTsStringToXml(tssValue, m_cache.WritingSystemFactory, ws, false));
			}
			w.WriteLine("</{0}>", sField);
		}

		private void ExportTranslatedItem(TextWriter w, ICmPossibility item, int listFlid)
		{
			if (m_flidsForGuids.ContainsKey(listFlid))
			{
				w.WriteLine("<{0} guid=\"{1}\">", item.ClassName, item.Guid);
			}
			else
			{
				w.WriteLine("<{0}>", item.ClassName);
			}
			ExportMultiUnicode(w, item.Name);
			ExportMultiUnicode(w, item.Abbreviation);
			ExportMultiString(w, item.Description);
			switch (item.ClassID)
			{
				case CmLocationTags.kClassId:
					ExportLocationFields(w, item as ICmLocation);
					break;
				case CmPersonTags.kClassId:
					ExportPersonFields(w, item as ICmPerson);
					break;
				case CmSemanticDomainTags.kClassId:
					ExportSemanticDomainFields(w, item as ICmSemanticDomain);
					break;
				case LexEntryTypeTags.kClassId:
					ExportLexEntryTypeFields(w, item as ILexEntryType);
					break;
				case LexEntryInflTypeTags.kClassId:
					ExportLexEntryInflTypeFields(w, item as ILexEntryInflType);
					break;
				case LexRefTypeTags.kClassId:
					ExportLexRefTypeFields(w, item as ILexRefType);
					break;
				case PartOfSpeechTags.kClassId:
					ExportPartOfSpeechFields(w, item as IPartOfSpeech);
					break;
			}
			if (item.SubPossibilitiesOS.Count > 0)
			{
				w.WriteLine("<SubPossibilities>");
				foreach (var subItem in item.SubPossibilitiesOS)
				{
					ExportTranslatedItem(w, subItem, listFlid);
				}
				w.WriteLine("</SubPossibilities>");
			}
			w.WriteLine("</{0}>", item.ClassName);
		}

		private void ExportLocationFields(TextWriter w, ICmLocation item)
		{
			if (item != null)
			{
				ExportMultiUnicode(w, item.Alias);
			}
		}

		private void ExportPersonFields(TextWriter w, ICmPerson item)
		{
			if (item != null)
			{
				ExportMultiUnicode(w, item.Alias);
			}
		}

		private void ExportSemanticDomainFields(TextWriter w, ICmSemanticDomain item)
		{
			if (item == null || item.QuestionsOS.Count <= 0)
			{
				return;
			}
			w.WriteLine("<Questions>");
			foreach (var domainQ in item.QuestionsOS)
			{
				w.WriteLine("<CmDomainQ>");
				ExportMultiUnicode(w, domainQ.Question);
				ExportMultiUnicode(w, domainQ.ExampleWords);
				ExportMultiString(w, domainQ.ExampleSentences);
				w.WriteLine("</CmDomainQ>");
			}
			w.WriteLine("</Questions>");
		}

		private void ExportLexEntryTypeFields(TextWriter w, ILexEntryType item)
		{
			if (item == null)
			{
				return;
			}
			ExportMultiUnicode(w, item.ReverseName);
			ExportMultiUnicode(w, item.ReverseAbbr);
		}

		private void ExportLexEntryInflTypeFields(TextWriter w, ILexEntryInflType item)
		{
			if (item == null)
			{
				return;
			}
			ExportLexEntryTypeFields(w, item);
			ExportMultiUnicode(w, item.GlossAppend);
		}

		private void ExportLexRefTypeFields(TextWriter w, ILexRefType item)
		{
			if (item == null)
			{
				return;
			}
			ExportMultiUnicode(w, item.ReverseName);
			ExportMultiUnicode(w, item.ReverseAbbreviation);
		}

		private void ExportPartOfSpeechFields(TextWriter w, IPartOfSpeech item)
		{
			// TODO: handle any other part of speech fields that we decide need to be localizable.
		}
	}
}