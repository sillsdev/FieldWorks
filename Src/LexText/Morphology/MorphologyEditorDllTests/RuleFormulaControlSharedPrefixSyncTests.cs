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
	/// Pins how <see cref="RuleFormulaControl.SyncSharedPrefix"/> and
	/// <see cref="RuleFormulaControl.SyncSharedPrefixes"/> keep the shared prefix on generated
	/// natural class names in line with how many rules use each class.
	/// </summary>
	[TestFixture]
	public class RuleFormulaControlSharedPrefixSyncTests : RuleFormulaVcTestBase
	{
		private static string GeneratedName =>
			string.Format(MEStrings.ksRuleNCFeatsName, "s to n");

		private static string PrefixedName =>
			string.Format(MEStrings.ksRuleNCSharedNameFormat, GeneratedName);

		private IPhNCFeatures AddClassNamed(string name)
		{
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, null,
				new FeatVals { { "vd", "+" } });
			natClass.Name.SetUserWritingSystem(name);
			return natClass;
		}

		private void UseInRules(IPhNaturalClass natClass, int ruleCount)
		{
			for (int i = 0; i < ruleCount; i++)
				AddStrucDescNCContext(AddRegularRule("rule " + i), natClass);
		}

		private static string NameOf(IPhNaturalClass natClass)
		{
			return natClass.Name.UserDefaultWritingSystem.Text;
		}

		[Test]
		public void SharedUnprefixedClass_GetsThePrefix()
		{
			IPhNCFeatures natClass = AddClassNamed(GeneratedName);
			UseInRules(natClass, 2);

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.True);
			Assert.That(NameOf(natClass), Is.EqualTo(PrefixedName));
		}

		[Test]
		public void PrefixedClassUsedByOneRule_LosesThePrefix()
		{
			IPhNCFeatures natClass = AddClassNamed(PrefixedName);
			UseInRules(natClass, 1);

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.True);
			Assert.That(NameOf(natClass), Is.EqualTo(GeneratedName));
		}

		[Test]
		public void PrefixedClassUsedByNoRule_LosesThePrefix()
		{
			IPhNCFeatures natClass = AddClassNamed(PrefixedName);

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.True);
			Assert.That(NameOf(natClass), Is.EqualTo(GeneratedName));
		}

		[Test]
		public void UnsharedUnprefixedClass_IsUnchanged()
		{
			IPhNCFeatures natClass = AddClassNamed(GeneratedName);
			UseInRules(natClass, 1);

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.False);
			Assert.That(NameOf(natClass), Is.EqualTo(GeneratedName));
		}

		[Test]
		public void SecondSync_ChangesNothing()
		{
			IPhNCFeatures natClass = AddClassNamed(GeneratedName);
			UseInRules(natClass, 2);
			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.True, "first sync");

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.False);
			Assert.That(NameOf(natClass), Is.EqualTo(PrefixedName));
		}

		[Test]
		public void UserEditedName_IsLeftAloneEvenWhenShared()
		{
			string editedName = GeneratedName + " - voiced";
			IPhNCFeatures natClass = AddClassNamed(editedName);
			UseInRules(natClass, 2);

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.False);
			Assert.That(NameOf(natClass), Is.EqualTo(editedName));
		}

		[Test]
		public void AbbreviatedSharedClass_DoesNotGetThePrefix()
		{
			IPhNCFeatures natClass = AddClassNamed(GeneratedName);
			natClass.Abbreviation.SetAnalysisDefaultWritingSystem("Vd");
			UseInRules(natClass, 2);

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.False);
			Assert.That(NameOf(natClass), Is.EqualTo(GeneratedName));
		}

		[Test]
		public void NullOrUnnamedClass_IsNotChanged()
		{
			IPhNCFeatures unnamed = AddFeatureNaturalClass(null, null,
				new FeatVals { { "vd", "+" } });
			UseInRules(unnamed, 2);

			Assert.That(RuleFormulaControl.SyncSharedPrefix(null), Is.False);
			Assert.That(RuleFormulaControl.SyncSharedPrefix(unnamed), Is.False);
		}

		/// <summary>
		/// The name is read with a fallback to other writing systems, so it is rewritten in the
		/// writing system it was read from rather than added to the user writing system.
		/// </summary>
		[Test]
		public void NameInAnotherAnalysisWs_IsRewrittenInThatWs()
		{
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("de",
				out CoreWritingSystemDefinition german);
			Cache.LanguageProject.AddToCurrentAnalysisWritingSystems(german);
			IPhNCFeatures natClass = AddFeatureNaturalClass(null, null,
				new FeatVals { { "vd", "+" } });
			natClass.Name.set_String(german.Handle,
				TsStringUtils.MakeString(PrefixedName, german.Handle));
			UseInRules(natClass, 1);
			Assert.That(NameOf(natClass), Is.EqualTo(PrefixedName),
				"the name must reach the sync through the analysis writing system");

			Assert.That(RuleFormulaControl.SyncSharedPrefix(natClass), Is.True);
			Assert.That(natClass.Name.get_String(german.Handle).Text,
				Is.EqualTo(GeneratedName));
			Assert.That(natClass.Name.get_String(Cache.DefaultUserWs).Text, Is.Null.Or.Empty,
				"no name is added in the user writing system");
		}

		[Test]
		public void SyncSharedPrefixes_BringsEveryClassInLine()
		{
			IPhNCFeatures shared = AddClassNamed(GeneratedName);
			UseInRules(shared, 2);
			IPhNCFeatures prefixedSingle = AddClassNamed(PrefixedName);
			UseInRules(prefixedSingle, 1);
			IPhNCFeatures plainSingle = AddClassNamed(GeneratedName);
			UseInRules(plainSingle, 1);
			IPhNCFeatures userNamed = AddFeatureNaturalClass("Voiced", "Vd",
				new FeatVals { { "vd", "+" } });
			UseInRules(userNamed, 2);
			IPhNCSegments segmentClass = AddSegmentNaturalClass("Vowels", "V");

			Assert.That(RuleFormulaControl.SyncSharedPrefixes(Cache), Is.True);
			Assert.That(NameOf(shared), Is.EqualTo(PrefixedName));
			Assert.That(NameOf(prefixedSingle), Is.EqualTo(GeneratedName));
			Assert.That(NameOf(plainSingle), Is.EqualTo(GeneratedName));
			Assert.That(NameOf(userNamed), Is.EqualTo("Voiced"));
			Assert.That(segmentClass.Name.AnalysisDefaultWritingSystem.Text,
				Is.EqualTo("Vowels"));

			Assert.That(RuleFormulaControl.SyncSharedPrefixes(Cache), Is.False,
				"a second pass changes nothing");
		}
	}
}
