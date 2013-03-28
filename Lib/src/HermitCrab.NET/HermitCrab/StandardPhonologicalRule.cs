using System;
using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a standard rewrite phonological rule as defined in classical generative phonology
	/// theory. It consists of a LHS, a RHS, a left environment, and a right environment. It also supports
	/// disjunctive subrules.
	/// </summary>
	public class StandardPhonologicalRule : PhonologicalRule
	{
		/// <summary>
		/// This class represents a phonological subrule. A subrule consists of the RHS,
		/// the left environment, and the right environment.
		/// </summary>
		public class Subrule
		{
			/// <summary>
			/// Change type
			/// </summary>
			public enum ChangeType { FEATURE, EPENTHESIS, WIDEN, NARROW, UNKNOWN };

			Environment m_env;
			PhoneticPattern m_rhs;
			PhoneticPattern m_analysisTarget;
			StandardPhonologicalRule m_rule;

			HCObjectSet<PartOfSpeech> m_requiredPOSs = null;
			MPRFeatureSet m_excludedMPRFeatures = null;
			MPRFeatureSet m_requiredMPRFeatures = null;

			/// <summary>
			/// Initializes a new instance of the <see cref="Subrule"/> class.
			/// </summary>
			/// <param name="rhs">The RHS.</param>
			/// <param name="env">The environment.</param>
			/// <param name="rule">The phonological rule.</param>
			/// <exception cref="System.ArgumentException">Thrown when the size of the RHS is greater than the
			/// size of the specified rule's LHS and the LHS's size is greater than 1. A standard phonological
			/// rule does not currently support this type of widening.</exception>
			public Subrule(PhoneticPattern rhs, Environment env, StandardPhonologicalRule rule)
			{
				m_rhs = rhs;
				m_env = env;
				m_rule = rule;

				switch (Type)
				{
					case ChangeType.NARROW:
					case ChangeType.EPENTHESIS:
						// analysis target is a copy of the RHS, because there is no LHS
						m_analysisTarget = m_rhs.Clone();
						break;

					case ChangeType.WIDEN:
						// before generating the analysis we must extend the length of the LHS
						// to match the length of the RHS
						PhoneticPattern lhs = m_rule.m_lhs.Clone();
						while (lhs.Count != m_rhs.Count)
							lhs.Add(lhs.First.Clone());
						m_analysisTarget = m_rhs.Combine(lhs);
						break;

					case ChangeType.FEATURE:
						m_analysisTarget = m_rhs.Combine(m_rule.m_lhs);
						break;

					case ChangeType.UNKNOWN:
						throw new ArgumentException(HCStrings.kstidInvalidSubruleType, "rhs");
				}
			}

			/// <summary>
			/// Gets the rule.
			/// </summary>
			/// <value>The rule.</value>
			public StandardPhonologicalRule Rule
			{
				get
				{
					return m_rule;
				}
			}

			/// <summary>
			/// Gets or sets the required parts of speech.
			/// </summary>
			/// <value>The required parts of speech.</value>
			public IEnumerable<PartOfSpeech> RequiredPOSs
			{
				get
				{
					return m_requiredPOSs;
				}

				set
				{
					m_requiredPOSs = new HCObjectSet<PartOfSpeech>(value);
				}
			}

			/// <summary>
			/// Gets or sets the excluded MPR features.
			/// </summary>
			/// <value>The excluded MPR features.</value>
			public MPRFeatureSet ExcludedMPRFeatures
			{
				get
				{
					return m_excludedMPRFeatures;
				}

				set
				{
					m_excludedMPRFeatures = value;
				}
			}

			/// <summary>
			/// Gets or sets the required MPR features.
			/// </summary>
			/// <value>The required MPR features.</value>
			public MPRFeatureSet RequiredMPRFeatures
			{
				get
				{
					return m_requiredMPRFeatures;
				}

				set
				{
					m_requiredMPRFeatures = value;
				}
			}

			/// <summary>
			/// Gets the change type.
			/// </summary>
			/// <value>The change type.</value>
			ChangeType Type
			{
				get
				{
					if (m_rule.m_lhs.Count == m_rhs.Count)
						return ChangeType.FEATURE;
					else if (m_rule.m_lhs.Count == 0)
						return ChangeType.EPENTHESIS;
					else if (m_rule.m_lhs.Count == 1 && m_rhs.Count > 1)
						return ChangeType.WIDEN;
					else if (m_rule.m_lhs.Count > m_rhs.Count)
						return ChangeType.NARROW;
					else
						return ChangeType.UNKNOWN;
				}
			}

			/// <summary>
			/// Gets a value indicating whether this subrule is self-opaquing.
			/// Self-opaquing basically means that an application of the subrule can
			/// alter the environment for succeeding applications of the subrule. This is only important
			/// for simultaneously applying subrules. During analysis, it might be necessary
			/// to unapply a subrule multiple times if it is self-opaquing, because a segment might have
			/// been altered during the first pass so that it now matches the subrule's environment. If
			/// we do not unapply the rule multiple times, we would miss segments that should be been
			/// unapplied. Note: although iteratively applying rules can be self-opaquing, we do not
			/// return <c>true</c>, unless it is a deletion rule, because we don't care.
			/// </summary>
			/// <value>
			/// 	<c>true</c> if this subrule is self-opaquing, otherwise <c>false</c>.
			/// </value>
			bool IsSelfOpaquing
			{
				get
				{
					if (Type == ChangeType.NARROW)
					{
						// deletion subrules are always self-opaquing
						return true;
					}
					else if (m_rule.m_multApplication == MultAppOrder.SIMULTANEOUS)
					{
						if (Type == ChangeType.FEATURE)
						{
							foreach (PhoneticPatternNode node in m_rhs)
							{
								switch (node.Type)
								{
									case PhoneticPatternNode.NodeType.SIMP_CTXT:
										// check if there is any overlap of features between
										// the context and the environments
										SimpleContext ctxt = node as SimpleContext;
										if (!IsNonSelfOpaquing(ctxt, m_env.LeftEnvironment))
											return true;
										if (!IsNonSelfOpaquing(ctxt, m_env.RightEnvironment))
											return true;
										break;

									case PhoneticPatternNode.NodeType.BDRY_CTXT:
										break;
								}
							}

							return false;
						}
						else
						{
							return true;
						}
					}
					else
					{
						// all other types of iteratively applying subrules are never self-opaquing
						// ok, so they might be self-opaquing, but we don't care
						return false;
					}
				}
			}

			/// <summary>
			/// Checks for overlap of features between the specified simple context and the specified
			/// environment.
			/// </summary>
			/// <param name="ctxt">The simple context.</param>
			/// <param name="envSeq">The environment.</param>
			/// <returns>
			/// 	<c>true</c> if there is no overlap, otherwise <c>false</c>.
			/// </returns>
			bool IsNonSelfOpaquing(SimpleContext ctxt, PhoneticPattern env)
			{
				foreach (PhoneticPatternNode node in env)
				{
					switch (node.Type)
					{
						case PhoneticPatternNode.NodeType.SIMP_CTXT:
							SimpleContext envCtxt = node as SimpleContext;
							if (!envCtxt.FeatureValues.IsDisjoint(ctxt.AntiFeatureValues))
								return false;
							break;

						case PhoneticPatternNode.NodeType.BDRY_CTXT:
							break;

						case PhoneticPatternNode.NodeType.PATTERN:
							NestedPhoneticPattern optSeq = node as NestedPhoneticPattern;
							if (!IsNonSelfOpaquing(ctxt, optSeq.Pattern))
								return false;
							break;
					}
				}

				return true;
			}

			/// <summary>
			/// Unapplies this subrule to specified input phonetic shape.
			/// </summary>
			/// <param name="input">The input phonetic shape.</param>
			public void Unapply(PhoneticShape input)
			{
				if (Type == ChangeType.NARROW)
				{
					int i = 0;
					// because deletion rules are self-opaquing it is unclear how many segments
					// could have been deleted during synthesis, so we unapply deletion rules
					// multiple times. Unfortunately, this could create a situation where the
					// deletion rule is unapplied infinitely, so we put an upper limit on the
					// number of times a deletion rule can unapply.
					while (i <= m_rule.Morpher.DelReapplications && UnapplyNarrow(input))
						i++;
				}
				else
				{
					Direction dir = Direction.LEFT;
					switch (m_rule.m_multApplication)
					{
						case MultAppOrder.LR_ITERATIVE:
						case MultAppOrder.SIMULTANEOUS:
							// simultaneous subrules could be unapplied left-to-right or
							// right-to-left, we arbitrarily choose left-to-right
							dir = Direction.LEFT;
							break;

						case MultAppOrder.RL_ITERATIVE:
							dir = Direction.RIGHT;
							break;
					}

					// only simultaneous subrules can be self-opaquing
					if (IsSelfOpaquing)
						// unapply the subrule until it no longer makes a change
						while (UnapplyIterative(input, dir)) { }
					else
						UnapplyIterative(input, dir);
				}
			}

			bool UnapplyIterative(PhoneticShape input, Direction dir)
			{
				bool unapplied = false;
				PhoneticShapeNode node = input.GetFirst(dir);
				Match match;
				// iterate thru all matches
				while (FindNextMatchRHS(node, dir, out match))
				{
					// unapply the subrule
					IList<PhoneticShapeNode> nodes = match.EntireMatch;
					UnapplyRHS(dir, nodes, match.VariableValues);
					unapplied = true;
					node = nodes[nodes.Count - 1].GetNext(dir);
				}

				return unapplied;
			}

			bool UnapplyNarrow(PhoneticShape input)
			{
				List<Match> matches = new List<Match>();
				PhoneticShapeNode node = input.First;
				Match match;
				// deletion subrules are always treated like simultaneous subrules during unapplication
				while (FindNextMatchRHS(node, Direction.RIGHT, out match))
				{
					matches.Add(match);
					node = match.EntireMatch[0].Next;
				}

				foreach (Match m in matches)
				{
					PhoneticShapeNode cur = m.EntireMatch[m.EntireMatch.Count - 1];
					foreach (PhoneticPatternNode lhsNode in m_rule.m_lhs)
					{
						if (lhsNode.Type != PhoneticPatternNode.NodeType.SIMP_CTXT)
							continue;

						SimpleContext ctxt = lhsNode as SimpleContext;
						Segment newSeg = ctxt.UnapplyDeletion(m.VariableValues);
						// mark the undeleted segment as optional
						newSeg.IsOptional = true;
						cur.Insert(newSeg, Direction.RIGHT);
						cur = newSeg;
					}

					if (m_analysisTarget.Count > 0)
					{
						foreach (PhoneticShapeNode matchNode in m.EntireMatch)
							matchNode.IsOptional = true;
					}
				}

				return matches.Count > 0;
			}

			/// <summary>
			/// Applies the RHS to the matched segments.
			/// </summary>
			/// <param name="dir">The direction.</param>
			/// <param name="match">The matched segments.</param>
			/// <param name="instantiatedVars">The instantiated variables.</param>
			public void ApplyRHS(Direction dir, IList<PhoneticShapeNode> match, VariableValues instantiatedVars)
			{
				switch (Type)
				{
					case ChangeType.FEATURE:
						int i = 0;
						for (PhoneticPatternNode pseqNode = m_rhs.GetFirst(dir); pseqNode != null;
							pseqNode = pseqNode.GetNext(dir))
						{
							switch (pseqNode.Type)
							{
								case PhoneticPatternNode.NodeType.SIMP_CTXT:
									SimpleContext ctxt = pseqNode as SimpleContext;
									// match[i] should be a segment, should I check that here?
									while (match[i].Type == PhoneticShapeNode.NodeType.BOUNDARY)
										i++;
									Segment seg = match[i] as Segment;
									ctxt.Apply(seg, instantiatedVars);
									// marked the segment as altered
									seg.IsClean = false;
									break;

								case PhoneticPatternNode.NodeType.BDRY_CTXT:
									// boundaries should match, should I check that here?
									break;
							}
							i++;
						}
						break;

					case ChangeType.NARROW:
						ApplyInsertion(dir, match, instantiatedVars);
						// remove matching segments
						foreach (PhoneticShapeNode node in match)
							node.IsDeleted = true;
						break;

					case ChangeType.EPENTHESIS:
						// insert new segments or boundaries
						ApplyInsertion(dir, match, instantiatedVars);
						break;
				}
			}

			void ApplyInsertion(Direction dir, IList<PhoneticShapeNode> match, VariableValues instantiatedVars)
			{
				PhoneticShapeNode cur = match[match.Count - 1];
				for (PhoneticPatternNode pseqNode = m_rhs.GetFirst(dir); pseqNode != null;
					pseqNode = pseqNode.GetNext(dir))
				{
					PhoneticShapeNode newNode = null;
					switch (pseqNode.Type)
					{
						case PhoneticPatternNode.NodeType.SIMP_CTXT:
							SimpleContext ctxt = pseqNode as SimpleContext;
							newNode = ctxt.ApplyInsertion(instantiatedVars);
							break;

						case PhoneticPatternNode.NodeType.BDRY_CTXT:
							newNode = new Boundary(pseqNode as BoundaryContext);
							break;
					}

					if (newNode != null)
					{
						try
						{
							cur.Insert(newNode, dir);
						}
						catch (InvalidOperationException ioe)
						{
							MorphException me = new MorphException(MorphException.MorphErrorType.TOO_MANY_SEGS, m_rule.Morpher,
								string.Format(HCStrings.kstidTooManySegs, m_rule.ID), ioe);
							me.Data["rule"] = m_rule.ID;
							throw me;
						}
						cur = newNode;
					}
				}
			}

			void UnapplyRHS(Direction dir, IList<PhoneticShapeNode> match, VariableValues instantiatedVars)
			{
				switch (Type)
				{
					case ChangeType.FEATURE:
						int i = 0;
						// if there are no phonetic features, unapply using the LHS, since we are simply replacing
						PhoneticPattern unappPattern = m_rule.Morpher.PhoneticFeatureSystem.HasFeatures ? m_analysisTarget : m_rule.m_lhs;
						for (PhoneticPatternNode pseqNode = unappPattern.GetFirst(dir); pseqNode != null;
							pseqNode = pseqNode.GetNext(dir))
						{
							switch (pseqNode.Type)
							{
								case PhoneticPatternNode.NodeType.SIMP_CTXT:
									SimpleContext ctxt = pseqNode as SimpleContext;
									// match[i] should be a segment, should I check that here?
									Segment seg = match[i] as Segment;
									ctxt.Unapply(seg, instantiatedVars);
									break;

								case PhoneticPatternNode.NodeType.BDRY_CTXT:
									// skip boundaries
									continue;
							}
							i++;
						}
						break;

					case ChangeType.EPENTHESIS:
						// do not remove epenthesized segments, since it is possible that they will not
						// be epenthesized during synthesis, we just mark them as optional
						foreach (PhoneticShapeNode node in match)
							node.IsOptional = true;
						break;

				}
			}

			bool FindNextMatchRHS(PhoneticShapeNode node, Direction dir, out Match match)
			{
				for (; node != node.Owner.GetLast(dir); node = node.GetNext(dir))
				{
					if (node.Type == PhoneticShapeNode.NodeType.BOUNDARY)
						continue;

					if (m_analysisTarget.Count == 0)
					{
						// if the analysis target is empty (deletion rule),
						// just check environment
						VariableValues instantiatedVars = new VariableValues(m_rule.m_alphaVars);
						if (MatchEnvEmpty(node, dir, ModeType.ANALYSIS, instantiatedVars))
						{
							match = new Match(m_analysisTarget, instantiatedVars);
							match.Add(node);
							return true;
						}
					}
					else
					{
						// analysis target is non-empty, check everything
						if (MatchAnalysisTarget(node, dir, out match))
							return true;
					}
				}

				match = null;
				return false;
			}

			bool MatchAnalysisTarget(PhoneticShapeNode node, Direction dir, out Match match)
			{
				// check analysis target
				IList<Match> matches;
				VariableValues instantiatedVars = new VariableValues(m_rule.m_alphaVars);
				if (!m_analysisTarget.IsMatch(node, dir, ModeType.ANALYSIS, instantiatedVars, out matches))
				{
					match = null;
					return false;
				}

				match = matches[0];

				// check vacuous unapplication
				// even if the subrule matches, we do not want to successfully unapply if no real changes are
				// going to be made to the phonetic shape
				if (!CheckVacuousUnapplication(match, dir))
				{
					match = null;
					return false;
				}

				// finally, check environment
				if (!MatchEnvNonempty(match.EntireMatch, dir, ModeType.ANALYSIS, match.VariableValues))
				{
					match = null;
					return false;
				}

				return true;
			}

			public bool MatchEnvNonempty(IList<PhoneticShapeNode> match, Direction dir, ModeType mode,
				VariableValues instantiatedVars)
			{
				PhoneticShapeNode leftNode = null;
				PhoneticShapeNode rightNode = null;
				switch (dir)
				{
					case Direction.LEFT:
						rightNode = match[0].GetNext(Direction.RIGHT);
						leftNode = match[match.Count - 1].GetNext(Direction.LEFT);
						break;

					case Direction.RIGHT:
						rightNode = match[match.Count - 1].GetNext(Direction.RIGHT);
						leftNode = match[0].GetNext(Direction.LEFT);
						break;
				}

				if (!m_env.IsMatch(leftNode, rightNode, mode, instantiatedVars))
					return false;

				return true;
			}

			public bool MatchEnvEmpty(PhoneticShapeNode node, Direction dir, ModeType mode,
				VariableValues instantiatedVars)
			{
				PhoneticShapeNode leftNode = null;
				PhoneticShapeNode rightNode = null;
				switch (dir)
				{
					case Direction.LEFT:
						rightNode = node;
						leftNode = node.GetNext(Direction.LEFT);
						break;

					case Direction.RIGHT:
						rightNode = node.GetNext(Direction.RIGHT);
						leftNode = node;
						break;
				}

				// in case this is an epenthesis rule, we want to ensure that the segment to the right
				// of where we're going to do the epenthesis is not a boundary marker, unless the
				// environment calls for one.
				if (mode == ModeType.SYNTHESIS && m_env.RightEnvironment != null && m_env.RightEnvironment.Count > 0)
				{
					if (rightNode.Type == PhoneticShapeNode.NodeType.BOUNDARY
							&& m_env.RightEnvironment.First.Type != PhoneticPatternNode.NodeType.BDRY_CTXT)
						return false;
				}

				// there is a small difference between legacy HC and HC.NET in matching environments when the
				// analysis target is empty and one of the environments is empty. In this case, legacy HC does
				// not try to skip the initial optional segments when matching the right environment. I think
				// this will cause HC.NET to overproduce a little more during analysis, which isn't that big of a
				// deal
				if (!m_env.IsMatch(leftNode, rightNode, mode, instantiatedVars))
					return false;

				// remove ambiguous variables
				instantiatedVars.RemoveAmbiguousVariables();

				return true;
			}

			/// <summary>
			/// Checks if the subrule will be unapplied vacuously. Vacuous unapplication means that
			/// the subrule will actually make changes to the phonetic shape. This is important to know
			/// for self-opaquing, simultaneously applying subrules, since we unapply these subrules
			/// until they unapply nonvacuously.
			/// </summary>
			/// <param name="match">The match.</param>
			/// <param name="dir">The direction.</param>
			/// <returns></returns>
			bool CheckVacuousUnapplication(Match match, Direction dir)
			{
				PhoneticPatternNode rhsNode = m_rhs.GetFirst(dir);
				IList<PhoneticShapeNode> nodes = match.EntireMatch;
				int i = 0;
				while (i < nodes.Count)
				{
					if (Type == ChangeType.EPENTHESIS)
					{
						// for epenthesis subrules, simply check if the epenthesized segment is
						// already marked as optional
						if (!nodes[i++].IsOptional)
							return true;
					}
					else
					{
						switch (rhsNode.Type)
						{
							case PhoneticPatternNode.NodeType.SIMP_CTXT:
								SimpleContext ctxt = rhsNode as SimpleContext;
								Segment seg = nodes[i] as Segment;
								if (ctxt.IsUnapplicationVacuous(seg, match.VariableValues))
									return true;
								i++;
								break;

							case PhoneticPatternNode.NodeType.BDRY_CTXT:
								break;
						}

						rhsNode = rhsNode.GetNext(dir);
					}
				}

				return false;
			}

			/// <summary>
			/// Determines whether this subrule is applicable to the specified word analysis.
			/// </summary>
			/// <param name="input">The word analysis.</param>
			/// <param name="trace"> </param>
			/// <returns>
			/// 	<c>true</c> if this subrule is applicable, otherwise <c>false</c>.
			/// </returns>
			public bool IsApplicable(WordSynthesis input, Trace trace)
			{
				// check part of speech and MPR features
				bool fRequiredPOSMet = m_requiredPOSs == null || m_requiredPOSs.Count == 0 || m_requiredPOSs.Contains(input.POS);
				bool fRequiredMPRFeaturesMet = m_requiredMPRFeatures == null || m_requiredMPRFeatures.Count == 0 || m_requiredMPRFeatures.IsMatch(input.MPRFeatures);
				bool fExcludedMPRFeaturesMet = m_excludedMPRFeatures == null || m_excludedMPRFeatures.Count == 0 || !m_excludedMPRFeatures.IsMatch(input.MPRFeatures);
				if (trace != null)
				{
					if (!fRequiredPOSMet)
					{
						var badPosTrace = new PhonologicalRuleSynthesisRequiredPOSTrace(input.POS, m_requiredPOSs);
						trace.AddChild(badPosTrace);
					}
					if (!fRequiredMPRFeaturesMet)
					{
						var badRequiredMPRFeaturesTrace =
							new PhonologicalRuleSynthesisMPRFeaturesTrace(
								PhonologicalRuleSynthesisMPRFeaturesTrace.PhonologicalRuleSynthesisMPRFeaturesTraceType.REQUIRED,
								input.MPRFeatures, m_requiredMPRFeatures);
						trace.AddChild(badRequiredMPRFeaturesTrace);
					}
					if (!fExcludedMPRFeaturesMet)
					{
						var badExcludedMPRFeaturesTrace =
							new PhonologicalRuleSynthesisMPRFeaturesTrace(
								PhonologicalRuleSynthesisMPRFeaturesTrace.PhonologicalRuleSynthesisMPRFeaturesTraceType.EXCLUDED,
								input.MPRFeatures, m_excludedMPRFeatures);
						trace.AddChild(badExcludedMPRFeaturesTrace);
					}
				}
				return (fRequiredPOSMet && fRequiredMPRFeaturesMet && fExcludedMPRFeaturesMet);
			}
		}

		List<Subrule> m_subrules;

		MultAppOrder m_multApplication = MultAppOrder.LR_ITERATIVE;
		AlphaVariables m_alphaVars = null;
		PhoneticPattern m_lhs = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="PhonologicalRule"/> class.
		/// </summary>
		/// <param name="featId">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public StandardPhonologicalRule(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_subrules = new List<Subrule>();
		}

		/// <summary>
		/// Gets or sets the LHS.
		/// </summary>
		/// <value>The LHS.</value>
		public PhoneticPattern LHS
		{
			get
			{
				return m_lhs;
			}

			set
			{
				m_lhs = value;
			}
		}

		/// <summary>
		/// Gets or sets the alpha variables.
		/// </summary>
		/// <value>The alpha variables.</value>
		public AlphaVariables AlphaVariables
		{
			get
			{
				return m_alphaVars;
			}

			set
			{
				m_alphaVars = value;
			}
		}

		/// <summary>
		/// Gets or sets the multiple application order.
		/// </summary>
		/// <value>The multiple application order.</value>
		public override MultAppOrder MultApplication
		{
			get
			{
				return m_multApplication;
			}

			set
			{
				m_multApplication = value;
			}
		}

		/// <summary>
		/// Adds a subrule.
		/// </summary>
		/// <param name="sr">The subrule.</param>
		/// <exception cref="System.ArgumentException">Thrown when the specified subrule is not associated with
		/// this rule.</exception>
		public void AddSubrule(Subrule sr)
		{
			if (sr.Rule != this)
				throw new ArgumentException(HCStrings.kstidPhonSubruleError, "sr");

			m_subrules.Add(sr);
		}

		/// <summary>
		/// Unapplies the rule to the specified word analysis.
		/// </summary>
		/// <param name="input">The input word analysis.</param>
		public override void Unapply(WordAnalysis input)
		{
			PhonologicalRuleAnalysisTrace trace = null;
			if (TraceAnalysis)
			{
				// create phonological rule analysis trace record
				trace = new PhonologicalRuleAnalysisTrace(this, input.Clone());
				input.CurrentTrace.AddChild(trace);
			}

			foreach (Subrule sr in m_subrules)
				sr.Unapply(input.Shape);

			if (trace != null)
				// add output to trace record
				trace.Output = input.Clone();
		}

		/// <summary>
		/// Applies the rule to the specified word synthesis.
		/// </summary>
		/// <param name="input">The word synthesis.</param>
		public override void Apply(WordSynthesis input)
		{
			PhonologicalRuleSynthesisTrace trace = null;
			if (TraceSynthesis)
			{
				// create phonological rule synthesis trace record
				trace = new PhonologicalRuleSynthesisTrace(this, input.Clone());
				input.CurrentTrace.AddChild(trace);
			}

			// only try to apply applicable subrules
			List<Subrule> subrules = new List<Subrule>();
			foreach (Subrule sr in m_subrules)
			{
				if (sr.IsApplicable(input, trace))
					subrules.Add(sr);
			}

			if (subrules.Count > 0)
			{
				// set all segments to clean
				PhoneticShape pshape = input.Shape;
				foreach (PhoneticShapeNode node in pshape)
				{
					if (node.Type == PhoneticShapeNode.NodeType.SEGMENT)
						(node as Segment).IsClean = true;
				}

				switch (m_multApplication)
				{
					case MultAppOrder.SIMULTANEOUS:
						ApplySimultaneous(input.Shape, subrules);
						break;

					case MultAppOrder.LR_ITERATIVE:
						ApplyIterative(input.Shape, Direction.RIGHT, subrules);
						break;

					case MultAppOrder.RL_ITERATIVE:
						ApplyIterative(input.Shape, Direction.LEFT, subrules);
						break;
				}
			}

			// add output to phonological rule trace record
			if (trace != null)
				trace.Output = input.Clone();
		}

		void ApplySimultaneous(PhoneticShape input, List<Subrule> subrules)
		{
			foreach (Subrule sr in subrules)
			{
				// first find all segments which match the LHS
				List<Match> matches = new List<Match>();
				PhoneticShapeNode node = input.First;
				Match match;
				while (FindNextMatchLHS(node, Direction.RIGHT, out match))
				{
					// check each candidate match against the subrule's environment
					IList<PhoneticShapeNode> nodes = match.EntireMatch;
					VariableValues instantiatedVars = match.VariableValues;
					if (m_lhs.Count == 0
						? sr.MatchEnvEmpty(nodes[0], Direction.RIGHT, ModeType.SYNTHESIS, instantiatedVars)
						: sr.MatchEnvNonempty(nodes, Direction.RIGHT, ModeType.SYNTHESIS, instantiatedVars))
					{
						matches.Add(match);
						node = nodes[nodes.Count - 1].Next;
					}
					else
					{
						node = nodes[0].Next;
					}
				}

				// then apply changes
				foreach (Match m in matches)
				{
					sr.ApplyRHS(Direction.RIGHT, m.EntireMatch, m.VariableValues);
				}
			}
		}

		void ApplyIterative(PhoneticShape input, Direction dir, List<Subrule> subrules)
		{
			Match match;
			PhoneticShapeNode node = input.GetFirst(dir);
			// iterate thru each LHS match
			while (FindNextMatchLHS(node, dir, out match))
			{
				IList<PhoneticShapeNode> nodes = match.EntireMatch;
				VariableValues instantiatedVars = match.VariableValues;
				bool matched = false;
				// check each subrule's environment
				foreach (Subrule sr in subrules)
				{
					if (m_lhs.Count == 0
						? sr.MatchEnvEmpty(nodes[0], dir, ModeType.SYNTHESIS, instantiatedVars)
						: sr.MatchEnvNonempty(nodes, dir, ModeType.SYNTHESIS, instantiatedVars))
					{
						sr.ApplyRHS(dir, nodes, instantiatedVars);
						matched = true;
						break;
					}
				}

				if (matched)
					node = nodes[nodes.Count - 1].GetNext(dir);
				else
					node = nodes[0].GetNext(dir);
			}
		}

		bool FindNextMatchLHS(PhoneticShapeNode node, Direction dir, out Match match)
		{
			for (; node != node.Owner.GetLast(dir); node = node.GetNext(dir))
			{
				VariableValues instantiatedVars = new VariableValues(m_alphaVars);
				if (m_lhs.Count == 0)
				{
					// epenthesis rules always match the LHS
					match = new Match(m_lhs, instantiatedVars);
					match.Add(node);
					return true;
				}
				else
				{
					IList<Match> matches;
					if (m_lhs.IsMatch(node, dir, ModeType.SYNTHESIS, instantiatedVars, out matches))
					{
						match = matches[0];
						return true;
					}
				}
			}

			match = null;
			return false;
		}

		public void Reset()
		{
			m_multApplication = MultAppOrder.LR_ITERATIVE;
			m_alphaVars = null;
			m_lhs = null;
			m_subrules.Clear();
		}
	}
}
