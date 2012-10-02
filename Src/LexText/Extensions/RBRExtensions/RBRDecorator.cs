using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using Microsoft.Practices.ServiceLocation;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;

namespace RBRExtensions
{
	/// <summary>
	/// Decorator for the various properties used by the extension.
	/// </summary>
	public class RBRDecorator : DomainDataByFlidDecoratorBase
	{
		/// <summary>owner="LexDb" property="AllAllomorphsList"</summary>
		internal const int kflid_Ldb_AllAllomorphsList = 899900;

		/// <summary>owner="LexSense" property="AllWordformClientIDs"</summary>
		internal const int kflid_Sense_AllWordformClientIDs = 899910;

		/// <summary>owner="PartOfSpeech" property="AllSenseClientIDs"</summary>
		internal const int kflid_Cat_AllSenseClientIDs = 899930;
		/// <summary>owner="PartOfSpeech" property="AllMSAClientIDs"</summary>
		internal const int kflid_Cat_AllMSAClientIDs = 899931;
		/// <summary>owner="PartOfSpeech" property="AllEntryClientIDs"</summary>
		internal const int kflid_Cat_AllEntryClientIDs = 899932;
		/// <summary>owner="PartOfSpeech" property="AllWordformClientIDs"</summary>
		internal const int kflid_Cat_AllWordformClientIDs = 899933;
		/// <summary>owner="PartOfSpeech" property="AllCompoundRuleClientIDs"</summary>
		internal const int kflid_Cat_AllCompoundRuleClientIDs = 899934;

		/// <summary>owner="MoForm" property="AllWordformClientIDs"</summary>
		internal const int kflid_MoForm_AllWordformClientIDs = 899940;
		/// <summary>owner="MoForm" property="AllAdHocRuleClientIDs"</summary>
		internal const int kflid_MoForm_AllAdHocRuleClientIDs = 899941;

		/// <summary>owner="LexEntry" property="AllWordformClientIDs"</summary>
		internal const int kflid_Entry_AllWordformClientIDs = 899950;

		/// <summary>owner="PhEnvironment" property="AllAllomorphClientIDs"</summary>
		internal const int kflid_Env_AllAllomorphClientIDs = 899960;

		/// <summary>owner="CmPossibility" property="AllSenseUsageTypes"</summary>
		internal const int kflid_CmPossibility_AllSenseUsageTypes = 899970;
		/// <summary>owner="CmPossibility" property="AllSenseStatuses"</summary>
		internal const int kflid_CmPossibility_AllSenseStatuses = 899971;
		/// <summary>owner="CmPossibility" property="AllSenseTypes"</summary>
		internal const int kflid_CmPossibility_AllSenseTypes = 899972;

		private readonly IPartOfSpeechRepository m_catRepos;
		private readonly ILexEntryRepository m_entryRepos;
		private readonly ILexSenseRepository m_senseRepos;
		private readonly IMoFormRepository m_alloRepos;
		private readonly ISegmentRepository m_segmentRepos;
		private readonly IWfiWordformRepository m_wordformRepos;
		private readonly IMoMorphSynAnalysisRepository m_msaRepos;
		private readonly IWfiAnalysisRepository m_analRepos;
		private readonly IWfiMorphBundleRepository m_mbRepos;
		private readonly IMoAlloAdhocProhibRepository m_alloAdHocCoProhibRepos;
		private readonly IPhEnvironmentRepository m_envRepos;
		private readonly ICmPossibilityRepository m_cmposRepos;

