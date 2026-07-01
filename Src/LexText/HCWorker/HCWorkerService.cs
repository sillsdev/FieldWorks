// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.Threading.Tasks;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.Machine.Annotations;
using SIL.Machine.Morphology.HermitCrab;

namespace SIL.FieldWorks.WordWorks.Parser.HCWorker
{
	/// <summary>
	/// Hosts one Morpher for the lifetime of the worker process. One instance is shared across all
	/// WCF calls (InstanceContextMode.Single) and calls run concurrently (ConcurrencyMode.Multiple)
	/// - Morpher.ParseWord was already called this way in-process (ParserWorker's parallel batch,
	/// each iteration calling HCParser.ParseWord -> Morpher.ParseWord with no external locking), so
	/// moving it out-of-process introduces no new thread-safety requirement.
	///
	/// The DTO extraction below (ToWordAnalysisDto) is the id-collection half of HCParser.GetMorphs,
	/// running here where the Word/Allomorph/Morpheme graph lives; the LCM-object-resolution half
	/// stays in HCParser.GetMorphs, which consumes the returned MorphDto[]. The Form/Msa/InflType
	/// keys come from HCParser's own constants, so worker and client can never disagree on them.
	/// </summary>
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple)]
	public class HCWorkerService : IHCWorkerService
	{
		private volatile Morpher _morpher;

		public void UpdateGrammar(HCGrammarDto grammar)
		{
			if (grammar == null)
				throw new ArgumentNullException(nameof(grammar));

			// XmlLanguageLoader.Load only takes a file path, so round-trip the grammar XML through a
			// temp file rather than adding a string/stream overload to the HC library.
			string tempPath = Path.Combine(Path.GetTempPath(), $"hcworker-grammar-{Guid.NewGuid():N}.xml");
			try
			{
				File.WriteAllText(tempPath, grammar.CompiledGrammarXml);
				Language language = XmlLanguageLoader.Load(tempPath);
				_morpher = new Morpher(new TraceManager(), language)
				{
					DeletionReapplications = grammar.DeletionReapplications,
					MaxStemCount = grammar.MaxStemCount,
					MergeEquivalentAnalyses = grammar.MergeEquivalentAnalyses
				};
			}
			finally
			{
				try
				{
					File.Delete(tempPath);
				}
				catch (IOException)
				{
					// best-effort cleanup; a stray temp file is not worth failing the grammar update over
				}
			}
		}

		public WordAnalysisDto[] ParseWord(string word, bool guessRoots)
		{
			Morpher morpher = RequireMorpher();
			return morpher.ParseWord(word, out _, guessRoots).Select(ToWordAnalysisDto).ToArray();
		}

		public IDictionary<string, WordAnalysisDto[]> ParseWordsBatch(string[] words, bool guessRoots)
		{
			Morpher morpher = RequireMorpher();
			var results = new ConcurrentDictionary<string, WordAnalysisDto[]>();
			// Parses the whole batch server-side with no artificial DOP cap: the cap in
			// ParserWorker existed only to keep FieldWorks' UI thread responsive under Workstation
			// GC, which no longer applies once parsing lives in this Server-GC process.
			Parallel.ForEach(
				words,
				new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount },
				word =>
				{
					try
					{
						results[word] = morpher.ParseWord(word, out _, guessRoots).Select(ToWordAnalysisDto).ToArray();
					}
					catch (Exception)
					{
						// Guard each word so one unexpected exception (e.g. an out-of-vocabulary
						// character, which throws InvalidShapeException) cannot abort the whole
						// batch, mirroring ParserWorker.ParseAndUpdateWordformGuarded and
						// HCParser.ParseWord's own try/catch around the equivalent call.
						results[word] = new WordAnalysisDto[0];
					}
				}
			);
			// Return a plain Dictionary: DataContractSerializer's IDictionary support is defined in
			// terms of the concrete Dictionary shape, so don't rely on ConcurrentDictionary matching it.
			return new Dictionary<string, WordAnalysisDto[]>(results);
		}

		private Morpher RequireMorpher()
		{
			Morpher morpher = _morpher;
			if (morpher == null)
				throw new InvalidOperationException("UpdateGrammar must be called before parsing.");
			return morpher;
		}

		internal static WordAnalysisDto ToWordAnalysisDto(Word ws)
		{
			var morphemeIndices = new Dictionary<Morpheme, int>();
			var morphs = new List<MorphDto>();
			foreach (Annotation<ShapeNode> morph in ws.Morphs)
			{
				Allomorph allomorph = ws.GetAllomorph(morph);
				int formId = ParseNullableIntProperty(allomorph.Properties, HCParser.FormID) ?? 0;
				if (formId == 0)
					continue;

				if (!morphemeIndices.TryGetValue(allomorph.Morpheme, out int morphemeIndex))
				{
					morphemeIndex = morphemeIndices.Count;
					morphemeIndices[allomorph.Morpheme] = morphemeIndex;
				}

				string formStr = ws.Shape.GetNodes(morph.Range).ToString(ws.Stratum.CharacterDefinitionTable, false);
				morphs.Add(
					new MorphDto
					{
						FormId = formId,
						FormId2 = ParseNullableIntProperty(allomorph.Properties, HCParser.FormID2) ?? 0,
						IsAffixProcessAllomorph = allomorph is MorphologicalRules.AffixProcessAllomorph,
						FormStr = formStr,
						Guessed = allomorph.Guessed,
						MsaId = ParseIntProperty(allomorph.Morpheme.Properties, HCParser.MsaID),
						InflTypeId = ParseNullableIntProperty(allomorph.Morpheme.Properties, HCParser.InflTypeID) ?? 0,
						MorphemeIndex = morphemeIndex
					}
				);
			}
			return new WordAnalysisDto { Morphs = morphs.ToArray() };
		}

		private static int ParseIntProperty(IDictionary<string, object> properties, string key)
		{
			// Properties round-trip through XmlLanguageWriter/XmlLanguageLoader as strings even
			// though HCLoader stored them as ints (hcEntry.Properties[HCParser.MsaID] = msa.Hvo),
			// so parse rather than unbox.
			if (!properties.TryGetValue(key, out object value) || value == null)
				throw new InvalidOperationException($"Morpheme is missing required property '{key}'.");
			return int.Parse(value.ToString());
		}

		private static int? ParseNullableIntProperty(IDictionary<string, object> properties, string key)
		{
			if (!properties.TryGetValue(key, out object value) || value == null)
				return null;
			return int.Parse(value.ToString());
		}
	}
}
