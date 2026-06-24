// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// avalonia-interlinear-editor (task 2.2) — T2 for the read-only interlinear plugin: the Words Analyses
	/// detail surface composes the <c>InterlinearAnal</c> slice as a native Avalonia Custom row (the plugin's
	/// control factory) instead of the §20.1.3 "unsupported" fallback, and the factory realizes an
	/// <see cref="InterlinearRegionEditor"/> over the projected analysis. Headless Avalonia is initialized by
	/// the assembly-level <c>AvaloniaHeadlessSetUpFixture</c>, so the control genuinely constructs.
	/// </summary>
	[TestFixture]
	public class InterlinearSlicePluginTests : MemoryOnlyBackendProviderTestBase
	{
		private IWfiWordform m_wordform;
		private IWfiAnalysis m_analysis;

		public override void TestSetup()
		{
			base.TestSetup();
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				m_wordform = Cache.ServiceLocator.GetInstance<IWfiWordformFactory>().Create();
				m_wordform.Form.set_String(Cache.DefaultVernWs,
					TsStringUtils.MakeString("kapula", Cache.DefaultVernWs));
				m_analysis = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				m_wordform.AnalysesOC.Add(m_analysis);

				// One stem bundle so the projection has an analysis line to render.
				var entry = Cache.ServiceLocator.GetInstance<ILexEntryFactory>().Create();
				var moForm = Cache.ServiceLocator.GetInstance<IMoStemAllomorphFactory>().Create();
				entry.LexemeFormOA = moForm;
				moForm.Form.set_String(Cache.DefaultVernWs, TsStringUtils.MakeString("pula", Cache.DefaultVernWs));
				var msa = Cache.ServiceLocator.GetInstance<IMoStemMsaFactory>().Create();
				entry.MorphoSyntaxAnalysesOC.Add(msa);
				var sense = Cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
				entry.SensesOS.Add(sense);
				sense.Gloss.set_String(Cache.DefaultAnalWs, TsStringUtils.MakeString("rain", Cache.DefaultAnalWs));
				sense.MorphoSyntaxAnalysisRA = msa;
				var bundle = Cache.ServiceLocator.GetInstance<IWfiMorphBundleFactory>().Create();
				m_analysis.MorphBundlesOS.Add(bundle);
				bundle.MorphRA = moForm;
				bundle.SenseRA = sense;
				bundle.MsaRA = msa;
			});
		}

		private static ViewNode InterlinearNode()
			=> new ViewNode("WfiAnalysis/MorphBundles/#0", ViewNodeKind.Field, "Analysis", null, "MorphBundles",
				"custom", EditorClassification.Dynamic, null, ViewVisibility.Always, ViewExpansion.NotApplicable,
				false, null, Array.Empty<ViewNode>(),
				customEditorClass: InterlinearSlicePlugin.InterlinearSliceClassName,
				customEditorAssembly: "MorphologyEditorDll.dll");

		private RegionEditorBuildContext Ctx(ICmObject obj, ViewNode node)
			=> new RegionEditorBuildContext(obj, node, () => null, Cache);

		[Test]
		public void Plugin_ClaimsTheLegacyInterlinearSliceClass()
		{
			Assert.That(new InterlinearSlicePlugin().LegacyClassName,
				Is.EqualTo("SIL.FieldWorks.XWorks.MorphologyEditor.InterlinearSlice"));
			Assert.That(RegionEditorPluginRegistry.Default.Resolve(InterlinearSlicePlugin.InterlinearSliceClassName),
				Is.InstanceOf<InterlinearSlicePlugin>(), "registered in the default builtins");
		}

		[Test]
		public void BuildControl_OverAPopulatedAnalysis_RealizesTheInterlinearEditor()
		{
			var control = new InterlinearSlicePlugin().BuildControl(Ctx(m_analysis, InterlinearNode()));
			Assert.That(control, Is.InstanceOf<InterlinearRegionEditor>(),
				"a populated analysis realizes the aligned interlinear control, not the unsupported row");
		}

		[Test]
		public void BuildControl_OverAnEmptyAnalysis_StillRealizesTheBareWordform()
		{
			IWfiAnalysis empty = null;
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
			{
				empty = Cache.ServiceLocator.GetInstance<IWfiAnalysisFactory>().Create();
				m_wordform.AnalysesOC.Add(empty);
			});

			var control = new InterlinearSlicePlugin().BuildControl(Ctx(empty, InterlinearNode()));
			Assert.That(control, Is.InstanceOf<InterlinearRegionEditor>(),
				"an empty analysis still renders (bare wordform), never null/unsupported");
		}

		[Test]
		public void BuildControl_WrongTargetType_DegradesToNull()
		{
			// A non-analysis target (defensive): the plugin returns null so the view's guard renders the
			// unsupported row rather than the pane crashing.
			Assert.That(new InterlinearSlicePlugin().BuildControl(Ctx(m_wordform, InterlinearNode())), Is.Null);
		}

		[Test]
		public void Compose_AnalysesSurface_RendersTheInterlinearAsACustomRow_NotUnsupported()
		{
			var composed = FullEntryRegionComposer.Compose(m_analysis, Cache);

			var customRows = composed.Model.Fields.Where(f => f.Kind == RegionFieldKind.Custom).ToList();
			Assert.That(customRows, Is.Not.Empty, "the InterlinearAnal slice composes as a Custom plugin row");
			var interlinear = customRows.Single(f => f.ControlFactory != null);
			Assert.That(interlinear.ObjectHvo, Is.EqualTo(m_analysis.Hvo));

			// The interlinear node must NOT have fallen through to the unsupported lane (spec scenario).
			Assert.That(composed.CustomEditorFields.Select(f => f.ClassName),
				Has.No.Member(InterlinearSlicePlugin.InterlinearSliceClassName),
				"a plugin-claimed class never reaches the companion/unsupported lane (D1 resolution order)");

			// The factory realizes the editor.
			Assert.That(interlinear.ControlFactory(), Is.InstanceOf<InterlinearRegionEditor>());
		}

		[Test]
		public void BuildControl_ApprovedAnalysis_ComposesEditable_ExercisingMorphCandidateLookup()
		{
			// Approving the analysis turns on the editable choosers (sense / MSA / morph). Building the
			// control with a real fenced host runs BuildBundleChoices end-to-end — including the morph
			// candidate lookup (MorphServices.GetMatchingMorphs over the real cache) — proving the (a)
			// morph/entry re-pointing path composes without throwing and the control is editable.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor,
				() => m_analysis.SetAgentOpinion(Cache.LangProject.DefaultUserAgent, Opinions.approves));

			var host = FullEntryRegionComposer.Compose(m_analysis, Cache).EditContext;
			var ctx = new RegionEditorBuildContext(m_analysis, InterlinearNode(), () => host, Cache);
			var control = new InterlinearSlicePlugin().BuildControl(ctx) as InterlinearRegionEditor;

			Assert.That(control, Is.Not.Null);
			Assert.That(control.IsEditable, Is.True,
				"an approved analysis composes editable sense/MSA/morph choosers");
		}

		[Test]
		public void Compose_WordformRooted_DescendsIntoAnalyses_AndRendersTheInterlinear()
		{
			// SCENARIO-LEVEL T2 (spec): the Analyses tool roots the detail pane on a WfiWordform; the
			// composer must descend the analyses sequence (WFI.fwlayout: WfiWordform Normal → analyses
			// sections → per-WfiAnalysis layout → InterlinearAnal/InterlinearParse) and reach the plugin.
			// This proves W-1 (wordform nested-tree compose) — without it the interlinear is unreachable
			// in the product even though the analysis-rooted compose above is green.
			NonUndoableUnitOfWorkHelper.Do(Cache.ActionHandlerAccessor, () =>
				m_analysis.SetAgentOpinion(Cache.LangProject.DefaultUserAgent, Opinions.approves));
			Assert.That(m_wordform.HumanApprovedAnalyses.Contains(m_analysis), Is.True,
				"the analysis is now an approved analysis of the wordform");

			var composed = FullEntryRegionComposer.Compose(m_wordform, Cache);

			var interlinearRows = composed.Model.Fields
				.Where(f => f.Kind == RegionFieldKind.Custom && f.ControlFactory != null
					&& f.ObjectHvo == m_analysis.Hvo)
				.ToList();
			Assert.That(interlinearRows, Is.Not.Empty,
				"composing the WORDFORM must descend into its analyses and render the interlinear plugin row "
				+ "(W-1 nested compose); if this is empty the analyses-sequence flid did not resolve/descend");
			Assert.That(interlinearRows[0].ControlFactory(), Is.InstanceOf<InterlinearRegionEditor>());
		}
	}
}
