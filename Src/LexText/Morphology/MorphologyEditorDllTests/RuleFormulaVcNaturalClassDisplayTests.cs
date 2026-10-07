// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;
using FeatVals = System.Collections.Generic.Dictionary<string, string>;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// What a rule formula draws for a natural class context: a class the user has named shows
	/// its abbreviation in brackets, and a class generated for a rule shows its feature list.
	/// </summary>
	[TestFixture]
	public class RuleFormulaVcNaturalClassDisplayTests : RuleFormulaVcTestBase
	{
		private const string LeftBracketUpHook = "\u23a1";
		private const string LeftBracketLowHook = "\u23a3";

		[Test]
		public void NamedClass_WithOneFeature_DrawsAbbreviation()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[Vd]"));
		}

		[Test]
		public void NamedClass_WithThreeFeatures_DrawsAbbreviationOnly()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced stop", "Vd",
				new FeatVals { { "vd", "+" }, { "cons", "+" }, { "cont", "-" } });

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[Vd]"),
				"a named class is drawn as its abbreviation however many features it carries");
		}

		[Test]
		public void NamedClass_WithNoFeatures_DrawsAbbreviation()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd", new FeatVals());

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[Vd]"),
				"a named class is drawn as its abbreviation even before it has features");
		}

		[Test]
		public void NamedClass_WithNoAbbreviation_DrawsStars()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", null,
				new FeatVals { { "vd", "+" } });

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[***]"));
		}

		[Test]
		public void NamedClass_WithEmptyAbbreviation_DrawsStars()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", string.Empty,
				new FeatVals { { "vd", "+" } });

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[***]"));
		}

		[Test]
		public void RuleNamedClass_WithOneFeature_DrawsFeature()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "vd", "+" } });

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[+ vd]"));
		}

		[Test]
		public void RuleNamedClass_WithThreeFeatures_DrawsEveryFeatureInAPile()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "vd", "+" }, { "cons", "+" }, { "cont", "-" } });

			string drawn = DrawContext(AddStandaloneNCContext(natClass));

			Assert.That(drawn, Does.Contain("+ vd"));
			Assert.That(drawn, Does.Contain("+ cons"));
			Assert.That(drawn, Does.Contain("- cont"));
			Assert.That(drawn, Does.Contain(LeftBracketUpHook),
				"a class drawn over several lines is bracketed by hooks, not square brackets");
			Assert.That(drawn, Does.Contain(LeftBracketLowHook));
		}

		[Test]
		public void RuleNamedClass_WithNoFeatures_DrawsQuestions()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n", new FeatVals());

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[???]"));
		}

		/// <summary>
		/// Whether a class was generated for a rule decides the drawing, not what its
		/// abbreviation says: a generated class keeps its feature list even after someone
		/// abbreviates it "C".
		/// </summary>
		[Test]
		public void RuleNamedClass_AbbreviatedC_DrawsFeature()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "cons", "+" } });
			natClass.Abbreviation.SetAnalysisDefaultWritingSystem("C");

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[+ cons]"));
		}

		/// <summary>
		/// A class generated for a rule becomes the user's once they name it in the natural class
		/// editor, so from then on the formula draws its abbreviation instead of its features.
		/// </summary>
		[Test]
		public void RuleNamedClass_OnceNamed_DrawsAbbreviation()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "vd", "+" } });
			IPhSimpleContextNC ctxt = AddStandaloneNCContext(natClass);
			Assert.That(DrawContext(ctxt), Is.EqualTo("[+ vd]"), "before the class is named");

			natClass.Name.SetAnalysisDefaultWritingSystem("Voiced");
			natClass.Abbreviation.SetAnalysisDefaultWritingSystem("Vd");

			Assert.That(DrawContext(ctxt), Is.EqualTo("[Vd]"), "after the class is named");
		}

		[Test]
		public void SegmentClass_DrawsAbbreviation()
		{
			IPhNCSegments natClass = AddSegmentNaturalClass("Consonant", "C");

			Assert.That(DrawContext(AddStandaloneNCContext(natClass)), Is.EqualTo("[C]"));
		}

		[Test]
		public void NamedClass_InsideAnIterationContext_DrawsAbbreviation()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhSimpleContextNC member = AddStandaloneNCContext(natClass);

			Assert.That(DrawContext(AddIterationContext(member)), Does.Contain("[Vd]"));
		}

		private string DrawContext(IPhContextOrVar ctxtOrVar)
		{
			var vc = new TestRuleFormulaVc(Cache, m_propertyTable);
			var env = new RecordingCollectorEnv(Cache.MainCacheAccessor, ctxtOrVar.Hvo);
			vc.Display(env, ctxtOrVar.Hvo, RuleFormulaVcBase.kfragContext);
			return env.Text;
		}
	}
}
