// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlTranslatedLists.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using System.Globalization;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Application.ApplicationServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class provides the ability to import translations for multi-lingual string fields
	/// in one or more lists.  See FWR-1739 for motivation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlTranslatedLists
	{
		IProgress m_progress;
		FdoCache m_cache;
		IFwMetaDataCacheManaged m_mdc;
		ILgWritingSystemFactory m_wsf;
		int[] m_rgItemClasses;
		int m_wsEn;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import a file containing translations for one or more lists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportTranslatedLists(string filename, FdoCache cache, IProgress progress)
		{
#if DEBUG
			DateTime dtBegin = DateTime.Now;
#endif
			try
			{
				using (var inputStream = FileUtils.OpenStreamForRead(filename))
				using (var zipReader = new ZipInputStream(inputStream))
				{
					var entry = zipReader.GetNextEntry(); // advances it to where we can read the one zipped file.

					using (var reader = new StreamReader(zipReader, Encoding.UTF8))
						ImportTranslatedLists(reader, cache, progress);
				}
			}
			catch (Exception e)
			{
				MessageBoxUtils.Show(e.Message);
			}
#if DEBUG
			DateTime dtEnd = DateTime.Now;
			TimeSpan span = new TimeSpan(dtEnd.Ticks - dtBegin.Ticks);
			Debug.WriteLine(String.Format("Elapsed time for loading translated list(s) from {0} = {1}",
				filename, span.ToString()));
#endif
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import a file (encapsulated by a TextReader) containing translations for one or more lists.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool ImportTranslatedLists(TextReader reader, FdoCache cache, IProgress progress)
		{
			m_cache = cache;
			m_mdc = cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			m_rgItemClasses = m_mdc.GetAllSubclasses(CmPossibilityTags.kClassId);
			m_wsf = cache.WritingSystemFactory;
			m_wsEn = GetWsFromStr("en");
			Debug.Assert(m_wsEn != 0);
			m_progress = progress;

			try
			{
				using (var xreader = XmlReader.Create(reader))
				Import(xreader);
			}
			catch (Exception e)
			{
				MessageBoxUtils.Show(e.Message, "Import Error");
				return false;
			}
			return true;
		}

		private int GetWsFromStr(string sWs)
		{
			int ws = m_wsf.GetWsFromStr(sWs);
			if (ws != 0)
				return ws;
			// Add this writing system.
			ILgWritingSystem lgws = m_wsf.get_Engine(sWs);
			return lgws.Handle;
		}

		private void Import(XmlReader xrdr)
		{
			xrdr.MoveToContent();
			if (xrdr.Name != "Lists")
				throw new Exception(String.Format("Unexpected outer element (expected <Lists>): {0}", xrdr.Name));
			DateTime dateOfTranslation = ReadDateAttribute(xrdr);
			if (!xrdr.ReadToDescendant("List"))
				throw new Exception(String.Format("Unexpected second element (expected <List>): {0}", xrdr.Name));

			while (xrdr.Name == "List")
			{
				ICmPossibilityList list = GetListFromAttributes(xrdr);
				xrdr.MoveToElement();
				string sItemClass;
				int clid;
				GetItemClassFromAttributes(xrdr, out sItemClass, out clid);
				xrdr.MoveToElement();
				xrdr.Read();
				while (xrdr.Depth == 2)
				{
					xrdr.MoveToContent();
					if (xrdr.Depth < 2)
						break;
					switch (xrdr.Name)
					{
						case "Description":
							SetMultiStringFromXml(xrdr, list.Description);
							break;
						case "Name":
							SetMultiUnicodeFromXml(xrdr, list.Name);
							break;
						case "Abbreviation":
							SetMultiUnicodeFromXml(xrdr, list.Abbreviation);
							break;
						case "Possibilities":
							ReadPossibilitiesFromXml(xrdr, list, sItemClass);
							break;
						default:
							throw new Exception(String.Format("Unknown field element in <List>: {0}", xrdr.Name));
					}
				}
				xrdr.ReadEndElement();
				xrdr.MoveToContent();	// skip any trailing whitespace
			}
		}

		Dictionary<Guid, Dictionary<string, ICmPossibility>> m_mapListToMap = new Dictionary<Guid, Dictionary<string, ICmPossibility>>();

		ICmPossibilityRepository m_repoPoss;

		private ICmPossibilityRepository PossibilityRepository
		{
			get
			{
				if (m_repoPoss == null)
					m_repoPoss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
				return m_repoPoss;
			}
		}

		private void ReadPossibilitiesFromXml(XmlReader xrdr, ICmPossibilityList list, string sItemClass)
		{
			if (xrdr.ReadToDescendant(sItemClass))
			{
				Dictionary<string, ICmPossibility> mapNameToPoss = GetMappingForList(list);
				do
				{
					string sGuid = xrdr.GetAttribute("guid");
					xrdr.MoveToElement();
					Guid guid = Guid.Empty;
					ICmPossibility poss = null;
					if (!String.IsNullOrEmpty(sGuid))
					{
						guid = new Guid(sGuid);
						PossibilityRepository.TryGetObject(guid, out poss);
					}
					ReadPossItem(xrdr.ReadSubtree(), poss, sItemClass, mapNameToPoss);
				} while (xrdr.ReadToNextSibling(sItemClass));
			}
			xrdr.Read();	// reads end element.
		}

		private Dictionary<string, ICmPossibility> GetMappingForList(ICmPossibilityList list)
		{
			Dictionary<string, ICmPossibility> mapNameToItem;
			if (!m_mapListToMap.TryGetValue(list.Guid, out mapNameToItem))
			{
				mapNameToItem = new Dictionary<string, ICmPossibility>();
				m_mapListToMap.Add(list.Guid, mapNameToItem);
				FillInMapForPossibilities(mapNameToItem, list.PossibilitiesOS);
			}
			return mapNameToItem;
		}

		private void FillInMapForPossibilities(Dictionary<string, ICmPossibility> mapNameToItem,
			IFdoOwningSequence<ICmPossibility> possibilities)
		{
			if (possibilities == null)
				return;
			foreach (var item in possibilities)
			{
				ITsString tss = item.Name.get_String(m_wsEn);
				if (tss == null)
					continue;
				string name = tss.Text;
				if (String.IsNullOrEmpty(name))
					continue;
				if (mapNameToItem.ContainsKey(name))
					continue;
				mapNameToItem.Add(name, item);
				FillInMapForPossibilities(mapNameToItem, item.SubPossibilitiesOS);
			}
		}

		private void ReadPossItem(XmlReader xrdrSub, ICmPossibility poss, string sItemClassName,
			Dictionary<string, ICmPossibility> mapNameToPoss)
		{
			xrdrSub.Read();
			Debug.Assert(xrdrSub.Name == sItemClassName);
			try
			{
				string abbrXml = null;
				string descXml = null;
				string aliasXml = null;
				string revabbrXml = null;
				string revabbrevXml = null;
				string revnameXml = null;
				string questionsXml = null;
				while (xrdrSub.Read())
				{
					if (xrdrSub.NodeType == XmlNodeType.Element)
					{
						switch (xrdrSub.Name)
						{
							case "Abbreviation":
								if (poss != null)
									SetMultiUnicodeFromXml(xrdrSub, poss.Abbreviation);
								else
									abbrXml = GetXmlOfElement(xrdrSub);
								break;
							case "Description":
								if (poss != null)
									SetMultiStringFromXml(xrdrSub, poss.Description);
								else
									descXml = GetXmlOfElement(xrdrSub);
								break;
							case "Name":
								if (poss != null)
								{
									SetMultiUnicodeFromXml(xrdrSub, poss.Name);
								}
								else
								{
									poss = FindPossibilityAndSetName(xrdrSub, mapNameToPoss);
									if (poss != null)
									{
										if (abbrXml != null)
										{
											using (var srdr = new StringReader(abbrXml))
											{
												using (var xrdrAbbr = XmlReader.Create(srdr))
												{
											SetMultiUnicodeFromXml(xrdrAbbr, poss.Abbreviation);
											xrdrAbbr.Close();
											abbrXml = null;
										}
											}
										}
										if (descXml != null)
										{
											using (var srdr = new StringReader(descXml))
											{
												using (var xrdrDesc = XmlReader.Create(srdr))
												{
											SetMultiStringFromXml(xrdrDesc, poss.Description);
											xrdrDesc.Close();
											descXml = null;
										}
											}
										}
										if (aliasXml != null)
										{
											if (poss is ICmPerson)
											{
												using (var srdr = new StringReader(aliasXml))
												{
													using (var xrdrT = XmlReader.Create(srdr))
													{
												SetMultiUnicodeFromXml(xrdrT, (poss as ICmPerson).Alias);
												xrdrT.Close();
											}
												}
											}
											else if (poss is ICmLocation)
											{
												using (var srdr = new StringReader(aliasXml))
												{
													using (var xrdrT = XmlReader.Create(srdr))
													{
												SetMultiUnicodeFromXml(xrdrT, (poss as ICmLocation).Alias);
												xrdrT.Close();
											}
												}
											}
											else
											{
												ReportUnexpectedElement(sItemClassName, "Alias");
											}
											aliasXml = null;
										}
										if (revabbrXml != null)
										{
											if (poss is ILexEntryType)
											{
												using (var srdr = new StringReader(revabbrXml))
												{
													using (var xrdrT = XmlReader.Create(srdr))
													{
												SetMultiUnicodeFromXml(xrdrT, (poss as ILexEntryType).ReverseAbbr);
												xrdrT.Close();
											}
												}
											}
											else
											{
												ReportUnexpectedElement(sItemClassName, "ReverseAbbr");
											}
											revabbrXml = null;
										}
										if (revabbrevXml != null)
										{
											if (poss is ILexRefType)
											{
												using (var srdr = new StringReader(revabbrXml))
												{
													using (var xrdrT = XmlReader.Create(srdr))
													{
												SetMultiUnicodeFromXml(xrdrT, (poss as ILexRefType).ReverseAbbreviation);
												xrdrT.Close();
											}
												}
											}
											else
											{
												ReportUnexpectedElement(sItemClassName, "ReverseAbbreviation");
											}
											revabbrevXml = null;
										}
										if (revnameXml != null)
										{
											if (poss is ILexRefType)
											{
												using (var srdr = new StringReader(revnameXml))
												{
													using (var xrdrT = XmlReader.Create(srdr))
													{
												SetMultiUnicodeFromXml(xrdrT, (poss as ILexRefType).ReverseName);
												xrdrT.Close();
											}
												}
											}
											else
											{
												ReportUnexpectedElement(sItemClassName, "ReverseName");
											}
											revnameXml = null;
										}
										if (questionsXml != null)
										{
											if (poss is ICmSemanticDomain)
											{
												using (var srdr = new StringReader(revnameXml))
												{
													using (var xrdrT = XmlReader.Create(srdr))
													{
												ReadQuestionsFromXml(xrdrT, poss as ICmSemanticDomain);
											}
												}
											}
											else
											{
												ReportUnexpectedElement(sItemClassName, "Questions");
											}
											questionsXml = null;
										}
									}
								}
								break;
							case "SubPossibilities":
								ReadSubPossibilitiesFromXml(xrdrSub, sItemClassName, mapNameToPoss);
								break;

							// Additional Subclass fields.

							case "Questions":
								if (poss is ICmSemanticDomain)
									ReadQuestionsFromXml(xrdrSub.ReadSubtree(), poss as ICmSemanticDomain);
								else if (poss == null)
									questionsXml = GetXmlOfElement(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub, sItemClassName);
								break;

							case "Alias":
								if (poss is ICmPerson)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ICmPerson).Alias);
								else if (poss is ICmLocation)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ICmLocation).Alias);
								else if (poss == null)
									aliasXml = GetXmlOfElement(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub, sItemClassName);
								break;

							case "ReverseAbbr":
								if (poss is ILexEntryType)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ILexEntryType).ReverseAbbr);
								else if (poss == null)
									revabbrXml = GetXmlOfElement(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub, sItemClassName);
								break;

							case "ReverseAbbreviation":
								if (poss is ILexRefType)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ILexRefType).ReverseAbbreviation);
								else if (poss == null)
									revabbrevXml = GetXmlOfElement(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub, sItemClassName);
								break;

							case "ReverseName":
								if (poss is ILexRefType)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ILexRefType).ReverseName);
								else if (poss == null)
									revnameXml = GetXmlOfElement(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub, sItemClassName);
								break;

							default:
								SkipAndReportUnexpectedElement(xrdrSub, sItemClassName);
								break;
						}
					}
				}
			}
			finally
			{
				xrdrSub.Close();
				if (m_progress != null)
					m_progress.Step(1);
			}
		}

		private void ReadQuestionsFromXml(XmlReader xrdrSub, ICmSemanticDomain semdom)
		{
			try
			{
				if (xrdrSub.IsEmptyElement)
				{
					xrdrSub.Read();
				}
				else if (xrdrSub.ReadToDescendant("CmDomainQ"))
				{
					do
					{
						ReadDomainQFromXml(xrdrSub.ReadSubtree(), semdom);
					} while (xrdrSub.ReadToNextSibling("CmDomainQ"));
				}
			}
			finally
			{
				xrdrSub.Close();
			}
		}

		private void ReadDomainQFromXml(XmlReader xrdrSub, ICmSemanticDomain semdom)
		{
			try
			{
				Dictionary<string, ICmDomainQ> mapQuest2Obj = new Dictionary<string, ICmDomainQ>();
				foreach (var dq in semdom.QuestionsOS)
				{
					ITsString tssQuest = dq.Question.get_String(m_wsEn);
					if (tssQuest == null)
						continue;
					string quest = tssQuest.Text;
					if (String.IsNullOrEmpty(quest))
						continue;
					if (!mapQuest2Obj.ContainsKey(quest))
						mapQuest2Obj.Add(quest, dq);
				}
				ICmDomainQ cdq = null;
				string wordsXml = null;
				string sentencesXml = null;
				while (xrdrSub.Read())
				{
					if (xrdrSub.NodeType == XmlNodeType.Element)
					{
						switch (xrdrSub.Name)
						{
							case "CmDomainQ":
								break;
							case "Question":
								cdq = FindQuestionAndSetIt(xrdrSub, mapQuest2Obj);
								if (!String.IsNullOrEmpty(wordsXml))
								{
									using (var srdr = new StringReader(wordsXml))
									{
										using (var xrdrWords = XmlReader.Create(srdr))
										{
									SetMultiUnicodeFromXml(xrdrWords, cdq.ExampleWords);
									xrdrWords.Close();
									wordsXml = null;
								}
									}
								}
								if (!String.IsNullOrEmpty(sentencesXml))
								{
									using (var srdr = new StringReader(sentencesXml))
									{
										using (var xrdrSentences = XmlReader.Create(srdr))
										{
									SetMultiStringFromXml(xrdrSentences, cdq.ExampleSentences);
									xrdrSentences.Close();
									wordsXml = null;
								}
									}
								}
								break;
							case "ExampleWords":
								if (cdq == null)
									wordsXml = GetXmlOfElement(xrdrSub);
								else
									SetMultiUnicodeFromXml(xrdrSub, cdq.ExampleWords);
								break;
							case "ExampleSentences":
								if (cdq == null)
									sentencesXml = GetXmlOfElement(xrdrSub);
								else
									SetMultiStringFromXml(xrdrSub, cdq.ExampleSentences);
								break;
							default:
								SkipAndReportUnexpectedElement(xrdrSub, "CmSemanticDomain/Questions/CmDomainQ");
								break;
						}
					}
				}
			}
			finally
			{
				xrdrSub.Close();
			}
		}

		private ICmDomainQ FindQuestionAndSetIt(XmlReader xrdr,
			Dictionary<string, ICmDomainQ> mapQuest2Obj)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();
				return null;
			}
			ICmDomainQ domq = null;
			if (xrdr.ReadToDescendant("AUni"))
			{
				Dictionary<int, string> mapWsValue = new Dictionary<int, string>();
				do
				{
					string sWs = xrdr.GetAttribute("ws");
					string sVal = xrdr.ReadString();
					if (sWs == "en")
					{
						// Use the English value to look up the CmDomainQ.
						mapQuest2Obj.TryGetValue(sVal, out domq);
						continue;		// don't overwrite the English string
					}
					int ws = GetWsFromStr(sWs);
					mapWsValue.Add(ws, sVal);
				} while (xrdr.ReadToNextSibling("AUni"));
				if (domq != null)
				{
					foreach (int ws in mapWsValue.Keys)
						domq.Question.set_String(ws, mapWsValue[ws]);
				}
			}
			xrdr.Read();	// read the end tag.
			return domq;
		}

		private ICmPossibility FindPossibilityAndSetName(XmlReader xrdr,
			Dictionary<string, ICmPossibility> mapNameToPoss)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();
				return null;
			}
			ICmPossibility poss = null;
			if (xrdr.ReadToDescendant("AUni"))
			{
				Dictionary<int, string> mapWsName = new Dictionary<int, string>();
				do
				{
					string sWs = xrdr.GetAttribute("ws");
					string sVal = xrdr.ReadString();
					if (sWs == "en")
					{
						// Use the English name to look up the possibility.
						mapNameToPoss.TryGetValue(sVal, out poss);
						continue;		// don't overwrite the English string
					}
					int ws = GetWsFromStr(sWs);
					mapWsName.Add(ws, sVal);
				} while (xrdr.ReadToNextSibling("AUni"));
				if (poss != null)
				{
					foreach (int ws in mapWsName.Keys)
					poss.Name.set_String(ws, mapWsName[ws]);
				}
			}
			xrdr.Read();	// read the end tag.
			return poss;
		}

		private string GetXmlOfElement(XmlReader xrdrSub)
		{
			return xrdrSub.ReadOuterXml();
		}

		private void ReadSubPossibilitiesFromXml(XmlReader xrdr, string sItemClassName,
			Dictionary<string, ICmPossibility> mapNameToPoss)
		{
			if (xrdr.ReadToDescendant(sItemClassName))
			{
				do
				{
					string sGuid = xrdr.GetAttribute("guid");
					xrdr.MoveToElement();
					ICmPossibility poss = null;
					Guid guid = Guid.Empty;
					if (!String.IsNullOrEmpty(sGuid))
					{
						guid = new Guid(sGuid);
						PossibilityRepository.TryGetObject(guid, out poss);
					}
					ReadPossItem(xrdr.ReadSubtree(), poss, sItemClassName, mapNameToPoss);
				} while (xrdr.ReadToNextSibling(sItemClassName));
			}
			xrdr.Read();	// reads end element.
		}

		private void SkipAndReportUnexpectedElement(XmlReader xrdrSub, string sItemClass)
		{
			// Ignore this -- it shouldn't be in the import file.
			ReportUnexpectedElement(sItemClass, xrdrSub.Name);
			xrdrSub.ReadOuterXml();
		}

		private void ReportUnexpectedElement(string sItemClass, string sField)
		{
			// TODO: do something...
		}

		private void SetMultiUnicodeFromXml(XmlReader xrdr, IMultiUnicode mu)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();
				return;
			}
			if (xrdr.ReadToDescendant("AUni"))
			{
				do
				{
					string sWs = xrdr.GetAttribute("ws");
					string sVal = xrdr.ReadString();
					if (sWs == "en")
						continue;		// don't overwrite the English string
					int ws = GetWsFromStr(sWs);
					mu.set_String(ws, sVal);
				} while (xrdr.ReadToNextSibling("AUni"));
			}
			xrdr.Read();	// read the end tag.
		}

		private void SetMultiStringFromXml(XmlReader xrdr, IMultiString ms)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();
				return;
			}
			if (xrdr.ReadToDescendant("AStr"))
			{
				do
				{
					string sWs = xrdr.GetAttribute("ws");
					string sXml = xrdr.ReadOuterXml();
					if (sWs == "en")
						continue;		// don't overwrite the English string
					int ws = GetWsFromStr(sWs);
					ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(sXml, m_wsf);
					ms.set_String(ws, tss);
				} while (xrdr.ReadToNextSibling("AStr"));
			}
			xrdr.Read();	// read the end tag.
		}

		private void GetItemClassFromAttributes(XmlReader xrdr, out string sItemClass, out int clid)
		{
			sItemClass = xrdr.GetAttribute("itemClass");
			if (String.IsNullOrEmpty(sItemClass))
				throw new Exception("Missing itemClass attribute for <List> element");
			clid = m_mdc.GetClassId(sItemClass);
			if (clid == 0)
				throw new Exception("Invalid itemClass attribute value for <List> element");
			bool fOk = false;
			for (int i = 0; i < m_rgItemClasses.Length; ++i)
			{
				if (clid == m_rgItemClasses[i])
				{
					fOk = true;
					break;
				}
			}
			if (!fOk)
				throw new Exception("Invalid itemClass attribute value for <List> element: not a CmPossibility");
		}

		private ICmPossibilityList GetListFromAttributes(XmlReader xrdr)
		{
			string sOwner;
			ICmObject owner = GetOwnerFromAttribute(xrdr, out sOwner);
			ICmPossibilityList list;
			string sField = xrdr.GetAttribute("field");
			if (String.IsNullOrEmpty(sField))
				throw new Exception("Missing field attribute for <List> element");
			int flid = m_mdc.GetFieldId2(owner.ClassID, sField, false);
			if (flid == 0)
				throw new Exception(String.Format("Invalid field attribute value for <List> element: {0}", sField));
			int hvoList = m_cache.DomainDataByFlid.get_ObjectProp(owner.Hvo, flid);
			if (hvoList == 0)
				throw new Exception(String.Format("List does not exist for {0} / {1}", sOwner, sField));
			list = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(hvoList);
			if (list == null)
				throw new Exception(String.Format("List does not exist for {0} / {1}", sOwner, sField));
			return list;
		}

		private ICmObject GetOwnerFromAttribute(XmlReader xrdr, out string sOwner)
		{
			sOwner = xrdr.GetAttribute("owner");
			if (String.IsNullOrEmpty(sOwner))
				throw new Exception("Missing owner attribute for <List> element");
			switch (sOwner)
			{
				case "LangProject":
					return m_cache.LangProject;
				case "LexDb":
					return m_cache.LangProject.LexDbOA;
				case "RnResearchNbk":
					return m_cache.LangProject.ResearchNotebookOA;
				case "DsDiscourseData":
					return m_cache.LangProject.DiscourseDataOA;
			}
			throw new Exception(String.Format("Invalid owner attribute value for <List> element: {0}", sOwner));
		}

		private static DateTime ReadDateAttribute(XmlReader xrdr)
		{
			DateTime dateOfTranslation;
			string sDate = xrdr.GetAttribute("date");
			if (String.IsNullOrEmpty(sDate))
				throw new Exception("Missing date attribute for <Lists> element");
			if (!DateTime.TryParse(sDate, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.AssumeUniversal, out dateOfTranslation))
				throw new Exception(String.Format("Invalid date attribute value for <Lists> element: {0}", sDate));
			return dateOfTranslation;
		}
	}
}
