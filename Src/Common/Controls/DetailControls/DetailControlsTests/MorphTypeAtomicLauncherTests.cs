// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Regression tests for the DoNotRefresh + m_postponePropChanged interaction (LT-22414).
	///
	/// Root cause: when <c>m_postponePropChanged=true</c> (default since LT-22018),
	/// <see cref="DataTree.PropChanged"/> defers refresh via <c>BeginInvoke</c>.
	/// This means <see cref="DataTree.RefreshListNeeded"/> is never set during a
	/// <c>DoNotRefresh</c> window, so releasing <c>DoNotRefresh</c> does not trigger
	/// a synchronous refresh. Code that brackets LCModel changes with DoNotRefresh
	/// (like <c>MorphTypeAtomicLauncher.SwapValues</c>) must explicitly set
	/// <c>RefreshListNeeded=true</c> before releasing <c>DoNotRefresh</c>.
	/// </summary>
	[TestFixture]
	public class MorphTypeSwapRefreshTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		private Inventory m_parts;
		private Inventory m_layouts;
		private ILexEntry m_entry;
		private Mediator m_mediator;
		private PropertyTable m_propertyTable;
		private DataTree m_dtree;
		private Form m_parent;

		#region Fixture Setup and Teardown

		public override void FixtureSetup()
		{
			base.FixtureSetup();
			m_layouts = DataTreeTests.GenerateLayouts();
			m_parts = DataTreeTests.GenerateParts();
		}

		#endregion

		#region Test Setup and Teardown

		public override void TestSetup()
		{
			base.TestSetup();

			m_entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
			m_entry.CitationForm.VernacularDefaultWritingSystem =
				TsStringUtils.MakeString("test", Cache.DefaultVernWs);
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("bib content");
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("bib content");

			m_dtree = new DataTree();
			m_mediator = new Mediator();
			m_propertyTable = new PropertyTable(m_mediator);
			m_dtree.Init(m_mediator, m_propertyTable, null);
			m_parent = new Form();
			m_parent.Controls.Add(m_dtree);
		}

		public override void TestTearDown()
		{
			if (m_parent != null)
			{
				m_parent.Close();
				m_parent.Dispose();
			}
			if (m_propertyTable != null)
			{
				m_propertyTable.Dispose();
				m_propertyTable = null;
			}
			if (m_mediator != null)
			{
				m_mediator.Dispose();
				m_mediator = null;
			}

			base.TestTearDown();
		}

		#endregion

		/// <summary>
		/// LT-22414 regression: after clearing bibliography data inside a
		/// DoNotRefresh window, the ifdata bibliography slice should disappear.
		///
		/// With m_postponePropChanged=true (default since LT-22018), PropChanged
		/// defers via BeginInvoke and never sets RefreshListNeeded during the
		/// DoNotRefresh window. Callers (like SwapValues) must explicitly set
		/// RefreshListNeeded=true before releasing DoNotRefresh.
		///
		/// RED phase:  comment out RefreshListNeeded=true → test FAILS (stale slices).
		/// GREEN phase: RefreshListNeeded=true present → test PASSES.
		/// </summary>
		[Test]
		public void DoNotRefresh_SlicesMustReflectChanges_AfterRelease_LT22414()
		{
			// Arrange: show entry with CfAndBib layout (CitationForm + Bibliography ifdata)
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);

			// Verify initial state: both slices visible (bib has data)
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2),
				"Setup: should have CitationForm + Bibliography");

			// Act: simulate the DoNotRefresh pattern from SwapValues
			m_dtree.DoNotRefresh = true;

			// Make changes that should affect visible slices:
			// clearing bibliography data should cause the ifdata slice to disappear on refresh
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");

			// LT-22414 FIX: callers must explicitly set RefreshListNeeded=true.
			// Without this line, the test FAILS (proving the bug exists).
			// >>> COMMENT OUT THIS LINE TO SEE THE BUG <<<
			m_dtree.RefreshListNeeded = true;

			m_dtree.DoNotRefresh = false;

			// Assert: after refresh, bibliography slice should be gone (no data → ifdata hides it)
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(1),
				"LT-22414: After DoNotRefresh=false, slices should reflect data changes. " +
				"Bibliography has no data so ifdata should hide it. " +
				"If this fails with count=2, no refresh occurred.");
			Assert.That((m_dtree.Controls[0] as Slice).Label, Is.EqualTo("CitationForm"));
		}

		/// <summary>
		/// Complementary test: verify that WITHOUT RefreshListNeeded=true,
		/// releasing DoNotRefresh does NOT trigger a refresh (the bug behavior).
		/// This documents the root cause of LT-22414.
		/// </summary>
		[Test]
		public void DoNotRefresh_WithoutRefreshListNeeded_DoesNotRefresh_LT22414_BugDemo()
		{
			// Arrange: show entry with CfAndBib layout
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));

			var originalSlice = m_dtree.Slices[0];

			// Act: DoNotRefresh without setting RefreshListNeeded
			m_dtree.DoNotRefresh = true;
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
			// Intentionally NOT setting RefreshListNeeded (simulates buggy SwapValues)
			m_dtree.DoNotRefresh = false;

			// Assert: slices are STALE — bibliography still visible despite no data
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2),
				"Without RefreshListNeeded, DoNotRefresh=false does not trigger refresh; " +
				"slices remain stale (bibliography still visible despite no data).");
		}

		[Test]
		public void DoNotRefresh_ClearedRefreshListNeededBeforeRelease_DoesNotRefresh()
		{
			m_dtree.Initialize(Cache, false, m_layouts, m_parts);
			m_dtree.ShowObject(m_entry, "CfAndBib", null, m_entry, false);
			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2));

			m_dtree.DoNotRefresh = true;
			m_entry.Bibliography.SetVernacularDefaultWritingSystem("");
			m_entry.Bibliography.SetAnalysisDefaultWritingSystem("");
			m_dtree.RefreshListNeeded = true;
			m_dtree.RefreshListNeeded = false;

			m_dtree.DoNotRefresh = false;

			Assert.That(m_dtree.Controls.Count, Is.EqualTo(2),
				"Clearing RefreshListNeeded before releasing DoNotRefresh should cancel the " +
				"synchronous rebuild and preserve the current slice tree.");
			Assert.That(m_dtree.RefreshListNeeded, Is.False);
		}

		[TestCaseSource(nameof(StemLikeMorphTypes))]
		public void IsStemType_StemLikeMorphTypes_ReturnsTrue(Guid morphTypeGuid)
		{
			var morphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(morphTypeGuid);

			Assert.That(InvokeIsStemType(morphType), Is.True);
		}

		[TestCaseSource(nameof(AffixLikeMorphTypes))]
		public void IsStemType_AffixLikeMorphTypes_ReturnsFalse(Guid morphTypeGuid)
		{
			var morphType = Cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>().GetObject(morphTypeGuid);

			Assert.That(InvokeIsStemType(morphType), Is.False);
		}

		[Test]
		public void IsStemType_NullMorphType_ReturnsFalse()
		{
			Assert.That(InvokeIsStemType(null), Is.False);
		}

		[Test]
		public void CheckForStemDataLoss_EmptyStemAndNoMorphSyntaxAnalyses_AllowsChangeWithoutPrompt()
		{
			var stem = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_entry.LexemeFormOA = stem;

			Assert.That(InvokeCheckForStemDataLoss(stem, new List<IMoMorphSynAnalysis>()), Is.False);
		}

		[Test]
		public void CheckForAffixDataLoss_EmptyAffixAndNoMorphSyntaxAnalyses_AllowsChangeWithoutPrompt()
		{
			var affix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			m_entry.LexemeFormOA = affix;

			Assert.That(InvokeCheckForAffixDataLoss(affix, new List<IMoMorphSynAnalysis>()), Is.False);
		}

		[Test]
		public void GetStemDataLossKinds_StemNameAndGrammarInfo_FlagsBoth()
		{
			var stem = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
			m_entry.LexemeFormOA = stem;
			var partOfSpeech = CreatePartOfSpeech("phase2-stem-pos");
			stem.StemNameRA = CreateStemName(partOfSpeech, "phase2-stem-name");
			var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
			m_entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.InflectionClassRA = CreateInflectionClass(partOfSpeech, "phase2-stem-class");

			Assert.That(
				InvokeGetStemDataLossKinds(stem, new List<IMoMorphSynAnalysis> { msa }),
				Is.EqualTo(MorphTypeDataLossKinds.StemName | MorphTypeDataLossKinds.GrammarInfo));
		}

		[Test]
		public void GetAffixDataLossKinds_AffixProcessWithInflectionClassAndGrammarInfo_FlagsRuleInflectionClassAndGrammarInfo()
		{
			var affix = Cache.ServiceLocator.GetInstance<IMoAffixProcessFactory>().Create();
			m_entry.LexemeFormOA = affix;
			var partOfSpeech = CreatePartOfSpeech("phase2-affix-pos");
			affix.InflectionClassesRC.Add(CreateInflectionClass(partOfSpeech, "phase2-affix-class"));
			var msa = Cache.ServiceLocator.GetInstance<IMoInflAffMsaFactory>().Create();
			m_entry.MorphoSyntaxAnalysesOC.Add(msa);
			msa.AffixCategoryRA = partOfSpeech;

			Assert.That(
				InvokeGetAffixDataLossKinds(affix, new List<IMoMorphSynAnalysis> { msa }),
				Is.EqualTo(
					MorphTypeDataLossKinds.Rule |
					MorphTypeDataLossKinds.InflectionClass |
					MorphTypeDataLossKinds.GrammarInfo));
		}

		[Test]
		public void GetAffixDataLossKinds_AffixAllomorphWithPositionAndMsEnv_FlagsInfixLocationAndGrammarInfo()
		{
			var affix = Cache.ServiceLocator.GetInstance<IMoAffixAllomorphFactory>().Create();
			m_entry.LexemeFormOA = affix;
			affix.PositionRS.Add(CreateEnvironment("/ _"));
			affix.MsEnvPartOfSpeechRA = CreatePartOfSpeech("phase2-infix-pos");

			Assert.That(
				InvokeGetAffixDataLossKinds(affix, new List<IMoMorphSynAnalysis>()),
				Is.EqualTo(MorphTypeDataLossKinds.InfixLocation | MorphTypeDataLossKinds.GrammarInfo));
		}

		[Test]
		public void LauncherButtonClick_WithValidObject_ReachesChooserDecisionPath()
		{
			using (var launcher = new RecordingAtomicReferenceLauncher())
			{
				launcher.Initialize(Cache, m_entry, LexEntryTags.kflidMorphoSyntaxAnalyses, "MorphoSyntaxAnalysesOC", "analysis");

				launcher.InvokeLauncherClickForTest();

				Assert.That(launcher.ChooserInvocationCount, Is.EqualTo(1));
				Assert.That(launcher.LauncherButton.Name, Is.EqualTo("m_btnLauncher"));
				Assert.That(launcher.LauncherButton.Enabled, Is.True);
			}
		}

		private static IEnumerable<TestCaseData> StemLikeMorphTypes()
		{
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphBoundRoot).SetName("IsStemType_BoundRoot_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphBoundStem).SetName("IsStemType_BoundStem_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphClitic).SetName("IsStemType_Clitic_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphDiscontiguousPhrase).SetName("IsStemType_DiscontiguousPhrase_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphEnclitic).SetName("IsStemType_Enclitic_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphParticle).SetName("IsStemType_Particle_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphPhrase).SetName("IsStemType_Phrase_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphProclitic).SetName("IsStemType_Proclitic_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphRoot).SetName("IsStemType_Root_ReturnsTrue");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphStem).SetName("IsStemType_Stem_ReturnsTrue");
		}

		private static IEnumerable<TestCaseData> AffixLikeMorphTypes()
		{
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphCircumfix).SetName("IsStemType_Circumfix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphInfix).SetName("IsStemType_Infix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphInfixingInterfix).SetName("IsStemType_InfixingInterfix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphPrefix).SetName("IsStemType_Prefix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphPrefixingInterfix).SetName("IsStemType_PrefixingInterfix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphSimulfix).SetName("IsStemType_Simulfix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphSuffix).SetName("IsStemType_Suffix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphSuffixingInterfix).SetName("IsStemType_SuffixingInterfix_ReturnsFalse");
			yield return new TestCaseData(MoMorphTypeTags.kguidMorphSuprafix).SetName("IsStemType_Suprafix_ReturnsFalse");
		}

		private static bool InvokeIsStemType(IMoMorphType morphType)
		{
			var launcher = new MorphTypeAtomicLauncher();
			return launcher.IsStemType(morphType);
		}

		private static bool InvokeCheckForStemDataLoss(
			IMoStemAllomorph stem,
			List<IMoMorphSynAnalysis> morphSyntaxAnalyses)
		{
			var launcher = new MorphTypeAtomicLauncher();
			return launcher.CheckForStemDataLoss(stem, morphSyntaxAnalyses);
		}

		private static bool InvokeCheckForAffixDataLoss(
			IMoAffixForm affix,
			List<IMoMorphSynAnalysis> morphSyntaxAnalyses)
		{
			var launcher = new MorphTypeAtomicLauncher();
			return launcher.CheckForAffixDataLoss(affix, morphSyntaxAnalyses);
		}

		private static MorphTypeDataLossKinds InvokeGetStemDataLossKinds(
			IMoStemAllomorph stem,
			List<IMoMorphSynAnalysis> morphSyntaxAnalyses)
		{
			var launcher = new MorphTypeAtomicLauncher();
			return launcher.GetStemDataLossKinds(stem, morphSyntaxAnalyses);
		}

		private static MorphTypeDataLossKinds InvokeGetAffixDataLossKinds(
			IMoAffixForm affix,
			List<IMoMorphSynAnalysis> morphSyntaxAnalyses)
		{
			var launcher = new MorphTypeAtomicLauncher();
			return launcher.GetAffixDataLossKinds(affix, morphSyntaxAnalyses);
		}

		private IPartOfSpeech CreatePartOfSpeech(string name)
		{
			var partOfSpeech = Cache.ServiceLocator.GetInstance<IPartOfSpeechFactory>().Create();
			Cache.LangProject.PartsOfSpeechOA.PossibilitiesOS.Add(partOfSpeech);
			partOfSpeech.Name.SetAnalysisDefaultWritingSystem(name);
			partOfSpeech.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			return partOfSpeech;
		}

		private IMoStemName CreateStemName(IPartOfSpeech partOfSpeech, string name)
		{
			var stemName = Cache.ServiceLocator.GetInstance<IMoStemNameFactory>().Create();
			partOfSpeech.StemNamesOC.Add(stemName);
			stemName.Name.SetAnalysisDefaultWritingSystem(name);
			stemName.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			return stemName;
		}

		private IMoInflClass CreateInflectionClass(IPartOfSpeech partOfSpeech, string name)
		{
			var inflClass = Cache.ServiceLocator.GetInstance<IMoInflClassFactory>().Create();
			partOfSpeech.InflectionClassesOC.Add(inflClass);
			inflClass.Name.SetAnalysisDefaultWritingSystem(name);
			inflClass.Abbreviation.SetAnalysisDefaultWritingSystem(name);
			return inflClass;
		}

		private IPhEnvironment CreateEnvironment(string representation)
		{
			var environment = Cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>().Create();
			Cache.LanguageProject.PhonologicalDataOA.EnvironmentsOS.Add(environment);
			environment.StringRepresentation = TsStringUtils.MakeString(representation, Cache.DefaultVernWs);
			return environment;
		}

		private sealed class RecordingAtomicReferenceLauncher : MockAtomicReferenceLauncher
		{
			public int ChooserInvocationCount { get; private set; }

			public void InvokeLauncherClickForTest()
			{
				OnClick(LauncherButton, EventArgs.Empty);
			}

			protected override void HandleChooser()
			{
				ChooserInvocationCount++;
			}
		}
	}
}
