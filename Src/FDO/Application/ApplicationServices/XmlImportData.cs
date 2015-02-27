// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: XmlImportData.cs
// Responsibility: mcconnel

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.Application.ApplicationServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class supports importing LinguaLinks data, standard format data, and any other form
	/// of data that can be transformed into the standard hiearchical FieldWorks XML format.
	/// </summary>
	/// <remarks>Some aspects of the behavior of this class are tested in
	/// LexTextControls.LexImportTests.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class XmlImportData
	{
		/// <summary>
		/// Objects of this class provide the context for the currently open CmObject and field
		/// while recursively parsing through the XML.
		/// </summary>
		internal class FieldInfo
		{
			private ICmObject m_cmoOwner;
			private int m_flid;
			private CellarPropertyType m_cpt;
			private bool m_fOwnerIsNew;
			private FieldInfo m_parent;

			internal FieldInfo(ICmObject cmoOwner, int flid, CellarPropertyType cpt, bool fOwnerIsNew,
				FieldInfo parent)
			{
				m_cmoOwner = cmoOwner;
				m_flid = flid;
				m_cpt = cpt;
				m_fOwnerIsNew = fOwnerIsNew;
				m_parent = parent;
			}

			internal ICmObject Owner
			{
				get { return m_cmoOwner; }
				set { m_cmoOwner = value; }
			}

			internal int FieldId
			{
				get { return m_flid; }
			}

			internal CellarPropertyType FieldType
			{
				get { return m_cpt; }
			}

			internal bool OwnerIsNew
			{
				get { return m_fOwnerIsNew; }
			}

			internal FieldInfo ParentOfOwner
			{
				get { return m_parent; }
			}
		}

		/// <summary>
		/// Objects of this class provide the information needed to resolve a link after all
		/// of the CmObjects have been created (and all links can finally be resolved).
		/// </summary>
		internal class PendingLink
		{
			FieldInfo m_fi;
			int m_line;
			string m_sName;
			string m_sState;
			Dictionary<string, string> m_dictAttrs = new Dictionary<string, string>();

			internal PendingLink(FieldInfo fi, XmlReader xrdr)
			{
				m_fi = fi;
				m_sName = xrdr.Name;
				m_sState = xrdr.ReadState.ToString();
				if (xrdr is IXmlLineInfo)
					m_line = (xrdr as IXmlLineInfo).LineNumber;
				else
					m_line = 0;
				if (xrdr.MoveToFirstAttribute())
				{
					do
					{
						m_dictAttrs.Add(xrdr.Name, xrdr.Value);
					} while (xrdr.MoveToNextAttribute());
				}
			}

			internal Dictionary<string, string> LinkAttributes
			{
				get { return m_dictAttrs; }
			}

			internal FieldInfo FieldInformation
			{
				get { return m_fi; }
			}

			internal int LineNumber
			{
				get { return m_line; }
			}

			internal string ElementName
			{
				get { return m_sName; }
			}

			internal string XmlState
			{
				get { return m_sState; }
			}
		}
		private FdoCache m_cache;
		private ISilDataAccessManaged m_sda;
		private IFwMetaDataCacheManaged m_mdc;
		private ILgWritingSystemFactory m_wsf;
		private ITsStrFactory m_tsf;
		private ICmObjectRepository m_repoCmObject;
		private IWritingSystemManager m_wsManager;
		private IProgress m_progress;
		// Factories for ownerless classes.
		private IWfiWordformFactory m_factWfiWordForm;
		private ICmBaseAnnotationFactory m_factCmBaseAnnotation;
		private ICmIndirectAnnotationFactory m_factCmIndirectAnnotation;
		private ITextFactory m_factText;
		private string m_sFilename = "data stream";

		private Dictionary<string, Guid> m_mapIdGuid = new Dictionary<string, Guid>();
		private Dictionary<Guid, string> m_mapGuidId = new Dictionary<Guid, string>();

		private ReferenceTracker m_rglinks = new ReferenceTracker();
		private TextReader m_rdrInput;
		private TextWriter m_wrtrLog;


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlImportData(FdoCache cache)
		{
			m_cache = cache;
			m_sda = cache.DomainDataByFlid as ISilDataAccessManaged;
			m_mdc = cache.DomainDataByFlid.MetaDataCache as IFwMetaDataCacheManaged;
			m_wsf = cache.WritingSystemFactory;
			m_tsf = cache.TsStrFactory;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import the file contents into the database represented by the FdoCache established
		/// by the constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ImportData(string sFilename, IProgress progress)
		{
			DateTime dtBegin = DateTime.Now;
			m_sFilename = sFilename;
			int idx = sFilename.LastIndexOf('.');
			string sLogFile = sFilename;
			if (idx >= 0)
				sLogFile = sLogFile.Substring(0, idx);
			sLogFile = sLogFile + "-Import.log";
			var streamReader = new StreamReader(sFilename, Encoding.UTF8);
			try
			{
				ImportData(streamReader,
					new StreamWriter(sLogFile, false, Encoding.UTF8),
					progress);
			}
			finally
			{
				DateTime dtEnd = DateTime.Now;
				var span = new TimeSpan(dtEnd.Ticks - dtBegin.Ticks);
				LogFinalCounts(Path.GetFileName(sFilename), span);
				if (m_wrtrLog != null)
				{
					m_wrtrLog.Close();
					m_wrtrLog = null;
				}
				streamReader.Dispose();
			}
		}

		private void LogFinalCounts(string sFilename, TimeSpan span)
		{
			if (m_wrtrLog != null)
			{
				string sMsg;
				foreach (int clid in m_mapClidCount.Keys)
				{
					string sClass = m_mdc.GetClassName(clid);
					sMsg = String.Format(AppStrings.ksCreatedObjectsFmt,
						m_mapClidCount[clid], sClass, sFilename);
					LogMessage(sMsg);
				}
				sMsg = String.Format(AppStrings.ksElapsedTime, span.TotalSeconds);
				LogMessage(sMsg);
				Debug.WriteLine(sMsg);
			}
		}

		/// <summary>
		/// This may be needed to parse the log file.
		/// </summary>
		public string ElapsedTimeMsg
		{
			get { return AppStrings.ksElapsedTime; }
		}

		private void LogMessage(string sMsg, int line)
		{
			if (m_wrtrLog != null && !String.IsNullOrEmpty(sMsg))
			{
				string sFullMsg = String.Format("{0}:{1}: {2}",
					m_sFilename, line, sMsg);
				m_wrtrLog.WriteLine(sFullMsg);
			}
		}

		private void LogMessage(string sMsg)
		{
			Debug.Assert(m_wrtrLog != null);
			Debug.Assert(!String.IsNullOrEmpty(sMsg));
			m_wrtrLog.WriteLine(sMsg);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Import the text reader contents into the database represented by the FdoCache set
		/// in the constructor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "xrdr is disposed when closed.")]
		public void ImportData(TextReader rdr, TextWriter wrtrLog, IProgress progress)
		{
			bool fRetVal = true;
			m_progress = progress;
			m_rdrInput = rdr;
			m_wrtrLog = wrtrLog;

			XmlReader xrdr = null;
			try
			{
				m_cache.MainCacheAccessor.BeginNonUndoableTask();
				XmlReaderSettings xrs = new XmlReaderSettings();
				xrs.LineNumberOffset = 0;
				xrs.LinePositionOffset = 0;
				xrdr = XmlReader.Create(rdr, xrs);
				xrdr.MoveToContent();
				if (xrdr.Name == "FwDatabase")
				{
					xrdr.Read();
					xrdr.MoveToContent();
				}
				int nOuterObjLevel = xrdr.Depth;
				while (!xrdr.EOF && xrdr.Depth >= nOuterObjLevel)
				{
					Debug.Assert(xrdr.Depth == nOuterObjLevel);
					ReadXmlObject(xrdr, null, null);
					xrdr.MoveToContent();
				}
				FixPendingLinks();
				CreateMissingSenses();
				CreateMissingMsas();
				CreateEmptyStTextFields();
			}
			catch (Exception e)
			{
				string sMsg = String.Format(AppStrings.ksProblemImporting,
					m_sFilename, e.Message);
				int line = LineNumber(xrdr);
				LogMessage(sMsg, line);
				throw;
			}
			finally
			{
				if (xrdr != null)
					xrdr.Close();
				m_cache.MainCacheAccessor.EndNonUndoableTask();
			}
		}

		ILexSenseFactory m_factLexSense;

		/// <summary>
		/// Entries that are not pure variants must have a sense, even if it's empty.
		/// </summary>
		private void CreateMissingSenses()
		{
			ILexEntryRepository repo = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			foreach (ILexEntry le in repo.AllInstances())
			{
				if (le.SensesOS.Count > 0)
					continue;
				if (IsSenseNeeded(le))
				{
					if (m_factLexSense == null)
						m_factLexSense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>();
					ILexSense ls = m_factLexSense.Create();
					le.SensesOS.Add(ls);
				}
			}
		}

		/// <summary>
		/// We don't need a sense if the entry is purely a variant.
		/// </summary>
		/// <param name="le"></param>
		/// <returns></returns>
		private bool IsSenseNeeded(ILexEntry le)
		{
			int cVariant = 0;
			foreach (ILexEntryRef ler in le.EntryRefsOS)
			{
				if (ler.VariantEntryTypesRS.Count > 0)
					++cVariant;
			}
			return cVariant ==  0 || cVariant < le.EntryRefsOS.Count;
		}

		/// <summary>
		/// Every sense must point to an MSA, even if it doesn't know anything about
		/// the grammatical information.
		/// </summary>
		private void CreateMissingMsas()
		{
			ILexSenseRepository repo = m_cache.ServiceLocator.GetInstance<ILexSenseRepository>();
			foreach (ILexSense ls in repo.AllInstances())
			{
				if (ls.MorphoSyntaxAnalysisRA != null)
					continue;
				ILexEntry le = ls.OwnerOfClass<ILexEntry>();
				Debug.Assert(le != null);
				ls.MorphoSyntaxAnalysisRA = GetEmptyMsa(le);
			}
		}

		IMoUnclassifiedAffixMsaFactory m_factUnclAffMsa;
		IMoStemMsaFactory m_factStemMsa;

		private IMoMorphSynAnalysis GetEmptyMsa(ILexEntry le)
		{
			if (le.LexemeFormOA is IMoAffixAllomorph)
			{
				foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
				{
					if (msa is IMoUnclassifiedAffixMsa)
					{
						IMoUnclassifiedAffixMsa msaUncl = msa as IMoUnclassifiedAffixMsa;
						if (msaUncl.PartOfSpeechRA == null &&
							msaUncl.ComponentsRS.Count == 0 &&
							msaUncl.GlossBundleRS.Count == 0 &&
							msaUncl.LiftResidue == null)
						{
							return msa;
						}
					}
				}
				if (m_factUnclAffMsa == null)
					m_factUnclAffMsa = m_cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>();
				IMoUnclassifiedAffixMsa msaT = m_factUnclAffMsa.Create();
				le.MorphoSyntaxAnalysesOC.Add(msaT);
				return msaT;
			}
			else
			{
				foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
				{
					if (msa is IMoStemMsa)
					{
						IMoStemMsa msaStem = msa as IMoStemMsa;
						if (msaStem.PartOfSpeechRA == null &&
							msaStem.ComponentsRS.Count == 0 &&
							msaStem.GlossBundleRS.Count == 0 &&
							msaStem.ProdRestrictRC.Count == 0 &&
							msaStem.StratumRA == null &&
							msaStem.FromPartsOfSpeechRC.Count == 0 &&
							msaStem.InflectionClassRA == null &&
							msaStem.MsFeaturesOA == null &&
							msaStem.LiftResidue == null)
						{
							return msa;
						}
					}
				}
				if (m_factStemMsa == null)
					m_factStemMsa = m_cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
				IMoStemMsa msaT = m_factStemMsa.Create();
				le.MorphoSyntaxAnalysesOC.Add(msaT);
				return msaT;
			}
		}

		/// <summary>
		/// Merge data from LexEntry leOld which is missing from LexEntry leNew into leNew.
		/// This may  result in the data being removed from LexEntry leOld, but it's going
		/// to be deleted anyway.
		/// If the model of a LexEntry changes, the code below may have to change also!
		/// </summary>
		private void MergeRedundantEntries(ILexEntry leOld, ILexEntry leNew)
		{
			if (leNew.ImportResidue == null && leOld.ImportResidue != null)
				leNew.ImportResidue = leOld.ImportResidue;
			CopyMultiString(leOld.Bibliography, leNew.Bibliography);
			CopyMultiUnicode(leOld.CitationForm, leNew.CitationForm);
			CopyMultiString(leOld.Comment, leNew.Comment);
			CopyMultiString(leOld.LiteralMeaning, leNew.LiteralMeaning);
			CopyMultiUnicode(leOld.Restrictions, leNew.Restrictions);
			CopyMultiString(leOld.SummaryDefinition, leNew.SummaryDefinition);
			ILexEtymology ety = leOld.EtymologyOA;
			if (leNew.EtymologyOA == null && ety != null)
				leNew.EtymologyOA = ety;
			IMoForm[] rgmf = leOld.AlternateFormsOS.ToArray();
			for (int i = 0; i < rgmf.Length; ++i)
				leNew.AlternateFormsOS.Add(rgmf[i]);
			IMoMorphSynAnalysis[] rgmsa = leOld.MorphoSyntaxAnalysesOC.ToArray();
			for (int i = 0; i < rgmsa.Length; ++i)
				leNew.MorphoSyntaxAnalysesOC.Add(rgmsa[i]);
			ILexPronunciation[] rgpron = leOld.PronunciationsOS.ToArray();
			for (int i = 0; i < rgpron.Length; ++i)
				leNew.PronunciationsOS.Add(rgpron[i]);
			ILexSense[] rgls = leOld.SensesOS.ToArray();
			for (int i = 0; i < rgls.Length; ++i)
				leNew.SensesOS.Add(rgls[i]);
			MergeEntryRefs(leOld, leNew);
			CopyCustomFieldData(leOld, leNew);

			// Clean up our internal Id map before deleting anything.
			string id;
			if (m_mapGuidId.TryGetValue(leOld.Guid, out id))
				m_mapIdGuid[id] = leNew.Guid;
		}

		private void MergeEntryRefs(ILexEntry leOld, ILexEntry leNew)
		{
			if (leOld.EntryRefsOS.Count == 0)
				return;
			foreach (var lreOld in leOld.EntryRefsOS)
			{
				bool fMerged = false;
				foreach (var lreNew in leNew.EntryRefsOS)
				{
					if (AreEntryRefsCompatible(lreOld, lreNew))
					{
						MergeEntryRefs(lreOld, lreNew);
						fMerged = true;
						break;
					}
				}
				if (!fMerged)
					leNew.EntryRefsOS.Add(lreOld);
			}
			leOld.EntryRefsOS.Clear();
		}

		private bool AreEntryRefsCompatible(ILexEntryRef lreOld, ILexEntryRef lreNew)
		{
			if (lreOld.RefType != lreNew.RefType)
			{
				if (lreOld.RefType == LexEntryRefTags.krtVariant && lreOld.VariantEntryTypesRS.Count > 0)
					return false;
				if (lreNew.RefType == LexEntryRefTags.krtVariant && lreNew.VariantEntryTypesRS.Count > 0)
					return false;
			}
			if (lreOld.HideMinorEntry != lreNew.HideMinorEntry)
				return false;
			if (lreOld.ComplexEntryTypesRS.Count > 0 && lreNew.VariantEntryTypesRS.Count > 0)
				return false;
			if (lreOld.VariantEntryTypesRS.Count > 0 && lreNew.ComplexEntryTypesRS.Count > 0)
				return false;
			if (!AreEntryRefTypesCompatible(lreOld.ComplexEntryTypesRS, lreNew.ComplexEntryTypesRS))
				return false;
			if (!AreEntryRefTypesCompatible(lreOld.VariantEntryTypesRS, lreNew.VariantEntryTypesRS))
				return false;
			if (!AreEntryRefTargetsCompatible(lreOld.ComponentLexemesRS, lreNew.ComponentLexemesRS))
				return false;
			return AreEntryRefTargetsCompatible(lreOld.PrimaryLexemesRS, lreNew.PrimaryLexemesRS);
		}

		private bool AreEntryRefTypesCompatible(IFdoReferenceSequence<ILexEntryType> oldTypes,
			IFdoReferenceSequence<ILexEntryType> newTypes)
		{
			if (oldTypes.Count >= newTypes.Count)
			{
				foreach (var type in newTypes)
				{
					if (!oldTypes.Contains(type))
						return false;
				}
			}
			else
			{
				foreach (var type in oldTypes)
				{
					if (!newTypes.Contains(type))
						return false;
				}
			}
			return true;
		}

		private bool AreEntryRefTargetsCompatible(IFdoReferenceSequence<ICmObject> oldTargets,
			IFdoReferenceSequence<ICmObject> newTargets)
		{
			if (oldTargets.Count >= newTargets.Count)
			{
				foreach (var target in newTargets)
				{
					if (!oldTargets.Contains(target))
						return false;
				}
			}
			else
			{
				foreach (var target in oldTargets)
				{
					if (!newTargets.Contains(target))
						return false;
				}
			}
			return true;
		}

		private void MergeEntryRefs(ILexEntryRef lreOld, ILexEntryRef lreNew)
		{
			if (lreOld.ComplexEntryTypesRS.Count > lreNew.ComplexEntryTypesRS.Count)
				lreNew.ComplexEntryTypesRS.Replace(0, lreNew.ComplexEntryTypesRS.Count, lreOld.ComplexEntryTypesRS.ToArray());
			if (lreOld.VariantEntryTypesRS.Count > lreNew.VariantEntryTypesRS.Count)
				lreNew.VariantEntryTypesRS.Replace(0, lreNew.VariantEntryTypesRS.Count, lreOld.VariantEntryTypesRS.ToArray());
			if (lreOld.ComponentLexemesRS.Count > lreNew.ComponentLexemesRS.Count)
				lreNew.ComponentLexemesRS.Replace(0, lreNew.ComponentLexemesRS.Count, lreOld.ComponentLexemesRS);
			if (lreOld.PrimaryLexemesRS.Count > lreNew.PrimaryLexemesRS.Count)
				lreNew.PrimaryLexemesRS.Replace(0, lreNew.PrimaryLexemesRS.Count, lreOld.PrimaryLexemesRS);
			if (lreNew.RefType == LexEntryRefTags.krtVariant)
				lreNew.RefType = lreOld.RefType;	// might well be krtComplexForm
			m_rglinks.UpdateOwner(lreOld, lreNew, false);
		}

		private void CopyCustomFieldData(ICmObject cmoOld, ICmObject cmoNew)
		{
			Debug.Assert(cmoOld.ClassID == cmoNew.ClassID);
			IFwMetaDataCacheManaged mdc = m_cache.MetaDataCache as IFwMetaDataCacheManaged;
			Debug.Assert(mdc != null);
			ISilDataAccessManaged sda = m_cache.DomainDataByFlid as ISilDataAccessManaged;
			Debug.Assert(sda != null);
			foreach (int flid in mdc.GetFields(cmoOld.ClassID, true, (int)CellarPropertyTypeFilter.All))
			{
				if (!mdc.IsCustom(flid))
					continue;
				bool fHandled = false;
				ITsString tss;
				string s;
				switch ((CellarPropertyType)mdc.GetFieldType(flid))
				{
					case CellarPropertyType.Boolean:
						break;
					case CellarPropertyType.Integer:
						int val = sda.get_IntProp(cmoOld.Hvo, flid);
						sda.SetInt(cmoNew.Hvo, flid, val);
						fHandled = true;
						break;
					case CellarPropertyType.Numeric:
						break;
					case CellarPropertyType.Float:
						break;
					case CellarPropertyType.Time:
						break;
					case CellarPropertyType.Guid:
						break;
					case CellarPropertyType.Image:
					case CellarPropertyType.Binary:
						byte[] rgb;
						int cb = sda.get_Binary(cmoOld.Hvo, flid, out rgb);
						if (cb > 0)
							sda.SetBinary(cmoNew.Hvo, flid, rgb, cb);
						fHandled = true;
						break;
					case CellarPropertyType.GenDate:
						GenDate date = sda.get_GenDateProp(cmoOld.Hvo, flid);
						sda.SetGenDate(cmoNew.Hvo, flid, date);
						fHandled = true;
						break;
					case CellarPropertyType.String:
						tss = sda.get_StringProp(cmoOld.Hvo, flid);
						if (tss != null && tss.Text != null)
							sda.SetString(cmoNew.Hvo, flid, tss);
						fHandled = true;
						break;
					case CellarPropertyType.MultiString:
					case CellarPropertyType.MultiUnicode:
						ITsMultiString tms = sda.get_MultiStringProp(cmoOld.Hvo, flid);
						for (int i = 0; i < tms.StringCount; ++i)
						{
							int ws;
							tss = tms.GetStringFromIndex(i, out ws);
							if (tss != null && tss.Length > 0)
								sda.SetMultiStringAlt(cmoNew.Hvo, flid, ws, tss);
						}
						fHandled = true;
						break;
					case CellarPropertyType.Unicode:
						s = sda.get_UnicodeProp(cmoOld.Hvo, flid);
						if (!String.IsNullOrEmpty(s))
							sda.SetUnicode(cmoNew.Hvo, flid, s, s.Length);
						fHandled = true;
						break;
					case CellarPropertyType.OwningAtomic:
						{
							int hvo = sda.get_ObjectProp(cmoOld.Hvo, flid);
							if (hvo != 0)
								sda.MoveOwn(cmoOld.Hvo, flid, hvo, cmoNew.Hvo, flid, 0);
							fHandled = true;
						}
						break;
					case CellarPropertyType.ReferenceAtomic:
						{
							int hvo = sda.get_ObjectProp(cmoOld.Hvo, flid);
							if (hvo != 0)
								sda.SetObjProp(cmoNew.Hvo, flid, hvo);
							fHandled = true;
						}
						break;
					case CellarPropertyType.OwningCollection:
						break;
					case CellarPropertyType.ReferenceCollection:
						{
							int[] hvosOld = sda.VecProp(cmoOld.Hvo, flid);
							int[] hvosNew = sda.VecProp(cmoNew.Hvo, flid);
							List<int> hvosMerged = new List<int>(hvosNew);
							foreach (int hvoOld in hvosOld)
							{
								if (!hvosMerged.Contains(hvoOld))
								{
									sda.Replace(cmoNew.Hvo, flid, hvosMerged.Count, hvosMerged.Count,
										new int[] { hvoOld }, 1);
									hvosMerged.Add(hvoOld);
								}
							}
							fHandled = true;
						}
						break;
					case CellarPropertyType.OwningSequence:
						break;
					case CellarPropertyType.ReferenceSequence:
						break;
				}
				if (!fHandled)
				{
					string sMsg = String.Format(AppStrings.ksCouldNotMergeCustomFields,
						mdc.GetFieldName(flid));
					throw new Exception(sMsg);
				}
			}
		}

		private void CopyMultiUnicode(IMultiUnicode muOld, IMultiUnicode muNew)
		{
			for (int i = 0; i < muOld.StringCount; ++i)
			{
				int ws;
				ITsString tss = muOld.GetStringFromIndex(i, out ws);
				muNew.set_String(ws, tss.Text);
			}
		}

		private void CopyMultiString(IMultiString msOld, IMultiString msNew)
		{
			for (int i = 0; i < msOld.StringCount; ++i)
			{
				int ws;
				ITsString tss = msOld.GetStringFromIndex(i, out ws);
				msNew.set_String(ws, tss);
			}
		}

		/// <summary>
		/// For all objects that own a structured text field, make sure that one has been
		/// created, even if it is empty.  This simplifies life elsewhere in the programming
		/// of Fieldworks.
		/// </summary>
		private void CreateEmptyStTextFields()
		{
			int[] rgflid = m_mdc.GetFieldIds();
			List<int> rgflidUnknown = new List<int>();
			for (int i = 0; i < rgflid.Length; ++i)
			{
				int flid = rgflid[i];
				if (flid == CmAnnotationTags.kflidText ||
					flid == PhPhonContextTags.kflidDescription)
				{
					// Ignore these two instances of an atomic field that points to an StText.
					continue;
				}
				int clidDst = m_mdc.GetDstClsId(flid);
				if (clidDst != StTextTags.kClassId)
					continue;
				int cpt = m_mdc.GetFieldType(flid);
				if (cpt != (int)CellarPropertyType.OwningAtomic)
				{
					// nothing has a sequence or collection of StText when this is written,
					// and I don't know how you would deal with a nonatomic property here.
					continue;
				}
				switch (flid)
				{
					//case CmAnnotationTags.kflidText:
					//case PhPhonContextTags.kflidDescription:
					//    break;
					case CmAgentTags.kflidNotes:
					case CmPossibilityTags.kflidDiscussion:
					case LexAppendixTags.kflidContents:
					case LexDbTags.kflidIntroduction:
					case RnGenericRecTags.kflidConclusions:
					case RnGenericRecTags.kflidDescription:
					case RnGenericRecTags.kflidDiscussion:
					case RnGenericRecTags.kflidExternalMaterials:
					case RnGenericRecTags.kflidFurtherQuestions:
					case RnGenericRecTags.kflidHypothesis:
					case RnGenericRecTags.kflidPersonalNotes:
					case RnGenericRecTags.kflidResearchPlan:
					case RnGenericRecTags.kflidVersionHistory:
					case ScrBookTags.kflidTitle:
					case ScrSectionTags.kflidContent:
					case ScrSectionTags.kflidHeading:
					case TextTags.kflidContents:
						// We know how to deal with these known cases.
						break;
					default:
						rgflidUnknown.Add(flid);
						break;
				}
			}
			EnsureCmAgentNotes();
			// CmPossibility items we create get the Discussion StText created at the same time,
			// so don't bother checking here.
			//EnsureCmPossibilityDiscussion();
			EnsureLexAppendixContents();
			EnsureLexDbIntroduction();
			EnsureRnGenericRecStTextFields();
			EnsureScrBookTitle();
			EnsureScrSectionStTextFields();
			EnsureTextContents();

			if (rgflidUnknown.Count > 0)
			{
				IStTextRepository repoStText = m_cache.ServiceLocator.GetInstance<IStTextRepository>();
				foreach (ICmObject cmo in m_repoCmObject.AllInstances())
				{
					foreach (int flid in rgflidUnknown)
					{
						foreach (int clid in m_mdc.GetAllSubclasses(m_mdc.GetOwnClsId(flid)))
						{
							if (cmo.ClassID == clid)
							{
								int hvo = m_sda.get_ObjectProp(cmo.Hvo, flid);
								if (hvo == 0)
								{
									hvo = m_sda.MakeNewObject(StTextTags.kClassId, cmo.Hvo, flid, -2);
									IncrementCreatedClidCount(StTextTags.kClassId);
								}
								IStText text = repoStText.GetObject(hvo);
								EnsureStTextParagraph(text);
								break;
							}
						}
					}
				}
			}
		}

		private void EnsureTextContents()
		{
			ITextRepository repoText = m_cache.ServiceLocator.GetInstance<ITextRepository>();
			foreach (IText text in repoText.AllInstances())
			{
				if (text.ContentsOA == null)
					text.ContentsOA = CreateStText();
				EnsureStTextParagraph(text.ContentsOA);
			}
		}

		private void EnsureScrSectionStTextFields()
		{
			IScrSectionRepository repoSection = m_cache.ServiceLocator.GetInstance<IScrSectionRepository>();
			foreach (IScrSection sect in repoSection.AllInstances())
			{
				if (sect.HeadingOA == null)
					sect.HeadingOA = CreateStText();
				EnsureStTextParagraph(sect.HeadingOA);
				if (sect.ContentOA == null)
					sect.ContentOA = CreateStText();
				EnsureStTextParagraph(sect.ContentOA);
			}
		}

		private void EnsureScrBookTitle()
		{
			IScrBookRepository repoBook = m_cache.ServiceLocator.GetInstance<IScrBookRepository>();
			foreach (IScrBook book in repoBook.AllInstances())
			{
				if (book.TitleOA == null)
					book.TitleOA = CreateStText();
				EnsureStTextParagraph(book.TitleOA);
			}
		}

		private void EnsureRnGenericRecStTextFields()
		{
			IRnGenericRecRepository repoRec = m_cache.ServiceLocator.GetInstance<IRnGenericRecRepository>();
			foreach (IRnGenericRec rec in repoRec.AllInstances())
			{
				if (rec.ConclusionsOA == null)
					rec.ConclusionsOA = CreateStText();
				EnsureStTextParagraph(rec.ConclusionsOA);
				if (rec.DescriptionOA == null)
					rec.DescriptionOA = CreateStText();
				EnsureStTextParagraph(rec.DescriptionOA);
				if (rec.DiscussionOA == null)
					rec.DiscussionOA = CreateStText();
				EnsureStTextParagraph(rec.DiscussionOA);
				if (rec.ExternalMaterialsOA == null)
					rec.ExternalMaterialsOA = CreateStText();
				EnsureStTextParagraph(rec.ExternalMaterialsOA);
				if (rec.FurtherQuestionsOA == null)
					rec.FurtherQuestionsOA = CreateStText();
				EnsureStTextParagraph(rec.FurtherQuestionsOA);
				if (rec.HypothesisOA == null)
					rec.HypothesisOA = CreateStText();
				EnsureStTextParagraph(rec.HypothesisOA);
				if (rec.PersonalNotesOA == null)
					rec.PersonalNotesOA = CreateStText();
				EnsureStTextParagraph(rec.PersonalNotesOA);
				if (rec.ResearchPlanOA == null)
					rec.ResearchPlanOA = CreateStText();
				EnsureStTextParagraph(rec.ResearchPlanOA);
				if (rec.VersionHistoryOA == null)
					rec.VersionHistoryOA = CreateStText();
				EnsureStTextParagraph(rec.VersionHistoryOA);
			}
		}

		private void EnsureLexDbIntroduction()
		{
			ILexDbRepository repoLexDb = m_cache.ServiceLocator.GetInstance<ILexDbRepository>();
			foreach (ILexDb lex in repoLexDb.AllInstances())
			{
				if (lex.IntroductionOA == null)
					lex.IntroductionOA = CreateStText();
				EnsureStTextParagraph(lex.IntroductionOA);
			}
		}

		private void EnsureLexAppendixContents()
		{
			ILexAppendixRepository repoAppend = m_cache.ServiceLocator.GetInstance<ILexAppendixRepository>();
			foreach (ILexAppendix append in repoAppend.AllInstances())
			{
				if (append.ContentsOA == null)
					append.ContentsOA = CreateStText();
				EnsureStTextParagraph(append.ContentsOA);
			}
		}

		//private void EnsureCmPossibilityDiscussion()
		//{
		//    ICmPossibilityRepository repoPoss = m_cache.ServiceLocator.GetInstance<ICmPossibilityRepository>();
		//    foreach (ICmPossibility poss in repoPoss.AllInstances())
		//    {
		//        if (poss.DiscussionOA == null)
		//            poss.DiscussionOA = CreateStText();
		//        EnsureStTextParagraph(poss.DiscussionOA);
		//    }
		//}

		private void EnsureCmAgentNotes()
		{
			ICmAgentRepository repoAgent = m_cache.ServiceLocator.GetInstance<ICmAgentRepository>();
			foreach (ICmAgent agent in repoAgent.AllInstances())
			{
				if (agent.NotesOA == null)
					agent.NotesOA = CreateStText();
				EnsureStTextParagraph(agent.NotesOA);
			}
		}

		IStTextFactory m_factStText;
		IStTxtParaFactory m_factTxtPara;

		private IStText CreateStText()
		{
			if (m_factStText == null)
				m_factStText = m_cache.ServiceLocator.GetInstance<IStTextFactory>();
			IncrementCreatedClidCount(StTextTags.kClassId);
			return m_factStText.Create();
		}

		private void EnsureStTextParagraph(IStText text)
		{
			if (text.ParagraphsOS.Count == 0)
			{
				if (m_factTxtPara == null)
					m_factTxtPara = m_cache.ServiceLocator.GetInstance<IStTxtParaFactory>();
				text.ParagraphsOS.Add(m_factTxtPara.Create());
				IncrementCreatedClidCount(StTxtParaTags.kClassId);
			}
		}

		Dictionary<int, int> m_mapClidCount = new Dictionary<int, int>();

		void IncrementCreatedClidCount(int clid)
		{
			int count;
			if (!m_mapClidCount.TryGetValue(clid, out count))
			{
				count = 0;
				m_mapClidCount.Add(clid, count);
			}
			m_mapClidCount[clid] = count + 1;
		}

		private void ReadXmlObject(XmlReader xrdr, FieldInfo fi, ICmObject objToUse)
		{
			Debug.Assert(xrdr.NodeType == XmlNodeType.Element);

#if DEBUG
			int nDepth = xrdr.Depth;
#endif
			string sClass = xrdr.Name;
			string sId = xrdr.GetAttribute("id");
			ICmObject cmo = null;
			// Check for singleton classes that should already exist before creating new
			// objects.
			bool fNewObject = false;
			switch (sClass)
			{
				case "LangProject":
					cmo = m_cache.LangProject;
					Debug.Assert(cmo != null);
					break;
				case "LexDb":
					cmo = m_cache.LangProject.LexDbOA;
					Debug.Assert(cmo != null);
					break;
				default:
					int clid = m_mdc.GetClassId(sClass);
					if (fi != null)
					{
						int hvo;
						switch ((CellarPropertyType)fi.FieldType)
						{
							case CellarPropertyType.OwningAtomic:
								hvo = m_sda.get_ObjectProp(fi.Owner.Hvo, fi.FieldId);
								if (hvo == 0)
								{
									hvo = m_sda.MakeNewObject(clid, fi.Owner.Hvo, fi.FieldId, -2);
									fNewObject = true;
									IncrementCreatedClidCount(clid);
								}
								break;
							case CellarPropertyType.OwningCollection:
								hvo = m_sda.MakeNewObject(clid, fi.Owner.Hvo, fi.FieldId, -1);
								fNewObject = true;
								IncrementCreatedClidCount(clid);
								break;
							case CellarPropertyType.OwningSequence:
								int cvec = m_sda.get_VecSize(fi.Owner.Hvo, fi.FieldId);
								if (clid == SegmentTags.kClassId && fi.Owner is IStTxtPara &&
									objToUse != null && objToUse is ISegment)
								{
									hvo = objToUse.Hvo;
								}
								else
								{
									hvo = m_sda.MakeNewObject(clid, fi.Owner.Hvo, fi.FieldId, cvec);
									fNewObject = true;
									IncrementCreatedClidCount(clid);
								}
								break;
							default:
								string sMsg = AppStrings.ksInvalidNestedXMLElements;
								LogMessage(sMsg, LineNumber(xrdr));
								throw new Exception(sMsg);
						}
						if (m_repoCmObject == null)
							m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
						cmo = m_repoCmObject.GetObject(hvo);
					}
					else
					{
						// must be an unowned object.
						switch (clid)
						{
							case WfiWordformTags.kClassId:
								if (m_factWfiWordForm == null)
									m_factWfiWordForm = m_cache.ServiceLocator.GetInstance<IWfiWordformFactory>();
								cmo = m_factWfiWordForm.Create();
								break;
							case CmBaseAnnotationTags.kClassId:
								if (m_factCmBaseAnnotation == null)
									m_factCmBaseAnnotation = m_cache.ServiceLocator.GetInstance<ICmBaseAnnotationFactory>();
								cmo = m_factCmBaseAnnotation.CreateOwnerless();
								break;
							case CmIndirectAnnotationTags.kClassId:
								if (m_factCmIndirectAnnotation == null)
									m_factCmIndirectAnnotation = m_cache.ServiceLocator.GetInstance<ICmIndirectAnnotationFactory>();
								cmo = m_factCmIndirectAnnotation.CreateOwnerless();
								break;
							case LexEntryTags.kClassId:
								if (m_factLexEntry == null)
									m_factLexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>();
								cmo = m_factLexEntry.Create();
								m_nHomograph = 0;
								break;
							case TextTags.kClassId:
								if (m_factText == null)
									m_factText = m_cache.ServiceLocator.GetInstance<ITextFactory>();
								cmo = m_factText.Create();
								break;
							default:
								string sMsg = AppStrings.ksUnrecognizedOwnerlessObjectClass;
								LogMessage(sMsg, LineNumber(xrdr));
								throw new Exception(sMsg);
						}
						fNewObject = true;
						IncrementCreatedClidCount(clid);
					}
					break;
			}
			if (!String.IsNullOrEmpty(sId))
			{
				m_mapIdGuid.Add(sId, cmo.Guid);
				m_mapGuidId.Add(cmo.Guid, sId);
			}
			if (!xrdr.IsEmptyElement)
			{
				ReadXmlFields(xrdr, cmo, fNewObject, fi);
#if DEBUG
				Debug.Assert(xrdr.Name == sClass);	// we should be on the end element
				Debug.Assert(xrdr.Depth == nDepth);
#endif
				xrdr.ReadEndElement();
			}
			else
			{
				// Degenerate case, but it can happen if the object has been created, but not
				// given any content (eg, a bare MoStemMsa).
				xrdr.Read();
			}
			xrdr.MoveToContent();
			if (cmo is ILexEntry)
			{
				if (m_mapFormEntry == null)
					FillEntryMap();
				ILexEntry le = cmo as ILexEntry;
				ILexEntry leDup;
				StoreEntryInMap(le, fNewObject, true, out leDup);
				if (leDup != null)
				{
					MergeRedundantEntries(leDup, le);
					List<WsString> delKeys = new List<WsString>();
					foreach (var x in m_mapFormEntry)
					{
						if (x.Value == leDup)
							delKeys.Add(x.Key);
					}
					foreach (WsString key in delKeys)
						m_mapFormEntry.Remove(key);
					m_rglinks.UpdateOwner(leDup, le, true);
					int nHomograph = le.HomographNumber;
					leDup.Delete();
					if (le.HomographNumber != m_nHomograph)
						le.HomographNumber = m_nHomograph;
				}
			}
		}

		private int LineNumber(XmlReader xrdr)
		{
			if (xrdr != null)
			{
				IXmlLineInfo xli = xrdr as IXmlLineInfo;
				if (xli != null)
					return xli.LineNumber;
			}
			return 0;
		}

		const int kflidCrossReferences = -123;
		const int kflidLexicalRelations = -124;

		private void ReadXmlFields(XmlReader xrdr, ICmObject cmoOwner, bool fOwnerIsNew,
			FieldInfo fiParent)
		{
			int nDepth = xrdr.Depth;
			// Consume the start element of the owning object.
			xrdr.Read();
			xrdr.MoveToContent();
			while (xrdr.Depth > nDepth)
			{
				while (xrdr.IsEmptyElement)
				{
					xrdr.Read();
					xrdr.MoveToContent();
				}
				if (xrdr.Depth <= nDepth)
					break;
				Debug.Assert(xrdr.NodeType == XmlNodeType.Element);
				string sField = xrdr.Name;
				int nDepthField = xrdr.Depth;
				int flid;
				CellarPropertyType cpt;
				ICmObject realOwner = null;
				FieldInfo fi = null;
				if (cmoOwner.ClassID == LexDbTags.kClassId && sField == "Entries")
				{
					flid = 0; // no actual owning sequence.
					cpt = CellarPropertyType.OwningCollection;
				}
				else if (sField == "CrossReferences")
				{
					flid = kflidCrossReferences;
					cpt = CellarPropertyType.ReferenceCollection;
				}
				else if (sField == "LexicalRelations")
				{
					flid = kflidLexicalRelations;
					cpt = CellarPropertyType.ReferenceCollection;
				}
				else if (sField == "Custom" || sField == "CustomStr")
				{
					var sName = xrdr.GetAttribute("name");
					flid = m_mdc.GetFieldId2(cmoOwner.ClassID, sName, true);
					cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
				}
				else if (sField == "ImportResidue")
				{
					if (cmoOwner is ILexEntry || cmoOwner is ILexSense)
					{
						flid = m_mdc.GetFieldId2(cmoOwner.ClassID, sField, true);
						cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
					}
					else
					{
						realOwner = cmoOwner.OwnerOfClass(LexSenseTags.kClassId);
						if (realOwner == null)
							realOwner = cmoOwner.OwnerOfClass(LexEntryTags.kClassId);
						if (realOwner != null)
						{
							flid = m_mdc.GetFieldId2(realOwner.ClassID, sField, true);
							cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
							FieldInfo fiBack = fiParent;
							while (fiBack != null && fiBack.Owner != realOwner)
								fiBack = fiBack.ParentOfOwner;
							if (fiBack.Owner == realOwner)
								fi = new FieldInfo(realOwner, flid, cpt, fiBack.OwnerIsNew, fiBack.ParentOfOwner);
						}
						else
						{
							flid = 0;
							cpt = CellarPropertyType.Nil;
						}
						if (flid == 0 || fi == null)
						{
							// Just swallow the data whole -- we don't have any way to store it.
							xrdr.ReadOuterXml();
							continue;
						}
					}
				}
				else
				{
					flid = m_mdc.GetFieldId2(cmoOwner.ClassID, sField, true);
					cpt = (CellarPropertyType)m_mdc.GetFieldType(flid);
				}
				if (realOwner == null)
					fi = new FieldInfo(cmoOwner, flid, cpt, fOwnerIsNew, fiParent);
				xrdr.Read();
				xrdr.MoveToContent();
				if (xrdr.NodeType == XmlNodeType.EndElement && xrdr.Name == sField && xrdr.Depth == nDepthField)
				{
					// On Linux/Mono, empty elements can end up with both a start element node
					// and an end element node after XSLT processing.  So if we're sitting on
					// the end element node, there's nothing to do.  (See FWNX-744.)
					xrdr.ReadEndElement();
					xrdr.MoveToContent();
					continue;
				}
				switch (cpt)
				{
					case CellarPropertyType.Boolean:
						ReadBooleanValue(xrdr, fi);
						break;
					case CellarPropertyType.Integer:
						ReadIntegerValue(xrdr, fi);
						break;
					case CellarPropertyType.Numeric:
						ReadNumericValue(xrdr, fi);
						break;
					case CellarPropertyType.Float:
						ReadFloatValue(xrdr, fi);
						break;
					case CellarPropertyType.Time:
						ReadTimeValue(xrdr, fi);
						break;
					case CellarPropertyType.Guid:
						ReadGuidValue(xrdr, fi);
						break;
					case CellarPropertyType.Image:
					// Image is "big" Binary
					case CellarPropertyType.Binary:
						ReadBinaryValue(xrdr, fi);
						break;
					case CellarPropertyType.GenDate:
						ReadGenDateValue(xrdr, fi);
						break;
					case CellarPropertyType.Unicode:
						ReadUnicodeValue(xrdr, fi);
						break;
					case CellarPropertyType.MultiUnicode:
						do
						{
							ReadMultiUnicodeValue(xrdr, fi);
						} while (xrdr.Depth > nDepthField);
						break;
					case CellarPropertyType.String:
						ReadTsStringValue(xrdr, fi);
						break;
					case CellarPropertyType.MultiString:
						do
						{
							ReadMultiTsStringValue(xrdr, fi);
						} while (xrdr.Depth > nDepthField);
						break;
					case CellarPropertyType.OwningAtomic:
						ReadXmlObject(xrdr, fi, null);
						break;
					case CellarPropertyType.ReferenceAtomic:
						ReadReferenceLink(xrdr, fi);
						break;
					case CellarPropertyType.OwningCollection:
					case CellarPropertyType.OwningSequence:
						if (flid == StTxtParaTags.kflidSegments)
						{
							IStTxtPara para = cmoOwner as IStTxtPara;
							Debug.Assert(para != null);
							int i = 0;
							do
							{
								if (i < para.SegmentsOS.Count)
									ReadXmlObject(xrdr, fi, para.SegmentsOS[i]);
								else
									ReadXmlObject(xrdr, fi, null);
								++i;
							} while (xrdr.Depth > nDepthField);
						}
						else if (flid == 0)
						{
							// Entries: now unowned, read like an owning sequence but there's no owning property to put them in.
							ReadOwningSequence(xrdr, null, nDepthField);
						}
						else
						{
							ReadOwningSequence(xrdr, fi, nDepthField);
						}
						break;
					case CellarPropertyType.ReferenceCollection:
					case CellarPropertyType.ReferenceSequence:
						do
						{
							ReadReferenceLink(xrdr, fi);
						} while (xrdr.Depth > nDepthField);
						break;
				}
				if (xrdr.Depth > nDepth)
				{
					while (xrdr.IsStartElement())
					{
						string sBadName = xrdr.Name;
						string sMsg = String.Format(AppStrings.ksUnexpectedElement, sBadName);
						LogMessage(sMsg, LineNumber(xrdr));
						string s = xrdr.ReadOuterXml();
						Debug.WriteLine(String.Format("Unexpected XML node: {0}", s));
						// Change line endings to the Unicode line separator character, so that the
						// multiple lines will be visible when displayed.
						s = s.Replace("\r\n", "\n");
						s = s.Replace('\r', '\n');
						s = s.Replace('\n', StringUtils.kChHardLB);
						if (fi.Owner is ILexEntry)
						{
							ITsString tss = (fi.Owner as ILexEntry).ImportResidue;
							if (tss.Length > 0)
							{
								ITsIncStrBldr tisb = tss.GetIncBldr();
								tisb.Append("; ");
								tisb.Append(s);
								tss = tisb.GetString();
							}
							else
							{
								tss = m_cache.TsStrFactory.MakeString(s, m_cache.DefaultUserWs);
							}
							(fi.Owner as ILexEntry).ImportResidue = tss;
						}
						else if (fi.Owner is ILexSense)
						{
						}
					}
					xrdr.ReadEndElement();
					xrdr.MoveToContent();
				}
			}
		}

		private void ReadOwningSequence(XmlReader xrdr, FieldInfo fi, int nDepthField)
		{
			do
			{
				ReadXmlObject(xrdr, fi, null);
			} while (xrdr.Depth > nDepthField);
		}

		private void ReadBooleanValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "Boolean")
			{
				string sMsg = AppStrings.ksExpectedBoolean;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sVal = xrdr.GetAttribute("val");
			if (String.IsNullOrEmpty(sVal))
			{
				string sMsg = AppStrings.ksMissingBooleanVal;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			sVal = sVal.ToLowerInvariant();
			bool fVal = sVal == "true" || sVal == "yes" || sVal == "t" || sVal == "y" || sVal == "1";
			m_sda.SetBoolean(fi.Owner.Hvo, fi.FieldId, fVal);
			xrdr.Read();
			xrdr.MoveToContent();
			if (xrdr.NodeType == XmlNodeType.EndElement && xrdr.Name == "Boolean")
			{
				xrdr.ReadEndElement();
				xrdr.MoveToContent();
			}
		}

		private void ReadIntegerValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "Integer")
			{
				string sMsg = AppStrings.ksExpectedInteger;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sVal = xrdr.GetAttribute("val");
			if (String.IsNullOrEmpty(sVal))
			{
				string sMsg = AppStrings.ksMissingIntegerVal;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			int nVal = Int32.Parse(sVal);
			m_sda.SetInt(fi.Owner.Hvo, fi.FieldId, nVal);
			if (fi.FieldId == LexEntryTags.kflidHomographNumber)
				m_nHomograph = nVal;
			xrdr.Read();
			xrdr.MoveToContent();
			if (xrdr.NodeType == XmlNodeType.EndElement && xrdr.Name == "Integer")
			{
				xrdr.ReadEndElement();
				xrdr.MoveToContent();
			}
		}

		private void ReadGuidValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "Guid")
			{
				string sMsg = AppStrings.ksExpectedGuid;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sVal = xrdr.GetAttribute("val");
			if (String.IsNullOrEmpty(sVal))
			{
				string sMsg = AppStrings.ksMissingGuidVal;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			Guid guid = new Guid(sVal);
			m_sda.SetGuid(fi.Owner.Hvo, fi.FieldId, guid);
			xrdr.Read();
			xrdr.MoveToContent();
			if (xrdr.NodeType == XmlNodeType.EndElement && xrdr.Name == "Guid")
			{
				xrdr.ReadEndElement();
				xrdr.MoveToContent();
			}
		}

		private void ReadTimeValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "Time")
			{
				string sMsg = AppStrings.ksExpectedTime;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sVal = xrdr.GetAttribute("val");
			if (String.IsNullOrEmpty(sVal))
			{
				string sMsg = AppStrings.ksMissingTimeVal;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			DateTime dt = DateTime.Parse(sVal);
			m_sda.SetDateTime(fi.Owner.Hvo, fi.FieldId, dt);
			xrdr.Read();
			xrdr.MoveToContent();
			if (xrdr.NodeType == XmlNodeType.EndElement && xrdr.Name == "Time")
			{
				xrdr.ReadEndElement();
				xrdr.MoveToContent();
			}
		}

		private void ReadGenDateValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "GenDate")
			{
				string sMsg = AppStrings.ksExpectedGenDate;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sVal = xrdr.GetAttribute("val");
			if (String.IsNullOrEmpty(sVal))
			{
				string sMsg = AppStrings.ksMissingGenDateVal;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			GenDate gdt = DomainImpl.CmObject.GetGenDateFromInt(Int32.Parse(sVal));
			m_sda.SetGenDate(fi.Owner.Hvo, fi.FieldId, gdt);
			xrdr.Read();
			xrdr.MoveToContent();
			if (xrdr.NodeType == XmlNodeType.EndElement && xrdr.Name == "GenDate")
			{
				xrdr.ReadEndElement();
				xrdr.MoveToContent();
			}
		}

		private void ReadNumericValue(XmlReader xrdr, FieldInfo fi)
		{
			string sMsg = "Warning: 'Numeric' values are not yet implemented!";
			LogMessage(sMsg, LineNumber(xrdr));
			throw new NotImplementedException();
		}

		private void ReadFloatValue(XmlReader xrdr, FieldInfo fi)
		{
			string sMsg = "Warning: 'Float' values are not yet implemented!";
			LogMessage(sMsg, LineNumber(xrdr));
			throw new NotImplementedException();
		}

		private void ReadBinaryValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "Binary")
			{
				string sMsg = AppStrings.ksExpectedBinary;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			bool fEmpty = xrdr.IsEmptyElement;
			xrdr.Read();
			byte[] rgb;
			if (fEmpty)
			{
				rgb = new byte[0];
			}
			else
			{
				string rawdata = xrdr.ReadString();
				rgb = ConvertBinHex(rawdata, xrdr);
			}
			m_sda.SetBinary(fi.Owner.Hvo, fi.FieldId, rgb, rgb.Length);
			if (!fEmpty)
				xrdr.ReadEndElement();
			xrdr.MoveToContent();
		}

		private byte[] ConvertBinHex(string rawstring, XmlReader xrdr)
		{
			List<byte> rgbytes = new List<byte>(rawstring.Length / 2);
			byte bT;
			char ch;
			int cch = rawstring.Length;
			for (int ib = 0, ich = 0; ich < cch; ++ib)
			{
				do
				{
					ch = rawstring[ich];
					++ich;
					if (ch < 0 || ch > 127)
					{
						LogMessage(AppStrings.ksInvalidBinaryData, LineNumber(xrdr));
						throw new Exception(AppStrings.ksInvalidBinaryData);
					}
				} while (Char.IsWhiteSpace(ch) && ich < cch);
				if (ich == cch)
				{
					if (!Char.IsWhiteSpace(ch))
					{
						LogMessage(AppStrings.ksIgnoringExtraNonBinaryData, LineNumber(xrdr));
					}
					break;
				}
				if (ch >= '0' && ch <= '9')
					bT = (byte)((ch & 0xF) << 4);
				else if (ch >= 'A' && ch <= 'F' || ch >= 'a' || ch <= 'f')
					bT = (byte)(((ch & 0xF) + 9) << 4);
				else
				{
					LogMessage(AppStrings.ksInvalidBinaryData, LineNumber(xrdr));
					throw new Exception(AppStrings.ksInvalidBinaryData);
				}
				++ich;
				ch = rawstring[ich];
				if (ch >= '0' && ch <= '9')
					bT |= (byte)(ch & 0xF);
				else if (ch >= 'A' && ch <= 'F' || ch >= 'a' || ch <= 'f')
					bT |= (byte)((ch & 0xF) + 9);
				else
				{
					LogMessage(AppStrings.ksInvalidBinaryData, LineNumber(xrdr));
					throw new Exception(AppStrings.ksInvalidBinaryData);
				}
				rgbytes.Add(bT);
			}
			return rgbytes.ToArray();
		}

		private void ReadUnicodeValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "Uni")
			{
				string sMsg = AppStrings.ksExpectedUni;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			bool fEmpty = xrdr.IsEmptyElement;
			xrdr.Read();
			string data ;
			if (fEmpty)
				data = String.Empty;
			else
				data = xrdr.ReadString();
			m_sda.SetUnicode(fi.Owner.Hvo, fi.FieldId, data, data.Length);
			if (!fEmpty)
				xrdr.ReadEndElement();
			xrdr.MoveToContent();
		}

		private void ReadMultiUnicodeValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "AUni")
			{
				string sMsg = AppStrings.ksExpectedAUni;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sWs = xrdr.GetAttribute("ws");
			int ws = GetWsFromId(sWs);
			bool fEmpty = xrdr.IsEmptyElement;
			xrdr.Read();
			string data;
			if (fEmpty)
				data = String.Empty;
			else
				data = xrdr.ReadString();
			m_sda.SetMultiStringAlt(fi.Owner.Hvo, fi.FieldId, ws, m_tsf.MakeString(data, ws));
			if (!fEmpty)
				xrdr.ReadEndElement();
			xrdr.MoveToContent();
		}

		private void ReadTsStringValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "Str")
			{
				string sMsg = AppStrings.ksExpectedStr;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sXml = xrdr.ReadOuterXml();
			EnsureAllWritingSystemsDefined(sXml);
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(sXml, m_wsf);
			// If it's import residue, append to whatever is already there (if anything is).
			if (fi.FieldId == LexEntryTags.kflidImportResidue ||
				fi.FieldId == LexSenseTags.kflidImportResidue)
			{
				ITsString tssOld = m_sda.get_StringProp(fi.Owner.Hvo, fi.FieldId);
				if (tssOld != null && tssOld.Length > 0)
				{
					ITsIncStrBldr tisb = tssOld.GetIncBldr();
					tisb.AppendTsString(m_cache.TsStrFactory.MakeString("; ", m_cache.DefaultUserWs));
					tisb.AppendTsString(tss);
					tss = tisb.GetString();
				}
			}
			m_sda.SetString(fi.Owner.Hvo, fi.FieldId, tss);
			xrdr.MoveToContent();
		}

		private void ReadMultiTsStringValue(XmlReader xrdr, FieldInfo fi)
		{
			if (xrdr.Name != "AStr")
			{
				string sMsg = AppStrings.ksExpectedAStr;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			string sWs = xrdr.GetAttribute("ws");
			int ws = GetWsFromId(sWs);
			string sXml = xrdr.ReadOuterXml();
			EnsureAllWritingSystemsDefined(sXml);
			ITsString tss = TsStringSerializer.DeserializeTsStringFromXml(sXml, m_wsf);
			m_sda.SetMultiStringAlt(fi.Owner.Hvo, fi.FieldId, ws, tss);
			xrdr.MoveToContent();
		}

		private void EnsureAllWritingSystemsDefined(string sXml)
		{
			for (int ich = 0; ich >= 0 && ich < sXml.Length; )
			{
				ich = sXml.IndexOf(" ws=", ich);
				if (ich < 0)
					break;
				ich += 4;
				if (ich >= sXml.Length)
					break;
				char chQuote = sXml[ich];
				++ich;
				if (chQuote != '"' && chQuote != '\'')
					continue;
				int ichLim = sXml.IndexOf(chQuote, ich);
				if (ichLim < 0)
					break;
				string sWs = sXml.Substring(ich, ichLim - ich);
				if (!String.IsNullOrEmpty(sWs))
				{
					GetWsFromId(sWs);
				}
				ich = ichLim + 1;
			}
		}

		private void ReadReferenceLink(XmlReader xrdr, FieldInfo fi)
		{
			// This is going to be difficult, because all sorts of variants of links exist.
			// The simplest has a target attribute which refers to the id attribute of another
			// object in the import file.  But, there are a number of other possible attributes
			// that are context dependent.  For now, we'll punt sort of, storing the FieldInfo
			// input value along with all the attributes.  Later, after everything has been
			// imported, so that all references can be resolved to actual objects, we'll try
			// to decipher all the pending reference links.
			if (xrdr.Name != "Link")
			{
				string sMsg = AppStrings.ksExpectedLink;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			PendingLink pend = new PendingLink(fi, xrdr);
			if (pend.LinkAttributes.Count == 0)
			{
				string sMsg = AppStrings.ksInvalidLinkElement;
				LogMessage(sMsg, LineNumber(xrdr));
				throw new Exception(sMsg);
			}
			bool fIsPossibility = false;
			switch (fi.FieldId)
			{
				case 0:
				case kflidCrossReferences:
				case kflidLexicalRelations:
					break;
				default:
					int clid = m_mdc.GetDstClsId(fi.FieldId);
					while (clid != 0)
					{
						if (clid == CmPossibilityTags.kClassId)
						{
							fIsPossibility = true;
							break;
						}
						clid = m_mdc.GetBaseClsId(clid);
					}
					break;
			}
			if (fIsPossibility)
			{
				int hvo = ResolveLinkReference(fi.FieldId, pend, false);
				if (hvo != 0)
				{
					int cpt = m_mdc.GetFieldType(fi.FieldId);
					if (cpt == (int)CellarPropertyType.ReferenceAtomic)
					{
						m_sda.SetObjProp(fi.Owner.Hvo, fi.FieldId, hvo);
					}
					else
					{
						Debug.Assert(cpt == (int)CellarPropertyType.ReferenceCollection ||
							cpt == (int)CellarPropertyType.ReferenceSequence);
						int cvec = m_sda.get_VecSize(fi.Owner.Hvo, fi.FieldId);
						m_sda.Replace(fi.Owner.Hvo, fi.FieldId, cvec, cvec, new int[] { hvo }, 1);
					}
				}
				else
				{
					m_rglinks.Add(pend);
				}
			}
			else
			{
				m_rglinks.Add(pend);
			}
			xrdr.MoveToElement();
			xrdr.Read();
			xrdr.MoveToContent();
			if (!xrdr.IsStartElement() && xrdr.Name == "Link")
			{
				xrdr.Read();	// read the end element -- shouldn't be one normally.
				xrdr.MoveToContent();
			}
		}

		// ChkRefTags.kflidRendering						WfiWordform
		// ChkRenderingTags.kflidMeaning					WfiGloss
		// ChkRenderingTags.kflidSurfaceForm				WfiWordform
		// ChkSenseTags.kflidSense							LexSense
		// CmAnnotationTags.kflidSource						CmAgent
		// CmBaseAnnotationTags.kflidOtherObjects			CmObject
		// CmBaseAnnotationTags.kflidWritingSystem			LgWritingSystem
		// CmMediaTags.kflidMediaFile						CmFile
		// CmOverlayTags.kflidPossItems						CmPossibility
		// CmOverlayTags.kflidPossList						CmPossibilityList
		// CmPersonTags.kflidEducation						CmPossibility
		// CmPersonTags.kflidPlaceOfBirth					CmLocation
		// CmPersonTags.kflidPlacesOfResidence				CmLocation
		// CmPersonTags.kflidPositions						CmPossibility
		// CmPictureTags.kflidPictureFile					CmFile
		// CmPossibilityTags.kflidConfidence				CmPossibility
		// CmPossibilityTags.kflidResearchers				CmPerson
		// CmPossibilityTags.kflidRestrictions				CmPossibility
		// CmPossibilityTags.kflidStatus					CmPossibility
		// CmPossibilityListTags.kflidWritingSystem			LgWritingSystem
		// CmSemanticDomainTags.kflidOcmRefs				CmAnthroItem
		// CmSemanticDomainTags.kflidRelatedDomains			CmSemanticDomain
		// CmSortSpecTags.kflidPrimaryCollation				LgCollation
		// CmSortSpecTags.kflidPrimaryWs					LgWritingSystem
		// CmSortSpecTags.kflidSecondaryCollation			LgCollation
		// CmSortSpecTags.kflidSecondaryWs					LgWritingSystem
		// CmSortSpecTags.kflidTertiaryCollation			LgCollation
		// CmSortSpecTags.kflidTertiaryWs					LgWritingSystem
		// DsChartTags.kflidTemplate						CmPossibility
		// DsConstChartTags.kflidBasedOn					StText
		// DsConstChartTags.kflidRows						CmIndirectAnnotation
		// FsClosedValueTags.kflidValue						FsSymFeatVal
		// FsComplexFeatureTags.kflidType					FsFeatStrucType
		// FsDisjunctiveValueTags.kflidValue				FsSymFeatVal
		// FsFeatStrucTags.kflidType						FsFeatStrucType
		// FsFeatStrucTypeTags.kflidFeatures				FsFeatDefn
		// FsFeatureSpecificationTags.kflidFeature			FsFeatDefn
		// FsNegatedValueTags.kflidValue					FsSymFeatVal
		// FsOpenFeatureTags.kflidWritingSystem				LgWritingSystem
		// FsSharedValueTags.kflidValue						FsFeatureSpecification
		// LangProjectTags.kflidAnalysisWss					LgWritingSystem
		// LangProjectTags.kflidCurAnalysisWss				LgWritingSystem
		// LangProjectTags.kflidCurPronunWss				LgWritingSystem
		// LangProjectTags.kflidCurVernWss					LgWritingSystem
		// LangProjectTags.kflidThesaurus					CmPossibilityList
		// LangProjectTags.kflidVernWss						LgWritingSystem
		// LangProjectTags.kflidWordformLookupLists			WordformLookupList
		// LexDbTags.kflidAllomorphIndex					MoForm
		// LexDbTags.kflidLexicalFormIndex					LexEntry
		// LexEntryTags.kflidMainEntriesOrSenses			CmObject
		// LexPronunciationTags.kflidLocation				CmLocation
		// LexSenseTags.kflidStatus							CmPossibility
		// LexSenseTags.kflidThesaurusItems					CmPossibility
		// MoAffixAllomorphTags.kflidMsEnvPartOfSpeech		PartOfSpeech
		// MoAffixAllomorphTags.kflidPhoneEnv				PhEnvironment
		// MoAffixAllomorphTags.kflidPosition				PhEnvironment
		// MoAffixFormTags.kflidInflectionClasses			MoInflClass
		// MoAlloAdhocProhibTags.kflidAllomorphs			MoForm
		// MoAlloAdhocProhibTags.kflidFirstAllomorph		MoForm
		// MoAlloAdhocProhibTags.kflidRestOfAllos			MoForm
		// MoCompoundRuleTags.kflidStratum					MoStratum
		// MoCompoundRuleTags.kflidToProdRestrict			CmPossibility
		// MoCompoundRuleAppTags.kflidLeftForm				MoStemAllomorph
		// MoCompoundRuleAppTags.kflidLinker				MoAffixAllomorph
		// MoCompoundRuleAppTags.kflidRightForm				MoStemAllomorph
		// MoCopyFromInputTags.kflidContent					PhContextOrVar
		// MoDerivTags.kflidStemForm						MoStemAllomorph
		// MoDerivTags.kflidStemMsa							MoStemMsa
		// MoDerivAffAppTags.kflidAffixForm					MoAffixAllomorph
		// MoDerivAffAppTags.kflidAffixMsa					MoDerivAffMsa
		// MoDerivAffMsaTags.kflidAffixCategory				CmPossibility
		// MoDerivAffMsaTags.kflidFromInflectionClass		MoInflClass
		// MoDerivAffMsaTags.kflidFromPartOfSpeech			PartOfSpeech
		// MoDerivAffMsaTags.kflidFromProdRestrict			CmPossibility
		// MoDerivAffMsaTags.kflidFromStemName				MoStemName
		// MoDerivAffMsaTags.kflidStratum					MoStratum
		// MoDerivAffMsaTags.kflidToInflectionClass			MoInflClass
		// MoDerivAffMsaTags.kflidToPartOfSpeech			PartOfSpeech
		// MoDerivAffMsaTags.kflidToProdRestrict			CmPossibility
		// MoDerivStepMsaTags.kflidInflectionClass			MoInflClass
		// MoDerivStepMsaTags.kflidProdRestrict				CmPossibility
		// MoGlossItemTags.kflidTarget						MoGlossItem
		// MoInflAffixSlotAppTags.kflidAffixForm			MoAffixForm
		// MoInflAffixSlotAppTags.kflidAffixMsa				MoInflAffMsa
		// MoInflAffixSlotAppTags.kflidSlot					MoInflAffixSlot
		// MoInflAffixTemplateTags.kflidPrefixSlots			MoInflAffixSlot
		// MoInflAffixTemplateTags.kflidSlots				MoInflAffixSlot
		// MoInflAffixTemplateTags.kflidStratum				MoStratum
		// MoInflAffixTemplateTags.kflidSuffixSlots			MoInflAffixSlot
		// MoInflAffMsaTags.kflidAffixCategory				CmPossibility
		// MoInflAffMsaTags.kflidFromProdRestrict			CmPossibility
		// MoInflAffMsaTags.kflidSlots						MoInflAffixSlot
		// MoInflTemplateAppTags.kflidTemplate				MoInflAffixTemplate
		// MoInsertNCTags.kflidContent						PhNaturalClass
		// MoInsertPhonesTags.kflidContent					PhTerminalUnit
		// MoModifyFromInputTags.kflidContent				PhContextOrVar
		// MoModifyFromInputTags.kflidModification			PhNCFeatures
		// MoMorphAdhocProhibTags.kflidFirstMorpheme		MoMorphSynAnalysis
		// MoMorphAdhocProhibTags.kflidMorphemes			MoMorphSynAnalysis
		// MoMorphAdhocProhibTags.kflidRestOfMorphs			MoMorphSynAnalysis
		// MoMorphDataTags.kflidAnalyzingAgents				CmAgent
		// MoMorphSynAnalysisTags.kflidComponents			MoMorphSynAnalysis
		// MoMorphSynAnalysisTags.kflidGlossBundle			MoGlossItem
		// MoPhonolRuleAppTags.kflidRule					CmObject
		// MoStemAllomorphTags.kflidPhoneEnv				PhEnvironment
		// MoStemAllomorphTags.kflidStemName				MoStemName
		// MoStemMsaTags.kflidFromPartsOfSpeech				PartOfSpeech
		// MoStemMsaTags.kflidInflectionClass				MoInflClass
		// MoStemMsaTags.kflidProdRestrict					CmPossibility
		// MoStemMsaTags.kflidStratum						MoStratum
		// MoStemNameTags.kflidDefaultAffix					MoInflAffMsa
		// MoStemNameTags.kflidDefaultStem					MoStemName
		// MoStratumTags.kflidPhonemes						PhPhonemeSet
		// MoStratumAppTags.kflidStratum					MoStratum
		// PartOfSpeechTags.kflidBearableFeatures			FsFeatDefn
		// PartOfSpeechTags.kflidDefaultInflectionClass		MoInflClass
		// PartOfSpeechTags.kflidInflectableFeats			FsFeatDefn
		// PhEnvironmentTags.kflidLeftContext				PhPhonContext
		// PhEnvironmentTags.kflidRightContext				PhPhonContext
		// PhFeatureConstraintTags.kflidFeature				FsFeatDefn
		// PhIterationContextTags.kflidMember				PhPhonContext
		// PhNCSegmentsTags.kflidSegments					PhPhoneme
		// PhPhonRuleFeatTags.kflidItem						CmObject
		// PhSegmentRuleTags.kflidFinalStratum				MoStratum
		// PhSegmentRuleTags.kflidInitialStratum			MoStratum
		// PhSegRuleRHSTags.kflidExclRuleFeats				PhPhonRuleFeat
		// PhSegRuleRHSTags.kflidInputPOSes					PartOfSpeech
		// PhSegRuleRHSTags.kflidReqRuleFeats				PhPhonRuleFeat
		// PhSequenceContextTags.kflidMembers				PhPhonContext
		// PhSimpleContextBdryTags.kflidFeatureStructure	PhBdryMarker
		// PhSimpleContextNCTags.kflidFeatureStructure		PhNaturalClass
		// PhSimpleContextNCTags.kflidMinusConstr			PhFeatureConstraint
		// PhSimpleContextNCTags.kflidPlusConstr			PhFeatureConstraint
		// PhSimpleContextSegTags.kflidFeatureStructure		PhPhoneme
		// ReversalIndexTags.kflidWritingSystem				LgWritingSystem
		// ReversalIndexEntryTags.kflidPartOfSpeech			PartOfSpeech
		// RnAnalysisTags.kflidCounterEvidence				RnGenericRec
		// RnAnalysisTags.kflidStatus						CmPossibility
		// RnAnalysisTags.kflidSupersededBy					RnAnalysis
		// RnAnalysisTags.kflidSupportingEvidence			RnGenericRec
		// RnEventTags.kflidLocations						CmLocation
		// RnEventTags.kflidSources							CmPerson
		// RnEventTags.kflidTimeOfEvent						CmPossibility
		// RnEventTags.kflidType							CmPossibility
		// RnEventTags.kflidWeather							CmPossibility
		// RnGenericRecTags.kflidAnthroCodes				CmAnthroItem
		// RnGenericRecTags.kflidConfidence					CmPossibility
		// RnGenericRecTags.kflidCrossReferences			CrossReference
		// RnGenericRecTags.kflidPhraseTags					CmPossibility
		// RnGenericRecTags.kflidReminders					Reminder
		// RnGenericRecTags.kflidResearchers				CmPerson
		// RnGenericRecTags.kflidRestrictions				CmPossibility
		// RnGenericRecTags.kflidSeeAlso					RnGenericRec
		// RnRoledParticTags.kflidParticipants				CmPerson
		// RnRoledParticTags.kflidRole						CmPossibility
		// ScrBookTags.kflidBookId							ScrBookRef
		// ScrDifferenceTags.kflidRevParagraph				StPara
		// ScrImportSourceTags.kflidNoteType				CmAnnotationDefn
		// ScrMarkerMappingTags.kflidNoteType				CmAnnotationDefn
		// ScrMarkerMappingTags.kflidStyle					StStyle
		// ScrScriptureNoteTags.kflidCategories				CmPossibility
		// StJournalTextTags.kflidCreatedBy					CmPerson
		// StJournalTextTags.kflidModifiedBy				CmPerson
		// StStyleTags.kflidBasedOn							StStyle
		// StStyleTags.kflidNext							StStyle
		// StTxtParaTags.kflidObjRefs						CmObject
		// StTxtParaTags.kflidTextObjects					CmObject
		// TextTags.kflidGenres								CmPossibility
		// UserAppFeatActTags.kflidUserConfigAcct			UserConfigAcct
		// WfiAnalysisTags.kflidCompoundRuleApps			MoCompoundRule
		// WfiAnalysisTags.kflidInflTemplateApps			MoInflAffixTemplate
		// WfiAnalysisTags.kflidStems						LexEntry
		// WfiWordSetTags.kflidCases						WfiWordform
		// WordFormLookupTags.kflidAnthroCodes				CmAnthroItem
		// WordFormLookupTags.kflidThesaurusItems			CmPossibility
		// WordformLookupListTags.kflidWritingSystem		LgWritingSystem

		ILexReferenceFactory m_factLexRef = null;

		/// <summary>
		/// Holds state information for HandleLrSeq
		/// </summary>
		class LrSeqInfo
		{
			public List<ICmObject> Targets = new List<ICmObject>();
			public ILexRefType RefType;
			public ICmObject PendingOwner;
			public int PendingFieldId;
		}
		/// <summary>
		/// Work on a pending link that might be part of a sequence (or tree).
		/// Returns true if pend is a member of one and should not be otherwise handled.
		/// Accumulates pending relations in lrSeq until it encounters a new item
		/// that is not part of the same sequence (or tree); then matches or adds it.
		/// </summary>
		/// <returns></returns>
		private bool HandleLrSeq(LrSeqInfo info, PendingLink pend, ICmObject target, ILexRefType parent)
		{
			switch (parent.MappingType)
			{
				case (int)LexRefTypeTags.MappingTypes.kmtSenseSequence:
				case (int)LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryTree:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
					break;
				default:
					// not part of any sequence; if we have a pending sequence finish it.
					FinalizeLrSeq(info);
					return false;
			}
			// OK, we have something to add and the RefType takes a sequence.
			if (info.RefType != parent
				|| pend.FieldInformation.Owner != info.PendingOwner
				|| pend.FieldInformation.FieldId != info.PendingFieldId)
			{
				// new (possibly first) sequence; wrap up any pending one.
				FinalizeLrSeq(info);
			}
			info.RefType = parent;
			info.PendingOwner = pend.FieldInformation.Owner;
			info.PendingFieldId = pend.FieldInformation.FieldId;
			info.Targets.Add(target);
			return true;
		}

		/// <summary>
		/// Finish any sequence being accumulated by HandlLrSeq. This is called both when
		/// we find the start of another sequence, and also when anything else happens that
		/// signals the end of the sequence (including the end of the whole list of links).
		/// </summary>
		/// <param name="info"></param>
		private void FinalizeLrSeq(LrSeqInfo info)
		{
			if (!info.Targets.Any())
				return;
			switch (info.RefType.MappingType)
			{
				case (int) LexRefTypeTags.MappingTypes.kmtSenseSequence:
				case (int) LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case (int) LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
					foreach (var rel in info.RefType.MembersOC)
					{
						if (IsMatchingSeq(info.Targets, rel))
						{
							info.Targets.Clear();
							return; // nothing to do, an exactly matching sequence exists.
						}
					}
					break;
				default: // tree
					// The first item in a tree is the 'whole', which is understood to be the
					// element that the links occur in. There is not a link for the 'whole'
					// but we need to include it in the relation.
					info.Targets.Insert(0, info.PendingOwner);
					// This check is currently redundant...a well-formed input file should only have one
					// copy of a tree relation, and it can't match something existing because we don't
					// (yet) try to match entries or senses in the input with ones in the database already.
					// I'm leaving it in just in case there's an unexpected duplicate in the file,
					// or in case we one day enhance things to match existing entries and senses,
					// which would make it relevant.
					// If we do implement that matching, enable the disabled part of the ImportTreeRelation test.
					foreach (var rel in info.RefType.MembersOC)
					{
						if (IsMatchingTree(info.Targets, rel))
						{
							info.Targets.Clear();
							return; // nothing to do, an exactly matching tree exists.
						}
					}
					break;
			}
			var newRel = info.Targets[0].Services.GetInstance<ILexReferenceFactory>().Create();
			info.RefType.MembersOC.Add(newRel);
			newRel.TargetsRS.Replace(0,0, info.Targets);
			info.Targets.Clear();
		}

		private bool IsMatchingSeq(List<ICmObject> targets, ILexReference lr)
		{
			if (targets.Count != lr.TargetsRS.Count)
				return false;
			for (int i = 0; i < targets.Count; i++)
			{
				if (targets[i] != lr.TargetsRS[i])
					return false;
			}
			return true;
		}

		private bool IsMatchingTree(List<ICmObject> targets, ILexReference lr)
		{
			if (targets.Count != lr.TargetsRS.Count || targets[0] != lr.TargetsRS[0])
				return false;
			return (new HashSet<ICmObject>(targets.Skip(1)).Intersect(lr.TargetsRS.Skip(1)).Count() == targets.Count - 1);
		}

		private void FixPendingLinks()
		{
			if (m_repoCmObject == null)
				m_repoCmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>();
			ICmObject cmoTargetPrev = null;
			ICmObject cmoOwnerPrev = null;
			ILexRefType lrtPrev = null;
			bool fReversePrev = false;
			ILexReference lrPrev = null;
			var lrSeqInfo = new LrSeqInfo();

			// This fills it in if we haven't already made it, but also, there are special cases where
			// some existing entries are not present in the map with the key corresponding to their
			// current state (e.g., they may be pre-existing entries put in the map with HN 0, but
			// a homograph was loaded so they are now HN1). Another pass ensures that any key that
			// matches an existing entry will find something (not necessarily that element, since there
			// are pathological cases where import produces an inconsistent set of HNs, and we give
			// preference to the one imported as the one that links in the file probably mean).
			FillEntryMap();

			foreach (var pend in m_rglinks.Links)
			{
				int flid = pend.FieldInformation.FieldId;
				if (flid == kflidCrossReferences || flid == kflidLexicalRelations)
				{
					ICmObject cmoTarget = ResolveLexReferenceLink(pend.LinkAttributes, flid);
					if (cmoTarget == null)
					{
						if (flid == kflidCrossReferences)
							LogMessage(AppStrings.ksCannottResolveCrossRef, pend.LineNumber);
						else
							LogMessage(AppStrings.ksCannotResolveLexRelation, pend.LineNumber);
						continue;
					}
					bool fReverse;
					ILexRefType lrt = GetLexRefType(pend.LinkAttributes, out fReverse, pend.LineNumber);
					if (lrt == null)
					{
						LogMessage(AppStrings.ksCannotCreateLexRefType, pend.LineNumber);
						continue;
					}
					ICmObject cmoOwner = pend.FieldInformation.Owner;
					// lrt, fReverse, cmoOwner, cmoTarget ...
					bool fJoinWithPrev = false;
					if (HandleLrSeq(lrSeqInfo, pend, cmoTarget, lrt))
						continue;
					switch (lrt.MappingType)
					{
						case (int)LexRefTypeTags.MappingTypes.kmtSenseCollection:
						case (int)LexRefTypeTags.MappingTypes.kmtEntryCollection:
						case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
							if (cmoOwner == cmoOwnerPrev &&
								lrtPrev == lrt &&
								fReversePrev == fReverse &&
								lrPrev != null)
							{
								fJoinWithPrev = true;
							}
							break;
						default:
							break;
					}
					if (fJoinWithPrev)
					{
						if (!lrPrev.TargetsRS.Contains(cmoTarget))
							lrPrev.TargetsRS.Add(cmoTarget);
					}
					else
					{
						if (m_factLexRef == null)
							m_factLexRef = m_cache.ServiceLocator.GetInstance<ILexReferenceFactory>();
						ILexReference lr = FindMatchingLexRef(lrt, cmoOwner, cmoTarget, fReverse);
						if (lr == null)
						{
							lr = m_factLexRef.Create();
							lrt.MembersOC.Add(lr);
							IncrementCreatedClidCount(LexReferenceTags.kClassId);
							if (fReverse)
							{
								lr.TargetsRS.Add(cmoTarget);
								lr.TargetsRS.Add(cmoOwner);
							}
							else
							{
								lr.TargetsRS.Add(cmoOwner);
								lr.TargetsRS.Add(cmoTarget);
							}
						}
						lrPrev = lr;
					}
					cmoTargetPrev = cmoTarget;
					cmoOwnerPrev = cmoOwner;
					lrtPrev = lrt;
					fReversePrev = fReverse;
				}
				else
				{
					FinalizeLrSeq(lrSeqInfo); // found something not part of an lr sequence.
					int hvo = ResolveLinkReference(flid, pend, true);
					if (hvo == 0)
					{
						// Complain in a technical fashion...
						StringBuilder bldr = new StringBuilder();
						foreach (string key in pend.LinkAttributes.Keys)
							bldr.AppendFormat(" {0}=\"{1}\"", key, pend.LinkAttributes[key]);
						string sMsg = String.Format(AppStrings.ksCannotResolveLink, bldr.ToString());
						LogMessage(sMsg, pend.LineNumber);
						continue;
					}
					int hvoOwner = pend.FieldInformation.Owner.Hvo;
					if (pend.FieldInformation.FieldType == CellarPropertyType.ReferenceAtomic)
					{
						m_sda.SetObjProp(hvoOwner, flid, hvo);
					}
					else
					{
						// Add to the reference list only if it's not there already.
						bool fAdd = true;
						int[] hvos = m_sda.VecProp(hvoOwner, flid);
						int chvo = hvos.Length;
						for (int ihvo = 0; ihvo < chvo; ++ihvo)
						{
							if (hvos[ihvo] == hvo)
							{
								fAdd = false;
								break;
							}
						}
						if (fAdd)
							m_sda.Replace(hvoOwner, flid, chvo, chvo, new int[] { hvo }, 1);
					}
					cmoTargetPrev = null;
					cmoOwnerPrev = null;
					lrtPrev = null;
					fReversePrev = false;
					lrPrev = null;
				}
			}
			FinalizeLrSeq(lrSeqInfo);
		}

		private ILexReference FindMatchingLexRef(ILexRefType lrt, ICmObject cmoOwner,
			ICmObject cmoTarget, bool fReverse)
		{
			switch (lrt.MappingType)
			{
				case (int)LexRefTypeTags.MappingTypes.kmtEntryAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseAsymmetricPair:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseAsymmetricPair:
					foreach (ILexReference lr in lrt.MembersOC)
					{
						if (lr.TargetsRS.Count >= 2)
						{
							if (fReverse)
							{
								if (lr.TargetsRS[0] == cmoTarget && lr.TargetsRS[1] == cmoOwner)
									return lr;
							}
							else
							{
								if (lr.TargetsRS[0] == cmoOwner && lr.TargetsRS[1] == cmoTarget)
									return lr;
							}
						}
					}
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryPair:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSensePair:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseCollection:
				case (int)LexRefTypeTags.MappingTypes.kmtSensePair:
					foreach (ILexReference lr in lrt.MembersOC)
					{
						if (lr.TargetsRS.Count >= 2 &&
							lr.TargetsRS.Contains(cmoTarget) && lr.TargetsRS.Contains(cmoOwner))
						{
							return lr;
						}
					}
					break;
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryTree:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseTree:
				case (int)LexRefTypeTags.MappingTypes.kmtEntrySequence:
				case (int)LexRefTypeTags.MappingTypes.kmtEntryOrSenseSequence:
				case (int)LexRefTypeTags.MappingTypes.kmtSenseSequence:
					throw new ApplicationException("FindMatchingLexRef should not be called for sequences or trees");
			}
			return null;
		}

		internal struct WsString
		{
			internal int ws;
			internal string sVal;

			internal WsString(int wsHvo, string sValue)
			{
				ws = wsHvo;
				sVal = sValue;
			}

			public override bool Equals(object obj)
			{
				if (obj is WsString)
				{
					return (ws == ((WsString)obj).ws) && sVal.Equals(((WsString)obj).sVal);
				}
				else
				{
					return false;
				}
			}

			public override int GetHashCode()
			{
				return sVal.GetHashCode() + (ws % 100);
			}
		}

		internal class WsStringIgnoreCaseComparer : IEqualityComparer<WsString>
		{
			#region IEqualityComparer<WsString> Members

			public bool Equals(WsString x, WsString y)
			{
				return x.ws == y.ws && x.sVal.Equals(y.sVal, StringComparison.OrdinalIgnoreCase);
			}

			public int GetHashCode(WsString obj)
			{
				return obj.sVal.ToLowerInvariant().GetHashCode() + (obj.ws % 100);
			}

			#endregion
		}
		static WsStringIgnoreCaseComparer s_wssIgnoreCaseComparer = new WsStringIgnoreCaseComparer();

		/// <summary>
		/// This dictionary is constructed so that for the most part, it contains an entry for each lex entry in the system,
		/// whether pre-existing or imported this time, with the key [prefix][form][suffix][homograph number].
		/// Exceptions:
		///  - a pre-existing entry may be in the dictionary with key [prefix][form][suffix]0 (its old key), even though
		///  we imported a homograph for it and its natural key would now be something else, probably 1.
		///  - when the import contains two or more entries which are homographs of each other but of no pre-existing entry,
		///  the last one created will be in the dictionary with key [prefix][form][suffix]0, and the others not at all.
		/// This behavior facilitates matching cross-refs to the expected entry in the input file, though it is somewhat
		/// arbitrary what entry we link to if the cross-ref does not specify an HN.
		/// After the import, but before resolving links, we further add entries under their proper keys, if there is not
		/// a conflicting one already present.
		/// </summary>
		private Dictionary<WsString, ILexEntry> m_mapFormEntry = null;

		private ICmObject ResolveLexReferenceLink(Dictionary<string, string> dictAttrs, int flid)
		{
			string sWs;
			if (dictAttrs.TryGetValue("wsv", out sWs))
			{
				int ws = GetWsFromId(sWs);
				string sSenseNumber;
				string sForm = GetLexFormAndSenseNumber(dictAttrs, out sSenseNumber);
				if (ws == 0 || String.IsNullOrEmpty(sForm))
					return null;
				else
					return GetEntryOrSense(ws, sForm, sSenseNumber, flid);
			}
			return null;
		}

		private ICmObject GetEntryOrSense(int ws, string sForm, string sSenseNumber, int flid)
		{
			string sn = Icu.Normalize(sForm, Icu.UNormalizationMode.UNORM_NFD);
			WsString wss = new WsString(ws, sn);
			ILexEntry le;
			if (m_mapFormEntry.TryGetValue(wss, out le))
			{
				if (String.IsNullOrEmpty(sSenseNumber))
					return le;
				else
					return GetIndicatedSense(sSenseNumber, le);
			}
			// If we're looking for a key unmarked with homograph number, see whether we have homographs of this word.
			// If so, allow it to locate the first one.
			if (sn.Length > 0 && !Char.IsDigit(sn[wss.sVal.Length - 1]))
			{
				var wss1 = new WsString(ws, sn + "1");
				if (m_mapFormEntry.TryGetValue(wss1, out le))
				{
					if (String.IsNullOrEmpty(sSenseNumber))
						return le;
					else
						return GetIndicatedSense(sSenseNumber, le);
				}
			}
			// no match...create one.
			le = CreateLexEntryForReference(ws, sn, flid);
			if (String.IsNullOrEmpty(sSenseNumber))
				return le;
			else
				return le.SensesOS[0];
		}

		private string GetLexFormAndSenseNumber(Dictionary<string, string> dictAttrs,
			out string sSenseNumber)
		{
			string sForm = null;
			sSenseNumber = null;
			if (!dictAttrs.TryGetValue("entry", out sForm))
			{
				if (dictAttrs.TryGetValue("sense", out sForm))
				{
					int idx = sForm.LastIndexOf(' ');
					if (idx >= 0 && sForm.Length > idx && Char.IsDigit(sForm[idx + 1]))
					{
						sSenseNumber = sForm.Substring(idx + 1);
						sForm = sForm.Substring(0, idx);
						sForm = sForm.Trim();
					}
					else
					{
						sSenseNumber = "1";		// may be only sense.
					}
				}
			}
			return sForm;
		}

		int m_nHomograph;
		ILexEntryFactory m_factLexEntry = null;

		private ILexEntry CreateLexEntryForReference(int ws, string sForm, int flid)
		{
			if (m_factLexEntry == null)
				m_factLexEntry = m_cache.ServiceLocator.GetInstance<ILexEntryFactory>();
			// Remove any homograph number.
			string sOrigForm = sForm;
			string sHomograph = String.Empty;
			m_nHomograph = 0;
			for (int idx = sForm.Length - 1; idx >= 0; --idx)
			{
				if (!Char.IsDigit(sForm[idx]))
					break;
				// We do not handle non-Roman homograph numbers, so until then, only accept ASCII digits.
				// Otherwise we would need to use .Parse with a proper locale to parse the character.
				if (sForm[idx] < '0' || sForm[idx] > '9')
					break;
				sHomograph = sForm.Substring(idx) + sHomograph;
				sForm = sForm.Substring(0, idx);
				m_nHomograph = Int32.Parse(sHomograph);
			}
			int clsidForm;
			IMoMorphType morphType = MorphServices.FindMorphType(
				m_cache, ref sForm, out clsidForm);
			SandboxGenericMSA msa = new SandboxGenericMSA();
			if (clsidForm == MoStemAllomorphTags.kClassId)
				msa.MsaType = MsaType.kStem;
			else
				msa.MsaType = MsaType.kUnclassified;
			ITsString tssForm = m_cache.TsStrFactory.MakeString(sForm, ws);
			ILexEntry le = m_factLexEntry.Create(morphType, tssForm, (ITsString)null, msa);
			IncrementCreatedClidCount(LexEntryTags.kClassId);
			ITsString tssResidue;
			string sMsg;
			if (flid == kflidLexicalRelations)
			{
				tssResidue = m_cache.TsStrFactory.MakeString(
					AppStrings.ksCheckCreatedForLexicalRelation, m_cache.DefaultUserWs);
				sMsg = String.Format(AppStrings.ksCreatedForLexicalRelation,
					m_sFilename, le.Hvo, sForm);
			}
			else if (flid == kflidCrossReferences)
			{
				tssResidue = m_cache.TsStrFactory.MakeString(
					AppStrings.ksCheckCreatedForCrossReference, m_cache.DefaultUserWs);
				sMsg = String.Format(AppStrings.ksCreatedForCrossReference,
					m_sFilename, le.Hvo, sForm);
			}
			else if (flid == LexEntryRefTags.kflidComponentLexemes)
			{
				tssResidue = m_cache.TsStrFactory.MakeString(
					AppStrings.ksCheckCreatedForComponentsLink, m_cache.DefaultUserWs);
				sMsg = String.Format(AppStrings.ksCreatedForComponentsLink,
					m_sFilename, le.Hvo, sForm);
			}
			else if (flid == LexEntryRefTags.kflidPrimaryLexemes)
			{
				tssResidue = m_cache.TsStrFactory.MakeString(
					AppStrings.ksCheckCreatedForShowSubentryUnderLink, m_cache.DefaultUserWs);
				sMsg = String.Format(AppStrings.ksCreatedForShowSubentryUnderLink,
					m_sFilename, le.Hvo, sForm);
			}
			else //if (flid != 0)
			{
				tssResidue = m_cache.TsStrFactory.MakeString(
					AppStrings.ksCheckCreatedForLinkTarget, m_cache.DefaultUserWs);
				sMsg = String.Format(AppStrings.ksCreatedForLinkTarget,
					m_sFilename, le.Hvo, sForm);
			}
			le.ImportResidue = tssResidue;
			le.SensesOS[0].ImportResidue = tssResidue;
			if (m_wrtrLog != null)
				LogMessage(sMsg);
			WsString wss = new WsString(ws, sOrigForm);
			m_mapFormEntry[wss] = le;
			return le;
		}

		/// <summary>
		/// Return the list of (localized) format strings used for logging creation of
		/// lexical entries.  This may be needed to parse the log file reliably.
		/// </summary>
		public List<string> CreatedForMessages
		{
			get
			{
				List<String> rgs = new List<string>();
				rgs.Add(AppStrings.ksCreatedForCrossReference);
				rgs.Add(AppStrings.ksCreatedForLexicalRelation);
				rgs.Add(AppStrings.ksCreatedForComponentsLink);
				rgs.Add(AppStrings.ksCreatedForShowSubentryUnderLink);
				rgs.Add(AppStrings.ksCreatedForLinkTarget);
				return rgs;
			}
		}

		private ILexSense GetIndicatedSense(string sSenseNumber, ILexEntry le)
		{
			string[] rgsNum = sSenseNumber.Split(new char[] { '.', ',' }, StringSplitOptions.None);
			if (le.SensesOS.Count == 0)
			{
				if (m_factLexSense == null)
					m_factLexSense = m_cache.ServiceLocator.GetInstance<ILexSenseFactory>();
				le.SensesOS.Add(m_factLexSense.Create());
			}
			IFdoOwningSequence<ILexSense> rgls = le.SensesOS;
			ILexSense ls = rgls[0];
			for (int i = 0; i < rgsNum.Length; ++i)
			{
				int num;
				if (Int32.TryParse(rgsNum[i], out num))
				{
					if (num <= rgls.Count)
					{
						ls = rgls[num - 1];
						rgls = ls.SensesOS;
					}
					else
					{
						break;
					}
				}
				else
				{
					break;
				}
			}
			return ls;
		}

		Dictionary<WsString, ILexRefType> m_mapNameLrt = null;
		Dictionary<WsString, ILexRefType> m_mapAbbrLrt = null;
		Dictionary<WsString, ILexRefType> m_mapRevNameLrt = null;
		Dictionary<WsString, ILexRefType> m_mapRevAbbrLrt = null;

		private ILexRefType GetLexRefType(Dictionary<string, string> dictAttrs,
			out bool fReverse, int nLine)
		{
			EnsureLrtMapsFilled();
			string sWsAnal;
			string sAbbr = null;
			string sName = null;
			fReverse = false;
			if (dictAttrs.TryGetValue("wsa", out sWsAnal))
			{
				int ws = GetWsFromId(sWsAnal);
				ILexRefType lrt;
				WsString wssName = new WsString();
				if (dictAttrs.TryGetValue("name", out sName))
				{
					wssName.ws = ws;
					wssName.sVal = sName;
					if (m_mapNameLrt.TryGetValue(wssName, out lrt))
						return lrt;
					if (m_mapRevNameLrt.TryGetValue(wssName, out lrt))
					{
						fReverse = true;
						return lrt;
					}
				}
				WsString wssAbbr = new WsString();
				if (dictAttrs.TryGetValue("abbr", out sAbbr))
				{
					wssAbbr.ws = ws;
					wssAbbr.sVal = sAbbr;
					if (m_mapAbbrLrt.TryGetValue(wssAbbr, out lrt))
						return lrt;
					if (m_mapRevAbbrLrt.TryGetValue(wssAbbr, out lrt))
					{
						fReverse = true;
						return lrt;
					}
				}
				if (String.IsNullOrEmpty(sName) && String.IsNullOrEmpty(sAbbr))
					return null;
				if (!String.IsNullOrEmpty(sName))
				{
					if (m_mapAbbrLrt.TryGetValue(wssName, out lrt))
					{
						//string sMsg = String.Format(AppStrings.ksTypeNameMatchesAbbr, sName);
						//LogMessage(sMsg, nLine);
						return lrt;
					}
					if (m_mapRevAbbrLrt.TryGetValue(wssName, out lrt))
					{
						fReverse = true;
						//string sMsg = String.Format(AppStrings.ksTypeNameMatchesAbbr, sName);
						//LogMessage(sMsg, nLine);
						return lrt;
					}
				}
				if (!String.IsNullOrEmpty(sAbbr))
				{
					if (m_mapNameLrt.TryGetValue(wssAbbr, out lrt))
					{
						//string sMsg = String.Format(AppStrings.ksTypeAbbrMatchesName, sAbbr);
						//LogMessage(sMsg, nLine);
						return lrt;
					}
					if (m_mapRevNameLrt.TryGetValue(wssAbbr, out lrt))
					{
						fReverse = true;
						//string sMsg = String.Format(AppStrings.ksTypeAbbrMatchesName, sAbbr);
						//LogMessage(sMsg, nLine;)
						return lrt;
					}
				}
				return CreateNewLexRefType(ws, sName, sAbbr);
			}
			return null;
		}

		private ILexRefType CreateNewLexRefType(int ws, string sName, string sAbbr)
		{
			if (m_factLexRefType == null)
				m_factLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
			ILexRefType lrt = m_factLexRefType.Create();
			m_cache.LangProject.LexDbOA.ReferencesOA.PossibilitiesOS.Add(lrt);
			IncrementCreatedClidCount(LexRefTypeTags.kClassId);
			if (String.IsNullOrEmpty(sName))
				sName = sAbbr;
			else if (String.IsNullOrEmpty(sAbbr))
				sAbbr = sName;
			lrt.Name.set_String(ws, m_cache.TsStrFactory.MakeString(sName, ws));
			WsString wss = new WsString(ws, sName);
			m_mapNameLrt[wss] = lrt;
			lrt.Abbreviation.set_String(ws, m_cache.TsStrFactory.MakeString(sAbbr, ws));
			wss = new WsString(ws, sAbbr);
			m_mapAbbrLrt[wss] = lrt;
			// We have to choose a type.  This is the most general in some ways.
			lrt.MappingType = (int)LexRefTypeTags.MappingTypes.kmtEntryOrSensePair;
			string sMsg = String.Format(AppStrings.ksCreatedTypeForLexReference,
				sName, sAbbr);
			LogMessage(sMsg, 0);
			return lrt;
		}

		private void EnsureLrtMapsFilled()
		{
			if (m_mapNameLrt != null)
				return;
			ILexRefTypeRepository repo = m_cache.ServiceLocator.GetInstance<ILexRefTypeRepository>();
			m_mapNameLrt = new Dictionary<WsString, ILexRefType>(s_wssIgnoreCaseComparer);
			m_mapAbbrLrt = new Dictionary<WsString, ILexRefType>(s_wssIgnoreCaseComparer);
			m_mapRevNameLrt = new Dictionary<WsString, ILexRefType>(s_wssIgnoreCaseComparer);
			m_mapRevAbbrLrt = new Dictionary<WsString, ILexRefType>(s_wssIgnoreCaseComparer);
			foreach (ILexRefType lrt in repo.AllInstances())
			{
				int ws;
				for (int i = 0; i < lrt.Name.StringCount; ++i)
				{
					ITsString tss = lrt.Name.GetStringFromIndex(i, out ws);
					if (tss.Length > 0)
					{
						WsString wss = new WsString(ws, tss.Text);
						m_mapNameLrt[wss] = lrt;
					}
				}
				for (int i = 0; i < lrt.Abbreviation.StringCount; ++i)
				{
					ITsString tss = lrt.Abbreviation.GetStringFromIndex(i, out ws);
					if (tss.Length > 0)
					{
						WsString wss = new WsString(ws, tss.Text);
						m_mapAbbrLrt[wss] = lrt;
					}
				}
				for (int i = 0; i < lrt.ReverseName.StringCount; ++i)
				{
					ITsString tss = lrt.ReverseName.GetStringFromIndex(i, out ws);
					if (tss.Length > 0)
					{
						WsString wss = new WsString(ws, tss.Text);
						m_mapRevNameLrt[wss] = lrt;
					}
				}
				for (int i = 0; i < lrt.ReverseAbbreviation.StringCount; ++i)
				{
					ITsString tss = lrt.ReverseAbbreviation.GetStringFromIndex(i, out ws);
					if (tss.Length > 0)
					{
						WsString wss = new WsString(ws, tss.Text);
						m_mapRevAbbrLrt[wss] = lrt;
					}
				}
			}
		}

		private void FillEntryMap()
		{
			if (m_mapFormEntry == null)
				m_mapFormEntry = new Dictionary<WsString, ILexEntry>();
			ILexEntryRepository repo = m_cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			foreach (ILexEntry le in repo.AllInstances())
			{
				ILexEntry leDup;
				StoreEntryInMap(le, false, false, out leDup);
			}
		}

		private void StoreEntryInMap(ILexEntry le, bool fNewEntry, bool fReplaceDuplicate, out ILexEntry leDuplicate)
		{
			string sPrefix = null;
			string sPostfix = null;
			string sHomograph = null;
			if (le.LexemeFormOA != null && le.LexemeFormOA.MorphTypeRA != null)
			{
				sPrefix = le.LexemeFormOA.MorphTypeRA.Prefix;
				sPostfix = le.LexemeFormOA.MorphTypeRA.Postfix;
			}
			if (le.HomographNumber > 0)
				sHomograph = le.HomographNumber.ToString();
			leDuplicate = null;
			var duplicates = new Set<ILexEntry>();	// set of possible duplicates
			ILexEntry leDup;
			if (le.CitationForm != null)
			{
				for (int i = 0; i < le.CitationForm.StringCount; ++i)
				{
					StoreFormEntryMapping(le.CitationForm, i, le, sPrefix, sPostfix, sHomograph, fNewEntry, fReplaceDuplicate, out leDup);
					if (leDup != null && leDup.EntryRefsOS.Count > 0 && !duplicates.Contains(leDup))
						duplicates.Add(leDup);
				}
			}
			if (le.LexemeFormOA != null && le.LexemeFormOA.Form != null)
			{
				for (int i = 0; i < le.LexemeFormOA.Form.StringCount; ++i)
				{
					StoreFormEntryMapping(le.LexemeFormOA.Form, i, le, sPrefix, sPostfix, sHomograph, fNewEntry, fReplaceDuplicate, out leDup);
					if (leDup != null && leDup.EntryRefsOS.Count > 0 && !duplicates.Contains(leDup))
						duplicates.Add(leDup);
				}
			}
			if (le.EntryRefsOS.Count == 0 || duplicates.Count == 0)
				return;
			// merging may be possible.
			foreach (var dup in duplicates)
			{
				if (le.EntryRefsOS.Count != dup.EntryRefsOS.Count)
					continue;
				var fMatch = true;
				for (var i = 0; i < le.EntryRefsOS.Count; ++i)
				{
					if (AreLexEntryRefsSame(le.EntryRefsOS[i], dup.EntryRefsOS[i]))
						continue;
					fMatch = false;
					break;
				}
				if (fMatch)
				{
					leDuplicate = dup;
					return;
				}
			}
		}

		private bool AreLexEntryRefsSame(ILexEntryRef refLex, ILexEntryRef refDup)
		{
			// For SFM import, the only thing that can be compared reliably is the component
			// list.
			return AreComponentsSame(m_rglinks.LinksForField(refLex, LexEntryRefTags.kflidComponentLexemes),
				m_rglinks.LinksForField(refDup, LexEntryRefTags.kflidComponentLexemes));
		}

		private bool AreComponentsSame(IList<PendingLink> refs, IList<PendingLink> refDups)
		{
			if (refs == null && refDups == null)
				return true;
			if (refs == null || refDups == null)
				return false;
			if (refs.Count != refDups.Count)
				return false;
			var matched = true;
			for(var i = 0; i < refs.Count && matched; ++i)
			{
				var resolvedLinkHvo = ResolveLinkReference(refs[i].FieldInformation.FieldId, refs[i], true);
				var resolvedDupHvo = ResolveLinkReference(refDups[i].FieldInformation.FieldId, refDups[i], true);
				matched = resolvedLinkHvo != 0 && resolvedDupHvo == resolvedLinkHvo;
			}
			return matched; // All corresponding items match
		}

		private void StoreFormEntryMapping(IMultiUnicode mu, int i, ILexEntry le,
			string sPrefix, string sPostfix, string sHomograph, bool fNewEntry, bool replaceDuplicate, out ILexEntry leDuplicate)
		{
			leDuplicate = null;
			int ws;
			string sForm = mu.GetStringFromIndex(i, out ws).Text;
			if (String.IsNullOrEmpty(sForm))
				return;
			if (!String.IsNullOrEmpty(sPrefix))
				sForm = sPrefix + sForm;
			if (!String.IsNullOrEmpty(sPostfix))
				sForm = sForm + sPostfix;
			string sForm0 = sForm;
			if (!String.IsNullOrEmpty(sHomograph))
				sForm += sHomograph;
			WsString wss = new WsString(ws, sForm);
			ILexEntry le2;
			if (m_mapFormEntry.TryGetValue(wss, out le2) && le2 != le)
			{
				leDuplicate = le2;
				if (!replaceDuplicate)
					return;
			}
			// The homograph number may be autogenerated.
			if (fNewEntry && le2 == null && !String.IsNullOrEmpty(sHomograph) && m_nHomograph == 0 && replaceDuplicate)
			{
				WsString wss0 = new WsString(ws, sForm0);
				if (m_mapFormEntry.TryGetValue(wss0, out le2) && le2 != le)
				{
					if (le2.HomographNumber != 0)
					{
						leDuplicate = le2;
						wss = wss0;
					}
				}

			}
			m_mapFormEntry[wss] = le;
		}

		Dictionary<int, Dictionary<WsString, int>> m_mapFlidListNameDict = new Dictionary<int, Dictionary<WsString, int>>();
		Dictionary<int, Dictionary<WsString, int>> m_mapFlidListAbbrDict = new Dictionary<int, Dictionary<WsString, int>>();

		private int ResolveLinkReference(int flid, PendingLink pend, bool fHandleForm)
		{
			Dictionary<string, string> dictAttrs = pend.LinkAttributes;
			string sVal;
			if (dictAttrs.TryGetValue("target", out sVal))
			{
				// CmAgentEvaluationTags.kflidTarget				CmObject
				// CmAnnotationTags.kflidInstanceOf					CmObject
				// CmBaseAnnotationTags.kflidBeginObject			CmObject
				// CmBaseAnnotationTags.kflidEndObject				CmObject
				// CmIndirectAnnotationTags.kflidAppliesTo			CmAnnotation
				// LexEntryRefTags.kflidComponentLexemes			CmObject
				// LexEntryRefTags.kflidPrimaryLexemes				CmObject
				// LexReferenceTags.kflidTargets					CmObject
				// LexSenseTags.kflidMorphoSyntaxAnalysis			MoMorphSynAnalysis
				// LexSenseTags.kflidAppendixes						LexAppendix
				// WfiMorphBundleTags.kflidMorph					MoForm
				// WfiMorphBundleTags.kflidMsa						MoMorphSynAnalysis
				// WfiMorphBundleTags.kflidSense					LexSense
				Guid guid;
				if (m_mapIdGuid.TryGetValue(sVal, out guid))
				{
					if (m_repoCmObject.IsValidObjectId(guid))
					{
						ICmObject cmo = m_repoCmObject.GetObject(guid);
						return cmo.Hvo;
					}
					else
					{
						return 0;
					}
				}
			}
			string sWs;
			if (dictAttrs.TryGetValue("ws", out sWs))
			{
				string sName = null;
				string sAbbr = null;
				Dictionary<WsString, int> dictName = null;
				Dictionary<WsString, int> dictAbbr = null;
				int ws = GetWsFromId(sWs);
				if (flid == ReversalIndexTags.kflidWritingSystem)
				{
					return ws;
				}
				if (dictAttrs.TryGetValue("name", out sName))
				{
					if (!m_mapFlidListNameDict.TryGetValue(flid, out dictName))
					{
						dictName = CreateListNameDict(flid);
						m_mapFlidListNameDict.Add(flid, dictName);
					}
					if (flid == LexSenseTags.kflidAnthroCodes)
					{
						// abbreviation may be prepended -- do something about it.
						if (dictAttrs.TryGetValue("abbr", out sAbbr) && sName.StartsWith(sAbbr))
						{
							string s1 = sName.Substring(sAbbr.Length);
							string s2 = s1.Trim();
							if (s1.Length > s2.Length && s2.Length > 0)
								sName = s2;
						}
					}
					int hvo = FindMatchingListItem(sName, ws, dictName);
					if (hvo != 0)
						return hvo;
				}
				if (dictAttrs.TryGetValue("abbr", out sAbbr))
				{
					if (!m_mapFlidListAbbrDict.TryGetValue(flid, out dictAbbr))
					{
						dictAbbr = CreateListAbbrDict(flid);
						m_mapFlidListAbbrDict.Add(flid, dictAbbr);
					}
					int hvo = FindMatchingListItem(sAbbr, ws, dictAbbr);
					if (hvo != 0)
						return hvo;
				}
				string sForm;
				if (dictAttrs.TryGetValue("form", out sForm))
				{
					if (flid == LexSenseTags.kflidReversalEntries)		//ReversalIndexEntry
						return GetMatchingReversalEntry(ws, sForm).Hvo;
				}
				string sSenseNumber;
				sForm = GetLexFormAndSenseNumber(dictAttrs, out sSenseNumber);
				if (!String.IsNullOrEmpty(sForm))
				{
					if (fHandleForm)
					{
						ICmObject cmo = GetEntryOrSense(ws, sForm, sSenseNumber, pend.FieldInformation.FieldId);
						return cmo.Hvo;
					}
					else
					{
						return 0;
					}
				}
				if (ws != 0 && (!String.IsNullOrEmpty(sName) || !String.IsNullOrEmpty(sAbbr)))
				{
					return FindInOtherMapOrCreateListItem(flid, ws, sName, sAbbr,
						ref dictName, ref dictAbbr, pend.LineNumber);
				}
			}
			string sPath;
			if (dictAttrs.TryGetValue("path", out sPath))
			{
				switch (flid)
				{
					case CmMediaTags.kflidMediaFile:				//CmFile
						return GetMediaFile(sPath, pend);
					case CmPictureTags.kflidPictureFile:			//CmFile
						return GetPictureFile(sPath, pend);
				}
			}
			return 0;
		}

		private int FindInOtherMapOrCreateListItem(int flid, int ws, string sName, string sAbbr,
			ref Dictionary<WsString, int> dictName, ref Dictionary<WsString, int> dictAbbr,
			int nLine)
		{
			if (!String.IsNullOrEmpty(sName))
			{
				// Look in the Abbreviation map.
				if (dictAbbr == null)
				{
					if (!m_mapFlidListAbbrDict.TryGetValue(flid, out dictAbbr))
					{
						dictAbbr = CreateListAbbrDict(flid);
						m_mapFlidListAbbrDict.Add(flid, dictAbbr);
					}
				}
				int hvo = FindMatchingListItem(sName, ws, dictAbbr);
				if (hvo != 0)
				{
					string sMsg = String.Format(AppStrings.ksNameMatchesAbbr, sName);
					LogMessage(sMsg, nLine);
					return hvo;
				}
			}
			if (!String.IsNullOrEmpty(sAbbr))
			{
				// Look in the Name map.
				if (dictName == null)
				{
					if (!m_mapFlidListNameDict.TryGetValue(flid, out dictName))
					{
						dictName = CreateListNameDict(flid);
						m_mapFlidListNameDict.Add(flid, dictName);
					}
				}
				int hvo = FindMatchingListItem(sAbbr, ws, dictName);
				if (hvo != 0)
				{
					string sMsg = String.Format(AppStrings.ksAbbrMatchesName, sAbbr);
					LogMessage(sMsg, nLine);
					return hvo;
				}
			}
			return CreateListItem(flid, ws, sName, sAbbr, dictName, dictAbbr);
		}

		private int GetWsFromId(string wsId)
		{
			// TODO WS: is this a lang tag or an ICU locale?
			IWritingSystem ws;
			if (!m_cache.ServiceLocator.WritingSystemManager.GetOrSet(wsId, out ws))
			{
				string sMsg = string.Format(AppStrings.ksCreatingWritingSystem, wsId);
				LogMessage(sMsg, 0);
			}
			return ws.Handle;
		}

		Dictionary<string, int> m_mapPathMediaFile = null;
		Dictionary<string, int> m_mapPathPictureFile = null;
		ICmFolderFactory m_factCmFolder = null;
		ICmFileFactory m_factCmFile = null;

		private int GetMediaFile(string sPath, PendingLink pend)
		{
			if (m_mapPathMediaFile == null)
				CreatePathMediaFileMap();
			int hvo;
			if (m_mapPathMediaFile.TryGetValue(sPath, out hvo))
				return hvo;
			ICmFolder folder = GetDesiredCmFolder("Local Media", m_cache.LangProject.MediaOC);
			if (m_factCmFile == null)
				m_factCmFile = m_cache.ServiceLocator.GetInstance<ICmFileFactory>();
			ICmFile file = m_factCmFile.Create();
			folder.FilesOC.Add(file);
			file.InternalPath = sPath;
			if (!File.Exists(sPath))
			{
				string sMsg = String.Format(AppStrings.ksMissingMediaFile, sPath);
				LogMessage(sMsg, pend.LineNumber);
			}
			IncrementCreatedClidCount(CmFileTags.kClassId);
			m_mapPathMediaFile.Add(sPath, file.Hvo);
			return file.Hvo;
		}

		private void CreatePathMediaFileMap()
		{
			m_mapPathMediaFile = new Dictionary<string, int>();
			foreach (ICmFolder folder in m_cache.LangProject.MediaOC)
			{
				foreach (ICmFile file in folder.FilesOC)
				{
					if (!String.IsNullOrEmpty(file.InternalPath))
					{
						m_mapPathMediaFile[file.InternalPath] = file.Hvo;
						if (!Path.IsPathRooted(file.InternalPath))
						{
							m_mapPathMediaFile[file.AbsoluteInternalPath] = file.Hvo;
						}
					}
					if (!String.IsNullOrEmpty(file.OriginalPath))
						m_mapPathMediaFile[file.OriginalPath] = file.Hvo;
				}
			}
		}

		private int GetPictureFile(string sPath, PendingLink pend)
		{
			if (m_mapPathPictureFile == null)
				CreatePathPictureFileMap();
			int hvo;
			if (m_mapPathPictureFile.TryGetValue(sPath, out hvo))
				return hvo;
			ICmFolder folder = GetDesiredCmFolder("Local Pictures", m_cache.LangProject.PicturesOC);
			if (m_factCmFile == null)
				m_factCmFile = m_cache.ServiceLocator.GetInstance<ICmFileFactory>();
			ICmFile file = m_factCmFile.Create();
			folder.FilesOC.Add(file);
			file.InternalPath = sPath;
			if (!File.Exists(sPath))
			{
				string sMsg = String.Format(AppStrings.ksMissingPictureFile, sPath);
				LogMessage(sMsg, pend.LineNumber);
			}
			m_mapPathPictureFile.Add(sPath, file.Hvo);
			IncrementCreatedClidCount(CmFileTags.kClassId);
			return file.Hvo;
		}

		private ICmFolder GetDesiredCmFolder(string sFolderName, IFdoOwningCollection<ICmFolder> rgfolders)
		{
			int wsEn = m_cache.WritingSystemFactory.GetWsFromStr("en");
			if (wsEn == 0)
				wsEn = m_cache.WritingSystemFactory.UserWs;
			ICmFolder folder = null;
			foreach (ICmFolder cfld in rgfolders)
			{
				if (cfld.Name.get_String(wsEn).Text == sFolderName)
				{
					folder = cfld;
					break;
				}
			}
			if (folder == null)
			{
				if (m_factCmFolder == null)
					m_factCmFolder = m_cache.ServiceLocator.GetInstance<ICmFolderFactory>();
				folder = m_factCmFolder.Create();
				m_cache.LangProject.PicturesOC.Add(folder);
				folder.Name.set_String(wsEn, sFolderName);
				IncrementCreatedClidCount(CmFolderTags.kClassId);
			}
			return folder;
		}

		private void CreatePathPictureFileMap()
		{
			m_mapPathPictureFile = new Dictionary<string, int>();
			foreach (ICmFolder folder in m_cache.LangProject.PicturesOC)
			{
				foreach (ICmFile file in folder.FilesOC)
				{
					if (!String.IsNullOrEmpty(file.InternalPath))
					{
						m_mapPathPictureFile[file.InternalPath] = file.Hvo;
						if (!Path.IsPathRooted(file.InternalPath))
						{
							m_mapPathPictureFile[file.AbsoluteInternalPath] = file.Hvo;
						}
					}
					if (!String.IsNullOrEmpty(file.OriginalPath))
						m_mapPathPictureFile[file.OriginalPath] = file.Hvo;
				}
			}
		}

		private int FindMatchingListItem(string sName, int ws, Dictionary<WsString, int> listDict)
		{
			int hvo;
			WsString wsstr = new WsString(ws, sName);
			if (listDict.TryGetValue(wsstr, out hvo))
				return hvo;
			WsString wsstr2 = new WsString(ws, sName.ToLowerInvariant());
			if (listDict.TryGetValue(wsstr2, out hvo))
				return hvo;
			return 0;
		}

		Dictionary<WsString, IReversalIndexEntry> m_mapFormReversal = null;
		IReversalIndexRepository m_repoIndex;
		ICmPossibilityListFactory m_factList;
		IReversalIndexEntryFactory m_factRevEntry;

		/// <summary>
		/// This gets a matching reversal index entry for the given writing system and form.
		/// Note that we may need to create a reversal index entry, and maybe even a reversal
		/// index.
		/// </summary>
		private IReversalIndexEntry GetMatchingReversalEntry(int wsHvo, string sForm)
		{
			if (m_mapFormReversal == null)
				CreateFormReversalMap();
			if (m_wsManager == null)
				m_wsManager = m_cache.ServiceLocator.GetInstance<IWritingSystemManager>();
			var wsstr = new WsString(wsHvo, sForm);
			IReversalIndexEntry rie;
			if (m_mapFormReversal.TryGetValue(wsstr, out rie))
				return rie;
			// Find (or create) the proper reversal index.
			IReversalIndex riWs = null;
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				if (m_wsManager.GetWsFromStr(ri.WritingSystem) == wsHvo)
				{
					riWs = ri;
					break;
				}
			}
			if (riWs == null)
			{
				var ws = m_wsManager.Get(wsHvo);
				if (m_repoIndex == null)
					m_repoIndex = m_cache.ServiceLocator.GetInstance<IReversalIndexRepository>();
				riWs = m_repoIndex.FindOrCreateIndexForWs(ws.Handle);
				IncrementCreatedClidCount(ReversalIndexTags.kClassId);
				IncrementCreatedClidCount(CmPossibilityListTags.kClassId);
				var sMsg = String.Format(AppStrings.ksCreatingReversalIndex, ws.DisplayLabel, ws.Id);
				LogMessage(sMsg, 0);
			}
			if (m_factRevEntry == null)
				m_factRevEntry = m_cache.ServiceLocator.GetInstance<IReversalIndexEntryFactory>();
			rie = m_factRevEntry.Create();
			riWs.EntriesOC.Add(rie);
			rie.ReversalForm.set_String(wsHvo, sForm);
			IncrementCreatedClidCount(rie.ClassID);
			m_mapFormReversal[wsstr] = rie;
			return rie;
		}

		private void CreateFormReversalMap()
		{
			m_mapFormReversal = new Dictionary<WsString, IReversalIndexEntry>();
			foreach (IReversalIndex ri in m_cache.LangProject.LexDbOA.ReversalIndexesOC)
			{
				AddFormsToFormEntryMap(ri.EntriesOC);
			}
		}

		// for entries (in a collection) that come from the ReversalIndex
		private void AddFormsToFormEntryMap(IFdoOwningCollection<IReversalIndexEntry> entries)
		{
			foreach (IReversalIndexEntry rie in entries)
			{
				AddFormToFormEntryMap(rie);
			}
		}


		private void AddFormToFormEntryMap(IReversalIndexEntry rie)
		{
			for (int i = 0; i < rie.ReversalForm.StringCount; ++i)
			{
				int ws;
				ITsString tss = rie.ReversalForm.GetStringFromIndex(i, out ws);
				if (tss.Length == 0)
					continue;
				WsString wsstr = new WsString(ws, tss.Text);
				m_mapFormReversal[wsstr] = rie;
				AddFormsToFormEntryMap(rie.SubentriesOS);
			}
		}

		// for subentries (in a sequence) that come from ReversalIndexEntry)
		private void AddFormsToFormEntryMap(IFdoOwningSequence<IReversalIndexEntry> entries)
		{
			foreach (IReversalIndexEntry rie in entries)
			{
				AddFormToFormEntryMap(rie);
			}
		}

		private Dictionary<WsString, int> CreateListNameDict(int flid)
		{
			Dictionary<WsString, int> listNameDict = new Dictionary<WsString, int>(s_wssIgnoreCaseComparer);
			ICmPossibilityList cpl = GetListForFlid(flid);
			if (cpl != null)
			{
				IFdoOwningSequence<ICmPossibility> seqPoss = cpl.PossibilitiesOS;
				if (seqPoss != null)
					InitializeByNames(listNameDict, seqPoss);
			}
			return listNameDict;
		}

		private Dictionary<WsString, int> CreateListAbbrDict(int flid)
		{
			Dictionary<WsString, int> listAbbrDict = new Dictionary<WsString, int>(s_wssIgnoreCaseComparer);
			ICmPossibilityList cpl = GetListForFlid(flid);
			if (cpl != null)
			{
				IFdoOwningSequence<ICmPossibility> seqPoss = cpl.PossibilitiesOS;
				if (seqPoss != null)
					InitializeByAbbrs(listAbbrDict, seqPoss);
			}
			return listAbbrDict;
		}

		private ICmPossibilityList GetListForFlid(int flid)
		{
			switch (flid)
			{
				case LexEntryRefTags.kflidComplexEntryTypes:		//LexEntryType
					return m_cache.LangProject.LexDbOA.ComplexEntryTypesOA;
				case LexEntryRefTags.kflidVariantEntryTypes:		//LexEntryType
					return m_cache.LangProject.LexDbOA.VariantEntryTypesOA;
				case MoFormTags.kflidMorphType:						//MoMorphType
					return m_cache.LangProject.LexDbOA.MorphTypesOA;
				case MoStemMsaTags.kflidPartOfSpeech:				//PartOfSpeech
				case MoDerivStepMsaTags.kflidPartOfSpeech:			//PartOfSpeech
				case MoInflAffMsaTags.kflidPartOfSpeech:			//PartOfSpeech
				case MoUnclassifiedAffixMsaTags.kflidPartOfSpeech:	//PartOfSpeech
				case WfiAnalysisTags.kflidCategory:					//PartOfSpeech
					return m_cache.LangProject.PartsOfSpeechOA;
				case CmTranslationTags.kflidType:					//CmPossibility
					return m_cache.LangProject.TranslationTagsOA;
				case LexSenseTags.kflidDomainTypes:					//CmPossibility
					return m_cache.LangProject.LexDbOA.DomainTypesOA;
				case LexSenseTags.kflidSenseType:					//CmPossibility
					return m_cache.LangProject.LexDbOA.SenseTypesOA;
				case LexSenseTags.kflidUsageTypes:					//CmPossibility
					return m_cache.LangProject.LexDbOA.UsageTypesOA;
				case CmAnnotationTags.kflidAnnotationType:			//CmAnnotationDefn
					return m_cache.LangProject.AnnotationDefsOA;
				case LexSenseTags.kflidAnthroCodes:					//CmAnthroItem
					return m_cache.LangProject.AnthroListOA;
				case LexSenseTags.kflidSemanticDomains:				//CmSemanticDomain
					return m_cache.LangProject.SemanticDomainListOA;
				case LexPronunciationTags.kflidLocation:			//CmLocation
					return m_cache.LangProject.LocationsOA;
				case LexSenseTags.kflidStatus:						//CmPossibility
					return m_cache.LangProject.StatusOA;
				default:
					if (m_mdc.IsCustom(flid))
					{
						Guid guid = m_mdc.GetFieldListRoot(flid);
						if (guid != Guid.Empty)
							return m_cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(guid);
					}
					break;
			}
			return null;
		}

		private void InitializeByNames(Dictionary<WsString, int> listNameDict,
			IFdoOwningSequence<ICmPossibility> seqPoss)
		{
			foreach (ICmPossibility poss in seqPoss)
			{
				for (int i = 0; i < poss.Name.StringCount; ++i)
				{
					int ws;
					ITsString tss = poss.Name.GetStringFromIndex(i, out ws);
					if (tss.Length == 0)
						continue;
					WsString wsstr = new WsString(ws, tss.Text);
					listNameDict[wsstr] = poss.Hvo;			// last one found wins.
					string sLower = tss.Text.ToLowerInvariant();
					if (sLower != tss.Text)
					{
						WsString wsstr2 = new WsString(ws, sLower);
						listNameDict[wsstr2] = poss.Hvo;	// last one found wins.
					}
					InitializeByNames(listNameDict, poss.SubPossibilitiesOS);
				}
			}
		}

		private void InitializeByAbbrs(Dictionary<WsString, int> listAbbrDict,
			IFdoOwningSequence<ICmPossibility> seqPoss)
		{
			foreach (ICmPossibility poss in seqPoss)
			{
				for (int i = 0; i < poss.Abbreviation.StringCount; ++i)
				{
					int ws;
					ITsString tss = poss.Abbreviation.GetStringFromIndex(i, out ws);
					if (tss.Length == 0)
						continue;
					WsString wsstr = new WsString(ws, tss.Text);
					listAbbrDict[wsstr] = poss.Hvo;			// last one found wins.
					string sLower = tss.Text.ToLowerInvariant();
					if (sLower != tss.Text)
					{
						WsString wsstr2 = new WsString(ws, sLower);
						listAbbrDict[wsstr2] = poss.Hvo;	// last one found wins.
					}
					InitializeByAbbrs(listAbbrDict, poss.SubPossibilitiesOS);
				}
			}
		}

		ICmPossibilityFactory m_factCmPossibility;
		IChkTermFactory m_factChkTerm;
		ICmAnnotationDefnFactory m_factCmAnnotationDefn;
		ICmAnthroItemFactory m_factCmAnthroItem;
		ICmCustomItemFactory m_factCmCustomItem;
		ICmLocationFactory m_factCmLocation;
		ICmPersonFactory m_factCmPerson;
		ICmSemanticDomainFactory m_factCmSemanticDomain;
		ILexEntryTypeFactory m_factLexEntryType;
		ILexRefTypeFactory m_factLexRefType;
		IMoMorphTypeFactory m_factMoMorphType;
		IPartOfSpeechFactory m_factPartOfSpeech;
		IPhPhonRuleFeatFactory m_factPhPhonRuleFeat;

		private int CreateListItem(int flid, int ws, string sName, string sAbbr,
			Dictionary<WsString, int> dictName, Dictionary<WsString, int> dictAbbr)
		{
			ICmPossibilityList cpl = GetListForFlid(flid);
			if (cpl != null)
			{
				ICmPossibility poss = null;
				switch (cpl.ItemClsid)
				{
					case CmPossibilityTags.kClassId:
						if (m_factCmPossibility == null)
							m_factCmPossibility = m_cache.ServiceLocator.GetInstance<ICmPossibilityFactory>();
						poss = m_factCmPossibility.Create();
						break;
					case ChkTermTags.kClassId:
						if (m_factChkTerm == null)
							m_factChkTerm = m_cache.ServiceLocator.GetInstance<IChkTermFactory>();
						poss = m_factChkTerm.Create() as ICmPossibility;
						break;
					case CmAnnotationDefnTags.kClassId:
						if (m_factCmAnnotationDefn == null)
							m_factCmAnnotationDefn = m_cache.ServiceLocator.GetInstance<ICmAnnotationDefnFactory>();
						poss = m_factCmAnnotationDefn.Create() as ICmPossibility;
						break;
					case CmAnthroItemTags.kClassId:
						if (m_factCmAnthroItem == null)
							m_factCmAnthroItem = m_cache.ServiceLocator.GetInstance<ICmAnthroItemFactory>();
						poss = m_factCmAnthroItem.Create() as ICmPossibility;
						break;
					case CmCustomItemTags.kClassId:
						if (m_factCmCustomItem == null)
							m_factCmCustomItem = m_cache.ServiceLocator.GetInstance<ICmCustomItemFactory>();
						poss = m_factCmCustomItem.Create() as ICmPossibility;
						break;
					case CmLocationTags.kClassId:
						if (m_factCmLocation == null)
							m_factCmLocation = m_cache.ServiceLocator.GetInstance<ICmLocationFactory>();
						poss = m_factCmLocation.Create() as ICmPossibility;
						break;
					case CmPersonTags.kClassId:
						if (m_factCmPerson == null)
							m_factCmPerson = m_cache.ServiceLocator.GetInstance<ICmPersonFactory>();
						poss = m_factCmPerson.Create() as ICmPossibility;
						break;
					case CmSemanticDomainTags.kClassId:
						if (m_factCmSemanticDomain == null)
							m_factCmSemanticDomain = m_cache.ServiceLocator.GetInstance<ICmSemanticDomainFactory>();
						poss = m_factCmSemanticDomain.Create() as ICmPossibility;
						break;
					case LexEntryTypeTags.kClassId:
						if (m_factLexEntryType == null)
							m_factLexEntryType = m_cache.ServiceLocator.GetInstance<ILexEntryTypeFactory>();
						poss = m_factLexEntryType.Create() as ICmPossibility;
						break;
					case LexRefTypeTags.kClassId:
						if (m_factLexRefType == null)
							m_factLexRefType = m_cache.ServiceLocator.GetInstance<ILexRefTypeFactory>();
						poss = m_factLexRefType.Create() as ICmPossibility;
						break;
					case MoMorphTypeTags.kClassId:
						poss = m_factMoMorphType.Create() as ICmPossibility;
						if (m_factMoMorphType == null)
							m_factMoMorphType = m_cache.ServiceLocator.GetInstance<IMoMorphTypeFactory>();
						break;
					case PartOfSpeechTags.kClassId:
						if (m_factPartOfSpeech == null)
							m_factPartOfSpeech = m_cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>();
						poss = m_factPartOfSpeech.Create() as ICmPossibility;
						break;
					case PhPhonRuleFeatTags.kClassId:
						if (m_factPhPhonRuleFeat == null)
							m_factPhPhonRuleFeat = m_cache.ServiceLocator.GetInstance<IPhPhonRuleFeatFactory>();
						poss = m_factPhPhonRuleFeat.Create() as ICmPossibility;
						break;
				}
				if (poss != null)
				{
					cpl.PossibilitiesOS.Add(poss);
					IncrementCreatedClidCount(cpl.ItemClsid);
					// Ensure that there's a name for the item.
					if (String.IsNullOrEmpty(sName) && !String.IsNullOrEmpty(sAbbr))
					{
						sName = sAbbr;
					}
					if (!String.IsNullOrEmpty(sName))
					{
						poss.Name.set_String(ws, sName);
						if (dictName != null)
						{
							WsString wss = new WsString(ws, sName);
							dictName.Add(wss, poss.Hvo);
						}
					}
					if (!String.IsNullOrEmpty(sAbbr))
					{
						poss.Abbreviation.set_String(ws, sAbbr);
						if (dictAbbr != null)
						{
							WsString wss = new WsString(ws, sAbbr);
							dictAbbr.Add(wss, poss.Hvo);
						}
					}
					poss.DiscussionOA = CreateStText();
					EnsureStTextParagraph(poss.DiscussionOA);
					poss.ForeColor = (int)FwTextColor.kclrTransparent;
					poss.UnderColor = (int)FwTextColor.kclrTransparent;
					poss.BackColor = (int)FwTextColor.kclrTransparent;

					string sWs = m_cache.WritingSystemFactory.GetStrFromWs(ws);
					string sList = cpl.Name.UserDefaultWritingSystem.Text;
					switch (cpl.OwningFlid)
					{
						case (int)LangProjectTags.kflidAnthroList:
							sList = AppStrings.ksAnthropologyCategories;
							break;
						case (int)LangProjectTags.kflidSemanticDomainList:
							sList = AppStrings.ksSemanticDomain;
							break;
						case (int)LangProjectTags.kflidPartsOfSpeech:
							sList = AppStrings.ksPartsOfSpeech;
							break;
						case (int)LangProjectTags.kflidLocations:
							sList = AppStrings.ksLocation;
							break;
						case (int)LangProjectTags.kflidPeople:
							sList = AppStrings.ksPeople;
							break;
						case (int)LangProjectTags.kflidConfidenceLevels:
							sList = AppStrings.ksConfidenceLevel;
							break;
					}
					string sMsg = String.Format(AppStrings.ksCreatingItemInList,
						sWs, sAbbr, sName, sList);
					LogMessage(sMsg, 0);
					return poss.Hvo;
				}
			}
			return 0;
		}
	}
}
