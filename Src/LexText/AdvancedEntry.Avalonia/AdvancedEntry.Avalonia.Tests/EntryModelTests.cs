using NUnit.Framework;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Models;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Tests;

[TestFixture]
public sealed class EntryModelTests
{
	[Test]
	public void PrimaryLexicalForm_RaisesPropertyChanged()
	{
		var model = new EntryModel();
		string? lastProperty = null;
		model.PropertyChanged += (_, e) => lastProperty = e.PropertyName;

		model.PrimaryLexicalForm = "abc";

		Assert.That(lastProperty, Is.EqualTo(nameof(EntryModel.PrimaryLexicalForm)));
	}
}
