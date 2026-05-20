using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Services;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Staging;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class ValidationServiceTests
{
	[Test]
	public void Validate_SkipsUnmaterializedSequenceItems()
	{
		var layout = new PresentationLayout(new PresentationNodeId("LexEntry-detail-Normal"))
		{
			RootClass = "LexEntry",
			RootType = "detail",
			RootName = "Normal",
			Children = new PresentationNode[]
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
							IsRequired = true,
						},
					},
				},
			},
		};
		var state = new StagedEntryState("LexEntry");
		var senses = state.Root.GetOrCreateSequence("Senses", "LexSense");
		senses.SetCount(2);
		_ = senses.EnsureItem(1);

		var errors = new ValidationService().Validate(layout, state);

		Assert.That(errors.Select(error => error.Path), Is.EqualTo(new[] { "LexEntry.Senses[1].Gloss" }));
	}

	[Test]
	public void Validate_ReturnsDeterministicErrorsInLayoutOrder()
	{
		var layout = new PresentationLayout(new PresentationNodeId("LexEntry-detail-Normal"))
		{
			RootClass = "LexEntry",
			RootType = "detail",
			RootName = "Normal",
			Children = new PresentationNode[]
			{
				new PresentationField(new PresentationNodeId("LexemeForm"))
				{
					Field = "LexemeForm",
					Label = "Lexeme Form",
					IsRequired = true,
				},
				new PresentationField(new PresentationNodeId("CitationForm"))
				{
					Field = "CitationForm",
					Label = "Citation Form",
					IsRequired = true,
				},
			},
		};
		var state = new StagedEntryState("LexEntry");
		var validator = new ValidationService();

		var first = validator.Validate(layout, state);
		var second = validator.Validate(layout, state);

		Assert.That(first, Is.EqualTo(second));
		Assert.That(
			first.Select(error => error.Path),
			Is.EqualTo(new[] { "LexEntry.LexemeForm", "LexEntry.CitationForm" })
		);
	}
}
