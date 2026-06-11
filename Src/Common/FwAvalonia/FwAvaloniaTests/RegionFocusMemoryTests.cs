// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
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
	/// Focus continuity across region re-shows (14.4 usability): the host replaces the entire view
	/// after every committed edit, so the focused editor (identified by its stable automation id)
	/// and caret must carry over to the rebuilt view — otherwise tabbing out of a field would
	/// destroy the editor the user just moved into.
	/// </summary>
	[TestFixture]
	public class RegionFocusMemoryTests
	{
		private static ViewDefinitionModel Definition() => new ViewDefinitionModel(
			"LexEntry", "identity", "detail",
			new List<ViewNode>
			{
				new ViewNode("LexEntry/identity/#0", ViewNodeKind.Field, "Lexeme Form", null, "Form", "multistring",
					EditorClassification.Known, "vernacular", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "LexemeFormEditor", routing: SurfaceRouting.Product),
				new ViewNode("LexEntry/identity/#1", ViewNodeKind.Field, "Gloss", null, "Gloss", "multistring",
					EditorClassification.Known, "analysis", ViewVisibility.Always, ViewExpansion.NotApplicable, false, null, null,
					automationId: "GlossEditor", routing: SurfaceRouting.Product)
			},
			new List<ViewDiagnostic>());

		private sealed class Provider : IRegionValueProvider
		{
			public IReadOnlyList<RegionWsValue> GetValues(ViewNode fieldNode)
				=> new List<RegionWsValue> { new RegionWsValue("vern", "casa") };

			public IReadOnlyList<RegionChoiceOption> GetOptions(ViewNode fieldNode) => new List<RegionChoiceOption>();

			public string GetSelectedOptionKey(ViewNode fieldNode) => null;
		}

		private static LexicalEditRegionView NewView()
			=> new LexicalEditRegionView(LexicalEditRegionMapper.FromViewDefinition(Definition(), new Provider()));

		private static TextBox FindEditor(Control root, string automationId)
		{
			foreach (var visual in root.GetVisualDescendants())
			{
				if (visual is TextBox box && AutomationProperties.GetAutomationId(box) == automationId)
					return box;
			}
			return null;
		}

		[AvaloniaTest]
		public void CaptureAndRestore_CarryFocusAndCaret_AcrossAViewRebuild()
		{
			var first = NewView();
			var window = new Window { Content = first, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var editor = FindEditor(first, "GlossEditor.vern");
			Assert.That(editor, Is.Not.Null);
			editor.Focus();
			editor.CaretIndex = 2;
			Dispatcher.UIThread.RunJobs();

			var memento = RegionFocusMemory.Capture(first);
			Assert.That(memento, Is.Not.Null, "the focused editor inside the view must be captured");
			Assert.That(memento.AutomationId, Is.EqualTo("GlossEditor.vern"));

			var second = NewView();
			window.Content = second;
			Dispatcher.UIThread.RunJobs();

			Assert.That(RegionFocusMemory.TryRestore(second, memento), Is.True);
			Dispatcher.UIThread.RunJobs();
			var restored = FindEditor(second, "GlossEditor.vern");
			Assert.That(restored.IsFocused, Is.True, "the same field/ws editor must own focus in the rebuilt view");
			Assert.That(restored.CaretIndex, Is.EqualTo(2), "the caret position carries over");
		}

		[AvaloniaTest]
		public void Capture_ReturnsNull_WhenFocusIsOutsideTheView()
		{
			var view = NewView();
			var other = new TextBox();
			var panel = new StackPanel();
			panel.Children.Add(view);
			panel.Children.Add(other);
			var window = new Window { Content = panel, Width = 420, Height = 240 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			other.Focus();
			Dispatcher.UIThread.RunJobs();

			Assert.That(RegionFocusMemory.Capture(view), Is.Null,
				"focus outside the region must not be captured (re-shows must not steal it)");
		}

		[AvaloniaTest]
		public void TryRestore_ReturnsFalse_WhenTheFieldDisappeared()
		{
			var view = NewView();
			var window = new Window { Content = view, Width = 420, Height = 200 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var memento = new RegionFocusMemory.Memento("NoSuchEditor.vern", 0);
			Assert.That(RegionFocusMemory.TryRestore(view, memento), Is.False);
		}
	}
}
