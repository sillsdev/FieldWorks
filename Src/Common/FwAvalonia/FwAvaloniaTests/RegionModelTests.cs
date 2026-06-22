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
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// A fake value provider so the mapper can be tested without LCModel. The LCModel-backed provider
	/// lives in xWorks (<c>LexicalEditRegionBuilder</c>).
	/// </summary>
	internal sealed class FakeRegionValueProvider : IRegionValueProvider
	{
		public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
		{
			switch (fieldNode.Field)
			{
				case "LexemeForm":
					return new List<RegionWsValue> { new RegionWsValue("vern", "dog", "Charis SIL", 12) };
				case "Gloss":
					return new List<RegionWsValue> { new RegionWsValue("anal", "canine") };
				default:
					return new List<RegionWsValue>();
			}
		}

		public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode)
			=> new List<RegionChoiceOption> { new RegionChoiceOption("stem", "stem"), new RegionChoiceOption("suffix", "suffix") };

		public string GetSelectedOptionKey(ViewNode fieldNode) => "suffix";
	}

	[TestFixture]
	public class LexicalEditRegionMapperTests
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
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());

			Assert.That(model.ClassName, Is.EqualTo("LexEntry"));
			Assert.That(model.Fields.Select(f => f.Field), Is.EqualTo(new[] { "LexemeForm", "MorphType", "Gloss" }));
		}

		[Test]
		public void TextFields_AreClassifiedAsText_AndBoundToValues()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());
			var lexeme = model.Fields.Single(f => f.Field == "LexemeForm");

			Assert.That(lexeme.Kind, Is.EqualTo(RegionFieldKind.Text));
			Assert.That(lexeme.Values.Single().Value, Is.EqualTo("dog"));
			Assert.That(lexeme.AutomationId, Is.EqualTo("LexemeFormEditor"));
		}

		private sealed class RichRegionValueProvider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new List<RegionWsValue>
				{
					new RegionWsValue("vern", "dog", richText: new RegionRichTextValue(
						"dog",
						new List<RegionTextRun>
						{
							new RegionTextRun("do", "qaa-x-one"),
							new RegionTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
						},
						richXml: "<AStr ws='qaa-x-one'><Run ws='qaa-x-one'>do</Run><Run ws='qaa-x-two' namedStyle='Emphasis'>g</Run></AStr>",
						requiresRichEditor: true))
				};

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode) => new List<RegionChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		private sealed class UnsupportedRichRegionValueProvider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new List<RegionWsValue>
				{
					new RegionWsValue("vern", "link", richText: new RegionRichTextValue(
						"link",
						new List<RegionTextRun>
						{
							new RegionTextRun("link", "qaa-x-one", objectData: "\uF8FFhttps://software.sil.org")
						},
						richXml: "<AStr ws='qaa-x-one'><Run ws='qaa-x-one' objData='x'>link</Run></AStr>",
						requiresRichEditor: true,
						canEditRichText: false))
				};

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode) => new List<RegionChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		[Test]
		public void RichTextFields_AreProjectedEditable_WhenRichRowsCanRoundTrip()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new RichRegionValueProvider());
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
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(),
				new UnsupportedRichRegionValueProvider());
			var lexeme = model.Fields.Single(f => f.Field == "LexemeForm");

			Assert.That(lexeme.IsEditable, Is.False,
				"rows with unsupported object-data runs must stay read-only until the owner task lands");
			Assert.That(lexeme.Values.Single().CanEditRichText, Is.False);
		}

		[Test]
		public void RichTextEditAlgorithm_NoOpEdit_ReturnsOriginalInstance()
		{
			var original = RegionRichTextEditAlgorithms.FromRuns("dog", new[]
			{
				new RegionTextRun("do", "qaa-x-one"),
				new RegionTextRun("g", "qaa-x-two", namedStyle: "Emphasis")
			});

			var result = RegionRichTextEditAlgorithms.ApplyPlainTextEdit(original, "dog");
			Assert.That(result, Is.SameAs(original),
				"a no-op edit should keep the exact rich payload so save-without-changes preserves runs");
		}

		[Test]
		public void ChooserField_IsClassifiedAsChooser_WithOptionsAndSelection()
		{
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());
			var morph = model.Fields.Single(f => f.Field == "MorphType");

			Assert.That(morph.Kind, Is.EqualTo(RegionFieldKind.Chooser));
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

			var model = LexicalEditRegionMapper.FromViewDefinition(def, new FakeRegionValueProvider());
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

			var model = LexicalEditRegionMapper.FromViewDefinition(def, new FakeRegionValueProvider());
			Assert.That(model.Fields.Single().Kind, Is.EqualTo(RegionFieldKind.Unsupported));
		}

		[Test]
		public void Diagnostics_ArePreserved_FromTheDefinition()
		{
			var diags = new List<ViewDiagnostic> { new ViewDiagnostic(ViewDiagnosticSeverity.Warning, "x", "m", "p") };
			var def = new ViewDefinitionModel("LexEntry", "identity", "detail", new List<ViewNode>(), diags);

			var model = LexicalEditRegionMapper.FromViewDefinition(def, new FakeRegionValueProvider());
			Assert.That(model.Diagnostics, Has.Count.EqualTo(1));
		}

		[Test]
		public void GraphemeClusters_KhmerSyllable_IsOneUserVisibleCluster()
		{
			var starts = RegionTextGraphemeClusters.GetClusterStarts("កាx");

			Assert.That(starts, Is.EqualTo(new[] { 0, 2 }),
				"Khmer base+vowel stays one grapheme cluster; the following Latin character starts a new cluster");
		}

		[Test]
		public void GraphemeClusters_CombiningMarkSequence_IsOneUserVisibleCluster()
		{
			var starts = RegionTextGraphemeClusters.GetClusterStarts("a\u0301b");

			Assert.That(starts, Is.EqualTo(new[] { 0, 2 }),
				"Latin base plus combining acute stays one cluster; the trailing letter starts the next cluster");
		}

		[Test]
		public void GraphemeClusters_SurrogatePairEmoji_IsOneUserVisibleCluster()
		{
			var starts = RegionTextGraphemeClusters.GetClusterStarts("\U0001F600x");

			Assert.That(starts, Is.EqualTo(new[] { 0, 2 }),
				"A surrogate-pair emoji stays one cluster; the following Latin character starts a new cluster");
		}

		[Test]
		public void GraphemeClusters_ZwjFamilySequence_IsOneUserVisibleCluster()
		{
			var starts = RegionTextGraphemeClusters.GetClusterStarts("\U0001F468\u200D\U0001F469\u200D\U0001F467z");

			Assert.That(starts, Is.EqualTo(new[] { 0, 8 }),
				"A ZWJ family sequence is one cluster; the following Latin character starts the next cluster");
		}

		[Test]
		public void ImeCompositionState_ComposeCancelCommit_LeavesCommittedTextUntouchedUntilCommit()
		{
			var ime = new RegionImeCompositionState("hello world");
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
			var ime = new RegionImeCompositionState("cat");
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
			var original = RegionRichTextEditAlgorithms.FromRuns("abc\u05d0\u05d1\u05d2", new[]
			{
				new RegionTextRun("abc", "qaa-x-left", namedStyle: "LeftStyle"),
				new RegionTextRun("\u05d0\u05d1\u05d2", "qaa-x-rtl", namedStyle: "RtlStyle")
			});

			var edited = RegionRichTextEditAlgorithms.ApplyPlainTextEdit(original, "abcX\u05d0\u05d1\u05d2");

			Assert.That(edited.Runs.Select(r => r.Text), Is.EqualTo(new[] { "abcX", "\u05d0\u05d1\u05d2" }));
			Assert.That(edited.Runs[0].NamedStyle, Is.EqualTo("LeftStyle"));
			Assert.That(edited.Runs[1].NamedStyle, Is.EqualTo("RtlStyle"),
				"inserts at run boundaries must not leak style metadata across the boundary");
		}

		[Test]
		public void BidirectionalCaretNavigation_MapsArrowKeysThroughActiveRunDirection()
		{
			const string mixed = "abc \u05d0\u05d1\u05d2 xyz";
			var rich = RegionRichTextEditAlgorithms.FromRuns(mixed, new[]
			{
				new RegionTextRun("abc ", "qaa-x-left"),
				new RegionTextRun("\u05d0\u05d1\u05d2", "qaa-x-rtl"),
				new RegionTextRun(" xyz", "qaa-x-left")
			});

			var insideRtl = 5;
			var afterLeft = RegionBidirectionalTextNavigation.MoveCaret(mixed, rich.Runs, insideRtl,
				physicalLeft: true, defaultRightToLeft: true);
			Assert.That(afterLeft, Is.EqualTo(6),
				"inside RTL run, Left arrow advances logically");

			var afterRight = RegionBidirectionalTextNavigation.MoveCaret(mixed, rich.Runs, afterLeft,
				physicalLeft: false, defaultRightToLeft: true);
			Assert.That(afterRight, Is.EqualTo(5),
				"inside RTL run, Right arrow moves logically backward");
		}

		[Test]
		public void SelectionAndHitTest_NormalizeToWholeGraphemeClusters()
		{
			const string text = "a\U0001F469\u200D\U0001F467b";

			var normalizedRange = RegionBidirectionalTextNavigation.NormalizeSelectionToClusters(text, 2, 4);
			Assert.That(normalizedRange.Start, Is.EqualTo(1));
			Assert.That(normalizedRange.End, Is.EqualTo(6),
				"selection covering part of a ZWJ cluster expands to whole user-visible character");

			var normalizedCaret = RegionBidirectionalTextNavigation.NormalizeHitTestCaretIndex(text, 3);
			Assert.That(normalizedCaret, Is.EqualTo(1),
				"hit-test caret in the middle of a grapheme snaps to cluster start");
		}
	}

	[TestFixture]
	public class LexicalEditRegionViewTests
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
			var model = LexicalEditRegionMapper.FromViewDefinition(SampleDefinition(), new FakeRegionValueProvider());
			var view = new LexicalEditRegionView(model);
			var window = new Window { Content = view, Width = 420, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			Assert.That(AutomationProperties.GetAutomationId(view), Is.EqualTo("LexicalEditRegionView"));

			var lexemeBox = view.GetVisualDescendants().OfType<TextBox>()
				.FirstOrDefault(b => AutomationProperties.GetAutomationId(b) == "LexemeFormEditor.vern");
			Assert.That(lexemeBox, Is.Not.Null, "the text field should render a per-ws box with a stable automation id");
			Assert.That(lexemeBox.Text, Is.EqualTo("dog"));

			var chooser = view.GetVisualDescendants().OfType<Button>()
				.FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "MorphTypeChooser");
			Assert.That(chooser, Is.Not.Null, "the chooser field should render the owned flyout chooser");
		}
	}
}
