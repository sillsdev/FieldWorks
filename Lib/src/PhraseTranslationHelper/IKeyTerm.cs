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
// File: IKeyTerm.cs
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IKeyTerm
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the term in the source language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Term { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the renderings for the term in the target language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<string> Renderings { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the primary (best) rendering for the term in the target language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string BestRendering { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the references of all occurences of this key term as integers in the form
		/// BBBCCCVVV.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IEnumerable<int> BcvOccurences { get; }
	}
}
