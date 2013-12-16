// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PuaCharacterFactory.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of IPuaCharacterFactory that creates real PuaCharacters for production
	/// purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class PuaCharacterFactory : IPuaCharacterFactory
	{
		#region IPuaCharacterFactory Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a PUACharcter.
		/// </summary>
		/// <param name="codepoint">A string representing the hexadecimal codepoint</param>
		/// <param name="data">The data, as it appears in the unicodedata.txt file.
		///		<see cref="IPuaCharacter.Data"/> </param>
		/// ------------------------------------------------------------------------------------
		public IPuaCharacter Create(string codepoint, string data)
		{
			return new PUACharacter(codepoint, data);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes an empty PUACharacter with just a codepoint.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IPuaCharacter Create(int codepoint)
		{
			return new PUACharacter(codepoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes an empty PUACharacter with just a codepoint.
		/// </summary>
		/// <param name="codepoint">A string representing the hexadecimal codepoint</param>
		/// ------------------------------------------------------------------------------------
		public IPuaCharacter Create(string codepoint)
		{
			return new PUACharacter(codepoint);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a PUA Character object
		/// </summary>
		/// <param name="charDef">The character definition</param>
		/// ------------------------------------------------------------------------------------
		public IPuaCharacter Create(CharDef charDef)
		{
			return new PUACharacter(charDef);
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of IBidiCharacterFactory that creates real BidiCharacters for production
	/// purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class BidiCharacterFactory : IBidiCharacterFactory
	{
		#region IBidiCharacterFactory Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new BidiCharacter
		/// </summary>
		/// <param name="codepoint">A string representing the hexadecimal codepoint</param>
		/// <param name="data">The data, as it appears in the XML definition file.</param>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter IBidiCharacterFactory.Create(string codepoint, string data)
		{
			return new BidiCharacter(codepoint, data);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a new BidiCharacter, copying all the values from <c>puaChar</c>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IUcdCharacter Create(IPuaCharacter puaChar)
		{
			return new BidiCharacter(puaChar);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a BidiCharacter as it appears in the UCD file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter IBidiCharacterFactory.Create(string line)
		{
			return new BidiCharacter(line);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a Bidi Character object
		/// </summary>
		/// <param name="charDef">The character definition</param>
		/// ------------------------------------------------------------------------------------
		IUcdCharacter IBidiCharacterFactory.Create(CharDef charDef)
		{
			return new BidiCharacter(charDef);
		}
		#endregion
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implementation of INormalizationCharacterFactory that creates real
	/// NormalizationCharacters for production purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class NormalizationCharacterFactory : INormalizationCharacterFactory
	{
		#region INormalizationCharacterFactory Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new Normalization character with the given normalization property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IUcdCharacter Create(string line, string property)
		{
			return new NormalizationCharacter(line, property);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new Normalization character with the given normalization property.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IUcdCharacter Create(IPuaCharacter puaChar, string property)
		{
			return new NormalizationCharacter(puaChar, property);
		}
		#endregion
	}
}
