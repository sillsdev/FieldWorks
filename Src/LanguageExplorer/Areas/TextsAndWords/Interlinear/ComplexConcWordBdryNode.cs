// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.Machine.Annotations;
using SIL.Machine.FeatureModel;
using SIL.Machine.Matching;

namespace LanguageExplorer.Areas.TextsAndWords.Interlinear
{
	public class ComplexConcWordBdryNode : ComplexConcLeafNode
	{
		public override PatternNode<ComplexConcParagraphData, ShapeNode> GeneratePattern(FeatureSystem featSys)
		{
			return new Constraint<ComplexConcParagraphData, ShapeNode>(FeatureStruct.New(featSys).Symbol("bdry").Symbol("wordBdry").Value);
		}
	}
}