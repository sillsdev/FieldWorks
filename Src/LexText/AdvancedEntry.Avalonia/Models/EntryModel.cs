using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Models;

public sealed class EntryModel : LcmPropertyModelBase
{
	private string? m_primaryLexicalForm;
	private string? m_citationForm;
	private string? m_morphType;
	private string? m_partOfSpeech;

	[DisplayName("Lexical Form")]
	[Required]
	public string? PrimaryLexicalForm
	{
		get => m_primaryLexicalForm;
		set => SetProperty(ref m_primaryLexicalForm, value);
	}

	[DisplayName("Citation Form")]
	public string? CitationForm
	{
		get => m_citationForm;
		set => SetProperty(ref m_citationForm, value);
	}

	[DisplayName("Morph Type")]
	[Required]
	public string? MorphType
	{
		get => m_morphType;
		set => SetProperty(ref m_morphType, value);
	}

	[DisplayName("Part of Speech")]
	public string? PartOfSpeech
	{
		get => m_partOfSpeech;
		set => SetProperty(ref m_partOfSpeech, value);
	}
}