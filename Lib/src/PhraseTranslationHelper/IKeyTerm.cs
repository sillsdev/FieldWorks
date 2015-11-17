// ---------------------------------------------------------------------------------------------
// Copyright (c) 2011-2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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
		/// Gets the term in the "source" language (i.e., the source of the UNS questions list,
		/// which is in English).
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
