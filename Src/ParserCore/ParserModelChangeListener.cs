// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using SIL.Code;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.ObjectModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary />
	/// <remarks>Is public for testing purposes</remarks>
	public class ParserModelChangeListener : DisposableBase, IVwNotifyChange
	{
		private readonly LcmCache m_cache;
		private readonly object m_syncRoot = new object();
		private bool m_changed;

		/// <summary />
		public ParserModelChangeListener(LcmCache cache)
		{
			Guard.AgainstNull(cache, nameof(cache));

			m_cache = cache;
			m_cache.DomainDataByFlid.AddNotification(this);
			m_changed = true;
		}

		protected override void DisposeManagedResources()
		{
			m_cache.DomainDataByFlid.RemoveNotification(this);
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");
			base.Dispose(disposing);
		}

		public bool ModelChanged
		{
			get
			{
				lock (m_syncRoot)
				{
					return m_changed;
				}
			}
		}

		/// <summary />
		public bool Reset()
		{
			lock (m_syncRoot)
			{
				if (!m_changed)
				{
					return false;
				}
				m_changed = false;
			}
			return true;
		}

		#region Implementation of IVwNotifyChange

		void IVwNotifyChange.PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			var clsid = m_cache.ServiceLocator.GetObject(hvo).ClassID;
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
					{
						m_changed = true;
					}
					break;
			}
		}

		#endregion
	}
}