// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// InterlinearExporter is an IVwEnv implementation which exports interlinear data to an XmlWriter.
	/// Make one of these by creating an XmlTextWriter.
	/// </summary>
	public class InterlinearExporter : CollectorEnv
	{
		protected XmlWriter m_writer;
		protected FdoCache m_cache;
		bool m_fItemIsOpen = false; // true while doing some <item> element (causes various things to output data)
		bool m_fDoingHeadword = false; // true while displaying a headword (causes some special behaviors)
		bool m_fDoingHomographNumber = false; // true after note dependency on homograph number, to end of headword.
		bool m_fDoingVariantTypes = false; // can become true after processing headword and homograph number
		bool m_fAwaitingHeadwordForm = false; // true after start of headword until we get a string alt.
		bool m_fDoingMorphType = false; // true during display of MorphType of MoForm.
		bool m_fDoingInterlinName = false; // true during MSA
		bool m_fDoingGlossAppend = false; // true after special AddProp
		string m_sPendingPrefix; // got a prefix, need the ws from the form itself before we write it.
		string m_sFreeAnnotationType;
		ITsString m_tssPendingHomographNumber;
		List<ITsString> pendingTitles = new List<ITsString>(); // these come along before the right element is open.
		List<ITsString> pendingSources = new List<ITsString>();
		private List<ITsString> pendingAbbreviations = new List<ITsString>();
		List<ITsString> pendingComments = new List<ITsString>();
		int m_flidStTextTitle;
		int m_flidStTextSource;
		InterlinVc m_vc = null;
		Set<int> m_usedWritingSystems = new Set<int>();
		/// <summary>saves the morphtype so that glosses can be marked as pro/enclitics.  See LT-8288.</summary>
		Guid m_guidMorphType = Guid.Empty;
		IMoMorphType m_mmtEnclitic;
		IMoMorphType m_mmtProclitic;
		protected IWritingSystemManager m_wsManager;
		protected ICmObjectRepository m_repoObj;

		public static InterlinearExporter Create(string mode, FdoCache cache, XmlWriter writer, ICmObject objRoot,
			InterlinLineChoices lineChoices, InterlinVc vc)
		{
			if (mode != null && mode.ToLowerInvariant() == "elan")
			{
				return new InterlinearExporterForElan(cache, writer, objRoot, lineChoices, vc);
			}
			else
			{
				return new InterlinearExporter(cache, writer, objRoot, lineChoices, vc);
			}
		}

		protected InterlinearExporter(FdoCache cache, XmlWriter writer, ICmObject objRoot,
			InterlinLineChoices lineChoices, InterlinVc vc)
			: base(null, cache.MainCacheAccessor, objRoot.Hvo)
		{
			m_cache = cache;
			m_writer = writer;
			m_flidStTextTitle = m_cache.MetaDataCacheAccessor.GetFieldId("StText", "Title", false);
			m_flidStTextSource = m_cache.MetaDataCacheAccessor.GetFieldId("StText", "Source", false);
			m_vc = vc;
			SetTextTitleAndMetadata(objRoot as IStText);

			// Get morphtype information that we need later.  (plus stuff we don't...)  See LT-8288.
			IMoMorphType mmtStem;
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			IMoMorphType mmtBoundStem;
			IMoMorphType mmtSimulfix;
			IMoMorphType mmtSuprafix;
			m_cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetMajorMorphTypes(
				out mmtStem, out mmtPrefix, out mmtSuffix, out mmtInfix,
				out mmtBoundStem, out m_mmtProclitic, out m_mmtEnclitic,
				out mmtSimulfix, out mmtSuprafix);

			m_wsManager = m_cache.ServiceLocator.WritingSystemManager;
			m_repoObj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
		}

		public void ExportDisplay()
		{
			m_vc.Display(this, this.OpenObject, (int)InterlinVc.kfragStText);
		}

		public virtual void WriteBeginDocument()
		{
			m_writer.WriteStartDocument();
			m_writer.WriteStartElement("document");
		}

		public void WriteEndDocument()
		{
			m_writer.WriteEndElement();
			m_writer.WriteEndDocument();
		}

		public string FreeAnnotationType
		{
			get { return m_sFreeAnnotationType; }
			set { m_sFreeAnnotationType = value; }
		}

		public override void AddStringProp(int tag, IVwViewConstructor _vwvc)
		{
			if (tag == PunctuationFormTags.kflidForm)
			{
				// <item type="punct">
				WriteItem(tag, "punct", 0);
			}
		}

		public override void NoteDependency(int[] _rghvo, int[] rgtag, int chvo)
		{
			if (m_fDoingHeadword && rgtag.Length == 1 && rgtag[0] == LexEntryTags.kflidHomographNumber)
				m_fDoingHomographNumber = true;
		}

		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			//get the writing system for english for use in writing out the morphType, the types will ALWAYS be defined in english
			//but they may not be defined in other languages, using English on import and export should garauntee the correct behavior
			int englishWS = m_cache.WritingSystemFactory.GetWsFromStr("en");
			if (m_fDoingMorphType)
			{
				m_writer.WriteAttributeString("type", GetText(m_sda.get_MultiStringAlt(m_hvoCurr, tag, englishWS)));
				// also output the GUID so any tool that processes the output can know for sure which morphtype it is
				// Save the morphtype.  See LT-8288.
				m_guidMorphType = WriteGuidAttributeForCurrentObj();
			}
			else if (m_fDoingHeadword)
			{
				// Lexeme or citation form in headword. Dump it out, along with any saved affix marker.
				WritePrefixLangAlt(ws, tag);
				m_fAwaitingHeadwordForm = false;
			}
			else if (tag == MoFormTags.kflidForm)
			{
				if (m_fItemIsOpen)
				{
					WritePrefixLangAlt(ws, tag);
				}
			}
			else if (m_fItemIsOpen)
			{
				if (tag == LexSenseTags.kflidGloss)
					WriteLangAndContent(ws, GetMarkedGloss(m_hvoCurr, tag, ws));
				else
					WriteLangAndContent(ws, m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws));
			}
			else if (tag == WfiWordformTags.kflidForm)
			{
				WriteItem(tag, "txt", ws);
			}
			else if (tag == WfiGlossTags.kflidForm)
			{
				WriteItem(tag, "gls", ws);
			}
			else if (tag == WfiMorphBundleTags.kflidForm)
			{
				WriteItem(tag, "txt", ws);
			}
			else if (tag == SegmentTags.kflidFreeTranslation ||
				tag == SegmentTags.kflidLiteralTranslation ||
				tag == NoteTags.kflidContent)
			{
				WriteItem(tag, m_sFreeAnnotationType, ws);
			}
			else if (tag == m_flidStTextTitle)
			{
				ITsString tssTitle = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
				if (!pendingTitles.Contains(tssTitle))
					pendingTitles.Add(tssTitle);
			}
			else if (tag == m_flidStTextSource)
			{
				ITsString source = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
				if (!pendingSources.Contains(source))
					pendingSources.Add(source);
			}
			else if (tag == CmMajorObjectTags.kflidDescription)
			{
				ITsString comment = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
				if (!pendingComments.Contains(comment))
					pendingComments.Add(comment);
			}
			else
			{
				Debug.WriteLine(
					String.Format("Export.AddStringAltMember(hvo={0}, tag={1}, ws={2})", m_hvoCurr, tag, ws));
			}
		}

		protected Guid WriteGuidAttributeForCurrentObj()
		{
			return WriteGuidAttributeForObj(m_hvoCurr);
		}

		protected Guid WriteGuidAttributeForObj(int hvo)
		{
			if (m_repoObj.IsValidObjectId(hvo))
			{
				ICmObject obj = m_repoObj.GetObject(hvo);
				Guid guid = obj.Guid;
				m_writer.WriteAttributeString("guid", guid.ToString());
				return guid;
			}
			else
			{
				return Guid.Empty;
			}
		}

		/// <summary>
		/// Glosses must be marked as proclitics or enclitics.  See LT-8288.
		/// </summary>
		private ITsString GetMarkedGloss(int hvo, int tag, int ws)
		{
			ITsString tss = m_sda.get_MultiStringAlt(hvo, tag, ws);
			string sPrefix = null;
			string sPostfix = null;
			if (m_guidMorphType == m_mmtEnclitic.Guid)
			{
				sPrefix = m_mmtEnclitic.Prefix;
				sPostfix = m_mmtEnclitic.Postfix;
			}
			else if (m_guidMorphType == m_mmtProclitic.Guid)
			{
				sPrefix = m_mmtProclitic.Prefix;
				sPostfix = m_mmtProclitic.Postfix;
			}
			if (sPrefix != null || sPostfix != null)
			{
				ITsStrBldr tsb = tss.GetBldr();
				if (!String.IsNullOrEmpty(sPrefix))
					tsb.Replace(0, 0, sPrefix, null);
				if (!String.IsNullOrEmpty(sPostfix))
					tsb.Replace(tsb.Length, tsb.Length, sPostfix, null);
				tss = tsb.GetString();
			}
			return tss;
		}

		private void WritePrefixLangAlt(int ws, int tag)
		{
			string icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			m_writer.WriteAttributeString("lang", icuCode);
			if (m_sPendingPrefix != null)
			{
				m_writer.WriteString(m_sPendingPrefix);
				m_sPendingPrefix = null;
			}
			m_writer.WriteString(GetText(m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws)));
		}

		private void WriteItem(int tag, string itemType, int alt)
		{
			ITsString tss;
			int ws = alt;
			if (ws == 0)
			{
				tss = m_sda.get_StringProp(m_hvoCurr, tag);
			}
			else
			{
				tss = m_sda.get_MultiStringAlt(m_hvoCurr, tag, alt);
			}
			WriteItem(itemType, tss);
		}

		private void WriteItem(string itemType, ITsString tss)
		{
			m_writer.WriteStartElement("item");
			m_writer.WriteAttributeString("type", itemType);
			int ws = GetWsFromTsString(tss);
			WriteLangAndContent(ws, tss);
			m_writer.WriteEndElement();
		}

		private int GetWsFromTsString(ITsString tss)
		{
			ITsTextProps ttp = tss.get_PropertiesAt(0);
			int var;
			return ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
		}

		/// <summary>
		/// Write a lang attribute identifying the string, then its content as the body of
		/// an element.
		/// </summary>
		/// <param name="ws"></param>
		/// <param name="tss"></param>
		private void WriteLangAndContent(int ws, ITsString tss)
		{
			UpdateWsList(ws);
			string icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			m_writer.WriteAttributeString("lang", icuCode);
			m_writer.WriteString(GetText(tss));
		}

		void UpdateWsList(int ws)
		{
			// only add valid actual ws
			if (ws > 0)
			{
				m_usedWritingSystems.Add(ws);
			}
		}

		string GetText(ITsString tss)
		{
			string result = tss.Text;
			if (result == null)
				return "";
			else
				return result.Normalize();
		}

		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			if (frag == (int)SIL.FieldWorks.FdoUi.VcFrags.kfragHeadWord)
			{
				// In the course of this AddObj, typically we get AddString calls for
				// morpheme separators, AddObjProp for kflidLexemeForm,
				// AddStringAltMember for the form of the LF, NoteDependency for a homograph number,
				// and possibly AddString for the HN itself. We want to produce something like
				// <item type="cf" lang="xkal">-de<hn lang="en">2</hn></item>
				OpenItem("cf");
				m_fDoingHeadword = true;
				m_fAwaitingHeadwordForm = true;
			}
			// (LT-9374) Export Variant Type information for variants
			if (frag == (int)SIL.FieldWorks.FdoUi.LexEntryVc.kfragVariantTypes)
			{
				m_fDoingVariantTypes = true;
				OpenItem("variantTypes");
				string icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultAnalWs);
				m_writer.WriteAttributeString("lang", icuCode);
			}
			if (vc is InterlinVc && frag >= InterlinVc.kfragLineChoices && frag < InterlinVc.kfragLineChoices + (vc as InterlinVc).LineChoices.Count)
			{
				var spec = (vc as InterlinVc).LineChoices[frag - InterlinVc.kfragLineChoices];
				if (spec.Flid == InterlinLineChoices.kflidLexGloss)
				{
					OpenItem("gls");
				}
			}
			base.AddObj (hvoItem, vc, frag);
			if (frag == (int)SIL.FieldWorks.FdoUi.VcFrags.kfragHeadWord)
			{
				CloseItem();
				m_fDoingHeadword = false;
				m_fDoingHomographNumber = false;
				WritePendingItem("hn", ref m_tssPendingHomographNumber);
			}
			if (frag == (int)SIL.FieldWorks.FdoUi.LexEntryVc.kfragVariantTypes)
			{
				CloseItem();
				m_fDoingVariantTypes = false;
			}
			if (vc is InterlinVc && frag >= InterlinVc.kfragLineChoices && frag < InterlinVc.kfragLineChoices + (vc as InterlinVc).LineChoices.Count)
			{
				var spec = (vc as InterlinVc).LineChoices[frag - InterlinVc.kfragLineChoices];
				if (spec.Flid == InterlinLineChoices.kflidLexGloss)
				{
					CloseItem();
				}
			}
		}

		public override void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			if (tag == InterlinVc.ktagGlossAppend)
			{
				m_fDoingGlossAppend = true;
			}
			base.AddProp(tag, vc, frag);
			if (tag == InterlinVc.ktagGlossAppend)
			{
				m_fDoingGlossAppend = false;
			}

		}

		public override void AddTsString(ITsString tss)
		{
			if (m_fDoingGlossAppend)
				WriteItem("glsAppend", tss);
			base.AddTsString(tss);
		}

		public override void AddString(ITsString tss)
		{
			// Ignore directionality markers on export.
			if (tss.Text == "\x200F" || tss.Text == "\x200E")
				return;

			if (m_fDoingHomographNumber)
			{
				m_tssPendingHomographNumber = tss;
			}
			else if (m_fDoingVariantTypes)
			{
				// For now just concatenate all the variant types info into one string (including separators [+,]).
				// NOTE: We'll need to re-evaluate this when we want this (and homograph item) to be
				// standard enough to import (see LT-9664).
				m_writer.WriteString(GetText(tss));
			}
			else if (m_fDoingHeadword)
			{
				if (m_fAwaitingHeadwordForm)
				{
					//prefix marker
					m_sPendingPrefix = GetText(tss);
				}
				else
				{
					// suffix, etc.; just write, we've done the lang attribute
					m_writer.WriteString(GetText(tss));
				}
			}
			else if (m_fDoingInterlinName)
			{
				WriteLangAndContent(GetWsFromTsString(tss), tss);
			}
			else if (m_vc.IsDoingRealWordForm)
			{
				WriteItem("txt", tss);
			}
			else if (m_vc.IsAddingSegmentReference)
			{
				WriteItem("segnum", tss);
			}
			base.AddString (tss);
		}

		public override void AddObjProp(int tag, IVwViewConstructor vc, int frag)
		{
			switch(frag)
			{
				case InterlinVc.kfragMorphType:
					m_fDoingMorphType = true;
					break;
				default:
				switch(tag)
				{
					case WfiAnalysisTags.kflidCategory:
						// <item type="pos"...AddStringAltMember will add the content...
						OpenItem("pos");
						break;
					case WfiMorphBundleTags.kflidMorph:
						OpenItem("txt");
						break;
					case WfiMorphBundleTags.kflidSense:
						OpenItem("gls");
						break;
					case WfiMorphBundleTags.kflidMsa:
						OpenItem("msa");
						m_fDoingInterlinName = true;
						break;
					default:
						break;
				}
					break;
			}
			base.AddObjProp (tag, vc, frag);

			switch(frag)
			{
				case InterlinVc.kfragStText:
					m_writer.WriteEndElement();
					break;
				case InterlinVc.kfragMorphType:
					m_fDoingMorphType = false;
					break;
				default:
				switch(tag)
				{
					case WfiAnalysisTags.kflidCategory:
					case WfiMorphBundleTags.kflidMorph:
					case WfiMorphBundleTags.kflidSense:
						CloseItem();
						break;
					case WfiMorphBundleTags.kflidMsa:
						CloseItem();
						m_fDoingInterlinName = false;
						break;
					default:
						break;
				}
					break;
			}
		}

		/// <summary>
		/// Write an item consisting of the specified string, and clear it.
		/// </summary>
		/// <param name="tss"></param>
		private void WritePendingItem(string itemType, ref ITsString tss)
		{
			if (tss == null)
				return;
			OpenItem(itemType);
			WriteLangAndContent(GetWsFromTsString(tss), tss);
			CloseItem();
			tss = null;
		}

		private void CloseItem()
		{
			m_writer.WriteEndElement();
			m_fItemIsOpen = false;
		}

		private void OpenItem(string itemType)
		{
			m_writer.WriteStartElement("item");
			m_writer.WriteAttributeString("type", itemType);
			m_fItemIsOpen = true;
		}

		/// <summary>
		/// This method (as far as I know) will be first called on the StText object, and then recursively from the
		/// base implementation for vector items in component objects.
		/// </summary>
		/// <param name="tag"></param>
		/// <param name="vc"></param>
		/// <param name="frag"></param>
		public override void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			ICmObject text = null;
			switch(frag)
			{
				case InterlinVc.kfragInterlinPara:
					m_writer.WriteStartElement("interlinear-text");
					//here the m_hvoCurr object is an StText object, store the IText owner
					//so that we can pull data from it to close out the interlinear-text element
					//Naylor 11-2011
					text = m_repoObj.GetObject(m_hvoCurr).Owner;
					if(text is IScrBook || text is IScrSection)
					{
						m_writer.WriteAttributeString("guid", m_repoObj.GetObject(m_hvoCurr).Guid.ToString());
					}
					else
					{
						m_writer.WriteAttributeString("guid", text.Guid.ToString());
					}
					foreach (var mTssPendingTitle in pendingTitles)
					{
						var hystericalRaisens = mTssPendingTitle;
						WritePendingItem("title", ref hystericalRaisens);
					}
					foreach (var mTssPendingAbbrev in pendingAbbreviations)
					{
						var hystericalRaisens = mTssPendingAbbrev;
						WritePendingItem("title-abbreviation", ref hystericalRaisens);
					}
					foreach(var source in pendingSources)
					{
						var hystericalRaisens = source;
						WritePendingItem("source", ref hystericalRaisens);
					}
					foreach (var desc in pendingComments)
					{
						var hystericalRaisens = desc;
						WritePendingItem("comment", ref hystericalRaisens);
					}
					m_writer.WriteStartElement("paragraphs");
					break;
				case InterlinVc.kfragParaSegment:
					m_writer.WriteStartElement("phrases");
					break;
				case InterlinVc.kfragBundle:
					m_writer.WriteStartElement("words");
					break;
				case InterlinVc.kfragMorphBundle:
					m_writer.WriteStartElement("morphemes");
					break;
				default:
					break;
			}
			base.AddObjVecItems (tag, vc, frag);
			switch(frag)
			{
				case InterlinVc.kfragInterlinPara:
					m_writer.WriteEndElement(); // paragraphs
					m_writer.WriteStartElement("languages");
					foreach (int wsActual in m_usedWritingSystems)
					{
						m_writer.WriteStartElement("language");
						// we don't have enough context at this point to get all the possible writing system
						// information we may encounter in the word bundles.
						string icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);
						m_writer.WriteAttributeString("lang", icuCode);
						IWritingSystem ws = m_wsManager.Get(wsActual);
						string fontName = ws.DefaultFontName;
						m_writer.WriteAttributeString("font", fontName);
						if (m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(ws))
							m_writer.WriteAttributeString("vernacular", "true");
						if (ws.RightToLeftScript)
							m_writer.WriteAttributeString("RightToLeft", "true");
						m_writer.WriteEndElement();
					}
					m_writer.WriteEndElement();	// languages
					//Media files section
					if (text != null && text is FDO.IText && ((FDO.IText)text).MediaFilesOA != null)
					{
						FDO.IText theText = (FDO.IText) text;
						m_writer.WriteStartElement("media-files");
						m_writer.WriteAttributeString("offset-type", theText.MediaFilesOA.OffsetType);
						foreach (var mediaFile in theText.MediaFilesOA.MediaURIsOC)
						{
							m_writer.WriteStartElement("media");
							m_writer.WriteAttributeString("guid", mediaFile.Guid.ToString());
							m_writer.WriteAttributeString("location", mediaFile.MediaURI);
							m_writer.WriteEndElement(); //media
						}
						m_writer.WriteEndElement(); //media-files
					}
					m_writer.WriteEndElement(); // interlinear-text

					//wipe out the pending items to be clean for next text.
					pendingTitles.Clear();
					pendingSources.Clear();
					pendingAbbreviations.Clear();
					pendingComments.Clear();
					break;
				case InterlinVc.kfragParaSegment:
				case InterlinVc.kfragBundle:
				case InterlinVc.kfragMorphBundle:
					m_writer.WriteEndElement();
					break;
				default:
					break;
			}
		}

		protected override void OpenTheObject(int hvo, int ihvo)
		{
			// NOTE: this block executes BEFORE we update CurrentObject() by calling base.OpenTheObject() below.
			int tag = CurrentPropTag;
			switch(tag)
			{
				case StTextTags.kflidParagraphs:
					// The paragraph data may need to be loaded if the paragraph has not yet
					// appeared on the screen.  See LT-7071.
					m_vc.LoadDataFor(this, new int[1] { hvo }, 1, CurrentObject(), tag, -1, ihvo);
					WriteStartParagraph(hvo);
					break;
				case WfiAnalysisTags.kflidMorphBundles:
					WriteStartMorpheme(hvo);
					m_guidMorphType = Guid.Empty;	// reset morphtype before each morph bundle.
					break;
				case StTxtParaTags.kflidSegments:
					WriteStartPhrase(hvo);
					break;
				case SegmentTags.kflidAnalyses:
					WriteStartWord(hvo);
					break;
			}
			base.OpenTheObject(hvo, ihvo);
		}

		protected virtual void WriteStartMorpheme(int hvo)
		{
			m_writer.WriteStartElement("morph");

		}

		protected virtual void WriteStartWord(int hvo)
		{
			m_writer.WriteStartElement("word");
		}

		protected virtual void WriteStartPhrase(int hvo)
		{
			m_writer.WriteStartElement("phrase");
		}

		protected virtual void WriteStartParagraph(int hvo)
		{
			m_writer.WriteStartElement("paragraph");
		}

		protected override void CloseTheObject()
		{
			base.CloseTheObject();
			switch(CurrentPropTag)
			{
				case StTextTags.kflidParagraphs:
					m_writer.WriteEndElement();
					break;
				case WfiAnalysisTags.kflidMorphBundles:
					m_writer.WriteEndElement();
					m_guidMorphType = Guid.Empty;	// reset morphtype after each morph bundle.
					break;
				case StTxtParaTags.kflidSegments:
					m_writer.WriteEndElement();
					break;
				case SegmentTags.kflidAnalyses:
					m_writer.WriteEndElement();
					break;
			}
		}

		public override void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc)
		{
			switch(tag)
			{
				case MoMorphTypeTags.kflidPrefix:
					m_sPendingPrefix = m_sda.get_UnicodeProp(m_hvoCurr, tag);
					break;
				case MoMorphTypeTags.kflidPostfix:
					m_writer.WriteString(m_sda.get_UnicodeProp(m_hvoCurr, tag));
					break;
			}
			base.AddUnicodeProp (tag, ws, _vwvc);
		}

		/// <summary>
		/// This allows the exporter to be called multiple times with different roots.
		/// </summary>
		internal void SetRootObject(ICmObject objRoot)
		{
			m_hvoCurr = objRoot.Hvo;
			SetTextTitleAndMetadata(objRoot as IStText);
		}

		/// <summary>
		/// Sets title, abbreviation, source and comment(description) data for the text.
		/// </summary>
		/// <param name="txt"></param>
		private void SetTextTitleAndMetadata(IStText txt)
		{
			if (txt == null)
				return;
			var text = txt.Owner as FDO.IText;
			if (text != null)
			{
				foreach (var writingSystemId in text.Name.AvailableWritingSystemIds)
				{
					pendingTitles.Add(text.Name.get_String(writingSystemId));
				}
				foreach (var writingSystemId in text.Abbreviation.AvailableWritingSystemIds)
				{
					pendingAbbreviations.Add(text.Abbreviation.get_String(writingSystemId));
				}
				foreach (var writingSystemId in text.Source.AvailableWritingSystemIds)
				{
					pendingSources.Add(text.Source.get_String(writingSystemId));
				}
				foreach (var writingSystemId in text.Description.AvailableWritingSystemIds)
				{
					pendingComments.Add(text.Description.get_String(writingSystemId));
				}
			}
			else if (TextSource.IsScriptureText(txt))
			{
				pendingTitles.Add(txt.ShortNameTSS);
				pendingAbbreviations.Add(null);
			}
		}
	}
	// Todo:
	// Freeforms.

	/// <summary>
	/// This handles exporting interlinear data into an xml format that is friendly to ELAN's overlapping time sequences.
	/// (LT-9904)
	/// </summary>
	public class InterlinearExporterForElan : InterlinearExporter
	{
		private const int kDocVersion = 2;
		protected internal InterlinearExporterForElan(FdoCache cache, XmlWriter writer, ICmObject objRoot,
			InterlinLineChoices lineChoices, InterlinVc vc)
			: base(cache, writer, objRoot, lineChoices, vc)
		{
		}

		public override void WriteBeginDocument()
		{
			base.WriteBeginDocument();
			m_writer.WriteAttributeString("version", kDocVersion.ToString());
		}

		protected override void WriteStartParagraph(int hvo)
		{
			base.WriteStartParagraph(hvo);
			WriteGuidAttributeForObj(hvo);
		}

		protected override void WriteStartPhrase(int hvo)
		{
			base.WriteStartPhrase(hvo);
			WriteGuidAttributeForObj(hvo);
			ISegment phrase = m_repoObj.GetObject(hvo) as ISegment;
			if(phrase != null && phrase.MediaURIRA != null)
			{
				m_writer.WriteAttributeString("begin-time-offset", phrase.BeginTimeOffset);
				m_writer.WriteAttributeString("end-time-offset", phrase.EndTimeOffset);
				if (phrase.SpeakerRA != null)
				{
					m_writer.WriteAttributeString("speaker", phrase.SpeakerRA.Name.BestVernacularAlternative.Text);
				}
				m_writer.WriteAttributeString("media-file", phrase.MediaURIRA.Guid.ToString());
			}
		}

		protected override void WriteStartWord(int hvo)
		{
			base.WriteStartWord(hvo);
			// Note that this guid may well not be unique in the file, since it refers to a
			// WfiWordform, WfiAnalysis, WfiGloss, or PunctuationForm (the last is not output),
			// any of which may be referred to repeatedly in an analyzed text.
			int clid = m_repoObj.GetClsid(hvo);
			if (clid != PunctuationFormTags.kClassId)
				WriteGuidAttributeForObj(hvo);
		}
	}
}
