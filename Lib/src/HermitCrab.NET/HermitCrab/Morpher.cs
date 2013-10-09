using System;
using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This enumeration represents the morpher mode type.
	/// </summary>
	public enum ModeType
	{
		/// <summary>
		/// Analysis mode (unapplication of rules)
		/// </summary>
		ANALYSIS,
		/// <summary>
		/// Synthesis mode (application of rules)
		/// </summary>
		SYNTHESIS
	}

	/// <summary>
	/// This exception is thrown during the morphing process. It is used to indicate
	/// that an error occurred while morphing a word.
	/// </summary>
	public class MorphException : Exception
	{
		/// <summary>
		/// The specific error type of morph exception. This is useful for displaying
		/// user-friendly error messages.
		/// </summary>
		public enum MorphErrorType
		{
			/// <summary>
			/// A character definition table could not translate a phonetic shape.
			/// </summary>
			INVALID_SHAPE,
			/// <summary>
			/// A feature is uninstantiated when a rule requires that it agree between the target and environment.
			/// </summary>
			UNINSTANTIATED_FEATURE,
			/// <summary>
			/// A phonetic shape contains too many segments.
			/// </summary>
			TOO_MANY_SEGS
		}

		readonly MorphErrorType m_errorType;
		readonly Morpher m_morpher;

		public MorphException(MorphErrorType errorType, Morpher morpher)
		{
			m_errorType = errorType;
			m_morpher = morpher;
		}

		public MorphException(MorphErrorType errorType, Morpher morpher, string message)
			: base(message)
		{
			m_errorType = errorType;
			m_morpher = morpher;
		}

		public MorphException(MorphErrorType errorType, Morpher morpher, string message, Exception inner)
			: base(message, inner)
		{
			m_errorType = errorType;
			m_morpher = morpher;
		}

		public MorphErrorType ErrorType
		{
			get
			{
				return m_errorType;
			}
		}

		public Morpher Morpher
		{
			get
			{
				return m_morpher;
			}
		}
	}

	/// <summary>
	/// This class acts as the main interface to the morphing capability of HC.NET. It encapsulates
	/// the feature systems, rules, character definition tables, etc. for a particular language.
	/// </summary>
	public class Morpher : HCObject
	{
		readonly FeatureSystem m_phoneticFeatSys;
		readonly FeatureSystem m_headFeatSys;
		readonly FeatureSystem m_footFeatSys;
		readonly HCObjectSet<Stratum> m_strata;
		readonly HCObjectSet<CharacterDefinitionTable> m_charDefTables;
		readonly HCObjectSet<NaturalClass> m_natClasses;
		readonly HCObjectSet<PhonologicalRule> m_prules;
		readonly HCObjectSet<MorphologicalRule> m_mrules;
		readonly HCObjectSet<AffixTemplate> m_templates;
		readonly Lexicon m_lexicon;
		readonly HCObjectSet<MPRFeatureGroup> m_mprFeatGroups;
		readonly HCObjectSet<MPRFeature> m_mprFeatures;
		readonly HCObjectSet<PartOfSpeech> m_pos;
		readonly HCObjectSet<Allomorph> m_allomorphs;

		/// <summary>
		/// Initializes a new instance of the <see cref="Morpher"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="language">The language.</param>
		public Morpher(string id, string language)
			: base(id, language, null)
		{
			m_strata = new HCObjectSet<Stratum>();
			m_phoneticFeatSys = new FeatureSystem();
			m_headFeatSys = new FeatureSystem();
			m_footFeatSys = new FeatureSystem();
			m_charDefTables = new HCObjectSet<CharacterDefinitionTable>();
			m_natClasses = new HCObjectSet<NaturalClass>();
			m_prules = new HCObjectSet<PhonologicalRule>();
			m_mrules = new HCObjectSet<MorphologicalRule>();
			m_lexicon = new Lexicon();
			m_templates = new HCObjectSet<AffixTemplate>();
			m_mprFeatGroups = new HCObjectSet<MPRFeatureGroup>();
			m_mprFeatures = new HCObjectSet<MPRFeature>();
			m_pos = new HCObjectSet<PartOfSpeech>();
			m_allomorphs = new HCObjectSet<Allomorph>();
		}

		/// <summary>
		/// Gets the deepest stratum that is not the surface stratum.
		/// </summary>
		/// <value>The deepest stratum.</value>
		public Stratum DeepestStratum
		{
			get
			{
				if (m_strata.Count < 2)
					return null;
				return m_strata[0];
			}
		}

		/// <summary>
		/// Gets the shallowest stratum that is not the surface stratum.
		/// </summary>
		/// <value>The shallowest stratum.</value>
		public Stratum ShallowestStratum
		{
			get
			{
				if (m_strata.Count < 2)
					return null;
				return m_strata[m_strata.Count - 2];
			}
		}

		/// <summary>
		/// Gets the surface stratum.
		/// </summary>
		/// <value>The surface stratum.</value>
		public Stratum SurfaceStratum
		{
			get
			{
				Stratum stratum;
				if (m_strata.TryGetValue(Stratum.SURFACE_STRATUM_ID, out stratum))
					return stratum;
				return null;
			}
		}

		/// <summary>
		/// Gets the phonetic feature system.
		/// </summary>
		/// <value>The phonetic feature system.</value>
		public FeatureSystem PhoneticFeatureSystem
		{
			get
			{
				return m_phoneticFeatSys;
			}
		}

		/// <summary>
		/// Gets the head feature system.
		/// </summary>
		/// <value>The head feature system.</value>
		public FeatureSystem HeadFeatureSystem
		{
			get
			{
				return m_headFeatSys;
			}
		}

		/// <summary>
		/// Gets the foot feature system.
		/// </summary>
		/// <value>The foot feature system.</value>
		public FeatureSystem FootFeatureSystem
		{
			get
			{
				return m_footFeatSys;
			}
		}

		/// <summary>
		/// Gets all strata, including the surface stratum.
		/// </summary>
		/// <value>The strata.</value>
		public IEnumerable<Stratum> Strata
		{
			get
			{
				return m_strata;
			}
		}

		/// <summary>
		/// Gets the lexicon
		/// </summary>
		/// <value>The lexicon.</value>
		public Lexicon Lexicon
		{
			get
			{
				return m_lexicon;
			}
		}

		/// <summary>
		/// Gets or sets the maximum number of times a deletion phonological rule can be reapplied.
		/// Default: 0.
		/// </summary>
		/// <value>Maximum number of delete reapplications.</value>
		public int DelReapplications { get; set; }

		/// <summary>
		/// Gets the MPR feature groups.
		/// </summary>
		/// <value>The MPR feature groups.</value>
		public IEnumerable<MPRFeatureGroup> MPRFeatureGroups
		{
			get
			{
				return m_mprFeatGroups;
			}
		}

		/// <summary>
		/// Gets the stratum associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The stratum.</returns>
		public Stratum GetStratum(string id)
		{
			Stratum stratum;
			if (m_strata.TryGetValue(id, out stratum))
				return stratum;
			return null;
		}

		/// <summary>
		/// Gets the character definition table associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The character definition table.</returns>
		public CharacterDefinitionTable GetCharacterDefinitionTable(string id)
		{
			CharacterDefinitionTable charDefTable;
			if (m_charDefTables.TryGetValue(id, out charDefTable))
				return charDefTable;
			return null;
		}

		/// <summary>
		/// Gets the natural class associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The natural class.</returns>
		public NaturalClass GetNaturalClass(string id)
		{
			NaturalClass nc;
			if (m_natClasses.TryGetValue(id, out nc))
				return nc;
			return null;
		}

		/// <summary>
		/// Gets the phonological rule associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The phonological rule.</returns>
		public PhonologicalRule GetPhonologicalRule(string id)
		{
			PhonologicalRule prule;
			if (m_prules.TryGetValue(id, out prule))
				return prule;
			return null;
		}

		/// <summary>
		/// Gets the morphological rule associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The morphological rule.</returns>
		public MorphologicalRule GetMorphologicalRule(string id)
		{
			MorphologicalRule mrule;
			if (m_mrules.TryGetValue(id, out mrule))
				return mrule;
			return null;
		}

		/// <summary>
		/// Gets the affix template associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The affix template.</returns>
		public AffixTemplate GetAffixTemplate(string id)
		{
			AffixTemplate template;
			if (m_templates.TryGetValue(id, out template))
				return template;
			return null;
		}

		/// <summary>
		/// Gets the MPR feature group associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The MPR feature group.</returns>
		public MPRFeatureGroup GetMPRFeatureGroup(string id)
		{
			MPRFeatureGroup group;
			if (m_mprFeatGroups.TryGetValue(id, out group))
				return group;
			return null;
		}

		/// <summary>
		/// Gets the MPR feature associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The MPR feature.</returns>
		public MPRFeature GetMPRFeature(string id)
		{
			MPRFeature mprFeat;
			if (m_mprFeatures.TryGetValue(id, out mprFeat))
				return mprFeat;
			return null;
		}

		/// <summary>
		/// Gets the part of speech associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The part of speech.</returns>
		public PartOfSpeech GetPOS(string id)
		{
			PartOfSpeech pos;
			if (m_pos.TryGetValue(id, out pos))
				return pos;
			return null;
		}

		/// <summary>
		/// Gets the morpheme associated with the specified ID. Morphological rules
		/// and lexical entries are morphemes.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The morpheme.</returns>
		public Morpheme GetMorpheme(string id)
		{
			Morpheme morpheme = GetMorphologicalRule(id);
			if (morpheme == null)
				morpheme = Lexicon.GetEntry(id);
			return morpheme;
		}

		/// <summary>
		/// Gets the allomorph associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		/// <returns>The allomorph.</returns>
		public Allomorph GetAllomorph(string id)
		{
			Allomorph allomorph;
			if (m_allomorphs.TryGetValue(id, out allomorph))
				return allomorph;
			return null;
		}

		/// <summary>
		/// Adds the stratum.
		/// </summary>
		/// <param name="stratum">The stratum.</param>
		public void AddStratum(Stratum stratum)
		{
			m_strata.Add(stratum);
		}

		/// <summary>
		/// Adds the character definition table.
		/// </summary>
		/// <param name="charDefTable">The character definition table.</param>
		public void AddCharacterDefinitionTable(CharacterDefinitionTable charDefTable)
		{
			m_charDefTables.Add(charDefTable);
		}

		/// <summary>
		/// Adds the natural class.
		/// </summary>
		/// <param name="nc">The natural class.</param>
		public void AddNaturalClass(NaturalClass nc)
		{
			m_natClasses.Add(nc);
		}

		/// <summary>
		/// Adds the phonological rule.
		/// </summary>
		/// <param name="prule">The phonological rule.</param>
		public void AddPhonologicalRule(PhonologicalRule prule)
		{
			m_prules.Add(prule);
		}

		/// <summary>
		/// Adds the morphological rule.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		public void AddMorphologicalRule(MorphologicalRule mrule)
		{
			m_mrules.Add(mrule);
		}

		/// <summary>
		/// Adds the affix template.
		/// </summary>
		/// <param name="template">The affix template.</param>
		public void AddAffixTemplate(AffixTemplate template)
		{
			m_templates.Add(template);
		}

		/// <summary>
		/// Adds the MPR feature group.
		/// </summary>
		/// <param name="group">The group.</param>
		public void AddMPRFeatureGroup(MPRFeatureGroup group)
		{
			m_mprFeatGroups.Add(group);
		}

		/// <summary>
		/// Adds the MPR feature.
		/// </summary>
		/// <param name="mprFeature">The MPR feature.</param>
		public void AddMPRFeature(MPRFeature mprFeature)
		{
			m_mprFeatures.Add(mprFeature);
		}

		/// <summary>
		/// Adds the part of speech.
		/// </summary>
		/// <param name="pos">The part of speech.</param>
		public void AddPOS(PartOfSpeech pos)
		{
			m_pos.Add(pos);
		}

		/// <summary>
		/// Adds the allomorph.
		/// </summary>
		/// <param name="allomorph">The allomorph.</param>
		public void AddAllomorph(Allomorph allomorph)
		{
			m_allomorphs.Add(allomorph);
		}

		/// <summary>
		/// Removes the character definition table associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemoveCharacterDefinitionTable(string id)
		{
			m_charDefTables.Remove(id);
		}

		/// <summary>
		/// Removes the natural class associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemoveNaturalClass(string id)
		{
			m_natClasses.Remove(id);
		}

		/// <summary>
		/// Removes the phonological rule associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemovePhonologicalRule(string id)
		{
			m_prules.Remove(id);
		}

		/// <summary>
		/// Removes the morphological rule associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemoveMorphologicalRule(string id)
		{
			m_mrules.Remove(id);
		}

		/// <summary>
		/// Removes the affix template associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemoveAffixTemplate(string id)
		{
			m_templates.Remove(id);
		}

		/// <summary>
		/// Removes the MPR feature group associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemoveMPRFeatureGroup(string id)
		{
			m_mprFeatGroups.Remove(id);
		}

		/// <summary>
		/// Removes the MPR feature associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemoveMPRFeature(string id)
		{
			m_mprFeatures.Remove(id);
		}

		/// <summary>
		/// Removes the part of speech associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemovePOS(string id)
		{
			m_pos.Remove(id);
		}

		/// <summary>
		/// Removes the allomorph associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemoveAllomorph(string id)
		{
			m_allomorphs.Remove(id);
		}

		/// <summary>
		/// Clears the strata.
		/// </summary>
		public void ClearStrata()
		{
			m_strata.Clear();
		}

		/// <summary>
		/// Morphs the specified word.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <returns>All valid word synthesis records.</returns>
		public ICollection<WordSynthesis> MorphAndLookupWord(string word)
		{
			return MorphAndLookupWord(word, null);
		}

		public ICollection<WordSynthesis> MorphAndLookupWord(string word, TraceManager trace)
		{
			return MorphAndLookupToken(word, null, null, trace, null);
		}

		public ICollection<WordSynthesis> MorphAndLookupWord(string word, TraceManager trace, string[] selectTraceMorphs)
		{
			return MorphAndLookupToken(word, null, null, trace, selectTraceMorphs);
		}
		/// <summary>
		/// Morphs the list of specified words.
		/// </summary>
		/// <param name="wordList">The word list.</param>
		/// <returns>All valid word synthesis records for each word.</returns>
		public IList<ICollection<WordSynthesis>> MorphAndLookupWordList(IList<string> wordList)
		{
			return MorphAndLookupWordList(wordList, null);
		}

		public IList<ICollection<WordSynthesis>> MorphAndLookupWordList(IList<string> wordList, TraceManager trace)
		{
			var results = new List<ICollection<WordSynthesis>>();
			string prev = null;
			string word = wordList[0];
			for (int i = 0; i < wordList.Count; i++)
			{
				string next = null;
				if (i + 1 < wordList.Count)
					next = wordList[i + 1];

				results.Add(MorphAndLookupToken(word, prev, next, trace));

				prev = word;
				word = next;
			}

			return results;
		}

		ICollection<WordSynthesis> MorphAndLookupToken(string word, string prev, string next, TraceManager trace)
		{
			return MorphAndLookupToken(word, prev, next, trace, null);
		}

		/// <summary>
		/// Does the real work of morphing the specified word.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="prev">The previous word.</param>
		/// <param name="next">The next word.</param>
		/// <param name="trace">The trace.</param>
		/// <param name="selectTraceMorphs"></param>
		/// <returns>All valid word synthesis records.</returns>
		ICollection<WordSynthesis> MorphAndLookupToken(string word, string prev, string next, TraceManager trace, string[] selectTraceMorphs)
		{
			PhoneticShape input;
			try
			{
				// convert the word to its phonetic shape; it could throw a missing phonetic shape exception
				input = SurfaceStratum.CharacterDefinitionTable.ToPhoneticShape(word, ModeType.ANALYSIS);
			}
			catch (MissingPhoneticShapeException mpse)
			{
				var me = new MorphException(MorphException.MorphErrorType.INVALID_SHAPE, this,
					string.Format(HCStrings.kstidInvalidWord, word, SurfaceStratum.CharacterDefinitionTable.ID, mpse.Position+1,
									word.Substring(mpse.Position), mpse.PhonemesFoundSoFar));
				me.Data["shape"] = word;
				me.Data["charDefTable"] = SurfaceStratum.CharacterDefinitionTable.ID;
				me.Data["position"] = mpse.Position;
				me.Data["phonemesFoundSoFar"] = mpse.PhonemesFoundSoFar;
				throw me;
			}

			var inputAnalysis = new WordAnalysis(input, SurfaceStratum);

			if (trace != null)
				trace.BeginAnalyzeWord(word, inputAnalysis);

			var candidates = new Set<WordSynthesis>();
			var inAnalysis = new Set<WordAnalysis>();
			var outAnalysis = new Set<WordAnalysis>();
			inAnalysis.Add(inputAnalysis);

			// Unapply rules
			for (int i = m_strata.Count - 1; i >= 0; i--)
			{
				outAnalysis.Clear();
				foreach (WordAnalysis wa in inAnalysis)
				{
					if (trace != null)
						trace.BeginUnapplyStratum(m_strata[i], wa);

					foreach (WordAnalysis outWa in m_strata[i].Unapply(wa, trace, selectTraceMorphs, candidates))
					{
						// promote each analysis to the next stratum
						if (i != 0)
							outWa.Stratum = m_strata[i - 1];

						if (trace != null)
							trace.EndUnapplyStratum(m_strata[i], outWa);

						outAnalysis.Add(outWa);
					}
				}

				inAnalysis.Clear();
				inAnalysis.AddMany(outAnalysis);
			}

			var allValidSyntheses = new Set<WordSynthesis>();
			// Apply rules for each candidate entry
			foreach (WordSynthesis candidate in candidates)
			{
				var inSynthesis = new Set<WordSynthesis>();
				var outSynthesis = new Set<WordSynthesis>();
				for (int i = 0; i < m_strata.Count; i++)
				{
					// start applying at the stratum that this lex entry belongs to
					if (m_strata[i] == candidate.Root.Stratum)
						inSynthesis.Add(candidate);

					outSynthesis.Clear();
					foreach (WordSynthesis cur in inSynthesis)
					{
						if (trace != null)
							trace.BeginApplyStratum(m_strata[i], cur);

						foreach (WordSynthesis outWs in m_strata[i].Apply(cur, trace))
						{
							// promote the word synthesis to the next stratum
							if (i != m_strata.Count - 1)
								outWs.Stratum = m_strata[i + 1];

							if (trace != null)
								trace.EndApplyStratum(m_strata[i], outWs);

							outSynthesis.Add(outWs);
						}
					}

					inSynthesis.Clear();
					inSynthesis.AddMany(outSynthesis);
				}

				foreach (WordSynthesis ws in outSynthesis)
				{
					if (ws.IsValid)
						allValidSyntheses.Add(ws);
				}
			}

			var results = new Set<WordSynthesis>();
			// sort the resulting syntheses according to the order of precedence of each allomorph in
			// their respective morphemes
			var sortedSyntheses = new List<WordSynthesis>(allValidSyntheses);
			sortedSyntheses.Sort();

			WordSynthesis prevValidSynthesis = null;
			bool allFreeFluctuation = true;
			foreach (WordSynthesis cur in sortedSyntheses)
			{
				// enforce the disjunctive property of allomorphs by ensuring that this word synthesis
				// has the highest order of precedence for its allomorphs while also allowing for free
				// fluctuation
				if (prevValidSynthesis == null || AreAllomorphsNondisjunctive(cur, prevValidSynthesis))
				{
					AddResult(word, results, cur, trace);
					allFreeFluctuation = true;
				}
				else if (allFreeFluctuation && CheckFreeFluctuation(cur, prevValidSynthesis))
				{
					AddResult(word, results, cur, trace);
				}
				else
				{
					allFreeFluctuation = false;
				}
				prevValidSynthesis = cur;
			}
			return results;
		} // end MorphAndLookupToken

		private void AddResult(string word, Set<WordSynthesis> results, WordSynthesis cur, TraceManager trace)
		{
			if (SurfaceStratum.CharacterDefinitionTable.IsMatch(word, cur.Shape))
			{
				if (trace != null)
					trace.ReportSuccess(cur);

				// do not add to the result if it has the same root, shape, and morphemes as another result
				bool duplicate = false;
				foreach (WordSynthesis ws in results)
				{
					if (cur.Duplicates(ws))
					{
						duplicate = true;
						break;
					}
				}
				if (!duplicate)
				{
					results.Add(cur);
				}
			}
		}

		/// <summary>
		/// Determines if the allomorphs in the two syntheses are not disjunctive.
		/// </summary>
		/// <param name="synthesis1">The first synthesis.</param>
		/// <param name="synthesis2">The second synthesis.</param>
		/// <returns></returns>
		private bool AreAllomorphsNondisjunctive(WordSynthesis synthesis1, WordSynthesis synthesis2)
		{
			if (synthesis1.Morphs.Count != synthesis2.Morphs.Count)
				return true;

			IEnumerator<Morph> enum1 = synthesis1.Morphs.GetEnumerator();
			IEnumerator<Morph> enum2 = synthesis2.Morphs.GetEnumerator();
			while (enum1.MoveNext() && enum2.MoveNext())
			{
				// if they have different morphemes then these allomorphs are not disjunctive
				if (enum1.Current.Allomorph.Morpheme != enum2.Current.Allomorph.Morpheme)
					return true;
			}
			return false;
		}

		private bool CheckFreeFluctuation(WordSynthesis synthesis1, WordSynthesis synthesis2)
		{
			IEnumerator<Morph> enum1 = synthesis1.Morphs.GetEnumerator();
			IEnumerator<Morph> enum2 = synthesis2.Morphs.GetEnumerator();
			while (enum1.MoveNext() && enum2.MoveNext())
			{
				if (enum1.Current.Allomorph != enum2.Current.Allomorph && !enum1.Current.Allomorph.ConstraintsEqual(enum2.Current.Allomorph))
					return false;
			}
			return true;
		}
	}
}
