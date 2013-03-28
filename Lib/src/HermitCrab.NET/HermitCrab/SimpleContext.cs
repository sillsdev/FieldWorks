using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a simple context in a phonetic pattern. Simple contexts are used to represent
	/// natural classes and segments in a pattern.
	/// </summary>
	public abstract class SimpleContext : PhoneticPatternNode
	{
		/// <summary>
		/// The simple context type
		/// </summary>
		public enum SimpleContextType { SEGMENT, NATURAL_CLASS };

		protected FeatureBundle m_featureValues;
		protected FeatureBundle m_antiFeatureValues;
		protected FeatureSystem m_featSys;

		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleContext"/> class.
		/// </summary>
		public SimpleContext(FeatureBundle featureValues, FeatureBundle antiFeatureValues)
		{
			m_featureValues = featureValues;
			m_antiFeatureValues = antiFeatureValues;
			m_featSys = m_featureValues.FeatureSystem;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="ctxt">The simple context.</param>
		public SimpleContext(SimpleContext ctxt)
			: base(ctxt)
		{
			m_featSys = ctxt.m_featSys;
			m_featureValues = new FeatureBundle(ctxt.m_featureValues);
			m_antiFeatureValues = new FeatureBundle(ctxt.m_antiFeatureValues);
		}

		/// <summary>
		/// Gets the node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.SIMP_CTXT;
			}
		}

		/// <summary>
		/// Gets the features.
		/// </summary>
		/// <value>The features.</value>
		public override IEnumerable<Feature> Features
		{
			get
			{
				return m_featureValues.Features;
			}
		}

		/// <summary>
		/// Gets the type of the context.
		/// </summary>
		/// <value>The type of the context.</value>
		public abstract SimpleContextType ContextType
		{
			get;
		}

		/// <summary>
		/// Gets the feature values.
		/// </summary>
		/// <value>The feature values.</value>
		public FeatureBundle FeatureValues
		{
			get
			{
				return m_featureValues;
			}
		}

		/// <summary>
		/// Gets the anti feature values.
		/// </summary>
		/// <value>The anti feature values.</value>
		public FeatureBundle AntiFeatureValues
		{
			get
			{
				return m_antiFeatureValues;
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
			return m_featureValues.ContainsFeature(feature);
		}

		/// <summary>
		/// Checks if the specified phonetic shape node matches this simple context.
		/// </summary>
		/// <param name="node">The phonetic shape node.</param>
		/// <param name="dir">The direction.</param>
		/// <param name="mode">The mode.</param>
		/// <param name="instantiatedVars">The instantiated variables.</param>
		/// <returns>All matches.</returns>
		internal override IList<Match> Match(PhoneticShapeNode node, Direction dir, ModeType mode,
			VariableValues instantiatedVars)
		{
			switch (node.Type)
			{
				case PhoneticShapeNode.NodeType.BOUNDARY:
					// only check boundaries in synthesis mode when the pattern is a target,
					// otherwise skip
					if (mode == ModeType.SYNTHESIS)
					{
						if (Owner.IsTarget)
						{
							return new List<Match>();
						}
						else
						{
							IList<Match> bdryMatches = Match(GetNextShapeNode(node, dir), dir, mode, instantiatedVars);
							foreach (Match match in bdryMatches)
								match.Add(node, m_partition);
							return bdryMatches;
						}
					}
					else
					{
						return Match(GetNextShapeNode(node, dir), dir, mode, instantiatedVars);
					}

				case PhoneticShapeNode.NodeType.MARGIN:
					Margin margin = node as Margin;
					if (dir == margin.MarginType)
						// we are at the end of the shape, so no match
						return new List<Match>();
					else
						return Match(GetNextShapeNode(node, dir), dir, mode, instantiatedVars);

				case PhoneticShapeNode.NodeType.SEGMENT:
					Segment seg = node as Segment;
					if (mode == ModeType.SYNTHESIS && Owner.IsTarget)
					{
						// check segment to see if it has already been altered by another
						// subrule, only matters for simultaneously applying rules
						if (!seg.IsClean)
							return new List<Match>();
					}

					VariableValues tempVars = instantiatedVars.Clone();
					if (m_featSys.HasFeatures)
					{
						if (!IsFeatureMatch(seg, tempVars, mode))
							return new List<Match>();
					}
					else
					{
						if (!IsSegmentMatch(seg))
							return new List<Match>();
					}

					// move to the next node
					IList<Match> segMatches = MatchNext(node, dir, mode, tempVars);
					foreach (Match match in segMatches)
						match.Add(node, m_partition);

					return segMatches;
			}

			return new List<Match>();
		}

		protected virtual bool IsFeatureMatch(Segment seg, VariableValues instantiatedVars, ModeType mode)
		{
			// check unifiability
			if (!seg.FeatureValues.IsUnifiable(m_featureValues))
				return false;

			return true;
		}

		protected abstract bool IsSegmentMatch(Segment seg);

		public void Apply(Segment seg, VariableValues instantiatedVars)
		{
			if (m_featSys.HasFeatures)
				ApplyFeatures(seg, instantiatedVars);
			else
				ApplySegments(seg);
		}

		protected abstract void ApplyFeatures(Segment seg, VariableValues instantiatedVars);
		protected abstract void ApplySegments(Segment seg);

		public void Unapply(Segment seg, VariableValues instantiatedVars)
		{
			if (m_featSys.HasFeatures)
				UnapplyFeatures(seg, instantiatedVars);
			else
				UnapplySegments(seg);
		}

		protected abstract void UnapplyFeatures(Segment seg, VariableValues instantiatedVars);
		protected abstract void UnapplySegments(Segment seg);

		public Segment ApplyInsertion(VariableValues instantiatedVars)
		{
			if (m_featSys.HasFeatures)
				return ApplyInsertionFeatures(instantiatedVars);
			else
				return ApplyInsertionSegments();
		}

		protected abstract Segment ApplyInsertionFeatures(VariableValues instantiatedVars);
		protected abstract Segment ApplyInsertionSegments();

		public Segment UnapplyDeletion(VariableValues instantiatedVars)
		{
			if (m_featSys.HasFeatures)
				return UnapplyDeletionFeatures(instantiatedVars);
			else
				return UnapplyDeletionSegments();
		}

		protected abstract Segment UnapplyDeletionFeatures(VariableValues instantiatedVars);
		protected abstract Segment UnapplyDeletionSegments();

		public SimpleContext Combine(SimpleContext ctxt)
		{
			if (m_featSys.HasFeatures)
				return CombineFeatures(ctxt);
			else
				return Clone() as SimpleContext;
		}

		protected virtual SimpleContext CombineFeatures(SimpleContext ctxt)
		{
			SimpleContext result = Clone() as SimpleContext;

			// collect all of the possible values of the features
			List<FeatureValue> featVals = new List<FeatureValue>();
			foreach (Feature feature in Owner.Features)
				featVals.AddRange(feature.PossibleValues);
			// create a mask feature bundle from all possible values
			FeatureBundle mask = new FeatureBundle(featVals, m_featSys);

			FeatureBundle temp = ctxt.FeatureValues.Clone();
			// remove features referenced in the this pattern's context
			temp.RemoveValues(mask);
			// add remaining features from the specified pattern's context to this pattern's context
			result.FeatureValues.AddValues(temp);
			// remove anti features referenced in the specified pattern's context from this pattern's context
			result.AntiFeatureValues.RemoveValues(ctxt.AntiFeatureValues);
			return result;
		}

		public virtual bool IsUnapplicationVacuous(Segment seg, VariableValues instantiatedVars)
		{
			// check if the context's anti features have already been set
			if (!seg.FeatureValues.IsUnifiable(m_antiFeatureValues))
				return true;

			return false;
		}
	}
}
