using System;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.NUnit;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;
using Avalonia.VisualTree;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;
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

		var sensesProp = FindByDisplayName(TypeDescriptor.GetProperties(root), "Senses");
		Assert.That(sensesProp, Is.Not.Null);

		var senses = sensesProp!.GetValue(root)!;
		var senseItemProp = FindByDisplayName(TypeDescriptor.GetProperties(senses), "Senses 1");
		Assert.That(senseItemProp, Is.Not.Null);

		var senseItem = senseItemProp!.GetValue(senses)!;
		var senseProps = TypeDescriptor.GetProperties(senseItem);
		Assert.That(FindByDisplayName(senseProps, "Gloss"), Is.Not.Null);
		Assert.That(FindByDisplayName(senseProps, "Examples"), Is.Not.Null);

		var examplesProp = FindByDisplayName(senseProps, "Examples");
		var examples = examplesProp!.GetValue(senseItem)!;
		var exampleItemProp = FindByDisplayName(
			TypeDescriptor.GetProperties(examples),
			"Examples 1"
		);
		Assert.That(exampleItemProp, Is.Not.Null);
	}

	[Test]
	public void DescriptorModel_DuplicateNodeIdsProduceUniquePropertyNames()
	{
		var schema = new PresentationNode[]
		{
			new PresentationField(new PresentationNodeId("Duplicate"))
			{
				Field = "CitationForm",
				Label = "Citation Form",
			},
			new PresentationField(new PresentationNodeId("Duplicate"))
			{
				Field = "LexemeForm",
				Label = "Lexeme Form",
			},
		};
		var staged = new StagedEntryState("LexEntry");
		var root = new StagedObjectView("LexEntry", staged.RootClass, staged.Root, schema);

		var names = TypeDescriptor.GetProperties(root)
			.Cast<PropertyDescriptor>()
			.Select(prop => prop.Name)
			.ToArray();

		Assert.That(names, Is.EqualTo(new[] { "Duplicate", "Duplicate_2" }));
	}

	[Test]
	public void DescriptorModel_FieldDescriptorsExposeDisplayCategoryAndValidationMetadata()
	{
		var schema = new PresentationNode[]
		{
			new PresentationSection(new PresentationNodeId("Identity"))
			{
				Label = "Identity",
				Children = new PresentationNode[]
				{
					new PresentationField(new PresentationNodeId("CitationForm"))
					{
						Field = "CitationForm",
						Label = "Citation Form",
						IsRequired = true,
					},
				},
			},
		};
		var staged = new StagedEntryState("LexEntry");
		var root = new StagedObjectView("LexEntry", staged.RootClass, staged.Root, schema);

		var prop = FindByDisplayName(TypeDescriptor.GetProperties(root), "Citation Form");

		Assert.That(prop, Is.Not.Null);
		Assert.That(prop!.Category, Is.EqualTo("Identity"));
		Assert.That(
			prop.Attributes.Cast<Attribute>().Any(attr => attr.GetType().Name == "RequiredAttribute"),
			Is.True,
			"Required field metadata must survive descriptor projection for validation and accessibility surfaces."
		);
	}

	[Test]
	public void DescriptorModel_SequenceItemsRemainUnmaterializedUntilInspected()
	{
		var schema = new PresentationNode[]
		{
			new PresentationSequence(new PresentationNodeId("Senses"))
			{
				Field = "Senses",
				Label = "Senses",
				ItemTemplate = new PresentationNode[]
				{
					new PresentationField(new PresentationNodeId("Gloss"))
					{
						Field = "Gloss",
						Label = "Gloss",
					},
				},
			},
		};
		var staged = new StagedEntryState("LexEntry");
		var sensesState = staged.Root.GetOrCreateSequence("Senses", "LexSense");
		sensesState.SetCount(2);
		var root = new StagedObjectView("LexEntry", staged.RootClass, staged.Root, schema);

		var sensesProp = FindByDisplayName(TypeDescriptor.GetProperties(root), "Senses");
		var sensesView = sensesProp!.GetValue(root)!;
		var itemProps = TypeDescriptor.GetProperties(sensesView);

		Assert.That(itemProps.Count, Is.EqualTo(2));
		Assert.That(sensesState.TryGetItem(0, out _), Is.False);

		var itemView = itemProps[0].GetValue(sensesView)!;
		Assert.That(sensesState.TryGetItem(0, out _), Is.False,
			"Creating the expandable sequence row must not materialize the staged item.");

		_ = TypeDescriptor.GetProperties(itemView);

		Assert.That(sensesState.TryGetItem(0, out _), Is.True);
		Assert.That(sensesState.TryGetItem(1, out _), Is.False);
	}

	[AvaloniaTest]
	public async Task HeadlessUI_PropertyGrid_Can_Expand_Sense_To_Show_Nested_Item()
	{
		var root = CreateSampleRoot();
		var window = new Window
		{
			Width = 900,
			Height = 700,
			Content = CreatePropertyGrid(root),
		};

		try
		{
			window.Show();
			ForceLayout(window);

			var sensesLabel = await WaitForLabelAsync(window, "Senses");
			var sensesToggle = FindClosestExpanderToggle(sensesLabel);
			Assert.That(
				sensesToggle,
				Is.Not.Null,
				"Expected an expander toggle near the Senses row."
			);

			sensesToggle!.IsChecked = true;
			await WaitForLabelAsync(window, "Senses 1");
		}
		finally
		{
			window.Close();
		}
	}

	private static StagedObjectView CreateSampleRoot()
	{
		var schema = new PresentationNode[]
		{
			new PresentationSequence(new PresentationNodeId("Senses"))
			{
				Field = "Senses",
				Label = "Senses",
				ItemTemplate = new PresentationNode[]
				{
					new PresentationField(new PresentationNodeId("Gloss"))
					{
						Field = "Gloss",
						Label = "Gloss",
					},
					new PresentationSequence(new PresentationNodeId("Examples"))
					{
						Field = "Examples",
						Label = "Examples",
						ItemTemplate = new PresentationNode[]
						{
							new PresentationField(new PresentationNodeId("Example"))
							{
								Field = "Example",
								Label = "Example",
							},
						},
					},
				},
			},
		};

		var staged = new StagedEntryState("LexEntry");
		var senses = staged.Root.GetOrCreateSequence("Senses", "LexSense");
		var sense1 = senses.EnsureItem(0);
		sense1.Fields["Gloss"] = "first sense";

		var examples = sense1.GetOrCreateSequence("Examples", "LexExampleSentence");
		var example1 = examples.EnsureItem(0);
		example1.Fields["Example"] = "This is a sample example.";

		return new StagedObjectView("LexEntry", staged.RootClass, staged.Root, schema);
	}

	private static PropertyDescriptor? FindByDisplayName(
		PropertyDescriptorCollection props,
		string displayName
	)
	{
		foreach (PropertyDescriptor prop in props)
		{
			if (string.Equals(prop.DisplayName, displayName, StringComparison.Ordinal))
				return prop;
		}

		return null;
	}

	private static Control CreatePropertyGrid(object selected)
	{
		EnsureFluentTheme();

		var grid = new global::Avalonia.PropertyGrid.Controls.PropertyGrid
		{
			DataContext = selected,
		};

		var selectedObjectProp = grid.GetType().GetProperty("SelectedObject");
		if (selectedObjectProp is not null && selectedObjectProp.CanWrite)
			selectedObjectProp.SetValue(grid, selected);

		return grid;
	}

	private static async Task<Control> WaitForLabelAsync(Window window, string text)
	{
		for (var attempt = 0; attempt < 20; attempt++)
		{
			await FlushUi();
			ForceLayout(window);

			var label = FindLabelControl(window, text);
			if (label is not null)
				return label;
		}

		Assert.Fail($"Expected the PropertyGrid visual tree to contain label '{text}'.");
		throw new InvalidOperationException("Unreachable after Assert.Fail.");
	}

	private static Task FlushUi() => Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background).GetTask();

	private static void ForceLayout(Window window)
	{
		var width = window.Width > 0 ? window.Width : 900;
		var height = window.Height > 0 ? window.Height : 700;
		window.Measure(new Size(width, height));
		window.Arrange(new Rect(0, 0, width, height));
		window.UpdateLayout();
	}

	private static Control? FindLabelControl(Control root, string text)
	{
		foreach (var control in root.GetVisualDescendants().OfType<Control>())
		{
			if (control is TextBlock textBlock)
			{
				var inlineText = NormalizeLabelText(GetInlineText(textBlock.Inlines));
				if (string.Equals(inlineText, text, StringComparison.Ordinal))
					return control;
			}

			if (
				control is ContentControl contentControl
				&& contentControl.Content is string contentText
				&& string.Equals(NormalizeLabelText(contentText), text, StringComparison.Ordinal))
			{
				return control;
			}

			var textProp = control.GetType().GetProperty("Text");
			if (textProp is not null && textProp.PropertyType == typeof(string))
			{
				var value = NormalizeLabelText(textProp.GetValue(control) as string);
				if (string.Equals(value, text, StringComparison.Ordinal))
					return control;
			}
		}

		return null;
	}

	private static string? GetInlineText(InlineCollection? inlines)
	{
		if (inlines is null || inlines.Count == 0)
			return null;

		return string.Concat(inlines.Select(GetInlineText));
	}

	private static string? GetInlineText(Inline inline) => inline switch
		{
			Run run => run.Text,
			Span span => GetInlineText(span.Inlines),
			_ => null,
		};

	private static string? NormalizeLabelText(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var builder = new StringBuilder(value.Length);
		var inWhitespace = false;
		foreach (var ch in value)
		{
			if (char.IsWhiteSpace(ch))
			{
				if (!inWhitespace)
				{
					builder.Append(' ');
					inWhitespace = true;
				}

				continue;
			}

			inWhitespace = false;
			builder.Append(ch);
		}

		return builder.ToString().Trim();
	}

	private static ToggleButton? FindClosestExpanderToggle(Control label)
	{
		foreach (var ancestor in label.GetVisualAncestors().Take(8))
		{
			var toggle = ancestor.GetVisualDescendants().OfType<ToggleButton>().FirstOrDefault();
			if (toggle is not null)
				return toggle;
		}

		return null;
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