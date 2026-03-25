// Copyright (c) 2018-2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

//using SIL.LcmLoaderUI;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.DisambiguateInFLExDB
{
	public class FLExDBExtractor
	{
		public LcmCache Cache { get; set; }

		public ICmPossibilityList CustomSenseList { get; set; }

		FieldDescription CustomField { get; set; }

		public List<String> BadGlosses { get; set; }

		public string MissingItemMessage { get; set; }

		public FLExDBExtractor(LcmCache cache)
		{
			Cache = cache;

			CustomSenseList = FillCustomList(PcPatrConstants.PcPatrFeatureDescriptorList);

			var customFields = GetListOfCustomFields();
			CustomField = customFields.Find(
				fd => fd.Name == PcPatrConstants.PcPatrFeatureDescriptorCustomField
			);
			BadGlosses = new List<string>();
			MissingItemMessage = "_FOUND!_PLEASE_FIX_THIS_ANALYSIS_IN_Word_Analyses";
		}

		public ICmPossibilityList FillCustomList(string listName)
		{
			var possListRepository =
				Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var customList = possListRepository
				.AllInstances()
				.FirstOrDefault(list => list.Name.BestAnalysisAlternative.Text == listName);
			return customList;
		}

		public string ExtractPcPatrLexicon()
		{
			var sb = new StringBuilder();
			var lexEntries = Cache.LanguageProject.LexDbOA.Entries;
			foreach (ILexEntry entry in lexEntries.OrderBy(e => e.ShortName))
			{
				formatEntry(entry, sb);
			}
			return sb.ToString();
		}

		protected void formatEntry(ILexEntry entry, StringBuilder sb)
		{
			var sense = entry.SensesOS.FirstOrDefault<ILexSense>();
			if (sense == null)
			{ // ignore variants for now
				return;
			}
			var msa = sense.MorphoSyntaxAnalysisRA as IMoStemMsa;
			if (msa == null)
				return;

			sb.Append("\\w ");
			sb.Append(entry.LexemeFormOA.Form.BestVernacularAlternative.Text);
			sb.Append("\n\\c ");
			var pos = msa.PartOfSpeechRA;
			if (pos != null)
				sb.Append(pos.Abbreviation.BestAnalysisAlternative.Text);
			else
				sb.Append("any");
			sb.Append("\n\\g ");
			sb.Append(sense.Gloss.BestAnalysisAlternative.Text);
			sb.Append("\n\\f");
			sb.Append(GetFeatureDescriptorsFromSense(sense, CustomField));
			sb.Append("\n\n");
		}

		public string GetFeatureDescriptorsFromSense(ILexSense sense, FieldDescription customField)
		{
			var sb = new StringBuilder();
			if (customField != null)
			{
				IList<string> fds = new List<string>() { };
				var size = Cache.MainCacheAccessor.get_VecSize(sense.Hvo, customField.Id);
				for (int i = 0; i < size; i++)
				{
					var hvo = Cache.MainCacheAccessor.get_VecItem(sense.Hvo, customField.Id, i);
					if (CustomSenseList != null)
					{
						var item = CustomSenseList.PossibilitiesOS.Where(ps => ps.Hvo == hvo);
						var fd = item.ElementAt(0).Name.BestAnalysisAlternative.Text;
						fds.Add(fd);
					}
				}
				fds = fds.OrderBy(fd => fd).ToList();
				foreach (string fd in fds)
				{
					sb.Append(" " + fd);
				}
			}
			return sb.ToString();
		}

		public List<FieldDescription> GetListOfCustomFields()
		{
			return (
				from fd in FieldDescription.FieldDescriptors(Cache)
				where fd.IsCustomField //&& GetItem(m_locationComboBox, fd.Class) != null
				select fd
			).ToList();
		}

		public string ExtractTextSegmentAsANA(ISegment segment)
		{
			BadGlosses.Clear();
			var sb = new StringBuilder();
			var sbA = new StringBuilder();
			var sbD = new StringBuilder();
			var sbC = new StringBuilder();
			var sbFD = new StringBuilder();
			var sbP = new StringBuilder();
			var sbW = new StringBuilder();
			foreach (IAnalysis analysis in segment.AnalysesRS)
			{
				var wordform = analysis.Wordform;
				if (wordform == null)
				{
					continue;
				}
				sbA.Clear();
				sbA.Append("\\a ");
				sbD.Clear();
				sbD.Append("\\d ");
				sbC.Clear();
				sbC.Append("\\cat ");
				sbFD.Clear();
				sbFD.Append("\\fd ");
				sbP.Clear();
				sbP.Append("\\p ");
				sbW.Clear();
				sbW.Append("\\w ");
				var shape = wordform.Form.VernacularDefaultWritingSystem.Text;
				sbW.Append(shape + "\n");
				int ambiguities = wordform.AnalysesOC.Count;
				if (ambiguities > 1)
				{
					String ambigs = "%" + ambiguities + "%";
					sbA.Append(ambigs);
					sbD.Append(ambigs);
					sbC.Append(ambigs);
					sbFD.Append(ambigs);
					sbP.Append(ambigs);
				}
				foreach (IWfiAnalysis wfiAnalysis in wordform.AnalysesOC)
				{
					IWfiMorphBundle previous = null;
					IMoForm previousMorph = null;
					var maxMorphs = wfiAnalysis.MorphBundlesOS.Count;
					int i = 0;
					foreach (IWfiMorphBundle bundle in wfiAnalysis.MorphBundlesOS)
					{
						var msa = bundle.MsaRA;
						if (msa == null)
						{
							sbA = MissingItemFound(sbA, "GRAMMATICAL_INFO");
							continue;
						}
						var morph = bundle.MorphRA;
						if (morph == null)
						{
							sbD = MissingItemFound(sbD, "FORM");
							continue;
						}
						if (
							msa is IMoStemMsa
							&& !IsAttachedClitic(morph.MorphTypeRA.Guid, maxMorphs)
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
							&& !IsAttachedClitic(morph.MorphTypeRA.Guid, maxMorphs)
						)
						{
							var cat = msa.PartOfSpeechForWsTSS(Cache.DefaultAnalWs).Text;
							sbA.Append(cat + " ");
						}
						else if (i > 0)
							sbA.Append(" ");
						if (morph != null)
						{
							sbD.Append(morph.Form.VernacularDefaultWritingSystem.Text);
						}
						var sense = bundle.SenseRA;
						if (sense == null)
						{ // a sense can be missing from a bundle if the bundle is built by the parser filer
							var entryOfMsa = (ILexEntry)msa.Owner;
							sense = entryOfMsa.SensesOS.FirstOrDefault(
								s => s.MorphoSyntaxAnalysisRA == msa
							);
							if (sense != null)
							{
								HandleSense(sbA, sbFD, sense, CustomField);
							}
							else if (morph != null)
							{
								var entry = (ILexEntry)morph.Owner;
								var sense2 = entry.SensesOS.FirstOrDefault();
								if (sense2 == null)
								{
									sbA.Append("missing_sense");
								}
								else
								{
									HandleSense(sbA, sbFD, sense2, CustomField);
								}
							}
						}
						else
						{
							HandleSense(sbA, sbFD, sense, CustomField);
						}
						if (
							msa is IMoStemMsa
							&& !IsAttachedClitic(morph.MorphTypeRA.Guid, maxMorphs)
						)
						{
							sbA.Append(" ");
							var next = wfiAnalysis.MorphBundlesOS.ElementAtOrDefault(i + 1);
							if (next == null)
								sbA.Append(">");
							else
							{
								var nextMorph = next.MorphRA;
								if (
									nextMorph == null
									|| nextMorph.MorphTypeRA.IsSuffixishType
									|| nextMorph.MorphTypeRA.Guid
										== MoMorphTypeTags.kguidMorphEnclitic
								)
									sbA.Append(">");
							}
						}
						sbP.Append(bundle.Guid.ToString());
						previous = bundle;
						previousMorph = morph;
						i++;
						if (i < maxMorphs)
						{
							sbD.Append("-");
							sbFD.Append("=");
							sbP.Append("=");
						}
					}
					sbC.Append(GetOrComputeWordCategory(wfiAnalysis));
					if (ambiguities > 1)
					{
						sbA.Append("%");
						sbD.Append("%");
						sbC.Append("%");
						sbFD.Append("%");
						sbP.Append("%");
					}
				}
				sbA.Append("\n");
				sbD.Append("\n");
				sbC.Append("\n");
				sbFD.Append("\n");
				sbP.Append("\n");
				sbW.Append("\n");
				sb.Append(sbA.ToString());
				sb.Append(sbD.ToString());
				sb.Append(sbC.ToString());
				sb.Append(sbP.ToString());
				sb.Append(sbFD.ToString());
				sb.Append(sbW.ToString());
			}
			return sb.ToString();
		}

		public StringBuilder MissingItemFound(StringBuilder sb, string item)
		{
			sb.Append("MISSING_");
			sb.Append(item);
			sb.Append(MissingItemMessage);
			return sb;
		}

		public void HandleSense(
			StringBuilder sbA,
			StringBuilder sbFD,
			ILexSense sense,
			FieldDescription customField
		)
		{
			var gloss = sense.Gloss.BestAnalysisAlternative.Text;
			sbA.Append(gloss);
			if (gloss.Contains(" ") && !BadGlosses.Contains(gloss))
			{
				BadGlosses.Add(gloss);
			}
			var fds = GetFeatureDescriptorsFromSense(sense, customField);
			fds = (fds.Length > 1) ? fds.Substring(1) : fds;
			sbFD.Append(fds);
		}

		public bool IsAttachedClitic(Guid mtypeGuid, int bundles)
		{
			if (
				mtypeGuid == MoMorphTypeTags.kguidMorphEnclitic
				|| mtypeGuid == MoMorphTypeTags.kguidMorphProclitic
			)
			{
				if (bundles > 1)
					return true;
			}
			return false;
		}

		public String GetOrComputeWordCategory(IWfiAnalysis wfiAnalysis)
		{
			String result = "";
			if (wfiAnalysis == null)
				return result;
			var cat = wfiAnalysis.CategoryRA;
			if (cat != null)
				return cat.Abbreviation.AnalysisDefaultWritingSystem.Text;
			var bundles = wfiAnalysis.MorphBundlesOS.Count;
			if (bundles == 1)
			{
				var bundle = wfiAnalysis.MorphBundlesOS.ElementAtOrDefault(0);
				return GetStemsCategory(bundle);
			}
			var stems = wfiAnalysis.MorphBundlesOS.Where(
				b =>
					b.MsaRA is IMoStemMsa
					&& b.MorphRA != null
					&& !IsAttachedClitic(b.MorphRA.MorphTypeRA.Guid, 2)
			);
			if (stems.Count() == 1)
			{
				var firstStem = wfiAnalysis.MorphBundlesOS.First(
					b =>
						b.MsaRA is IMoStemMsa
						&& b.MorphRA != null
						&& !IsAttachedClitic(b.MorphRA.MorphTypeRA.Guid, 2)
				);
				result = GetStemsCategory(firstStem);
			}
			else if (stems.Count() > 1)
			{ // has at least one stem compound
			  // Use right-most for now
				var rightmostStem = stems.Last();
				result = GetStemsCategory(rightmostStem);
			}
			return result;
		}

		private string GetStemsCategory(IWfiMorphBundle bundle)
		{
			string result = "";
			var msa = bundle.MsaRA as IMoStemMsa;
			if (msa != null)
			{
				var pos = msa.PartOfSpeechForWsTSS(Cache.DefaultAnalWs);
				if (pos != null)
					result = pos.Text;
			}
			return result;
		}
	}

	public static class PcPatrConstants
	{
		public const string PcPatrFeatureDescriptorCustomField = "PCPATR";
		public const string PcPatrFeatureDescriptorList = "PCPATR Feature Descriptors";
	}
}
