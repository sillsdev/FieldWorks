// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlList.cs
// Responsibility: mcconnel
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Application.ApplicationServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class is used to import a possibility list from an XML file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlList
	{
		private Dictionary<Guid, List<Guid>> m_mapRelatedDomains;	// for Semantic Domain list
		private FdoCache m_cache;
		private IFwMetaDataCache m_mdc;
		private ILgWritingSystemFactory m_wsf;
		private IProgress m_progress;
		private ICmPossibilityFactory m_factPoss = null;
		private ICmAnthroItemFactory m_factAnthro = null;
		private ICmSemanticDomainFactory m_factSemDom = null;
		private ICmDomainQFactory m_factCmDomainQ = null;
		private IPartOfSpeechFactory m_factPOS = null;

		/// <summary>
		/// Constructor.
		/// </summary>
		public XmlList()
		{
			m_mapRelatedDomains = new Dictionary<Guid, List<Guid>>();
		}

		/// <summary>
		/// Load the list (given by owner and sFieldName) from the given (XML) file.
		/// </summary>
		public void ImportList(ICmObject owner, string sFieldName, string sFile, IProgress progress)
		{
#if DEBUG
			DateTime dtBegin = DateTime.Now;
#endif
			try
			{
				using (var reader = new StreamReader(sFile, Encoding.UTF8))
					ImportList(owner, sFieldName, reader, progress);
			}
			catch (Exception e)
			{
				MessageBoxUtils.Show(e.Message);
			}
#if DEBUG
			DateTime dtEnd = DateTime.Now;
			TimeSpan span = new TimeSpan(dtEnd.Ticks - dtBegin.Ticks);
			Debug.WriteLine(String.Format("Elapsed time for loading list from {0} = {1}", sFile, span.ToString()));
#endif
		}

		/// <summary>
		/// Load the list (given by owner and sFieldName) from the given TextReader.
		/// </summary>
		public void ImportList(ICmObject owner, string sFieldName, TextReader reader, IProgress progress)
		{
			m_cache = owner.Cache;
			m_mdc = m_cache.MetaDataCache;
			m_wsf = m_cache.WritingSystemFactory;
			m_progress = progress;

			try
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor, () =>
					{
						int flidList = m_mdc.GetFieldId(owner.ClassName, sFieldName, true);
						if (flidList == 0)
							throw new Exception(String.Format("Invalid list fieldname (programming error): {0}", sFieldName));
						using (var xrdr = XmlReader.Create(reader))
						{
							xrdr.MoveToContent();
							if (xrdr.Name != owner.ClassName)
								throw new Exception(String.Format("Unexpected outer element: {0}", xrdr.Name));

							if (!xrdr.ReadToDescendant(sFieldName))
								throw new Exception(String.Format("Unexpected second element: {0}", xrdr.Name));

							if (!xrdr.ReadToDescendant("CmPossibilityList"))
								throw new Exception(String.Format("Unexpected third element: {0}", xrdr.Name));

							ICmPossibilityList list;
							int hvo = m_cache.MainCacheAccessor.get_ObjectProp(owner.Hvo, flidList);
							if (hvo == 0)
								hvo = m_cache.MainCacheAccessor.MakeNewObject(CmPossibilityListTags.kClassId, owner.Hvo, flidList, -2);
							ICmPossibilityListRepository repo = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
							list = repo.GetObject(hvo);
							string sItemClassName = "CmPossibility";
							xrdr.Read();
							while (xrdr.Depth == 3)
							{
								xrdr.MoveToContent();
								if (xrdr.Depth < 3)
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
									case "Depth":
										list.Depth = ReadIntFromXml(xrdr);
										break;
									case "DisplayOption":
										list.DisplayOption = ReadIntFromXml(xrdr);
										break;
									case "HelpFile":
										list.HelpFile = ReadUnicodeFromXml(xrdr);
										break;
									case "IsClosed":
										list.IsClosed = ReadBoolFromXml(xrdr);
										break;
									case "IsSorted":
										list.IsSorted = ReadBoolFromXml(xrdr);
										break;
									case "IsVernacular":
										list.IsVernacular = ReadBoolFromXml(xrdr);
										break;
									case "ItemClsid":
										list.ItemClsid = ReadIntFromXml(xrdr);
										sItemClassName = m_mdc.GetClassName(list.ItemClsid);
										break;
									case "ListVersion":
										list.ListVersion = ReadGuidFromXml(xrdr);
										break;
									case "PreventChoiceAboveLevel":
										list.PreventChoiceAboveLevel = ReadIntFromXml(xrdr);
										break;
									case "PreventDuplicates":
										list.PreventDuplicates = ReadBoolFromXml(xrdr);
										break;
									case "PreventNodeChoices":
										list.PreventNodeChoices = ReadBoolFromXml(xrdr);
										break;
									case "UseExtendedFields":
										list.UseExtendedFields = ReadBoolFromXml(xrdr);
										break;
									case "WsSelector":
										list.WsSelector = ReadIntFromXml(xrdr);
										break;
									case "Possibilities":
										LoadPossibilitiesFromXml(xrdr, list, sItemClassName);
										break;
									case "HeaderFooterSets":
										throw new Exception("We don't (yet?) handle HeaderFooterSets for CmPossibilityList (programming issue)");
									case "Publications":
										throw new Exception("We don't (yet?) handle Publications for CmPossibilityList (programming issue)");
									default:
										throw new Exception(String.Format("Unknown field element in CmPossibilityList: {0}", xrdr.Name));
								}
							}
							xrdr.Close();
							if (m_mapRelatedDomains.Count > 0)
								SetRelatedDomainsLinks();
						}
					});
			}
			catch (Exception e)
			{
				MessageBoxUtils.Show(e.Message);
			}
		}

		Guid EnsureGuid(Guid guid)
		{
			if (guid == Guid.Empty)
				return Guid.NewGuid();
			return guid;
		}

		private void LoadPossibilitiesFromXml(XmlReader xrdr, ICmPossibilityList owner, string sItemClassName)
		{
			if (xrdr.ReadToDescendant(sItemClassName))
			{
				EnsureFactoryForClass(sItemClassName);
				do
				{
					string sGuid = xrdr.GetAttribute("guid");
					xrdr.MoveToElement();
					Guid guid = Guid.Empty;
					if (!String.IsNullOrEmpty(sGuid))
						guid = new Guid(sGuid);
					ICmPossibility poss = null;
					switch (sItemClassName)
					{
						case "CmPossibility":
							poss = m_factPoss.Create(EnsureGuid(guid), owner);
							break;
						case "CmAnthroItem":
							poss = m_factAnthro.Create(EnsureGuid(guid), owner) as ICmPossibility;
							break;
						case "CmSemanticDomain":
							poss = m_factSemDom.Create(EnsureGuid(guid), owner) as ICmPossibility;
							break;
						case "PartOfSpeech":
							Debug.Assert(guid == Guid.Empty);
							poss = m_factPOS.Create() as ICmPossibility;
							owner.PossibilitiesOS.Add(poss);
							break;
						default:
							// TODO: implement other subclasses of CmPossibility?
							break;
					}
					Debug.Assert(poss != null);
					ReadPossItem(xrdr.ReadSubtree(), poss, sItemClassName);
				} while (xrdr.ReadToNextSibling(sItemClassName));
			}
			xrdr.Read();	// reads end element.
		}

		private void ReadPossItem(XmlReader xrdrSub, ICmPossibility poss, string sItemClassName)
		{
			xrdrSub.Read();
			Debug.Assert(xrdrSub.Name == sItemClassName);
			try
			{
				while (xrdrSub.Read())
				{
					if (xrdrSub.NodeType == XmlNodeType.Element)
					{
						switch (xrdrSub.Name)
						{
							case "Abbreviation":
								SetMultiUnicodeFromXml(xrdrSub, poss.Abbreviation);
								break;
							case "BackColor":
								poss.BackColor = ReadIntFromXml(xrdrSub);
								break;
							case "Description":
								SetMultiStringFromXml(xrdrSub, poss.Description);
								break;
							case "ForeColor":
								poss.ForeColor = ReadIntFromXml(xrdrSub);
								break;
							case "HelpId":
								poss.HelpId = ReadUnicodeFromXml(xrdrSub);
								break;
							case "Hidden":
								poss.Hidden = ReadBoolFromXml(xrdrSub);
								break;
							case "IsProtected":
								poss.IsProtected = ReadBoolFromXml(xrdrSub);
								break;
							case "Name":
								SetMultiUnicodeFromXml(xrdrSub, poss.Name);
								break;
							case "SortSpec":
								poss.SortSpec = ReadIntFromXml(xrdrSub);
								break;
							case "SubPossibilities":
								LoadSubPossibilitiesFromXml(xrdrSub, poss, sItemClassName);
								break;
							case "UnderColor":
								poss.UnderColor = ReadIntFromXml(xrdrSub);
								break;
							case "UnderStyle":
								poss.UnderStyle = ReadIntFromXml(xrdrSub);
								break;

							// Additional CmSemanticDomain fields.
							case "LouwNidaCodes":
								if (poss is ICmSemanticDomain)
									(poss as ICmSemanticDomain).LouwNidaCodes = ReadUnicodeFromXml(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub);
								break;
							case "OcmCodes":
								if (poss is ICmSemanticDomain)
									(poss as ICmSemanticDomain).OcmCodes = ReadUnicodeFromXml(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub);
								break;
							case "Questions":
								if (poss is ICmSemanticDomain)
									LoadQuestionsFromXml(xrdrSub, poss as ICmSemanticDomain);
								else
									SkipAndReportUnexpectedElement(xrdrSub);
								break;
							case "RelatedDomains":
								if (poss is ICmSemanticDomain)
									LoadRelatedDomainsFromXml(xrdrSub, poss as ICmSemanticDomain);
								else
									SkipAndReportUnexpectedElement(xrdrSub);
								break;

							// Additional PartOfSpeech fields.
							case "CatalogSourceId":
								if (poss is IPartOfSpeech)
									(poss as IPartOfSpeech).CatalogSourceId = ReadUnicodeFromXml(xrdrSub);
								else
									SkipAndReportUnexpectedElement(xrdrSub);
								break;

							default:
								SkipAndReportUnexpectedElement(xrdrSub);
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

		private void SkipAndReportUnexpectedElement(XmlReader xrdrSub)
		{
			// Ignore this -- it shouldn't be in the import file.
			xrdrSub.ReadOuterXml();
		}

		/// <summary>
		/// Load data that looks like this
		/// &lt;Questions&gt;
		///	&lt;CmDomainQ&gt;
		///		&lt;Question&gt;
		///			&lt;AUni ws="en"&gt;(1) What words refer to the sun?&lt;/AUni&gt;
		///			&lt;AUni ws="fr"&gt;Quels sont les mots pour parler du soleil?&lt;/AUni&gt;
		///		&lt;/Question&gt;
		///		&lt;ExampleWords&gt;
		///			&lt;AUni ws="en"&gt;sun, solar, sol, daystar, our star&lt;/AUni&gt;
		///			&lt;AUni ws="fr"&gt;soleil, solaire, notre soleil&lt;/AUni&gt;
		///		&lt;/ExampleWords&gt;
		///	&lt;/CmDomainQ&gt;
		///	&lt;CmDomainQ&gt;
		///		&lt;Question&gt;
		///			&lt;AUni ws="en"&gt;(2) What words refer to the time when the sun rises?&lt;/AUni&gt;
		///			&lt;AUni ws="fr"&gt;Quels sont les mots qui signifient le lever du soleil?&lt;/AUni&gt;
		///		&lt;/Question&gt;
		///		&lt;ExampleWords&gt;
		///			&lt;AUni ws="en"&gt;dawn, sunrise, sunup, daybreak, cockcrow, &lt;/AUni&gt;
		///		&lt;/ExampleWords&gt;
		///		&lt;ExampleSentences&gt;
		///			&lt;AStr ws="en"&gt;
		///				&lt;Run ws="en"&gt;We got up before dawn, in order to get an early start.&lt;/Run&gt;
		///			&lt;/AStr&gt;
		///		&lt;/ExampleSentences&gt;
		///	&lt;/CmDomainQ&gt;
		///&lt;/Questions&gt;
		/// </summary>
		private void LoadQuestionsFromXml(XmlReader xrdr, ICmSemanticDomain dom)
		{
			Debug.Assert(dom != null);
			if (xrdr.ReadToDescendant("CmDomainQ"))
			{
				EnsureFactoryForClass("CmDomainQ");
				do
				{
					ReadDomainQuestion(xrdr.ReadSubtree(), dom);
				} while (xrdr.ReadToNextSibling("CmDomainQ"));
			}
			xrdr.Read();	// reads end element
		}

		private void ReadDomainQuestion(XmlReader xrdrSub, ICmSemanticDomain dom)
		{
			try
			{
				ICmDomainQ domQ = null;
				while (xrdrSub.Read())
				{
					if (xrdrSub.NodeType == XmlNodeType.Element)
					{
						switch (xrdrSub.Name)
						{
							case "ExampleSentences":
								if (domQ == null)
								{
									domQ = m_factCmDomainQ.Create();
									dom.QuestionsOS.Add(domQ);
								}
								SetMultiStringFromXml(xrdrSub, domQ.ExampleSentences);
								break;
							case "ExampleWords":
								if (domQ == null)
								{
									domQ = m_factCmDomainQ.Create();
									dom.QuestionsOS.Add(domQ);
								}
								SetMultiUnicodeFromXml(xrdrSub, domQ.ExampleWords);
								break;
							case "Question":
								if (domQ == null)
								{
									domQ = m_factCmDomainQ.Create();
									dom.QuestionsOS.Add(domQ);
								}
								SetMultiUnicodeFromXml(xrdrSub, domQ.Question);
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

		/// <summary>
		/// Load data that looks like this, storing it for future processing.  (Some of the
		/// guids may be forward references.)
		/// &lt;RelatedDomains&gt;
		///		&lt;Link guid="4BF411B7-2B5B-4673-B116-0E6C31FBD08A"/&gt;
		///		&lt;Link guid="CBA6876C-5B48-42F4-AE0A-7FBE9BB971EF"/&gt;
		///		&lt;Link guid="0CC62B4A-D5FF-4F45-83D1-E2B46E5D159A"/&gt;
		/// &lt;/RelatedDomains&gt;
		/// </summary>
		private void LoadRelatedDomainsFromXml(XmlReader xrdr, ICmSemanticDomain dom)
		{
			Debug.Assert(dom != null);
			if (xrdr.ReadToDescendant("Link"))
			{
				do
				{
					string sGuid = xrdr.GetAttribute("guid");
					if (!String.IsNullOrEmpty(sGuid))
					{
						Guid guidLink = new Guid(sGuid);
						List<Guid> rgGuids;
						if (!m_mapRelatedDomains.TryGetValue(dom.Guid, out rgGuids))
						{
							rgGuids = new List<Guid>();
							m_mapRelatedDomains.Add(dom.Guid, rgGuids);
						}
						rgGuids.Add(guidLink);
					}
				} while (xrdr.ReadToNextSibling("Link"));
			}
			xrdr.Read();	// reads end element
		}

		private void SetRelatedDomainsLinks()
		{
			ICmSemanticDomainRepository repo = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainRepository>();
			foreach (Guid guidKey in m_mapRelatedDomains.Keys)
			{
				try
				{
					ICmSemanticDomain dom = repo.GetObject(guidKey);
					foreach (Guid guidLink in m_mapRelatedDomains[guidKey])
					{
						try
						{
							ICmSemanticDomain domLink = repo.GetObject(guidLink);
							dom.RelatedDomainsRC.Add(domLink);
						}
						catch { }
					}
					if (m_progress != null)
						m_progress.Step(1);
				}
				catch { }
			}
		}

		private void LoadSubPossibilitiesFromXml(XmlReader xrdr, ICmPossibility owner, string sItemClassName)
		{
			Debug.Assert(owner != null);
			if (xrdr.ReadToDescendant(sItemClassName))
			{
				do
				{
					string sGuid = xrdr.GetAttribute("guid");
					xrdr.MoveToElement();
					Guid guid = Guid.Empty;
					if (!String.IsNullOrEmpty(sGuid))
						guid = new Guid(sGuid);
					ICmPossibility poss = null;
					switch (sItemClassName)
					{
						case "CmPossibility":
							poss = m_factPoss.Create(EnsureGuid(guid), owner);
							break;
						case "CmAnthroItem":
							Debug.Assert(owner is ICmAnthroItem);
							poss = m_factAnthro.Create(EnsureGuid(guid), owner as ICmAnthroItem) as ICmPossibility;
							break;
						case "CmSemanticDomain":
							Debug.Assert(owner is ICmSemanticDomain);
							poss = m_factSemDom.Create(EnsureGuid(guid), owner as ICmSemanticDomain) as ICmPossibility;
							break;
						case "PartOfSpeech":
							Debug.Assert(owner is IPartOfSpeech);
							Debug.Assert(guid == Guid.Empty);
							poss = m_factPOS.Create() as ICmPossibility;
							owner.SubPossibilitiesOS.Add(poss);
							break;
						default:
							// TODO: implement the other subclasses of CmPossibility?
							break;
					}
					Debug.Assert(poss != null);
					ReadPossItem(xrdr.ReadSubtree(), poss, sItemClassName);
				} while (xrdr.ReadToNextSibling(sItemClassName));
			}
			xrdr.Read();	// reads end element.
		}

		private void EnsureFactoryForClass(string sClassName)
		{
			switch (sClassName)
			{
				case "CmPossibility":
					if (m_factPoss == null)
						m_factPoss = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
					break;
				case "CmAnthroItem":
					if (m_factAnthro == null)
						m_factAnthro = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
					break;
				case "CmSemanticDomain":
					if (m_factSemDom == null)
						m_factSemDom = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
					break;
				case "CmDomainQ":
					if (m_factCmDomainQ == null)
						m_factCmDomainQ = m_cache.ServiceLocator.GetInstance<ICmDomainQFactory>();
					break;
				case "PartOfSpeech":
					if (m_factPOS == null)
						m_factPOS = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
					break;
				default:
					// TODO: implement the other subclasses of CmPossibility.
					throw new NotImplementedException();
			}
		}

		/// <summary>
		/// Read and return the value from an XML fragment like
		/// &lt;Depth&gt;&lt;Boolean val="true"/&gt;&lt;/Depth&gt;.
		/// </summary>
		private bool ReadBoolFromXml(XmlReader xrdr)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();
				return false;
			}
			if (xrdr.ReadToDescendant("Boolean"))
			{
				string sVal = xrdr.GetAttribute("val");
				bool fVal;
				if (bool.TryParse(sVal, out fVal))
				{
					xrdr.Read();	// reads <Boolean/>
					xrdr.Read();	// reads end element.
					return fVal;
				}
				else
				{
					throw new Exception(String.Format("Invalid Boolean value: val=\"{0}\"", sVal));
				}
			}
			else
			{
				xrdr.Read();		// reads the end tag.
				return false;
			}
		}

		/// <summary>
		/// Read and return the value from an XML fragment like
		/// &lt;Depth&gt;&lt;Integer val="127"/&gt;&lt;/Depth&gt;.
		/// </summary>
		private int ReadIntFromXml(XmlReader xrdr)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();	// reads the end tag.
				return 0;
			}
			if (xrdr.ReadToDescendant("Integer"))
			{
				string sVal = xrdr.GetAttribute("val");
				int nVal;
				if (Int32.TryParse(sVal, out nVal))
				{
					xrdr.Read();	// reads <Integer/>
					xrdr.Read();	// reads end tag.
					return nVal;
				}
				else
				{
					throw new Exception(String.Format("Invalid Integer value: val=\"{0}\"", sVal));
				}
			}
			else
			{
				xrdr.Read();	// reads the end tag.
				return 0;
			}
		}

		/// <summary>
		/// Read and return the value from an XML fragment like
		/// &lt;ListVersion8&gt;&lt;Guid val="B41FF27F-5CAF-4EAC-92DE-0F92ACB0CAA3"/&gt;&lt;/ListVersion8&gt;
		/// </summary>
		/// <param name="xrdr"></param>
		/// <returns></returns>
		private Guid ReadGuidFromXml(XmlReader xrdr)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();	// reads the end tag.
				return Guid.Empty;
			}
			if (xrdr.ReadToDescendant("Guid"))
			{
				string sVal = xrdr.GetAttribute("val");
				Guid guidVal = new Guid(sVal);
				xrdr.Read();	// reads <Guid/>
				xrdr.Read();	// reads end tag.
				return guidVal;
			}
			else
			{
				xrdr.Read();	// reads the end tag.
				return Guid.Empty;
			}
		}

		/// <summary>
		/// Read and return the value from an XML fragment like
		/// &lt;HelpFile&gt;&lt;Uni&gt;OcmFrame.chm&lt;/Uni&gt;&lt;/HelpFile&gt;.
		/// </summary>
		private string ReadUnicodeFromXml(XmlReader xrdr)
		{
			if (xrdr.IsEmptyElement)
			{
				xrdr.Read();
				return null;
			}
			if (xrdr.ReadToDescendant("Uni"))
			{
				string sVal = xrdr.ReadString();
				xrdr.Read();		// reads </Uni>
				xrdr.Read();		// reads </[sField]>
				return sVal;
			}
			else
			{
				xrdr.Read();	// reads the end tag.
				return null;
			}
		}

		/// <summary>
		/// Read multilingual Unicode data from XML fragments like
		/// &lt;Name&gt;
		///		&lt;AUni ws="en"&gt;FRAME and OCM Categories&lt;/AUni&gt;
		///	&lt;/Name&gt;
		/// </summary>
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
					int ws = m_wsf.GetWsFromStr(sWs);
					if (ws != 0)
						mu.set_String(ws, sVal);
				} while (xrdr.ReadToNextSibling("AUni"));
			}
			xrdr.Read();	// read the end tag.
		}

		/// <summary>
		/// Reads multilingual string data from XML fragments like
		/// &lt;Description&gt;
		///		&lt;AStr ws="en"&gt;
		///			&lt;Run ws="en"&gt;This list contains categories.&lt;/Run&gt;
		///		&lt;/AStr&gt;
		///	&lt;/Description&gt;
		/// </summary>
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
					int ws = m_wsf.GetWsFromStr(sWs);
					if (ws != 0)
					{
						ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(sXml, m_wsf);
						ms.set_String(ws, tss);
					}
				} while (xrdr.ReadToNextSibling("AStr"));
			}
			xrdr.Read();	// read the end tag.
		}
	}
}
