// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using HCSynthByGlossLib;
using SIL.Machine.FeatureModel;
using SIL.Machine.Morphology;
using SIL.Machine.Morphology.HermitCrab;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
namespace HCSynthByGloss
{
	public class Synthesizer
	{
		private static readonly Synthesizer instance = new Synthesizer();
		public object Trace { get; set; }

		public static Synthesizer Instance
		{
			get { return instance; }
		}

		public string SynthesizeGlosses(
			string glosses,
			Morpher morpher,
			Language lang,
			ISynTraceManager traceManager
		)
		{
			StringBuilder sb = new StringBuilder();
			var analysesCreator = AnalysesCreator.Instance;
			var formatter = SynthesizedWordFomatter.Instance;
			int indexCaret = glosses.IndexOf("^");
			int indexBeg = glosses.IndexOf("^");
			int indexEnd = glosses.IndexOf("$");
			while (indexCaret >= 0 && indexBeg >= 0 && indexEnd >= 0)
			{
				string analysis = glosses.Substring(indexBeg, (indexEnd - indexBeg) + 1);
				List<Morpheme> morphemes = analysesCreator.ExtractMorphemes(analysis, morpher);
				if (morphemes.Contains(null))
				{
					sb.Append(formatter.Format(new List<string>(), analysis));
					CollectMissingGlosses(sb, analysesCreator, morphemes);
					CheckForDuplicates(morpher, sb, morphemes);
					AddToTracing(traceManager, sb, analysis);
				}
				else
				{
					WordAnalysis wordAnalysis = new WordAnalysis(
						morphemes,
						analysesCreator.RootIndex,
						analysesCreator.category
					);
					IEnumerable<string> newSyntheses = new List<string>();
					if (traceManager.IsTracing)
					{
						LexEntry rootEntry = morphemes[analysesCreator.RootIndex] as LexEntry;
						FeatureStruct realizationalFS = new FeatureStruct();
						var results = new HashSet<string>();
						object trace = null;
						XElement topLevelTrace = CreateTopLevelElement(analysis);

						foreach (
							Stack<Morpheme> otherMorphemes in PermuteOtherMorphemes(
								morphemes,
								wordAnalysis.RootMorphemeIndex - 1,
								wordAnalysis.RootMorphemeIndex + 1
							)
						)
						{
							results.UnionWith(
								morpher.GenerateWords(
									rootEntry,
									otherMorphemes,
									realizationalFS,
									out trace
								)
							);
							// output of trace makes more sense if we invert the order
							topLevelTrace.AddFirst(trace);
						}
						Trace = topLevelTrace;
#if ChangePeriodToSpace
						IEnumerable<XElement> list = topLevelTrace.XPathSelectElements(
							"//ParseCompleteTrace[@success='true']/Result"
						);
						for (int i = 0; i < results.Count && i < list.Count(); i++)
						{
							string fromHC = results.ElementAt(i);
							string fromTrace = list.ElementAt(i).Value;
							if (fromTrace.Contains("."))
							{
								fromHC = fromTrace.Replace(".", " ");
							}
							((List<string>)newSyntheses).Add(fromHC);
						}
#else
						newSyntheses = results;
#endif
					}
					else
					{
						newSyntheses = morpher.GenerateWords(wordAnalysis);
					}
					string result = formatter.Format(newSyntheses, analysis);
					sb.Append(result);
					if (CheckForDuplicates(morpher, sb, morphemes))
					{
						AddToTracing(traceManager, sb, analysis);
					}
				}
				int lastIndexEnd = indexEnd;
				indexCaret = AppendBetweenWordsContent(glosses, sb, lastIndexEnd);
				indexBeg = indexCaret + lastIndexEnd;
				indexEnd = glosses.Substring(lastIndexEnd + 1).IndexOf("$") + lastIndexEnd + 1;
			}
			return sb.ToString();
		}

