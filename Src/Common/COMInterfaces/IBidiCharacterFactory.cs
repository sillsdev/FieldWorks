// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
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