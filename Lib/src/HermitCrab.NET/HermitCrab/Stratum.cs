using System;
using System.Collections.Generic;
using System.Linq;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a trie data structure. This is a specialized trie that maps phonetic shape keys to
	/// values. Each node in the trie represents a set of features instead of a character.
	/// </summary>
	class SegmentDefinitionTrie<T>
	{
		public class Match
		{
			List<PhoneticShapeNode> m_nodes;
			T m_value;

			public Match(T value)
			{
				m_value = value;
				m_nodes = new List<PhoneticShapeNode>();
			}

			public T Value
			{
				get
				{
					return m_value;
				}
			}

			public IList<PhoneticShapeNode> Nodes
			{
				get
				{
					return m_nodes;
				}
			}

			public void AddNode(PhoneticShapeNode node)
			{
				m_nodes.Insert(0, node);
			}

			public override bool Equals(object obj)
			{
				if (obj == null)
					return false;
				return Equals(obj as Match);
			}

			public bool Equals(Match other)
			{
				if (other == null)
					return false;

				if (!m_value.Equals(other.m_value))
					return false;

				if (m_nodes.Count != other.m_nodes.Count)
					return false;

				for (int i = 0; i < m_nodes.Count; i++)
				{
					if (!m_nodes[i].Equals(other.m_nodes[i]))
						return false;
				}
				return true;
			}

			public override int GetHashCode()
			{
				int hashCode = 0;
				foreach (PhoneticShapeNode node in m_nodes)
					hashCode ^= node.GetHashCode();
				return hashCode ^ m_value.GetHashCode();
			}
		}

		class TrieNode
		{
			List<T> m_values;
			SegmentDefinition m_segDef;
			List<TrieNode> m_children;

			public TrieNode()
				: this(null)
			{
			}

			public TrieNode(SegmentDefinition segDef)
			{
				m_segDef = segDef;
				m_values = new List<T>();
				m_children = new List<TrieNode>();
			}

			public void Add(PhoneticShapeNode node, T value, Direction dir)
			{
				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.MARGIN:
						if (node == node.Owner.GetLast(dir))
						{
							// we are at the end of the phonetic shape, so add the lexical
							// entry to this node
							m_values.Add(value);
							return;
						}
						else
						{
							// skip first margin
							Add(node.GetNext(dir), value, dir);
						}
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						// skip boundaries
						Add(node.GetNext(dir), value, dir);
						break;

					case PhoneticShapeNode.NodeType.SEGMENT:
						Segment seg = (Segment) node;
						TrieNode tnode = null;
						foreach (TrieNode child in m_children)
						{
							if (seg.FeatureValues.FeatureSystem.HasFeatures)
							{
								// we check for exact matches of feature sets when adding
								if (child.m_segDef.SynthFeatures.Equals(seg.FeatureValues))
								{
									tnode = child;
									break;
								}
							}
							else if (child.m_segDef == seg.SegmentDefinition)
							{
								tnode = child;
								break;
							}
						}

						if (tnode == null)
						{
							// new node needs to be added
							tnode = new TrieNode(seg.SegmentDefinition);
							m_children.Add(tnode);
						}

						// recursive call matching child node
						tnode.Add(node.GetNext(dir), value, dir);
						break;
				}
			}

			public IList<Match> Search(PhoneticShapeNode node, Direction dir, bool partialMatch)
			{
				IList<Match> matches = null;
				switch (node.Type)
				{
					case PhoneticShapeNode.NodeType.MARGIN:
						if (node == node.Owner.GetLast(dir))
						{
							matches = new List<Match>();
							if (!partialMatch)
							{
								// we are at the end of the phonetic shape, so return
								// all values in this node
								foreach (T value in m_values)
									matches.Add(new Match(value));
							}
						}
						else
						{
							// skip the first margin
							matches = Search(node.GetNext(dir), dir, partialMatch);
						}
						break;

					case PhoneticShapeNode.NodeType.BOUNDARY:
						// skip boundaries
						matches = Search(node.GetNext(dir), dir, partialMatch);
						foreach (Match match in matches)
							match.AddNode(node);
						break;

					case PhoneticShapeNode.NodeType.SEGMENT:
						Segment seg = (Segment) node;
						PhoneticShapeNode nextNode = node.GetNext(dir);
						List<Match> segMatches = new List<Match>();
						foreach (TrieNode child in m_children)
						{
							// check for unifiability when searching
							if (seg.FeatureValues.FeatureSystem.HasFeatures)
							{
								if (seg.FeatureValues.IsUnifiable(child.m_segDef.SynthFeatures))
									segMatches.AddRange(child.Search(nextNode, dir, partialMatch));
							}
							else if (seg.IsSegmentInstantiated(child.m_segDef))
							{
								segMatches.AddRange(child.Search(nextNode, dir, partialMatch));
							}
						}

						// if this is an optional node, we can try skipping it
						if (node.IsOptional)
							segMatches.AddRange(Search(nextNode, dir, partialMatch));

						matches = segMatches;

						foreach (Match match in matches)
							match.AddNode(node);
						break;
				}

				if (partialMatch)
				{
					foreach (T value in m_values)
						matches.Add(new Match(value));
				}

				return matches;
			}

			public override string ToString()
			{
				return m_segDef.ToString();
			}
		}

		TrieNode m_root;
		int m_numValues = 0;
		Direction m_dir;

		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentDefinitionTrie&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="dir">The direction.</param>
		public SegmentDefinitionTrie(Direction dir)
		{
			m_dir = dir;
			m_root = new TrieNode();
		}

		public Direction Direction
		{
			get
			{
				return m_dir;
			}
		}

		/// <summary>
		/// Adds the specified lexical entry.
		/// </summary>
		/// <param name="entry">The lexical entry.</param>
		public void Add(PhoneticShape shape, T value)
		{
			m_root.Add(shape.GetFirst(m_dir), value, m_dir);
			m_numValues++;
		}

		public void Clear()
		{
			m_root = new TrieNode();
			m_numValues = 0;
		}

		public int Count
		{
			get
			{
				return m_numValues;
			}
		}

		/// <summary>
		/// Searches for all values which match the specified phonetic
		/// shape.
		/// </summary>
		/// <param name="shape">The phonetic shape.</param>
		/// <returns>All matching values.</returns>
		public IEnumerable<Match> Search(PhoneticShape shape)
		{
			return new Set<Match>(m_root.Search(shape.GetFirst(m_dir), m_dir, false));
		}

		public IEnumerable<Match> SearchPartial(PhoneticShape shape)
		{
			return new Set<Match>(m_root.Search(shape.GetFirst(m_dir), m_dir, true));
		}
	}

	/// <summary>
	/// This class encapsulates the character definition table, rules, and lexicon for
	/// a particular stratum.
	/// </summary>
	public class Stratum : HCObject
	{
		/// <summary>
		/// This enumeration represents the rule ordering for phonological rules.
		/// </summary>
		public enum PRuleOrder { LINEAR, SIMULTANEOUS };

		/// <summary>
		/// This enumeration represents the rule ordering for morphological rules.
		/// </summary>
		public enum MRuleOrder { LINEAR, UNORDERED };

		/// <summary>
		/// The surface stratum ID
		/// </summary>
		public const string SURFACE_STRATUM_ID = "surface";

		CharacterDefinitionTable m_charDefTable = null;
		bool m_isCyclic = false;
		PRuleOrder m_pruleOrder = PRuleOrder.LINEAR;
		MRuleOrder m_mruleOrder = MRuleOrder.LINEAR;

		SegmentDefinitionTrie<LexEntry.RootAllomorph> m_entryTrie;
		HCObjectSet<MorphologicalRule> m_mrules;
		HCObjectSet<PhonologicalRule> m_prules;
		HCObjectSet<AffixTemplate> m_templates;

		/// <summary>
		/// Initializes a new instance of the <see cref="Stratum"/> class.
		/// </summary>
		/// <param name="featId">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public Stratum(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_mrules = new HCObjectSet<MorphologicalRule>();
			m_prules = new HCObjectSet<PhonologicalRule>();
			m_templates = new HCObjectSet<AffixTemplate>();
			m_entryTrie = new SegmentDefinitionTrie<LexEntry.RootAllomorph>(Direction.RIGHT);
		}

		/// <summary>
		/// Gets or sets the character definition table.
		/// </summary>
		/// <value>The character definition table.</value>
		public CharacterDefinitionTable CharacterDefinitionTable
		{
			get
			{
				return m_charDefTable;
			}

			set
			{
				m_charDefTable = value;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether this instance is cyclic.
		/// </summary>
		/// <value><c>true</c> if this instance is cyclic; otherwise, <c>false</c>.</value>
		public bool IsCyclic
		{
			get
			{
				return m_isCyclic;
			}

			set
			{
				m_isCyclic = value;
			}
		}

		/// <summary>
		/// Gets or sets the phonological rule order.
		/// </summary>
		/// <value>The phonological rule order.</value>
		public PRuleOrder PhonologicalRuleOrder
		{
			get
			{
				return m_pruleOrder;
			}

			set
			{
				m_pruleOrder = value;
			}
		}

		/// <summary>
		/// Gets or sets the morphological rule order.
		/// </summary>
		/// <value>The morphological rule order.</value>
		public MRuleOrder MorphologicalRuleOrder
		{
			get
			{
				return m_mruleOrder;
			}

			set
			{
				m_mruleOrder = value;
			}
		}

		/// <summary>
		/// Gets the affix templates.
		/// </summary>
		/// <value>The affix templates.</value>
		public IEnumerable<AffixTemplate> AffixTemplates
		{
			get
			{
				return m_templates;
			}
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
		/// Removes the phonological rule associated with the specified ID.
		/// </summary>
		/// <param name="id">The ID.</param>
		public void RemovePhonologicalRule(string id)
		{
			m_prules.Remove(id);
		}

		/// <summary>
		/// Adds the morphological rule.
		/// </summary>
		/// <param name="mrule">The morphological rule.</param>
		public void AddMorphologicalRule(MorphologicalRule mrule)
		{
			mrule.Stratum = this;
			m_mrules.Add(mrule);
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
		/// Adds the lexical entry.
		/// </summary>
		/// <param name="entry">The lexical entry.</param>
		public void AddEntry(LexEntry entry)
		{
			entry.Stratum = this;
			foreach (LexEntry.RootAllomorph allomorph in entry.Allomorphs)
				m_entryTrie.Add(allomorph.Shape, allomorph);
		}

		/// <summary>
		/// Searches for the lexical entry that matches the specified shape.
		/// </summary>
		/// <param name="shape">The shape.</param>
		/// <returns>The matching lexical entries.</returns>
		public IEnumerable<LexEntry.RootAllomorph> SearchEntries(PhoneticShape shape)
		{
			foreach (SegmentDefinitionTrie<LexEntry.RootAllomorph>.Match match in m_entryTrie.Search(shape))
				yield return match.Value;
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
		/// Clears the affix templates.
		/// </summary>
		public void ClearAffixTemplates()
		{
			m_templates.Clear();
		}

		/// <summary>
		/// Unapplies all of the rules to the specified input word analysis. All matching lexical
		/// entries are added to the <c>candidates</c> parameter.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="candidates">The set of candidate word synthesis records.</param>
		/// <returns>All word analyses that result from the unapplication of rules.</returns>
		public IEnumerable<WordAnalysis> Unapply(WordAnalysis input, ICollection<WordSynthesis> candidates)
		{
			return Unapply(input, candidates, null);

		}
		/// <summary>
		/// Unapplies all of the rules to the specified input word analysis. All matching lexical
		/// entries are added to the <c>candidates</c> parameter.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		/// <param name="candidates">The set of candidate word synthesis records.</param>
		/// <returns>All word analyses that result from the unapplication of rules.</returns>
		public IEnumerable<WordAnalysis> Unapply(WordAnalysis input, ICollection<WordSynthesis> candidates, string[] selectTraceMorphs)
		{
			if (m_isCyclic)
				throw new NotImplementedException(HCStrings.kstidCyclicStratumNotSupported);

			if (m_pruleOrder == PRuleOrder.SIMULTANEOUS)
				throw new NotImplementedException(HCStrings.kstidSimultOrderNotSupported);

			WordAnalysis wa = input.Clone();

			UnapplyPhonologicalRules(wa);

			LexicalLookup(wa, candidates, selectTraceMorphs);

			Set<WordAnalysis> output = new Set<WordAnalysis>();
			UnapplyMorphologicalRulesAndTemplates(wa, output, candidates, selectTraceMorphs);

			return output;
		}

		void UnapplyMorphologicalRules(WordAnalysis input, int rIndex, int srIndex, ICollection<WordSynthesis> candidates,
			Set<WordAnalysis> output)
		{
			UnapplyMorphologicalRules(input, rIndex, srIndex, candidates, output, null);
		}
			void UnapplyMorphologicalRules(WordAnalysis input, int rIndex, int srIndex, ICollection<WordSynthesis> candidates,
			Set<WordAnalysis> output, string[] selectTraceMorphs)
		{
			// first try to unapply the specified subrule
			bool unapplied = false;
			if (rIndex >= 0)
			{
				if (m_mrules[rIndex].BeginUnapplication(input))
				{
					ICollection<WordAnalysis> analyses;
					if (m_mrules[rIndex].Unapply(input, srIndex, out analyses, selectTraceMorphs))
					{
						foreach (WordAnalysis wa in analyses)
							MorphologicalRuleUnapplied(wa, rIndex, srIndex, candidates, output, selectTraceMorphs);

						unapplied = true;
					}
				}
				else
				{
					// move to next rule
					rIndex--;
					srIndex = -1;
				}
			}

			// iterate thru all subrules that occur after the specified rule in analysis order
			for (int i = rIndex; i >= 0; i--)
			{
				if (srIndex != -1 || m_mrules[i].BeginUnapplication(input))
				{
					for (int j = 0; j < m_mrules[i].SubruleCount; j++)
					{
						// skip the specified subrule, since we already tried to unapply it
						if (j != srIndex)
						{
							ICollection<WordAnalysis> analyses;
							if (m_mrules[i].Unapply(input, j, out analyses, selectTraceMorphs))
							{
								foreach (WordAnalysis wa in analyses)
									MorphologicalRuleUnapplied(wa, i, j, candidates, output, selectTraceMorphs);

								unapplied = true;
							}
						}
					}

					m_mrules[i].EndUnapplication(input, unapplied);
				}

				unapplied = false;
				srIndex = -1;
			}

			output.Add(input);
		}

		void MorphologicalRuleUnapplied(WordAnalysis ruleOutput, int rIndex, int srIndex, ICollection<WordSynthesis> candidates,
			Set<WordAnalysis> output, string[] selectTraceMorphs)
		{
			if (ruleOutput.Shape.Count > 2)
			{
				// lookup resulting phonetic shape in lexicon
				LexicalLookup(ruleOutput, candidates, selectTraceMorphs);

				// recursive call so that we can cover every permutation of rule unapplication
				switch (m_mruleOrder)
				{
					case MRuleOrder.LINEAR:
						UnapplyMorphologicalRules(ruleOutput, rIndex, srIndex, candidates, output, selectTraceMorphs);
						break;

					case MRuleOrder.UNORDERED:
						UnapplyMorphologicalRulesAndTemplates(ruleOutput, output, candidates, selectTraceMorphs);
						break;
				}
			}
		}

		void UnapplyMorphologicalRulesAndTemplates(WordAnalysis wa, Set<WordAnalysis> output, ICollection<WordSynthesis> candidates)
		{
			UnapplyMorphologicalRulesAndTemplates(wa, output, candidates, null);
		}

		void UnapplyMorphologicalRulesAndTemplates(WordAnalysis wa, Set<WordAnalysis> output, ICollection<WordSynthesis> candidates, string[] selectTraceMorphs)
		{
			Set<WordAnalysis> tempOutput = new Set<WordAnalysis>();
			tempOutput.Add(wa);
			UnapplyTemplates(wa, tempOutput, candidates, selectTraceMorphs);
			foreach (WordAnalysis analysis in tempOutput)
			{
				// start over from the very beginning
				UnapplyMorphologicalRules(analysis, m_mrules.Count - 1, 0, candidates, output, selectTraceMorphs);
			}
		}

		void UnapplyTemplates(WordAnalysis input, Set<WordAnalysis> output, ICollection<WordSynthesis> candidates, string[] selectTraceMorphs)
		{
			foreach (AffixTemplate template in m_templates)
			{
				if (template.IsUnapplicable(input))
				{
					IEnumerable<WordAnalysis> tempOutput;
					if (template.Unapply(input, out tempOutput, selectTraceMorphs))
					{
						foreach (WordAnalysis tempAnalysis in tempOutput)
						{
							// don't bother to do a lookup if this analysis already exists
							if (!output.Contains(tempAnalysis))
							{
								output.Add(tempAnalysis);
								// lookup resulting phonetic shape in lexicon
								LexicalLookup(tempAnalysis, candidates, selectTraceMorphs);
							}
						}
					}
				}
			}
		}

		void LexicalLookup(WordAnalysis input, ICollection<WordSynthesis> candidates)
		{
			LexicalLookup(input, candidates, null);
		}
		void LexicalLookup(WordAnalysis input, ICollection<WordSynthesis> candidates, string[] selectTraceMorphs)
		{
			LexLookupTrace lookupTrace = null;
			if (Morpher.TraceLexLookup)
			{
				// create lexical lookup trace record
				lookupTrace = new LexLookupTrace(this, input.Shape.Clone());
				input.CurrentTrace.AddChild(lookupTrace);
			}

			foreach (SegmentDefinitionTrie<LexEntry.RootAllomorph>.Match match in m_entryTrie.Search(input.Shape))
			{
				// don't allow a compound where both roots are the same
				if (input.NonHead == null || input.NonHead.RootAllomorph.Morpheme != match.Value.Morpheme)
				{
					LexEntry entry = (LexEntry) match.Value.Morpheme;
					if (IgnoreEntry(entry, selectTraceMorphs))
						continue;
					foreach (LexEntry.RootAllomorph allomorph in entry.Allomorphs)
					{
						WordAnalysis wa = input.Clone();

						wa.RootAllomorph = allomorph;

						if (Morpher.TraceLexLookup)
						{
							// successful lookup, so create word synthesis trace record
							WordSynthesisTrace wsTrace = new WordSynthesisTrace(wa.RootAllomorph, wa.UnappliedMorphologicalRules,
								wa.RealizationalFeatures.Clone());
							lookupTrace.AddChild(wsTrace);
							wa.CurrentTrace = wsTrace;
						}

						candidates.Add(new WordSynthesis(wa));
					}
				}
			}
		}
		
		bool IgnoreEntry(LexEntry entry, string[] selectTraceMorphs)
		{
			if (selectTraceMorphs != null)
			{
				if (!selectTraceMorphs.Contains(entry.ID))
					return true;
			}
			return false;
		}

		void UnapplyPhonologicalRules(WordAnalysis input)
		{
			// TODO: handle ordering properly
			for (int i = m_prules.Count - 1; i >= 0; i--)
				m_prules[i].Unapply(input);
		}

		/// <summary>
		/// Applies all of the rules to the specified word synthesis.
		/// </summary>
		/// <param name="input">The input word synthesis.</param>
		/// <returns>All word synthesis records that result from the application of rules.</returns>
		public IEnumerable<WordSynthesis> Apply(WordSynthesis input)
		{
			if (m_isCyclic)
				throw new NotImplementedException(HCStrings.kstidCyclicStratumNotSupported);

			if (m_pruleOrder == PRuleOrder.SIMULTANEOUS)
				throw new NotImplementedException(HCStrings.kstidSimultOrderNotSupported);

			// TODO: handle cyclicity
			Set<WordSynthesis> output = new Set<WordSynthesis>();
			ApplyMorphologicalRules(input.Clone(), 0, output);

			foreach (WordSynthesis cur in output)
				ApplyPhonologicalRules(cur);

			return output;
		}

		void ApplyMorphologicalRules(WordSynthesis input, int rIndex, Set<WordSynthesis> output)
		{
			// iterate thru all rules starting from the specified rule in synthesis order
			for (int i = rIndex; i < m_mrules.Count; i++)
			{
				if (m_mrules[i].IsApplicable(input))
				{
					ICollection<WordSynthesis> syntheses;
					if (m_mrules[i].Apply(input, out syntheses))
					{
						foreach (WordSynthesis ws in syntheses)
						{
							// recursive call so that we can cover every permutation of rule application
							switch (m_mruleOrder)
							{
								case MRuleOrder.LINEAR:
									ApplyMorphologicalRules(ws, i, output);
									break;

								case MRuleOrder.UNORDERED:
									ApplyMorphologicalRules(ws, 0, output);
									break;
							}
						}
					}
				}
			}

			switch (m_mruleOrder)
			{
				case MRuleOrder.LINEAR:
					ApplyTemplates(input, output);
					break;

				case MRuleOrder.UNORDERED:
					Set<WordSynthesis> tempOutput = new Set<WordSynthesis>();
					if (ApplyTemplates(input, tempOutput))
					{
						if (tempOutput.Remove(input))
							output.Add(input);
						foreach (WordSynthesis synthesis in tempOutput)
							ApplyMorphologicalRules(synthesis, 0, output);
					}
					output.AddMany(tempOutput);
					break;
			}
		}

		bool ApplyTemplates(WordSynthesis input, Set<WordSynthesis> output)
		{
			// if this word synthesis is not compatible with the realizational features,
			// then skip it
			if (!input.RealizationalFeatures.IsCompatible(input.HeadFeatures))
				return false;

			WordSynthesis ws = ChooseInflStem(input);
			bool applicableTemplate = false;
			foreach (AffixTemplate template in m_templates)
			{
				// HC.NET does not treat templates as applying disjunctively, as opposed to legacy HC,
				// which does
				if (template.IsApplicable(ws))
				{
					applicableTemplate = true;
					IEnumerable<WordSynthesis> tempOutput;
					if (template.Apply(ws, out tempOutput))
						output.AddMany(tempOutput);
				}
			}

			if (!applicableTemplate)
				output.Add(ws);

			return applicableTemplate;
		}

		/// <summary>
		/// If the list of Realizational Features is non-empty, choose from either the input stem or its relatives
		/// of this stratum that stem which incorporates the most realizational features (without being incompatible
		/// with any realizational feature or with the head and foot features of the input stem).
		/// </summary>
		/// <param name="ws">The input word synthesis.</param>
		/// <returns>The resulting word synthesis.</returns>
		WordSynthesis ChooseInflStem(WordSynthesis ws)
		{
			if (ws.RealizationalFeatures.NumFeatures == 0 || ws.Root.Family == null)
				return ws;

			WordSynthesis best = ws;
			// iterate thru all relatives
			foreach (LexEntry relative in ws.Root.Family.Entries)
			{
				if (relative != ws.Root && relative.Stratum == ws.Stratum
					&& ws.RealizationalFeatures.IsCompatible(relative.HeadFeatures)
					&& ws.POS == relative.POS && relative.FootFeatures.Equals(ws.FootFeatures))
				{
					FeatureValues remainder;
					if (best.HeadFeatures.GetSupersetRemainder(relative.HeadFeatures, out remainder) && remainder.NumFeatures > 0
						&& ws.RealizationalFeatures.IsCompatible(remainder))
					{
						if (Morpher.TraceBlocking)
							// create blocking trace record, should this become the current trace?
							ws.CurrentTrace.AddChild(new BlockingTrace(BlockingTrace.BlockType.TEMPLATE, relative));
						best = new WordSynthesis(relative.PrimaryAllomorph, ws.RealizationalFeatures, ws.CurrentTrace);
					}
				}
			}
			return best;
		}

		void ApplyPhonologicalRules(WordSynthesis input)
		{
			for (int i = 0; i < m_prules.Count; i++)
				m_prules[i].Apply(input);
		}
	}
}
