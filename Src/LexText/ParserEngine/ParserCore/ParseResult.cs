using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class ParseResult
	{
		public ParseResult(IWfiWordform wordform, uint crc, ParserPriority priority, IList<ParseAnalysis> analyses,
			string errorMessage)
		{
			Wordform = wordform;
			Crc = crc;
			Priority = priority;
			Analyses = analyses;
			ErrorMessage = errorMessage;
		}

		public IWfiWordform Wordform
		{
			get;
			private set;
		}

		public uint Crc
		{
			get;
			private set;
		}

		public IList<ParseAnalysis> Analyses
		{
			get;
			private set;
		}

		public string ErrorMessage
		{
			get;
			private set;
		}

		public ParserPriority Priority
		{
			get;
			private set;
		}

		public bool IsValid
		{
			get
			{
				if (!Wordform.IsValidObject)
					return false;
				return Analyses.All(analysis => analysis.IsValid);
			}
		}
	}

	public class ParseAnalysis
	{
		public ParseAnalysis(IList<ParseMorph> morphs)
		{
			Morphs = morphs;
		}

		public IList<ParseMorph> Morphs
		{
			get;
			private set;
		}

		public bool IsValid
		{
			get
			{
				return Morphs.All(morph => morph.IsValid);
			}
		}
	}

	public class ParseMorph
	{
		public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa)
		{
			Form = form;
			Msa = msa;
		}

		public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType)
		{
			Form = form;
			Msa = msa;
			InflType = inflType;
		}

		public IMoForm Form
		{
			get;
			private set;
		}

		public IMoMorphSynAnalysis Msa
		{
			get;
			private set;
		}

		public ILexEntryInflType InflType
		{
			get;
			private set;
		}

		public bool IsValid
		{
			get
			{
				return Form.IsValidObject && Msa.IsValidObject;
			}
		}
	}
}
