// Copyright (c) 2013-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Globalization;
using SIL.LCModel;
using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace LanguageExplorer.Areas.TextsAndWords.Tools.ComplexConcordance
{
	public class ComplexConcTagNode : ComplexConcLeafNode
	{
		public ICmPossibility Tag { get; set; }

		public override PatternNode<ComplexConcParagraphData, ShapeNode> GeneratePattern(FeatureSystem featSys)
		{
			var fs = new FeatureStruct();
			var typeFeat = featSys.GetFeature<SymbolicFeature>("type");
			fs.AddValue(typeFeat, typeFeat.PossibleSymbols["ttag"]);

			if (Tag != null)
			{
				var tagFeat = featSys.GetFeature<SymbolicFeature>("tag");
				fs.AddValue(tagFeat, tagFeat.PossibleSymbols[Tag.Hvo.ToString(CultureInfo.InvariantCulture)]);
			}

			return AddQuantifier(new Constraint<ComplexConcParagraphData, ShapeNode>(fs));
		}
	}
}