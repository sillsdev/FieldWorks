// Copyright (c) 2013-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using SIL.Collections;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	public class ComplexConcWordNode : ComplexConcLeafNode
	{
		private readonly Dictionary<IFsFeatDefn, object> m_inflFeatures;

		public ComplexConcWordNode()
		{
			m_inflFeatures = new Dictionary<IFsFeatDefn, object>();
		}

		public ITsString Form { get; set; }

		public ITsString Gloss { get; set; }

		public IPartOfSpeech Category { get; set; }

		public bool NegateCategory { get; set; }

		public IDictionary<IFsFeatDefn, object> InflFeatures => m_inflFeatures;

		public override PatternNode<ComplexConcParagraphData, ShapeNode> GeneratePattern(FeatureSystem featSys)
		{
			var fs = new FeatureStruct();
			var typeFeat = featSys.GetFeature<SymbolicFeature>("type");
			fs.AddValue(typeFeat, typeFeat.PossibleSymbols["word"]);
			AddStringValue(featSys, fs, Form, "form");
			AddStringValue(featSys, fs, Gloss, "gloss");
			if (Category != null)
			{
				var catFeat = featSys.GetFeature<SymbolicFeature>("cat");
				var symbols = Category.ReallyReallyAllPossibilities.Concat(Category).Select(pos => catFeat.PossibleSymbols[pos.Hvo.ToString(CultureInfo.InvariantCulture)]);
				if (NegateCategory)
				{
					symbols = catFeat.PossibleSymbols.Except(symbols);
				}
				fs.AddValue(catFeat, symbols);
			}
			if (m_inflFeatures.Count > 0)
			{
				var inflFeat = featSys.GetFeature<ComplexFeature>("infl");
				fs.AddValue(inflFeat, GetFeatureStruct(featSys, m_inflFeatures));
			}
			var wordBdryFS = FeatureStruct.New(featSys).Symbol("bdry").Symbol("wordBdry").Value;
			var group = new Group<ComplexConcParagraphData, ShapeNode>();
			group.Children.Add(new Quantifier<ComplexConcParagraphData, ShapeNode>(0, 1, new Constraint<ComplexConcParagraphData, ShapeNode>(wordBdryFS))
			{
				IsGreedy = false
			});
			group.Children.Add(new Constraint<ComplexConcParagraphData, ShapeNode>(fs));
			group.Children.Add(new Quantifier<ComplexConcParagraphData, ShapeNode>(0, 1, new Constraint<ComplexConcParagraphData, ShapeNode>(wordBdryFS))
			{
				IsGreedy = false
			});
			return AddQuantifier(group);
		}
	}
}