		/// <summary>
		/// Constructor
		/// </summary>
		public RBRDecorator(ISilDataAccessManaged domainDataByFlid, XmlNode configurationNode,
			IServiceLocator services)
			: base(domainDataByFlid)
		{
			m_catRepos = services.GetInstance<IPartOfSpeechRepository>();
			m_senseRepos = services.GetInstance<ILexSenseRepository>();
			m_entryRepos = services.GetInstance<ILexEntryRepository>();
			m_alloRepos = services.GetInstance<IMoFormRepository>();
			m_segmentRepos = services.GetInstance<ISegmentRepository>();
			m_wordformRepos = services.GetInstance<IWfiWordformRepository>();
			m_msaRepos = services.GetInstance<IMoMorphSynAnalysisRepository>();
			m_analRepos = services.GetInstance<IWfiAnalysisRepository>();
			m_mbRepos = services.GetInstance<IWfiMorphBundleRepository>();
			m_alloAdHocCoProhibRepos = services.GetInstance<IMoAlloAdhocProhibRepository>();
			m_envRepos = services.GetInstance<IPhEnvironmentRepository>();
			m_cmposRepos = services.GetInstance<ICmPossibilityRepository>();

			SetOverrideMdc(new ConcorderMdc(services.GetInstance<IFwMetaDataCacheManaged>()));
		}

		private static int[] GetIdsFromEnumerable(IEnumerable<ICmObject> objects)
		{
			var retval = new HashSet<int>();
			retval.UnionWith(from obj in objects select obj.Hvo);
			return retval.ToArray();
		}

		/// <summary>
		/// Override to handle custom props.
		/// </summary>
		public override int[] VecProp(int hvo, int tag)
		{
			var objects = new HashSet<ICmObject>();
			switch (tag)
			{
				default:
					return base.VecProp(hvo, tag);

				case kflid_Ldb_AllAllomorphsList: // Only ones owned by an entry.
					objects.UnionWith(from allo in m_alloRepos.AllInstances()
									 where allo.Owner is ILexEntry
									 orderby allo.ClassID
									 select (ICmObject)allo);
					return GetIdsFromEnumerable(objects);

				case kflid_Sense_AllWordformClientIDs:
					AllWordformsForSense(m_senseRepos.GetObject(hvo), objects);
					return GetIdsFromEnumerable(objects);

				case kflid_CmPossibility_AllSenseUsageTypes:
					var usageType = m_cmposRepos.GetObject(hvo);
					return GetIdsFromEnumerable(from sense in m_senseRepos.AllInstances()
													where sense.UsageTypesRC.Contains(usageType)
												select (ICmObject)sense);
				case kflid_CmPossibility_AllSenseStatuses:
					var status = m_cmposRepos.GetObject(hvo);
					return GetIdsFromEnumerable(from sense in m_senseRepos.AllInstances()
												where sense.StatusRA == status
												select (ICmObject)sense);
				case kflid_CmPossibility_AllSenseTypes:
					var senseType = m_cmposRepos.GetObject(hvo);
					return GetIdsFromEnumerable(from sense in m_senseRepos.AllInstances()
												where sense.SenseTypeRA == senseType
												select (ICmObject)sense);

				case kflid_Env_AllAllomorphClientIDs:
					var env = m_envRepos.GetObject(hvo);
					return GetIdsFromEnumerable(from allo in m_alloRepos.AllInstances()
												where (allo is IMoStemAllomorph && (((IMoStemAllomorph)allo).PhoneEnvRC.Contains(env))
													|| (allo is IMoAffixAllomorph && (((IMoAffixAllomorph)allo).PositionRS.Contains(env) || ((IMoAffixAllomorph)allo).PhoneEnvRC.Contains(env))))
												select (ICmObject)allo);

				case kflid_Cat_AllMSAClientIDs:
					AllMsasForCat(m_catRepos.GetObject(hvo), objects);
					return GetIdsFromEnumerable(objects);
				case kflid_Cat_AllEntryClientIDs:
					var msas = new HashSet<ICmObject>();
					AllMsasForCat(m_catRepos.GetObject(hvo), msas);
					foreach (var msaOwner in msas
						.Select(msa => msa.Owner)
						.Where(msaOwner => msaOwner is ILexEntry && !objects.Contains(msaOwner)))
					{
						objects.Add(msaOwner);
					}
					return GetIdsFromEnumerable(objects);
				case kflid_Cat_AllCompoundRuleClientIDs:
					var msas2 = new HashSet<ICmObject>();
					AllMsasForCat(m_catRepos.GetObject(hvo), msas2);
					foreach (var msaOwner in msas2
						.Select(msa => msa.Owner)
						.Where(msaOwner => msaOwner is IMoCompoundRule && !objects.Contains(msaOwner)))
					{
						objects.Add(msaOwner);
					}
					return GetIdsFromEnumerable(objects);
				case kflid_Cat_AllSenseClientIDs:
					// Find all senses with MSAs that reference this POS.
					var msas4 = new HashSet<ICmObject>();
					AllMsasForCat(m_catRepos.GetObject(hvo), msas4);
					objects.UnionWith(from sense in m_senseRepos.AllInstances()
									  where sense.MorphoSyntaxAnalysisRA != null && msas4.Contains(sense.MorphoSyntaxAnalysisRA)
								  select (ICmObject)sense);
					return GetIdsFromEnumerable(objects);
				case kflid_Cat_AllWordformClientIDs:
					AllWordformsForCat(m_catRepos.GetObject(hvo), objects);
					return GetIdsFromEnumerable(objects);

				case kflid_MoForm_AllWordformClientIDs:
					var allo2 = m_alloRepos.GetObject(hvo);
					return GetIdsFromEnumerable(from mb in m_mbRepos.AllInstances()
												where mb.MorphRA == allo2
												select mb.Owner.Owner);
				case kflid_MoForm_AllAdHocRuleClientIDs:
					var allo3 = m_alloRepos.GetObject(hvo);
					var ahps = (from ahp in m_alloAdHocCoProhibRepos.AllInstances()
												where ahp.FirstAllomorphRA == allo3
													|| ahp.AllomorphsRS.Contains(allo3)
													|| ahp.RestOfAllosRS.Contains(allo3)
												select (ICmObject)ahp).ToList();
					return GetIdsFromEnumerable(ahps);

				case kflid_Entry_AllWordformClientIDs:
					var entry = m_entryRepos.GetObject(hvo);
					return GetIdsFromEnumerable(from mb in m_mbRepos.AllInstances()
												where mb.SenseRA != null && mb.SenseRA.Entry == entry
												select mb.Owner.Owner);
			}
		}

