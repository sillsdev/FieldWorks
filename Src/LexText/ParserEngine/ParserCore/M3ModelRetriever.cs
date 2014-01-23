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
using System.Diagnostics;
using System.IO;
using System.Xml;

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
		private readonly string m_modelPath;
		private readonly string m_templatePath;
		private readonly string m_outputDirectory;
		private XmlDocument m_modelDom;
		private XmlDocument m_templateDom;
		private bool m_loaded;

		private readonly object m_syncRoot = new object();

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

			m_outputDirectory = Path.GetTempPath();
			m_modelPath = Path.Combine(m_outputDirectory, m_cache.ProjectId.Name + "ParserFxtResult.xml");
			m_templatePath = Path.Combine(m_outputDirectory, m_cache.ProjectId.Name + "GAFAWSFxtResult.xml");
		}

		protected override void DisposeManagedResources()
		{
			m_cache.DomainDataByFlid.RemoveNotification(this);
		}

		/// <summary>
		///
		/// </summary>
		public bool RetrieveModel()
		{
			lock (m_syncRoot)
			{
				if (m_loaded)
					return false;
				m_loaded = true;
			}

			// According to the fxt template files, GAFAWS is NFC, all others are NFD.
			using (new WorkerThreadReadHelper(m_cache.ServiceLocator.GetInstance<IWorkerThreadReadHandler>()))
			{
				ILangProject lp = m_cache.LanguageProject;
				// 1. Export lexicon and/or grammar.
				m_modelDom = null;
				M3ModelExportServices.ExportGrammarAndLexicon(m_modelPath, lp);

				// 2. Export GAFAWS data.
				m_templateDom = null;
				M3ModelExportServices.ExportGafaws(m_outputDirectory, m_cache.ProjectId.Name,
					lp.PartsOfSpeechOA.PossibilitiesOS);
			}
			return true;
		}

		public void Reset()
		{
			lock (m_syncRoot)
				m_loaded = false;
			m_modelDom = null;
			m_templateDom = null;
		}

		/// <summary>
		/// Get the model (FXT result) DOM
		/// </summary>
		/// <remarks>Is public for testing only</remarks>
		public XmlDocument ModelDom
		{
			get
			{
				Debug.Assert(m_modelPath != null);
				Debug.Assert(File.Exists(m_modelPath));
				if (m_modelDom == null)
				{
					m_modelDom = new XmlDocument();
					m_modelDom.Load(m_modelPath);
				}
				return m_modelDom;
			}
		}

		internal XmlDocument TemplateDom
		{
			get
			{
				if (m_templateDom == null)
				{
				Debug.Assert(m_templatePath != null);
				Debug.Assert(File.Exists(m_templatePath));
					m_templateDom = new XmlDocument();
					m_templateDom.Load(m_templatePath);
				}
				return m_templateDom;

			}
		}

		#region Implementation of IVwNotifyChange

		void IVwNotifyChange.PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			lock (m_syncRoot)
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
						m_loaded = false;
						break;
				}
			}
		}

		#endregion
	}
}
