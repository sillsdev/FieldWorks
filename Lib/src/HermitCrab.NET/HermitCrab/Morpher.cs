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

		MorphErrorType m_errorType;
		Morpher m_morpher;

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
		FeatureSystem m_phoneticFeatSys;
		FeatureSystem m_headFeatSys;
		FeatureSystem m_footFeatSys;
		HCObjectSet<Stratum> m_strata;
		HCObjectSet<CharacterDefinitionTable> m_charDefTables;
		HCObjectSet<NaturalClass> m_natClasses;
		HCObjectSet<PhonologicalRule> m_prules;
		HCObjectSet<MorphologicalRule> m_mrules;
		HCObjectSet<AffixTemplate> m_templates;
		Lexicon m_lexicon;
		HCObjectSet<MPRFeatureGroup> m_mprFeatGroups;
		HCObjectSet<MPRFeature> m_mprFeatures;
		HCObjectSet<PartOfSpeech> m_pos;
		HCObjectSet<Allomorph> m_allomorphs;
		int m_delReapps = 0;

		bool m_traceStrataAnalysis = false;
		bool m_traceStrataSynthesis = false;
		bool m_traceTemplatesAnalysis = false;
		bool m_traceTemplatesSynthesis = false;
		bool m_traceLexLookup = false;
		bool m_traceBlocking = false;
		bool m_traceSuccess = false;

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
		public int DelReapplications
		{
			get
			{
				return m_delReapps;
			}

			set
			{
				m_delReapps = value;
			}
		}

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
		/// Gets a value indicating whether this morpher is tracing.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if this morpher is tracing, otherwise <c>false</c>.
		/// </value>
		public bool IsTracing
		{
			get
			{
				if (m_traceStrataAnalysis || m_traceStrataSynthesis || m_traceTemplatesAnalysis || m_traceTemplatesSynthesis
					|| m_traceLexLookup || m_traceBlocking || m_traceSuccess)
				{
					return true;
				}

				foreach (PhonologicalRule prule in m_prules)
				{
					if (prule.TraceAnalysis || prule.TraceSynthesis)
						return true;
				}

				foreach (MorphologicalRule mrule in m_mrules)
				{
					if (mrule.TraceAnalysis || mrule.TraceSynthesis)
						return true;
				}

				return false;
			}
		}

		/// <summary>
		/// Turns tracing on and off for all parts of the morpher.
		/// </summary>
		/// <value><c>true</c> to turn tracing on, <c>false</c> to turn tracing off.</value>
		public bool TraceAll
		{
			set
			{
				m_traceStrataAnalysis = value;
				m_traceStrataSynthesis = value;
				m_traceTemplatesAnalysis = value;
				m_traceTemplatesSynthesis = value;
				m_traceLexLookup = value;
				m_traceBlocking = value;
				m_traceSuccess = value;
				SetTraceRules(value, value);
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of strata during analysis is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceStrataAnalysis
		{
			get
			{
				return m_traceStrataAnalysis;
			}

			set
			{
				m_traceStrataAnalysis = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of strata during synthesis is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceStrataSynthesis
		{
			get
			{
				return m_traceStrataSynthesis;
			}

			set
			{
				m_traceStrataSynthesis = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of affix templates during analysis
		/// is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceTemplatesAnalysis
		{
			get
			{
				return m_traceTemplatesAnalysis;
			}

			set
			{
				m_traceTemplatesAnalysis = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of affix templates during synthesis
		/// is on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceTemplatesSynthesis
		{
			get
			{
				return m_traceTemplatesSynthesis;
			}

			set
			{
				m_traceTemplatesSynthesis = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of lexical lookup is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceLexLookup
		{
			get
			{
				return m_traceLexLookup;
			}

			set
			{
				m_traceLexLookup = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of blocking is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceBlocking
		{
			get
			{
				return m_traceBlocking;
			}

			set
			{
				m_traceBlocking = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether tracing of successful parses is
		/// on or off.
		/// </summary>
		/// <value><c>true</c> if tracing is on, <c>false</c> if tracing is off.</value>
		public bool TraceSuccess
		{
			get
			{
				return m_traceSuccess;
			}

			set
			{
				m_traceSuccess = value;
			}
		}

		/// <summary>
		/// Turns tracing of all rules on or off.
		/// </summary>
		/// <param name="traceAnalysis"><c>true</c> if tracing during analysis is on, <c>false</c>
		/// if tracing during analysis is off.</param>
		/// <param name="traceSynthesis"><c>true</c> if tracing during synthesis is on, <c>false</c>
		/// if tracing during synthesis is off.</param>
		public void SetTraceRules(bool traceAnalysis, bool traceSynthesis)
		{
			foreach (PhonologicalRule prule in m_prules)
			{
				prule.TraceAnalysis = traceAnalysis;
				prule.TraceSynthesis = traceSynthesis;
			}

			foreach (MorphologicalRule mrule in m_mrules)
			{
				mrule.TraceAnalysis = traceAnalysis;
				mrule.TraceSynthesis = traceSynthesis;
			}
		}

		/// <summary>
		/// Turns tracing of a rule on or off.
		/// </summary>
		/// <param name="id">The rule ID.</param>
		/// <param name="traceAnalysis"><c>true</c> if tracing during analysis is on, <c>false</c>
		/// if tracing during analysis is off.</param>
		/// <param name="traceSynthesis"><c>true</c> if tracing during synthesis is on, <c>false</c>
		/// if tracing during synthesis is off.</param>
		public void SetTraceRule(string id, bool traceAnalysis, bool traceSynthesis)
		{
			PhonologicalRule prule = GetPhonologicalRule(id);
			if (prule != null)
			{
				prule.TraceAnalysis = traceAnalysis;
				prule.TraceSynthesis = traceSynthesis;
			}
			else
			{
				MorphologicalRule mrule = GetMorphologicalRule(id);
				if (mrule != null)
				{
					mrule.TraceAnalysis = traceAnalysis;
					mrule.TraceSynthesis = traceSynthesis;
				}
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
			WordAnalysisTrace trace;
			return MorphAndLookupWord(word, out trace);
		}

		public ICollection<WordSynthesis> MorphAndLookupWord(string word, out WordAnalysisTrace trace)
		{
			return MorphAndLookupToken(word, null, null, out trace, null);
		}

		public ICollection<WordSynthesis> MorphAndLookupWord(string word, out WordAnalysisTrace trace, string[] selectTraceMorphs)
		{
			return MorphAndLookupToken(word, null, null, out trace, selectTraceMorphs);
		}
		/// <summary>
		/// Morphs the list of specified words.
		/// </summary>
		/// <param name="wordList">The word list.</param>
		/// <returns>All valid word synthesis records for each word.</returns>
		public IList<ICollection<WordSynthesis>> MorphAndLookupWordList(IList<string> wordList)
		{
			IList<WordAnalysisTrace> traces;
			return MorphAndLookupWordList(wordList, out traces);
		}

		public IList<ICollection<WordSynthesis>> MorphAndLookupWordList(IList<string> wordList,
			out IList<WordAnalysisTrace> traces)
		{
			List<ICollection<WordSynthesis>> results = new List<ICollection<WordSynthesis>>();
			traces = new List<WordAnalysisTrace>();
			string prev = null;
			string word = wordList[0];
			for (int i = 0; i < wordList.Count; i++)
			{
				string next = null;
				if (i + 1 < wordList.Count)
					next = wordList[i + 1];

				WordAnalysisTrace trace;
				results.Add(MorphAndLookupToken(word, prev, next, out trace));
				traces.Add(trace);

				prev = word;
				word = next;
			}

			return results;
		}

		ICollection<WordSynthesis> MorphAndLookupToken(string word, string prev, string next, out WordAnalysisTrace trace)
		{
			return MorphAndLookupToken(word, prev, next, out trace, null);
		}
		/// <summary>
		/// Does the real work of morphing the specified word.
		/// </summary>
		/// <param name="word">The word.</param>
		/// <param name="prev">The previous word.</param>
		/// <param name="next">The next word.</param>
		/// <param name="trace">The trace.</param>
		/// <returns>All valid word synthesis records.</returns>
		ICollection<WordSynthesis> MorphAndLookupToken(string word, string prev, string next, out WordAnalysisTrace trace, string[] selectTraceMorphs)
		{
			PhoneticShape input;
			try
			{
				// convert the word to its phonetic shape; it could throw a missing phonetic shape exception
				input = SurfaceStratum.CharacterDefinitionTable.ToPhoneticShape(word, ModeType.ANALYSIS);
			}
			catch (MissingPhoneticShapeException mpse)
			{
				MorphException me = new MorphException(MorphException.MorphErrorType.INVALID_SHAPE, this,
					string.Format(HCStrings.kstidInvalidWord, word, SurfaceStratum.CharacterDefinitionTable.ID, mpse.Position+1, word.Substring(mpse.Position)));
				me.Data["shape"] = word;
				me.Data["charDefTable"] = SurfaceStratum.CharacterDefinitionTable.ID;
				me.Data["position"] = mpse.Position;
				throw me;
			}

			// create the root of the trace tree
			trace = new WordAnalysisTrace(word, input.Clone());

			Set<WordSynthesis> candidates = new Set<WordSynthesis>();
			Set<WordAnalysis> inAnalysis = new Set<WordAnalysis>();
			Set<WordAnalysis> outAnalysis = new Set<WordAnalysis>();
			inAnalysis.Add(new WordAnalysis(input, SurfaceStratum, trace));

			// Unapply rules
			for (int i = m_strata.Count - 1; i >= 0; i--)
			{
				outAnalysis.Clear();
				foreach (WordAnalysis wa in inAnalysis)
				{
					if (m_traceStrataAnalysis)
					{
						// create the stratum analysis input trace record
						StratumAnalysisTrace stratumTrace = new StratumAnalysisTrace(m_strata[i], true, wa.Clone());
						wa.CurrentTrace.AddChild(stratumTrace);
					}
					foreach (WordAnalysis outWa in m_strata[i].Unapply(wa, candidates, selectTraceMorphs))
					{
						// promote each analysis to the next stratum
						if (i != 0)
							outWa.Stratum = m_strata[i - 1];

						if (m_traceStrataAnalysis)
							// create the stratum analysis output trace record for the output word synthesis
							outWa.CurrentTrace.AddChild(new StratumAnalysisTrace(m_strata[i], false, outWa.Clone()));

						outAnalysis.Add(outWa);
					}
				}

				inAnalysis.Clear();
				inAnalysis.AddMany(outAnalysis);
			}

			Set<WordSynthesis> allValidSyntheses = new Set<WordSynthesis>();
			// Apply rules for each candidate entry
			foreach (WordSynthesis candidate in candidates)
			{
				Set<WordSynthesis> inSynthesis = new Set<WordSynthesis>();
				Set<WordSynthesis> outSynthesis = new Set<WordSynthesis>();
				for (int i = 0; i < m_strata.Count; i++)
				{
					// start applying at the stratum that this lex entry belongs to
					if (m_strata[i] == candidate.Root.Stratum)
						inSynthesis.Add(candidate);

					outSynthesis.Clear();
					foreach (WordSynthesis cur in inSynthesis)
					{
						if (m_traceStrataSynthesis)
						{
							// create the stratum synthesis input trace record
							StratumSynthesisTrace stratumTrace = new StratumSynthesisTrace(m_strata[i], true, cur.Clone());
							cur.CurrentTrace.AddChild(stratumTrace);
						}
						foreach (WordSynthesis outWs in m_strata[i].Apply(cur))
						{
							// promote the word synthesis to the next stratum
							if (i != m_strata.Count - 1)
								outWs.Stratum = m_strata[i + 1];

							if (m_traceStrataSynthesis)
								// create the stratum synthesis output trace record for the output analysis
								outWs.CurrentTrace.AddChild(new StratumSynthesisTrace(m_strata[i], false, outWs.Clone()));

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

			Set<WordSynthesis> results = new Set<WordSynthesis>();
			// sort the resulting syntheses according to the order of precedence of each allomorph in
			// their respective morphemes
			List<WordSynthesis> sortedSyntheses = new List<WordSynthesis>(allValidSyntheses);
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
					AddResult(word, results, cur);
					allFreeFluctuation = true;
				}
				else if (allFreeFluctuation && CheckFreeFluctuation(cur, prevValidSynthesis))
				{
					AddResult(word, results, cur);
				}
				else
				{
					allFreeFluctuation = false;
				}
				prevValidSynthesis = cur;
			}
			return results;
		} // end MorphAndLookupToken

		private void AddResult(string word, Set<WordSynthesis> results, WordSynthesis cur)
		{
			if (SurfaceStratum.CharacterDefinitionTable.IsMatch(word, cur.Shape))
			{
				if (m_traceSuccess)
					// create the report a success output trace record for the output analysis
					cur.CurrentTrace.AddChild(new ReportSuccessTrace(cur));
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
