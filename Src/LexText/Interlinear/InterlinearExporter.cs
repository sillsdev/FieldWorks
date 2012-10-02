using System;
using System.IO;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// InterlinearExporter is an IVwEnv implementation which exports interlinear data to an XmlWriter.
	/// Make one of these by creating an XmlTextWriter.
	/// </summary>
	public class InterlinearExporter : CollectorEnv
	{
		protected XmlWriter m_writer;
		int ktagParaSegments;
		int ktagSegmentForms;
		int m_flidStringValue;
		protected FdoCache m_cache;
		bool m_fItemIsOpen = false; // true while doing some <item> element (causes various things to output data)
		bool m_fDoingHeadword = false; // true while displaying a headword (causes some special behaviors)
		bool m_fDoingHomographNumber = false; // true after note dependency on homograph number, to end of headword.
		bool m_fDoingVariantTypes = false; // can become true after processing headword and homograph number
		bool m_fAwaitingHeadwordForm = false; // true after start of headword until we get a string alt.
		bool m_fDoingMorphType = false; // true during display of MorphType of MoForm.
		bool m_fDoingInterlinName = false; // true during MSA
		string m_sPendingPrefix; // got a prefix, need the ws from the form itself before we write it.
		string m_sFreeAnnotationType;
		ITsString m_tssPendingHomographNumber;
		ITsString m_tssPendingTitle; // these come along before the right element is open.
		ITsString m_tssPendingTitle2;
		ITsString m_tssPendingSource;
		private ITsString m_tssTitleAbbreviation;
		ITsString m_tssPendingDescription;
		InterlinLineChoices m_lineChoices;
		int m_defaultGlossVirtFlid;
		InterlinVc m_vc = null;
		Set<int> m_usedWritingSystems = new Set<int>();
		/// <summary>saves the morphtype so that glosses can be marked as pro/enclitics.  See LT-8288.</summary>
		Guid m_guidMorphType = Guid.Empty;
		IMoMorphType m_mmtEnclitic;
		IMoMorphType m_mmtProclitic;

		public static InterlinearExporter Create(string mode, FdoCache cache, XmlWriter writer, int hvoRoot,
			InterlinLineChoices lineChoices, InterlinVc vc, ITsString tssTextName, ITsString tssTextAbbreviation)
		{
			if (mode != null && mode.ToLowerInvariant() == "elan")
			{
				return new InterlinearExporterForElan(cache, writer, hvoRoot, lineChoices, vc, tssTextName,
													  tssTextAbbreviation);
			}
			else
			{
				return new InterlinearExporter(cache, writer, hvoRoot, lineChoices, vc, tssTextName, tssTextAbbreviation);
			}
		}

		protected InterlinearExporter(FdoCache cache, XmlWriter writer, int hvoRoot, InterlinLineChoices lineChoices, InterlinVc vc, ITsString tssTextName, ITsString tssTextAbbreviation)
			: base(null, cache.MainCacheAccessor, hvoRoot)
		{
			m_cache = cache;
			m_writer = writer;
			ktagParaSegments = InterlinVc.ParaSegmentTag(cache);
			ktagSegmentForms = InterlinVc.SegmentFormsTag(cache);
			m_flidStringValue = CmBaseAnnotation.StringValuePropId(cache);
			m_lineChoices = lineChoices;
			m_defaultGlossVirtFlid = BaseVirtualHandler.GetInstalledHandlerTag(m_cache, "WfiMorphBundle", "DefaultSense");
			m_vc = vc;
			m_tssPendingTitle = tssTextName;
			m_tssTitleAbbreviation = tssTextAbbreviation;

			// Get morphtype information that we need later.  (plus stuff we don't...)  See LT-8288.
			IMoMorphType mmtStem;
			IMoMorphType mmtPrefix;
			IMoMorphType mmtSuffix;
			IMoMorphType mmtInfix;
			IMoMorphType mmtBoundStem;
			IMoMorphType mmtSimulfix;
			IMoMorphType mmtSuprafix;
			MoMorphType.GetMajorMorphTypes(cache, out mmtStem, out mmtPrefix, out mmtSuffix, out mmtInfix,
				out mmtBoundStem, out m_mmtProclitic, out m_mmtEnclitic, out mmtSimulfix, out mmtSuprafix);
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
			if (tag == m_flidStringValue)
			{
				// <item type="punct">
				WriteItem(tag, "punct", 0);
			}
		}

		public override void NoteDependency(int[] _rghvo, int[] rgtag, int chvo)
		{
			if (m_fDoingHeadword && rgtag.Length == 1 && rgtag[0] == (int)LexEntry.LexEntryTags.kflidHomographNumber)
				m_fDoingHomographNumber = true;
		}

		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor _vwvc)
		{
			if (m_fDoingMorphType)
			{
				m_writer.WriteAttributeString("type", GetText(m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws)));
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
			else if (tag == (int)MoForm.MoFormTags.kflidForm)
			{
				if (m_fItemIsOpen)
				{
					WritePrefixLangAlt(ws, tag);
				}
			}
			else if (m_fItemIsOpen)
			{
				if (tag == (int)LexSense.LexSenseTags.kflidGloss || tag == m_defaultGlossVirtFlid)
					WriteLangAndContent(ws, GetMarkedGloss(m_hvoCurr, tag, ws));
				else
					WriteLangAndContent(ws, m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws));
			}
			else if (tag == (int)WfiWordform.WfiWordformTags.kflidForm)
			{
				WriteItem(tag, "txt", ws);
			}
			else if (tag == (int)WfiGloss.WfiGlossTags.kflidForm)
			{
				WriteItem(tag, "gls", ws);
			}
			else if (tag == (int)WfiMorphBundle.WfiMorphBundleTags.kflidForm)
			{
				WriteItem(tag, "txt", ws);
			}
			else if (tag == (int)CmAnnotation.CmAnnotationTags.kflidComment)
			{
				WriteItem(tag, m_sFreeAnnotationType, ws);
			}
			else if (tag == m_vc.vtagStTextTitle)
			{
				ITsString tssTitle = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
				if (m_tssPendingTitle == null)
					m_tssPendingTitle = tssTitle;
				else
					m_tssPendingTitle2 = tssTitle;
			}
			else if (tag == m_vc.vtagStTextSource)
			{
				m_tssPendingSource = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
			}
			else if (tag == (int)CmMajorObject.CmMajorObjectTags.kflidDescription)
			{
				m_tssPendingDescription = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
			}
		}

		protected Guid WriteGuidAttributeForCurrentObj()
		{
			return WriteGuidAttributeForObj(m_hvoCurr);
		}

		protected Guid WriteGuidAttributeForObj(int hvo)
		{
			if (m_cache.IsDummyObject(hvo))
			{
				return System.Guid.Empty;
			}
			Guid guid = m_cache.GetGuidFromId(hvo);
			// (note: using m_sda.get_GuidProp(m_hvoCurr, tag) caused a general database error)
			m_writer.WriteAttributeString("guid", guid.ToString());
			return guid;
		}

		/// <summary>
		/// Glosses must be marked as proclitics or enclitics.  See LT-8288.
		/// </summary>
		private ITsString GetMarkedGloss(int m_hvoCurr, int tag, int ws)
		{
			ITsString tss = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
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
					case (int)WfiAnalysis.WfiAnalysisTags.kflidCategory:
						// <item type="pos"...AddStringAltMember will add the content...
						OpenItem("pos");
						break;
					case (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph:
						OpenItem("txt");
						break;
					case (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense:
						OpenItem("gls");
						break;
					case (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa:
						OpenItem("msa");
						m_fDoingInterlinName = true;
						break;
					default:
						if (tag == m_defaultGlossVirtFlid) // treat as kflidSense
							OpenItem("gls");
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
					case (int)WfiAnalysis.WfiAnalysisTags.kflidCategory:
					case (int)WfiMorphBundle.WfiMorphBundleTags.kflidMorph:
					case (int)WfiMorphBundle.WfiMorphBundleTags.kflidSense:
						CloseItem();
						break;
					case (int)WfiMorphBundle.WfiMorphBundleTags.kflidMsa:
						CloseItem();
						m_fDoingInterlinName = false;
						break;
					default:
						if (tag == m_defaultGlossVirtFlid) // treat as kflidSense
							CloseItem();
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

		public override void AddObjVecItems(int tag, IVwViewConstructor vc, int frag)
		{
			switch(frag)
			{
				case InterlinVc.kfragInterlinPara:
					m_writer.WriteStartElement("interlinear-text");
					WritePendingItem("title", ref m_tssPendingTitle);
					if (m_tssPendingTitle2 != null && m_tssPendingTitle2.Length > 0)
						WritePendingItem("title", ref m_tssPendingTitle2);
					WritePendingItem("title-abbreviation", ref m_tssTitleAbbreviation);
					if (m_tssPendingSource != null && m_tssPendingSource.Length > 0)
						WritePendingItem("source", ref m_tssPendingSource);
					WritePendingItem("description", ref m_tssPendingDescription);
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
						string fontName = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsActual).DefaultSerif;
						m_writer.WriteAttributeString("font", fontName);
						if (m_cache.LangProject.VernWssRC.Contains(wsActual))
						{
							m_writer.WriteAttributeString("vernacular", "true");
						}
						IWritingSystem wsObj = m_cache.LanguageWritingSystemFactoryAccessor.get_EngineOrNull(wsActual);
						if (wsObj != null && wsObj.RightToLeft)
							m_writer.WriteAttributeString("RightToLeft", "true");
						m_writer.WriteEndElement();
					}
					m_writer.WriteEndElement();	// languages
					m_writer.WriteEndElement(); // interlinear-text
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
				case (int)StText.StTextTags.kflidParagraphs:
					// The paragraph data may need to be loaded if the paragraph has not yet
					// appeared on the screen.  See LT-7071.
					m_vc.LoadDataFor(this, new int[1] { hvo }, 1, CurrentObject(), tag, -1, ihvo);
					WriteStartParagraph(hvo);
					break;
				case (int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles:
					m_writer.WriteStartElement("morph");
					m_guidMorphType = Guid.Empty;	// reset morphtype before each morph bundle.
					break;
				default:
					if (tag == ktagParaSegments)
					{
						EnsureSegmentCbaIsRealIfNeeded(ref hvo);
						WriteStartPhrase(hvo);
					}
					else if (tag == ktagSegmentForms)
					{
						EnsureXficCbaIsRealIfNeeded(ref hvo);
						WriteStartWord(hvo);
					}
					break;
			}
			base.OpenTheObject(hvo, ihvo);
		}

		protected virtual void EnsureSegmentCbaIsRealIfNeeded(ref int hvo)
		{
			// subclasses can override.
		}


		protected virtual void EnsureXficCbaIsRealIfNeeded(ref int hvo)
		{
			// subclasses can override.
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
				case (int)StText.StTextTags.kflidParagraphs:
					m_writer.WriteEndElement();
					break;
				case (int)WfiAnalysis.WfiAnalysisTags.kflidMorphBundles:
					m_writer.WriteEndElement();
					m_guidMorphType = Guid.Empty;	// reset morphtype after each morph bundle.
					break;
				default:
					if (CurrentPropTag == ktagParaSegments)
						m_writer.WriteEndElement();
					else if (CurrentPropTag == ktagSegmentForms)
						m_writer.WriteEndElement();
					break;
			}
		}

		public override void AddUnicodeProp(int tag, int ws, IVwViewConstructor _vwvc)
		{
			switch(tag)
			{
				case (int)MoMorphType.MoMorphTypeTags.kflidPrefix:
					m_sPendingPrefix = m_sda.get_UnicodeProp(m_hvoCurr, tag);
					break;
				case (int)MoMorphType.MoMorphTypeTags.kflidPostfix:
					m_writer.WriteString(m_sda.get_UnicodeProp(m_hvoCurr, tag));
					break;
			}
			base.AddUnicodeProp (tag, ws, _vwvc);
		}

		/// <summary>
		/// This allows the exporter to be called multiple times with different roots.
		/// </summary>
		/// <param name="hvoRoot"></param>
		internal void SetRootObject(int hvoRoot)
		{
			m_hvoCurr = hvoRoot;
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
		private const int kDocVersion = 1;
		private int kPunctuationAnnType = 0;
		protected internal InterlinearExporterForElan(FdoCache cache, XmlWriter writer, int hvoRoot, InterlinLineChoices lineChoices, InterlinVc vc, ITsString tssTextName, ITsString tssTextAbbreviation)
			: base(cache, writer, hvoRoot, lineChoices, vc, tssTextName, tssTextAbbreviation)
		{
			kPunctuationAnnType = CmAnnotationDefn.Punctuation(cache).Hvo;
		}

		protected override void EnsureSegmentCbaIsRealIfNeeded(ref int hvo)
		{
			EnsureCbaIsReal(ref hvo);
			base.EnsureSegmentCbaIsRealIfNeeded(ref hvo);
		}

		protected override void EnsureXficCbaIsRealIfNeeded(ref int hvo)
		{
			ICmBaseAnnotation cba = CmBaseAnnotation.CreateFromDBObject(m_cache, hvo);
			// avoid converting dummy punctuations, since we don't always have guids for those
			// because our dummy conversion process does not update the cache for punctuation types.
			if (cba.AnnotationTypeRAHvo != kPunctuationAnnType)
				EnsureCbaIsReal(ref hvo);
			base.EnsureXficCbaIsRealIfNeeded(ref hvo);
		}

		private void EnsureCbaIsReal(ref int hvo)
		{
			ICmBaseAnnotation cba = CmBaseAnnotation.CreateFromDBObject(m_cache, hvo);
			if (cba != null && cba.IsDummyObject)
				hvo = CmBaseAnnotation.ConvertBaseAnnotationToReal(m_cache, hvo).Hvo;
		}

		public override void WriteBeginDocument()
		{
			base.WriteBeginDocument();
			m_writer.WriteAttributeString("exportTarget", "elan");
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
		}

		protected override void WriteStartWord(int hvo)
		{
			base.WriteStartWord(hvo);
			ICmBaseAnnotation cba = CmBaseAnnotation.CreateFromDBObject(m_cache, hvo);
			// avoid writing guids for punctuations, since some may be dummy annotations.
			if (cba.AnnotationTypeRAHvo != kPunctuationAnnType)
				WriteGuidAttributeForObj(hvo);
		}
	}
}
