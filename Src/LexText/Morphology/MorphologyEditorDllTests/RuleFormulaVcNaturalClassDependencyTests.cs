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
	/// The display dependencies a rule formula registers on the natural classes it draws. A class
	/// name decides whether the formula draws an abbreviation or a feature list, and both the
	/// name
	/// and the abbreviation decide the cell size, so editing either has to rebuild the formula.
	/// </summary>
	[TestFixture]
	public class RuleFormulaVcNaturalClassDependencyTests : RuleFormulaVcTestBase
	{
		[Test]
		public void RegularRule_RegistersTheNameAndAbbreviationOfItsClass()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucDescNCContext(rhs, natClass);

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidName), Is.True);
			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidAbbreviation), Is.True);
		}

		[Test]
		public void RegularRule_RegistersAClassInItsStructuralChange()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Nasal", "N",
				new FeatVals { { "nasal", "+" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucChangeNCContext(rhs, natClass);

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidName), Is.True);
		}

		[Test]
		public void RegularRule_RegistersAClassNestedInASequenceContext()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			rhs.LeftContextOA = AddSequenceContext(AddStandaloneNCContext(natClass));

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidName), Is.True);
			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidAbbreviation), Is.True);
		}

		[Test]
		public void RegularRule_RegistersAClassInsideAnIterationContext()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			rhs.RightContextOA = AddIterationContext(AddStandaloneNCContext(natClass));

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidName), Is.True);
		}

		[Test]
		public void RegularRule_WithAnEmptyIterationContext_DrawsWithoutError()
		{
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			rhs.LeftContextOA = AddIterationContext(null);

			Assert.That(() => DrawRegularRule(rhs), Throws.Nothing);
		}

		[Test]
		public void RegularRule_WithoutSurroundingContexts_DrawsWithoutError()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucDescNCContext(rhs, natClass);

			Assert.That(rhs.LeftContextOA, Is.Null);
			Assert.That(rhs.RightContextOA, Is.Null);
			Assert.That(() => DrawRegularRule(rhs), Throws.Nothing);
		}

		[Test]
		public void RegularRule_WithoutFeatureClasses_RegistersNoNaturalClassDependency()
		{
			IPhSegRuleRHS rhs = AddRegularRule("s to n");

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(NamesRegistered(env), Is.Empty);
		}

		[Test]
		public void RegularRule_RegistersEveryClassItDraws()
		{
			IPhNCFeatures first = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhNCFeatures second = AddFeatureNaturalClass("Nasal", "N",
				new FeatVals { { "nasal", "+" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucDescNCContext(rhs, first);
			AddStrucChangeNCContext(rhs, second);

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(NamesRegistered(env), Is.EquivalentTo(new[] { first.Hvo, second.Hvo }));
		}

		[Test]
		public void AffixProcessRule_RegistersTheClassInItsInput()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IMoAffixProcess rule = AddAffixProcessRule();
			AddInputNCContext(rule, natClass);

			var vc = new AffixRuleFormulaVc(Cache, m_propertyTable);
			var env = new RecordingCollectorEnv(Cache.MainCacheAccessor, rule.Hvo);
			vc.Display(env, rule.Hvo, AffixRuleFormulaVc.kfragRule);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidName), Is.True);
			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidAbbreviation), Is.True);
		}

		[Test]
		public void MetathesisRule_RegistersTheNameAndAbbreviationOfItsClass()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhMetathesisRule rule = AddMetathesisRule("t s to s t");
			AddStrucDescNCContext(rule, natClass);

			RecordingCollectorEnv env = DrawMetathesisRule(rule);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidName), Is.True);
			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidAbbreviation), Is.True);
		}

		[Test]
		public void MetathesisRule_RegistersEveryClassItDraws()
		{
			IPhNCFeatures first = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			IPhNCFeatures second = AddFeatureNaturalClass("Nasal", "N",
				new FeatVals { { "nasal", "+" } });
			IPhMetathesisRule rule = AddMetathesisRule("t s to s t");
			AddStrucDescNCContext(rule, first);
			AddStrucDescNCContext(rule, second);

			RecordingCollectorEnv env = DrawMetathesisRule(rule);

			Assert.That(NamesRegistered(env), Is.EquivalentTo(new[] { first.Hvo, second.Hvo }));
		}

		[Test]
		public void MetathesisRule_WithoutContexts_DrawsWithoutError()
		{
			IPhMetathesisRule rule = AddMetathesisRule("t s to s t");

			Assert.That(() => DrawMetathesisRule(rule), Throws.Nothing);
		}

		/// <summary>
		/// A segment-based class is drawn as its abbreviation and its cell is sized to it, while
		/// registration covers feature-based classes. Editing a segment class's abbreviation
		/// therefore leaves the cell at the width the old abbreviation asked for until something
		/// else rebuilds the formula.
		/// </summary>
		[Test]
		public void RegularRule_DoesNotRegisterASegmentClass()
		{
			IPhNCSegments natClass = AddSegmentNaturalClass("Consonant", "C");
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucDescNCContext(rhs, natClass);

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidAbbreviation), Is.False);
		}

		private RecordingCollectorEnv DrawRegularRule(IPhSegRuleRHS rhs)
		{
			var vc = new RegRuleFormulaVc(Cache, m_propertyTable);
			var env = new RecordingCollectorEnv(Cache.MainCacheAccessor, rhs.Hvo);
			vc.Display(env, rhs.Hvo, RegRuleFormulaVc.kfragRHS);
			return env;
		}

		private RecordingCollectorEnv DrawMetathesisRule(IPhMetathesisRule rule)
		{
			var vc = new MetaRuleFormulaVc(Cache, m_propertyTable);
			var env = new RecordingCollectorEnv(Cache.MainCacheAccessor, rule.Hvo);
			vc.Display(env, rule.Hvo, MetaRuleFormulaVc.kfragRule);
			return env;
		}

		private static int[] NamesRegistered(RecordingCollectorEnv env)
		{
			return env.Dependencies
				.SelectMany(call => Enumerable.Range(0, call.Count)
					.Where(i => call.Tags[i] == PhNaturalClassTags.kflidName)
					.Select(i => call.Hvos[i]))
				.Distinct()
				.ToArray();
		}
	}
}
