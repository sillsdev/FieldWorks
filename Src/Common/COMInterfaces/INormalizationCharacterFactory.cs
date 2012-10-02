// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: INormalizationCharacterFactory.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows for INormalizationCharacter to be mocked out for tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface INormalizationCharacterFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new Normalization character with the given normalization property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(IPuaCharacter puaChar, string property);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new Normalization character with the given normalization property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(string line, string property);
	}
}