		private void AllWordformsForCat(IPartOfSpeech cat, HashSet<ICmObject> wordforms)
		{
			var anals = new HashSet<ICmObject>();
			AllMlAnalysesForCat(cat, anals);
			wordforms.UnionWith(from anal in anals select anal.Owner);
			anals.Clear();
			AllAnalysesForCat(cat, anals);
			wordforms.UnionWith(from anal in anals select anal.Owner);
		}

		private void AllMlAnalysesForSense(ILexSense sense, HashSet<ICmObject> objects)
		{
			objects.UnionWith(from mb in m_mbRepos.AllInstances()
							  where mb.SenseRA == sense
							  select mb.Owner);
		}

		private void AllWordformsForSense(ILexSense sense, HashSet<ICmObject> wordforms)
		{
			var anals = new HashSet<ICmObject>();
			AllMlAnalysesForSense(sense, anals);
			wordforms.UnionWith(from anal in anals select anal.Owner);
		}

		private void AllAnalysesForCat(IPartOfSpeech cat, HashSet<ICmObject> objects)
		{
			objects.UnionWith(from anal in m_analRepos.AllInstances()
							 where anal.CategoryRA == cat
							 select (ICmObject)anal);
		}

		private void AllMlAnalysesForCat(IPartOfSpeech cat, HashSet<ICmObject> objects)
		{
			var msas = new HashSet<ICmObject>();
			AllMsasForCat(cat, msas);
			objects.UnionWith(from mb in m_mbRepos.AllInstances()
							  where mb.MsaRA != null && msas.Contains(mb.MsaRA)
							  select mb.Owner);
		}

