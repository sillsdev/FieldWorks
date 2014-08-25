// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: M3ParserModelRetriever.cs
// Responsibility: John Hatton
//
// <remarks>
//	this is  a MethodObject (see "Refactoring", Fowler).
// </remarks>

using System;
using System.Xml.Linq;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for M3ParserModelRetriever.
	/// </summary>
	/// <remarks>Is public for testing purposes</remarks>
	public class M3ParserModelRetriever : FwDisposableBase, IVwNotifyChange
	{
		private readonly FdoCache m_cache;
		private readonly object m_syncRoot = new object();
		private bool m_updated;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="M3ParserModelRetriever"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public M3ParserModelRetriever(FdoCache cache)
		{
			if (cache == null) throw new ArgumentNullException("cache");

			m_cache = cache;
			m_cache.DomainDataByFlid.AddNotification(this);
			m_updated = true;
		}

		protected override void DisposeManagedResources()
		{
			m_cache.DomainDataByFlid.RemoveNotification(this);
		}

		public bool Updated
		{
			get
			{
				lock (m_syncRoot)
					return m_updated;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool RetrieveModel(out XDocument model, out XDocument template)
		{
			lock (m_syncRoot)
			{
				if (!m_updated)
				{
					model = null;
					template = null;
					return false;
				}
				m_updated = false;
			}

			// According to the fxt template files, GAFAWS is NFC, all others are NFD.
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				ILangProject lp = m_cache.LanguageProject;
				// 1. Export lexicon and/or grammar.
				model = M3ModelExportServices.ExportGrammarAndLexicon(lp);

				// 2. Export GAFAWS data.
				template = M3ModelExportServices.ExportGafaws(lp.PartsOfSpeechOA.PossibilitiesOS);
			}
			return true;
		}

		public bool RetrieveModel(out XDocument model)
		{
			lock (m_syncRoot)
			{
				if (!m_updated)
				{
					model = null;
					return false;
				}
				m_updated = false;
			}

			// According to the fxt template files, GAFAWS is NFC, all others are NFD.
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				ILangProject lp = m_cache.LanguageProject;
				// 1. Export lexicon and/or grammar.
				model = M3ModelExportServices.ExportGrammarAndLexicon(lp);
			}
			return true;
		}

		public void Reset()
		{
			lock (m_syncRoot)
				m_updated = true;
		}

		#region Implementation of IVwNotifyChange

		void IVwNotifyChange.PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			int clsid = m_cache.ServiceLocator.GetObject(hvo).ClassID;
			switch (clsid)
			{
				case LexDbTags.kClassId:
				case LexEntryTags.kClassId:
				case LexSenseTags.kClassId:

				case FsClosedValueTags.kClassId:
				case FsComplexValueTags.kClassId:
				case FsFeatStrucTags.kClassId:
				case FsFeatStrucTypeTags.kClassId:
				case FsClosedFeatureTags.kClassId:
				case FsComplexFeatureTags.kClassId:
				case FsFeatureSystemTags.kClassId:
				case FsSymFeatValTags.kClassId:

				case MoMorphTypeTags.kClassId:
				case MoAdhocProhibGrTags.kClassId:
				case MoAlloAdhocProhibTags.kClassId:
				case MoMorphAdhocProhibTags.kClassId:
				case MoEndoCompoundTags.kClassId:
				case MoExoCompoundTags.kClassId:
				case MoInflAffixSlotTags.kClassId:
				case MoInflAffixTemplateTags.kClassId:
				case MoInflClassTags.kClassId:
				case MoAffixAllomorphTags.kClassId:
				case MoStemAllomorphTags.kClassId:
				case MoAffixProcessTags.kClassId:
				case MoCopyFromInputTags.kClassId:
				case MoInsertPhonesTags.kClassId:
				case MoInsertNCTags.kClassId:
				case MoModifyFromInputTags.kClassId:
				case MoDerivAffMsaTags.kClassId:
				case MoInflAffMsaTags.kClassId:
				case MoUnclassifiedAffixMsaTags.kClassId:
				case MoStemMsaTags.kClassId:
				case MoMorphDataTags.kClassId:

				case PhCodeTags.kClassId:
				case PhIterationContextTags.kClassId:
				case PhMetathesisRuleTags.kClassId:
				case PhRegularRuleTags.kClassId:
				case PhSegRuleRHSTags.kClassId:
				case PhPhonemeSetTags.kClassId:
				case PartOfSpeechTags.kClassId:
				case PhFeatureConstraintTags.kClassId:
				case PhNCFeaturesTags.kClassId:
				case PhNCSegmentsTags.kClassId:
				case PhPhonDataTags.kClassId:
				case PhPhonemeTags.kClassId:
				case PhSequenceContextTags.kClassId:
				case PhSimpleContextBdryTags.kClassId:
				case PhSimpleContextNCTags.kClassId:
				case PhSimpleContextSegTags.kClassId:
				case PhVariableTags.kClassId:
				case PhEnvironmentTags.kClassId:
					lock (m_syncRoot)
						m_updated = true;
					break;
			}
		}

		#endregion
	}
}
