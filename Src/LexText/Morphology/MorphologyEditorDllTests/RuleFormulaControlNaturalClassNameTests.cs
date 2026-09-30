// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using FeatVals = System.Collections.Generic.Dictionary<string, string>;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Which feature-based natural classes count as carrying a name the user gave them. This is
	/// the single input deciding whether a rule formula draws a class as an abbreviation or as a
	/// feature list, so each shape of name a project can hold is pinned here.
	/// </summary>
	[TestFixture]
	public class RuleFormulaControlNaturalClassNameTests : RuleFormulaVcTestBase
	{
		[Test]
		public void NoClass_IsNotUserDefined()
		{
			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(null), Is.False);
		}

		/// <summary>
		/// The check answers whether a class carries a name the user gave it, and an unnamed
		/// class carries none. Set Phonological Features relies on that answer to keep
		/// offering feature edits on unnamed classes (LT-22576).
		/// </summary>
		[Test]
		public void ClassWithNoName_IsNotUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, "Vd",
				new FeatVals { { "vd", "+" } });

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.False,
				"an unnamed class carries no name the user gave it");
		}

		[Test]
		public void ClassWithAnEmptyName_IsNotUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(string.Empty, "Vd",
				new FeatVals { { "vd", "+" } });

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.False,
				"an empty name is not a name the user gave the class");
		}

		[Test]
		public void ClassNamedForARule_IsNotUserDefined()
		{
			IPhNCFeatures natClass = AddRuleNamedFeatureNaturalClass("s to n",
				new FeatVals { { "vd", "+" } });

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.False);
		}

		/// <summary>
		/// Projects hold rule-generated names that carry a prefix ahead of the generated text, so
		/// the generated text is recognised wherever it sits in the name.
		/// </summary>
		[Test]
		public void ClassNamedForARuleBehindAPrefix_IsNotUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, null,
				new FeatVals { { "vd", "+" } });
			natClass.Name.SetUserWritingSystem("Phonemes s and t - "
				+ string.Format(MEStrings.ksRuleNCFeatsName, "s to n"));

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.False);
		}

		[Test]
		public void NamedClass_IsUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.True);
		}

		/// <summary>
		/// The natural class editor writes a name into the analysis writing systems while the
		/// check reads the user writing system. A project whose analysis language differs from
		/// its interface language must still have its named classes qualify.
		/// </summary>
		[Test]
		public void ClassNamedInAnAnalysisWritingSystemOtherThanTheUserOne_IsUserDefined()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de",
				out CoreWritingSystemDefinition german);
			Cache.LanguageProject.AddToCurrentAnalysisWritingSystems(german);
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, "Vd",
				new FeatVals { { "vd", "+" } });
			natClass.Name.set_String(german.Handle,
				TsStringUtils.MakeString("Stimmhaft", german.Handle));
			Assert.That(natClass.Name.UserDefaultWritingSystem.Text, Is.EqualTo("Stimmhaft"),
				"the name must reach the check through the analysis writing system, "
				+ "not through a placeholder");

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.True);
		}

		[Test]
		public void NamedClassWithNoFeatures_IsUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass("Voiced", "Vd", new FeatVals());

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.True,
				"a name qualifies a class whatever it carries for features");
		}
	}
}
