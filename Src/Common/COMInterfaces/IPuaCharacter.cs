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
// File: IPuaCharacter.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Interface for Unicode characters (to allow support for Private Use Area)
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPuaCharacter : IComparable
	{
		/// <summary>
		/// The Hexadecimal value of the codepoint represented as a string (field 0)
		/// </summary>
		string CodePoint { get; set; }

		/// <summary>
		/// A quick way to access the Bidi value.
		/// </summary>
		string Bidi { get; set; }

		/// <summary>
		/// Contains the 12 values separated by ';'s in the unicodedata.txt file.
		/// Represented by an array of strings representing each piece between the ';'s.
		/// </summary>
		string[] Data { get; }

		/// <summary>
		/// Accesses the decompostion value, not including the decomposition type.
		/// Assumes a valid decomposition
		/// That is:
		/// &lt;decompositionType&gt; decompositionCharacters
		/// For example:
		/// "&lt;small&gt; 0030"
		/// or:
		/// "0050 0234"
		/// </summary>
		string Decomposition {get; set; }

		/// <summary>
		/// Returns the decomposition type, found between the "&lt;" and "&gt;" signs.
		/// Returns "" for BOTH no decomposition, and the canonical decomposition,
		///		which has no "&lt;" or "&gt;" signs.
		///	Throws an LDException with ErrorCode.PUADefinitionFormat if the field is not formatted correctly.
		///	(This doesn't guaruntee a complete formatting check, some invalid formats may fail silently.)
		/// </summary>
		string DecompositionType { get; set; }

		/// <summary>
		/// Prints a single DerivedBidiData.txt style line.
		/// e.g.
		/// 00BA          ; L # L&amp;       MASCULINE ORDINAL INDICATOR
		/// </summary>
		/// <returns></returns>
		string ToBidiString();

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the codepoints of two characters to order them from least to greatest.
		/// This implements IComparable for use in the Array.Sort method which uses
		/// the compareTo method.
		/// If we are given a String, assumme it is a codepoint to compare with.
		/// </summary>
		/// <param name="obj">The character or codepoint string to compare with</param>
		/// <returns>1 if greater, -1 if less, 0 if same</returns>
		/// <exception cref="T:System.ArgumentException">obj is not the same type as this
		/// instance. </exception>
		/// ------------------------------------------------------------------------------------
		int CompareCodePoint(object obj);
	}
}
