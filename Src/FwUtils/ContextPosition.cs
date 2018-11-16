// Copyright (c) 2012-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Enumeration indicating where a punctuation pattern occurs with respect to its context
	/// </summary>
	public enum ContextPosition
	{
		/// <summary>Occurs at the start of a word or paragraph</summary>
		WordInitial,
		/// <summary>Occurs between two words and is word-forming (or in the middle
		/// of a compound word)</summary>
		WordMedial,
		/// <summary>Occurs between two words and is not word-forming (or in the middle
		/// of a compound word)</summary>
		WordBreaking,
		/// <summary>Occurs at the end of a word or paragraph</summary>
		WordFinal,
		/// <summary>Occurs surrounded by whitespace or alone in a paragraph</summary>
		Isolated,
		/// <summary>Undefined</summary>
		Undefined,
	}
}