		private void AllMsasForCat(IPartOfSpeech cat, HashSet<ICmObject> objects)
		{
			// MSA owner is not important to this method.
			foreach (var msa in m_msaRepos.AllInstances())
			{
				switch (msa.ClassName)
				{
					default:
						throw new InvalidOperationException("Unrecognized MSA class.");
					case "MoStemMsa":
						var asStemMsa = (IMoStemMsa) msa;
						if (asStemMsa.PartOfSpeechRA == cat)
							objects.Add(msa);
						break;
					case "MoUnclassifiedAffixMsa":
						var asUncAfxMsa = (IMoUnclassifiedAffixMsa) msa;
						if (asUncAfxMsa.PartOfSpeechRA == cat)
							objects.Add(msa);
						break;
					case "MoInflAffMsa":
						var asInflAfxMsa = (IMoInflAffMsa) msa;
						if (asInflAfxMsa.PartOfSpeechRA == cat)
							objects.Add(msa);
						break;
					case "MoDerivStepMsa":
						var asDerivStepMsa = (IMoDerivStepMsa) msa;
						if (asDerivStepMsa.PartOfSpeechRA == cat)
							objects.Add(msa);
						break;
					case "MoDerivAffMsa":
						var asDerivAffMsa = (IMoDerivAffMsa) msa;
						if (asDerivAffMsa.FromPartOfSpeechRA == cat || asDerivAffMsa.ToPartOfSpeechRA == cat)
							objects.Add(msa);
						break;
				}
			}
		}

		/// <summary>
		/// Override to handle custom props.
		/// </summary>
		public override int get_VecSize(int hvo, int tag)
		{
			return VecProp(hvo, tag).Length;
		}
	}

	/// <summary>
	/// MDC decorator for the concorder dlg.
	/// </summary>
	public class ConcorderMdc : FdoMetaDataCacheDecoratorBase
	{
		/// <summary>
		/// Constructor.
		/// </summary>
		public ConcorderMdc(IFwMetaDataCacheManaged metaDataCache)
			: base(metaDataCache)
		{
		}

		/// <summary></summary>
		public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
		{
			throw new NotSupportedException();
		}

		/// <summary></summary>
		public override int GetFieldId2(int luClid, string bstrFieldName, bool fIncludeBaseClasses)
		{
			switch (luClid)
			{
				case LexDbTags.kClassId:
					switch (bstrFieldName)
					{
						case "AllAllomorphsList":
							return RBRDecorator.kflid_Ldb_AllAllomorphsList;
						default:
							return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
					}

				case LexEntryTags.kClassId:
					switch (bstrFieldName)
					{
						case "AllWordformClientIDs":
							return RBRDecorator.kflid_Entry_AllWordformClientIDs;
						default:
							return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
					}

				case LexSenseTags.kClassId:
					switch (bstrFieldName)
					{
						case "AllWordformClientIDs":
							return RBRDecorator.kflid_Sense_AllWordformClientIDs;
						default:
							return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
					}

				case CmPossibilityTags.kClassId:
					switch (bstrFieldName)
					{
						case "AllSenseUsageTypes":
							return RBRDecorator.kflid_CmPossibility_AllSenseUsageTypes;
						case "AllSenseStatuses":
							return RBRDecorator.kflid_CmPossibility_AllSenseStatuses;
						case "AllSenseTypes":
							return RBRDecorator.kflid_CmPossibility_AllSenseTypes;
						default:
							return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
					}

				case PhEnvironmentTags.kClassId:
					switch (bstrFieldName)
					{
						case "AllAllomorphClientIDs":
							return RBRDecorator.kflid_Env_AllAllomorphClientIDs;
						default:
							return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
					}

				case LangProjectTags.kClassId:
					if (bstrFieldName == "WfiWordforms")
						bstrFieldName = "AllWordforms";
					return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);

				case PartOfSpeechTags.kClassId:
					switch (bstrFieldName)
					{
						case "AllSenseClientIDs":
							return RBRDecorator.kflid_Cat_AllSenseClientIDs;
						case "AllMSAClientIDs":
							return RBRDecorator.kflid_Cat_AllMSAClientIDs;
						case "AllEntryClientIDs":
							return RBRDecorator.kflid_Cat_AllEntryClientIDs;
						case "AllWordformClientIDs":
							return RBRDecorator.kflid_Cat_AllWordformClientIDs;
						case "AllCompoundRuleClientIDs":
							return RBRDecorator.kflid_Cat_AllCompoundRuleClientIDs;
						default:
							return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
					}

				case MoFormTags.kClassId:
					switch (bstrFieldName)
					{
						case "AllWordformClientIDs":
							return RBRDecorator.kflid_MoForm_AllWordformClientIDs;
						case "AllAdHocRuleClientIDs":
							return RBRDecorator.kflid_MoForm_AllAdHocRuleClientIDs;
						default:
							return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
					}
			}
			return base.GetFieldId2(luClid, bstrFieldName, fIncludeBaseClasses);
		}

