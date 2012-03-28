// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LiftExporter.cs
// Responsibility: mcconnel
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using Palaso.WritingSystems;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using System.Text;
using SIL.FieldWorks.FDO.Application.ApplicationServices;
using System.Windows.Forms;

namespace SIL.FieldWorks.LexText.Controls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Export the lexicon as a LIFT file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class LiftExporter
	{
		/// <summary></summary>
		public delegate void ProgressHandler(object sender);

		/// <summary>
		/// Notify the progress dialog (if there is one) to update the progress bar.
		/// </summary>
		public event ProgressHandler UpdateProgress;

		/// <summary>
		/// Notify the progress dialog (if there is one) to update the progress message and the
		/// maximum limit of the progress bar.
		/// </summary>
		public event EventHandler<ProgressMessageArgs> SetProgressMessage;

		private readonly FdoCache m_cache;
		private readonly IFwMetaDataCacheManaged m_mdc;
		private readonly IWritingSystemManager m_wsManager;
		private readonly int m_wsEn;
		private readonly int m_wsBestAnalVern;
		private Dictionary<Guid, String> m_CmPossibilityListsReferencedByFields = new Dictionary<Guid, String>();
		private Dictionary<Guid, String> m_ListsGuidToRangeName = new Dictionary<Guid, String>();
		private readonly ICmPossibilityListRepository m_repoCmPossibilityLists;
		private readonly ISilDataAccessManaged m_sda;

		public LiftExporter(FdoCache cache)
		{
			m_cache = cache;
			m_mdc = cache.MetaDataCacheAccessor as IFwMetaDataCacheManaged;
			Debug.Assert(m_mdc != null);
			m_sda = m_cache.DomainDataByFlid as ISilDataAccessManaged;
			Debug.Assert(m_sda != null);
			m_repoCmPossibilityLists = m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();

			m_wsEn = cache.WritingSystemFactory.GetWsFromStr("en");
			if (m_wsEn == 0)
				m_wsEn = cache.DefaultUserWs;
			m_wsManager = cache.ServiceLocator.WritingSystemManager;
			m_wsBestAnalVern = (int)SpecialWritingSystemCodes.BestAnalysisOrVernacular;
		}

		/// <summary>
		/// Flag whether or not to export external files along with the lexical data.
		/// </summary>
		public bool ExportPicturesAndMedia { get; set; }

		/// <summary>
		/// Path to the folder where the output is being written. Referenced files are copied to subfolders within this,
		/// if ExportPicturesAndMedia is true.
		/// </summary>
		private string FolderPath { get; set; }

		/// <summary>
		/// Export without pictures (mainly for testing).
		/// </summary>
		/// <param name="w"></param>
		public void ExportLift(TextWriter w)
		{
			ExportPicturesAndMedia = false;
			ExportLift(w, null);
		}

		/// <summary>
		/// Export the lexicon.
		/// </summary>
		public void ExportLift(TextWriter w, string folderPath)
		{
			var repoLexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			ExportLift(w, folderPath, repoLexEntry.AllInstances(), repoLexEntry.Count);
		}

		private void ExportLift(TextWriter w, string folderPath,
			IEnumerable<ILexEntry> entries, int cEntries)
		{
			FolderPath = folderPath;
			if (SetProgressMessage != null)
			{
				var ma = new ProgressMessageArgs { Max = cEntries, MessageId = "ksExportingLift" };
				SetProgressMessage(this, ma);
			}

			// pre-emtively delete the audio folder so files of deleted/changed references
			// won't be orphaned
			if (Directory.Exists(Path.Combine(FolderPath,"audio")))
				Directory.Delete(Path.Combine(FolderPath, "audio"), true);

			w.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\" ?>");
			w.WriteLine("<!-- See http://code.google.com/p/lift-standard for more information on the format used here. -->");
			w.WriteLine("<lift producer=\"SIL.FLEx {0}\" version=\"0.13\">", GetVersion());

			//Determine which custom fields ones point to a CmPossibility List (either custom or standard).
			//Determine which ones point to a custom CmPossibility List and make sure we output that list as a range.
			//Also if a List is not referred to by standard fields, it might not be output as a range, so if the List
			//is referenced by a custom field, then that List does need to be output to the LIFT ranges file.
			m_CmPossibilityListsReferencedByFields = GetCmPossibilityListsReferencedByFields(entries);
			MapCmPossibilityListGuidsToLiftRangeNames(m_CmPossibilityListsReferencedByFields);

			WriteHeaderInformation(w);
			foreach (var entry in entries)
			{
				WriteLiftEntry(w, entry);
				if (UpdateProgress != null)
					UpdateProgress(this);
			}
			w.WriteLine("</lift>");
			w.Flush();

			ExportWsAsLdml(Path.Combine(folderPath, "WritingSystems"));
		}

		/// <summary>
		/// Export the lexicon entries filtered into the list given by flid with relation to
		/// the LexDb object.
		/// </summary>
		public void ExportLift(TextWriter w, string folderPath, ISilDataAccess sda, int flid)
		{
			var hvoObject = m_cache.LangProject.LexDbOA.Hvo;
			var chvo = sda.get_VecSize(hvoObject, flid);
			int[] contents;
			using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				sda.VecProp(hvoObject, flid, chvo, out chvo, arrayPtr);
				contents = MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}
			var entries = FilterVirtualFlidVector(contents);
			ExportLift(w, folderPath, entries, entries.Count);
		}

		/// <summary>
		/// A filtered browseview might give us something other than a simple nonredundant
		/// list of the desired objects.  (Checking the class may be paranoid overkill,
		/// however.)
		/// </summary>
		/// <remarks>This supports limiting export by filtering.  See FWR-1223.</remarks>
		private List<ILexEntry> FilterVirtualFlidVector(IList<int> rghvo)
		{
			var sethvoT = new HashSet<int>();
			var rglex = new List<ILexEntry>();
			var repo = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			for (var ihvo = 0; ihvo < rghvo.Count; ++ihvo)
			{
				var hvoT = rghvo[ihvo];
				var obj = repo.GetObject(hvoT);
				if (obj.ClassID != LexEntryTags.kClassId)
				{
					var objT = obj.OwnerOfClass(LexEntryTags.kClassId);
					if (objT == null)
						continue;
					obj = objT;
					hvoT = obj.Hvo;
				}
				if (sethvoT.Contains(hvoT))
					continue;
				sethvoT.Add(hvoT);
				Debug.Assert(obj is ILexEntry);
				rglex.Add(obj as ILexEntry);
			}
			return rglex;
		}

		private void ExportWsAsLdml(string sDirectory)
		{
			if (!Directory.Exists(sDirectory))
				Directory.CreateDirectory(sDirectory);

			var wss = new HashSet<IWritingSystem>(m_cache.ServiceLocator.WritingSystems.AllWritingSystems);
			wss.UnionWith(from index in m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>().AllInstances()
						  where !string.IsNullOrEmpty(index.WritingSystem)
						  select m_cache.ServiceLocator.WritingSystemManager.Get(index.WritingSystem));

			var writerSettings = new XmlWriterSettings
			{
				Indent = true,
				IndentChars = "\t",
				NewLineHandling = NewLineHandling.None
			};
			foreach (var ws in wss)
			{
				using (var writer = XmlWriter.Create(Path.Combine(sDirectory, ws.Id + ".ldml"), writerSettings))
				{
					ws.WriteLdml(writer);
					writer.Close();
				}
			}
		}

		private void WriteLiftEntry(TextWriter w, ILexEntry entry)
		{
			var dateCreated = entry.DateCreated.ToUniversalTime().ToString("yyyy-MM-ddTHH':'mm':'ssZ");
			var dateModified = entry.DateModified.ToUniversalTime().ToString("yyyy-MM-ddTHH':'mm':'ssZ");
			var sGuid = entry.Guid.ToString();
			var sId = XmlUtils.MakeSafeXmlAttribute(entry.LIFTid);
			if (entry.HomographNumber != 0)
			{
				w.WriteLine("<entry dateCreated=\"{0}\" dateModified=\"{1}\" id=\"{2}\" guid=\"{3}\" order=\"{4}\">",
					dateCreated, dateModified, sId, sGuid, entry.HomographNumber);
			}
			else
			{
				w.WriteLine("<entry dateCreated=\"{0}\" dateModified=\"{1}\" id=\"{2}\" guid=\"{3}\">",
					dateCreated, dateModified, sId, sGuid);
			}
			if (entry.LexemeFormOA != null)
			{
				WriteAllFormsWithMarkers(w, "lexical-unit", null, "form", entry.LexemeFormOA);
				if (entry.LexemeFormOA.MorphTypeRA != null)
					WriteTrait(w, RangeNames.sDbMorphTypesOA, entry.LexemeFormOA.MorphTypeRA.Name, m_wsEn);
			}
			WriteAllForms(w, "citation", null, "form", entry.CitationForm);
			WriteAllForms(w, "note", "type=\"bibliography\"", "form", entry.Bibliography);
			WriteAllForms(w, "note", null, "form", entry.Comment);
			WriteAllForms(w, "field", "type=\"literal-meaning\"", "form", entry.LiteralMeaning);
			WriteAllForms(w, "note", "type=\"restrictions\"", "form", entry.Restrictions);
			WriteAllForms(w, "field", "type=\"summary-definition\"", "form", entry.SummaryDefinition);
			WriteString(w, "field", "type=\"import-residue\"", "form", entry.ImportResidue);
			foreach (var alt in entry.AlternateFormsOS)
				WriteAlternateForm(w, alt);
			if (entry.EtymologyOA != null)
				WriteEtymology(w, entry.EtymologyOA);
			foreach (var er in entry.EntryRefsOS)
				WriteLexEntryRef(w, er);
			foreach (var ler in entry.LexEntryReferences)
				WriteLexReference(w, ler, entry);
			if (entry.DoNotUseForParsing)
				w.WriteLine("<trait name=\"DoNotUseForParsing\" value=\"true\"/>");
			//<booleanElement name="ExcludeAsHeadword" simpleProperty="ExcludeAsHeadword" optional="true" writeAsTrait="true"/>
			foreach (var pron in entry.PronunciationsOS)
				WritePronunciation(w, pron);
			WriteCustomFields(w, entry);
			WriteLiftResidue(w, entry);
			foreach (var sense in entry.SensesOS)
				WriteLexSense(w, sense, entry.SensesOS.Count > 1);
			w.WriteLine("</entry>");
		}

		/// <summary>
		/// Write the custom field elements for all the custom fields of the given class.
		/// </summary>
		private void WriteCustomFields(TextWriter w, ICmObject obj)
		{
			foreach (var flid in m_mdc.GetFields(obj.ClassID, true, (int)CellarPropertyTypeFilter.All))
			{
				if (!m_mdc.IsCustom(flid))
					continue;
				var fieldName = m_mdc.GetFieldName(flid);
				var type = (CellarPropertyType)m_mdc.GetFieldType(flid);
				int ws;
				ITsString tssString;
				String sLang;
				switch (type)
				{
					case CellarPropertyType.MultiUnicode:
						//<field type=\"CustomField2\">
						//    <form lang=\"en\"><text>MultiString Analysis ws string</text></form>
						//    <form lang=\"fr\"><text>MultiString Vernacular ws string</text></form>
						//</field>
						var tssMultiString = m_cache.DomainDataByFlid.get_MultiStringProp(obj.Hvo, flid);
						if (tssMultiString.StringCount > 0)
						{
							w.WriteLine("<field type=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(fieldName));
							for (var i = 0; i < tssMultiString.StringCount; ++i)
							{
								tssString = tssMultiString.GetStringFromIndex(i, out ws);
								if (IsVoiceWritingSystem(ws))
								{
									// The alternative contains a file path. We need to adjust and export and copy the file.
									var internalPath = tssString.Text;
									// usually this will be unchanged, but it is pathologically possible that the file name conflicts.
									var exportedForm = ExportFile(internalPath,
										Path.Combine(DirectoryFinder.GetMediaDir(m_cache.LangProject.LinkedFilesRootDir), internalPath),
										"audio");
									if (internalPath != exportedForm)
										tssString = m_cache.TsStrFactory.MakeString(exportedForm, ws);
								}
								WriteFormElement(w, ws, tssString);
							}
							w.WriteLine("</field>");
						}
						break;
					case CellarPropertyType.MultiBigString:
					case CellarPropertyType.MultiBigUnicode:
					case CellarPropertyType.MultiString:
						break;
					case CellarPropertyType.ReferenceAtomic:
						{
							var possibilityHvo = m_cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
							if (possibilityHvo != 0)
							{
								WritePossibilityLiftTrait(fieldName, w, possibilityHvo);
							}
						}
						break;
					case CellarPropertyType.ReferenceCollection:
						var hvos = m_sda.VecProp(obj.Hvo, flid);
						foreach (var hvo in hvos)
						{
							WritePossibilityLiftTrait(fieldName, w, hvo);
						}
						break;
					case CellarPropertyType.String:
						//<field type=\"CustomField1\">
						//<form lang=\"en\">
						//    <text>CustomField1text.</text>
						//</form>
						//</field>
						tssString = m_cache.DomainDataByFlid.get_StringProp(obj.Hvo, flid);
						if (!String.IsNullOrEmpty(tssString.Text))
						{
							w.WriteLine("<field type=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(fieldName));
							ws = tssString.get_WritingSystem(0);
							WriteFormElement(w, ws, tssString);
							w.WriteLine("</field>");
						}
						break;
					case CellarPropertyType.GenDate:
						WriteCustomGenDate(obj, flid, fieldName, w);
						break;
					case CellarPropertyType.Integer:
						//<trait name="CustomField2-LexSense Integer" value="5"></trait>
						var intVal = m_cache.DomainDataByFlid.get_IntProp(obj.Hvo, flid);
						if (intVal != 0)
						{
							var str = String.Format("<trait name=\"{0}\" value=\"{1}\"/>", XmlUtils.MakeSafeXmlAttribute(fieldName),
												XmlUtils.MakeSafeXmlAttribute(intVal.ToString()));
							w.WriteLine(str);
						}

						break;
					case CellarPropertyType.OwningAtomic:
						var clidDest = m_mdc.GetDstClsId(flid);
						if (clidDest == StTextTags.kClassId)
						{
							var hvoText = m_cache.DomainDataByFlid.get_ObjectProp(obj.Hvo, flid);
							if (hvoText > 0)
							{
								var text = m_cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(hvoText);
								if (text.ParagraphsOS.Count > 0)
								{
									WriteCustomStText(w, fieldName, text.ParagraphsOS);
								}
							}
						}
						break;
					default:
						break;
				}

			}
		}

		private void WriteCustomStText(TextWriter w, string fieldName, IEnumerable<IStPara> paras)
		{
			//If there are no paragraphs then do nothing.
			var para1 = paras.OfType<IStTxtPara>().FirstOrDefault();
			if (para1 == null)
				return;
			//We don't want to output anything if the paragraphs have no content.
			var allParasAreEmpty = true;
			foreach (var para in paras.OfType<IStTxtPara>())
			{
				if (para.Contents.Text != null)
				{
					allParasAreEmpty = false;
					break;
				}
			}
			if (allParasAreEmpty)
				return;
			w.WriteLine("<field type=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(fieldName));
			var ws = TsStringUtils.GetWsOfRun(para1.Contents, 0);
			var sLang = m_cache.WritingSystemFactory.GetStrFromWs(ws);
			w.Write("<form lang=\"{0}\"><text>", XmlUtils.MakeSafeXmlAttribute(sLang));
			var fFirstPara = true;
			foreach (var para in paras.OfType<IStTxtPara>())
			{
				if (fFirstPara)
					fFirstPara = false;
				else
					w.Write("\u2029");	// flag end of preceding paragraph with 'Paragraph Separator'.
				if (!String.IsNullOrEmpty(para.StyleName))
					w.Write("<span class=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(para.StyleName));
				WriteTsStringContent(w, para.Contents);
				if (!String.IsNullOrEmpty(para.StyleName))
					w.Write("</span>");
			}
			w.WriteLine("</text></form>");
			w.WriteLine("</field>");
		}

		private void WriteTsStringContent(TextWriter w, ITsString tss)
		{
			for (var irun = 0; irun < tss.RunCount; ++irun)
			{
				var ttp = tss.get_Properties(irun);
				int nvar;
				var ws = ttp.GetIntPropValues((int)FwTextPropType.ktptWs, out nvar);
				var sLang = m_cache.WritingSystemFactory.GetStrFromWs(ws);
				var style = ttp.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
				w.Write("<span lang=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sLang));
				if (!String.IsNullOrEmpty(style))
					w.Write(" class=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(style));
				w.Write(">{0}</span>", tss.get_RunText(irun));
			}
		}

		private void WritePossibilityLiftTrait(string labelName, TextWriter w, int possibilityHvo)
		{
			if (possibilityHvo == 0)
				return;
			var tss = GetPossibilityBestAlternative(possibilityHvo, m_cache);
			var str = String.Format("<trait name=\"{0}\" value=\"{1}\"/>", XmlUtils.MakeSafeXmlAttribute(labelName),
									XmlUtils.MakeSafeXmlAttribute(tss));
			w.WriteLine(str);
		}

		public static String GetPossibilityBestAlternative(int possibilityHvo, FdoCache cache)
		{
			ITsMultiString tsm =
				cache.DomainDataByFlid.get_MultiStringProp(possibilityHvo, CmPossibilityTags.kflidName);
			var str = BestAlternative(tsm as IMultiAccessorBase, cache.DefaultUserWs);
			return str;
		}

		private void WriteCustomGenDate(ICmObject obj, int flid, string fieldName, TextWriter w)
		{
			var genDate = m_sda.get_GenDateProp(obj.Hvo, flid);
			if (!genDate.IsEmpty)
			{
				var genDateAttr = GetGenDateAttribute(genDate);
				var str =
					String.Format(
						"<trait name=\"{0}\" value=\"{1}\"/>",
						XmlUtils.MakeSafeXmlAttribute(fieldName),
						XmlUtils.MakeSafeXmlAttribute(genDateAttr));
				w.WriteLine(str);
			}
		}

		public static string GetGenDateAttribute(GenDate dataProperty)
		{
			var genDateStr = "0";
			if (!dataProperty.IsEmpty)
			{
				genDateStr = string.Format("{0}{1:0000}{2:00}{3:00}{4}", dataProperty.IsAD ? "" : "-", dataProperty.Year,
					dataProperty.Month, dataProperty.Day, (int)dataProperty.Precision);
			}
			return genDateStr;
		}

		/// <summary>
		/// Given its integer representation, return a GenDate object.
		/// </summary>
		public static GenDate GetGenDateFromInt(int nVal)
		{
			var fAD = true;
			if (nVal < 0)
			{
				fAD = false;
				nVal = -nVal;
			}
			var prec = nVal % 10;
			nVal /= 10;
			var day = nVal % 100;
			nVal /= 100;
			var month = nVal % 100;
			var year = nVal / 100;
			return new GenDate((GenDate.PrecisionType)prec, month, day, year, fAD);
		}

		private void WriteFormElement(TextWriter w, int ws, ITsString tss)
		{
			var sLang = m_cache.WritingSystemFactory.GetStrFromWs(ws);
			w.WriteLine("<form lang=\"{0}\"><text>{1}</text></form>",
						XmlUtils.MakeSafeXmlAttribute(sLang), XmlUtils.MakeSafeXml(tss.Text));
		}

		private void WritePronunciation(TextWriter w, ILexPronunciation pron)
		{
			w.Write("<pronunciation");
			WriteLiftDates(w, pron);
			w.WriteLine(">");
			WriteAllForms(w, null, null, "form", pron.Form);
			foreach (var file in pron.MediaFilesOS)
				WriteMediaFile(w, file);
			WriteString(w, "field", "type=\"cv-pattern\"", "form", pron.CVPattern);
			WriteString(w, "field", "type=\"tone\"", "form", pron.Tone);
			if (pron.LocationRA != null)
				w.WriteLine("<trait name=\"{0}\" value=\"{1}\"/>",
					RangeNames.sLocationsOA, XmlUtils.MakeSafeXmlAttribute(pron.LocationRA.Name.BestAnalysisVernacularAlternative.Text));
			WriteLiftResidue(w, pron);
			w.WriteLine("</pronunciation>");
		}

		private void WriteMediaFile(TextWriter w, ICmMedia file)
		{
			w.Write("<media href=\"");
			ExportFile(w, file.MediaFileRA.InternalPath, file.MediaFileRA.AbsoluteInternalPath, "audio",
				DirectoryFinder.ksMediaDir);
			//if (file.MediaFileRA != null)
			//    w.Write(XmlUtils.MakeSafeXmlAttribute(Path.GetFileName(file.MediaFileRA.InternalPath)));
			w.WriteLine("\">");
			WriteAllForms(w, "label", null, "form", file.Label);
			w.Write("</media>");
		}

		private void WriteLexReference(TextWriter w, ILexReference lref, ICmObject lexItem)
		{
			var slr = new SingleLexReference(lref, lref.TargetsRS[0].Hvo);
			var nMappingType = slr.MappingType;
			var hvoOpen = lexItem.Hvo;
			for (var i = 0; i < lref.TargetsRS.Count; i++)
			{
				var target = lref.TargetsRS[i];
				// If the LexReference vector element is the currently open object, ignore
				// it unless it's a sequence type relation.
				if (nMappingType != (int)LexRefTypeTags.MappingTypes.kmtSenseSequence &&
					nMappingType != (int)LexRefTypeTags.MappingTypes.kmtEntrySequence &&
					nMappingType != (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence)
				{
					if (target.Hvo == hvoOpen)
						continue;
				}
				slr.CrossRefHvo = target.Hvo;
				w.Write("<relation");
				WriteLiftDates(w, lref);
				w.Write(" type=\"{0}\"",
						slr.TypeName((int)SpecialWritingSystemCodes.BestAnalysisOrVernacular, lexItem.Hvo));
				w.Write(" ref=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(slr.RefLIFTid));
				var refOrder = slr.RefOrder;
				if (!String.IsNullOrEmpty(refOrder))
					w.Write(" order=\"{0}\"", refOrder);
				var residue = slr.LiftResidueContent;
				if (String.IsNullOrEmpty(residue))
				{
					w.WriteLine("/>");
				}
				else
				{
					w.WriteLine(">");
					w.Write(residue);
					w.WriteLine("</relation>");
				}

				// If this is a tree type relation, show only the first element if the
				// currently open object is not the first element.
				if (nMappingType == (int)LexRefTypeTags.MappingTypes.kmtSenseTree ||
					nMappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryTree ||
					nMappingType == (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree)
				{
					if (hvoOpen != lref.TargetsRS[0].Hvo)
						break;
				}
			}
		}

		private void WriteLexEntryRef(TextWriter w, ILexEntryRef ler)
		{
			var typeString = "_component-lexeme";
			// special case to export the BaseForm complex form type as expected by WeSay.
			if (ler.ComplexEntryTypesRS.Count == 1 && ler.ComplexEntryTypesRS[0].Name.get_String(m_cache.WritingSystemFactory.GetWsFromStr("en")).Text == "BaseForm")
				typeString = "BaseForm";
			foreach (var obj in ler.ComponentLexemesRS)
			{
				w.WriteLine("<relation type=\"" + typeString + "\" ref=\"{0}\">",
					XmlUtils.MakeSafeXmlAttribute(GetProperty(obj, "LIFTid").ToString()));
				if (ler.PrimaryLexemesRS.Contains(obj))
					w.WriteLine("<trait name=\"is-primary\" value=\"true\"/>");
				WriteLexEntryRefBasics(w, ler);
				w.WriteLine("</relation>");
			}
			if (ler.ComponentLexemesRS.Count == 0)
			{
				w.WriteLine("<relation type=\"_component-lexeme\" ref=\"\">");
				WriteLexEntryRefBasics(w, ler);
				w.WriteLine("</relation>");
			}
		}

		private void WriteLexEntryRefBasics(TextWriter w, ILexEntryRef ler)
		{
			foreach (var type in ler.ComplexEntryTypesRS)
				WriteTrait(w, "complex-form-type", type.Name, m_wsBestAnalVern);
			foreach (var type in ler.VariantEntryTypesRS)
				WriteTrait(w, "variant-type", type.Name, m_wsBestAnalVern);
			if (ler.ComplexEntryTypesRS.Count == 0 && ler.VariantEntryTypesRS.Count == 0)
			{
				switch (ler.RefType)
				{
					case LexEntryRefTags.krtVariant:
						w.WriteLine("<trait name=\"variant-type\" value=\"\"/>");
						break;
					case LexEntryRefTags.krtComplexForm:
						w.WriteLine("<trait name=\"complex-form-type\" value=\"\"/>");
						break;
				}
			}
			if (ler.HideMinorEntry != 0)
				w.WriteLine("<trait name=\"hide-minor-entry\" value=\"{0}\"/>", ler.HideMinorEntry);
			WriteAllForms(w, "field", "type=\"summary\"", "form", ler.Summary);
		}

		private void WriteEtymology(TextWriter w, ILexEtymology ety)
		{
			w.Write("<etymology");
			WriteLiftDates(w, ety);
			w.Write(" type=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(GetProperty(ety, "LiftType").ToString()));
			w.Write(" source=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(GetProperty(ety, "LiftSource").ToString()));
			w.WriteLine(">");
			WriteAllForms(w, null, null, "form", ety.Form);
			WriteAllForms(w, null, null, "gloss", ety.Gloss);
			WriteAllForms(w, "field", "type=\"comment\"", "form", ety.Comment);
			WriteLiftResidue(w, ety);
			w.WriteLine("</etymology>");
		}

		private void WriteAlternateForm(TextWriter w, IMoForm alt)
		{
			w.Write("<variant");
			WriteLiftDates(w, alt);
			switch (alt.ClassID)
			{
				case MoStemAllomorphTags.kClassId:
					{
						var stemAllo = alt as IMoStemAllomorph;
						Debug.Assert(stemAllo != null);
						var refer = GetProperty(alt, "LiftRefAttribute") as string;
						if (!String.IsNullOrEmpty(refer))
							w.Write(" ref=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(refer));
						w.WriteLine(">");
						WriteAllFormsWithMarkers(w, null, null, "form", alt);
						foreach (var env in stemAllo.PhoneEnvRC)
							WritePhEnvironment(w, env);
					}
					break;
				case MoAffixAllomorphTags.kClassId:
					{
						var affixAllo = alt as IMoAffixAllomorph;
						Debug.Assert(affixAllo != null);
						var refer = GetProperty(alt, "LiftRefAttribute") as string;
						if (!String.IsNullOrEmpty(refer))
							w.Write(" ref=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(refer));
						w.WriteLine(">");
						WriteAllFormsWithMarkers(w, null, null, "form", alt);
						foreach (var env in affixAllo.PhoneEnvRC)
							WritePhEnvironment(w, env);
					}
					break;
				case MoAffixProcessTags.kClassId:
					{
						w.WriteLine(">");
						WriteAllFormsWithMarkers(w, null, null, "form", alt);
					}
					break;
			}
			if (alt.MorphTypeRA != null)
				WriteTrait(w, RangeNames.sDbMorphTypesOA, alt.MorphTypeRA.Name, m_wsEn);
			WriteCustomFields(w, alt);
			WriteLiftResidue(w, alt);
			w.WriteLine("</variant>");
		}

		private static void WritePhEnvironment(TextWriter w, IPhEnvironment env)
		{
			var repr = env.StringRepresentation.Text;
			if (repr != null)
				w.WriteLine("<trait name =\"environment\" value=\"{0}\"/>", XmlUtils.MakeSafeXmlAttribute(repr));
		}

		private void WriteLexSense(TextWriter w, ILexSense sense, bool fOrder)
		{
			if (sense.Owner is ILexEntry)
				w.Write("<sense id=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sense.LIFTid));
			else
				w.Write("<subsense id=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sense.LIFTid));
			WriteLiftDates(w, sense);
			if (fOrder)
				w.Write(" order=\"{0}\"", sense.IndexInOwner);
			w.WriteLine(">");
			if (sense.MorphoSyntaxAnalysisRA != null)
			{
				switch (sense.MorphoSyntaxAnalysisRA.ClassID)
				{
					case MoStemMsaTags.kClassId:
						WriteMoStemMsa(w, sense.MorphoSyntaxAnalysisRA as IMoStemMsa);
						break;
					case MoUnclassifiedAffixMsaTags.kClassId:
						WriteMoUnclassifiedAffixMsa(w, sense.MorphoSyntaxAnalysisRA as IMoUnclassifiedAffixMsa);
						break;
					case MoInflAffMsaTags.kClassId:
						WriteMoInflAffMsa(w, sense.MorphoSyntaxAnalysisRA as IMoInflAffMsa);
						break;
					case MoDerivAffMsaTags.kClassId:
						WriteMoDerivAffMsa(w, sense.MorphoSyntaxAnalysisRA as IMoDerivAffMsa);
						break;
					case MoDerivStepMsaTags.kClassId:
						WriteMoDerivStepMsa(w, sense.MorphoSyntaxAnalysisRA as IMoDerivStepMsa);
						break;
				}
			}
			WriteAllForms(w, null, null, "gloss", sense.Gloss);
			WriteAllForms(w, "definition", null, "form", sense.Definition);
			foreach (var example in sense.ExamplesOS)
				WriteExampleSentence(w, example);
			foreach (var sem in sense.SemanticDomainsRC)
			{
				var val = GetProperty(sem, "LiftAbbrAndName") as string;
				w.WriteLine("<trait name =\"{0}\" value=\"{1}\"/>", RangeNames.sSemanticDomainListOA, XmlUtils.MakeSafeXmlAttribute(val));
			}
			WriteAllForms(w, "note", "type=\"anthropology\"", "form", sense.AnthroNote);
			WriteAllForms(w, "note", "type=\"bibliography\"", "form", sense.Bibliography);
			WriteAllForms(w, "note", "type=\"discourse\"", "form", sense.DiscourseNote);
			WriteAllForms(w, "note", "type=\"encyclopedic\"", "form", sense.EncyclopedicInfo);
			WriteAllForms(w, "note", null, "form", sense.GeneralNote);
			WriteAllForms(w, "note", "type=\"grammar\"", "form", sense.GrammarNote);
			WriteString(w, "field", "type=\"import-residue\"", "form", sense.ImportResidue);
			WriteAllForms(w, "note", "type=\"phonology\"", "form", sense.PhonologyNote);
			WriteAllForms(w, "note", "type=\"restrictions\"", "form", sense.Restrictions);
			WriteString(w, "field", "type=\"scientific-name\"", "form", sense.ScientificName);
			WriteAllForms(w, "note", "type=\"semantics\"", "form", sense.SemanticsNote);
			WriteAllForms(w, "note", "type=\"sociolinguistics\"", "form", sense.SocioLinguisticsNote);
			WriteString(w, "note", "type=\"source\"", "form", sense.Source);
			foreach (var anthro in sense.AnthroCodesRC)
				WriteTrait(w, RangeNames.sAnthroListOA, anthro.Abbreviation, m_wsBestAnalVern);
			foreach (var dom in sense.DomainTypesRC)
				WriteTrait(w, RangeNames.sDbDomainTypesOA, dom.Name, m_wsBestAnalVern);
			foreach (var reversal in sense.ReversalEntriesRC)
				WriteReversal(w, reversal);
			if (sense.SenseTypeRA != null)
				WriteTrait(w, RangeNames.sDbSenseTypesOA, sense.SenseTypeRA.Name, m_wsBestAnalVern);
			if (sense.StatusRA != null)
				WriteTrait(w, RangeNames.sStatusOA, sense.StatusRA.Name, m_wsBestAnalVern);
			foreach (var usage in sense.UsageTypesRC)
				WriteTrait(w, RangeNames.sDbUsageTypesOA, usage.Name, m_wsBestAnalVern);
			foreach (var picture in sense.PicturesOS)
				WriteCmPicture(w, picture);
			foreach (var lref in sense.LexSenseReferences)
				WriteLexReference(w, lref, sense);
			WriteCustomFields(w, sense);
			WriteLiftResidue(w, sense);
			foreach (var subsense in sense.SensesOS)
				WriteLexSense(w, subsense, subsense.SensesOS.Count > 1);
			if (sense.Owner is ILexEntry)
				w.WriteLine("</sense>");
			else
				w.WriteLine("</subsense>");
		}

		private void WriteCmPicture(TextWriter w, ICmPicture picture)
		{
			w.Write("<illustration href=\"");
			if (picture.PictureFileRA != null)
			{
				ExportFile(w, picture.PictureFileRA.InternalPath, picture.PictureFileRA.AbsoluteInternalPath,
					"pictures", DirectoryFinder.ksPicturesDir);
			}
			w.WriteLine("\">");
			WriteAllForms(w, "label", null, "form", picture.Caption);
			w.Write("</illustration>");
		}

		// Path of every file we created (or deliberately overwrote) in ExportFile.
		Set<string> m_filesCreated = new Set<string>();

		private string ExportFile(string internalPath, string actualPath, string liftFolderName, string expectRootFolder)
		{
			if (string.IsNullOrEmpty(internalPath))
				return null;
			// Typically internalPath is something like "Pictures\MyFile.jpg".
			// If it starts with the expected folder, we want to make the LIFT path omit that element.
			var writePath = Path.GetFileName(internalPath); // the path to store in the lift file (by default).
			if (writePath == null)
				return null; // not sure how this can happen, but Resharper is worried.
			// Try a few ways stripping off the expected root folder. I'm not sure what separator we actually store,
			// especially if the FW project has lived on both Linux and Windows.
			if (internalPath.StartsWith(expectRootFolder + Path.PathSeparator))
				writePath = internalPath.Substring((expectRootFolder + Path.PathSeparator).Length);
			else if (internalPath.StartsWith(expectRootFolder + "\\"))
				writePath = internalPath.Substring((expectRootFolder + "\\").Length);
			else if (internalPath.StartsWith(expectRootFolder + "/"))
				writePath = internalPath.Substring((expectRootFolder + "/").Length);
			// Otherwise, it's in some non-standard position, and we'll have to just put it directly in the target folder.
			writePath = ExportFile(writePath, actualPath, liftFolderName);
			return writePath;
		}

		/// <summary>
		/// This simpler overload is useful when we already know the exact path we will write in the destination folder
		/// (unless it exists already).
		/// </summary>
		/// <param name="writePath"></param>
		/// <param name="actualPath"></param>
		/// <param name="liftFolderName"></param>
		/// <returns></returns>
		private string ExportFile(string writePath, string actualPath, string liftFolderName)
		{
			if (ExportPicturesAndMedia && !string.IsNullOrEmpty(FolderPath) && File.Exists(actualPath))
			{
				var destFolder = Path.Combine(FolderPath, liftFolderName);
				Directory.CreateDirectory(destFolder);
				var destFilePath = Path.Combine(destFolder, writePath);
				int affix = 1;
				var pathWithoutExt = Path.Combine(Path.GetDirectoryName(writePath), Path.GetFileNameWithoutExtension(writePath));
				var ext = Path.GetExtension(writePath) ?? "";
				while (m_filesCreated.Contains(destFilePath))
				{
					// generate a new name
					writePath = Path.ChangeExtension(pathWithoutExt + "_" + affix++, ext);
					destFilePath = Path.Combine(destFolder, writePath);
				}
				m_filesCreated.Add(destFilePath);
				// There may nevertheless be an existing file of that name, e.g., from an earlier export to the same location.
				// We don't want to genereate mangled names and multiple copies in such cases, so we allow overwrite.
				Directory.CreateDirectory(Path.GetDirectoryName(destFilePath));
				File.Copy(actualPath, destFilePath, true);
			}
			return writePath;
		}

		private void ExportFile(TextWriter w, string internalPath, string actualPath, string liftFolderName, string expectRootFolder)
		{
			var writePath = ExportFile(internalPath, actualPath, liftFolderName, expectRootFolder);
			w.Write(XmlUtils.MakeSafeXmlAttribute(writePath)); // write the name we actually used to copy the file.
		}

		private void WriteReversal(TextWriter w, IReversalIndexEntry reversal)
		{
			w.Write("<reversal type=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(reversal.ReversalIndex.WritingSystem));
			WriteAllForms(w, null, null, "form", reversal.ReversalForm);
			if (reversal.PartOfSpeechRA != null)
				w.WriteLine("<grammatical-info value=\"{0}\"/>", BestAlternative(reversal.PartOfSpeechRA.Name, m_wsEn));
			if (reversal.OwningEntry != null)
				WriteOwningReversal(w, reversal.OwningEntry);
			w.WriteLine("</reversal>");
		}

		private void WriteOwningReversal(TextWriter w, IReversalIndexEntry reversal)
		{
			w.WriteLine("<main>");
			WriteAllForms(w, null, null, "form", reversal.ReversalForm);
			if (reversal.PartOfSpeechRA != null)
				w.WriteLine("<grammatical-info value=\"{0}\"/>", BestAlternative(reversal.PartOfSpeechRA.Name, m_wsEn));
			if (reversal.OwningEntry != null)
				WriteOwningReversal(w, reversal.OwningEntry);
			w.WriteLine("</main>");
		}

		private static string BestAlternative(IMultiAccessorBase multi, int wsDefault)
		{
			var tss = multi.BestAnalysisVernacularAlternative;
			if (tss.Text == "***")
				tss = multi.get_String(wsDefault);
			return XmlUtils.MakeSafeXmlAttribute(tss.Text);
		}

		private void WriteExampleSentence(TextWriter w, ILexExampleSentence example)
		{
			w.Write("<example");
			WriteLiftDates(w, example);
			if (example.Reference != null && example.Reference.Length > 0)
				w.Write(" source=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(example.Reference.Text));
			w.WriteLine(">");
			WriteAllForms(w, null, null, "form", example.Example);
			foreach (var trans in example.TranslationsOC)
				WriteTranslation(w, trans);
			WriteString(w, "note", "type=\"reference\"", "form", example.Reference);
			WriteCustomFields(w, example);
			WriteLiftResidue(w, example);
			w.WriteLine("</example>");
		}

		private void WriteTranslation(TextWriter w, ICmTranslation trans)
		{
			if (trans.TypeRA == null)
				w.WriteLine("<translation>");
			else
				w.WriteLine("<translation type=\"{0}\">", BestAlternative(trans.TypeRA.Name, m_wsEn));
			WriteAllForms(w, null, null, "form", trans.Translation);
			w.WriteLine("</translation>");
		}

		private int m_flidMoStemMsaIsEmpty;
		private int m_flidUnclAffixIsEmpty;

		private void WriteMoStemMsa(TextWriter w, IMoStemMsa msa)
		{
			if (m_flidMoStemMsaIsEmpty == 0)
				m_flidMoStemMsaIsEmpty = m_cache.MetaDataCacheAccessor.GetFieldId("MoStemMsa", "IsEmpty", false);
			if (m_cache.DomainDataByFlid.get_BooleanProp(msa.Hvo, m_flidMoStemMsaIsEmpty))
				return;
			if (String.IsNullOrEmpty(msa.PosFieldName))
				w.WriteLine("<grammatical-info value=\"\">");
			else
				w.WriteLine("<grammatical-info value=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(msa.PosFieldName));
			if (msa.InflectionClassRA != null)
			{
				w.Write("<trait name=\"{0}-infl-class\" value=\"{1}\"/>",
					XmlUtils.MakeSafeXmlAttribute(msa.InflectionClassRA.Owner.ShortName),
					BestAlternative(msa.InflectionClassRA.Name, m_wsEn));
			}
			foreach (var pos in msa.FromPartsOfSpeechRC)
				WriteTrait(w, RangeNames.sPartsOfSpeechOAold1, pos.Name, m_wsBestAnalVern);
			if (msa.MsFeaturesOA != null && !msa.MsFeaturesOA.IsEmpty)
				w.WriteLine("<trait name=\"{0}\" value=\"{1}\"/>", RangeNames.sMSAinflectionFeature, XmlUtils.MakeSafeXmlAttribute(msa.MsFeaturesOA.LiftName));
			foreach (var restrict in msa.ProdRestrictRC)
				WriteTrait(w, RangeNames.sProdRestrictOA, restrict.Name, m_wsBestAnalVern);
			WriteLiftResidue(w, msa);
			w.WriteLine("</grammatical-info>");
		}

		private void WriteMoUnclassifiedAffixMsa(TextWriter w, IMoUnclassifiedAffixMsa msa)
		{
			m_flidUnclAffixIsEmpty = m_cache.MetaDataCacheAccessor.GetFieldId("MoUnclassifiedAffixMsa", "IsEmpty", false);
			if (m_cache.DomainDataByFlid.get_BooleanProp(msa.Hvo, m_flidUnclAffixIsEmpty))
				return;
			if (String.IsNullOrEmpty(msa.PosFieldName))
				w.WriteLine("<grammatical-info value=\"\">");
			else
				w.WriteLine("<grammatical-info value=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(msa.PosFieldName));
			w.WriteLine("<trait name=\"type\" value=\"affix\"/>");
			WriteLiftResidue(w, msa);
			w.WriteLine("</grammatical-info>");
		}

		private void WriteMoInflAffMsa(TextWriter w, IMoInflAffMsa msa)
		{
			if (String.IsNullOrEmpty(msa.PosFieldName))
				w.WriteLine("<grammatical-info value=\"\">");
			else
				w.WriteLine("<grammatical-info value=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(msa.PosFieldName));
			w.WriteLine("<trait name=\"type\" value=\"inflAffix\"/>");
			foreach (var slot in msa.SlotsRC)
			{
				w.Write("<trait name=\"{0}-slot\" value=\"{1}\"/>",
					XmlUtils.MakeSafeXmlAttribute(slot.Owner.ShortName), BestAlternative(slot.Name, m_wsEn));
			}
			if (msa.InflFeatsOA != null && !msa.InflFeatsOA.IsEmpty)
				w.WriteLine("<trait name=\"{0}\" value=\"{1}\"/>", RangeNames.sMSAinflectionFeature, XmlUtils.MakeSafeXmlAttribute(msa.InflFeatsOA.LiftName));
			foreach (var restrict in msa.FromProdRestrictRC)
				WriteTrait(w, RangeNames.sProdRestrictOA, restrict.Name, m_wsBestAnalVern);
			WriteLiftResidue(w, msa);
			w.WriteLine("</grammatical-info>");
		}

		private void WriteMoDerivAffMsa(TextWriter w, IMoDerivAffMsa msa)
		{
			if (String.IsNullOrEmpty(msa.PosFieldName))
				w.WriteLine("<grammatical-info value=\"\">");
			else
				w.WriteLine("<grammatical-info value=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(msa.PosFieldName));
			w.WriteLine("<trait name=\"type\" value=\"derivAffix\"/>");
			if (msa.FromPartOfSpeechRA != null)
			{
				WriteTrait(w, RangeNames.sPartsOfSpeechOAold1, msa.FromPartOfSpeechRA.Name, m_wsBestAnalVern);
			}
			if (msa.FromInflectionClassRA != null)
			{
				w.WriteLine("<trait name=\"from-{0}-infl-class\" value=\"{1}\"/>",
							XmlUtils.MakeSafeXmlAttribute(msa.FromInflectionClassRA.Owner.ShortName),
							BestAlternative(msa.FromInflectionClassRA.Name, m_wsEn));
			}
			foreach (var restrict in msa.FromProdRestrictRC)
			{
				WriteTrait(w, RangeNames.sProdRestrictOAfrom, restrict.Name, m_wsBestAnalVern);
			}
			if (msa.FromMsFeaturesOA != null && !msa.FromMsFeaturesOA.IsEmpty)
			{
				w.WriteLine("<trait name=\"from-inflection-feature\" value=\"{0}\"/>",
							XmlUtils.MakeSafeXmlAttribute(msa.FromMsFeaturesOA.LiftName));
			}
			if (msa.FromStemNameRA != null)
			{
				WriteTrait(w, "from-stem-name", msa.FromStemNameRA.Name, m_wsBestAnalVern);
			}
			if (msa.ToInflectionClassRA != null)
			{
				w.WriteLine("<trait name=\"{0}-infl-class\" value=\"{1}\"/>",
							XmlUtils.MakeSafeXmlAttribute(msa.ToInflectionClassRA.Owner.ShortName),
							BestAlternative(msa.ToInflectionClassRA.Name, m_wsEn));
			}
			foreach (var restrict in msa.ToProdRestrictRC)
			{
				WriteTrait(w, RangeNames.sProdRestrictOA, restrict.Name, m_wsBestAnalVern);
			}
			if (msa.ToMsFeaturesOA != null && !msa.ToMsFeaturesOA.IsEmpty)
			{
				w.WriteLine("<trait name=\"{0}\" value=\"{1}\"/>", RangeNames.sMSAinflectionFeature,
							XmlUtils.MakeSafeXmlAttribute(msa.ToMsFeaturesOA.LiftName));
			}
			WriteLiftResidue(w, msa);
			w.WriteLine("</grammatical-info>");
		}

		private void WriteMoDerivStepMsa(TextWriter w, IMoDerivStepMsa msa)
		{
			if (String.IsNullOrEmpty(msa.PosFieldName))
				w.WriteLine("<grammatical-info value=\"\">");
			else
				w.WriteLine("<grammatical-info value=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(msa.PosFieldName));
			w.WriteLine("<trait name=\"type\" value=\"derivStepAffix\"/>");
			WriteLiftResidue(w, msa);
			w.WriteLine("</grammatical-info>");
		}

		private void WriteHeaderInformation(TextWriter w)
		{
			w.WriteLine("<header>");
			WriteInternalRangeInformation(w);
			WriteInternalFieldInformation(w);
			w.WriteLine("</header>");
		}

		/// <summary>
		/// Write the references to the ranges file.
		/// </summary>
		/// <remarks>
		/// It might be nice to put the different ranges in different files.
		/// </remarks>
		private void WriteInternalRangeInformation(TextWriter w)
		{
			w.WriteLine("<ranges>");
			var sOutputFilePath = "DUMMYFILENAME.lift";
			if (w is StreamWriter)
			{
				var sw = w as StreamWriter;
				if (sw.BaseStream is FileStream)
					sOutputFilePath = ((FileStream)sw.BaseStream).Name;
			}
			var sRangesFile = Path.ChangeExtension(sOutputFilePath.Replace('\\', '/'), ".lift-ranges");
			sRangesFile = XmlUtils.MakeSafeXmlAttribute(sRangesFile);
			w.WriteLine("<range id=\"dialect\" href=\"file://{0}\"/>", sRangesFile);
			w.WriteLine("<range id=\"etymology\" href=\"file://{0}\"/>", sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sPartsOfSpeechOA, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sDbReferencesOA, sRangesFile);
			w.WriteLine("<range id=\"note-type\" href=\"file://{0}\"/>", sRangesFile);
			w.WriteLine("<range id=\"paradigm\" href=\"file://{0}\"/>", sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sReversalType, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sSemanticDomainListOA, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sStatusOA, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sPeopleOA, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sLocationsOA, sRangesFile);
			w.WriteLine("<!-- The following ranges are produced by FieldWorks Language Explorer, and are not part of the LIFT standard. -->");
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sAnthroListOA, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sTranslationTagsOA, sRangesFile);
			w.WriteLine("<!-- The parts of speech are duplicated in another range because derivational affixes require a \"From\" PartOfSpeech as well as a \"To\" PartOfSpeech. -->");
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sPartsOfSpeechOAold1, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sDbMorphTypesOA, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sProdRestrictOA, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sMSAinflectionFeature, sRangesFile);
			w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>", RangeNames.sMSAinflectionFeatureType, sRangesFile);
			if (m_cache.LangProject.MsFeatureSystemOA != null)
				WriteRangeRefsForMsFeatureSystem(w, sRangesFile);
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
				WriteRangeRefsForSlots(w, sRangesFile, pos);
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
				WriteRangeRefForInflectionClasses(w, sRangesFile, pos);
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
				WriteRangeRefsForStemNames(w, sRangesFile, pos);
			WriteRangeRefsForListsReferencedByFields(w, sRangesFile);
			w.WriteLine("</ranges>");
		}


		/// <summary>
		/// This is writing out the reference to the range in the main LIFT file.
		/// The range is actually written to a different file.
		/// </summary>
		/// <param name="w"></param>
		/// <param name="sRangesFile"></param>
		private void WriteRangeRefsForListsReferencedByFields(TextWriter w, string sRangesFile)
		{
			foreach (var posList in m_CmPossibilityListsReferencedByFields)
			{
				//We actually want to export any range which is referenced by a Custom field and is not already output.
				//not just Custom ranges.
				String rangeName;
				var haveValue = m_CmPossibilityListsReferencedByFields.TryGetValue(posList.Key, out rangeName);
				w.WriteLine("<range id=\"{0}\" href=\"file://{1}\"/>",
				XmlUtils.MakeSafeXmlAttribute(rangeName), sRangesFile);
			}
		}

		private static void WriteRangeRefForInflectionClasses(TextWriter w, string sRangesFile, IPartOfSpeech pos)
		{
			if (pos.InflectionClassesOC.Count > 0)
			{
				w.WriteLine("<range id=\"{0}-infl-class\" href=\"file://{1}\"/>",
					XmlUtils.MakeSafeXmlAttribute(pos.Name.BestAnalysisVernacularAlternative.Text),
					sRangesFile);
			}
		}

		private static void WriteRangeRefsForStemNames(TextWriter w, string sRangesFile, IPartOfSpeech pos)
		{
			if (pos.StemNamesOC.Count > 0)
			{
				w.WriteLine("<range id=\"{0}-stem-name\" href=\"file://{1}\"/>",
					XmlUtils.MakeSafeXmlAttribute(pos.Name.BestAnalysisVernacularAlternative.Text),
					sRangesFile);
			}
		}

		private static void WriteRangeRefsForSlots(TextWriter w, string sRangesFile, IPartOfSpeech pos)
		{
			if (pos.AffixSlotsOC.Count > 0)
			{
				w.WriteLine("<range id=\"{0}-slot\" href=\"file://{1}\"/>",
					XmlUtils.MakeSafeXmlAttribute(pos.Name.BestAnalysisVernacularAlternative.Text),
					sRangesFile);
			}
		}

		private void WriteRangeRefsForMsFeatureSystem(TextWriter w, string sRangesFile)
		{
			foreach (var featDefn in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.OfType<IFsClosedFeature>())
			{
				if (featDefn.ValuesOC.Count > 0)
				{
					w.WriteLine("<range id=\"{0}-feature-value\" href=\"file://{1}\"/>",
						XmlUtils.MakeSafeXmlAttribute(featDefn.Abbreviation.BestAnalysisVernacularAlternative.Text),
						sRangesFile);
				}
			}
		}

		private void WriteInternalFieldInformation(TextWriter w)
		{
			w.WriteLine("<fields>");
			w.WriteLine("<field tag=\"cv-pattern\">");
			w.WriteLine("<form lang=\"en\"><text>This records the syllable pattern for a LexPronunciation in FieldWorks.</text></form>");
			w.WriteLine("</field>");
			w.WriteLine("<field tag=\"tone\">");
			w.WriteLine("<form lang=\"en\"><text>This records the tone information for a LexPronunciation in FieldWorks.</text></form>");
			w.WriteLine("</field>");
			w.WriteLine("<field tag=\"comment\">");
			w.WriteLine("<form lang=\"en\"><text>This records a comment (note) in a LexEtymology in FieldWorks.</text></form>");
			w.WriteLine("</field>");
			w.WriteLine("<field tag=\"import-residue\">");
			w.WriteLine("<form lang=\"en\"><text>This records residue left over from importing a standard format file into FieldWorks (or LinguaLinks).</text></form>");
			w.WriteLine("</field>");
			w.WriteLine("<field tag=\"literal-meaning\">");
			w.WriteLine("<form lang=\"en\"><text>This field is used to store a literal meaning of the entry.  Typically, this field is necessary only for a compound or an idiom where the meaning of the whole is different from the sum of its parts.</text></form>");
			w.WriteLine("</field>");
			w.WriteLine("<field tag=\"summary-definition\">");
			w.WriteLine("<form lang=\"en\"><text>A summary definition (located at the entry level in the Entry pane) is a general definition summarizing all the senses of a primary entry. It has no theoretical value; its use is solely pragmatic.</text></form>");
			w.WriteLine("</field>");
			w.WriteLine("<field tag=\"scientific-name\">");
			w.WriteLine("<form lang=\"en\"><text>This field stores the scientific name pertinent to the current sense.</text></form>");
			w.WriteLine("</field>");

			GenerateCustomFieldSpecs(w, "LexEntry");
			GenerateCustomFieldSpecs(w, "LexSense");
			GenerateCustomFieldSpecs(w, "MoForm");
			GenerateCustomFieldSpecs(w, "LexExampleSentence");
			w.WriteLine("</fields>");
		}

		/// <summary>
		/// Generate the field definition elements for all the custom fields of the given class.
		/// </summary>
		/// <remarks>
		/// NOTE: This will have to change considerably according to LT-10964.
		/// </remarks>
		private void GenerateCustomFieldSpecs(TextWriter w, string className)
		{
			var clid = m_mdc.GetClassId(className);
			foreach (var flid in m_mdc.GetFields(clid, true, (int)CellarPropertyTypeFilter.All))
			{
				if (!m_mdc.IsCustom(flid))
					continue;
				var labelName = m_mdc.GetFieldLabel(flid);
				var fieldName = m_mdc.GetFieldName(flid);
				if (String.IsNullOrEmpty(labelName))
					labelName = fieldName;
				var sHelp = m_mdc.GetFieldHelp(flid);
				var sSpec = GetCustomFieldDefinition(className, flid);
				w.WriteLine("<field tag=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(labelName));
				w.WriteLine("<form lang=\"en\"><text>{0}</text></form>", XmlUtils.MakeSafeXmlAttribute(sHelp));
				w.WriteLine("<form lang=\"qaa-x-spec\"><text>{0}</text></form>", XmlUtils.MakeSafeXmlAttribute(sSpec));
				w.WriteLine("</field>");
			}
		}

		/// <summary>
		/// This method was written for LT-12275 because WritingSystemServices.GetMagicWsNameFromId(ws);
		/// does not return the desired strings for LIFT export.
		/// Must be consistent with FlexLiftMerger.GetLiftExportMagicWsIdFromName
		/// </summary>
		/// <param name="ws">This should be between -1 and -6.</param>
		/// <returns></returns>
		private string GetLiftExportMagicWsNameFromId(int ws)
		{
			switch (ws)
			{
				case WritingSystemServices.kwsAnal:
					return "kwsAnal";
				case WritingSystemServices.kwsVern:
					return "kwsVern";
				case WritingSystemServices.kwsAnals:
					return "kwsAnals";
				case WritingSystemServices.kwsVerns:
					return "kwsVerns";
				case WritingSystemServices.kwsAnalVerns:
					return "kwsAnalVerns";
				case WritingSystemServices.kwsVernAnals:
					return "kwsVernAnals";
			}
			return "";
		}

		/// <summary>
		/// This is a temporary (I HOPE!) hack to get something out to the LIFT file until
		/// the LIFT spec allows a better form of field definition.
		/// </summary>
		private string GetCustomFieldDefinition(string className, int flid)
		{
			var sb = new StringBuilder();
			var cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
			var sType = cpt.ToString();
			sb.AppendFormat("Class={0}; Type={1}", className, sType);
			var ws = m_mdc.GetFieldWs(flid);
			if (ws < 0)
			{
				var wsInternalName = GetLiftExportMagicWsNameFromId(ws);
				Debug.Assert(!String.IsNullOrEmpty(wsInternalName), "There is an invalid magic writing system value being exported. It should be between -1 and -6. ");
				sb.AppendFormat("; WsSelector={0}", wsInternalName);
			}
			else if (ws > 0)
				sb.AppendFormat("; WsSelector={0}", m_cache.WritingSystemFactory.GetStrFromWs(ws));
			var clidDst = m_mdc.GetDstClsId(flid);
			if (clidDst > 0)
				sb.AppendFormat("; DstCls={0}", m_mdc.GetClassName(clidDst));
			if (cpt == CellarPropertyType.ReferenceAtomic || cpt == CellarPropertyType.ReferenceCollection ||
				cpt == CellarPropertyType.ReferenceSequence)
			{
				var listGuid = m_mdc.GetFieldListRoot(flid);
				String listName;
				var haveValue = m_ListsGuidToRangeName.TryGetValue(listGuid, out listName);
				Debug.Assert(haveValue, "We have a problem of having a Custom List which is not accounted for.");
				sb.AppendFormat("; range={0}", listName);
			}
			return sb.ToString();
		}

		private static string GetVersion()
		{
			var assembly = Assembly.GetEntryAssembly();
			string sVersion = null;
			if (assembly != null)
			{
				// Set the application version text
				var attributes = assembly.GetCustomAttributes(typeof(AssemblyFileVersionAttribute), false);
				sVersion = (attributes.Length > 0) ?
					((AssemblyFileVersionAttribute)attributes[0]).Version :
					System.Windows.Forms.Application.ProductVersion;
			}
			if (String.IsNullOrEmpty(sVersion))
				sVersion = "???";
			return XmlUtils.MakeSafeXmlAttribute(sVersion);
		}

		private void WriteAllForms(TextWriter w, string wrappingElementName, string attrs,
			string elementName, IMultiUnicode multi)
		{
			if (multi == null || multi.StringCount == 0)
				return;
			if (!String.IsNullOrEmpty(wrappingElementName))
			{
				if (String.IsNullOrEmpty(attrs))
					w.WriteLine("<{0}>", wrappingElementName);
				else
					w.WriteLine("<{0} {1}>", wrappingElementName, attrs);
			}
			for (var i = 0; i < multi.StringCount; ++i)
			{
				int ws;
				var sForm = multi.GetStringFromIndex(i, out ws).Text;
				if (String.IsNullOrEmpty(sForm))
					continue;

				var sLang = m_cache.WritingSystemFactory.GetStrFromWs(ws);
				if (IsVoiceWritingSystem(ws))
				{
					// The alternative contains a file path. We need to adjust and export and copy the file.
					var internalPath = sForm;
					// usually this will be unchanged, but it is pathologically possible that the file name conflicts.
					sForm = ExportFile(internalPath,
						Path.Combine(DirectoryFinder.GetMediaDir(m_cache.LangProject.LinkedFilesRootDir), internalPath),
						"audio");
				}
				w.WriteLine("<{0} lang=\"{1}\"><text>{2}</text></{0}>", elementName,
					XmlUtils.MakeSafeXmlAttribute(sLang),
					XmlUtils.MakeSafeXml(sForm.Replace("\x2028", Environment.NewLine)));
			}
			if (!String.IsNullOrEmpty(wrappingElementName))
				w.WriteLine("</{0}>", wrappingElementName);
		}


		private void WriteAllFormsWithMarkers(TextWriter w, string wrappingElementName, string attrs,
			string elementName, IMoForm alt)
		{
			if (alt == null || alt.Form == null || alt.Form.StringCount == 0)
				return;
			if (!String.IsNullOrEmpty(wrappingElementName))
			{
				if (String.IsNullOrEmpty(attrs))
					w.WriteLine("<{0}>", wrappingElementName);
				else
					w.WriteLine("<{0} {1}>", wrappingElementName, attrs);
			}
			for (var i = 0; i < alt.Form.StringCount; ++i)
			{
				int ws;
				var tssForm = alt.Form.GetStringFromIndex(i, out ws);
				var sLang = m_cache.WritingSystemFactory.GetStrFromWs(ws);
				var internalPath = alt.Form.get_String(ws).Text;
				// a deleted audio link can leave an empty string - it's the file path
				// Chorus sometimes tells users "Report this problem to the developers" when missing this element
				// Users should refresh and try to send/receive again.
				if (!String.IsNullOrEmpty(internalPath))
					w.WriteLine("<{0} lang=\"{1}\"><text>{2}</text></{0}>", elementName,
						XmlUtils.MakeSafeXmlAttribute(sLang), XmlUtils.MakeSafeXml(GetFormWithMarkers(alt, ws)));
			}
			if (!String.IsNullOrEmpty(wrappingElementName))
				w.WriteLine("</{0}>", wrappingElementName);
		}

		private string GetFormWithMarkers(IMoForm alt, int ws)
		{
			if (IsVoiceWritingSystem(ws))
			{
				// The alternative contains a file path. We need to adjust and export and copy the file.
				// We also don't want to decorate the file name with any affix markers.
				// Form is MultiUnicode, so we don't need to check for a single run.
				var internalPath = alt.Form.get_String(ws).Text;
				// usually this will be unchanged, but it is pathologically possible that the file name conflicts.
				var writePath = ExportFile(internalPath,
					Path.Combine(DirectoryFinder.GetMediaDir(m_cache.LangProject.LinkedFilesRootDir), internalPath),
					"audio");
				return writePath;
			}
			return alt.GetFormWithMarkers(ws);
		}

		private void WriteAllForms(TextWriter w, string wrappingElementName, string attrs,
			string elementName, IMultiString multi)
		{
			if (multi == null || multi.StringCount == 0)
				return;
			if (!String.IsNullOrEmpty(wrappingElementName))
			{
				if (String.IsNullOrEmpty(attrs))
					w.WriteLine("<{0}>", wrappingElementName);
				else
					w.WriteLine("<{0} {1}>", wrappingElementName, attrs);
			}
			for (var i = 0; i < multi.StringCount; ++i)
			{
				int ws;
				var tssForm = multi.GetStringFromIndex(i, out ws);
				var sLang = m_cache.WritingSystemFactory.GetStrFromWs(ws);
				w.WriteLine("<{0} lang=\"{1}\"><text>{2}</text></{0}>", elementName,
					XmlUtils.MakeSafeXmlAttribute(sLang),
					ConvertTsStringToLiftXml(tssForm, ws));
			}
			if (!String.IsNullOrEmpty(wrappingElementName))
				w.WriteLine("</{0}>", wrappingElementName);
		}

		private void WriteString(TextWriter w, string wrappingElementName, string attrs,
			string elementName, ITsString tssForm)
		{
			if (tssForm == null || tssForm.Length == 0)
				return;
			if (!String.IsNullOrEmpty(wrappingElementName))
			{
				if (String.IsNullOrEmpty(attrs))
					w.WriteLine("<{0}>", wrappingElementName);
				else
					w.WriteLine("<{0} {1}>", wrappingElementName, attrs);
			}
			var ws = TsStringUtils.GetWsAtOffset(tssForm, 0);
			var sLang = m_cache.WritingSystemFactory.GetStrFromWs(ws);
			w.WriteLine("<{0} lang=\"{1}\"><text>{2}</text></{0}>", elementName,
				XmlUtils.MakeSafeXmlAttribute(sLang),
				ConvertTsStringToLiftXml(tssForm, ws));
			if (!String.IsNullOrEmpty(wrappingElementName))
				w.WriteLine("</{0}>", wrappingElementName);
		}

		private string ConvertTsStringToLiftXml(ITsString tssVal, int wsString)
		{
			var bldr = new StringBuilder();
			var crun = tssVal.RunCount;
			if (crun == 1)
			{
				if (IsVoiceWritingSystem(wsString))
				{
					// The alternative contains a file path or null. We need to adjust and export and copy the file.
					// The whole content of the representation of the TsString will be the adjusted file name,
					// since these WS alternatives never contain formatted data.
					var internalPath = tssVal.Text == null ? "" : tssVal.Text;
					// usually this will be unchanged, but it is pathologically possible that the file name conflicts.
					var writePath = ExportFile(internalPath,
						Path.Combine(DirectoryFinder.GetMediaDir(m_cache.LangProject.LinkedFilesRootDir), internalPath),
						"audio");
					return XmlUtils.MakeSafeXml(writePath);
				}
			}
			for (var irun = 0; irun < crun; ++irun)
			{
				int tpt;
				int nProp;
				int nVar;
				var ttp = tssVal.get_Properties(irun);
				var fSpan = true;
				if (ttp.IntPropCount == 1 && ttp.StrPropCount == 0)
				{
					nProp = ttp.GetIntProp(0, out tpt, out nVar);
					if (tpt == (int)FwTextPropType.ktptWs && nProp == wsString)
						fSpan = false;
				}
				if (fSpan)
				{
					bldr.Append("<span");
					var cprop = ttp.IntPropCount;
					for (var iprop = 0; iprop < cprop; ++iprop)
					{
						nProp = ttp.GetIntProp(iprop, out tpt, out nVar);
						if (tpt == (int)FwTextPropType.ktptWs && nProp != wsString)
						{
							var ws = m_wsManager.Get(nProp);
							bldr.AppendFormat(" lang=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(ws.Id));
						}
					}
					cprop = ttp.StrPropCount;
					for (var iprop = 0; iprop < cprop; ++iprop)
					{
						var sProp = ttp.GetStrProp(iprop, out tpt);
						switch (tpt)
						{
							case (int)FwTextPropType.ktptNamedStyle:
								bldr.AppendFormat(" class=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sProp));
								break;
							case (int)FwTextPropType.ktptObjData:
								AddObjPropData(bldr, sProp);
								break;
						}
					}
					bldr.Append(">");
				}
				var sRun = tssVal.get_RunText(irun);
				if (sRun != null)
					sRun = Icu.Normalize(sRun.Replace("\x2028", Environment.NewLine), Icu.UNormalizationMode.UNORM_NFC);
				bldr.Append(XmlUtils.MakeSafeXml(sRun));
				if (fSpan)
					bldr.Append("</span>");
			}
			return bldr.ToString();
		}

		private void AddObjPropData(StringBuilder bldr, string sProp)
		{
			if (!String.IsNullOrEmpty(sProp) && sProp[0] == Convert.ToChar((int)FwObjDataTypes.kodtExternalPathName))
			{
				var destination = sProp.Substring(1);
				if (destination.IndexOf(':') <= 1)
				{
					// it's not a URL, so assume it's a path to a file; we want to copy the file and adjust the link.
					// This is pulling in bits of knowledge from TsStringUtils.GetURL and VwBaseVc.DoHotLinkAction
					// but I don't see any good way to avoid it.
					var absPath = destination;
					if (!Path.IsPathRooted(destination))
						absPath = Path.Combine(m_cache.LangProject.LinkedFilesRootDir, destination);
					var writePath = ExportFile(destination, absPath, "others", DirectoryFinder.ksOtherLinkedFilesDir);
					// We force the file to be in the "others" directory, but in this case we include "others" in the URL,
					// so it will actually work as a URL relative to the LIFT file.
					bldr.AppendFormat(" href=\"file://others/{0}\"", XmlUtils.MakeSafeXmlAttribute(writePath.Replace('\\', '/')));
					return;
				}
			}

			var sRef = TsStringUtils.GetURL(sProp);
			if (!String.IsNullOrEmpty(sRef))
				bldr.AppendFormat(" href=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(sRef));
		}

		private bool IsVoiceWritingSystem(int wsString)
		{
			var wsEngine = m_wsManager.get_EngineOrNull(wsString);
			return wsEngine is WritingSystemDefinition && ((WritingSystemDefinition)wsEngine).IsVoice;
		}

		private void WriteTrait(TextWriter w, string sName, IMultiAccessorBase multi, int wsWant)
		{
			string sValue = null;
			if (wsWant > 0)
				sValue = multi.get_String(wsWant).Text;
			if (String.IsNullOrEmpty(sValue))
				sValue = multi.BestAnalysisVernacularAlternative.Text;
			if (sValue == "***" && wsWant <= 0)
				sValue = multi.get_String(m_wsEn).Text;
			w.WriteLine("<trait  name=\"{0}\" value=\"{1}\"/>", sName, XmlUtils.MakeSafeXmlAttribute(sValue));
		}

		private void WriteLiftDates(TextWriter w, ICmObject obj)
		{
			var dateCreated = GetProperty(obj, "LiftDateCreated") as string;
			if (!String.IsNullOrEmpty(dateCreated))
				w.Write(" dateCreated=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(dateCreated));
			var dateModified = GetProperty(obj, "LiftDateModified") as string;
			if (!String.IsNullOrEmpty(dateModified))
				w.Write(" dateModified=\"{0}\"", XmlUtils.MakeSafeXmlAttribute(dateModified));
		}

		private void WriteLiftResidue(TextWriter w, ICmObject obj)
		{
			var residue = GetProperty(obj, "LiftResidueContent") as string;
			if (!String.IsNullOrEmpty(residue))
				w.Write(residue);
		}

		protected object GetProperty(ICmObject target, string property)
		{
			if (target == null)
				return null;

			var fWantHvo = false;
			var type = target.GetType();
			var info = type.GetProperty(property, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			if (info == null && property.EndsWith(".Hvo"))
			{
				fWantHvo = true;
				var realprop = property.Substring(0, property.Length - 4);
				info = type.GetProperty(realprop, BindingFlags.Instance | BindingFlags.Public | BindingFlags.FlattenHierarchy);
			}
			if (info == null)
				throw new ConfigurationException("There is no public property named '" + property + "' in " + type + ". Remember, properties often end in a two-character suffix such as OA,OS,RA, or RS.");

			object result;
			try
			{
				result = info.GetValue(target, null);
				if (fWantHvo)
				{
					var hvo = 0;
					if (result != null)
						hvo = ((ICmObject)result).Hvo;
					return hvo > 0 ? hvo.ToString() : "0";
				}
			}
			catch (Exception error)
			{
				throw new ApplicationException(string.Format("There was an error while trying to get the property {0}. One thing that has caused this in the past has been a database which was not migrated properly.", property), error);
			}
			return result;
		}

		/// <summary>
		/// Write the .lift-ranges data into the string writer.
		/// <note>Does not write to a file, anymore.</note>
		/// </summary>
		public void ExportLiftRanges(StringWriter w)
		{
			w.WriteLine("<?xml version=\"1.0\" encoding=\"UTF-8\"?>");
			w.WriteLine("<!-- See http://code.google.com/p/lift-standard for more information on the format used here. -->");
			w.WriteLine("<lift-ranges>");
			WriteEtymologyRange(w);
			WriteGrammaticalInfoRange(w);
			WriteLexicalRelationRange(w);
			WriteNoteTypeRange(w);
			WriteParadigmRange(w);
			WriteReversalTypeRange(w);
			WriteSemanticDomainRange(w);
			WriteStatusRange(w);
			WriteUsersRange(w);
			WriteLocationRange(w);
			WriteAnthroCodeRange(w);
			WriteTranslationTypeRange(w);
			WriteExceptionFeatureRange(w);
			WriteInflectionFeatureRange(w);
			WriteInflectionFeatureTypeRange(w);
			WriteFromPartsOfSpeechRange(w);
			WriteMorphTypeRange(w);
			WriteFeatureValueRanges(w);
			WriteAffixSlotRanges(w);
			WriteInflectionClassRanges(w);
			WriteStemNameRanges(w);
			WriteAnyOtherRangesReferencedByFields(w);
			w.WriteLine("</lift-ranges>");
		}

		private void WriteAnyOtherRangesReferencedByFields(TextWriter w)
		{
			foreach (var possList in m_CmPossibilityListsReferencedByFields)
			{
				//We actually want to export any range which is referenced by a Custom field and is not already output.
				//not just Custom ranges.
				String rangeName;
				var gotRangeName = m_CmPossibilityListsReferencedByFields.TryGetValue(possList.Key, out rangeName);
				var customPossList = m_repoCmPossibilityLists.GetObject(possList.Key);
				w.WriteLine("<!-- This is a custom list or other list which is not output by default but if referenced in the data of a field.  -->");
				WritePossibilityListAsRange(w, rangeName, customPossList, customPossList.Guid.ToString());
			}
		}

		private static void WriteEtymologyRange(TextWriter w)
		{
			w.WriteLine("<range id=\"etymology\">");
			w.WriteLine("<range-element id=\"borrowed\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">borrowed</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">The word is borrowed from another language</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"proto\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">proto</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">The proto form of the word in another language</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("</range>");
		}

		private void WriteGrammaticalInfoRange(TextWriter w)
		{
			w.WriteLine("<range id=\"{0}\">", RangeNames.sPartsOfSpeechOA);
			w.WriteLine("<!-- These are all the parts of speech in the FLEx db, used or unused.  These are used as the basic grammatical-info values. -->");
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
				WritePartOfSpeechRangeElement(w, pos);
			w.WriteLine("</range>");
		}

		private void WritePartOfSpeechRangeElement(TextWriter w, IPartOfSpeech pos)
		{
			var liftId = pos.Name.BestAnalysisVernacularAlternative.Text;
			if (pos.Owner is IPartOfSpeech)
			{
				var liftIdOwner = ((IPartOfSpeech)(pos.Owner)).Name.BestAnalysisVernacularAlternative.Text;
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\" parent=\"{2}\">",
					XmlUtils.MakeSafeXmlAttribute(liftId), pos.Guid,
					XmlUtils.MakeSafeXmlAttribute(liftIdOwner));
			}
			else
			{
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(liftId), pos.Guid);
			}
			WriteAllForms(w, "label", null, "form", pos.Name);
			WriteAllForms(w, "abbrev", null, "form", pos.Abbreviation);
			WriteAllForms(w, "description", null, "form", pos.Description);
			foreach (var inflFeat in pos.InflectableFeatsRC)
				WriteTrait(w, "inflectable-feat", inflFeat.Abbreviation, m_wsBestAnalVern);
			w.WriteLine("</range-element>");
		}

		private void WriteLexicalRelationRange(TextWriter w)
		{
			w.WriteLine("<range id=\"{0}\">", RangeNames.sDbReferencesOA);
			w.WriteLine("<range-element id=\"ref\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">ref</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">General cross reference.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"main\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">main</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Reference to a main entry from a minor entry.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"isa\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">isa</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">The gen-spec relation where the special relates to the general.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"kindof\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">kindof</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">The kind-of relation in which the Sense is a kind of another sense.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"actor\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">actor</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">The actor of this verb</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"undergoer\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">undergoer</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">The undergoer of this verb</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"component\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">component</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">This word is grammatically built from these components.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<!--The following range elements were added by FieldWorks Language Explorer. -->");
			if (m_cache.LangProject.LexDbOA.ReferencesOA != null)
			{
				foreach (var refer in m_cache.LangProject.LexDbOA.ReferencesOA.ReallyReallyAllPossibilities.OfType<ILexRefType>())
					WriteLexRefType(w, refer);
			}
			w.WriteLine("<range-element id=\"subentry\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">subentry</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Reference to a subentry from a main entry.  This is a backreference in FLEX.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"minorentry\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">minorentry</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Reference to a minor entry from a main entry.  This is a backreference in FLEX.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("</range>");
		}

		private void WriteLexRefType(TextWriter w, ILexRefType refer)
		{
			var liftId = refer.Name.BestAnalysisVernacularAlternative.Text;
			if (refer.Owner is ILexRefType)
			{
				var liftIdOwner = ((ILexRefType)refer.Owner).Name.BestAnalysisVernacularAlternative.Text;
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\" parent=\"{2}\">",
					XmlUtils.MakeSafeXmlAttribute(liftId), refer.Guid,
					XmlUtils.MakeSafeXmlAttribute(liftIdOwner));
			}
			else
			{
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(liftId), refer.Guid);
			}
			WriteAllForms(w, "label", null, "form", refer.Name);
			WriteAllForms(w, "abbrev", null, "form", refer.Abbreviation);
			WriteAllForms(w, "description", null, "form", refer.Description);
			WriteAllForms(w, "field", "type=\"reverse-label\"", "form", refer.ReverseName);
			WriteAllForms(w, "field", "type=\"reverse-abbrev\"", "form", refer.ReverseAbbreviation);
			w.WriteLine("</range-element>");
		}

		static void WriteNoteTypeRange(TextWriter w)
		{
			w.WriteLine("<range id=\"note-type\">");
			w.WriteLine("<!-- The following elements are defined by the LIFT standard. -->");
			w.WriteLine("<range-element id=\"anthropology\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">anthropology</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives anthropological information.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"biblography\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">biblography</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Biblographic information.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"comment\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">comment</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">This note is an arbitrary comment not for publication</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"discourse\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">discourse</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives discourse information about a sense.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"encyclopedic\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">encyclopedic</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">This note gives encyclopedic information.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"general\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">general</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">General notes that do not fall in another clear category</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"grammar\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">grammar</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives grammatical information about a word.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"phonology\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">phonology</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives phonological information about a word.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"questions\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">questions</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Contains questions yet to be answered</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"restrictions\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">restrictions</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives information on the restriction of usage of a word.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"scientific-name\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">scientific name</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives the scientific name of a sense</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"sociolinguistics\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">sociolinguistics</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives sociolinguistic information about a sense.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"source\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">source</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Contains information on sources</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"usage\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">usage</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives information on usage</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<!-- The following elements are added here for use by FLEX. -->");
			w.WriteLine("<range-element id=\"literal-meaning\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">literal-meaning</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives the literal meaning of a word.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"semantics\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">semantics</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives semantic information about a sense.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"summary-definition\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">summary-definition</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">Gives a summary definition of a word.</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("</range>");
		}

		void WriteParadigmRange(TextWriter w)
		{
			w.WriteLine("<range id=\"paradigm\">");
			w.WriteLine("<range-element id=\"1d\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">1d</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">1st dual</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"1e\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">1e</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">1st exclusive</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"1i\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">1i</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">1st inclusive</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"1p\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">1p</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">1st person plural</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"1s\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">1s</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">1st person singular</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"2d\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">2d</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">2nd dual</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"2p\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">2p</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">2nd plural</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"2s\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">2s</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">2nd singular</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"3d\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">3d</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">3rd dual</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"3p\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">3p</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">3rd plural</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"3s\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">3s</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">3rd singular</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"non\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">non</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">-dual non-human or inanimate dual</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"non\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">non</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">-plural non-human or inanimate plural</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"non\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">non</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">-sing non-human or inanimate singulare</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"plural\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">plural</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">plural form</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"redup\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">redup</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">reduplication form</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("<range-element id=\"sing\">");
			w.WriteLine("<label><form lang=\"en\"><element name=\"text\">sing</element></form></label>");
			w.WriteLine("<description><form lang=\"en\"><element name=\"text\">singular</element></form></description>");
			w.WriteLine("</range-element>");
			w.WriteLine("</range>");
		}

		void WriteReversalTypeRange(TextWriter w)
		{
			if (m_cache.LangProject.LexDbOA.ReversalIndexesOC.Count == 0)
				return;
			w.WriteLine("<range id=\"{0}\">", RangeNames.sReversalType);
			foreach (var index in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				w.WriteLine("<range-element id=\"{0}\">", XmlUtils.MakeSafeXmlAttribute(index.WritingSystem));
				WriteAllForms(w, "label", null, "form", index.Name);
				WriteAllForms(w, "description", null, "form", index.Description);
				w.WriteLine("</range-element>");
			}
			w.WriteLine("</range>");
		}

		void WriteSemanticDomainRange(TextWriter w)
		{
			if (m_cache.LangProject.SemanticDomainListOA == null || m_cache.LangProject.SemanticDomainListOA.PossibilitiesOS.Count == 0)
				return;
			w.WriteLine("<range id=\"{0}\">", RangeNames.sSemanticDomainListOA);
			foreach (var sem in m_cache.LangProject.SemanticDomainListOA.ReallyReallyAllPossibilities)
			{
				var liftId = GetProperty(sem, "LiftAbbrAndName") as string;
				string liftIdOwner = null;
				if (sem.OwningFlid == CmPossibilityTags.kflidSubPossibilities)
					liftIdOwner = GetProperty(sem.Owner, "LiftAbbrAndName") as string;
				WritePossibilityRangeElement(w, sem, liftId, liftIdOwner);
			}
			w.WriteLine("</range>");
		}

		private void WritePossibilityListAsRange(TextWriter w, string rangeId, ICmPossibilityList list, string guid)
		{
			if (list.PossibilitiesOS.Count == 0)
				return;
			if (String.IsNullOrEmpty(guid))					//only output the guid for custom lists
				w.WriteLine("<range id=\"{0}\">", rangeId);
			else
				w.WriteLine("<range id=\"{0}\" guid=\"{1}\">", rangeId, XmlUtils.MakeSafeXmlAttribute(guid));
			foreach (var poss in list.ReallyReallyAllPossibilities)
			{
				var liftId = poss.Name.BestAnalysisVernacularAlternative.Text;
				string liftIdParent = null;
				if (poss.OwningFlid == CmPossibilityTags.kflidSubPossibilities)
					liftIdParent = ((ICmPossibility)(poss.Owner)).Name.BestAnalysisVernacularAlternative.Text;
				WritePossibilityRangeElement(w, poss, liftId, liftIdParent);
			}
			w.WriteLine("</range>");
		}

		private void WritePossibilityRangeElement(TextWriter w, ICmPossibility poss, string liftId, string liftIdParent)
		{
			if (poss.OwningFlid == CmPossibilityTags.kflidSubPossibilities)
			{
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\" parent=\"{2}\">",
					XmlUtils.MakeSafeXmlAttribute(liftId), poss.Guid,
					XmlUtils.MakeSafeXmlAttribute(liftIdParent));
			}
			else
			{
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(liftId), poss.Guid);
			}
			WriteAllForms(w, "label", null, "form", poss.Name);
			WriteAllForms(w, "abbrev", null, "form", poss.Abbreviation);
			WriteAllForms(w, "description", null, "form", poss.Description);
			w.WriteLine("</range-element>");
		}

		void WriteStatusRange(TextWriter w)
		{
			if (m_cache.LangProject.StatusOA == null)
				return;
			WritePossibilityListAsRange(w, RangeNames.sStatusOA, m_cache.LangProject.StatusOA, null);
		}

		void WriteUsersRange(TextWriter w)
		{
			if (m_cache.LangProject.PeopleOA == null || m_cache.LangProject.PeopleOA.PossibilitiesOS.Count == 0)
				return;
			w.WriteLine("<range id=\"{0}\">", RangeNames.sPeopleOA);
			foreach (var person in m_cache.LangProject.PeopleOA.ReallyReallyAllPossibilities.OfType<ICmPerson>())
			{
				if (!person.IsResearcher)
					continue;
				var liftId = person.Name.BestAnalysisVernacularAlternative.Text;
				string liftIdParent = null;
				if (person.OwningFlid == CmPossibilityTags.kflidSubPossibilities)
					liftIdParent = ((ICmPossibility)(person.Owner)).Name.BestAnalysisVernacularAlternative.Text;
				WritePossibilityRangeElement(w, person, liftId, liftIdParent);
			}
			w.WriteLine("</range>");
		}

		void WriteLocationRange(TextWriter w)
		{
			if (m_cache.LangProject.LocationsOA == null)
				return;
			WritePossibilityListAsRange(w, RangeNames.sLocationsOA, m_cache.LangProject.LocationsOA, null);
		}

		void WriteAnthroCodeRange(TextWriter w)
		{
			if (m_cache.LangProject.AnthroListOA == null || m_cache.LangProject.AnthroListOA.PossibilitiesOS.Count == 0)
				return;
			w.WriteLine("<range id=\"{0}\">", RangeNames.sAnthroListOA);
			foreach (var poss in m_cache.LangProject.AnthroListOA.ReallyReallyAllPossibilities)
			{
				var liftId = poss.Abbreviation.BestAnalysisVernacularAlternative.Text;
				string liftIdParent = null;
				if (poss.OwningFlid == CmPossibilityTags.kflidSubPossibilities)
					liftIdParent = ((ICmPossibility)(poss.Owner)).Abbreviation.BestAnalysisVernacularAlternative.Text;
				WritePossibilityRangeElement(w, poss, liftId, liftIdParent);
			}
			w.WriteLine("</range>");
		}

		void WriteTranslationTypeRange(TextWriter w)
		{
			if (m_cache.LangProject.TranslationTagsOA == null)
				return;
			WritePossibilityListAsRange(w, RangeNames.sTranslationTagsOA, m_cache.LangProject.TranslationTagsOA, null);
		}

		void WriteExceptionFeatureRange(TextWriter w)
		{
			if (m_cache.LangProject.MorphologicalDataOA == null || m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA == null)
				return;
			WritePossibilityListAsRange(w, RangeNames.sProdRestrictOA, m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA, null);
		}

		void WriteInflectionFeatureRange(TextWriter w)
		{
			if (m_cache.LangProject.MsFeatureSystemOA == null || m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.Count == 0)
				return;
			w.WriteLine("<range id=\"{0}\">", RangeNames.sMSAinflectionFeature);
			foreach (var featDefn in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC)
			{
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(featDefn.Abbreviation.BestAnalysisVernacularAlternative.Text),
					featDefn.Guid);
				WriteAllForms(w, "label", null, "form", featDefn.Name);
				WriteAllForms(w, "abbrev", null, "form", featDefn.Abbreviation);
				WriteAllForms(w, "description", null, "form", featDefn.Description);
				WriteAllForms(w, "field", "type=\"gloss-abbreviation\"", "form", featDefn.GlossAbbreviation);
				WriteAllForms(w, "field", "type=\"right-gloss-sep\"", "form", featDefn.RightGlossSep);
				w.WriteLine("<trait name=\"catalog-source-id\" value=\"{0}\"/>",
					XmlUtils.MakeSafeXmlAttribute(featDefn.CatalogSourceId));
				w.WriteLine("<trait name=\"display-to-right\" value=\"{0}\"/>", featDefn.DisplayToRightOfValues);
				w.WriteLine("<trait name=\"show-in-gloss\" value=\"{0}\"/>", featDefn.ShowInGloss);
				switch (featDefn.ClassID)
				{
					case FsClosedFeatureTags.kClassId:
						var closed = (IFsClosedFeature)featDefn;
						foreach (var value in closed.ValuesOC)
						{
							var name = String.Format("{0}-feature-value",
								closed.Abbreviation.BestAnalysisVernacularAlternative.Text);
							WriteTrait(w, name, value.Abbreviation, m_wsBestAnalVern);
						}
						w.WriteLine("<trait name=\"feature-definition-type\" value=\"closed\"/>");
						break;
					case FsComplexFeatureTags.kClassId:
						var complex = (IFsComplexFeature)featDefn;
						if (complex.TypeRA != null)
							WriteTrait(w, "type", complex.TypeRA.Abbreviation, m_wsBestAnalVern);
						w.WriteLine("<trait name=\"feature-definition-type\" value=\"complex\"/>");
						break;
					case FsOpenFeatureTags.kClassId:
						var open = (IFsOpenFeature)featDefn;
						if (open.WsSelector != 0)
							w.WriteLine("<trait name=\"ws-selector\" value=\"{0}\"/>", open.WsSelector);
						if (!String.IsNullOrEmpty(open.WritingSystem))
							w.WriteLine("<trait name=\"writing-system\" value=\"{0}\"/>",
								XmlUtils.MakeSafeXmlAttribute(open.WritingSystem));
						w.WriteLine("<trait name=\"feature-definition-type\" value=\"open\"/>");
						break;
				}
				w.WriteLine("</range-element>");
			}
			w.WriteLine("</range>");
		}

		void WriteInflectionFeatureTypeRange(TextWriter w)
		{
			if (m_cache.LangProject.MsFeatureSystemOA == null || m_cache.LangProject.MsFeatureSystemOA.TypesOC.Count == 0)
				return;
			w.WriteLine("<range id=\"{0}\">", RangeNames.sMSAinflectionFeatureType);
			foreach (var type in m_cache.LangProject.MsFeatureSystemOA.TypesOC)
			{
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(type.Abbreviation.BestAnalysisVernacularAlternative.Text),
					type.Guid);
				WriteAllForms(w, "label", null, "form", type.Name);
				WriteAllForms(w, "abbrev", null, "form", type.Abbreviation);
				WriteAllForms(w, "description", null, "form", type.Description);
				w.WriteLine("<trait name=\"catalog-source-id\" value=\"{0}\"/>",
					XmlUtils.MakeSafeXmlAttribute(type.CatalogSourceId));
				foreach (var feat in type.FeaturesRS)
				{
					w.WriteLine("<trait name=\"feature\" value=\"{0}\"/>",
						XmlUtils.MakeSafeXmlAttribute(feat.Abbreviation.BestAnalysisVernacularAlternative.Text));
				}
				w.WriteLine("</range-element>");
			}
			w.WriteLine("</range>");
		}

		void WriteFromPartsOfSpeechRange(TextWriter w)
		{
			w.WriteLine("<!-- The parts of speech are duplicated in another range because derivational affixes require a \"From\" PartOfSpeech as well as a \"To\" PartOfSpeech. -->");
			w.WriteLine("<range id=\"from-part-of-speech\">");
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
				WritePartOfSpeechRangeElement(w, pos);
			w.WriteLine("</range>");
		}

		void WriteMorphTypeRange(TextWriter w)
		{
			w.WriteLine("<range id=\"morph-type\">");
			foreach (var type in m_cache.LangProject.LexDbOA.MorphTypesOA.PossibilitiesOS.OfType<IMoMorphType>())
			{
				var liftId = type.Name.get_String(m_wsEn).Text;
				if (String.IsNullOrEmpty(liftId))
					liftId = type.Name.BestAnalysisVernacularAlternative.Text;
				w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">", liftId, type.Guid);
				WriteAllForms(w, "label", null, "form", type.Name);
				WriteAllForms(w, "abbrev", null, "form", type.Abbreviation);
				WriteAllForms(w, "description", null, "form", type.Description);
				if (type.Prefix != null)
				{
					w.WriteLine("<trait name=\"leading-symbol\" value=\"{0}\"/>",
						XmlUtils.MakeSafeXmlAttribute(type.Prefix));
				}
				if (type.Postfix != null)
				{
					w.WriteLine("<trait name=\"trailing-symbol\" value=\"{0}\"/>",
						XmlUtils.MakeSafeXmlAttribute(type.Postfix));
				}
				w.WriteLine("</range-element>");
			}
			w.WriteLine("</range>");
		}

		void WriteFeatureValueRanges(TextWriter w)
		{
			if (m_cache.LangProject.MsFeatureSystemOA == null)
				return;
			foreach (var feat in m_cache.LangProject.MsFeatureSystemOA.FeaturesOC.OfType<IFsClosedFeature>())
			{
				if (feat.ValuesOC.Count == 0)
					continue;
				w.WriteLine("<range id=\"{0}-feature-value\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(feat.Abbreviation.BestAnalysisVernacularAlternative.Text),
					feat.Guid);
				foreach (var value in feat.ValuesOC)
				{
					w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
						XmlUtils.MakeSafeXmlAttribute(value.Abbreviation.BestAnalysisVernacularAlternative.Text),
						value.Guid);
					WriteAllForms(w, "label", null, "form", value.Name);
					WriteAllForms(w, "abbrev", null, "form", value.Abbreviation);
					WriteAllForms(w, "description", null, "form", value.Description);
					WriteAllForms(w, "field", "type=\"gloss-abbrev\"", "form", value.GlossAbbreviation);
					WriteAllForms(w, "field", "type=\"right-gloss-sep\"", "form", value.RightGlossSep);
					w.WriteLine("<trait name=\"catalog-source-id\" value=\"{0}\"/>",
						XmlUtils.MakeSafeXmlAttribute(value.CatalogSourceId));
					w.WriteLine("<trait name=\"show-in-gloss\" value=\"{0}\"/>", value.ShowInGloss);
					w.WriteLine("</range-element>");
				}
				w.WriteLine("</range>");
			}
		}

		void WriteAffixSlotRanges(TextWriter w)
		{
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
			{
				if (pos.AffixSlotsOC.Count == 0)
					continue;
				w.WriteLine("<range id=\"{0}-slot\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(pos.Name.BestAnalysisVernacularAlternative.Text), pos.Guid);
				foreach (var slot in pos.AffixSlotsOC)
				{
					w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
						XmlUtils.MakeSafeXmlAttribute(slot.Name.BestAnalysisVernacularAlternative.Text), slot.Guid);
					WriteAllForms(w, "label", null, "form", slot.Name);
					WriteAllForms(w, "description", null, "form", slot.Description);
					if (slot.Optional)
						w.WriteLine("<trait name=\"optional\" value=\"{0}\"/>", slot.Optional);
					w.WriteLine("</range-element>");
				}
				w.WriteLine("</range>");
			}
		}

		void WriteInflectionClassRanges(TextWriter w)
		{
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
			{
				if (pos.InflectionClassesOC.Count == 0)
					continue;
				w.WriteLine("<range id=\"{0}-infl-class\" guid=\"{1}\">",
					XmlUtils.MakeSafeXmlAttribute(pos.Name.BestAnalysisVernacularAlternative.Text), pos.Guid);
				foreach (var inflClass in pos.InflectionClassesOC)
				{
					var liftId = inflClass.Name.BestAnalysisVernacularAlternative.Text;
					if (inflClass.OwningFlid == MoInflClassTags.kflidSubclasses)
					{
						var liftIdParent = ((IMoInflClass)(inflClass.Owner)).Name.BestAnalysisVernacularAlternative.Text;
						w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\" parent=\"{2}\">",
							XmlUtils.MakeSafeXmlAttribute(liftId), inflClass.Guid,
							XmlUtils.MakeSafeXmlAttribute(liftIdParent));
					}
					else
					{
						w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
							XmlUtils.MakeSafeXmlAttribute(liftId), inflClass.Guid);
					}
					WriteAllForms(w, "label", null, "form", inflClass.Name);
					WriteAllForms(w, "abbrev", null, "form", inflClass.Abbreviation);
					WriteAllForms(w, "description", null, "form", inflClass.Description);
					w.WriteLine("</range-element>");
				}
				w.WriteLine("</range>");
			}
		}

		void WriteStemNameRanges(TextWriter w)
		{
			foreach (var pos in m_cache.LangProject.AllPartsOfSpeech)
			{
				if (pos.StemNamesOC.Count == 0)
					continue;
				w.WriteLine("<range id=\"{0}-stem-name\" guid=\"{1}\">",
							XmlUtils.MakeSafeXmlAttribute(pos.Name.BestAnalysisVernacularAlternative.Text), pos.Guid);
				foreach (var stem in pos.StemNamesOC)
				{
					w.WriteLine("<range-element id=\"{0}\" guid=\"{1}\">",
						XmlUtils.MakeSafeXmlAttribute(stem.Name.BestAnalysisVernacularAlternative.Text), stem.Guid);
					WriteAllForms(w, "label", null, "form", stem.Name);
					WriteAllForms(w, "abbrev", null, "form", stem.Abbreviation);
					WriteAllForms(w, "description", null, "form", stem.Description);
					foreach (var region in stem.RegionsOC)
					{
						w.WriteLine("<trait name=\"feature-set\" value=\"{0}\"/>",
							XmlUtils.MakeSafeXmlAttribute(region.LiftName));
					}
					w.WriteLine("</range-element>");
				}
				w.WriteLine("</range>");
			}
		}

		private Dictionary<Guid, String> GetCmPossibilityListsReferencedByFields(IEnumerable<ILexEntry> entries)
		{
			var cmPossibilityListsReferenced = new Dictionary<Guid, String>();

			foreach (var entry in entries)
			{
				GetListsNotAddedYet(entry, cmPossibilityListsReferenced);
				foreach (var sense in entry.SensesOS)
				{
					GetListsNotAddedYet(sense, cmPossibilityListsReferenced);
					foreach (var example in sense.ExamplesOS)
					{
						GetListsNotAddedYet(example, cmPossibilityListsReferenced);
					}
				}
				foreach (var allomorph in entry.AlternateFormsOS)
				{
					GetListsNotAddedYet(allomorph, cmPossibilityListsReferenced);
				}
			}
			return cmPossibilityListsReferenced;
		}

		private void GetListsNotAddedYet(ICmObject obj, Dictionary<Guid, String> cmPossibilityListsReferenced)
		{
			var cmPossibilityListGuidsTemp = GetCmPossibiityListsObjectReferences(obj);
			foreach (var possList in cmPossibilityListGuidsTemp)
			{
				if (!cmPossibilityListsReferenced.ContainsKey(possList.Key))
				{
					cmPossibilityListsReferenced.Add(possList.Key, possList.Value);
				}
			}
		}

		private Dictionary<Guid, String> GetCmPossibiityListsObjectReferences(ICmObject obj)
		{
			var cmPossibilityListsReferencedByFields = new Dictionary<Guid, String>();
			foreach (var flid in m_mdc.GetFields(obj.ClassID, true, (int)CellarPropertyTypeFilter.All))
			{
				var type = (CellarPropertyType) m_mdc.GetFieldType(flid);

				//First of all only fields which have the following types will have references to data in lists.
				if ((type == CellarPropertyType.ReferenceAtomic ||
					 type == CellarPropertyType.ReferenceCollection ||
					 type == CellarPropertyType.ReferenceSequence))
				{
					var fieldName = m_mdc.GetFieldName(flid);
					var fieldDstClsName = m_mdc.GetDstClsName(flid);

					if ((fieldDstClsName.Equals("CmPossibility") ||
						 fieldDstClsName.Equals("CmSemanticDomain") ||
						 fieldDstClsName.Equals("CmAnthroItem")))
					{
						switch (type)
						{
							case CellarPropertyType.ReferenceAtomic:
								{
									var possibilityHvo = m_sda.get_ObjectProp(obj.Hvo, flid);
									AddPossListRefdByField(possibilityHvo, cmPossibilityListsReferencedByFields);
								}
								break;
							case CellarPropertyType.ReferenceCollection:
							case CellarPropertyType.ReferenceSequence:
								{
									var hvos = m_sda.VecProp(obj.Hvo, flid);
									if (hvos.Length > 0)
									{
										AddPossListRefdByField(hvos[0], cmPossibilityListsReferencedByFields);
									}
								}
								break;
							default:
								break;
						}
					}
				}
			}
			return cmPossibilityListsReferencedByFields;
		}

		private void AddPossListRefdByField(int possHvo, Dictionary<Guid, string> cmPossibilityListsReferencedByFields)
		{
			var repoCmPossibilities = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
			ICmPossibility poss;
			if (repoCmPossibilities.TryGetObject(possHvo, out poss))
			{
				var possList = poss.OwningList;
				var possListGuid = possList.Guid;
				var rangeName = RangeNames.GetRangeNameForLiftExport(m_mdc, possList);
				if (!cmPossibilityListsReferencedByFields.ContainsKey(possListGuid))
				{
					cmPossibilityListsReferencedByFields.Add(possListGuid, XmlUtils.MakeSafeXml(rangeName));
				}
			}
		}

		private void MapCmPossibilityListGuidsToLiftRangeNames(Dictionary<Guid, String> cmPossibilityListsReferencedByFields)
		{
			MapGuidsToStandardLiftRanges();

			//First remove items from m_CmPossibilityListsReferencedByCustomFields which are in the group
			//of ranges which are going to be output by default so that they are not output twice.
			foreach (var standardRangesOutput in m_ListsGuidToRangeName)
			{
				if (cmPossibilityListsReferencedByFields.ContainsKey(standardRangesOutput.Key))
				{
					cmPossibilityListsReferencedByFields.Remove(standardRangesOutput.Key);
				}
			}

			//Now include the remaining lists which need to be output. That is ones which are referenced
			//by a custom field and which are not already included yet.
			foreach (var list in cmPossibilityListsReferencedByFields)
			{
				m_ListsGuidToRangeName.Add(list.Key, XmlUtils.MakeSafeXml(list.Value));
			}
		}

		private void MapGuidsToStandardLiftRanges()
		{
			MapGuidToRange(m_cache.LangProject.SemanticDomainListOA, RangeNames.sSemanticDomainListOA);
			MapGuidToRange(m_cache.LangProject.StatusOA, RangeNames.sStatusOA);
			MapGuidToRange(m_cache.LangProject.PeopleOA, RangeNames.sPeopleOA);
			MapGuidToRange(m_cache.LangProject.LocationsOA, RangeNames.sLocationsOA);
			MapGuidToRange(m_cache.LangProject.AnthroListOA, RangeNames.sAnthroListOA);
			MapGuidToRange(m_cache.LangProject.TranslationTagsOA, RangeNames.sTranslationTagsOA);
			MapGuidToRange(m_cache.LangProject.MorphologicalDataOA.ProdRestrictOA, RangeNames.sProdRestrictOA);
			MapGuidToRange(m_cache.LangProject.LexDbOA.MorphTypesOA, RangeNames.sDbMorphTypesOA);
		}

		private void MapGuidToRange(ICmPossibilityList possList, string rangeName)
		{
			if (possList == null || m_ListsGuidToRangeName.ContainsKey(possList.Guid))
				return;
			m_ListsGuidToRangeName.Add(possList.Guid, rangeName);
		}
	}

	public static class RangeNames
	{
		/// <summary> </summary>
		public const string sAffixCategoriesOA = "affix-categories";

		/// <summary> </summary>
		public const string sAnnotationDefsOA = "annotation-definitions";

		/// <summary> </summary>
		public const string sAnthroListOAold1 = "anthro_codes";
		/// <summary> </summary>
		public const string sAnthroListOA = "anthro-code";

		/// <summary> </summary>
		public const string sConfidenceLevelsOA = "confidence-levels";

		/// <summary> </summary>
		public const string sEducationOA = "education";

		/// <summary> </summary>
		public const string sGenreListOA = "genres";

		/// <summary> </summary>
		public const string sLocationsOA = "location";

		/// <summary> </summary>
		public const string sPartsOfSpeechOA = "grammatical-info";

		/// <summary> </summary>
		public const string sPartsOfSpeechOAold2 = "FromPartOfSpeech";
		/// <summary> </summary>
		public const string sPartsOfSpeechOAold1 = "from-part-of-speech";

		/// <summary> </summary>
		public const string sPeopleOA = "users";

		/// <summary> </summary>
		public const string sPositionsOA = "positions";

		/// <summary> </summary>
		public const string sRestrictionsOA = "restrictions";

		/// <summary> </summary>
		public const string sRolesOA = "roles";

		/// <summary> </summary>
		public const string sSemanticDomainListOAold1 = "semanticdomainddp4";
		/// <summary> </summary>
		public const string sSemanticDomainListOAold2 = "semantic_domain";
		/// <summary> </summary>
		public const string sSemanticDomainListOAold3 = "semantic-domain";
		/// <summary> </summary>
		public const string sSemanticDomainListOA = "semantic-domain-ddp4";

		/// <summary> </summary>
		public const string sStatusOA = "status";

		/// <summary> </summary>
		public const string sThesaurusRA = "thesaurus";

		/// <summary> </summary>
		public const string sTranslationTagsOAold1 = "translation-types";
		/// <summary> </summary>
		public const string sTranslationTagsOA = "translation-type";

		//=========================================================================================
		/// <summary> </summary>
		public const string sProdRestrictOA = "exception-feature";
		/// <summary> </summary>
		public const string sProdRestrictOAfrom = "from-exception-feature";

		//=========================================================================================
		//lists under m_cache.LangProject.LexDbOA
		/// <summary> </summary>
		public const string sDbComplexEntryTypesOA = "complex-form-types";

		/// <summary> </summary>
		public const string sDbDomainTypesOA = "domain-type";
		/// <summary> </summary>
		public const string sDbDomainTypesOAold1 = "domaintype";

		/// <summary> </summary>
		public const string sDbMorphTypesOAold = "MorphType";
		/// <summary> </summary>
		public const string sDbMorphTypesOA = "morph-type";

		/// <summary> </summary>
		public const string sDbPublicationTypesOA = "publishin";

		/// <summary> </summary>
		public const string sDbReferencesOAold = "lexical-relations";
		/// <summary> </summary>
		public const string sDbReferencesOA = "lexical-relation";

		/// <summary> </summary>
		public const string sDbSenseTypesOA = "sense-type";
		/// <summary> </summary>
		public const string sDbSenseTypesOAold1 = "sensetype";

		/// <summary> </summary>
		public const string sDbUsageTypesOAold = "usagetype";
		/// <summary> </summary>
		public const string sDbUsageTypesOA = "usage-type";

		/// <summary> </summary>
		public const string sDbVariantEntryTypesOA = "variant-types";


		//=====================EXTRA RANGES NOT  CmPossibilityLists============================
		/// <summary> </summary>
		public const string sMSAinflectionFeature = "inflection-feature";
		/// <summary> </summary>
		public const string sMSAfromInflectionFeature = "from-inflection-feature";
		/// <summary> </summary>
		public const string sMSAinflectionFeatureType = "inflection-feature-type";

		/// <summary> </summary>
		public const string sReversalType = "reversal-type";


		/// <summary>
		/// Return the LIFT range name for a given cmPossibilityList. Get the fieldName
		/// of the owning field and use that to get the range name.
		/// </summary>
		/// <returns></returns>
		public static string GetRangeNameForLiftExport(IFwMetaDataCacheManaged mdc, ICmPossibilityList list)
		{
			string rangeName = null;
			if (list.OwningFlid == 0)
			{
				rangeName = list.Name.BestAnalysisVernacularAlternative.Text;
			}
			else
			{
				var fieldName = mdc.GetFieldName(list.OwningFlid);
				rangeName = GetRangeName(fieldName);
			}
			return rangeName;
		}

		/// <summary>
		/// Return the LIFT range name for a given fieldName in Flex
		/// </summary>
		/// <param name="fieldName"></param>
		/// <returns></returns>
		public static string GetRangeName(string fieldName)
		{
			string rangeName = null;

			switch (fieldName)
			{
				case "AffixCategories": rangeName = sAffixCategoriesOA; break;
				case "AnnotationDefs": rangeName = sAnnotationDefsOA; break;

				case "AnthroList": rangeName = sAnthroListOA; break;

				case "ConfidenceLevels": rangeName = sConfidenceLevelsOA; break;

				case "Education": rangeName = sEducationOA; break;

				case "GenreList": rangeName = sGenreListOA; break;

				case "Locations": rangeName = sLocationsOA; break;

				case "PartsOfSpeech": rangeName = sPartsOfSpeechOA; break;

				case "People": rangeName = sPeopleOA; break;

				case "Positions": rangeName = sPositionsOA; break;

				case "Restrictions": rangeName = sRestrictionsOA; break;

				case "Roles": rangeName = sRolesOA; break;

				case "SemanticDomainList": rangeName = sSemanticDomainListOA; break;

				case "Status": rangeName = sStatusOA; break;

				case "Thesaurus": rangeName = sThesaurusRA; break;

				case "TranslationTags": rangeName = sTranslationTagsOA; break;

				////=========================================================================================
				//case  "": rangeName = sProdRestrictOA = "exception-feature"; break;
				//case  "": rangeName = sProdRestrictOAfrom = "from-exception-feature"; break;

				////=========================================================================================
				////lists under m_cache.LangProject.LexDbOA

				case "ComplexEntryTypes": rangeName = sDbComplexEntryTypesOA; break;

				case "DomainTypes": rangeName = sDbDomainTypesOA; break;

				case "MorphTypes": rangeName = sDbMorphTypesOA; break;

				case "PublicationTypes": rangeName = sDbPublicationTypesOA; break;

				case "References": rangeName = sDbReferencesOA; break;

				case "SenseTypes": rangeName = sDbSenseTypesOA; break;

				case "UsageTypes": rangeName = sDbUsageTypesOA; break;

				case "VariantEntryTypes": rangeName = sDbVariantEntryTypesOA; break;

				////=====================EXTRA RANGES NOT  CmPossibilityLists============================
				//case  "": rangeName = sMSAinflectionFeature = "inflection-feature"; break;
				//case  "": rangeName = sMSAinflectionFeatureType = "inflection-feature-type"; break;
				//case  "": rangeName = sReversalType = "reversal-type"; break;

				//============================================================================================
				default:
					rangeName = fieldName.ToLowerInvariant();
					break;
			}
			return rangeName;
		}

		public static bool RangeNameIsCustomList(string range)
		{
			switch (range)
			{
				case sAffixCategoriesOA:
				case sAnnotationDefsOA:
				case sAnthroListOAold1:
				case sAnthroListOA:
				case sConfidenceLevelsOA:
				case sEducationOA:
				case sGenreListOA:
				case sLocationsOA:
				case sPartsOfSpeechOA:
				case sPartsOfSpeechOAold2:
				case sPartsOfSpeechOAold1:
				case sPeopleOA:
				case sPositionsOA:
				case sRestrictionsOA:
				case sRolesOA:
				case sSemanticDomainListOAold1:
				case sSemanticDomainListOAold2:
				case sSemanticDomainListOAold3:
				case sSemanticDomainListOA:
				case sStatusOA:
				case sThesaurusRA:
				case sTranslationTagsOAold1:
				case sTranslationTagsOA:
				//=========================================================================================
				case sProdRestrictOA:
				case sProdRestrictOAfrom:
				//=========================================================================================
				//lists under m_cache.LangProject.LexDbOA
				case sDbComplexEntryTypesOA:
				case sDbDomainTypesOA:
				case sDbDomainTypesOAold1:
				case sDbMorphTypesOAold:
				case sDbMorphTypesOA:
				case sDbPublicationTypesOA:
				case sDbReferencesOAold:
				case sDbReferencesOA:
				case sDbSenseTypesOA:
				case sDbSenseTypesOAold1:
				case sDbUsageTypesOAold:
				case sDbUsageTypesOA:
				case sDbVariantEntryTypesOA:
				//=====================EXTRA RANGES NOT  CmPossibilityLists============================
				case sMSAinflectionFeature:
				case sMSAfromInflectionFeature:
				case sMSAinflectionFeatureType:
				case sReversalType:
				case "dialect":
				case "etymology":
				case "note-type":
				case "paradigm":
					return false;
				default:
					if (range.EndsWith("-slot") || range.EndsWith("-Slots") ||
						range.EndsWith("-infl-class") || range.EndsWith("-InflClasses") ||
						range.EndsWith("-feature-value") || range.EndsWith("-stem-name"))
					{
						return false;
					}
					return true;
			}
		}
	}
}
