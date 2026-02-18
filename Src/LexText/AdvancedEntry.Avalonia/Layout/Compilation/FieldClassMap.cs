using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Presentation;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;

internal static class FieldClassMap
{
	public static string? GetItemClass(string ownerClass, string field, GhostSpec? ghost)
	{
		// Prefer explicit ghost class if present (common for LexemeForm and other "ghost" nodes).
		if (!string.IsNullOrWhiteSpace(ghost?.GhostClass))
			return ghost!.GhostClass;

		return ownerClass switch
		{
			"LexEntry" => field switch
			{
				"Senses" => "LexSense",
				"Pronunciations" => "LexPronunciation",
				"Etymology" => "LexEtymology",
				"EntryRefs" => "LexEntryRef",
				_ => null,
			},
			"LexSense" => field switch
			{
				"Examples" => "LexExampleSentence",
				"Senses" => "LexSense",
				_ => null,
			},
			_ => null,
		};
	}
}