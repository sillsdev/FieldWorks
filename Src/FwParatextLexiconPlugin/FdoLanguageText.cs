using System.Diagnostics;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	internal class FdoLanguageText : LanguageText
	{
		/// <summary>
		/// Creates a new LanguageText by wrapping the specified data
		/// </summary>
		public FdoLanguageText(string language, string text)
		{
			Debug.Assert(language.IsNormalized(), "We expect all strings to be normalized composed");
			Debug.Assert(text.IsNormalized(), "We expect all strings to be normalized composed");
			Language = language;
			Text = text;
		}

		#region Implementation of LanguageText
		/// <summary>
		/// Language of the text.
		/// </summary>
		public string Language { get; set; }

		/// <summary>
		/// The actual text.
		/// </summary>
		public string Text { get; set; }
		#endregion
	}
}
