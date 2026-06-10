// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Graphite;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// graphite-transition-support tasks 1.3/1.4: font-table evidence and the deterministic G0–G3
	/// classification, validated against the shipped fixture fonts (Charis SIL = dual-engine,
	/// piglatin/stakdiac = Graphite-only, SILDUb3 = neither).
	/// </summary>
	[TestFixture]
	public class GraphiteClassificationTests
	{
		private static string RepoRoot()
		{
			var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
			while (dir != null && !File.Exists(Path.Combine(dir.FullName, "FieldWorks.sln")))
				dir = dir.Parent;
			Assert.That(dir, Is.Not.Null, "could not locate the repo root");
			return dir.FullName;
		}

		private static GraphiteFontTableEvidence Sniff(params string[] relativePath)
			=> FontTableSniffer.FromFile(Path.Combine(new[] { RepoRoot() }.Concat(relativePath).ToArray()));

		[Test]
		public void Sniffer_CharisSil_IsDualEngine()
		{
			var evidence = Sniff("Src", "views", "Test", "TestData", "Fonts", "CharisSIL-5.000", "CharisSIL-R.ttf");
			Assert.That(evidence.HasGraphiteTables, Is.True, "Charis SIL 5.0 carries Silf");
			Assert.That(evidence.HasOpenTypeShapingTables, Is.True, "Charis SIL 5.0 carries GSUB/GPOS");
		}

		[Test]
		public void Sniffer_PigLatin_IsGraphiteOnly()
		{
			var evidence = Sniff("DistFiles", "Graphite", "pl", "piglatin.ttf");
			Assert.That(evidence.HasGraphiteTables, Is.True);
			Assert.That(evidence.HasOpenTypeShapingTables, Is.False, "the Graphite sample font has no OpenType shaping");
		}

		[Test]
		public void Sniffer_PlainFont_HasNeither()
		{
			var evidence = Sniff("DistFiles", "Graphite", "ipa", "SILDUb3.TTF");
			Assert.That(evidence.HasGraphiteTables, Is.False);
			Assert.That(evidence.HasOpenTypeShapingTables, Is.False);
		}

		[Test]
		public void Sniffer_GarbageAndMissingInput_YieldsUnknown_NeverThrows()
		{
			Assert.That(FontTableSniffer.FromBytes(null), Is.SameAs(GraphiteFontTableEvidence.Unknown));
			Assert.That(FontTableSniffer.FromBytes(new byte[] { 1, 2, 3 }), Is.SameAs(GraphiteFontTableEvidence.Unknown));
			Assert.That(FontTableSniffer.FromFile(Path.Combine(Path.GetTempPath(), "no-such-font.ttf")).HasGraphiteTables, Is.False);
			Assert.That(FontTableSniffer.FromInstalledFamily("No Such Font Family 42"), Is.SameAs(GraphiteFontTableEvidence.Unknown));
		}

		private static readonly GraphiteFontTableEvidence DualEngine = new GraphiteFontTableEvidence(true, true);
		private static readonly GraphiteFontTableEvidence GraphiteOnly = new GraphiteFontTableEvidence(true, false);
		private static readonly GraphiteFontTableEvidence NoGraphite = new GraphiteFontTableEvidence(false, true);

		[Test]
		public void Classifier_FlagWithoutGraphiteTables_IsG0()
		{
			var c = GraphiteWsClassifier.Classify(true, "lang=ipa", NoGraphite, "en", "English", "Arial");
			Assert.That(c.Tier, Is.EqualTo(GraphiteTier.G0));
			Assert.That(c.Message, Is.Empty, "G0 produces no user-facing message");
		}

		[Test]
		public void Classifier_FlagDisabled_IsG0_EvenForGraphiteOnlyFont()
		{
			// Legacy would not Graphite-shape it either, so rendering is equivalent.
			var c = GraphiteWsClassifier.Classify(false, null, GraphiteOnly, "xx", "Test WS", "piglatin");
			Assert.That(c.Tier, Is.EqualTo(GraphiteTier.G0));
		}

		[Test]
		public void Classifier_DualEngineNoFeatures_IsG1()
		{
			var c = GraphiteWsClassifier.Classify(true, "", DualEngine, "seh", "Sena", "Charis SIL");
			Assert.That(c.Tier, Is.EqualTo(GraphiteTier.G1));
			Assert.That(c.Message, Does.Contain("Sena").And.Contain("Charis SIL"));
		}

		[Test]
		public void Classifier_DualEngineWithFeatures_IsG2()
		{
			var c = GraphiteWsClassifier.Classify(true, "litr=1", DualEngine, "seh", "Sena", "Charis SIL");
			Assert.That(c.Tier, Is.EqualTo(GraphiteTier.G2));
			Assert.That(c.DiagnosticCode, Is.EqualTo("graphite-g2"));
		}

		[Test]
		public void Classifier_GraphiteOnlyFont_IsG3_RegardlessOfFeatures()
		{
			var c = GraphiteWsClassifier.Classify(true, null, GraphiteOnly, "ur", "Urdu", "Awami Nastaliq");
			Assert.That(c.Tier, Is.EqualTo(GraphiteTier.G3));
			Assert.That(c.Message, Does.Contain("Urdu").And.Contain("Awami Nastaliq"));
		}

		[Test]
		public void Classifier_IsDeterministic()
		{
			GraphiteWsClassification Run() => GraphiteWsClassifier.Classify(true, "f=1", DualEngine, "a", "A", "F");
			Assert.That(Run().Tier, Is.EqualTo(Run().Tier));
			Assert.That(Run().Message, Is.EqualTo(Run().Message));
			Assert.That(Run().DiagnosticCode, Is.EqualTo(Run().DiagnosticCode));
		}

		[Test]
		public void EndToEnd_ShippedFixtureFonts_ClassifyAsDesigned()
		{
			var charis = Sniff("Src", "views", "Test", "TestData", "Fonts", "CharisSIL-5.000", "CharisSIL-R.ttf");
			var piglatin = Sniff("DistFiles", "Graphite", "pl", "piglatin.ttf");

			Assert.That(GraphiteWsClassifier.Classify(true, null, charis, "x", "X", "Charis SIL").Tier, Is.EqualTo(GraphiteTier.G1));
			Assert.That(GraphiteWsClassifier.Classify(true, "wsys=1", charis, "x", "X", "Charis SIL").Tier, Is.EqualTo(GraphiteTier.G2));
			Assert.That(GraphiteWsClassifier.Classify(true, null, piglatin, "x", "X", "Pig Latin").Tier, Is.EqualTo(GraphiteTier.G3));
		}
	}

	/// <summary>
	/// graphite-transition-support tasks 2.1/2.3: presentation grading and per-session rate limiting.
	/// </summary>
	[TestFixture]
	public class GraphiteWarningPolicyTests
	{
		private static GraphiteWsClassification Make(GraphiteTier tier, string wsId = "seh")
			=> new GraphiteWsClassification(tier, wsId, "WS " + wsId, "Font", "message for " + wsId);

		[Test]
		public void G0_IsNeverPresented()
			=> Assert.That(new GraphiteWarningPolicy().Decide(Make(GraphiteTier.G0)), Is.EqualTo(GraphiteWarningPresentation.None));

		[Test]
		public void G1_IsLogOnly_AndNeverBecomesAPopup()
		{
			var policy = new GraphiteWarningPolicy();
			Assert.That(policy.Decide(Make(GraphiteTier.G1)), Is.EqualTo(GraphiteWarningPresentation.LogOnly));
			Assert.That(policy.Decide(Make(GraphiteTier.G1)), Is.EqualTo(GraphiteWarningPresentation.LogOnly));
		}

		[Test]
		public void G2_PresentsOncePerWritingSystemPerSession()
		{
			var policy = new GraphiteWarningPolicy();
			Assert.That(policy.Decide(Make(GraphiteTier.G2)), Is.EqualTo(GraphiteWarningPresentation.Warning));
			Assert.That(policy.Decide(Make(GraphiteTier.G2)), Is.EqualTo(GraphiteWarningPresentation.None), "rate-limited within the session");
			Assert.That(policy.Decide(Make(GraphiteTier.G2, "other")), Is.EqualTo(GraphiteWarningPresentation.Warning), "per writing system");
		}

		[Test]
		public void G3_IsProminent_AndReappearsInANewSession()
		{
			var session1 = new GraphiteWarningPolicy();
			Assert.That(session1.Decide(Make(GraphiteTier.G3)), Is.EqualTo(GraphiteWarningPresentation.ProminentWarning));
			Assert.That(session1.Decide(Make(GraphiteTier.G3)), Is.EqualTo(GraphiteWarningPresentation.None));

			var session2 = new GraphiteWarningPolicy();
			Assert.That(session2.Decide(Make(GraphiteTier.G3)), Is.EqualTo(GraphiteWarningPresentation.ProminentWarning),
				"a G3 condition is never permanently suppressed — each session presents it again while it holds");
		}
	}

	/// <summary>
	/// graphite-transition-support tasks 2.1/2.3: the banner renders with stable automation ids and
	/// the whole-surface switch-to-legacy affordance fires the supplied callback.
	/// </summary>
	[TestFixture]
	public class GraphiteWarningBannerTests
	{
		private static LexicalEditRegionModel EmptyRegion()
			=> new LexicalEditRegionModel("LexEntry", "identity", new List<LexicalEditRegionField>(), new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void Banner_RendersMessageAndAffordance_WithStableAutomationIds()
		{
			var warning = new GraphiteWsClassification(GraphiteTier.G3, "ur", "Urdu", "Awami Nastaliq", "Urdu uses a Graphite-only font.");
			var switched = 0;
			var view = new LexicalEditRegionView(EmptyRegion(), new[] { warning }, () => switched++);
			var window = new Window { Content = view, Width = 500, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var banner = view.GetVisualDescendants().OfType<Border>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "GraphiteWarningBanner.ur");
			Assert.That(banner, Is.Not.Null, "the warning banner must render with a stable per-ws automation id");

			var message = view.GetVisualDescendants().OfType<TextBlock>()
				.FirstOrDefault(t => AutomationProperties.GetAutomationId(t) == "GraphiteWarningBanner.ur.Message");
			Assert.That(message?.Text, Does.Contain("Graphite-only"));

			var button = view.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "GraphiteWarningBanner.ur.SwitchToLegacy");
			Assert.That(button, Is.Not.Null, "the switch-to-legacy affordance must be present");

			button.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
			Dispatcher.UIThread.RunJobs();
			Assert.That(switched, Is.EqualTo(1), "the affordance invokes the whole-surface legacy switch");
		}

		[AvaloniaTest]
		public void NoWarnings_RendersNoBanner()
		{
			var view = new LexicalEditRegionView(EmptyRegion());
			var window = new Window { Content = view, Width = 500, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(view.GetVisualDescendants().OfType<Border>()
					.Any(b => (AutomationProperties.GetAutomationId(b) ?? "").StartsWith("GraphiteWarningBanner")),
				Is.False);
		}
	}
}
