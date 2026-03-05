// Copyright (c) 2015-2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using DocumentFormat.OpenXml.Drawing;
using SIL.Extensions;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology.HermitCrab;
using SIL.Machine.Morphology.HermitCrab.MorphologicalRules;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.GenerateHCConfigForFLExTrans
{
	internal class HCLoaderForFLExTrans : HCLoader
	{
		public static new Language Load(LcmCache cache, IHCLoadErrorLogger logger)
		{
			var loader = new HCLoaderForFLExTrans(cache, logger);
			loader.LoadLanguage();
			return loader.m_language;
		}

		protected HCLoaderForFLExTrans(LcmCache cache, IHCLoadErrorLogger logger) : base(cache, logger)
		{
		}

		protected override void LoadMorphologicalRules(Stratum stratum, ILexEntry entry, IList<IMoForm> allos)
		{
			if (!HasValidRuleForm(entry))
				return;

			if (entry.SensesOS.Count == 0)
			{
				foreach (ILexEntryRef lexEntryRef in entry.EntryRefsOS)
				{
					foreach (ICmObject component in lexEntryRef.ComponentLexemesRS)
					{
						var mainEntry = component as ILexEntry;
						if (mainEntry != null)
						{
							foreach (IMoMorphSynAnalysis msa in mainEntry.MorphoSyntaxAnalysesOC)
							{
								int variantIndex =
									lexEntryRef.ComponentLexemesRS.IndexOf(component) + 1;
								LoadMorphologicalRule(stratum, entry, allos, msa, variantIndex);
							}
						}
						else
						{
							var sense = (ILexSense)component;
							LoadMorphologicalRule(
								stratum,
								entry,
								allos,
								sense.MorphoSyntaxAnalysisRA,
								-1
							);
						}
					}
				}
			}

			foreach (IMoMorphSynAnalysis msa in entry.MorphoSyntaxAnalysesOC)
				LoadMorphologicalRule(stratum, entry, allos, msa, -1);
		}
		private void LoadMorphologicalRule(
	Stratum stratum,
	ILexEntry entry,
	IList<IMoForm> allos,
	IMoMorphSynAnalysis msa,
	int variantIndex
)
		{
			AffixProcessRule mrule = null;
			Stratum s = stratum;
			bool isCliticAffix = false;
			switch (msa.ClassID)
			{
				case MoDerivAffMsaTags.kClassId:
					mrule = LoadDerivAffixProcessRule(entry, (IMoDerivAffMsa)msa, allos);
					break;

				case MoInflAffMsaTags.kClassId:
					var inflMsa = (IMoInflAffMsa)msa;
					if (inflMsa.SlotsRC.Count > 0)
						s = null;
					mrule = LoadInflAffixProcessRule(entry, inflMsa, allos);
					break;

				case MoUnclassifiedAffixMsaTags.kClassId:
					mrule = LoadUnclassifiedAffixProcessRule(
						entry,
						(IMoUnclassifiedAffixMsa)msa,
						allos
					);
					break;

				case MoStemMsaTags.kClassId:
					mrule = LoadCliticAffixProcessRule(entry, (IMoStemMsa)msa, allos);
					isCliticAffix = true;
					break;
			}

			if (mrule != null)
			{
				mrule.Gloss = GetGloss(msa, isCliticAffix);
				if (variantIndex > 0)
				{
					mrule.Gloss += "_variant." + variantIndex + "_";
				}
				AddMorphologicalRule(s, mrule, msa);
			}
		}

		private string GetGloss(IMoMorphSynAnalysis msa, bool isCliticAffix)
		{
			ILexSense sense = msa.OwnerOfClass<ILexEntry>().SenseWithMsa(msa);
			string result = GetGlossViaSense(msa, isCliticAffix, sense);
			return result;
		}
		private string GetGlossForStem(ILexSense sense, IMoMorphSynAnalysis msa, bool isCliticAffix)
		{
			string result = GetGlossViaSense(msa, isCliticAffix, sense);
			return result;
		}

		private string GetGlossViaSense(
	IMoMorphSynAnalysis msa,
	bool isCliticAffix,
	ILexSense sense
)
		{
			string result =
				sense == null
					? null
					: sense.Gloss.BestAnalysisAlternative.Text.Normalize(NormalizationForm.FormD);
			if (msa is IMoStemMsa && sense != null && !isCliticAffix)
			{
				ILexEntry entry = sense.Entry;
				if (entry != null)
				{
					StringBuilder sb = new StringBuilder();
					string formatted = entry.HeadWord.Text;
					formatted = formatted.Replace("#", " ");
					sb.Append(formatted);
					int homograph = entry.HomographNumber;
					if (homograph == 0)
					{
						sb.Append("1");
					}
					sb.Append(".");
					int index = entry.SensesOS.IndexOf(sense);
					sb.Append(index + 1);
					result = sb.ToString().Normalize(NormalizationForm.FormD);
				}
			}

			return result;
		}
		protected override void LoadLexEntries(Stratum stratum, ILexEntry entry, IList<IMoStemAllomorph> allos)
		{
			if (entry.SensesOS.Count == 0)
			{
				foreach (ILexEntryRef lexEntryRef in entry.EntryRefsOS)
				{
					foreach (ILexEntryInflType inflType in GetInflTypes(lexEntryRef))
					{
						foreach (ICmObject component in lexEntryRef.ComponentLexemesRS)
						{
							var mainEntry = component as ILexEntry;
							if (mainEntry != null)
							{
								foreach (
									IMoStemMsa msa in mainEntry.MorphoSyntaxAnalysesOC.OfType<IMoStemMsa>()
								)
									LoadLexEntryOfVariant(stratum, inflType, entry, msa, allos);
							}
							else
							{
								ILexSense sense = (ILexSense)component;
								LoadLexEntryOfVariant(
									stratum,
									inflType,
									entry,
									(IMoStemMsa)sense.MorphoSyntaxAnalysisRA,
									allos
								);
							}
						}
					}
				}
			}

			foreach (ILexSense sense in entry.SensesOS)
			{
				IMoMorphSynAnalysis msaOfSense = sense.MorphoSyntaxAnalysisRA;
				if (msaOfSense != null && msaOfSense.ClassID == MoStemMsaTags.kClassId)
				{
					IMoStemMsa msa = msaOfSense as IMoStemMsa;
					string gloss = GetGlossForStem(sense, msa, false);
					LoadLexEntry(stratum, gloss, msa, allos, entry.ShortName);
				}
			}
		}

		private void LoadLexEntryOfVariant(
	Stratum stratum,
	ILexEntryInflType inflType,
	ILexEntry entry,
	IMoStemMsa msa,
	IList<IMoStemAllomorph> allos
)
		{
			var hcEntry = new LexEntry();

			IMoInflClass inflClass = GetInflClass(msa);
			if (inflClass != null)
				hcEntry.MprFeatures.Add(m_mprFeatures[inflClass]);

			foreach (ICmPossibility prodRestrict in msa.ProdRestrictRC)
				hcEntry.MprFeatures.Add(m_mprFeatures[prodRestrict]);

			// TODO: irregularly inflected forms should be handled by rule blocking in HC
			if (inflType != null)
				hcEntry.MprFeatures.Add(m_mprFeatures[inflType]);

			var glossSB = new StringBuilder();
			// we ignore any prepend material
			//if (inflType != null)
			//{
			//	string prepend = inflType.GlossPrepend.BestAnalysisAlternative.Text;
			//	if (prepend != "***")
			//		glossSB.Append(prepend);
			//}
			glossSB.Append(GetGlossOfVariant(entry));
			// we ignore any append material
			//if (inflType != null)
			//{
			//	string append = inflType.GlossAppend.BestAnalysisAlternative.Text;
			//	if (append != "***")
			//		glossSB.Append(append);
			//}
			// we add the first sense number of this msa
			int senseIndex = 1;
			ILexEntry ownerEntry = msa.Owner as ILexEntry;
			if (ownerEntry != null)
			{
				int index = ownerEntry.AllSenses.IndexOf(
					s => s.MorphoSyntaxAnalysisRA.Hvo == msa.Hvo
				);
				if (index != -1)
				{
					senseIndex = index + 1;
				}
			}
			glossSB.Append(".");
			glossSB.Append(senseIndex);
			glossSB.Append("_variant_");
			hcEntry.Gloss = glossSB.ToString();

			var fs = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
				fs.AddValue(
					m_posFeature,
					m_posFeature.PossibleSymbols["pos" + msa.PartOfSpeechRA.Hvo]
				);
			else
				hcEntry.IsPartial = true;
			FeatureStruct headFS = null;
			if (msa.MsFeaturesOA != null && !msa.MsFeaturesOA.IsEmpty)
				headFS = LoadFeatureStruct(msa.MsFeaturesOA, m_language.SyntacticFeatureSystem);
			if (inflType != null)
			{
				if (
					inflType.SlotsRC.Count == 0
					&& inflType.InflFeatsOA != null
					&& !inflType.InflFeatsOA.IsEmpty
				)
				{
					FeatureStruct inflFS = LoadFeatureStruct(
						inflType.InflFeatsOA,
						m_language.SyntacticFeatureSystem
					);
					if (headFS == null)
						headFS = inflFS;
					else
						headFS.Add(inflFS);
				}
			}
			if (headFS != null)
				fs.AddValue(m_headFeature, headFS);
			fs.Freeze();
			hcEntry.SyntacticFeatureStruct = fs;

			hcEntry.Properties[HCParser.MsaID] = msa.Hvo;
			if (inflType != null)
				hcEntry.Properties[HCParser.InflTypeID] = inflType.Hvo;

			foreach (IMoStemAllomorph allo in allos)
			{
				try
				{
					RootAllomorph hcAllo = LoadRootAllomorph(allo, msa);
					hcEntry.Allomorphs.Add(hcAllo);
					m_allomorphs.GetOrCreate(allo, () => new List<Allomorph>()).Add(hcAllo);
				}
				catch (InvalidShapeException ise)
				{
					m_logger.InvalidShape(ise.String, ise.Position, msa);
				}
			}

			AddEntry(stratum, hcEntry, msa, entry.ShortName);
		}

		private void LoadLexEntry(
	Stratum stratum,
	string gloss,
	IMoStemMsa msa,
	IList<IMoStemAllomorph> allos,
			string name
)
		{
			var hcEntry = new LexEntry();

			IMoInflClass inflClass = GetInflClass(msa);
			if (inflClass != null)
				hcEntry.MprFeatures.Add(m_mprFeatures[inflClass]);

			foreach (ICmPossibility prodRestrict in msa.ProdRestrictRC)
				hcEntry.MprFeatures.Add(m_mprFeatures[prodRestrict]);

			//			hcEntry.Gloss = GetGloss(msa, false);
			hcEntry.Gloss = gloss;

			var fs = new FeatureStruct();
			if (msa.PartOfSpeechRA != null)
				fs.AddValue(
					m_posFeature,
					m_posFeature.PossibleSymbols["pos" + msa.PartOfSpeechRA.Hvo]
				);
			else
				hcEntry.IsPartial = true;
			if (msa.MsFeaturesOA != null && !msa.MsFeaturesOA.IsEmpty)
				fs.AddValue(
					m_headFeature,
					LoadFeatureStruct(msa.MsFeaturesOA, m_language.SyntacticFeatureSystem)
				);
			fs.Freeze();
			hcEntry.SyntacticFeatureStruct = fs;

			hcEntry.Properties[HCParser.MsaID] = msa.Hvo;

			foreach (IMoStemAllomorph allo in allos)
			{
				try
				{
					RootAllomorph hcAllo = LoadRootAllomorph(allo, msa);
					hcEntry.Allomorphs.Add(hcAllo);
					m_allomorphs.GetOrCreate(allo, () => new List<Allomorph>()).Add(hcAllo);
				}
				catch (InvalidShapeException ise)
				{
					m_logger.InvalidShape(ise.String, ise.Position, msa);
				}
			}

			AddEntry(stratum, hcEntry, msa, name);
		}
		private string GetGlossOfVariant(ILexEntry entry)
		{
			StringBuilder sb = new StringBuilder();
			string result = "";
			if (entry != null)
			{
				sb.Append(entry.HeadWord.Text);
				int homograph = entry.HomographNumber;
				if (homograph == 0)
				{
					sb.Append("1");
				}
				result = sb.ToString().Normalize(NormalizationForm.FormD);
			}
			return result;
			;
		}



	}
}
