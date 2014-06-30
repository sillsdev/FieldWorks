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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// Summary description for M3ParserModelRetriever.
	/// </summary>
	/// <remarks>Is public for testing purposes</remarks>
	public class ParserModelChangeListener : FwDisposableBase, IVwNotifyChange
	{
		private readonly FdoCache m_cache;
		private readonly object m_syncRoot = new object();
		private bool m_changed;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ParserModelChangeListener"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ParserModelChangeListener(FdoCache cache)
		{
			if (cache == null) throw new ArgumentNullException("cache");

			m_cache = cache;
			m_cache.DomainDataByFlid.AddNotification(this);
			m_changed = true;
		}

		protected override void DisposeManagedResources()
		{
			m_cache.DomainDataByFlid.RemoveNotification(this);
		}

		public bool ModelChanged
		{
			get
			{
				lock (m_syncRoot)
					return m_changed;
			}
		}

		/// <summary>
		///
		/// </summary>
		public bool Reset()
		{
			lock (m_syncRoot)
			{
				if (!m_changed)
					return false;
				m_changed = false;
			}
			return true;
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
				case LexEntryInflTypeTags.kClassId:

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
				case PartOfSpeechTags.kClassId:

				case PhCodeTags.kClassId:
				case PhIterationContextTags.kClassId:
				case PhMetathesisRuleTags.kClassId:
				case PhRegularRuleTags.kClassId:
				case PhSegRuleRHSTags.kClassId:
				case PhPhonemeSetTags.kClassId:
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
						m_changed = true;
					break;
			}
		}

		#endregion
	}
}
