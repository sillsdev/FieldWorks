// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Runtime.Serialization;
using System.ServiceModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The WCF contract for the out-of-process HermitCrab worker (Src\LexText\HCWorker), defined
	/// once here in ParserCore and referenced by BOTH the worker (which implements it) and
	/// HCWorkerClient (which consumes it) - no hand-synced duplicate. The explicit Namespace/Name/
	/// DataMember attributes pin the wire shape. See RUSTIFY-fieldworks-worker-design.md.
	/// </summary>
	[ServiceContract(Namespace = "http://sil.org/machine/hermitcrab/worker", Name = "IHCWorkerService")]
	public interface IHCWorkerService
	{
		[OperationContract]
		void UpdateGrammar(HCGrammarDto grammar);

		[OperationContract]
		WordAnalysisDto[] ParseWord(string word, bool guessRoots);

		[OperationContract]
		IDictionary<string, WordAnalysisDto[]> ParseWordsBatch(string[] words, bool guessRoots);
	}

	[DataContract(Namespace = "http://sil.org/machine/hermitcrab/worker")]
	public class HCGrammarDto
	{
		[DataMember]
		public string CompiledGrammarXml { get; set; }

		[DataMember]
		public int DeletionReapplications { get; set; }

		[DataMember]
		public int MaxStemCount { get; set; }

		[DataMember]
		public bool MergeEquivalentAnalyses { get; set; }
	}

	[DataContract(Namespace = "http://sil.org/machine/hermitcrab/worker")]
	public class WordAnalysisDto
	{
		[DataMember]
		public MorphDto[] Morphs { get; set; }
	}

	/// <summary>
	/// One morph's raw, LCM-lookup-ready fields, in the order HermitCrab.Worker encountered them
	/// walking the parsed Word - i.e. exactly what HCParser.GetMorphs used to read directly off
	/// the live Word/Allomorph/Morpheme object graph before parsing moved out-of-process. FormId/
	/// MsaId/InflTypeId are the ids GetMorphs already resolves via IMoFormRepository/
	/// IMoMorphSynAnalysisRepository/ILexEntryInflTypeRepository; the circumfix/infix placement
	/// logic that used to run inline (checking a resolved IMoForm's MorphTypeRA) still runs here,
	/// now over this flat list instead of over Annotation&lt;ShapeNode&gt;/Allomorph/Morpheme.
	/// </summary>
	[DataContract(Namespace = "http://sil.org/machine/hermitcrab/worker")]
	public class MorphDto
	{
		[DataMember]
		public int FormId { get; set; }

		[DataMember]
		public int FormId2 { get; set; }

		[DataMember]
		public bool IsAffixProcessAllomorph { get; set; }

		[DataMember]
		public string FormStr { get; set; }

		[DataMember]
		public bool Guessed { get; set; }

		[DataMember]
		public int MsaId { get; set; }

		[DataMember]
		public int InflTypeId { get; set; }

		[DataMember]
		public int MorphemeIndex { get; set; }
	}
}
