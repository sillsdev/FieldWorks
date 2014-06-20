using System;
using Paratext.LexicalContracts;

namespace SIL.FieldWorks.ParatextLexiconPlugin
{
	/// <summary>
	/// Lexicon unavailable exception
	/// </summary>
	public class LexiconUnavailableException : Exception
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="LexiconUnavailableException"/> class.
		/// </summary>
		/// <param name="message">The message that describes the error.</param>
		public LexiconUnavailableException(string message)
			: base(message)
		{
			this.SetLexiconUnavailableException();
		}
	}
}