		/// <summary></summary>
		public override int GetFieldId(string bstrClassName, string bstrFieldName, bool fIncludeBaseClasses)
		{
			switch (bstrClassName)
			{
				case "LexDb":
					return GetFieldId2(LexDbTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "LexEntry":
					return GetFieldId2(LexEntryTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "LexSense":
					return GetFieldId2(LexSenseTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "PhEnvironment":
					return GetFieldId2(PhEnvironmentTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "LangProject":
					return GetFieldId2(LangProjectTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "PartOfSpeech":
					return GetFieldId2(PartOfSpeechTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "MoForm":
					return GetFieldId2(MoFormTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				case "CmPossibility":
					return GetFieldId2(CmPossibilityTags.kClassId, bstrFieldName, fIncludeBaseClasses);
				default:
					return base.GetFieldId(bstrClassName, bstrFieldName, fIncludeBaseClasses);
			}
		}

		/// <summary></summary>
		public override string GetOwnClsName(int flid)
		{
			switch (flid)
			{
				case RBRDecorator.kflid_Ldb_AllAllomorphsList: return "LexDb";

				case RBRDecorator.kflid_Entry_AllWordformClientIDs: return "LexEntry";

				case RBRDecorator.kflid_Env_AllAllomorphClientIDs: return "PhEnvironment";

				case RBRDecorator.kflid_Sense_AllWordformClientIDs: return "LexSense";

				case RBRDecorator.kflid_CmPossibility_AllSenseStatuses: // Fall through
				case RBRDecorator.kflid_CmPossibility_AllSenseTypes: // Fall through
				case RBRDecorator.kflid_CmPossibility_AllSenseUsageTypes: return "CmPossibility";

				case RBRDecorator.kflid_Cat_AllSenseClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllMSAClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllEntryClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllWordformClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllCompoundRuleClientIDs: return "PartOfSpeech";

				case RBRDecorator.kflid_MoForm_AllWordformClientIDs: // Fall through
				case RBRDecorator.kflid_MoForm_AllAdHocRuleClientIDs: return "MoForm";

				default: return base.GetOwnClsName(flid);
			}
		}

		/// <summary>
		/// The clerk currently ignores properties with signature 0, so doesn't do more with them.
		/// </summary>
		public override int GetDstClsId(int flid)
		{
			switch (flid)
			{
				case RBRDecorator.kflid_Ldb_AllAllomorphsList: return 0;

				case RBRDecorator.kflid_Entry_AllWordformClientIDs: return 0;

				case RBRDecorator.kflid_Env_AllAllomorphClientIDs: return 0;

				case RBRDecorator.kflid_Sense_AllWordformClientIDs: return 0;

				case RBRDecorator.kflid_CmPossibility_AllSenseStatuses: // Fall through
				case RBRDecorator.kflid_CmPossibility_AllSenseTypes: // Fall through
				case RBRDecorator.kflid_CmPossibility_AllSenseUsageTypes: return 0;

				case RBRDecorator.kflid_Cat_AllSenseClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllMSAClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllEntryClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllWordformClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllCompoundRuleClientIDs: return 0;

				case RBRDecorator.kflid_MoForm_AllWordformClientIDs: // Fall through
				case RBRDecorator.kflid_MoForm_AllAdHocRuleClientIDs: return 0;

				default: return base.GetDstClsId(flid);
			}
		}

		/// <summary></summary>
		public override string GetFieldName(int flid)
		{
			switch (flid)
			{
				case RBRDecorator.kflid_Ldb_AllAllomorphsList: return "AllAllomorphsList";

				case RBRDecorator.kflid_Entry_AllWordformClientIDs: return "AllWordformClientIDs";

				case RBRDecorator.kflid_Env_AllAllomorphClientIDs: return "AllAllomorphClientIDs";

				case RBRDecorator.kflid_Sense_AllWordformClientIDs: return "AllWordformClientIDs";

				case RBRDecorator.kflid_CmPossibility_AllSenseStatuses: return "AllSenseStatuses";
				case RBRDecorator.kflid_CmPossibility_AllSenseUsageTypes: return "AllSenseUsageTypes";
				case RBRDecorator.kflid_CmPossibility_AllSenseTypes: return "AllSenseTypes";

				case RBRDecorator.kflid_Cat_AllSenseClientIDs: return "AllSenseClientIDs";
				case RBRDecorator.kflid_Cat_AllMSAClientIDs: return "AllMSAClientIDs";
				case RBRDecorator.kflid_Cat_AllEntryClientIDs: return "AllEntryClientIDs";
				case RBRDecorator.kflid_Cat_AllWordformClientIDs: return "AllWordformClientIDs";
				case RBRDecorator.kflid_Cat_AllCompoundRuleClientIDs: return "AllCompoundRuleClientIDs";

				case RBRDecorator.kflid_MoForm_AllWordformClientIDs: return "AllWordformClientIDs";
				case RBRDecorator.kflid_MoForm_AllAdHocRuleClientIDs: return "AllAdHocRuleClientIDs";

				default: return base.GetFieldName(flid);
			}
		}

		/// <summary></summary>
		public override int GetFieldType(int flid)
		{
			switch (flid)
			{
				case RBRDecorator.kflid_Ldb_AllAllomorphsList: return (int)CellarPropertyType.ReferenceSequence;

				case RBRDecorator.kflid_Entry_AllWordformClientIDs: return (int)CellarPropertyType.ReferenceSequence;

				case RBRDecorator.kflid_Env_AllAllomorphClientIDs: return (int)CellarPropertyType.ReferenceSequence;

				case RBRDecorator.kflid_Sense_AllWordformClientIDs: return (int)CellarPropertyType.ReferenceSequence;

				case RBRDecorator.kflid_CmPossibility_AllSenseStatuses: // Fall through
				case RBRDecorator.kflid_CmPossibility_AllSenseTypes: // Fall through
				case RBRDecorator.kflid_CmPossibility_AllSenseUsageTypes: return (int)CellarPropertyType.ReferenceSequence;

				case RBRDecorator.kflid_Cat_AllSenseClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllMSAClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllEntryClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllWordformClientIDs: // Fall through
				case RBRDecorator.kflid_Cat_AllCompoundRuleClientIDs: return (int)CellarPropertyType.ReferenceSequence;

				case RBRDecorator.kflid_MoForm_AllWordformClientIDs: // Fall through
				case RBRDecorator.kflid_MoForm_AllAdHocRuleClientIDs: return (int)CellarPropertyType.ReferenceSequence;

				default: return base.GetFieldType(flid);
			}
		}
	}
}