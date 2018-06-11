// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	/// <summary>
	/// InterlinearExporter is an IVwEnv implementation which exports interlinear data to an XmlWriter.
	/// Make one of these by creating an XmlTextWriter.
	/// </summary>
	internal class InterlinearExporter : CollectorEnv
	{
		protected XmlWriter m_writer;
		protected LcmCache m_cache;
		bool m_fItemIsOpen; // true while doing some <item> element (causes various things to output data)
		bool m_fDoingHeadword; // true while displaying a headword (causes some special behaviors)
		bool m_fDoingHomographNumber; // true after note dependency on homograph number, to end of headword.
		bool m_fDoingVariantTypes; // can become true after processing headword and homograph number
		bool m_fAwaitingHeadwordForm; // true after start of headword until we get a string alt.
		bool m_fDoingMorphType; // true during display of MorphType of MoForm.
		bool m_fDoingInterlinName; // true during MSA
		bool m_fDoingGlossPrepend; // true after special AddProp
		bool m_fDoingGlossAppend; // true after special AddProp
		string m_sPendingPrefix; // got a prefix, need the ws from the form itself before we write it.
		string m_sFreeAnnotationType;
		ITsString m_tssPendingHomographNumber;
		List<ITsString> pendingTitles = new List<ITsString>(); // these come along before the right element is open.
		List<ITsString> pendingSources = new List<ITsString>();
		private List<ITsString> pendingAbbreviations = new List<ITsString>();
		List<ITsString> pendingComments = new List<ITsString>();
		int m_flidStTextTitle;
		int m_flidStTextSource;
		InterlinVc m_vc;
		private readonly HashSet<int> m_usedWritingSystems = new HashSet<int>();
		/// <summary>saves the morphtype so that glosses can be marked as pro/enclitics.  See LT-8288.</summary>
		Guid m_guidMorphType = Guid.Empty;
		IMoMorphType m_mmtEnclitic;
		IMoMorphType m_mmtProclitic;
		protected WritingSystemManager m_wsManager;
		protected ICmObjectRepository m_repoObj;

		public static InterlinearExporter Create(string mode, LcmCache cache, XmlWriter writer, ICmObject objRoot, InterlinLineChoices lineChoices, InterlinVc vc)
		{
			return mode != null && mode.ToLowerInvariant() == "elan"
				? new InterlinearExporterForElan(cache, writer, objRoot, lineChoices, vc)
				: new InterlinearExporter(cache, writer, objRoot, lineChoices, vc);
		}

		protected InterlinearExporter(LcmCache cache, XmlWriter writer, ICmObject objRoot, InterlinLineChoices lineChoices, InterlinVc vc)
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
			m_vc.Display(this, OpenObject, InterlinVc.kfragStText);
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

		public override void AddStringProp(int tag, IVwViewConstructor vwvc)
		{
			if (tag == PunctuationFormTags.kflidForm)
			{
				// <item type="punct">
				WriteItem(tag, "punct", 0);
			}
			else if (m_cache.GetManagedMetaDataCache().IsCustom(tag))
			{
				// custom fields are not multi-strings, so pass 0 for the ws
				WriteItem(tag, m_cache.MetaDataCacheAccessor.GetFieldName(tag), 0);
			}
		}

		public override void NoteDependency(int[] rghvo, int[] rgtag, int chvo)
		{
			if (m_fDoingHeadword && rgtag.Length == 1 && rgtag[0] == LexEntryTags.kflidHomographNumber)
			{
				m_fDoingHomographNumber = true;
			}
		}

		public override void AddStringAltMember(int tag, int ws, IVwViewConstructor vwvc)
		{
			//get the writing system for english for use in writing out the morphType, the types will ALWAYS be defined in english
			//but they may not be defined in other languages, using English on import and export should garauntee the correct behavior
			var englishWS = m_cache.WritingSystemFactory.GetWsFromStr("en");
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
				WriteLangAndContent(ws, tag == LexSenseTags.kflidGloss
						? GetMarkedGloss(m_hvoCurr, tag, ws)
						: m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws));
			}
			else switch (tag)
			{
				case WfiWordformTags.kflidForm:
					WriteItem(tag, "txt", ws);
					break;
				case WfiGlossTags.kflidForm:
					WriteItem(tag, "gls", ws);
					break;
				case WfiMorphBundleTags.kflidForm:
					WriteItem(tag, "txt", ws);
					break;
				case SegmentTags.kflidFreeTranslation:
				case SegmentTags.kflidLiteralTranslation:
				case NoteTags.kflidContent:
					WriteItem(tag, m_sFreeAnnotationType, ws);
					break;
				default:
					if (tag == m_flidStTextTitle)
					{
						var tssTitle = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
						if (!pendingTitles.Contains(tssTitle))
						{
							pendingTitles.Add(tssTitle);
						}
					}
					else if (tag == m_flidStTextSource)
					{
						var source = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
						if (!pendingSources.Contains(source))
						{
							pendingSources.Add(source);
						}
					}
					else if (tag == CmMajorObjectTags.kflidDescription)
					{
						var comment = m_sda.get_MultiStringAlt(m_hvoCurr, tag, ws);
						if (!pendingComments.Contains(comment))
						{
							pendingComments.Add(comment);
						}
					}
					else
					{
						Debug.WriteLine("Export.AddStringAltMember(hvo={0}, tag={1}, ws={2})", m_hvoCurr, tag, ws);
					}

					break;
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
				var obj = m_repoObj.GetObject(hvo);
				var guid = obj.Guid;
				m_writer.WriteAttributeString("guid", guid.ToString());
				return guid;
			}
				return Guid.Empty;
		}

		/// <summary>
		/// Glosses must be marked as proclitics or enclitics.  See LT-8288.
		/// </summary>
		private ITsString GetMarkedGloss(int hvo, int tag, int ws)
		{
			var tss = m_sda.get_MultiStringAlt(hvo, tag, ws);
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

			if (sPrefix == null && sPostfix == null)
			{
				return tss;
			}
			var tsb = tss.GetBldr();
			if (!string.IsNullOrEmpty(sPrefix))
			{
					tsb.Replace(0, 0, sPrefix, null);
			}

			if (!string.IsNullOrEmpty(sPostfix))
			{
					tsb.Replace(tsb.Length, tsb.Length, sPostfix, null);
			}
			tss = tsb.GetString();
			return tss;
		}

		private void WritePrefixLangAlt(int ws, int tag)
		{
			var icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
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
			var ws = alt;
			var tss = ws == 0 ? m_sda.get_StringProp(m_hvoCurr, tag) : m_sda.get_MultiStringAlt(m_hvoCurr, tag, alt);
			WriteItem(itemType, tss);
		}

		private void WriteItem(string itemType, ITsString tss)
		{
			m_writer.WriteStartElement("item");
			m_writer.WriteAttributeString("type", itemType);
			var ws = GetWsFromTsString(tss);
			WriteLangAndContent(ws, tss);
			m_writer.WriteEndElement();
		}

		private static int GetWsFromTsString(ITsString tss)
		{
			var ttp = tss.get_PropertiesAt(0);
			int var;
			return ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out var);
		}

		/// <summary>
		/// Write a lang attribute identifying the string, then its content as the body of
		/// an element.
		/// </summary>
		private void WriteLangAndContent(int ws, ITsString tss)
		{
			UpdateWsList(ws);
			var icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(ws);
			m_writer.WriteAttributeString("lang", icuCode);
			m_writer.WriteString(GetText(tss));
		}

		private void UpdateWsList(int ws)
		{
			// only add valid actual ws
			if (ws > 0)
			{
				m_usedWritingSystems.Add(ws);
			}
		}

		private static string GetText(ITsString tss)
		{
			var result = tss.Text;
			return result?.Normalize() ?? string.Empty;
		}

		public override void AddObj(int hvoItem, IVwViewConstructor vc, int frag)
		{
			switch (frag)
			{
				case (int)LcmUi.VcFrags.kfragHeadWord:
					// In the course of this AddObj, typically we get AddString calls for
					// morpheme separators, AddObjProp for kflidLexemeForm,
					// AddStringAltMember for the form of the LF, NoteDependency for a homograph number,
					// and possibly AddString for the HN itself. We want to produce something like
					// <item type="cf" lang="xkal">-de<hn lang="en">2</hn></item>
					OpenItem("cf");
					m_fDoingHeadword = true;
					m_fAwaitingHeadwordForm = true;
					break;
				case LcmUi.LexEntryVc.kfragVariantTypes:
					m_fDoingVariantTypes = true;
					OpenItem("variantTypes");
					var icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(m_cache.DefaultAnalWs);
					m_writer.WriteAttributeString("lang", icuCode);
					break;
			}
			// (LT-9374) Export Variant Type information for variants
			if (vc is InterlinVc && frag >= InterlinVc.kfragLineChoices && frag < InterlinVc.kfragLineChoices + ((InterlinVc)vc).LineChoices.Count)
			{
				var spec = ((InterlinVc)vc).LineChoices[frag - InterlinVc.kfragLineChoices];
				if (spec.Flid == InterlinLineChoices.kflidLexGloss)
				{
					OpenItem("gls");
				}
			}
			base.AddObj (hvoItem, vc, frag);
			switch (frag)
			{
				case (int)LcmUi.VcFrags.kfragHeadWord:
					CloseItem();
					m_fDoingHeadword = false;
					m_fDoingHomographNumber = false;
					WritePendingItem("hn", ref m_tssPendingHomographNumber);
					break;
				case LcmUi.LexEntryVc.kfragVariantTypes:
					CloseItem();
					m_fDoingVariantTypes = false;
					break;
			}

			if (!(vc is InterlinVc) || frag < InterlinVc.kfragLineChoices || frag >= InterlinVc.kfragLineChoices + ((InterlinVc)vc).LineChoices.Count)
			{
				return;
			}
			var spec2 = ((InterlinVc)vc).LineChoices[frag - InterlinVc.kfragLineChoices];
			if (spec2.Flid == InterlinLineChoices.kflidLexGloss)
			{
				CloseItem();
			}
		}

		public override void AddProp(int tag, IVwViewConstructor vc, int frag)
		{
			if (tag == InterlinVc.ktagGlossPrepend)
			{
				m_fDoingGlossPrepend = true;
			}
			if (tag == InterlinVc.ktagGlossAppend)
			{
				m_fDoingGlossAppend = true;
			}
			base.AddProp(tag, vc, frag);
			if (tag == InterlinVc.ktagGlossPrepend)
			{
				m_fDoingGlossPrepend = false;
			}
			if (tag == InterlinVc.ktagGlossAppend)
			{
				m_fDoingGlossAppend = false;
			}
		}

		public override void AddTsString(ITsString tss)
		{
			if (m_fDoingGlossAppend)
			{
				WriteItem("glsAppend", tss);
			}
			else if (m_fDoingGlossPrepend)
			{
				WriteItem("glsPrepend", tss);
			}
			base.AddTsString(tss);
		}

		public override void AddString(ITsString tss)
		{
			// Ignore directionality markers on export.
			if (tss.Text == "\x200F" || tss.Text == "\x200E")
			{
				return;
			}

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
		private void WritePendingItem(string itemType, ref ITsString tss)
		{
			if (tss == null)
			{
				return;
			}
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
					foreach (var wsActual in m_usedWritingSystems)
					{
						m_writer.WriteStartElement("language");
						// we don't have enough context at this point to get all the possible writing system
						// information we may encounter in the word bundles.
						var icuCode = m_cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsActual);
						m_writer.WriteAttributeString("lang", icuCode);
						var ws = m_wsManager.Get(wsActual);
						var fontName = ws.DefaultFontName;
						m_writer.WriteAttributeString("font", fontName);
						if (m_cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Contains(ws))
						{
							m_writer.WriteAttributeString("vernacular", "true");
						}

						if (ws.RightToLeftScript)
						{
							m_writer.WriteAttributeString("RightToLeft", "true");
						}
						m_writer.WriteEndElement();
					}
					m_writer.WriteEndElement();	// languages
					//Media files section
					if ((text as IText)?.MediaFilesOA != null)
					{
						var theText = (IText)text;
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
			var tag = CurrentPropTag;
			switch(tag)
			{
				case StTextTags.kflidParagraphs:
					// The paragraph data may need to be loaded if the paragraph has not yet
					// appeared on the screen.  See LT-7071.
					m_vc.LoadDataFor(this, new[] { hvo }, 1, CurrentObject(), tag, -1, ihvo);
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

		public override void AddUnicodeProp(int tag, int ws, IVwViewConstructor vwvc)
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
			base.AddUnicodeProp (tag, ws, vwvc);
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
		private void SetTextTitleAndMetadata(IStText txt)
		{
			if (txt == null)
			{
				return;
			}
			var text = txt.Owner as IText;
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
}
