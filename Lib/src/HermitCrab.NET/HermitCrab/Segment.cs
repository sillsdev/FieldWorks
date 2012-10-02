using System.Collections.Generic;
using System.Text;

namespace SIL.HermitCrab
{
	/// <summary>
	/// This class represents a segment in a phonetic shape.
	/// </summary>
	public class Segment : PhoneticShapeNode
	{
		SegmentDefinition m_segDef = null;
		string m_desc;
		Set<SegmentDefinition> m_instantiatedSegs;
		bool m_isClean = true;
		FeatureBundle m_featureValues;

		/// <summary>
		/// Initializes a new instance of the <see cref="Segment"/> class.
		/// </summary>
		/// <param name="segDef">The segment definition.</param>
		/// <param name="features">The features.</param>
		public Segment(SegmentDefinition segDef, FeatureBundle featureValues)
		{
			m_segDef = segDef;
			m_desc = segDef.StrRep;
			m_featureValues = featureValues;
			m_instantiatedSegs = new Set<SegmentDefinition>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="Segment"/> class.
		/// </summary>
		/// <param name="desc">The segment description.</param>
		/// <param name="featureValues">The feature values.</param>
		public Segment(string desc, FeatureBundle featureValues)
		{
			m_desc = desc;
			m_featureValues = featureValues;
			m_instantiatedSegs = new Set<SegmentDefinition>();
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="seg">The segment.</param>
		public Segment(Segment seg)
			: base(seg)
		{
			m_desc = seg.m_desc;
			m_segDef = seg.m_segDef;
			m_instantiatedSegs = new Set<SegmentDefinition>(seg.m_instantiatedSegs);
			m_featureValues = seg.m_featureValues.Clone();
			m_isClean = seg.m_isClean;
		}

		/// <summary>
		/// Gets the phonetic shape node type.
		/// </summary>
		/// <value>The node type.</value>
		public override NodeType Type
		{
			get
			{
				return NodeType.SEGMENT;
			}
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
		/// Gets or sets a value indicating whether this instance is clean. This is used
		/// for phonological rules that apply simultaneously. In order to enforce the disjunctive
		/// nature of the subrules, we do not allow another subrule to apply on segment that
		/// has already been altered by another subrule.
		/// </summary>
		/// <value><c>true</c> if this instance is clean, otherwise <c>false</c>.</value>
		internal bool IsClean
		{
			get
			{
				return m_isClean;
			}

			set
			{
				m_isClean = value;
			}
		}

		public SegmentDefinition SegmentDefinition
		{
			get
			{
				return m_segDef;
			}

			internal set
			{
				m_segDef = value;
			}
		}

		/// <summary>
		/// Gets the instantiated segments.
		/// </summary>
		/// <value>The instantiated segments.</value>
		public IEnumerable<SegmentDefinition> InstantiatedSegments
		{
			get
			{
				return m_instantiatedSegs;
			}
		}

		/// <summary>
		/// Gets the description.
		/// </summary>
		/// <value>The description.</value>
		public string Description
		{
			get
			{
				return m_desc;
			}
		}

		/// <summary>
		/// Instantiates the segment represented by the specified segment definition.
		/// </summary>
		/// <param name="segDef">The segment definition.</param>
		internal void InstantiateSegment(SegmentDefinition segDef)
		{
			m_instantiatedSegs.Add(segDef);
		}

		internal void ClearInstantiatedSegments()
		{
			m_instantiatedSegs.Clear();
		}

		public bool IsSegmentInstantiated(SegmentDefinition segDef)
		{
			if (m_instantiatedSegs.Count == 0)
				return true;

			return m_instantiatedSegs.Contains(segDef);
		}

		public override PhoneticShapeNode Clone()
		{
			return new Segment(this);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as Segment);
		}

		public bool Equals(Segment other)
		{
			if (other == null)
				return false;
			if (m_featureValues.FeatureSystem.HasFeatures)
				return m_featureValues.Equals(other.m_featureValues);
			else
				return m_instantiatedSegs.Equals(other.m_instantiatedSegs);
		}

		public override int GetHashCode()
		{
			if (m_featureValues.FeatureSystem.HasFeatures)
				return m_featureValues.GetHashCode();
			else
				return m_instantiatedSegs.GetHashCode();
		}

		public override string ToString()
		{
			if (m_isOptional)
				return "(" + m_desc + ")";
			else
				return m_desc;
		}
	}

	/// <summary>
	/// This class represents a segment context in a phonetic pattern.
	/// </summary>
	public class SegmentContext : SimpleContext
	{
		SegmentDefinition m_segDef;

		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentContext"/> class from a segment.
		/// </summary>
		/// <param name="segDef">The segment definition.</param>
		public SegmentContext(SegmentDefinition segDef)
			: base(segDef.SynthFeatures.Clone(), segDef.AntiFeatures.Clone())
		{
			m_segDef = segDef;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="SegmentContext"/> class from a segment.
		/// </summary>
		/// <param name="seg">The segment.</param>
		public SegmentContext(Segment seg)
			: base(seg.FeatureValues.Clone(), seg.SegmentDefinition.AntiFeatures.Clone())
		{
			m_segDef = seg.SegmentDefinition;
		}

		/// <summary>
		/// Copy constructor.
		/// </summary>
		/// <param name="ctxt">The simple context.</param>
		public SegmentContext(SegmentContext ctxt)
			: base(ctxt)
		{
			m_segDef = ctxt.m_segDef;
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
		/// Gets the type of the context.
		/// </summary>
		/// <value>The type of the context.</value>
		public override SimpleContextType ContextType
		{
			get
			{
				return SimpleContextType.SEGMENT;
			}
		}

		public SegmentDefinition SegmentDefinition
		{
			get
			{
				return m_segDef;
			}
		}

		protected override bool IsSegmentMatch(Segment seg)
		{
			return seg.IsSegmentInstantiated(m_segDef);
		}

		protected override void ApplyFeatures(Segment seg, VariableValues instantiatedVars)
		{
			seg.SegmentDefinition = m_segDef;
			seg.FeatureValues.SetAll(false);
			seg.FeatureValues.Apply(m_featureValues, true);
		}

		protected override void ApplySegments(Segment seg)
		{
			seg.ClearInstantiatedSegments();
			UnapplySegments(seg);
		}

		protected override void UnapplyFeatures(Segment seg, VariableValues instantiatedVars)
		{
			seg.FeatureValues.Apply(m_antiFeatureValues, true);
		}

		protected override void UnapplySegments(Segment seg)
		{
			seg.SegmentDefinition = m_segDef;
			seg.InstantiateSegment(m_segDef);
		}

		protected override Segment ApplyInsertionFeatures(VariableValues instantiatedVars)
		{
			return new Segment(m_segDef, m_featureValues.Clone());
		}

		protected override Segment ApplyInsertionSegments()
		{
			Segment seg = new Segment(m_segDef, new FeatureBundle(false, m_featSys));
			seg.InstantiateSegment(m_segDef);
			return seg;
		}

		protected override Segment UnapplyDeletionFeatures(VariableValues instantiatedVars)
		{
			FeatureBundle feats = new FeatureBundle(false, m_featSys);
			feats.AddAntiValues(m_antiFeatureValues);
			return new Segment(m_segDef, feats);
		}

		protected override Segment UnapplyDeletionSegments()
		{
			return ApplyInsertionSegments();
		}

		public override PhoneticPatternNode Clone()
		{
			return new SegmentContext(this);
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
				return false;
			return Equals(obj as SegmentContext);
		}

		public bool Equals(SegmentContext other)
		{
			if (other == null)
				return false;

			return m_segDef.Equals(other.m_segDef);
		}

		public override int GetHashCode()
		{
			return m_segDef.GetHashCode();
		}

		public override string ToString()
		{
			return m_segDef.StrRep;
		}
	}
}
