// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

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
		/// <summary>
		/// The glyphs that assemble a bracket spanning several lines.
		/// </summary>
		private static readonly char[] MultiLineBracketParts =
			{ '\u23a1', '\u23a2', '\u23a3', '\u23a4', '\u23a5', '\u23a6' };

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

		/// <summary>
		/// A formula is drawn as tall as its tallest context, so a named class with many features
		/// must not stretch the rule holding it into a bracket pile several lines tall.
		/// </summary>
		[Test]
		public void NamedClass_WithThreeFeatures_KeepsTheFormulaOnOneLine()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced stop", "Vd",
				new FeatVals { { "vd", "+" }, { "cons", "+" }, { "cont", "-" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucDescNCContext(rhs, natClass);

			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new RecordingCollectorEnv(Cache.MainCacheAccessor, rhs.Hvo);
			vc.Display(env, rhs.Hvo, RegRuleFormulaVc.kfragRHS);

			Assert.That(env.Text, Does.Contain("[Vd]"));
			Assert.That(env.Text.IndexOfAny(MultiLineBracketParts), Is.EqualTo(-1),
				"no part of the formula is drawn with a bracket spanning several lines");
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
