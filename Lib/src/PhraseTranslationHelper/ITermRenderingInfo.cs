// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ITermRenderingInfo.cs
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface to encapsulate what is known about a single occurrence of a key biblical term
	/// and its rendering in a string in the target language.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface ITermRenderingInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the key biblical term object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IKeyTerm Term { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the known renderings for the term in the target language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<string> Renderings { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This will almost always be 0, but if a term occurs more than once in a phrase, this
		/// will be the character offset of the end of the preceding occurrence of the rendering
		/// of the term in the translation string.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int EndOffsetOfRenderingOfPreviousOccurrenceOfThisTerm { get; set; }
	}
}
