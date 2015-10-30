// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SIL.Collections;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcMorphNode : ComplexConcLeafNode
	{
		private readonly Dictionary<IFsFeatDefn, object> m_inflFeatures;

		public ComplexConcMorphNode()
		{
			m_inflFeatures = new Dictionary<IFsFeatDefn, object>();
		}

		public ITsString Form { get; set; }

		public ITsString Gloss { get; set; }

		public ITsString Entry { get; set; }

		public IPartOfSpeech Category { get; set; }

		public bool NegateCategory { get; set; }

		public IDictionary<IFsFeatDefn, object> InflFeatures
		{
			get { return m_inflFeatures; }
		}

		public override PatternNode<ComplexConcParagraphData, ShapeNode> GeneratePattern(FeatureSystem featSys)
		{
			var fs = new FeatureStruct();
			var typeFeat = featSys.GetFeature<SymbolicFeature>("type");
			fs.AddValue(typeFeat, typeFeat.PossibleSymbols["morph"]);
			AddStringValue(featSys, fs, Form, "form");
			AddStringValue(featSys, fs, Gloss, "gloss");
			AddStringValue(featSys, fs, Entry, "entry");
			if (Category != null)
			{
				var catFeat = featSys.GetFeature<SymbolicFeature>("cat");
				IEnumerable<FeatureSymbol> symbols = Category.ReallyReallyAllPossibilities.Concat(Category).Select(pos => catFeat.PossibleSymbols[pos.Hvo.ToString(CultureInfo.InvariantCulture)]);
				if (NegateCategory)
					symbols = catFeat.PossibleSymbols.Except(symbols);
				fs.AddValue(catFeat, symbols);
			}
			if (m_inflFeatures.Count > 0)
			{
				var inflFeat = featSys.GetFeature<ComplexFeature>("infl");
				fs.AddValue(inflFeat, GetFeatureStruct(featSys, m_inflFeatures));
			}

			return AddQuantifier(new Constraint<ComplexConcParagraphData, ShapeNode>(fs));
		}
	}
}
