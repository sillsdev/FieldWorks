// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Headless.NUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;
using SIL.FieldWorks.Common.FwAvalonia.ViewDefinition;

namespace FwAvaloniaTests
{
	/// <summary>
	/// Task 6.4 — TreeView spike: multiple translations per sense rendered as compact
	/// multi-writing-system node templates. Per the control-selection matrix, stock TreeView is
	/// acceptable only for bounded trees (it does not virtualize); this spike proves the node
	/// template density story it was kept for.
	/// </summary>
	[TestFixture]
	public class SenseTreeSpikeTests
	{
		private sealed class SenseNode
		{
			public string Number;
			public IReadOnlyList<(string ws, string text)> Translations;
			public List<SenseNode> Children = new List<SenseNode>();
		}

		private static SenseNode Sense(string number, params (string, string)[] translations)
			=> new SenseNode { Number = number, Translations = translations };

		[AvaloniaTest]
		public void SenseTree_RendersCompactMultiWsNodeTemplates()
		{
			var senses = new List<SenseNode>
			{
				Sense("1", ("en", "house"), ("es", "casa"), ("fr", "maison")),
				Sense("2", ("en", "home"), ("es", "hogar"))
			};
			senses[0].Children.Add(Sense("1.1", ("en", "hut"), ("es", "choza")));

			var tree = new TreeView
			{
				ItemsSource = senses,
				ItemTemplate = new FuncTreeDataTemplate<SenseNode>(
					(node, _) =>
					{
						// Compact node: sense number + one dense ws/text row per translation.
						var panel = new StackPanel { Orientation = Avalonia.Layout.Orientation.Horizontal, Spacing = 6 };
						panel.Children.Add(new TextBlock { Text = node.Number, FontWeight = FontWeight.Bold });
						foreach (var (ws, text) in node.Translations)
						{
							panel.Children.Add(new TextBlock { Text = ws, Foreground = Brushes.Gray });
							panel.Children.Add(new TextBlock { Text = text });
						}

						Avalonia.Automation.AutomationProperties.SetAutomationId(panel, "SenseNode." + node.Number);
						return panel;
					},
					node => node.Children)
			};

			var window = new Window { Content = tree, Width = 480, Height = 320 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var items = tree.GetVisualDescendants().OfType<TreeViewItem>().ToList();
			Assert.That(items.Count, Is.GreaterThanOrEqualTo(2), "top-level senses render");

			items[0].IsExpanded = true;
			Dispatcher.UIThread.RunJobs();
			Assert.That(tree.GetVisualDescendants().OfType<TreeViewItem>().Count(), Is.GreaterThanOrEqualTo(3),
				"subsenses expand");

			var node1 = tree.GetVisualDescendants().OfType<StackPanel>()
				.FirstOrDefault(p => Avalonia.Automation.AutomationProperties.GetAutomationId(p) == "SenseNode.1");
			Assert.That(node1, Is.Not.Null);
			Assert.That(node1.Children.OfType<TextBlock>().Select(t => t.Text),
				Does.Contain("casa").And.Contain("maison"), "all translations render in one compact node");
		}
	}

	/// <summary>
	/// Task 6.13, RTL lane — headless evidence that the owned multi-WS field edits right-to-left
	/// script text: the editor takes RTL flow direction from the writing system, Arabic text
	/// round-trips through editing, and caret/selection indices operate on the logical string
	/// (Avalonia's TextLayout handles visual bidi reordering). IME composition and on-device
	/// complex-script verification remain the manual half of the gate.
	/// </summary>
	[TestFixture]
	public class RtlEditingTests
	{
		private const string ArabicHouse = "بيت"; // بيت
		private const string ArabicBig = "كبير"; // كبير

		private static LexicalEditRegionField RtlField() => new LexicalEditRegionField(
			"LexEntry/x/#0", "Lexeme Form", "Form", "vernacular", RegionFieldKind.Text,
			EditorClassification.Known, "RtlEditor", null, SurfaceRouting.Product,
			new List<RegionWsValue> { new RegionWsValue("ar", ArabicHouse, "Scheherazade New", 0, rightToLeft: true, wsTag: "ar") },
			null, null);

		[AvaloniaTest]
		public void RtlWritingSystem_GetsRtlFlowDirection_AndArabicTextRoundTripsThroughEditing()
		{
			var context = new FakeRegionEditContext();
			var focusedWs = new List<string>();
			var field = new FwMultiWsTextField(RtlField(), "RtlEditor", context, focusedWs.Add);
			var window = new Window { Content = field, Width = 400, Height = 80 };
			window.Show();
			Dispatcher.UIThread.RunJobs();

			var box = field.GetVisualDescendants().OfType<TextBox>().Single();
			Assert.That(box.FlowDirection, Is.EqualTo(FlowDirection.RightToLeft),
				"the editor takes flow direction from the writing system");
			Assert.That(box.Text, Is.EqualTo(ArabicHouse));

			// Logical-order caret/selection over RTL text.
			box.Focus();
			Dispatcher.UIThread.RunJobs();
			Assert.That(focusedWs, Is.EqualTo(new[] { "ar" }), "per-WS keyboard activation fires on focus");

			box.SelectionStart = 0;
			box.SelectionEnd = box.Text.Length;
			Assert.That(box.SelectedText, Is.EqualTo(ArabicHouse), "selection operates on the logical string");

			// Append a second word (logical order); the edit stages through the context.
			box.Text = ArabicHouse + " " + ArabicBig;
			Dispatcher.UIThread.RunJobs();
			Assert.That(context.TextEdits.Single().Value, Is.EqualTo(ArabicHouse + " " + ArabicBig),
				"Arabic text round-trips the staging path unmangled");
		}
	}
}
