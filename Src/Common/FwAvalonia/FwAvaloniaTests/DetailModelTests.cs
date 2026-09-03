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
	public class DetailModelProjectorTests
	{
		private static ViewDefinitionModel SampleDefinition()
		{
			var roots = new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "LexemeForm", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: HostRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Morph Type", null, "MorphType", "morphtypeatomicreference",
					EditorClassification.Known, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "MorphTypeChooser", routing: HostRouting.Product),
				new ViewNode("LexEntry/identity/#2", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "SenseGlossEditor", routing: HostRouting.Product)
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
		public void ReplacePlainText_InsertAtRunBoundary_PreservesNeighborRunMetadata()
		{
			const string text = "abc\u05d0\u05d1\u05d2";
			var initial = DetailRichTextEditAlgorithms.FromRuns(text, new[]
			{
				new DetailTextRun("abc", "qaa-x-left", namedStyle: "LeftStyle"),
				new DetailTextRun("\u05d0\u05d1\u05d2", "qaa-x-rtl", namedStyle: "RtlStyle")
			});
			var editor = new DetailTextEditor(initial, () => text, "qaa-x-left", _ => true);

			Assert.That(editor.ReplacePlainText("abcX\u05d0\u05d1\u05d2"), Is.True);

			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "abcX", "\u05d0\u05d1\u05d2" }));
			Assert.That(editor.Current.Runs[0].NamedStyle, Is.EqualTo("LeftStyle"));
			Assert.That(editor.Current.Runs[1].NamedStyle, Is.EqualTo("RtlStyle"),
				"inserts at run boundaries must not leak style metadata across the boundary");
		}

		[Test]
		public void MoveCaret_MapsArrowKeysThroughActiveRunDirection()
		{
			const string mixed = "abc \u05d0\u05d1\u05d2 xyz";
			var initial = DetailRichTextEditAlgorithms.FromRuns(mixed, new[]
			{
				new DetailTextRun("abc ", "qaa-x-left"),
				new DetailTextRun("\u05d0\u05d1\u05d2", "qaa-x-rtl"),
				new DetailTextRun(" xyz", "qaa-x-left")
			});
			var editor = new DetailTextEditor(initial, () => mixed, "qaa-x-left", _ => true, rightToLeft: true);

			var insideRtl = 5;
			var afterLeft = editor.MoveCaret(insideRtl, physicalLeft: true);
			Assert.That(afterLeft, Is.EqualTo(6),
				"inside RTL run, Left arrow advances logically");

			var afterRight = editor.MoveCaret(afterLeft, physicalLeft: false);
			Assert.That(afterRight, Is.EqualTo(5),
				"inside RTL run, Right arrow moves logically backward");
		}

		[Test]
		public void NormalizeSelectionAndHitTest_SnapToWholeGraphemeClusters()
		{
			const string text = "a\U0001F469\u200D\U0001F467b";
			var editor = new DetailTextEditor(null, () => text, "en", _ => true);

			var normalizedRange = editor.NormalizeSelectionToClusters(2, 4);
			Assert.That(normalizedRange.Start, Is.EqualTo(1));
			Assert.That(normalizedRange.End, Is.EqualTo(6),
				"selection covering part of a ZWJ cluster expands to whole user-visible character");

			var normalizedCaret = editor.NormalizeHitTestCaretIndex(3);
			Assert.That(normalizedCaret, Is.EqualTo(1),
				"hit-test caret in the middle of a grapheme snaps to cluster start");
		}
	}

	[TestFixture]
	public class DataTreeTests
	{
		private static ViewDefinitionModel SampleDefinition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "LexemeForm", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: HostRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Morph Type", null, "MorphType", "morphtypeatomicreference",
					EditorClassification.Known, null, ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "MorphTypeChooser", routing: HostRouting.Product)
			},
			new List<ViewDiagnostic>());

		[AvaloniaTest]
		public void DetailView_RendersFields_WithStableAutomationIds()
		{
			var model = DetailModelProjector.FromViewDefinition(SampleDefinition(), new FakeDetailValueProvider());
			var view = new DataTree(model);
			var window = new Window { Content = view, Width = 420, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(AutomationProperties.GetAutomationId(view), Is.EqualTo("DataTree"));

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
	/// Character-format GESTURES driven through <see cref="DetailTextEditor.ToggleFormat"/>: it
	/// splits runs at the selection boundaries and sets the chosen attribute only on covered
	/// runs,
	/// leaving the rest of the value's run metadata untouched -- across run boundaries, partial
	/// runs,
	/// grapheme clusters, and the lossy read-only guard. The one exception is
	/// <see cref="SpanFullyHasFormat_ReportsWhetherTheWholeSpanCarriesTheAttribute"/>, which
	/// stays at the algorithm layer: <see cref="DetailTextEditor"/> exposes no probe for it,
	/// because it is a decision <c>ToggleFormat</c> makes rather than a query callers ask.
	/// </summary>
	[TestFixture]
	public class DetailSpanFormattingTests
	{
		private static DetailRichTextValue TwoRunDog() => DetailRichTextEditAlgorithms.FromRuns("dog", new[]
		{
			new DetailTextRun("do", "qaa-x-one"),
			new DetailTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
		});

		private static DetailTextEditor Editor(DetailRichTextValue initial)
			=> new DetailTextEditor(initial, () => "dog", "qaa-x-one", _ => true);

		// Selection fully inside the FIRST run: the run splits into bold "do"-prefix... here the whole
		// first run is covered, so it becomes one bold run; the styled trailing run is untouched.
		[Test]
		public void ToggleFormat_CoveringFirstRun_BoldsOnlyThatRun()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.ToggleFormat(0, 2, DetailRunFormat.Bold), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo("dog"), "plain text is never changed");
			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "do", "g" }));
			Assert.That(editor.Current.Runs[0].Bold, Is.True, "the covered run gets bold");
			Assert.That(editor.Current.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"), "other metadata is preserved");
			Assert.That(editor.Current.Runs[1].Bold, Is.False, "the uncovered run is untouched");
			Assert.That(editor.Current.Runs[1].NamedStyle, Is.EqualTo("Emphasis"), "uncovered run keeps its style");
			Assert.That(editor.Current.RichXml, Is.Null, "no RichXml so ToTsString takes the run-replay path");
		}

		// A PARTIAL-run selection splits that run: "d" stays plain, "o" goes bold, "g" untouched.
		[Test]
		public void ToggleFormat_PartialRun_SplitsAndBoldsOnlyTheCoveredSlice()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.ToggleFormat(1, 2, DetailRunFormat.Bold), Is.True);

			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }),
				"the first run splits at the selection boundary");
			Assert.That(editor.Current.Runs[0].Bold, Is.False);
			Assert.That(editor.Current.Runs[1].Bold, Is.True, "only the covered slice is bold");
			Assert.That(editor.Current.Runs[1].WritingSystemTag, Is.EqualTo("qaa-x-one"),
				"the split slice inherits its source run's metadata");
			Assert.That(editor.Current.Runs[2].Bold, Is.False);
		}

		// A selection that SPANS a run boundary bolds across both runs, splitting each as needed.
		[Test]
		public void ToggleFormat_AcrossRunBoundary_BoldsBothCoveredSlices()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.ToggleFormat(1, 3, DetailRunFormat.Bold), Is.True);

			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }));
			Assert.That(editor.Current.Runs[0].Bold, Is.False, "the leading slice outside the span stays plain");
			Assert.That(editor.Current.Runs[1].Bold, Is.True, "the tail of run 1 inside the span is bold");
			Assert.That(editor.Current.Runs[2].Bold, Is.True, "run 2 (fully covered) is bold");
			Assert.That(editor.Current.Runs[2].NamedStyle, Is.EqualTo("Emphasis"),
				"the bolded run keeps its other metadata");
		}

		[Test]
		public void ToggleFormat_Italic_And_Underline_SetTheCorrectAttribute()
		{
			var italicEditor = Editor(TwoRunDog());
			italicEditor.ToggleFormat(0, 2, DetailRunFormat.Italic);
			Assert.That(italicEditor.Current.Runs[0].Italic, Is.True);
			Assert.That(italicEditor.Current.Runs[0].Bold, Is.False);
			Assert.That(italicEditor.Current.Runs[0].Underline, Is.False);

			var underlineEditor = Editor(TwoRunDog());
			underlineEditor.ToggleFormat(0, 2, DetailRunFormat.Underline);
			Assert.That(underlineEditor.Current.Runs[0].Underline, Is.True);
			Assert.That(underlineEditor.Current.Runs[0].Bold, Is.False);
		}

		[Test]
		public void ToggleFormat_ASecondTime_ClearsTheAttribute()
		{
			var editor = Editor(TwoRunDog());
			editor.ToggleFormat(1, 2, DetailRunFormat.Bold);
			Assert.That(editor.Current.Runs.First(r => r.Text == "o").Bold, Is.True);

			Assert.That(editor.ToggleFormat(1, 2, DetailRunFormat.Bold), Is.True,
				"an all-on span toggles off rather than staying a no-op");

			Assert.That(editor.Current.Runs.Any(r => r.Bold), Is.False, "the attribute is cleared over the span");
			Assert.That(editor.Current.PlainText, Is.EqualTo("dog"));
		}

		[Test]
		public void ToggleFormat_ZeroLengthSelection_IsNoOp()
		{
			var initial = TwoRunDog();
			var editor = Editor(initial);

			Assert.That(editor.ToggleFormat(1, 1, DetailRunFormat.Bold), Is.False, "a collapsed selection is a no-op");
			Assert.That(editor.Current, Is.SameAs(initial));
		}

		// At the gesture layer: a lossy value is read-only and the toggle stages nothing.
		[Test]
		public void ToggleFormat_LossyValue_StagesNothing()
		{
			var lossy = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "qaa-x-one") },
				richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true);
			Assert.That(lossy.CanEditRichText, Is.False);
			var editor = new DetailTextEditor(lossy, () => "coloured", "qaa-x-one", _ => true);

			Assert.That(editor.ToggleFormat(0, 4, DetailRunFormat.Bold), Is.False,
				"a lossy/read-only value is never reformatted");
			Assert.That(editor.Current, Is.SameAs(lossy));
		}

		// Grapheme-cluster safety: a selection whose boundaries fall inside a combining cluster snaps
		// OUTWARD so the cluster is never split (matching the bidi navigation's boundary logic).
		[Test]
		public void ToggleFormat_RespectsGraphemeClusterBoundaries()
		{
			// "a" + (e + combining acute U+0301) + "b": indices 0='a',1='e',2=U+0301,3='b'.
			const string text = "aéb";
			var initial = DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, "qaa-x-one") });
			var editor = new DetailTextEditor(initial, () => text, "qaa-x-one", _ => true);

			// Selecting [1,2) lands inside the e-acute cluster; it must snap out to cover the whole cluster.
			editor.ToggleFormat(1, 2, DetailRunFormat.Bold);

			var boldText = string.Concat(editor.Current.Runs.Where(r => r.Bold).Select(r => r.Text));
			Assert.That(boldText, Is.EqualTo("é"),
				"the combining cluster is bolded whole, never split mid-character");
		}

		// FINDING: no editor-level equivalent exists for this probe (see class remarks), so it
		// stays at the algorithm layer.
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
	/// Named-character-style GESTURES driven through <see cref="DetailTextEditor.SetNamedStyle"/>
	/// and probed through <see cref="DetailTextEditor.NamedStyleIn"/>: it splits runs at the
	/// selection boundaries and sets/clears the named character style only on covered runs,
	/// cluster-safe, honoring the lossy read-only guard.
	/// </summary>
	[TestFixture]
	public class DetailSpanNamedStyleTests
	{
		// "do" (plain) + "g" (Emphasis) -- a run boundary at index 2.
		private static DetailRichTextValue TwoRunDog() => DetailRichTextEditAlgorithms.FromRuns("dog", new[]
		{
			new DetailTextRun("do", "qaa-x-one"),
			new DetailTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
		});

		private static DetailTextEditor Editor(DetailRichTextValue initial)
			=> new DetailTextEditor(initial, () => "dog", "qaa-x-one", _ => true);

		[Test]
		public void SetNamedStyle_CoveringFirstRun_StylesOnlyThatRun()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.SetNamedStyle(0, 2, "Strong"), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo("dog"), "plain text is never changed");
			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "do", "g" }));
			Assert.That(editor.Current.Runs[0].NamedStyle, Is.EqualTo("Strong"), "the covered run gets the style");
			Assert.That(editor.Current.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"), "other metadata is preserved");
			Assert.That(editor.Current.Runs[1].NamedStyle, Is.EqualTo("Emphasis"), "the uncovered styled run is untouched");
			Assert.That(editor.Current.RichXml, Is.Null, "no RichXml so ToTsString takes the run-replay path");
		}

		[Test]
		public void SetNamedStyle_PartialRun_SplitsAndStylesOnlyTheCoveredSlice()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.SetNamedStyle(1, 2, "Strong"), Is.True);

			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }),
				"the first run splits at the selection boundary");
			Assert.That(editor.Current.Runs[0].NamedStyle, Is.Null);
			Assert.That(editor.Current.Runs[1].NamedStyle, Is.EqualTo("Strong"), "only the covered slice gets the style");
			Assert.That(editor.Current.Runs[1].WritingSystemTag, Is.EqualTo("qaa-x-one"),
				"the split slice inherits its source run's metadata");
			Assert.That(editor.Current.Runs[2].NamedStyle, Is.EqualTo("Emphasis"));
		}

		[Test]
		public void SetNamedStyle_AcrossRunBoundary_StylesBothCoveredSlices()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.SetNamedStyle(1, 3, "Strong"), Is.True);

			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }));
			Assert.That(editor.Current.Runs[0].NamedStyle, Is.Null, "the leading slice outside the span keeps no style");
			Assert.That(editor.Current.Runs[1].NamedStyle, Is.EqualTo("Strong"), "the tail of run 1 inside the span is styled");
			Assert.That(editor.Current.Runs[2].NamedStyle, Is.EqualTo("Strong"),
				"run 2 (fully covered) is restyled, overwriting its previous Emphasis");
		}

		[Test]
		public void SetNamedStyle_NullStyle_ClearsTheStyleOverTheSpan()
		{
			var editor = Editor(TwoRunDog());

			// The trailing run carries "Emphasis"; clearing over [2,3) drops it, leaving no styled run.
			Assert.That(editor.SetNamedStyle(2, 3, null), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo("dog"));
			Assert.That(editor.Current.Runs.Any(r => !string.IsNullOrEmpty(r.NamedStyle)), Is.False,
				"clearing removes the named style over the span");
			Assert.That(editor.Current.RichXml, Is.Null);
		}

		[Test]
		public void SetNamedStyle_ZeroLengthSelection_IsNoOp()
		{
			var initial = TwoRunDog();
			var editor = Editor(initial);

			Assert.That(editor.SetNamedStyle(1, 1, "Strong"), Is.False, "a collapsed selection is a no-op");
			Assert.That(editor.Current, Is.SameAs(initial));
		}

		[Test]
		public void SetNamedStyle_LossyValue_StagesNothing()
		{
			var lossy = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "qaa-x-one") },
				richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true);
			Assert.That(lossy.CanEditRichText, Is.False);
			var editor = new DetailTextEditor(lossy, () => "coloured", "qaa-x-one", _ => true);

			Assert.That(editor.SetNamedStyle(0, 4, "Strong"), Is.False, "a lossy/read-only value is never restyled");
			Assert.That(editor.Current, Is.SameAs(lossy));
		}

		[Test]
		public void SetNamedStyle_RespectsGraphemeClusterBoundaries()
		{
			const string text = "aéb"; // 'a', 'e'+combining-acute, 'b' -- combining cluster at [1,3)
			var initial = DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, "qaa-x-one") });
			var editor = new DetailTextEditor(initial, () => text, "qaa-x-one", _ => true);

			editor.SetNamedStyle(1, 2, "Strong");

			var styledText = string.Concat(editor.Current.Runs
				.Where(r => r.NamedStyle == "Strong").Select(r => r.Text));
			Assert.That(styledText, Is.EqualTo("é"),
				"the combining cluster is styled whole, never split mid-character");
		}

		[Test]
		public void NamedStyleIn_ReportsCommonStyle_OrNullWhenMixedOrNone()
		{
			var styledEditor = Editor(TwoRunDog());
			styledEditor.SetNamedStyle(0, 3, "Strong");
			Assert.That(styledEditor.NamedStyleIn(0, 3), Is.EqualTo("Strong"),
				"a uniformly styled span reports its common style");

			// Original: "do" plain + "g" Emphasis -> mixed across [0,3).
			var unstyledEditor = Editor(TwoRunDog());
			Assert.That(unstyledEditor.NamedStyleIn(0, 3), Is.Null,
				"a span whose runs carry different styles reports null (mixed)");

			// A span entirely within the plain first run reports null (no style).
			Assert.That(unstyledEditor.NamedStyleIn(0, 2), Is.Null,
				"a span carrying no style reports null");

			// A span entirely within the styled run reports that style.
			Assert.That(unstyledEditor.NamedStyleIn(2, 3), Is.EqualTo("Emphasis"));

			Assert.That(unstyledEditor.NamedStyleIn(1, 1), Is.Null,
				"a collapsed span reports null");
		}
	}

	/// <summary>
	/// Per-run writing-system-retag GESTURES driven through
	/// <see cref="DetailTextEditor.RetagWritingSystem"/> and probed through
	/// <see cref="DetailTextEditor.WritingSystemIn"/>: the gesture splits runs at the
	/// selection boundaries and sets the writing-system tag only on covered runs,
	/// cluster-safe, honoring the lossy read-only guard.
	/// </summary>
	[TestFixture]
	public class DetailSpanWritingSystemTests
	{
		// "do" (qaa-x-one) + "g" (qaa-x-two) -- a run boundary at index 2.
		private static DetailRichTextValue TwoRunDog() => DetailRichTextEditAlgorithms.FromRuns("dog", new[]
		{
			new DetailTextRun("do", "qaa-x-one", namedStyle: "Emphasis"),
			new DetailTextRun("g", "qaa-x-two")
		});

		private static DetailTextEditor Editor(DetailRichTextValue initial)
			=> new DetailTextEditor(initial, () => "dog", "qaa-x-one", _ => true);

		[Test]
		public void RetagWritingSystem_CoveringFirstRun_RetagsOnlyThatRun()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.RetagWritingSystem(0, 2, "fr"), Is.True);

			Assert.That(editor.Current.PlainText, Is.EqualTo("dog"), "plain text is never changed");
			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "do", "g" }));
			Assert.That(editor.Current.Runs[0].WritingSystemTag, Is.EqualTo("fr"), "the covered run gets the new ws");
			Assert.That(editor.Current.Runs[0].NamedStyle, Is.EqualTo("Emphasis"), "other metadata is preserved");
			Assert.That(editor.Current.Runs[1].WritingSystemTag, Is.EqualTo("qaa-x-two"), "the uncovered run keeps its ws");
			Assert.That(editor.Current.RichXml, Is.Null, "no RichXml so ToTsString takes the run-replay path");
		}

		[Test]
		public void RetagWritingSystem_PartialRun_SplitsAndRetagsOnlyTheCoveredSlice()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.RetagWritingSystem(1, 2, "fr"), Is.True);

			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }),
				"the first run splits at the selection boundary");
			Assert.That(editor.Current.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"));
			Assert.That(editor.Current.Runs[1].WritingSystemTag, Is.EqualTo("fr"), "only the covered slice is retagged");
			Assert.That(editor.Current.Runs[1].NamedStyle, Is.EqualTo("Emphasis"),
				"the split slice inherits its source run's metadata");
			Assert.That(editor.Current.Runs[2].WritingSystemTag, Is.EqualTo("qaa-x-two"));
		}

		[Test]
		public void RetagWritingSystem_AcrossRunBoundary_RetagsBothCoveredSlices()
		{
			var editor = Editor(TwoRunDog());

			Assert.That(editor.RetagWritingSystem(1, 3, "fr"), Is.True);

			Assert.That(editor.Current.Runs.Select(r => r.Text), Is.EqualTo(new[] { "d", "o", "g" }));
			Assert.That(editor.Current.Runs[0].WritingSystemTag, Is.EqualTo("qaa-x-one"), "the leading slice keeps its ws");
			Assert.That(editor.Current.Runs[1].WritingSystemTag, Is.EqualTo("fr"), "the tail of run 1 inside the span is retagged");
			Assert.That(editor.Current.Runs[2].WritingSystemTag, Is.EqualTo("fr"), "run 2 (fully covered) is retagged");
		}

		[Test]
		public void RetagWritingSystem_EmptyWsTag_IsNoOp()
		{
			var initial = TwoRunDog();
			var editor = Editor(initial);

			Assert.That(editor.RetagWritingSystem(0, 2, null), Is.False,
				"a run must always carry a ws; a null tag is a no-op");
			Assert.That(editor.RetagWritingSystem(0, 2, string.Empty), Is.False, "an empty tag is a no-op too");
			Assert.That(editor.Current, Is.SameAs(initial));
		}

		[Test]
		public void RetagWritingSystem_ZeroLengthSelection_IsNoOp()
		{
			var initial = TwoRunDog();
			var editor = Editor(initial);

			Assert.That(editor.RetagWritingSystem(1, 1, "fr"), Is.False, "a collapsed selection is a no-op");
			Assert.That(editor.Current, Is.SameAs(initial));
		}

		[Test]
		public void RetagWritingSystem_LossyValue_StagesNothing()
		{
			var lossy = new DetailRichTextValue("coloured",
				new[] { new DetailTextRun("coloured", "qaa-x-one") },
				richXml: "<Str/>", requiresRichEditor: true, lossyProperties: true);
			Assert.That(lossy.CanEditRichText, Is.False);
			var editor = new DetailTextEditor(lossy, () => "coloured", "qaa-x-one", _ => true);

			Assert.That(editor.RetagWritingSystem(0, 4, "fr"), Is.False, "a lossy/read-only value is never retagged");
			Assert.That(editor.Current, Is.SameAs(lossy));
		}

		[Test]
		public void RetagWritingSystem_RespectsGraphemeClusterBoundaries()
		{
			const string text = "aéb"; // 'a', 'e'+combining-acute, 'b' — combining cluster at [1,3)
			var initial = DetailRichTextEditAlgorithms.FromRuns(text,
				new[] { new DetailTextRun(text, "qaa-x-one") });
			var editor = new DetailTextEditor(initial, () => text, "qaa-x-one", _ => true);

			editor.RetagWritingSystem(1, 2, "fr");

			var retaggedText = string.Concat(editor.Current.Runs
				.Where(r => r.WritingSystemTag == "fr").Select(r => r.Text));
			Assert.That(retaggedText, Is.EqualTo("é"),
				"the combining cluster is retagged whole, never split mid-character");
		}

		[Test]
		public void WritingSystemIn_ReportsCommonWs_OrNullWhenMixed()
		{
			var editor = Editor(TwoRunDog());

			// "do" qaa-x-one + "g" qaa-x-two.
			Assert.That(editor.WritingSystemIn(0, 2), Is.EqualTo("qaa-x-one"),
				"a span entirely within one run reports that run's ws");
			Assert.That(editor.WritingSystemIn(0, 3), Is.Null,
				"a span whose runs carry different ws reports null (mixed)");
			Assert.That(editor.WritingSystemIn(2, 3), Is.EqualTo("qaa-x-two"));
			Assert.That(editor.WritingSystemIn(1, 1), Is.Null,
				"a collapsed span reports null");
		}
	}
}
