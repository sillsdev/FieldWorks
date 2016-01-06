// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlTranslatedLists.cs
// Responsibility: mcconnel

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using ICSharpCode.SharpZipLib.Zip;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
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
		internal int m_wsEn;	// allow access to tests.
		// Record subclass names to go along with sItemClass used in many methods
		List<string> m_itemClassNames = new List<string>();

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
			using (var inputStream = FileUtils.OpenStreamForRead(filename))
			{
				var type = Path.GetExtension(filename).ToLowerInvariant();
				if (type == ".zip")
				{
					using (var zipStream = new ZipInputStream(inputStream))
					{
						var entry = zipStream.GetNextEntry(); // advances it to where we can read the one zipped file.
						using (var reader = new StreamReader(zipStream, Encoding.UTF8))
							ImportTranslatedLists(reader, cache, progress);
					}
				}
				else
				{
					using (var reader = new StreamReader(inputStream, Encoding.UTF8))
						ImportTranslatedLists(reader, cache, progress);
				}
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

			using (var xreader = XmlReader.Create(reader))
				Import(xreader);

			return true;
		}

		/// <summary>
		/// All localized list files are named with this prefix followed by the IcuLocale of the writing system, then .zip.
		/// Each file contains a single compressed XML file containing the data. It must contain nothing else.
		/// </summary>
		public const string LocalizedListPrefix = "LocalizedLists-";

		/// <summary>
		/// If a localized lists file is available for the specified ws, load the information.
		/// Note: call this only when you are sure the WS is new to this project. It takes considerable time
		/// to run if it finds a localized list file.
		/// Our current strategy for loading localized lists is to have one file per localization.
		/// It is always LocalizedLists-XX.zip, where XX is the ICU locale for the writing system.
		/// (The zip file contains a single file, LocalizedLists-XX.xml.)
		/// So if such a file exists for this WS, we can import lists for that writing system.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="cache"></param>
		/// <param name="templateDir"></param>
		/// <param name="progress"> </param>
		public static void ImportTranslatedListsForWs(string ws, FdoCache cache, string templateDir, IProgress progress)
		{
			string path = TranslatedListsPathForWs(ws, templateDir);
			if (File.Exists(path))
			{
				var instance = new XmlTranslatedLists();
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(cache.ActionHandlerAccessor,
					() => instance.ImportTranslatedLists(path, cache, progress));
			}
		}

		/// <summary>
		/// Caption recommended for a progress dialog while running ImportTranslatedListsForWs
		/// </summary>
		public static string ProgressDialogCaption
		{
			get { return Strings.ksTransListCaption; }
		}

		/// <summary>
		/// Call before ImportTranslatedListsForWs. Call that only if the file exists.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="templateDir"></param>
		/// <returns></returns>
		public static string TranslatedListsPathForWs(string ws, string templateDir)
		{
			return Path.Combine(templateDir, Path.ChangeExtension(LocalizedListPrefix + ws, "zip"));
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
							// we know the list, but we don't have the map yet at this level.
							ReadPossibilitiesFromXml(xrdr, list, sItemClass, "");
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

		private void ReadPossibilitiesFromXml(XmlReader xrdr, ICmPossibilityList list, string sItemClass,
			string ownerPath, Dictionary<string, ICmPossibility> mapNameToPoss = null)
		{
			var listElementName = xrdr.Name;
			do
			{
				xrdr.Read();
				if (xrdr.NodeType == XmlNodeType.Element &&
					(xrdr.Name == sItemClass || m_itemClassNames.Contains(xrdr.Name)))
				{
					if (mapNameToPoss == null)
						mapNameToPoss = GetMappingForList(list);
					string sGuid = xrdr.GetAttribute ("guid");
					xrdr.MoveToElement ();
					Guid guid = Guid.Empty;
					ICmPossibility poss = null;
					if (!String.IsNullOrEmpty (sGuid))
					{
						guid = new Guid (sGuid);
						PossibilityRepository.TryGetObject(guid, out poss);
					}
					using (var inner = xrdr.ReadSubtree())
					{
						ReadPossItem(inner, poss, sItemClass, ownerPath, mapNameToPoss);
						inner.Close();
					}
				}
				else if (xrdr.NodeType == XmlNodeType.EndElement && xrdr.Name == listElementName)
				{
					xrdr.ReadEndElement();
					return;
				}
			} while (!xrdr.EOF);
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

		/// <summary>
		/// Fills the in map of English name to CmPossibility object.
		/// </summary>
		/// <remarks>
		/// Allow access to tests.
		/// </remarks>
		internal void FillInMapForPossibilities(Dictionary<string, ICmPossibility> mapNameToItem,
			IFdoOwningSequence<ICmPossibility> possibilities)
		{
			if (possibilities == null)
				return;
			foreach (var item in possibilities)
			{
				var key = GetKeyForPossibility(item);
				if (String.IsNullOrEmpty(key))
					continue;
				if (mapNameToItem.ContainsKey(key))
					continue;
				mapNameToItem.Add(key, item);
				FillInMapForPossibilities(mapNameToItem, item.SubPossibilitiesOS);
			}
		}

		/// <summary>
		/// Generate a key by appending the name of this CmPossibility to the names of
		/// each of its owning CmPossibilities (if the owner is a CmPossibility).  The
		/// result is a rooted path of English CmPossibility names separated by colons,
		/// with a leading colon preceding the CmPossibility at the top level of the list.
		/// This allows items at lower levels of the list to have the same name without
		/// being confused.
		/// </summary>
		private string GetKeyForPossibility(ICmPossibility item)
		{
			ITsString tss = item.Name.get_String(m_wsEn);
			if (tss == null)
				return String.Empty;
			string name = tss.Text;
			if (String.IsNullOrEmpty(name))
				return String.Empty;
			var ownerPath = String.Empty;
			if (item.Owner is ICmPossibility)
				ownerPath = GetKeyForPossibility(item.Owner as ICmPossibility);
			return String.Format("{0}:{1}", ownerPath, name);
		}

		private void ReadPossItem(XmlReader xrdrSub, ICmPossibility poss, string sItemClassName, string ownerPath,
			Dictionary<string, ICmPossibility> mapNameToPoss)
		{
			xrdrSub.Read();
			Debug.Assert(xrdrSub.Name == sItemClassName || m_itemClassNames.Contains(xrdrSub.Name));
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
									poss = FindPossibilityAndSetName(xrdrSub, ownerPath, mapNameToPoss);
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
								// we have the map, but we don't know the list at this level.
								var myName = String.Empty;
								if (poss != null && poss.Name != null)
								{
									var tss = poss.Name.get_String(m_wsEn);
									if (tss != null && tss.Text != null)
										myName = tss.Text;
								}
								ReadPossibilitiesFromXml(xrdrSub, null, sItemClassName, String.Format("{0}:{1}", ownerPath, myName), mapNameToPoss);
								break;

							// Additional Subclass fields.

							case "Questions":
								if (poss is ICmSemanticDomain)
									ReadQuestionsFromXml(xrdrSub.ReadSubtree(), poss as ICmSemanticDomain);
								else if (poss == null)
									questionsXml = GetXmlOfElement(xrdrSub);
								else
									StoreOrReportUnexpectedElement(poss, xrdrSub, sItemClassName);
								break;

							case "Alias":
								if (poss is ICmPerson)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ICmPerson).Alias);
								else if (poss is ICmLocation)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ICmLocation).Alias);
								else if (poss == null)
									aliasXml = GetXmlOfElement(xrdrSub);
								else
									StoreOrReportUnexpectedElement(poss, xrdrSub, sItemClassName);
								break;

							case "ReverseAbbr":
								if (poss is ILexEntryType)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ILexEntryType).ReverseAbbr);
								else if (poss == null)
									revabbrXml = GetXmlOfElement(xrdrSub);
								else
									StoreOrReportUnexpectedElement(poss, xrdrSub, sItemClassName);
								break;

							case "ReverseAbbreviation":
								if (poss is ILexRefType)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ILexRefType).ReverseAbbreviation);
								else if (poss == null)
									revabbrevXml = GetXmlOfElement(xrdrSub);
								else
									StoreOrReportUnexpectedElement(poss, xrdrSub, sItemClassName);
								break;

							case "ReverseName":
								if (poss is ILexRefType)
									SetMultiUnicodeFromXml(xrdrSub, (poss as ILexRefType).ReverseName);
								else if (poss == null)
									revnameXml = GetXmlOfElement(xrdrSub);
								else
									StoreOrReportUnexpectedElement(poss, xrdrSub, sItemClassName);
								break;

							default:
								StoreOrReportUnexpectedElement(poss, xrdrSub, sItemClassName);
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
								StoreOrReportUnexpectedElement(cdq, xrdrSub, "CmSemanticDomain/Questions/CmDomainQ");
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

		private ICmPossibility FindPossibilityAndSetName(XmlReader xrdr, string ownerPath,
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
						mapNameToPoss.TryGetValue(String.Format("{0}:{1}", ownerPath, sVal), out poss);
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

		private void StoreOrReportUnexpectedElement(ICmObject obj, XmlReader xrdrSub, string sItemClass)
		{
			if (StoreUnanticipatedDataIfPossible(obj, xrdrSub))
				return;
			// Ignore this -- it shouldn't be in the import file.
			ReportUnexpectedElement(sItemClass, xrdrSub.Name);
			xrdrSub.ReadOuterXml();
		}

		/// <summary>
		/// Use the MetaDataCache and reflection to store the XML data if at all possible.
		/// </summary>
		/// <returns><c>true</c> if data was stored, <c>false</c> otherwise.</returns>
		private bool StoreUnanticipatedDataIfPossible(ICmObject obj, XmlReader xrdrSub)
		{
			try
			{
				var metaDataCache = obj.Cache.MetaDataCache as IFwMetaDataCacheManaged;
				if (metaDataCache == null)
					return false;
				var flid = metaDataCache.GetFieldId(obj.GetType().Name, xrdrSub.Name, true);
				if (flid == 0)
					return false;
				var type = metaDataCache.GetFieldType(flid);
				// REVIEW: how do we handle custom fields?  Do we need to worry about those in this context?
				var prop = obj.GetType().GetProperty(xrdrSub.Name);
				if (prop == null)
					return false;
				switch (type)
				{
				case (int)CellarPropertyType.MultiString:
					var ms = prop.GetValue(obj, null) as IMultiString;
					if (ms == null)
						return false;
					SetMultiStringFromXml(xrdrSub, ms);
					return true;
				case (int)CellarPropertyType.MultiUnicode:
					var mu = prop.GetValue(obj, null) as IMultiUnicode;
					if (mu == null)
						return false;
					SetMultiUnicodeFromXml(xrdrSub, mu);
					return true;
				default:
					// We don't know how to handle any other data types for now.  The two above are the ones
					// that are relevant for translation anyway.
					return false;
				}
			}
			catch
			{
				return false;
			}
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
			// Find the names of subclasses of our particular class.  One or more may be used in the XML file
			// instead of sItemClass.
			m_itemClassNames.Clear();
			int[] subClassIds = m_mdc.GetAllSubclasses(clid);
			for (int i = 0; i < subClassIds.Length; ++i)
				m_itemClassNames.Add(m_mdc.GetClassName(subClassIds [i]));
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
