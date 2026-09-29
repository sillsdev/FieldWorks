// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.LCModel;
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

		[Test]
		public void ClassWithNoName_IsNotUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, "Vd",
				new FeatVals { { "vd", "+" } });

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.False,
				"an unnamed class reads back as a placeholder rather than as empty text");
		}

		[Test]
		public void ClassWithAnEmptyName_IsNotUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(string.Empty, "Vd",
				new FeatVals { { "vd", "+" } });

			Assert.That(RuleFormulaControl.IsFeatureBasedNCNameUserDefined(natClass), Is.False);
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
		/// check reads the user writing system, so the name has to be found across writing
		/// systems for a named class to qualify.
		/// </summary>
		[Test]
		public void ClassNamedInTheVernacularWritingSystemOnly_IsUserDefined()
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, "Vd",
				new FeatVals { { "vd", "+" } });
			natClass.Name.SetVernacularDefaultWritingSystem("Voiced");

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
