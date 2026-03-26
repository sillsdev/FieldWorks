// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.DisambiguateInFLExDB;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.ToneParsFLEx
{
	public class ANABuilder
	{
		public LcmCache Cache { get; set; }
		FieldDescription CustomFormField { get; set; }
		FieldDescription CustomSenseField { get; set; }
		public ICmPossibilityList CustomList { get; set; }

		protected FLExDBExtractor Extractor { get; set; }
		protected OrthoChanger Changer { get; set; }
		IParser hcParser;

		public ANABuilder(LcmCache cache, FLExDBExtractor extractor, OrthoChanger changer)
		{
			Cache = cache;
			Extractor = extractor;
			Changer = changer;
			hcParser = new HCParser(Cache);
			var customFields = Extractor.GetListOfCustomFields();
			CustomFormField = customFields.Find(
				fd => fd.Name == ToneParsConstants.ToneParsPropertiesFormCustomField
			);
			CustomSenseField = customFields.Find(
				fd => fd.Name == ToneParsConstants.ToneParsPropertiesSenseCustomField
			);
			CustomList = extractor.FillCustomList(ToneParsConstants.ToneParsPropertiesList);
		}

		public string ExtractTextSegmentAndParseWordAsANA(ISegment segment)
		{
			hcParser.Update();

			Extractor.BadGlosses.Clear();
			var sb = new StringBuilder();
			foreach (IAnalysis analysis in segment.AnalysesRS)
			{
				var wordform = analysis.Wordform;
				if (wordform == null)
				{
					continue;
				}
				var shape = wordform.Form.VernacularDefaultWritingSystem.Text;
				sb.Append(ExtractANAFromWordShape(shape));
			}
			return sb.ToString();
		}

		public string ExtractANAFromWordShape(string shape)
		{
			var sb = new StringBuilder();
			var sbA = new StringBuilder();
			var sbD = new StringBuilder();
			//var sbFD = new StringBuilder();
			var sbP = new StringBuilder();
			var sbU = new StringBuilder();
			var sbW = new StringBuilder();
			sbA.Append("\\a ");
			sbD.Append("\\d ");
			//sbFD.Append("\\fd ");
			sbP.Append("\\p ");
			sbU.Append("\\u ");
			sbW.Append("\\w ");
			sbW.Append(shape + "\n");

			ParseResult parserResult = ParseWordWithHermitCrab(shape);

			int ambiguities = parserResult.Analyses.Count;
			if (ambiguities > 1)
			{
				string ambigs = "%" + ambiguities + "%";
				sbA.Append(ambigs);
				sbD.Append(ambigs);
				sbP.Append(ambigs);
				sbU.Append(ambigs);
			}
			foreach (ParseAnalysis pAnalysis in parserResult.Analyses)
			{
				ParseMorph previous = null;
				IMoForm previousMorph = null;
				var maxMorphs = pAnalysis.Morphs.Count;
				int i = 0;
				foreach (ParseMorph pMorph in pAnalysis.Morphs)
				{
					var msa = pMorph.Msa;
					if (msa == null)
					{
						sbA = Extractor.MissingItemFound(sbA, "GRAMMATICAL_INFO");
						continue;
					}
					var morph = pMorph.Form;
					if (morph == null)
					{
						sbD = Extractor.MissingItemFound(sbD, "FORM");
						sbU = Extractor.MissingItemFound(sbU, "FORM");
						continue;
					}
					if (
						msa is IMoStemMsa
						&& !Extractor.IsAttachedClitic(morph.MorphTypeRA.Guid, maxMorphs)
					)
					{
						if (previous == null)
							sbA.Append("< ");
						else
						{
							if (
								previousMorph.MorphTypeRA.IsPrefixishType
								|| previousMorph.MorphTypeRA.Guid
									== MoMorphTypeTags.kguidMorphProclitic
							)
								sbA.Append(" < ");
						}
					}
					if (
						msa is IMoStemMsa
						&& !Extractor.IsAttachedClitic(morph.MorphTypeRA.Guid, maxMorphs)
					)
					{
						var cat = msa.PartOfSpeechForWsTSS(Cache.DefaultAnalWs).Text;
						sbA.Append("W ");
					}
					else if (i > 0)
						sbA.Append(" ");
					if (morph != null)
					{
						string sForm = morph.Form.VernacularDefaultWritingSystem.Text;
						if (sForm == "^0" || sForm == "&0" || sForm == "*0" || sForm == "∅")
						{
							sForm = "0";
						}
						sbD.Append(sForm);
						sbU.Append("0");
					}
					sbA.Append(msa.Hvo);
					if (
						msa is IMoStemMsa
						&& !Extractor.IsAttachedClitic(morph.MorphTypeRA.Guid, maxMorphs)
					)
					{
						sbA.Append(" ");
						var next = pAnalysis.Morphs.ElementAtOrDefault(i + 1);
						if (next == null)
							sbA.Append(">");
						else
						{
							var nextMorph = next.Form;
							if (
								nextMorph == null
								|| nextMorph.MorphTypeRA.IsSuffixishType
								|| nextMorph.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphEnclitic
							)
								sbA.Append(">");
						}
					}
					sbP.Append(GetAnaProperties(pMorph));
					previous = pMorph;
					previousMorph = morph;
					i++;
					if (i < maxMorphs)
					{
						sbD.Append("+");
						sbP.Append("=");
						sbU.Append("+");
					}
				}
				if (ambiguities > 1)
				{
					sbA.Append("%");
					sbD.Append("%");
					sbP.Append("%");
					sbU.Append("%");
				}
			}
			sbA.Append("\n");
			sbD.Append("\n");
			sbP.Append("\n");
			sbU.Append("\n");
			sbW.Append("\n");
			sb.Append(sbA.ToString());
			sb.Append(sbD.ToString());
			sb.Append(sbP.ToString());
			sb.Append(sbU.ToString());
			sb.Append(sbW.ToString());
			sb.Append("\n");
			return sb.ToString();
		}

		private string GetAnaProperties(ParseMorph pMorph)
		{
			StringBuilder sb = new StringBuilder();
			IMoMorphSynAnalysis msa = pMorph.Msa;
			switch (msa.ClassID)
			{
				case MoStemMsaTags.kClassId:
					var stemMsa = msa as IMoStemMsa;
					sb.Append("RootPOS");
					sb.Append(stemMsa.PartOfSpeechRA.Hvo);
					break;
				case MoInflAffMsaTags.kClassId:
					break;
				case MoDerivAffMsaTags.kClassId:
					break;
				case MoUnclassifiedAffixMsaTags.kClassId:
					break;
			}
			var senseProps = GetPropertiesFromSense(msa, pMorph.Form);
			sb.Append(senseProps);
			var alloProps = GetPropertiesFromForm(pMorph.Form, CustomFormField);
			sb.Append(alloProps);
			return sb.ToString();
		}

		private string GetPropertiesFromSense(IMoMorphSynAnalysis msa, IMoForm morph)
		{
			string returnValue = "";
			var entryOfMsa = (ILexEntry)msa.Owner;
			ILexSense sense = entryOfMsa.SensesOS.FirstOrDefault(
				s => s.MorphoSyntaxAnalysisRA == msa
			);
			if (sense != null)
			{
				returnValue = GetPropertiesFromSense(sense);
			}
			else if (morph != null)
			{
				var entry = (ILexEntry)morph.Owner;
				var sense2 = entry.SensesOS.FirstOrDefault();
				if (sense2 != null)
				{
					returnValue = GetPropertiesFromSense(sense2);
				}
			}
			return returnValue;
		}

		private string GetPropertiesFromSense(ILexSense sense)
		{
			var sb = new StringBuilder();
			if (CustomSenseField != null)
			{
				IList<string> props = new List<string>() { };
				var size = Cache.MainCacheAccessor.get_VecSize(sense.Hvo, CustomSenseField.Id);
				for (int i = 0; i < size; i++)
				{
					var hvo = Cache.MainCacheAccessor.get_VecItem(
						sense.Hvo,
						CustomSenseField.Id,
						i
					);
					if (CustomSenseField != null)
					{
						var item = CustomList.PossibilitiesOS.Where(ps => ps.Hvo == hvo);
						var prop = item.ElementAt(0).Name.BestAnalysisAlternative.Text;
						props.Add(prop);
					}
				}
				props = props.OrderBy(fd => fd).ToList();
				foreach (string fd in props)
				{
					sb.Append(" " + fd);
				}
			}
			return sb.ToString();
		}

		private string GetPropertiesFromForm(IMoForm form, FieldDescription customField)
		{
			var sb = new StringBuilder();
			if (customField != null)
			{
				IList<string> props = new List<string>() { };
				var size = Cache.MainCacheAccessor.get_VecSize(form.Hvo, customField.Id);
				for (int i = 0; i < size; i++)
				{
					var hvo = Cache.MainCacheAccessor.get_VecItem(form.Hvo, customField.Id, i);
					if (CustomFormField != null)
					{
						var item = CustomList.PossibilitiesOS.Where(ps => ps.Hvo == hvo);
						var prop = item.ElementAt(0).Name.BestAnalysisAlternative.Text;
						props.Add(prop);
					}
				}
				props = props.OrderBy(fd => fd).ToList();
				foreach (string fd in props)
				{
					sb.Append(" " + fd);
				}
			}
			return sb.ToString();
		}

		private ParseResult ParseWordWithHermitCrab(string word)
		{
			if (Changer.ChangesExist)
			{
				word = Changer.ApplyChangesToWord(word);
			}
			ParseResult result = hcParser.ParseWord(word);
			return result;
		}
	}
}
