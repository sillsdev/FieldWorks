// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
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
