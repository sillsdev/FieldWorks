// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Linq;
using NUnit.Framework;
using SIL.LCModel;
using FeatVals = System.Collections.Generic.Dictionary<string, string>;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// The line count and cell width a rule formula reserves for a natural class context. A cell
	/// sized from anything other than what the context draws leaves the drawing clipped or the
	/// cell padded, so the governing assertion is that the two agree.
	/// </summary>
	[TestFixture]
	public class RuleFormulaVcNaturalClassSizingTests : RuleFormulaVcTestBase
	{
		[Test]
		public void NamedClass_WithOneFeature_IsSizedToItsAbbreviation()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });

			AssertCellMatchesDrawing(AddStandaloneNCContext(natClass), "[Vd]");
		}

		[Test]
		public void NamedClass_WithThreeFeatures_IsSizedToItsAbbreviation()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced stop", "Vd",
				new FeatVals { { "vd", "+" }, { "cons", "+" }, { "cont", "-" } });

			AssertCellMatchesDrawing(AddStandaloneNCContext(natClass), "[Vd]");
		}

		[Test]
		public void NamedClass_WithNoAbbreviation_IsSizedToTheStars()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", null,
				new FeatVals { { "vd", "+" } });

			AssertCellMatchesDrawing(AddStandaloneNCContext(natClass), "[***]");
		}

		[Test]
		public void RuleNamedClass_WithOneFeature_IsSizedToItsFeature()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "vd", "+" } });

			AssertCellMatchesDrawing(AddStandaloneNCContext(natClass), "[+ vd]");
		}

		[Test]
		public void NamedClass_WithLongAbbreviation_IsSizedToTheWholeAbbreviation()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiceless aspirated stop",
				"VlAspStop", new FeatVals { { "vd", "-" }, { "cons", "+" } });

			AssertCellMatchesDrawing(AddStandaloneNCContext(natClass), "[VlAspStop]");
		}

		[Test]
		public void NamedClass_WithThreeFeatures_OccupiesOneLine()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced stop", "Vd",
				new FeatVals { { "vd", "+" }, { "cons", "+" }, { "cont", "-" } });

			Assert.That(NumLinesFor(AddStandaloneNCContext(natClass)), Is.EqualTo(1));
		}

		[Test]
		public void RuleNamedClass_WithThreeFeatures_OccupiesOneLinePerFeature()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "vd", "+" }, { "cons", "+" }, { "cont", "-" } });

			Assert.That(NumLinesFor(AddStandaloneNCContext(natClass)), Is.EqualTo(3));
		}

		[Test]
		public void RuleNamedClass_WithNoFeatures_OccupiesNoLines()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n", new FeatVals());

			Assert.That(NumLinesFor(AddStandaloneNCContext(natClass)), Is.EqualTo(0));
		}

		/// <summary>
		/// A formula is as tall as its tallest context, so naming the only many-featured class in
		/// a rule brings the whole formula down to a single line.
		/// </summary>
		[Test]
		public void NamedClass_IsTheHeightOfAFormulaThatHoldsNothingTaller()
		{
			IPhNCFeatures named = AddFeatureNaturalClass("Voiced stop", "Vd",
				new FeatVals { { "vd", "+" }, { "cons", "+" }, { "cont", "-" } });
			IPhNCSegments segments = AddSegmentNaturalClass("Consonant", "C");
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucDescNCContext(rhs, named);
			AddStrucChangeNCContext(rhs, segments);

			var vc = new TestRuleFormulaVc(Cache, m_propertyTable);
			int tallest = rhs.OwningRule.StrucDescOS.Concat(rhs.StrucChangeOS)
				.Max(ctxt => vc.NumLinesFor(ctxt));

			Assert.That(tallest, Is.EqualTo(1));
		}

		private int NumLinesFor(IPhContextOrVar ctxtOrVar)
		{
			return new TestRuleFormulaVc(Cache, m_propertyTable).NumLinesFor(ctxtOrVar);
		}

		/// <summary>
		/// Asserts that the context draws the expected text and that its cell is sized to exactly
		/// that text plus the margins every context carries. String measurement in this
		/// environment reports one unit per character, so the two are directly comparable.
		/// </summary>
		private void AssertCellMatchesDrawing(IPhContextOrVar ctxtOrVar, string expected)
		{
			var vc = new TestRuleFormulaVc(Cache, m_propertyTable);
			var env = new RecordingCollectorEnv(Cache.MainCacheAccessor, ctxtOrVar.Hvo);
			vc.Display(env, ctxtOrVar.Hvo, RuleFormulaVcBase.kfragContext);

			Assert.That(env.Text, Is.EqualTo(expected));
			Assert.That(vc.WidthOf(ctxtOrVar, env),
				Is.EqualTo(expected.Length + vc.ContextMargins),
				"the cell is sized to the text the context draws");
		}
	}
}
