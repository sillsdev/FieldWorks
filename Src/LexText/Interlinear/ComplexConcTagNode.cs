using System.Globalization;
using SIL.FieldWorks.FDO;
using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.FieldWorks.IText
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
