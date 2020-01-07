// Copyright (c) 2002-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// The parser queue priority
	/// </summary>
	public enum ParserPriority
	{
		ReloadGrammarAndLexicon = 0,
		TryAWord = 1,
		High = 2,
		Medium = 3,
		Low = 4
	};
}