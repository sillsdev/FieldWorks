// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Headless.NUnit;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Detail;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// A fake value provider so the mapper can be tested without LCModel. The LCModel-backed provider
	/// lives in xWorks (<c>LexiconEditErrorFallback</c>).
	/// </summary>
	internal sealed class FakeDetailValueProvider : IDetailValueProvider
	{
		public IReadOnlyList<DetailWsValue> GetValues(ViewNode fieldNode)
		{
			switch (fieldNode.Field)
			{
				case "LexemeForm":
					return new List<DetailWsValue> { new DetailWsValue("vern", "dog", "Charis SIL", 12) };
				case "Gloss":
					return new List<DetailWsValue> { new DetailWsValue("anal", "canine") };
				default:
					return new List<DetailWsValue>();
			}
		}

		public IReadOnlyList<DetailChoiceOption> GetOptions(ViewNode fieldNode)
			=> new List<DetailChoiceOption> { new DetailChoiceOption("stem", "stem"), new DetailChoiceOption("suffix", "suffix") };

		public string GetSelectedOptionKey(ViewNode fieldNode) => "suffix";
	}

	[TestFixture]
	public class RegionModelProjectorTests
	{
		private static ViewDefinitionModel SampleDefinition()
		{
			var roots = new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "LexemeForm", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Morph Type", null, "MorphType", "morphtypeatomicreference",
					EditorClassification.Known, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "MorphTypeChooser", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#2", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "SenseGlossEditor", routing: SurfaceRouting.Product)
			};
			return new ViewDefinitionModel("LexEntry", "identity", "detail", roots, new List<ViewDiagnostic>());
		}

		[Test]
		public void FromViewDefinition_ProjectsFields_FromTheTypedDefinition()
		{
			var model = DetailModelProjector.FromViewDefinition(SampleDefinition(), new FakeDetailValueProvider());

			Assert.That(model.ClassName, Is.EqualTo("LexEntry"));
			Assert.That(model.Fields.Select(f => f.Field), Is.EqualTo(new[] { "LexemeForm", "MorphType", "Gloss" }));
		}

		[Test]
		public void TextFields_AreClassifiedAsText_AndBoundToValues()
		{
			var model = DetailModelProjector.FromViewDefinition(SampleDefinition(), new FakeDetailValueProvider());
			var lexeme = model.Fields.Single(f => f.Field == "LexemeForm");

			Assert.That(lexeme.Kind, Is.EqualTo(DetailFieldKind.Text));
			Assert.That(lexeme.Values.Single().Value, Is.EqualTo("dog"));
			Assert.That(lexeme.AutomationId, Is.EqualTo("LexemeFormEditor"));
		}

		private sealed class RichDetailValueProvider : IDetailValueProvider
		{
			public IReadOnlyList<DetailWsValue> GetValues(ViewNode fieldNode)
				=> new List<DetailWsValue>
				{
					new DetailWsValue("vern", "dog", richText: new DetailRichTextValue(
						"dog",
						new List<DetailTextRun>
						{
							new DetailTextRun("do", "qaa-x-one"),
							new DetailTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
						},
						richXml: "<AStr ws='qaa-x-one'><Run ws='qaa-x-one'>do</Run><Run ws='qaa-x-two' namedStyle='Emphasis'>g</Run></AStr>",
						requiresRichEditor: true))
				};

			public IReadOnlyList<DetailChoiceOption> GetOptions(ViewNode fieldNode) => new List<DetailChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		private sealed class UnsupportedRichDetailValueProvider : IDetailValueProvider
		{
			public IReadOnlyList<DetailWsValue> GetValues(ViewNode fieldNode)
				=> new List<DetailWsValue>
				{
					new DetailWsValue("vern", "link", richText: new DetailRichTextValue(
						"link",
						new List<DetailTextRun>
						{
							new DetailTextRun("link", "qaa-x-one", objectData: "\uF8FFhttps://software.sil.org")
						},
						richXml: "<AStr ws='qaa-x-one'><Run ws='qaa-x-one' objData='x'>link</Run></AStr>",
						requiresRichEditor: true,
						canEditRichText: false))
				};

			public IReadOnlyList<DetailChoiceOption> GetOptions(ViewNode fieldNode) => new List<DetailChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		[Test]
		public void RichTextFields_AreProjectedEditable_WhenRichRowsCanRoundTrip()
		{
			var model = DetailModelProjector.FromViewDefinition(SampleDefinition(), new RichDetailValueProvider());
			var lexeme = model.Fields.Single(f => f.Field == "LexemeForm");

			Assert.That(lexeme.IsEditable, Is.True,
				"rows carrying rich-text runs stay editable when the value advertises rich edit support");
			Assert.That(lexeme.Values.Single().RichText, Is.Not.Null);
			Assert.That(lexeme.Values.Single().RichText.Runs.Select(r => r.WritingSystemTag),
				Is.EqualTo(new[] { "qaa-x-one", "qaa-x-two" }));
		}

		[Test]
		public void RichTextFields_WithUnsupportedObjectData_AreProjectedReadOnly()
		{
			var model = DetailModelProjector.FromViewDefinition(SampleDefinition(),
				new UnsupportedRichDetailValueProvider());
			var lexeme = model.Fields.Single(f => f.Field == "LexemeForm");

			Assert.That(lexeme.IsEditable, Is.False,
				"rows with unsupported object-data runs must stay read-only until the owner task lands");
			Assert.That(lexeme.Values.Single().CanEditRichText, Is.False);
		}

		[Test]
		public void RichTextEditAlgorithm_NoOpEdit_ReturnsOriginalInstance()
		{
			var original = DetailRichTextEditAlgorithms.FromRuns("dog", new[]
			{
				new DetailTextRun("do", "qaa-x-one"),
				new DetailTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
			});

			var result = DetailRichTextEditAlgorithms.ApplyPlainTextEdit(original, "dog");
			Assert.That(result, Is.SameAs(original),
				"a no-op edit should keep the exact rich payload so save-without-changes preserves runs");
		}

		[Test]
		public void ChooserField_IsClassifiedAsChooser_WithOptionsAndSelection()
		{
			var model = DetailModelProjector.FromViewDefinition(SampleDefinition(), new FakeDetailValueProvider());
			var morph = model.Fields.Single(f => f.Field == "MorphType");

			Assert.That(morph.Kind, Is.EqualTo(DetailFieldKind.Chooser));
			Assert.That(morph.Options.Select(o => o.Key), Is.EqualTo(new[] { "stem", "suffix" }));
			Assert.That(morph.SelectedOptionKey, Is.EqualTo("suffix"));
		}

		[Test]
		public void NeverVisibleFields_AreExcluded()
		{
			var roots = new List<ViewNode>
			{
				new ViewNode("x/#0", ViewNodeKind.Field, "Hidden", null, "Hidden", "multistring",
					EditorClassification.Known, null, ViewVisibility.Never, ViewExpansion.NotApplicable, false, null, null)
			};
			var def = new ViewDefinitionModel("LexEntry", "identity", "detail", roots, new List<ViewDiagnostic>());

			var model = DetailModelProjector.FromViewDefinition(def, new FakeDetailValueProvider());
			Assert.That(model.Fields, Is.Empty);
		}

		[Test]
		public void ObsoleteEditor_IsClassifiedUnsupported()
		{
			var roots = new List<ViewNode>
			{
				new ViewNode("x/#0", ViewNodeKind.Field, "Old", null, "Old", "message",
					EditorClassification.Obsolete, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null)
			};
			var def = new ViewDefinitionModel("LexEntry", "identity", "detail", roots, new List<ViewDiagnostic>());

			var model = DetailModelProjector.FromViewDefinition(def, new FakeDetailValueProvider());
			Assert.That(model.Fields.Single().Kind, Is.EqualTo(DetailFieldKind.Unsupported));
		}

		[Test]
		public void Diagnostics_ArePreserved_FromTheDefinition()
		{
			var diags = new List<ViewDiagnostic> { new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "x", "m", "p") };
			var def = new ViewDefinitionModel("LexEntry", "identity", "detail", new List<ViewNode>(), diags);

			var model = DetailModelProjector.FromViewDefinition(def, new FakeDetailValueProvider());
			Assert.That(model.Diagnostics, Has.Count.EqualTo(1));
		}

		[Test]
		public void GraphemeClusters_KhmerSyllable_IsOneUserVisibleCluster()
		{
			var starts = DetailTextGraphemeClusters.GetClusterStarts("កាx");

			Assert.That(starts, Is.EqualTo(new[] { 0, 2 }),
				"Khmer base+vowel stays one grapheme cluster; the following Latin character starts a new cluster");
		}

		[Test]
		public void GraphemeClusters_CombiningMarkSequence_IsOneUserVisibleCluster()
		{
			var starts = DetailTextGraphemeClusters.GetClusterStarts("a\u0301b");

			Assert.That(starts, Is.EqualTo(new[] { 0, 2 }),
				"Latin base plus combining acute stays one cluster; the trailing letter starts the next cluster");
		}

		[Test]
		public void GraphemeClusters_SurrogatePairEmoji_IsOneUserVisibleCluster()
		{
			var starts = DetailTextGraphemeClusters.GetClusterStarts("\U0001F600x");

			Assert.That(starts, Is.EqualTo(new[] { 0, 2 }),
				"A surrogate-pair emoji stays one cluster; the following Latin character starts a new cluster");
		}

		[Test]
		public void GraphemeClusters_ZwjFamilySequence_IsOneUserVisibleCluster()
		{
			var starts = DetailTextGraphemeClusters.GetClusterStarts("\U0001F468\u200D\U0001F469\u200D\U0001F467z");

			Assert.That(starts, Is.EqualTo(new[] { 0, 8 }),
				"A ZWJ family sequence is one cluster; the following Latin character starts the next cluster");
		}

		[Test]
		public void ImeCompositionState_ComposeCancelCommit_LeavesCommittedTextUntouchedUntilCommit()
		{
			var ime = new DetailImeCompositionState("hello world");
			const string thaiGa = "\u0E01\u0E32";

			ime.Begin(6, 11, thaiGa);
			Assert.That(ime.IsActive, Is.True);
			Assert.That(ime.CommittedText, Is.EqualTo("hello world"),
				"composition is editor-local and must not mutate committed text until commit");
			Assert.That(ime.DisplayText, Is.EqualTo("hello " + thaiGa));

			var canceled = ime.Cancel();
			Assert.That(canceled, Is.EqualTo("hello world"));
			Assert.That(ime.IsActive, Is.False);

			ime.Begin(6, 11, thaiGa);
			var committed = ime.Commit();
			Assert.That(committed, Is.EqualTo("hello " + thaiGa));
			Assert.That(ime.IsActive, Is.False);
		}

		[Test]
		public void ImeCompositionState_Backspace_DeletesWithinActiveCompositionOnly()
		{
			var ime = new DetailImeCompositionState("cat");
			ime.Begin(3, 3, "a\u0301b");

			var afterBackspace = ime.Backspace();
			Assert.That(afterBackspace, Is.EqualTo("cata\u0301"),
				"Backspace removes the last grapheme in composition text before touching committed text");
			Assert.That(ime.CommittedText, Is.EqualTo("cat"));

			afterBackspace = ime.Backspace();
			Assert.That(afterBackspace, Is.EqualTo("cat"));
			Assert.That(ime.CommittedText, Is.EqualTo("cat"));
		}

		[Test]
		public void RichTextEditAlgorithm_InsertAtRunBoundary_PreservesNeighborRunMetadata()
		{
			var original = DetailRichTextEditAlgorithms.FromRuns("abc\u05d0\u05d1\u05d2", new[]
			{
				new DetailTextRun("abc", "qaa-x-left", namedStyle: "LeftStyle"),
				new DetailTextRun("\u05d0\u05d1\u05d2", "qaa-x-rtl", namedStyle: "RtlStyle")
			});

			var edited = DetailRichTextEditAlgorithms.ApplyPlainTextEdit(original, "abcX\u05d0\u05d1\u05d2");

			Assert.That(edited.Runs.Select(r => r.Text), Is.EqualTo(new[] { "abcX", "\u05d0\u05d1\u05d2" }));
			Assert.That(edited.Runs[0].NamedStyle, Is.EqualTo("LeftStyle"));
			Assert.That(edited.Runs[1].NamedStyle, Is.EqualTo("RtlStyle"),
				"inserts at run boundaries must not leak style metadata across the boundary");
		}

		[Test]
		public void BidirectionalCaretNavigation_MapsArrowKeysThroughActiveRunDirection()
		{
			const string mixed = "abc \u05d0\u05d1\u05d2 xyz";
			var rich = DetailRichTextEditAlgorithms.FromRuns(mixed, new[]
			{
				new DetailTextRun("abc ", "qaa-x-left"),
				new DetailTextRun("\u05d0\u05d1\u05d2", "qaa-x-rtl"),
				new DetailTextRun(" xyz", "qaa-x-left")
			});

			var insideRtl = 5;
			var afterLeft = DetailBidirectionalTextNavigation.MoveCaret(mixed, rich.Runs, insideRtl,
				physicalLeft: true, defaultRightToLeft: true);
			Assert.That(afterLeft, Is.EqualTo(6),
				"inside RTL run, Left arrow advances logically");

			var afterRight = DetailBidirectionalTextNavigation.MoveCaret(mixed, rich.Runs, afterLeft,
				physicalLeft: false, defaultRightToLeft: true);
			Assert.That(afterRight, Is.EqualTo(5),
				"inside RTL run, Right arrow moves logically backward");
		}

		[Test]
		public void SelectionAndHitTest_NormalizeToWholeGraphemeClusters()
		{
			const string text = "a\U0001F469\u200D\U0001F467b";

			var normalizedRange = DetailBidirectionalTextNavigation.NormalizeSelectionToClusters(text, 2, 4);
			Assert.That(normalizedRange.Start, Is.EqualTo(1));
			Assert.That(normalizedRange.End, Is.EqualTo(6),
				"selection covering part of a ZWJ cluster expands to whole user-visible character");

			var normalizedCaret = DetailBidirectionalTextNavigation.NormalizeHitTestCaretIndex(text, 3);
			Assert.That(normalizedCaret, Is.EqualTo(1),
				"hit-test caret in the middle of a grapheme snaps to cluster start");
		}
	}

	[TestFixture]
	public class RegionDataTreeTests
	{
		private static ViewDefinitionModel SampleDefinition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "LexemeForm", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Morph Type", null, "MorphType", "morphtypeatomicreference",
					EditorClassification.Known, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "MorphTypeChooser", routing: SurfaceRouting.Product)
			},
			new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void RegionView_RendersFields_WithStableAutomationIds()
		{
			var model = DetailModelProjector.FromViewDefinition(SampleDefinition(), new FakeDetailValueProvider());
			var view = new DataTree(model);
			var window = new Window { Content = view, Width = 420, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(AutomationProperties.GetAutomationId(view), Is.EqualTo("RegionDataTree"));

			var lexemeBox = view.GetVisualDescendants().OfType<TextBox>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "LexemeFormEditor.vern");
			Assert.That(lexemeBox, Is.Not.Null, "the text field should render a per-ws box with a stable automation id");
			Assert.That(lexemeBox.Text, Is.EqualTo("dog"));

			var chooser = view.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "MorphTypeChooser");
			Assert.That(chooser, Is.Not.Null, "the chooser field should render the owned flyout chooser");
		}
	}

	/// <summary>
	/// Pure: <see cref="DetailRichTextEditAlgorithms.ApplySpanFormatting"/> splits runs at the
	/// selection boundaries and sets the chosen attribute only on covered runs, leaving the rest of the
	/// value's run metadata untouched — across run boundaries, partial runs, grapheme clusters, and the
	/// lossy read-only guard.
	/// </summary>
	[TestFixture]
	public class RegionSpanFormattingTests
	{
		private static DetailRichTextValue TwoRunDog() => DetailRichTextEditAlgorithms.FromRuns("dog", new[]
		{
			new DetailTextRun("do", "qaa-x-one"),
			new DetailTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
		});

		// Selection fully inside the FIRST run: the run splits into bold "do"-prefix... here the whole
		// first run is covered, so it becomes one bold run; the styled trailing run is untouched.
		[Test]
		public void ApplySpanFormatting_CoveringFirstRun_BoldsOnlyThatRun()
		{
			var result = DetailRichTextEditAlgorithms.ApplySpanFormatting(TwoRunDog(), 0, 2,
				DetailRunFormat.Bold, true);

			Assert.That(result.PlainText, Is.EqualTo("dog"), "plain text is never changed");
			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "do", "g" }));
			Assert.That(result.Runs[0].Bold, Is.True, "the covered run gets bold");
			Assert.That(result.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"), "other metadata is preserved");
			Assert.That(result.Runs[1].Bold, Is.False, "the uncovered run is untouched");
			Assert.That(result.Runs[1].NamedStyle, Is.EqualTo("Emphasis"), "uncovered run keeps its style");
			Assert.That(result.RichXml, Is.Null, "no RichXml so ToTsString takes the run-replay path");
		}

		// A PARTIAL-run selection splits that run: "d" stays plain, "o" goes bold, "g" untouched.
		[Test]
		public void ApplySpanFormatting_PartialRun_SplitsAndBoldsOnlyTheCoveredSlice()
		{
			var result = DetailRichTextEditAlgorithms.ApplySpanFormatting(TwoRunDog(), 1, 2,
				DetailRunFormat.Bold, true);

			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }),
				"the first run splits at the selection boundary");
			Assert.That(result.Runs[0].Bold, Is.False);
			Assert.That(result.Runs[1].Bold, Is.True, "only the covered slice is bold");
			Assert.That(result.Runs[1].WritingSystemTag, Is.EqualTo("qaa-x-one"),
				"the split slice inherits its source run's metadata");
			Assert.That(result.Runs[2].Bold, Is.False);
		}

		// A selection that SPANS a run boundary bolds across both runs, splitting each as needed.
		[Test]
		public void ApplySpanFormatting_AcrossRunBoundary_BoldsBothCoveredSlices()
		{
			var result = DetailRichTextEditAlgorithms.ApplySpanFormatting(TwoRunDog(), 1, 3,
				DetailRunFormat.Bold, true);

			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }));
			Assert.That(result.Runs[0].Bold, Is.False, "the leading slice outside the span stays plain");
			Assert.That(result.Runs[1].Bold, Is.True, "the tail of run 1 inside the span is bold");
			Assert.That(result.Runs[2].Bold, Is.True, "run 2 (fully covered) is bold");
			Assert.That(result.Runs[2].NamedStyle, Is.EqualTo("Emphasis"),
				"the bolded run keeps its other metadata");
		}

		[Test]
		public void ApplySpanFormatting_Italic_And_Underline_SetTheCorrectAttribute()
		{
			var italic = DetailRichTextEditAlgorithms.ApplySpanFormatting(TwoRunDog(), 0, 2,
				DetailRunFormat.Italic, true);
			Assert.That(italic.Runs[0].Italic, Is.True);
			Assert.That(italic.Runs[0].Bold, Is.False);
			Assert.That(italic.Runs[0].Underline, Is.False);

			var underline = DetailRichTextEditAlgorithms.ApplySpanFormatting(TwoRunDog(), 0, 2,
				DetailRunFormat.Underline, true);
			Assert.That(underline.Runs[0].Underline, Is.True);
			Assert.That(underline.Runs[0].Bold, Is.False);
		}

		[Test]
		public void ApplySpanFormatting_TogglingOff_ClearsTheAttribute()
		{
			var bolded = DetailRichTextEditAlgorithms.ApplySpanFormatting(TwoRunDog(), 1, 2,
				DetailRunFormat.Bold, true);
			Assert.That(bolded.Runs.First(r => r.Text == "o").Bold, Is.True);

			var cleared = DetailRichTextEditAlgorithms.ApplySpanFormatting(bolded, 1, 2,
				DetailRunFormat.Bold, false);
			Assert.That(cleared.Runs.Any(r => r.Bold), Is.False, "the attribute is cleared over the span");
			Assert.That(cleared.PlainText, Is.EqualTo("dog"));
		}

		[Test]
		public void ApplySpanFormatting_ZeroLengthSelection_IsNoOp()
		{
			var original = TwoRunDog();
			var result = DetailRichTextEditAlgorithms.ApplySpanFormatting(original, 1, 1,
				DetailRunFormat.Bold, true);
			Assert.That(result, Is.SameAs(original), "a collapsed selection is a no-op");
		}

		// At the pure layer: a lossy value is read-only and is returned unchanged.
		[Test]
		public void ApplySpanFormatting_LossyValue_ReturnsUnchanged()
		{
			var lossy = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "qaa-x-one") },
				richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true);
			Assert.That(lossy.CanEditRichText, Is.False);

			var result = DetailRichTextEditAlgorithms.ApplySpanFormatting(lossy, 0, 4,
				DetailRunFormat.Bold, true);
			Assert.That(result, Is.SameAs(lossy), "a lossy/read-only value is never reformatted");
		}

		// Grapheme-cluster safety: a selection whose boundaries fall inside a combining cluster snaps
		// OUTWARD so the cluster is never split (matching the bidi navigation's boundary logic).
		[Test]
		public void ApplySpanFormatting_RespectsGraphemeClusterBoundaries()
		{
			// "a" + (e + combining acute U+0301) + "b": indices 0='a',1='e',2=U+0301,3='b'.
			const string text = "aéb";
			var value = DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, "qaa-x-one") });

			// Selecting [1,2) lands inside the e-acute cluster; it must snap out to cover the whole cluster.
			var result = DetailRichTextEditAlgorithms.ApplySpanFormatting(value, 1, 2,
				DetailRunFormat.Bold, true);

			var boldText = string.Concat(result.Runs.Where(r => r.Bold).Select(r => r.Text));
			Assert.That(boldText, Is.EqualTo("é"),
				"the combining cluster is bolded whole, never split mid-character");
		}

		[Test]
		public void SpanFullyHasFormat_ReportsWhetherTheWholeSpanCarriesTheAttribute()
		{
			var bolded = DetailRichTextEditAlgorithms.ApplySpanFormatting(TwoRunDog(), 0, 2,
				DetailRunFormat.Bold, true);

			Assert.That(DetailRichTextEditAlgorithms.SpanFullyHasFormat(bolded, 0, 2, DetailRunFormat.Bold),
				Is.True, "the fully-bolded span reports all-on (so the UI toggles off next)");
			Assert.That(DetailRichTextEditAlgorithms.SpanFullyHasFormat(bolded, 0, 3, DetailRunFormat.Bold),
				Is.False, "extending into the plain tail is not all-on");
			Assert.That(DetailRichTextEditAlgorithms.SpanFullyHasFormat(bolded, 1, 1, DetailRunFormat.Bold),
				Is.False, "a collapsed span has nothing to toggle off");
		}
	}

	/// <summary>
	/// Pure: <see cref="DetailRichTextEditAlgorithms.ApplySpanNamedStyle"/> splits runs at the
	/// selection boundaries and sets/clears the named character style only on covered runs, cluster-safe,
	/// honoring the lossy read-only guard; <see cref="DetailRichTextEditAlgorithms.SpanNamedStyle"/>
	/// reports the common style across the span (null when mixed/none).
	/// </summary>
	[TestFixture]
	public class RegionSpanNamedStyleTests
	{
		// "do" (plain) + "g" (Emphasis) — a run boundary at index 2.
		private static DetailRichTextValue TwoRunDog() => DetailRichTextEditAlgorithms.FromRuns("dog", new[]
		{
			new DetailTextRun("do", "qaa-x-one"),
			new DetailTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
		});

		[Test]
		public void ApplySpanNamedStyle_CoveringFirstRun_StylesOnlyThatRun()
		{
			var result = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(TwoRunDog(), 0, 2, "Strong");

			Assert.That(result.PlainText, Is.EqualTo("dog"), "plain text is never changed");
			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "do", "g" }));
			Assert.That(result.Runs[0].NamedStyle, Is.EqualTo("Strong"), "the covered run gets the style");
			Assert.That(result.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"), "other metadata is preserved");
			Assert.That(result.Runs[1].NamedStyle, Is.EqualTo("Emphasis"), "the uncovered styled run is untouched");
			Assert.That(result.RichXml, Is.Null, "no RichXml so ToTsString takes the run-replay path");
		}

		[Test]
		public void ApplySpanNamedStyle_PartialRun_SplitsAndStylesOnlyTheCoveredSlice()
		{
			var result = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(TwoRunDog(), 1, 2, "Strong");

			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }),
				"the first run splits at the selection boundary");
			Assert.That(result.Runs[0].NamedStyle, Is.Null);
			Assert.That(result.Runs[1].NamedStyle, Is.EqualTo("Strong"), "only the covered slice gets the style");
			Assert.That(result.Runs[1].WritingSystemTag, Is.EqualTo("qaa-x-one"),
				"the split slice inherits its source run's metadata");
			Assert.That(result.Runs[2].NamedStyle, Is.EqualTo("Emphasis"));
		}

		[Test]
		public void ApplySpanNamedStyle_AcrossRunBoundary_StylesBothCoveredSlices()
		{
			var result = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(TwoRunDog(), 1, 3, "Strong");

			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }));
			Assert.That(result.Runs[0].NamedStyle, Is.Null, "the leading slice outside the span keeps no style");
			Assert.That(result.Runs[1].NamedStyle, Is.EqualTo("Strong"), "the tail of run 1 inside the span is styled");
			Assert.That(result.Runs[2].NamedStyle, Is.EqualTo("Strong"),
				"run 2 (fully covered) is restyled, overwriting its previous Emphasis");
		}

		[Test]
		public void ApplySpanNamedStyle_NullStyle_ClearsTheStyleOverTheSpan()
		{
			// The trailing run carries "Emphasis"; clearing over [2,3) drops it, leaving no styled run.
			var cleared = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(TwoRunDog(), 2, 3, null);

			Assert.That(cleared.PlainText, Is.EqualTo("dog"));
			Assert.That(cleared.Runs.Any(r => !string.IsNullOrEmpty(r.NamedStyle)), Is.False,
				"clearing removes the named style over the span");
			Assert.That(cleared.RichXml, Is.Null);
		}

		[Test]
		public void ApplySpanNamedStyle_ZeroLengthSelection_IsNoOp()
		{
			var original = TwoRunDog();
			var result = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(original, 1, 1, "Strong");
			Assert.That(result, Is.SameAs(original), "a collapsed selection is a no-op");
		}

		[Test]
		public void ApplySpanNamedStyle_LossyValue_ReturnsUnchanged()
		{
			var lossy = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "qaa-x-one") },
				richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true);
			Assert.That(lossy.CanEditRichText, Is.False);

			var result = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(lossy, 0, 4, "Strong");
			Assert.That(result, Is.SameAs(lossy), "a lossy/read-only value is never restyled");
		}

		[Test]
		public void ApplySpanNamedStyle_RespectsGraphemeClusterBoundaries()
		{
			const string text = "aéb"; // 'a', 'e'+combining-acute, 'b' — combining cluster at [1,3)
			var value = DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, "qaa-x-one") });

			var result = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(value, 1, 2, "Strong");

			var styledText = string.Concat(result.Runs
				.Where(r => r.NamedStyle == "Strong").Select(r => r.Text));
			Assert.That(styledText, Is.EqualTo("é"),
				"the combining cluster is styled whole, never split mid-character");
		}

		[Test]
		public void SpanNamedStyle_ReportsCommonStyle_OrNullWhenMixedOrNone()
		{
			var styled = DetailRichTextEditAlgorithms.ApplySpanNamedStyle(TwoRunDog(), 0, 3, "Strong");
			Assert.That(DetailRichTextEditAlgorithms.SpanNamedStyle(styled, 0, 3), Is.EqualTo("Strong"),
				"a uniformly styled span reports its common style");

			// Original: "do" plain + "g" Emphasis -> mixed across [0,3).
			Assert.That(DetailRichTextEditAlgorithms.SpanNamedStyle(TwoRunDog(), 0, 3), Is.Null,
				"a span whose runs carry different styles reports null (mixed)");

			// A span entirely within the plain first run reports null (no style).
			Assert.That(DetailRichTextEditAlgorithms.SpanNamedStyle(TwoRunDog(), 0, 2), Is.Null,
				"a span carrying no style reports null");

			// A span entirely within the styled run reports that style.
			Assert.That(DetailRichTextEditAlgorithms.SpanNamedStyle(TwoRunDog(), 2, 3), Is.EqualTo("Emphasis"));

			Assert.That(DetailRichTextEditAlgorithms.SpanNamedStyle(TwoRunDog(), 1, 1), Is.Null,
				"a collapsed span reports null");
		}
	}

	/// <summary>
	/// Pure: <see cref="DetailRichTextEditAlgorithms.RetagSpanWritingSystem"/> splits runs at
	/// the selection boundaries and sets the writing-system tag only on covered runs, cluster-safe,
	/// honoring the lossy read-only guard; <see cref="DetailRichTextEditAlgorithms.SpanWritingSystem"/>
	/// reports the common writing system across the span (null when mixed).
	/// </summary>
	[TestFixture]
	public class RegionSpanWritingSystemTests
	{
		// "do" (qaa-x-one) + "g" (qaa-x-two) — a run boundary at index 2.
		private static DetailRichTextValue TwoRunDog() => DetailRichTextEditAlgorithms.FromRuns("dog", new[]
		{
			new DetailTextRun("do", "qaa-x-one", namedStyle: "Emphasis"),
			new DetailTextRun("g", "qaa-x-two")
		});

		[Test]
		public void RetagSpanWritingSystem_CoveringFirstRun_RetagsOnlyThatRun()
		{
			var result = DetailRichTextEditAlgorithms.RetagSpanWritingSystem(TwoRunDog(), 0, 2, "fr");

			Assert.That(result.PlainText, Is.EqualTo("dog"), "plain text is never changed");
			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "do", "g" }));
			Assert.That(result.Runs[0].WritingSystemTag, Is.EqualTo("fr"), "the covered run gets the new ws");
			Assert.That(result.Runs[0].NamedStyle, Is.EqualTo("Emphasis"), "other metadata is preserved");
			Assert.That(result.Runs[1].WritingSystemTag, Is.EqualTo("qaa-x-two"), "the uncovered run keeps its ws");
			Assert.That(result.RichXml, Is.Null, "no RichXml so ToTsString takes the run-replay path");
		}

		[Test]
		public void RetagSpanWritingSystem_PartialRun_SplitsAndRetagsOnlyTheCoveredSlice()
		{
			var result = DetailRichTextEditAlgorithms.RetagSpanWritingSystem(TwoRunDog(), 1, 2, "fr");

			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }),
				"the first run splits at the selection boundary");
			Assert.That(result.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"));
			Assert.That(result.Runs[1].WritingSystemTag, Is.EqualTo("fr"), "only the covered slice is retagged");
			Assert.That(result.Runs[1].NamedStyle, Is.EqualTo("Emphasis"),
				"the split slice inherits its source run's metadata");
			Assert.That(result.Runs[2].WritingSystemTag, Is.EqualTo("qaa-x-two"));
		}

		[Test]
		public void RetagSpanWritingSystem_AcrossRunBoundary_RetagsBothCoveredSlices()
		{
			var result = DetailRichTextEditAlgorithms.RetagSpanWritingSystem(TwoRunDog(), 1, 3, "fr");

			Assert.That(result.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }));
			Assert.That(result.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"), "the leading slice keeps its ws");
			Assert.That(result.Runs[1].WritingSystemTag, Is.EqualTo("fr"), "the tail of run 1 inside the span is retagged");
			Assert.That(result.Runs[2].WritingSystemTag, Is.EqualTo("fr"), "run 2 (fully covered) is retagged");
		}

		[Test]
		public void RetagSpanWritingSystem_EmptyWsTag_IsNoOp()
		{
			var original = TwoRunDog();
			Assert.That(DetailRichTextEditAlgorithms.RetagSpanWritingSystem(original, 0, 2, null),
				Is.SameAs(original), "a run must always carry a ws; a null tag is a no-op");
			Assert.That(DetailRichTextEditAlgorithms.RetagSpanWritingSystem(original, 0, 2, string.Empty),
				Is.SameAs(original), "an empty tag is a no-op too");
		}

		[Test]
		public void RetagSpanWritingSystem_ZeroLengthSelection_IsNoOp()
		{
			var original = TwoRunDog();
			var result = DetailRichTextEditAlgorithms.RetagSpanWritingSystem(original, 1, 1, "fr");
			Assert.That(result, Is.SameAs(original), "a collapsed selection is a no-op");
		}

		[Test]
		public void RetagSpanWritingSystem_LossyValue_ReturnsUnchanged()
		{
			var lossy = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "qaa-x-one") },
				richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true);
			Assert.That(lossy.CanEditRichText, Is.False);

			var result = DetailRichTextEditAlgorithms.RetagSpanWritingSystem(lossy, 0, 4, "fr");
			Assert.That(result, Is.SameAs(lossy), "a lossy/read-only value is never retagged");
		}

		[Test]
		public void RetagSpanWritingSystem_RespectsGraphemeClusterBoundaries()
		{
			const string text = "aéb"; // 'a', 'e'+combining-acute, 'b' — combining cluster at [1,3)
			var value = DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, "qaa-x-one") });

			var result = DetailRichTextEditAlgorithms.RetagSpanWritingSystem(value, 1, 2, "fr");

			var retaggedText = string.Concat(result.Runs
				.Where(r => r.WritingSystemTag == "fr").Select(r => r.Text));
			Assert.That(retaggedText, Is.EqualTo("é"),
				"the combining cluster is retagged whole, never split mid-character");
		}

		[Test]
		public void SpanWritingSystem_ReportsCommonWs_OrNullWhenMixed()
		{
			// "do" qaa-x-one + "g" qaa-x-two.
			Assert.That(DetailRichTextEditAlgorithms.SpanWritingSystem(TwoRunDog(), 0, 2), Is.EqualTo("qaa-x-one"),
				"a span entirely within one run reports that run's ws");
			Assert.That(DetailRichTextEditAlgorithms.SpanWritingSystem(TwoRunDog(), 0, 3), Is.Null,
				"a span whose runs carry different ws reports null (mixed)");
			Assert.That(DetailRichTextEditAlgorithms.SpanWritingSystem(TwoRunDog(), 2, 3), Is.EqualTo("qaa-x-two"));
			Assert.That(DetailRichTextEditAlgorithms.SpanWritingSystem(TwoRunDog(), 1, 1), Is.Null,
				"a collapsed span reports null");
		}
	}
}
