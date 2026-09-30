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
	/// name and the abbreviation decide the cell size, so editing either has to rebuild the
	/// formula. The collector environment does not re-run layout, so these assert the
	/// registration that triggers the rebuild rather than the rebuild itself.
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

		/// <summary>
		/// Naming a class generated for a rule turns its feature list into an abbreviation,
		/// so the formula has to depend on that class's name before the user names it.
		/// </summary>
		[Test]
		public void RegularRule_RegistersTheNameOfARuleNamedClass()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "vd", "+" } });
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			AddStrucDescNCContext(rhs, natClass);

			RecordingCollectorEnv env = DrawRegularRule(rhs);

			Assert.That(env.DependsOn(natClass.Hvo, PhNaturalClassTags.kflidName), Is.True);
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

		/// <summary>
		/// An iteration context loses its member when the class inside it is deleted, and the
		/// formula must still draw.
		/// </summary>
		[Test]
		public void RegularRule_WithAnEmptyIterationContext_DrawsWithoutError()
		{
			IPhSegRuleRHS rhs = AddRegularRule("s to n");
			rhs.LeftContextOA = AddIterationContext(null);

			Assert.That(() => DrawRegularRule(rhs), Throws.Nothing);
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
