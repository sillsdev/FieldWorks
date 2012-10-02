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
// File: IPuaCharacterFactory.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Allows for IPuaCharacter to be mocked out for tests
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPuaCharacterFactory
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a PUA Character object
		/// </summary>
		/// <param name="charDef">The character definition</param>
		/// ------------------------------------------------------------------------------------
		IPuaCharacter Create(CharDef charDef);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes an empty PUACharacter with just a codepoint.
		/// </summary>
		/// <param name="codepoint">A string representing the hexadecimal codepoint</param>
		/// ------------------------------------------------------------------------------------
		IPuaCharacter Create(string codepoint);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes an empty PUACharacter with just a codepoint.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IPuaCharacter Create(int codepoint);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a PUACharcter.
		/// </summary>
		/// <param name="codepoint">A string representing the hexadecimal codepoint</param>
		/// <param name="data">The data, as it appears in the unicodedata.txt file.
		///		<see cref="IPuaCharacter.Data"/> </param>
		/// ------------------------------------------------------------------------------------
		IPuaCharacter Create(string codepoint, string data);
	}
}
