using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SIL.Collections;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.FieldWorks.IText
{
	public abstract class ComplexConcPatternNode
	{
		private static int s_nextHvo = -1000;

		private readonly int m_hvo;
		private ComplexConcPatternNode m_parent;
		private ComplexConcPatternSda m_sda;

		protected ComplexConcPatternNode()
		{
			m_hvo = s_nextHvo--;
			Minimum = 1;
			Maximum = 1;
		}

		public int Hvo
		{
			get { return m_hvo; }
		}

		public ComplexConcPatternNode Parent
		{
			get { return m_parent; }
			set
			{
				if (value == null)
					Sda = null;
				else if (value.m_sda != null)
					Sda = value.m_sda;
				m_parent = value;
			}
		}

		public ComplexConcPatternSda Sda
		{
			get { return m_sda; }
			set
			{
				if (value == null)
				{
					if (m_sda != null)
					{
						m_sda.Nodes.Remove(m_hvo);
						m_sda = null;
						if (!IsLeaf)
						{
							foreach (ComplexConcPatternNode child in Children)
								child.Sda = null;
						}
					}
				}
				else
				{
					m_sda = value;
					m_sda.Nodes[m_hvo] = this;
					if (!IsLeaf)
					{
						foreach (ComplexConcPatternNode child in Children)
							child.Sda = value;
					}
				}
			}
		}

		public int Minimum { get; set; }
		public int Maximum { get; set; }

		public abstract IList<ComplexConcPatternNode> Children { get; }

		public abstract bool IsLeaf { get; }

		public abstract PatternNode<ComplexConcParagraphData, ShapeNode> GeneratePattern(FeatureSystem featSys);

		protected PatternNode<ComplexConcParagraphData, ShapeNode> AddQuantifier(PatternNode<ComplexConcParagraphData, ShapeNode> node)
		{
			return Minimum != 1 || Maximum != 1 ? new Quantifier<ComplexConcParagraphData, ShapeNode>(Minimum, Maximum, node) : node;
		}

		protected void AddStringValue(FeatureSystem featSys, FeatureStruct fs, ITsString tss, string id)
		{
			if (tss != null)
			{
				var feat = featSys.GetFeature<StringFeature>(string.Format("{0}-{1}", id, tss.get_WritingSystemAt(0).ToString(CultureInfo.InvariantCulture)));
				fs.AddValue(feat, tss.Text);
			}
		}

		protected FeatureStruct GetFeatureStruct(FeatureSystem featSys, IDictionary<IFsFeatDefn, object> values)
		{
			var fs = new FeatureStruct();
			foreach (KeyValuePair<IFsFeatDefn, object> kvp in values)
			{
				if (kvp.Key is IFsComplexFeature)
				{
					var childValues = (IDictionary<IFsFeatDefn, object>) kvp.Value;
					fs.AddValue(featSys.GetFeature(kvp.Key.Hvo.ToString(CultureInfo.InvariantCulture)), GetFeatureStruct(featSys, childValues));
				}
				else if (kvp.Key is IFsClosedFeature)
				{
					var value = (ClosedFeatureValue) kvp.Value;
					var symFeat = featSys.GetFeature<SymbolicFeature>(kvp.Key.Hvo.ToString(CultureInfo.InvariantCulture));

					FeatureSymbol symbol = symFeat.PossibleSymbols[value.Symbol.Hvo.ToString(CultureInfo.InvariantCulture)];
					fs.AddValue(symFeat, value.Negate ? new SymbolicFeatureValue(symFeat.PossibleSymbols.Except(symbol.ToEnumerable())) : new SymbolicFeatureValue(symbol));
				}
			}

			return fs;
		}
	}
}
