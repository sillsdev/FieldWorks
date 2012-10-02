// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IVerseReference.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;

namespace SILUBS.SharedScrUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a scripture reference
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IVerseReference
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the verse reference as a string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string ToString();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets and sets the current versification scheme
		/// </summary>
		/// ------------------------------------------------------------------------------------
		Paratext.ScrVers Versification { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Parses Scripture reference string.
		/// </summary>
		/// <param name="sTextToBeParsed">Reference string the user types in.</param>
		/// <remarks>This method is pretty similar to MultilingScrBooks.ParseRefString, but
		/// it deals only with SIL codes.</remarks>
		/// ------------------------------------------------------------------------------------
		void Parse(string sTextToBeParsed);
	}
}
