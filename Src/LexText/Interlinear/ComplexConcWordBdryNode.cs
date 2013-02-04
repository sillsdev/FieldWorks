using SIL.Machine;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace SIL.FieldWorks.IText
{
	public class ComplexConcWordBdryNode : ComplexConcLeafNode
	{
		public override PatternNode<ComplexConcParagraphData, ShapeNode> GeneratePattern(FeatureSystem featSys)
		{
			return new Constraint<ComplexConcParagraphData, ShapeNode>(FeatureStruct.New(featSys).Symbol("bdry").Symbol("wordBdry").Value);
		}
	}
}
