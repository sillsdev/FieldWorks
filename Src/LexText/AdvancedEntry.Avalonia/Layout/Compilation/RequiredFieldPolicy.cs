namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Layout.Compilation;

internal static class RequiredFieldPolicy
{
	// This is a minimal policy for early staging/validation.
	// The long-term goal is to drive requiredness from metadata + project configuration.
	public static bool IsRequired(string ownerClass, string field)
	{
		return ownerClass switch
		{
			"MoStemAllomorph" => field switch
			{
				"Form" => true,
				_ => false,
			},
			"LexSense" => field switch
			{
				"Gloss" => true,
				_ => false,
			},
			_ => false,
		};
	}
}