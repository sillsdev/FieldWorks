// Copyright (c) 2018-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.DisambiguateInFLExDB;
using SIL.LCModel;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIL.PrepFLExDB
{
	public class Preparer
	{
		private static readonly string[] ToneParsProperties = { "sampleToneParsAllomorphProperty", "sampleToneParsMorphemeProperty" };
		private static readonly string[] FeatureDescriptors = { "+root", "-CP-final", "-CP-initial", "-DP-final", "-DP-initial", "-absolutive",
			"-accusative", "-alone", "-animate", "-animate_absolutive", "-animate_ergative", "-animate_object", "-animate_subject", "-compound", "-conjoins_DP", "-conjoins_IP", "-dative", "-dativeErg", "-ergative", "-finite", "-future", "-genitive", "-human", "-human_absolutive", "-human_ergative", "-human_object", "-human_subject", "-interrogative", "-nominative", "-past", "-question", "-root", "-third_singular_subject", "-wh", "Adj", "AdjP-final", "AdjP-initial", "Adv", "AdvP-final", "AdvP-initial", "Art", "Aux", "C", "CP-final", "CP-initial", "CP-specifier-initial", "Case", "Class", "Cond", "Conj", "Conseq", "DP-final", "DP-initial", "Deg", "Dem", "Det", "Excl", "FocusM", "Greet", "Indef", "InitialConj", "Intj", "N", "Neg", "Num", "P", "PP", "PP-final", "PP-initial", "P_prefix", "P_suffix", "Poss", "Poss_absolutive", "Poss_ergative", "Pron", "PropN", "Q", "QP-final", "QP-initial", "TopicM", "V", "YNQ", "YNQ_prefix", "YNQ_suffix", "ablative", "absolutive", "absolutive.or.dative", "absolutive.or.ergative", "absolutive.or.genitive", "absolutive_prefix", "absolutive_suffix", "accusative", "accusative.or.dative", "accusative.or.genitive", "accusative.or.nominative", "actorVoice", "animate", "animate_absolutive", "animate_ergative", "animate_object", "animate_subject", "antipassive", "applicative", "assumed", "auditory", "case_prefix_ablative", "case_prefix_absolutive", "case_prefix_absolutive.or.dative", "case_prefix_accusative", "case_prefix_accusative.or.dative", "case_prefix_dative", "case_prefix_ergative", "case_prefix_genitive", "case_prefix_instrumental", "case_prefix_locative", "case_prefix_nominative", "case_prefix_oblique", "case_prefix_vocative", "case_suffix_ablative", "case_suffix_absolutive", "case_suffix_absolutive.or.dative", "case_suffix_accusative", "case_suffix_accusative.or.dative", "case_suffix_dative", "case_suffix_ergative", "case_suffix_genitive", "case_suffix_instrumental", "case_suffix_locative", "case_suffix_nominative", "case_suffix_oblique", "case_suffix_vocative", "causative", "causative_syntax", "class_1", "class_10", "class_10_absolutive", "class_10_ergative", "class_10_object", "class_10_subject", "class_11", "class_11_absolutive", "class_11_ergative", "class_11_object", "class_11_subject", "class_12", "class_12_absolutive", "class_12_ergative", "class_12_object", "class_12_subject", "class_13", "class_13_absolutive", "class_13_ergative", "class_13_object", "class_13_subject", "class_14", "class_14_absolutive", "class_14_ergative", "class_14_object", "class_14_subject", "class_15", "class_15_absolutive", "class_15_ergative", "class_15_object", "class_15_subject", "class_16", "class_16_absolutive", "class_16_ergative", "class_16_object", "class_16_subject", "class_17", "class_17_absolutive", "class_17_ergative", "class_17_object", "class_17_subject", "class_18", "class_18_absolutive", "class_18_ergative", "class_18_object", "class_18_subject", "class_19", "class_19_absolutive", "class_19_ergative", "class_19_object", "class_19_subject", "class_1_absolutive", "class_1_ergative", "class_1_object", "class_1_subject", "class_2", "class_20", "class_20_absolutive", "class_20_ergative", "class_20_object", "class_20_subject", "class_21", "class_21_absolutive", "class_21_ergative", "class_21_object", "class_21_subject", "class_22", "class_22_absolutive", "class_22_ergative", "class_22_object", "class_22_subject", "class_23", "class_23_absolutive", "class_23_ergative", "class_23_object", "class_23_subject", "class_24", "class_24_absolutive", "class_24_ergative", "class_24_object", "class_24_subject", "class_25", "class_25_absolutive", "class_25_ergative", "class_25_object", "class_25_subject", "class_2_absolutive", "class_2_ergative", "class_2_object", "class_2_subject", "class_3", "class_3_absolutive", "class_3_ergative", "class_3_object", "class_3_subject", "class_4", "class_4_absolutive", "class_4_ergative", "class_4_object", "class_4_subject", "class_5", "class_5_absolutive", "class_5_ergative", "class_5_object", "class_5_subject", "class_6", "class_6_absolutive", "class_6_ergative", "class_6_object", "class_6_subject", "class_7", "class_7_absolutive", "class_7_ergative", "class_7_object", "class_7_subject", "class_8", "class_8_absolutive", "class_8_ergative", "class_8_object", "class_8_subject", "class_9", "class_9_absolutive", "class_9_ergative", "class_9_object", "class_9_subject", "class_animal", "class_animal_absolutive", "class_animal_ergative", "class_animal_object", "class_animal_subject", "class_bird", "class_boat", "class_clothing", "class_cylindrical", "class_cylindrical_absolutive", "class_cylindrical_ergative", "class_cylindrical_object", "class_cylindrical_subject", "class_flat", "class_generic", "class_human", "class_insect", "class_insect_absolutive", "class_insect_ergative", "class_insect_object", "class_insect_subject", "class_liquid", "class_liquid_absolutive", "class_liquid_ergative", "class_liquid_object", "class_liquid_subject", "class_mechanical", "class_other", "class_other_absolutive", "class_other_ergative", "class_other_object", "class_other_subject", "class_round", "class_round_absolutive", "class_round_ergative", "class_round_object", "class_round_subject", "class_spherical", "class_spherical_absolutive", "class_spherical_ergative", "class_spherical_object", "class_spherical_subject", "class_thin", "class_tree", "class_tree_absolutive", "class_tree_ergative", "class_tree_object", "class_tree_subject", "class_wavy", "class_wood", "class_wood_absolutive", "class_wood_ergative", "class_wood_object", "class_wood_subject", "comma", "comp", "comp_prefix", "comp_suffix", "comparative", "comparative_prefix", "comparative_suffix", "compareAdj", "compareN", "completive", "compound", "conditional", "conditional_prefix", "conditional_suffix", "conj_prefix", "conj_prefix_neg", "conj_prefix_pl", "conj_suffix", "conj_suffix_neg", "conj_suffix_pl", "conjoins_DP", "conjoins_IP", "contemplative", "continuative", "contrafactual", "copular", "copular_prefix", "copular_suffix", "dative", "dative.or.absolutive", "dative.or.accusative", "dative.or.ergative", "dative.or.genitive", "dative.or.nominative", "dativeVoice", "declarative", "definite", "ditransitive", "ditransitive.opt", "dual", "dual_absolutive", "dual_ergative", "dual_object", "dual_subject", "emphatic", "equalAdj", "equalN", "ergative", "ergative.or.absolutive", "ergative.or.dative", "ergative.or.genitive", "ergative_prefix", "ergative_suffix", "excl-final", "excl-initial", "exclusive", "exist", "experiencer", "feminine", "feminine_absolutive", "feminine_ergative", "feminine_object", "feminine_subject", "finite", "first", "first_absolutive", "first_ergative", "first_exclusive", "first_exclusive_absolutive", "first_exclusive_ergative", "first_exclusive_object", "first_exclusive_subject", "first_inclusive", "first_inclusive_absolutive", "first_inclusive_ergative", "first_inclusive_object", "first_inclusive_subject", "first_object", "first_subject", "firsthand", "focus", "focus-final", "focus-initial", "focus_prefix", "focus_suffix", "future", "generic", "genitive", "genitive.or.accusative", "genitive.or.dative", "genitive.or.nominative", "genitive_prefix", "genitive_suffix", "gerund", "goalVoice", "habitual", "hearsay", "human", "human_absolutive", "human_ergative", "human_object", "human_subject", "imperative", "imperfect", "imperfective", "inclusive", "incompletive", "indefinite", "indefiniteAdv", "indefinitePron", "indicative", "inferential", "infinitive", "instrumental", "instrumentalVoice", "interrogative", "intransitive", "irrealis", "locative", "locativeVoice", "locative_case", "makeAdj", "makeAdv", "makeArt", "makeAux", "makeC", "makeCase", "makeClass", "makeCond", "makeConj", "makeConseq", "makeDeg", "makeDem", "makeDet", "makeExcl", "makeFocusM", "makeGreet", "makeIndef", "makeInitialConj", "makeIntj", "makeN", "makeNeg", "makeNum", "makeP", "makePoss", "makePoss_absolutive", "makePoss_ergative", "makePron", "makePropN", "makeQ", "makeTopicM", "makeV", "manner", "masculine", "masculine_absolutive", "masculine_ergative", "masculine_object", "masculine_subject", "mass", "modifies_Adj", "modifies_Adv", "modifies_Adv-reason", "modifies_NP", "modifies_PP", "modifies_Q", "modifies_locative", "modifies_manner", "modifies_reason", "modifies_temporal", "motion", "negative", "negative-polarity", "negative-polarity_prefix", "negative-polarity_suffix", "negative_prefix", "negative_suffix", "neuter", "neuter_absolutive", "neuter_ergative", "neuter_object", "neuter_subject", "nominative", "nominative.or.accusative", "nominative.or.genitive", "nonwitness", "objectVoice", "oblique", "oblique_case", "olfactory", "ordinal", "participle", "particle", "partitive", "passive", "passive.optional", "past", "perception", "perfect", "perfective", "plural", "plural_absolutive", "plural_ergative", "plural_object", "plural_subject", "poss_animate", "poss_class_1", "poss_class_10", "poss_class_11", "poss_class_12", "poss_class_13", "poss_class_14", "poss_class_15", "poss_class_16", "poss_class_17", "poss_class_18", "poss_class_19", "poss_class_2", "poss_class_20", "poss_class_21", "poss_class_22", "poss_class_23", "poss_class_24", "poss_class_25", "poss_class_3", "poss_class_4", "poss_class_5", "poss_class_6", "poss_class_7", "poss_class_8", "poss_class_9", "poss_class_animal", "poss_class_cylindrical", "poss_class_insect", "poss_class_liquid", "poss_class_other", "poss_class_round", "poss_class_spherical", "poss_class_tree", "poss_class_wood", "poss_dual", "poss_exclusive", "poss_feminine", "poss_first", "poss_human", "poss_inanimate", "poss_inclusive", "poss_masculine", "poss_neuter", "poss_nonhuman", "poss_plural", "poss_prefix", "poss_second", "poss_singular", "poss_suffix", "poss_third", "possessed", "possessive", "potential", "present", "progressive", "proper", "purposive", "quantifier", "quantifier_prefix", "quantifier_suffix", "question", "quotative", "realis", "reason", "reciprocal", "reflexive", "relative", "relative_prefix", "relative_suffix", "reportative", "root", "second", "second_absolutive", "second_ergative", "second_object", "second_subject", "secondhand", "sentential", "sentential_-finite", "sentential_-finiteIP", "sentential_-finiteIP_or_transitive", "sentential_-finiteIPpro-drop", "sentential_-finiteIPpro-drop_or_transitive", "sentential_-finiteIPpro-drop_or_transitive_or_question", "sentential_-finite_or_transitive", "sentential_-question", "sentential_A", "sentential_CP", "sentential_CP_or_-finiteIP", "sentential_CP_or_-finiteIP_or_transitive", "sentential_IP", "sentential_IPpro-dropOrCP", "sentential_finite", "sentential_finite_CP", "sentential_finite_IP", "sentential_finite_or_transitive", "sentential_perfective", "sentential_pro-drop", "sentential_question", "sentential_raising", "sentential_raising_or_copular", "sentential_raising_or_perception", "sentential_subjunctive", "sentential_with_object", "singular", "singular_absolutive", "singular_ergative", "singular_object", "singular_subject", "speech_DP", "speech_DP_or_transitive", "speech_DPwh", "speech_DPwh_or_transitive", "speech_PP", "speech_PP_or_transitive", "stative", "subjunctive", "subjunctive_conditional", "subjunctive_conditional_prefix", "subjunctive_conditional_suffix", "superlative", "superlative_prefix", "superlative_suffix", "takes_Adv", "takes_DP", "takes_P", "temporal", "third", "third_absolutive", "third_ergative", "third_object", "third_subject", "thirdhand", "topic", "topic-final", "topic-initial", "topic_prefix", "topic_suffix", "transitive", "transitive_motion", "unreal", "visual", "vocative", "wh", "whQ", "whQ_prefix", "whQ_suffix", "witness" };

		public Preparer(LcmCache cache, bool fShowMessages = true)
		{
			this.Cache = cache;
			this.ShowMessages = fShowMessages;
		}

		public LcmCache Cache { get; set; }
		private bool ShowMessages { get; set; }

		/// <summary>
		/// Creates a new possibility list for PC-PATR feature descriptors.
		/// </summary>
		public void AddPCPATRList()
		{
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var pcpatrList = possListRepository.AllInstances().FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == PcPatrConstants.PcPatrFeatureDescriptorList);
			if (pcpatrList != null)
			{
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				int ws = WritingSystemServices.kwsAnal;
				Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(PcPatrConstants.PcPatrFeatureDescriptorList, ws);
				pcpatrList = possListRepository.AllInstances().Last();
				var factPoss = Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>();
				ws = Cache.DefaultAnalWs;
				foreach (var name in FeatureDescriptors)
				{
					CreateNewPcPatrFeaturePossibility(ws, pcpatrList, factPoss, name);
				}
			});
		}

		private static void CreateNewPcPatrFeaturePossibility(int ws, ICmPossibilityList newList, ICmCustomItemFactory factPoss, string name)
		{
			var poss = factPoss.Create();
			newList.PossibilitiesOS.Add(poss);
			poss.Name.set_String(ws, name);
		}

		/// <summary>
		/// Creates a new sense-level custom field for PC-PATR feature descriptors.
		/// </summary>
		public void AddPCPATRSenseCustomField()
		{
			var customFields = GetListOfCustomFields();
			if (customFields.Find(fd => fd.Name == PcPatrConstants.PcPatrFeatureDescriptorCustomField) != null)
			{
				// already done; quit
				return;
			}
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var pcpatrList = possListRepository.AllInstances().FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == PcPatrConstants.PcPatrFeatureDescriptorList);
			if (pcpatrList == null)
			{
				// need the master possibility list and it does not exist
				if (ShowMessages)
				{
					MessageBox.Show("Need to create the master list of possibilities first.", "Wrong Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				int ws = Cache.DefaultAnalWs;
				// create new custom field
				var fd = new FieldDescription(Cache)
				{
					Name = PcPatrConstants.PcPatrFeatureDescriptorCustomField,
					Userlabel = PcPatrConstants.PcPatrFeatureDescriptorCustomField,
					HelpString = string.Empty,
					Class = LexSenseTags.kClassId
				};
				fd.Type = CellarPropertyType.ReferenceCollection;
				fd.DstCls = CmCustomItemTags.kClassId;
				fd.WsSelector = WritingSystemServices.kwsAnal;

				fd.ListRootId = pcpatrList.Guid;
				fd.UpdateCustomField();
				FieldDescription.ClearDataAbout();
			});
		}

		public List<FieldDescription> GetListOfCustomFields()
		{
			return (from fd in FieldDescription.FieldDescriptors(Cache)
					where fd.IsCustomField //&& GetItem(m_locationComboBox, fd.Class) != null
					select fd).ToList();
		}

		/// <summary>
		/// Creates a new possibility list for TonePars properties.
		/// </summary>
		public void AddToneParsList()
		{
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var toneParsList = possListRepository.AllInstances().FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == ToneParsConstants.ToneParsPropertiesList);
			if (toneParsList != null)
			{
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				int ws = WritingSystemServices.kwsAnal;
				Cache.ServiceLocator.GetInstance<ICmPossibilityListFactory>().CreateUnowned(ToneParsConstants.ToneParsPropertiesList, ws);
				toneParsList = possListRepository.AllInstances().Last();
				var factPoss = Cache.ServiceLocator.GetInstance<ICmCustomItemFactory>();
				ws = Cache.DefaultAnalWs;
				foreach (var name in ToneParsProperties)
				{
					CreateNewToneParsPropertyPossibility(ws, toneParsList, factPoss, name);
				}
			});
		}

		private static void CreateNewToneParsPropertyPossibility(int ws, ICmPossibilityList newList, ICmCustomItemFactory factPoss, string name)
		{
			var poss = factPoss.Create();
			newList.PossibilitiesOS.Add(poss);
			poss.Name.set_String(ws, name);
		}

		/// <summary>
		/// Creates a new sense-level custom field for TonePars property.
		/// </summary>
		public void AddToneParsSenseCustomField()
		{
			var customFields = GetListOfCustomFields();
			if (customFields.Find(fd => fd.Name == ToneParsConstants.ToneParsPropertiesSenseCustomField) != null)
			{
				// already done; quit
				return;
			}
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var toneParsList = possListRepository.AllInstances().FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == ToneParsConstants.ToneParsPropertiesList);
			if (toneParsList == null)
			{
				// need the master possibility list and it does not exist
				if (ShowMessages)
				{
					MessageBox.Show("Need to create the master list of possibilities first.", "Wrong Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				int ws = Cache.DefaultAnalWs;
				// create new custom field
				var fd = new FieldDescription(Cache)
				{
					Name = ToneParsConstants.ToneParsPropertiesSenseCustomField,
					Userlabel = ToneParsConstants.ToneParsPropertiesSenseCustomField,
					HelpString = string.Empty,
					Class = LexSenseTags.kClassId
				};
				fd.Type = CellarPropertyType.ReferenceCollection;
				fd.DstCls = CmCustomItemTags.kClassId;
				fd.WsSelector = WritingSystemServices.kwsAnal;

				fd.ListRootId = toneParsList.Guid;
				fd.UpdateCustomField();
				FieldDescription.ClearDataAbout();
			});
		}

		/// <summary>
		/// Creates a new allomorph-level custom field for TonePars property.
		/// </summary>
		public void AddToneParsFormCustomField()
		{
			var customFields = GetListOfCustomFields();
			if (customFields.Find(fd => fd.Name == ToneParsConstants.ToneParsPropertiesFormCustomField) != null)
			{
				// already done; quit
				return;
			}
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var toneParsList = possListRepository.AllInstances().FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == ToneParsConstants.ToneParsPropertiesList);
			if (toneParsList == null)
			{
				// need the master possibility list and it does not exist
				if (ShowMessages)
				{
					MessageBox.Show("Need to create the master list of possibilities first.", "Wrong Order", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				}
				return;
			}
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				int ws = Cache.DefaultAnalWs;
				// create new custom field
				var fd = new FieldDescription(Cache)
				{
					Name = ToneParsConstants.ToneParsPropertiesFormCustomField,
					Userlabel = ToneParsConstants.ToneParsPropertiesFormCustomField,
					HelpString = string.Empty,
					Class = MoFormTags.kClassId
				};
				fd.Type = CellarPropertyType.ReferenceCollection;
				fd.DstCls = CmCustomItemTags.kClassId;
				fd.WsSelector = WritingSystemServices.kwsAnal;

				fd.ListRootId = toneParsList.Guid;
				fd.UpdateCustomField();
				FieldDescription.ClearDataAbout();
			});
		}

	}
}
