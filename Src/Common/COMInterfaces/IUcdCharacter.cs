// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: IUcdCharacter.cs
// Responsibility: TE Team

using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;

namespace SIL.FieldWorks.Common.COMInterfaces
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IUcdCharacter : IPuaCharacter
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the name of the accociated filename.
		/// This DOES NOT include the entire path,
		///		it just includes the name of the specific related file.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string FileName { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///The line that appears in field one, directly after the codepoint.
		///Technically this could be either the property value or the property name.
		///		property value - when only one property in the entire file
		///		property name - there are several properties in this file.
		///			The value will follow in the next feild.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string Property { get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two strings that represent properties.
		/// If the two are in the same "region" sorted as though they are identical, they will "match".
		/// For most files this will just be as simple "equals" comparing the strings.
		/// However, for the DerivedNormalizationProps.txt file,
		///		both NFC_NO and NFC_MAYBE are the in the same region.
		/// </summary>
		/// <param name="property1">The property1.</param>
		/// <param name="property2">The property2.</param>
		/// <returns>
		/// True if the properties are in the same "region" sorted as though they are identical in a file.
		///		</returns>
		/// ------------------------------------------------------------------------------------
		bool SameRegion(string property1, string property2);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two strings that represent properties.
		/// If the two are in the same "region" sorted as though they are identical, they will "match".
		/// For most files this will just be as simple "equals" comparing the strings.
		/// However, for the DerivedNormalizationProps.txt file, both NFC_NO and NFC_MAYBE are the in the same region.
		/// </summary>
		/// <param name="property">The property to compare this with</param>
		/// <returns>
		/// True if the properties are in the same "region" sorted as though they are identical in a file.
		///		</returns>
		/// ------------------------------------------------------------------------------------
		bool SameRegion(string property);

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two UCDCharacters.
		/// If the two are in the same "region" sorted as though they are identical, they will "match".
		/// For most files this will just be as simple "equals" comparing the strings.
		/// However, for the DerivedNormalizationProps.txt file,
		/// both NFC_NO and NFC_MAYBE are the in the same region.
		/// </summary>
		/// <param name="ucd">The ucd.</param>
		/// <returns>
		/// True if the properties are in the same "region" sorted as though they are identical in a file.
		///		</returns>
		/// ------------------------------------------------------------------------------------
		bool SameRegion(IUcdCharacter ucd);
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// See UCDComparer.Compare method
	/// <see cref="IUCDComparer.Compare(IUcdCharacter, IUcdCharacter)"/>
	///
	/// Compares two PUACharacters by their Bidi information, the fourth element in the data.
	/// It also performs a secondary sort on the codepoints.
	/// </summary>
	/// <example>
	/// Note this can be used to sort an array of PUACharacters using the following code:
	///
	/// <code>
	/// PUACharacter[] puaCharacterArray;
	/// UCDComparer UCDComparer;
	/// System.Array.Sort(puaCharacterArray, UCDComparer);
	/// </code>
	/// The following would be the order of the actual sorted output of some sample code:
	/// <code>
	///		data="PIG NUMERAL 7;Ll;0;A;;;;;N;;;;;" code="2000"
	///		data="PIG NUMERAL 8;Ll;0;A;;;;;N;;;;;" code="2001"
	///		data="PIG NUMERAL 1;Ll;0;B;;;;;N;;;;;" code="1C01"
	///		data="PIG NUMERAL 2;Ll;0;B;;;;;N;;;;;" code="1C02"
	///		data="PIG NUMERAL 3;Ll;0;B;;;;;N;;;;;" code="1EEE"
	///		data="PIG NUMERAL 5;Ll;0;B;;;;;N;;;;;" code="1FFE"
	///		data="PIG NUMERAL 4;Ll;0;C;;;;;N;;;;;" code="1EEF"
	///		data="PIG NUMERAL 6;Ll;0;C;;;;;N;;;;;" code="1FFF"
	/// </code>
	/// Notice that the main sort is the fourth column, the bidi column.  The secondary sort is the codes.
	/// </example>
	/// ----------------------------------------------------------------------------------------
	public class IUCDComparer : IComparer<IUcdCharacter>
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two UCDCharacters by their "Property" information.
		/// It also performs a secondary sort on the codepoints.
		/// </summary>
		/// <param name="x">First UCDCharacter</param>
		/// <param name="y">Second UCDCharacter</param>
		/// <returns>1 if greater, -1 if less, 0 if same</returns>
		/// <example>
		/// Note this can be used to sort and array of PUACharacters using the following code:
		/// <code>
		/// BidiCharacter[] bidiCharacterArray;
		/// UCDComparer UCDComparer;
		/// System.Array.Sort(bidiCharacterArray, UCDComparer);
		/// </code>
		/// If this were extended such that Property was the Bidi value, the fourth element in
		/// the data, the following would be the order of the actual sorted output of some
		/// sample code:
		/// <code>
		///     Bidi value (Property)----|     Code points ----|
		///                              V                     V
		///		data="PIG NUMERAL 7;Ll;0;A;;;;;N;;;;;" code="2000"
		///		data="PIG NUMERAL 8;Ll;0;A;;;;;N;;;;;" code="2001"
		///		data="PIG NUMERAL 1;Ll;0;B;;;;;N;;;;;" code="1C01"
		///		data="PIG NUMERAL 2;Ll;0;B;;;;;N;;;;;" code="1C02"
		///		data="PIG NUMERAL 3;Ll;0;B;;;;;N;;;;;" code="1EEE"
		///		data="PIG NUMERAL 5;Ll;0;B;;;;;N;;;;;" code="1FFE"
		///		data="PIG NUMERAL 4;Ll;0;C;;;;;N;;;;;" code="1EEF"
		///		data="PIG NUMERAL 6;Ll;0;C;;;;;N;;;;;" code="1FFF"
		/// </code>
		/// Notice that the main sort is the fourth column, the bidi column.  The secondary sort
		/// is the codes.
		/// </example>
		/// ------------------------------------------------------------------------------------
		public int Compare(IUcdCharacter x, IUcdCharacter y)
		{
			// The objects match if they are the same instance.
			if (x == y)
				return 0;

			if (x == null)
				return -1;

			if (y == null)
				return 1;

			return x.CompareTo(y);
		}
	}
}