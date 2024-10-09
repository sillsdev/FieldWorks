// Copyright (c) 2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using SIL.LCModel;

namespace SIL.FieldWorks.WordWorks.Parser
{
	public class ParseResult : IEquatable<ParseResult>
	{
		private readonly ReadOnlyCollection<ParseAnalysis> m_analyses;
		private readonly string m_errorMessage;

		public ParseResult(string errorMessage)
			: this(Enumerable.Empty<ParseAnalysis>(), errorMessage)
		{
		}

		public ParseResult(IEnumerable<ParseAnalysis> analyses)
			: this(analyses, null)
		{
		}

		public ParseResult(IEnumerable<ParseAnalysis> analyses, string errorMessage)
		{
			m_analyses = new ReadOnlyCollection<ParseAnalysis>(analyses.ToArray());
			m_errorMessage = errorMessage;
		}

		public ReadOnlyCollection<ParseAnalysis> Analyses
		{
			get { return m_analyses; }
		}

		public string ErrorMessage
		{
			get { return m_errorMessage; }
		}

		public long ParseTime;

		public bool IsValid
		{
			get { return Analyses.All(analysis => analysis.IsValid); }
		}

		public bool Equals(ParseResult other)
		{
			return m_analyses.SequenceEqual(other.m_analyses) && m_errorMessage == other.m_errorMessage;
		}

		public override bool Equals(object obj)
		{
			var other = obj as ParseResult;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			int code = 23;
			foreach (ParseAnalysis analysis in m_analyses)
				code = code * 31 + analysis.GetHashCode();
			code = code * 31 + (m_errorMessage == null ? 0 : m_errorMessage.GetHashCode());
			return code;
		}
	}

	public class ParseAnalysis : IEquatable<ParseAnalysis>
	{
		private readonly ReadOnlyCollection<ParseMorph> m_morphs;

		public ParseAnalysis(IEnumerable<ParseMorph> morphs)
		{
			m_morphs = new ReadOnlyCollection<ParseMorph>(morphs.ToArray());
		}

		public ReadOnlyCollection<ParseMorph> Morphs
		{
			get { return m_morphs; }
		}

		public bool IsValid
		{
			get { return Morphs.All(morph => morph.IsValid); }
		}

		public bool Equals(ParseAnalysis other)
		{
			return m_morphs.SequenceEqual(other.m_morphs);
		}

		public override bool Equals(object obj)
		{
			var other = obj as ParseAnalysis;
			return other != null && Equals(other);
		}

		public bool MatchesIWfiAnalysis(IWfiAnalysis analysis)
		{
			/*
				A "match" is one in which:
				(1) the number of morph bundles equal the number of the MoForm and
					MorphoSyntaxAnanlysis (MSA) IDs passed in to the stored procedure, and
				(2) The objects of each MSA+Form pair match those of the corresponding WfiMorphBundle.
			*/
			if (analysis.MorphBundlesOS.Count == this.Morphs.Count)
			{
				// Meets match condition (1), above.
				bool mbMatch = false; //Start pessimistically.
				int i = 0;
				foreach (IWfiMorphBundle mb in analysis.MorphBundlesOS)
				{
					var current = this.Morphs[i++];
					if (mb.MorphRA == current.Form && mb.MsaRA == current.Msa && mb.InflTypeRA == current.InflType &&
						(current.GuessedString == null || EquivalentFormString(mb.Form, current.GuessedString)))
					{
						// Possibly matches condition (2), above.
						mbMatch = true;
					}
					else
					{
						// Fails condition (2), above.
						return false;
					}
				}
				return mbMatch;
			}
			return false;
		}

		private bool EquivalentFormString(IMultiString multiString, string formString)
		{
			foreach (int ws in multiString.AvailableWritingSystemIds)
			{
				if (multiString.get_String(ws).Text == formString)
					return true;
			}
			return false;
		}

		public override int GetHashCode()
		{
			int code = 23;
			foreach (ParseMorph morph in m_morphs)
				code = code * 31 + morph.GetHashCode();
			return code;
		}
	}

	public class ParseMorph : IEquatable<ParseMorph>
	{
		private readonly IMoForm m_form;
		private readonly IMoMorphSynAnalysis m_msa;
		private readonly ILexEntryInflType m_inflType;
		private readonly string m_guessedString;

		public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa)
			: this(form, msa, null)
		{
		}

		public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType)
			: this(form, msa, inflType, null)
		{
		}

		public ParseMorph(IMoForm form, IMoMorphSynAnalysis msa, ILexEntryInflType inflType, string guessedString)
		{
			m_form = form;
			m_msa = msa;
			m_inflType = inflType;
			m_guessedString = guessedString;
		}

		public IMoForm Form
		{
			get { return m_form; }
		}

		public IMoMorphSynAnalysis Msa
		{
			get { return m_msa; }
		}

		public ILexEntryInflType InflType
		{
			get { return m_inflType; }
		}

		public string GuessedString
		{
			get { return m_guessedString; }
		}

		public bool IsValid
		{
			get { return Form.IsValidObject && Msa.IsValidObject && (m_inflType == null || m_inflType.IsValidObject); }
		}

		public bool Equals(ParseMorph other)
		{
			return m_form == other.m_form
				&& m_msa == other.m_msa
				&& m_inflType == other.m_inflType
				&& m_guessedString == other.m_guessedString;
		}

		public override bool Equals(object obj)
		{
			var other = obj as ParseMorph;
			return other != null && Equals(other);
		}

		public override int GetHashCode()
		{
			int code = 23;
			code = code * 31 + m_form.Guid.GetHashCode();
			code = code * 31 + m_msa.Guid.GetHashCode();
			code = code * 31 + (m_inflType == null ? 0 : m_inflType.Guid.GetHashCode());
			code = code * 31 + (m_guessedString == null ? 0 : m_guessedString.GetHashCode());
			return code;
		}
	}
}
