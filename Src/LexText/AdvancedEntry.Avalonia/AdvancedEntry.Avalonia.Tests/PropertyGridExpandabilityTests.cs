using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Documents;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.XmlContract;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.PropertyGrid;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Staging;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class PropertyGridExpandabilityTests
{
	[Test]
	public void DescriptorModel_Exposes_Nested_Sequence_Items()
	{
		var root = CreateSampleRoot();

		var rootProps = TypeDescriptor.GetProperties(root);
		var sensesProp = FindByDisplayName(rootProps, "Senses");
		Assert.That(sensesProp, Is.Not.Null);


		try
		{
			window.Show();
			await WaitForLabelAsync(window, "Senses");

			// Best-effort expand interaction: find a toggle/expander near the 'Senses' label and activate it.
			var sensesLabel = await WaitForLabelAsync(window, "Senses");
			var expanderToggle = FindClosestExpanderToggle(sensesLabel);
			Assert.That(expanderToggle, Is.Not.Null,
				"Expected an expander toggle near 'Senses' (ExpandableObjectConverter should provide one)."
			);

			// Toggle open.
			expanderToggle!.IsChecked = true;
			await WaitForLabelAsync(window, "Senses 1");
		}
		finally
		{
			window.Close();
		}
		await FlushUi();

		// Sanity: row for the Senses property exists.
		Assert.That(FindText(window, "Senses"), Is.True, "Expected the PropertyGrid to render a 'Senses' row.");

		// Best-effort expand interaction: find a toggle/expander near the 'Senses' label and activate it.
		var sensesLabel = FindLabelControl(window, "Senses");
		Assert.That(sensesLabel, Is.Not.Null);

		var expanderToggle = FindClosestExpanderToggle(sensesLabel!);
		Assert.That(expanderToggle, Is.Not.Null,
			"Expected an expander toggle near 'Senses' (ExpandableObjectConverter should provide one).");

		// Toggle open.
		expanderToggle!.IsChecked = true;
		await FlushUi();
		ForceLayout(window);
		await FlushUi();

		// If the control templates are updated asynchronously, re-scan the visual tree.
		Assert.That(FindText(window, "Senses 1"), Is.True,
			"Expected expanding 'Senses' to reveal 'Senses 1' item.");
	}

	[AvaloniaTest]
	public async Task HeadlessUI_PropertyGrid_Can_Expand_Sense_To_Show_Examples_And_Items()
	{
		var root = CreateSampleRoot();

		var window = new Window
		{
			Width = 900,
			Height = 700,
			Content = CreatePropertyGrid(root)
		};

		window.Show();
		await FlushUi();
		ForceLayout(window);
		await FlushUi();

		// Expand Senses
		var sensesLabel = FindLabelControl(window, "Senses");
		Assert.That(sensesLabel, Is.Not.Null);
		var sensesToggle = FindClosestExpanderToggle(sensesLabel!);
		Assert.That(sensesToggle, Is.Not.Null);
		sensesToggle!.IsChecked = true;
		await FlushUi();
		ForceLayout(window);
		await FlushUi();
		Assert.That(FindText(window, "Senses 1"), Is.True);

		// Expand Senses 1
		var sense1Label = FindLabelControl(window, "Senses 1");
		Assert.That(sense1Label, Is.Not.Null);
		var sense1Toggle = FindClosestExpanderToggle(sense1Label!);
		Assert.That(sense1Toggle, Is.Not.Null);
		sense1Toggle!.IsChecked = true;
		await FlushUi();
		ForceLayout(window);
		await FlushUi();
		Assert.That(FindText(window, "Examples"), Is.True,
			"Expected expanding 'Senses 1' to reveal the 'Examples' sequence.");

		// Expand Examples
		var examplesLabel = FindLabelControl(window, "Examples");
		Assert.That(examplesLabel, Is.Not.Null);
		var examplesToggle = FindClosestExpanderToggle(examplesLabel!);
		Assert.That(examplesToggle, Is.Not.Null);
		examplesToggle!.IsChecked = true;
		await FlushUi();
		ForceLayout(window);
		await FlushUi();
		Assert.That(FindText(window, "Examples 1"), Is.True,
			"Expected expanding 'Examples' to reveal 'Examples 1' item.");
	}

	private static Task FlushUi()
	{
		// Give the UI thread a chance to process DataContext updates and templating.
		return Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();
	}

	private static void ForceLayout(Window window)
	{
		var width = window.Width > 0 ? window.Width : 900;
		var height = window.Height > 0 ? window.Height : 700;
		window.Measure(new Size(width, height));
		window.Arrange(new Rect(0, 0, width, height));
		window.UpdateLayout();
	}

	private static StagedObjectView CreateSampleRoot()
	{
		var contract = LoadShippedContractFromRepo();
		var compiler = new PresentationCompiler();
		var ir = compiler.Compile(contract);

		var staged = new StagedEntryState("LexEntry");
		ApplySampleData(staged);

		return new StagedObjectView("LexEntry", ir.RootClass, staged.Root, ir.Children);
	}

				try
				{
					window.Show();

					// Expand Senses
					var sensesLabel = await WaitForLabelAsync(window, "Senses");
					var sensesToggle = FindClosestExpanderToggle(sensesLabel);
					Assert.That(sensesToggle, Is.Not.Null);
					sensesToggle!.IsChecked = true;
					await WaitForLabelAsync(window, "Senses 1");

					// Expand Senses 1
					var sense1Label = await WaitForLabelAsync(window, "Senses 1");
					var sense1Toggle = FindClosestExpanderToggle(sense1Label);
					Assert.That(sense1Toggle, Is.Not.Null);
					sense1Toggle!.IsChecked = true;
					await WaitForLabelAsync(window, "Examples");

					// Expand Examples
					var examplesLabel = await WaitForLabelAsync(window, "Examples");
					var examplesToggle = FindClosestExpanderToggle(examplesLabel);
					Assert.That(examplesToggle, Is.Not.Null);
					examplesToggle!.IsChecked = true;
					await WaitForLabelAsync(window, "Examples 1");
				}
				finally
				{
					window.Close();
				}
		ex1.Fields["Example"] = "This is a sample example.";
	}

	private static PropertyDescriptor? FindByDisplayName(PropertyDescriptorCollection props, string displayName)
	{
		foreach (PropertyDescriptor p in props)
		{
			if (string.Equals(p.DisplayName, displayName, StringComparison.Ordinal))
				return p;
		}
		return null;
	}

	private static bool FindText(Control root, string text) => FindLabelControl(root, text) is not null;

	private static Control? FindLabelControl(Control root, string text)
	{
		foreach (var control in root.GetVisualDescendants().OfType<Control>())
		{
			if (control is TextBlock tb)
			{
				var inlineText = GetInlineText(tb.Inlines);
				if (string.Equals(inlineText, text, StringComparison.Ordinal))
					return control;
			}

			if (control is ContentControl cc && cc.Content is string contentText
				&& string.Equals(contentText, text, StringComparison.Ordinal))
			{
				return control;
			private static bool FindText(Control root, string text) => FindLabelControl(root, text) is not null;

			var textProp = control.GetType().GetProperty("Text");
			if (textProp is not null && textProp.PropertyType == typeof(string))
			{
				var value = textProp.GetValue(control) as string;
				if (string.Equals(value, text, StringComparison.Ordinal))
					return control;
						var inlineText = NormalizeLabelText(GetInlineText(tb.Inlines));
						if (string.Equals(inlineText, text, StringComparison.Ordinal))

		return null;
	}

	private static string? GetInlineText(InlineCollection? inlines)
	{
		if (inlines is null || inlines.Count == 0)
			return null;

		return string.Concat(inlines.Select(i => i switch
		{
			Run r => r.Text,
			_ => null,
		}));
	}

	private static ToggleButton? FindClosestExpanderToggle(Control label)
	{
		// Heuristic: in PropertyGrid templates, the name cell and expander usually share a small ancestor.
		foreach (var ancestor in label.GetVisualAncestors().Take(8))
		{
			private static string? NormalizeLabelText(string? value)
			{
				if (string.IsNullOrWhiteSpace(value))
					return null;

				var sb = new StringBuilder(value.Length);
				var inWs = false;
				foreach (var ch in value)
				{
					if (char.IsWhiteSpace(ch))
					{
						if (!inWs)
						{
							sb.Append(' ');
							inWs = true;
						}
						continue;
					}

					inWs = false;
					sb.Append(ch);
				}

				return sb.ToString().Trim();
			}

			var toggle = ancestor.GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault();
			if (toggle is not null)
				return toggle;
		}

				return string.Concat(inlines.Select(GetInlineText));
		EnsureFluentTheme();

			private static string? GetInlineText(Inline inline)
			{
				return inline switch
				{
					Run r => r.Text,
					Span s => GetInlineText(s.Inlines),
					_ => null,
				};
			}
		var grid = new global::Avalonia.PropertyGrid.Controls.PropertyGrid
		{
			DataContext = selected,
		};

		// Some PropertyGrid implementations use a SelectedObject property instead of DataContext.
		var selectedObjectProp = grid.GetType().GetProperty("SelectedObject");
		if (selectedObjectProp is not null && selectedObjectProp.CanWrite)
			selectedObjectProp.SetValue(grid, selected);

		return grid;
	}

	private static void EnsureFluentTheme()
	{
		var app = Application.Current;
		if (app is null)
			return;

		if (app.Styles.OfType<FluentTheme>().Any())
			return;

		app.Styles.Add(new FluentTheme());
	}
}
