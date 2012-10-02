using System;
using System.Collections.Generic;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a natural class of phonetic segments.
	/// </summary>
	public class NaturalClass : HCObject
	{
		Set<SegmentDefinition> m_segDefs;
		FeatureBundle m_features = null;
		FeatureBundle m_antiFeatures = null;

		/// <summary>
		/// Initializes a new instance of the <see cref="NaturalClass"/> class.
		/// </summary>
		/// <param name="featId">The ID.</param>
		/// <param name="desc">The description.</param>
		/// <param name="morpher">The morpher.</param>
		public NaturalClass(string id, string desc, Morpher morpher)
			: base(id, desc, morpher)
		{
			m_segDefs = new Set<SegmentDefinition>();
		}

		/// <summary>
		/// Gets or sets the features.
		/// </summary>
		/// <value>The features.</value>
		public FeatureBundle Features
		{
			get
			{
				return m_features;
			}

			set
			{
				m_features = value;
				m_antiFeatures = new FeatureBundle(m_features, true);
			}
		}

		/// <summary>
		/// Gets or sets the anti features.
		/// </summary>
		/// <value>The anti features.</value>
		public FeatureBundle AntiFeatures
		{
			get
			{
				return m_antiFeatures;
			}
		}

		/// <summary>
		/// Gets the segment definitions.
		/// </summary>
		/// <value>The segment definitions.</value>
		public IEnumerable<SegmentDefinition> SegmentDefinitions
		{
			get
			{
				return m_segDefs;
			}
		}

		/// <summary>
		/// Gets the number of segment definitions.
		/// </summary>
		/// <value>The number of segment definitions.</value>
		public int NumSegmentDefinitions
		{
			get
			{
				return m_segDefs.Count;
			}
		}

		/// <summary>
		/// Adds the segment definition.
		/// </summary>
		/// <param name="segDef">The seg def.</param>
		public void AddSegmentDefinition(SegmentDefinition segDef)
		{
			m_segDefs.Add(segDef);
		}
	}

	/// <summary>
	/// This class represents a natural class context in a phonetic pattern.
	/// </summary>
	public class NaturalClassContext : SimpleContext
	{
		NaturalClass m_natClass;
		IDictionary<string, bool> m_variables;
		IDictionary<string, bool> m_antiVariables;
		AlphaVariables m_alphaVars;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleContext"/> class from a natural class.
		/// </summary>
		/// <param name="natClass">The natural class.</param>
		/// <param name="variables">The variables.</param>
		/// <param name="alphaVars">The alpha variables.</param>
		public NaturalClassContext(NaturalClass natClass, IDictionary<string, bool> variables, AlphaVariables alphaVars)
			: base(natClass.Features.Clone(), natClass.AntiFeatures.Clone())
		{
			m_natClass = natClass;
			m_variables = variables;
			m_alphaVars = alphaVars;
			m_antiVariables = new Dictionary<string, bool>(variables.Count);
			foreach (KeyValuePair<string, bool> kvp in variables)
				m_antiVariables[kvp.Key] = !kvp.Value;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="ctxt">The simple context.</param>
		public NaturalClassContext(NaturalClassContext ctxt)
			: base(ctxt)
		{
			m_natClass = ctxt.m_natClass;
			if (ctxt.m_variables != null)
			{
				m_variables = new Dictionary<string, bool>(ctxt.m_variables);
				m_antiVariables = new Dictionary<string, bool>(ctxt.m_antiVariables);
				m_alphaVars = ctxt.m_alphaVars;
			}
		}

		/// <summary>
		/// Gets the type of the context.
		/// </summary>
		/// <value>The type of the context.</value>
		public override SimpleContextType ContextType
		{
			get
			{
				return SimpleContextType.NATURAL_CLASS;
			}
		}

		public override IEnumerable<Feature> Features
		{
			get
			{
				HCObjectSet<Feature> features = new HCObjectSet<Feature>(base.Features);
				// get features from variables
				foreach (string variable in m_variables.Keys)
				{
					Feature feature = m_alphaVars.GetFeature(variable);
					features.Add(feature);
				}
				return features;
			}
		}

		/// <summary>
		/// Gets the natural class.
		/// </summary>
		/// <value>The natural class.</value>
		public NaturalClass NaturalClass
		{
			get
			{
				return m_natClass;
			}
		}

		/// <summary>
		/// Determines whether this node references the specified feature.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <returns>
		/// 	<c>true</c> if the specified feature is referenced, otherwise <c>false</c>.
		/// </returns>
		public override bool IsFeatureReferenced(Feature feature)
		{
			if (base.IsFeatureReferenced(feature))
				return true;
			foreach (string variable in m_variables.Keys)
			{
				Feature feat = m_alphaVars.GetFeature(variable);
				if (feature == feat)
					return true;
			}
			return false;
		}

		protected override bool IsFeatureMatch(Segment seg, VariableValues instantiatedVars, ModeType mode)
		{
			if (!base.IsFeatureMatch(seg, instantiatedVars, mode))
				return false;

			if (m_alphaVars != null)
			{
				// only one possible binding during synthesis
				if (Owner.IsTarget)
				{
					if (!m_alphaVars.GetBinding(m_variables, seg, instantiatedVars))
						return false;
				}
				else if (mode == ModeType.SYNTHESIS)
				{
					if (!m_alphaVars.GetBinding(m_variables, seg, instantiatedVars))
					{
						// when a variable is specified in a target and environment for agreement, the environment
						// must specify a feature for each variable
						foreach (KeyValuePair<string, bool> varPolarity in m_variables)
						{
							// check each variable to see which one is not specified in the environment
							if (!m_alphaVars.GetBinding(varPolarity.Key, varPolarity.Value, seg, new VariableValues(m_alphaVars)))
							{
								Feature f = m_alphaVars.GetFeature(varPolarity.Key);
								MorphException me = new MorphException(MorphException.MorphErrorType.UNINSTANTIATED_FEATURE, m_natClass.Morpher,
									string.Format(HCStrings.kstidUninstEnv, f.ID));
								me.Data["feature"] = f.ID;
								throw me;
							}
						}
						return false;
					}
				}
				else
				{
					// during analysis, get all possible bindings, since a feature could
					// be uninstantiated
					if (!m_alphaVars.GetAllBindings(m_variables, seg, instantiatedVars))
						return false;
				}
			}
			return true;
		}

		protected override bool IsSegmentMatch(Segment seg)
		{
			if (m_natClass.NumSegmentDefinitions == 0)
				return true;

			foreach (SegmentDefinition segDef in m_natClass.SegmentDefinitions)
			{
				if (seg.IsSegmentInstantiated(segDef))
					return true;
			}
			return false;
		}

		protected override void ApplyFeatures(Segment seg, VariableValues instantiatedVars)
		{
			// set the context's features on the segment
			seg.FeatureValues.Apply(m_featureValues, true);
			m_alphaVars.Apply(seg.FeatureValues, m_variables, instantiatedVars, true);
			// unset the context's anti features on the segment
			seg.FeatureValues.Apply(m_antiFeatureValues, false);
			m_alphaVars.Apply(seg.FeatureValues, m_antiVariables, instantiatedVars, false);
		}

		protected override void ApplySegments(Segment seg)
		{
			seg.ClearInstantiatedSegments();
			UnapplySegments(seg);
		}

		protected override void UnapplyFeatures(Segment seg, VariableValues instantiatedVars)
		{
			// set the context's anti features on the segment
			seg.FeatureValues.Apply(m_antiFeatureValues, true);
			m_alphaVars.Apply(seg.FeatureValues, m_antiVariables, instantiatedVars, true);
		}

		protected override void UnapplySegments(Segment seg)
		{
			foreach (SegmentDefinition segDef in m_natClass.SegmentDefinitions)
				seg.InstantiateSegment(segDef);
		}

		protected override Segment ApplyInsertionFeatures(VariableValues instantiatedVars)
		{
			// copy over the context's features
			FeatureBundle feats = new FeatureBundle(m_featureValues);
			// apply the context's variable features to the segment's features
			m_alphaVars.Apply(feats, m_variables, instantiatedVars, true);
			return new Segment(ToString(), feats);
		}

		protected override Segment ApplyInsertionSegments()
		{
			Segment seg = new Segment(ToString(), new FeatureBundle(false, m_featSys));
			foreach (SegmentDefinition segDef in m_natClass.SegmentDefinitions)
				seg.InstantiateSegment(segDef);
			return seg;
		}

		protected override Segment UnapplyDeletionFeatures(VariableValues instantiatedVars)
		{
			// in analysis mode, we set all of the feature values
			FeatureBundle feats = new FeatureBundle(true, m_featSys);
			// then unset each anti feature
			feats.Apply(m_antiFeatureValues, false);
			m_alphaVars.ApplyCurrent(feats, m_antiVariables, instantiatedVars);
			return new Segment(ToString(), feats);
		}

		protected override Segment UnapplyDeletionSegments()
		{
			return ApplyInsertionSegments();
		}

		protected override SimpleContext CombineFeatures(SimpleContext ctxt)
		{
			NaturalClassContext result = base.CombineFeatures(ctxt) as NaturalClassContext;
			if (ctxt.ContextType == SimpleContextType.NATURAL_CLASS)
			{
				NaturalClassContext ncCtxt = ctxt as NaturalClassContext;
				// add variables from the specified pattern's context
				foreach (KeyValuePair<string, bool> varPolarity in ncCtxt.m_variables)
				{
					if (!Owner.IsFeatureReferenced(m_alphaVars.GetFeature(varPolarity.Key)))
						result.m_variables[varPolarity.Key] = varPolarity.Value;
				}

				foreach (KeyValuePair<string, bool> varPolarity in ncCtxt.m_antiVariables)
				{
					if (!Owner.IsFeatureReferenced(m_alphaVars.GetFeature(varPolarity.Key)))
						result.m_antiVariables[varPolarity.Key] = varPolarity.Value;
				}
			}
			return result;
		}

		public override bool IsUnapplicationVacuous(Segment seg, VariableValues instantiatedVars)
		{
			if (base.IsUnapplicationVacuous(seg, instantiatedVars))
				return true;

			// check if the context's anti variables have already been set
			if (!m_alphaVars.GetBinding(m_antiVariables, seg, instantiatedVars.Clone()))
				return true;

			return false;
		}

		public override PhoneticPatternNode Clone()
		{
			return new NaturalClassContext(this);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as NaturalClassContext);
		}

		public bool Equals(NaturalClassContext other)
		{
			if (other == null)
				return false;

			return m_natClass == other.m_natClass;
		}

		public override int GetHashCode()
		{
			return m_natClass.GetHashCode();
		}

		public override string ToString()
		{
			return "[:" + m_natClass.Description + ":]";
		}
	}
}
