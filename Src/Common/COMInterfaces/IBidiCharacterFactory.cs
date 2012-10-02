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
// File: IBidiCharacterFactory.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows for IBidiCharacter to be mocked out for tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IBidiCharacterFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a Bidi Character object
		/// </summary>
		/// <param name="charDef">The character definition</param>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(CharDef charDef);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a BidiCharacter as it appears in the UCD file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(string line);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new BidiCharacter, copying all the values from <c>puaChar</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(IPuaCharacter puaChar);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new BidiCharacter
		/// </summary>
		/// <param name="codepoint">A string representing the hexadecimal codepoint</param>
		/// <param name="data">The data, as it appears in the file.</param>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter Create(string codepoint, string data);
	}
}