		private void CollectMissingGlosses(
			StringBuilder sb,
			AnalysesCreator analysesCreator,
			List<Morpheme> morphemes
		)
		{
			sb.Append(HCSynthByGlossStrings.ksOneOrMoreGlossesNotFound);
			var glossesFound = new List<string>();
			foreach (Morpheme morpheme in morphemes)
			{
				if (morpheme != null)
				{
					glossesFound.Add(morpheme.Gloss);
				}
			}
			foreach (string form in analysesCreator.Forms)
			{
				if (!glossesFound.Contains(form))
				{
					sb.Append(" '");
					sb.Append(form);
					sb.Append("';");
				}
			}
		}

		private void AddToTracing(ISynTraceManager traceManager, StringBuilder sb, string analysis)
		{
			if (traceManager.IsTracing)
			{
				XElement topLevelTrace = CreateTopLevelElement(analysis);
				XElement error = new XElement("error");
				error.Add(sb.ToString());
				topLevelTrace.Add(error);
				Trace = topLevelTrace;
			}
		}

		private bool CheckForDuplicates(Morpher morpher, StringBuilder sb, List<Morpheme> morphemes)
		{
			bool duplicateFound = false;
			foreach (Morpheme morph in morphemes)
			{
				if (morph == null)
					continue;
				var duplicateGloss = morpher.Morphemes.FirstOrDefault(
					m => m.Gloss == morph.Gloss && m != morph
				);
				if (duplicateGloss != null)
				{
					if (!duplicateFound)
					{
						sb.Append(HCSynthByGlossStrings.ksDuplicateGlossFoundFor);
						duplicateFound = true;
					}
					else
					{
						sb.Append(" '");
					}
					sb.Append(morph.Gloss);
					sb.Append("';");
				}
			}
			if (duplicateFound)
			{
				sb.Append(HCSynthByGlossStrings.ksSynthesisMayNotWork);
			}
			return duplicateFound;
		}

		private static XElement CreateTopLevelElement(string analysis)
		{
			XElement traceRemember = new XElement("Synthesis");
			int len = analysis.Length - 1;
			int indexBegin = analysis[0] == '^' ? 1 : 0;
			int indexEnd = analysis[len] == '$' ? len - 1 : len;
			var inputAnalysis = new XAttribute(
				"analysis",
				analysis.Substring(indexBegin, (indexEnd - indexBegin) + 1)
			);
			traceRemember.Add(inputAnalysis);
			return traceRemember;
		}

		// folloowing borrowed form Morpher; we could make the Morpher one be public
		private IEnumerable<Stack<Morpheme>> PermuteOtherMorphemes(
			List<Morpheme> morphemes,
			int leftIndex,
			int rightIndex
		)
		{
			if (leftIndex == -1 && rightIndex == morphemes.Count)
			{
				yield return new Stack<Morpheme>();
			}
			else
			{
				if (rightIndex < morphemes.Count)
				{
					foreach (
						Stack<Morpheme> p in PermuteOtherMorphemes(
							morphemes,
							leftIndex,
							rightIndex + 1
						)
					)
					{
						p.Push(morphemes[rightIndex]);
						yield return p;
					}
				}

				if (leftIndex > -1)
				{
					foreach (
						Stack<Morpheme> p in PermuteOtherMorphemes(
							morphemes,
							leftIndex - 1,
							rightIndex
						)
					)
					{
						p.Push(morphemes[leftIndex]);
						yield return p;
					}
				}
			}
		}

		private static int AppendBetweenWordsContent(
			string glosses,
			StringBuilder sb,
			int lastIndexEnd
		)
		{
			int indexWhiteSpace = lastIndexEnd + 1;
			int indexCaret = glosses.Substring(lastIndexEnd).IndexOf("^");
			if (indexCaret != -1)
			{
				string afterDollar = glosses.Substring(
					indexWhiteSpace,
					(lastIndexEnd + indexCaret) - indexWhiteSpace
				);
				if (indexWhiteSpace < glosses.Length && !afterDollar.Contains("\n"))
					sb.Append(",");
				else
					sb.Append("\n");
			}
			else
				sb.Append("\n");
			return indexCaret;
		}
	}
}
