#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PUACharacter.cs
// Responsibility: Tres London, Zachariah Yoder
//
// <remarks>
// </remarks>
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using System.Diagnostics;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This represents a Private Use Area character.
	/// (Actually, it has begun to be used to represent any Unicode Character)
	/// </summary>
	public class PUACharacter : IComparable
	{
		#region constructors
		/// <summary>
		/// Makes a PUACharcter using the SIL.FieldWorks.Common.FwUtils.CharDef.
		/// </summary>
		/// <param name="puaChar">A <code>CharDef</code> taken from xml file.</param>
		public PUACharacter(CharDef puaChar) : this(puaChar.code, puaChar.data)
		{
		}

		/// <summary>
		/// Makes an empty PUACharacter with just a codepoint.
		/// </summary>
		/// <param name="codepoint"></param>
		public PUACharacter(string codepoint)
		{
			this.CodePoint = codepoint;
		}

		/// <summary>
		/// Makes an empty PUACharacter with just a codepoint.
		/// </summary>
		/// <param name="codepoint"></param>
		public PUACharacter(int codepoint)
		{
			this.CodePoint = codepoint.ToString("x4").ToUpperInvariant();
		}

		/// <summary>
		/// Makes a PUACharcter.
		/// </summary>
		/// <param name="codepoint">The hexadecimal codepoint</param>
		/// <param name="data">The data, as it appears in the unicodedata.txt file.
		///		<see cref="PUACharacter.Data"/> </param>
		public PUACharacter(string codepoint, string data)
		{
			this.CodePoint = codepoint;
			string[] dataProperties = data.Split(new char[] {';'});

			//Check to make sure the right number of properties are passed
			if (dataProperties.Length != 14)
			{
				throw new ArgumentException("Wrong number of unicode data properties. There were " +
					dataProperties.Length + " properties when there should be 14.", "data");
			}

			m_name = dataProperties[0];
			// TODO: fill in the other four UcdProperty values.
			m_generalCategory = UcdProperty.GetInstance(dataProperties[1],
				UcdProperty.UcdCategories.generalCategory);
			m_canonicalCombiningClass = UcdProperty.GetInstance(dataProperties[2],
				UcdProperty.UcdCategories.canonicalCombiningClass);
			m_bidiClass = UcdProperty.GetInstance(dataProperties[3],
				UcdProperty.UcdCategories.bidiClass);
			// TODO: actually split this into two parts, code is already written
			string decompositionTypeTemp = GetDecompositionType(dataProperties[4]);
			m_decompositionType = UcdProperty.GetInstance(decompositionTypeTemp,
				UcdProperty.UcdCategories.compatabilityDecompositionType);
			m_decomposition = GetDecompostion(dataProperties[4]);

			// Set the numeric type based on which entries are empty
			// Decimal Digit: X;X;X;
			if( dataProperties[5].Trim()!= "" )
				m_numericType = UcdProperty.GetInstance(Icu.UNumericType.U_NT_DECIMAL);
				// Digit: ;X;X;
			else if( dataProperties[6].Trim()!= "" )
				m_numericType = UcdProperty.GetInstance(Icu.UNumericType.U_NT_DIGIT);
				// Numeric: ;;X;
			else if(  dataProperties[7].Trim()!= "" )
				m_numericType = UcdProperty.GetInstance(Icu.UNumericType.U_NT_NUMERIC);
				// Not Numeric: ;;;
			else
				m_numericType = UcdProperty.GetInstance(Icu.UNumericType.U_NT_NONE);
			// (There is no 'COUNT' represented here)

			// If there was a numeric type, set its value
			if( m_numericType != UcdProperty.GetInstance(Icu.UNumericType.U_NT_NONE) )
				m_numericValue = dataProperties[7];

			//Set bidiMirrored property, blank is considered to "Y"
			if(dataProperties[8] == "Y")
				m_bidiMirrored = true;
			else
				m_bidiMirrored = false;

			m_unicode1Name = dataProperties[9];
			m_isoComment = dataProperties[10];
			m_upper = dataProperties[11];
			m_title = dataProperties[13];
			m_lower = dataProperties[12];
		}
		/// <summary>
		/// Makes a PUACharacter.
		/// This is useful when you are extending the class so that further constructors
		/// can make sure that we copy all the important data internally.
		/// </summary>
		/// <param name="puaChar">The PUACharacter that we are copying.</param>
		public PUACharacter(PUACharacter puaChar)
		{
			this.Copy(puaChar);
		}

		#endregion

		#region member variables
		private string m_codepoint;
		private string m_name = string.Empty;
		private UcdProperty m_generalCategory;
		private UcdProperty m_canonicalCombiningClass;
		private UcdProperty m_bidiClass;
		private UcdProperty m_decompositionType;
		private string m_decomposition = string.Empty;
		private UcdProperty m_numericType;
		private string m_numericValue = string.Empty;
		private bool m_bidiMirrored;
		private string m_unicode1Name = string.Empty;
		private string m_isoComment = string.Empty;
		private string m_upper = string.Empty;
		private string m_lower = string.Empty;
		private string m_title = string.Empty;

		private static LgIcuCharPropEngine s_charPropEngine;
		#endregion

		#region Historically Underlying Data Attributes
		/// <summary>
		/// The Hexadecimal value of the codepoint represented as a string (field 0)
		/// </summary>
		public string CodePoint
		{
			get{ return m_codepoint; }
			set
			{
				m_codepoint = value;
				// Don't make empty string into "0000"
				if(m_codepoint.Length == 0)
					return;
				// Add on leading zeros until the character is 4 digits long.
				if( m_codepoint.Length < 4 )
					while( m_codepoint.Length < 4 )
						m_codepoint = "0" + m_codepoint;
			}
		}

		/// <summary>
		/// Contains the 12 values separated by ';'s in the unicodedata.txt file.
		/// Represented by an array of strings representing each piece between the ';'s.
		/// </summary>
		public string[] Data
		{
			get
			{
				string [] data = new string[14];
				data[0] = m_name;
				data[1] = m_generalCategory.UcdRepresentation;
				data[2] = m_canonicalCombiningClass.UcdRepresentation;
				data[3] = m_bidiClass.UcdRepresentation;

				// If there is no compat. decomposition type,
				// don't print anything for the decomposition type.
				if( m_decompositionType.UcdRepresentation.IndexOf("<") < 0 )
					data[4] = m_decomposition;
				else
					data[4] = m_decompositionType.UcdRepresentation + " " + m_decomposition;

				data[5] = (m_numericType ==
					UcdProperty.GetInstance(Icu.UNumericType.U_NT_DECIMAL))?m_numericValue:"";
				data[6] = (m_numericType ==
					UcdProperty.GetInstance(Icu.UNumericType.U_NT_DIGIT))?m_numericValue:"";
				data[7] = m_numericValue;
				data[8] = m_bidiMirrored?"Y":"N";
				data[9] = m_unicode1Name;
				data[10] = m_isoComment;
				data[11] = m_upper;
				data[12] = m_lower;
				data[13] = m_title;
				return data;
			}
		}
		#endregion

		#region Unicode Property Attributes
		/// <summary>
		/// The name of puaCharacter (field 1)
		/// </summary>
		public string Name
		{
			get{ return m_name; }
			set{ m_name = value; }
		}


		/// <summary>
		/// The general category of the character, e.g. "Nd" for numeric digit.
		/// (Field 2)
		/// See; http://www.unicode.org/Public/UNIDATA/UCD.html#General_Category_Values
		/// </summary>
		public UcdProperty GeneralCategory
		{
			get { return m_generalCategory; }
			set { m_generalCategory = value; }
		}

		/// <summary>
		/// The canonical combining class, e.g. "8" for hiragana/katakana voicing marks.
		/// (Field 3)
		/// See: http://www.unicode.org/Public/UNIDATA/UCD.html#Canonical_Combining_Class_Values
		/// </summary>
		public UcdProperty CanonicalCombiningClass
		{
			get { return m_canonicalCombiningClass; }
			set { m_canonicalCombiningClass = value; }
		}

		/// <summary>
		/// The uppercase equivelant (field 12)
		/// </summary>
		public string Upper
		{
			get{ return m_upper; }
			set{ m_upper = value; }
		}

		/// <summary>
		/// The lowercase equivelant (field 13)
		/// </summary>
		public string Lower
		{
			get{ return m_lower; }
			set{ m_lower = value; }
		}

		/// <summary>
		/// The titlecase equivelant (field 14)
		/// </summary>
		public string Title
		{
			get{ return m_title; }
			set{ m_title = value; }
		}

		/// <summary>
		/// Whether the Bidi Value is mirrored.
		/// (Field 9)
		/// <c>true</c> is "Y"
		/// </summary>
		public bool BidiMirrored
		{
			get
			{
				return m_bidiMirrored;
			}
			set
			{
				m_bidiMirrored = value;
			}
		}

		/// <summary>
		/// The numeric value (field 8)
		/// http://www.unicode.org/Public/UNIDATA/UCD.html#Numeric_Type
		/// </summary>
		public string NumericValue
		{
			get { return m_numericValue; }
			set { m_numericValue = value; }
		}

		/// <summary>
		/// The numeric type. (i.e. decimal digit, digit, numeric, none, (or count???) )
		/// </summary>
		public UcdProperty NumericType
		{
			get { return m_numericType; }
			set { m_numericType = value; }
		}


		/// <summary>
		/// A quick way to access the Bidi value.
		/// </summary>
		public string Bidi
		{
			get{ return m_bidiClass.UcdRepresentation; }
			set{ m_bidiClass.UcdRepresentation = value; }
		}
		/// <summary>
		/// Gets the Bidirectional Class
		/// </summary>
		public UcdProperty BidiClass
		{
			get { return m_bidiClass; }
			set { m_bidiClass = value; }
		}

		/// <summary>
		/// The compatability decomposition, e.g. "fraction" for vulgar fraction forms.
		/// Note: this is almost the same as DecompositionType, except that this returns the UcdEnumeration form
		/// (Field 5)
		/// See: http://www.unicode.org/Public/UNIDATA/UCD.html#Character_Decomposition_Mappings
		/// </summary>
		public UcdProperty CompatabilityDecomposition
		{
			get { return m_decompositionType; }
			set { m_decompositionType = value; }
		}

		/// <summary>
		/// Returns the decomposition type, found between the "&lt;" and "&gt;" signs.
		/// Returns "" for BOTH no decomposition, and the canonical decomposition,
		///		which has no "&lt;" or "&gt;" signs.
		///	Throws an LDException with ErrorCode.PUADefinitionFormat if the field is not formatted correctly.
		///	(This doesn't guaruntee a complete formatting check, some invalid formats may fail silently.)
		/// </summary>
		public string DecompositionType
		{
			get { return m_decompositionType.UcdRepresentation; }
			set { m_decompositionType.UcdRepresentation = value; }
		}

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
		public string Decomposition
		{
			get { return m_decomposition; }
			set { m_decomposition = value; }

		}


		#endregion

		#region other attributes
		/// <summary>
		/// Returns <c>true</c> if this PUACharacter's properties have not been initialized.
		/// </summary>
		public bool Empty
		{
			get { return m_generalCategory == null; }
		}
		/// <summary>
		/// Returns the codepoint for the character that this represents.
		/// Silently fails and returns a space if the character doesn't parse correctly.
		/// </summary>
		public string Display
		{
			get
			{
				return CodepointAsString(m_codepoint);
			}
			set
			{
				m_codepoint = ((int)value[0]).ToString("x");
			}
		}

		/// <summary>
		/// Returns the unicode character codepoint as an integer.
		/// </summary>
		public int Character
		{
			get
			{
				return System.Convert.ToInt32(m_codepoint,16);
			}
			set
			{
				m_codepoint = ((int)value).ToString("x");
			}
		}

		/// <summary>
		/// Returns a new PUACharacter with the UnicodeDefault
		/// </summary>
		public static PUACharacter UnicodeDefault
		{
			get
			{
				return new PUACharacter("",";Lu;0;L;;;;;N;;;;;");
			}
		}

		/// <summary>
		/// Returns the CharDef representation of this PUACharacter for XMl serialization.
		/// </summary>
		public CharDef CharDef
		{
			get
			{
				return new CharDef(Character, this.ToString().Substring(this.ToString().IndexOf(';')+1));
			}
		}
		#endregion

		#region data related methods

		private string GetDecompositionType(string combinedDecomposition)
		{
			if(combinedDecomposition.Trim()=="")
				// There is no decomposition.
				// Note this return value is identical to there being no decomposition TYPE
				return "";
			else
			{
				int left = combinedDecomposition.IndexOf('<');
				int right = combinedDecomposition.IndexOf('>');
				if(left==-1)
				{
					// catch unpaired ">"
					if(right!=-1)
						throw new Exception("Error while parsing Decomposition.  " +
							"Poorly formed Decomposition");
					else
						// there is no decomposition type, this means that the decomposition is canonical
						return "";
				}
				else
				{
					// catch unpaired "<"
					if(right==-1)
						throw new Exception("Error while parsing Decomposition.  " +
							"Poorly formed Decomposition");
					else
						return combinedDecomposition.Substring(left, right - left + 1).Trim();
				}

			}
		}

		/// <summary>
		/// Gets the "decompostion" without the decomposition type from the "decomposition" field
		/// (field 5) of UnicodeData.txt
		/// This will be a string of hexadecimal numbers separated by spaces, e.g. 0020 064F
		/// </summary>
		/// <example>
		/// From the line:
		/// <code>
		/// &lt;isolated&gt; 0020 064F
		/// </code>
		/// this gets the decomposition:
		/// 0020 064F
		/// </example>
		/// <param name="combinedDecomposition">Field 5 of UnicodeData.txt</param>
		/// <returns></returns>
		private string GetDecompostion(string combinedDecomposition)
		{
			if(combinedDecomposition.Trim()=="")
				// There is no decomposition.
				return "";

			int right = combinedDecomposition.IndexOf('>');
			if(right == -1)
				return combinedDecomposition.Trim();
			else
				return combinedDecomposition.Substring(right+1).Trim();
		}

		/// <summary>
		/// Compares two PUACharacters to see if they are the same.
		/// </summary>
		/// <param name="puaChar">The second PUACharacter to compare with.</param>
		/// <returns><c>true</c> if the characters are identical.</returns>
		public bool Equals(PUACharacter puaChar)
		{
			return
				this.m_codepoint == puaChar.m_codepoint &&
				this.m_name == puaChar.m_name &&
				this.m_generalCategory.Equals(puaChar.m_generalCategory) &&
				this.m_canonicalCombiningClass.Equals(puaChar.m_canonicalCombiningClass) &&
				this.m_bidiClass.Equals(puaChar.m_bidiClass) &&
				this.m_decompositionType.Equals(puaChar.m_decompositionType) &&
				this.m_decomposition == puaChar.m_decomposition &&
				this.m_numericType == puaChar.m_numericType &&
				this.m_numericValue == puaChar.m_numericValue &&
				this.m_bidiMirrored.Equals(puaChar.m_bidiMirrored) &&
				this.m_unicode1Name == puaChar.m_unicode1Name &&
				this.m_isoComment == puaChar.m_isoComment &&
				this.m_upper == puaChar.m_upper &&
				this.m_lower == puaChar.m_lower &&
				this.m_title == puaChar.m_title;
		}


		/// <summary>
		/// Copy data from the given PUACharacter
		/// </summary>
		/// <param name="sourcePuaChar">The character to copy.</param>
		public void Copy(PUACharacter sourcePuaChar)
		{
			this.CodePoint = sourcePuaChar.m_codepoint;
			this.Name = sourcePuaChar.m_name;
			this.m_generalCategory = sourcePuaChar.m_generalCategory;
			this.m_canonicalCombiningClass = sourcePuaChar.m_canonicalCombiningClass;
			this.m_bidiClass = sourcePuaChar.m_bidiClass;
			this.m_decompositionType = sourcePuaChar.m_decompositionType;
			this.m_decomposition = sourcePuaChar.m_decomposition;
			this.m_numericType = sourcePuaChar.m_numericType;
			this.m_numericValue = sourcePuaChar.m_numericValue;
			this.m_bidiMirrored = sourcePuaChar.m_bidiMirrored;
			this.m_unicode1Name = sourcePuaChar.m_unicode1Name;
			this.m_isoComment = sourcePuaChar.m_isoComment;
			this.m_upper = sourcePuaChar.m_upper;
			this.m_lower = sourcePuaChar.m_lower;
			this.m_title = sourcePuaChar.m_title;
		}

		/// <summary>
		/// Update this PUA character to match the codepoint in ICU.
		/// </summary>
		/// <returns>Whether the character was completely loaded from Icu.</returns>
		public bool RefreshFromIcu(bool loadBlankNames)
		{
			InitTheCom();

			// use the codepoint
			int parsedCodepoint = (int)Character;
			if(!IsInRange(m_codepoint, m_validCodepointRanges))
				return false;

			// set the name
			Icu.UErrorCode error;
			Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
			int len = Icu.u_CharName(parsedCodepoint, choice, out m_name, out error);

			// Don't load blank names, unless requested to.
			if(!loadBlankNames && m_name.Length <= 0)
				return false;

			// Set several properties
			m_generalCategory =
				UcdProperty.GetInstance(s_charPropEngine.get_GeneralCategory(parsedCodepoint));
			m_canonicalCombiningClass =
				UcdProperty.GetInstance(s_charPropEngine.get_CombiningClass(parsedCodepoint));
			m_bidiClass =UcdProperty.GetInstance(s_charPropEngine.get_BidiCategory(parsedCodepoint));

			m_decomposition = ConvertToHexString(s_charPropEngine.get_Decomposition(parsedCodepoint));
			m_decompositionType = Icu.GetDecompositionType(parsedCodepoint); 
			m_numericType = Icu.GetNumericType(parsedCodepoint);
			if( m_numericType != UcdProperty.GetInstance(Icu.UNumericType.U_NT_NONE) )
				m_numericValue = decimalToFraction(Icu.u_GetNumericValue(parsedCodepoint));
			else
				m_numericValue = "";

			// Set the bidi mirrored
			BidiMirrored = Icu.u_IsMirrored(parsedCodepoint);

			// Set upper, lower, and title
			m_upper = PUACharacter.ConvertToHexString(s_charPropEngine.get_ToUpperCh(parsedCodepoint));
			m_lower = PUACharacter.ConvertToHexString(s_charPropEngine.get_ToLowerCh(parsedCodepoint));
			m_title = PUACharacter.ConvertToHexString(s_charPropEngine.get_ToTitleCh(parsedCodepoint));

			// We don't have to do the following (if they already exist, we may delete the values)
			//unicode1Name
			m_isoComment = s_charPropEngine.get_Comment(parsedCodepoint);

			return true;
		}

		#endregion

		#region ToString methods

		/// <summary>
		/// Prints the PUACharacter in the UnicodeData.txt format (e.g. witha ';' between each value)
		/// For example:
		/// FBA9;ARABIC LETTER HEH GOAL MEDIAL FORM;Lo;0;AL;&lt;medial&gt; 06C1;;;;N;;;;;
		/// </summary>
		/// <returns></returns>
		override public string ToString()
		{
			string returnString = m_codepoint+";";
			// print all the data elemts up to and including the second to last one
			for(int i=0; i<=Data.Length-2; i++)
				returnString  += Data[i]+";";
			// print the last one without a semi colon
			returnString +=Data[Data.Length-1];
			return returnString;
		}
		/// <summary>
		/// Prints a single DerivedBidiData.txt style line.
		/// e.g.
		/// 00BA          ; L # L&amp;       MASCULINE ORDINAL INDICATOR
		/// </summary>
		/// <returns></returns>
		public string ToBidiString()
		{
			System.IO.StringWriter writer = new System.IO.StringWriter();
			writer.Write("{0,-14}; {1} # {2,-8} {3}",
				this.CodePoint,this.Bidi,this.Data[1],this.Data[0]);
			return writer.ToString();
		}


		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the codepoints of two <code>PUACharacter</code>s to order
		/// them from least to greatest.
		/// This implements IComparable for use in the Array.Sort method which uses
		/// the compareTo method.
		/// If we are given a String, assumme it is a codepoint to compare with.
		/// </summary>
		/// <param name="obj">The PUACharacter or codepoint string to compare with</param>
		/// <returns>1 if greater, -1 if less, 0 if same</returns>
		/// <exception cref="T:System.ArgumentException">obj is not the same type as this
		/// instance. </exception>
		/// ------------------------------------------------------------------------------------
		public int CompareCodePoint(object obj)
		{
			// if they give us a PUACharacter, compare codepoints
			if (obj is PUACharacter)
				return CompareHex(m_codepoint, ((PUACharacter)obj).m_codepoint);
			// if they give us a string, assume its a codepoint
			else if (obj is string)
				return CompareHex(m_codepoint, ((string)obj));
			else
				throw new ArgumentException(
					"Must compare a PUACharacter with a PUACharacter or string", "obj");
		}

		#region IComparable Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the codepoints of two <code>PUACharacter</code>s to order
		/// them from least to greatest.
		/// This implements IComparable for use in the Array.Sort method which uses
		/// the compareTo method.
		/// If we are given a String, assumme it is a codepoint to compare with.
		/// </summary>
		/// <param name="obj">The PUACharacter or codepoint string to compare with</param>
		/// <returns>1 if greater, -1 if less, 0 if same</returns>
		/// <exception cref="T:System.ArgumentException">obj is not the same type as this
		/// instance. </exception>
		/// ------------------------------------------------------------------------------------
		public virtual int CompareTo(object obj)
		{
			return CompareCodePoint(obj);
		}
		#endregion

		#region static methods
		/// <summary>
		/// Whether the character is the first of a surrogate pair.
		///  This was copied from SIL.FieldWorks.IText
		/// </summary>
		/// <param name="ch">The character</param>
		/// <returns></returns>
		public static bool IsLeadSurrogate(char ch)
		{
			const char minLeadSurrogate = '\xD800';
			const char maxLeadSurrogate = '\xDBFF';
			return ch >= minLeadSurrogate && ch <= maxLeadSurrogate;
		}
		/// <summary>
		/// Increment an index into a string, allowing for surrogates.
		///  This was copied from SIL.FieldWorks.IText
		/// </summary>
		/// <param name="st"></param>
		/// <param name="ich"></param>
		/// <returns></returns>
		public static int NextChar(string st, int ich)
		{
			if (IsLeadSurrogate(st[ich]))
				return ich + 2;
			return ich + 1;
		}

		/// <summary>
		/// Return a full 32-bit character value from the surrogate pair.
		///  This was copied from SIL.FieldWorks.IText
		/// </summary>
		/// <param name="ch1"></param>
		/// <param name="ch2"></param>
		/// <returns></returns>
		public static int Int32FromSurrogates(char ch1, char ch2)
		{
			System.Diagnostics.Debug.Assert(IsLeadSurrogate(ch1));
			return ((ch1 - 0xD800) << 10) + ch2 + 0x2400;
		}

		/// <summary>
		/// Returns a string representation of the codepoint, handling surrogate pairs if necessary.
		/// </summary>
		/// <param name="codepoint">The codepoint</param>
		/// <returns>The string representation of the codepoint</returns>
		public static string StringFromCodePoint(int codepoint)
		{
			if(codepoint <= 0xFFFF)
				return new string((char)codepoint,1);

			char codepointH = (char)(((codepoint-0x10000)/0x400) + 0xD800);
			char codepointL = (char)(((codepoint-0x10000)%0x400) + 0xDC00);
			return "" + codepointH + codepointL;
		}

		/// <summary>
		/// Converts a decimal value to its fractional equivelant as it appears in the UnicodeData.txt.
		/// </summary>
		/// <remarks>
		/// http://homepage.smc.edu/kennedy_john/DEC2FRAC.PDF
		///
		///  		Algorithm from:
		///  		John Kennedy
		///  		Mathematics Department
		///  		Santa Monica College
		///  		1900 Pico Blvd.
		///  		Santa Monica, CA 90405
		///  		rkennedy@ix.netcom.com
		/// </remarks>
		/// <param name="decimalValue">The decimal value of a fraction, e.g. .33333333</param>
		/// <returns>A string representation of the fractional value, e.g. "1/3"</returns>
		public static string decimalToFraction(double decimalValue)
		{
			// Allow negative numbers
			if( decimalValue < 0 )
				return "-" + decimalToFraction(-decimalValue);

			int Dprev = 0;
			int D = 1;
			D = decimalToFraction(decimalValue, D, Dprev);
			int N = (int)(decimalValue*D);
			if( D == 1 )
				return N.ToString();
			else
				return N.ToString() + "/" + D.ToString();
		}

		/// <summary>
		/// A recursive helper function used to implement John Kennedy's algorithm
		/// http://homepage.smc.edu/kennedy_john/DEC2FRAC.PDF
		/// </summary>
		/// <returns></returns>
		private static int decimalToFraction(double Z, int D, int Dprev)
		{
			if( ( Z - (int)Z ) < 0.0000001 )
				return D;
			else
			{
				double Znext = (1/(Z - (int)Z));
				int Dnext = D*(int)Znext + Dprev;
				return decimalToFraction(Znext, Dnext, D);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Release the character property engine.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static void ReleaseTheCom()
		{
			if (s_charPropEngine != null)
			{
				Marshal.ReleaseComObject(s_charPropEngine);
				s_charPropEngine = null;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the COM.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void InitTheCom()
		{
			if (s_charPropEngine == null)
				s_charPropEngine = LgIcuCharPropEngineClass.Create();
		}
		#endregion

		#region Hex string manipulation (for use with codepoints)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This acts like a compareTo method when you are dealing with hex numbers as strings
		/// </summary>
		/// <param name="one">hex number one</param>
		/// <param name="two">hex number two</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static int CompareHex(string one, string two)
		{
			one = one.TrimStart(new char[] {'0'});
			two = two.TrimStart(new char[] {'0'});

			if (one.Length==two.Length)
				return one.CompareTo(two);
			else
				return one.Length>two.Length?1:-1;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds a number to a hex string
		/// </summary>
		/// <param name="hex">The hex string you want to add</param>
		/// <param name="num">The number you want to add to the string</param>
		/// <returns>the hex string sum</returns>
		/// ------------------------------------------------------------------------------------
		public static string AddHex(string hex, int num)
		{
			//A long because that's the return type required for ToString
			long sum = Convert.ToInt64(hex,16) + num;
			return Convert.ToString(sum,16).ToUpper();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Subtracts two hex numbers and returns an integer value of their difference.
		/// i.e. returns an long representing hex-hex2
		/// </summary>
		/// <param name="hex">A string value representing a hexadecimal number</param>
		/// <param name="hex2">A string value representing a hexadecimal number</param>
		/// <returns>The difference between the two values.</returns>
		/// ------------------------------------------------------------------------------------
		public static long SubHex(string hex, string hex2)
		{
			//A long because that's the return type required for ToString
			return Convert.ToInt64(hex, 16) - Convert.ToInt64(hex2, 16);
		}


		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts a hex codepoint to the string it represents.
		/// </summary>
		/// <param name="codepoint">The codepoint.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string CodepointAsString(string codepoint)
		{
			try
			{
				if(codepoint.Length > 0)
					return StringFromCodePoint(ConvertToIntegerCodepoint(codepoint));
				else
					return " ";
			}
			catch (FormatException)
			{
				return " ";
			}
			catch (OverflowException)
			{
				return " ";
			}
			catch (Exception e)
			{
				return e.Message;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the hexadecimal string codepoint to it's integer equivelant.
		/// </summary>
		/// <param name="codepoint">The hexadecimal string to convert.</param>
		/// <returns>The Int32 equivelant.</returns>
		/// ------------------------------------------------------------------------------------
		public static int ConvertToIntegerCodepoint(string codepoint)
		{
			return Convert.ToInt32(codepoint, 16);
		}

		/// <summary>
		/// Converts the given codepoint to its hexadecimal value
		/// </summary>
		/// <param name="codepoint">The integer version of the codepoint</param>
		/// <returns></returns>
		public static string ConvertToHexString(int codepoint)
		{
			string hex = codepoint.ToString("x").ToUpper();
			while( hex.Length < 4 )
				hex = "0" + hex;
			return hex;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the given string of characters to a spaces separated string its hexadecimal values
		/// </summary>
		/// <param name="codepoints">The string of unicode characters</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static string ConvertToHexString(string codepoints)
		{
			string returnCodePoints = "";
			foreach( int character in codepoints)
			{
				returnCodePoints += ConvertToHexString(character) + " ";
			}
			if( returnCodePoints.Length == 0 )
				return "";
			else
				return returnCodePoints.Substring(0, returnCodePoints.Length-1);
		}


		private static string[][] m_validCodepointRanges = new string[][]
			{
				new string[] {"0000","10FFFD"}
			};

		// List of the ranges that are acceptable
		private static string[][] m_puaRanges = new string[][] {
			new string[] {"E000","F8FF"},
			new string[] {"F0000","FFFFD"},
			new string[] {"100000","10FFFD"}
		};
		/// <summary>
		/// <c>true</c> when the codepoint is in the Private Use range.
		/// </summary>
		/// <param name="codepoint"></param>
		/// <returns></returns>
		public static bool IsPrivateUse(string codepoint)
		{
			return IsInRange(codepoint, m_puaRanges);
		}

		/// <summary>
		/// <c>true</c> when the PuaCharacter is in the Private Use range.
		/// </summary>
		public bool PrivateUse
		{
			get
			{
				return IsPrivateUse(this.m_codepoint);
			}
		}

		// List of the ranges that are set aside for custom private-use characters
		// The actual ranges of PUA characters in the Unicode standard are E000-F8FF,
		// F0000-FFFFD, and 100000-10FFFD (see IsPrivateUse above).  We don't use the
		// full range because
		// 1) Microsoft has used codepoints from F000-F0FF
		// 2) NRSI wants to reserve F100-F8FF for its own purposes
		// 3) NRSI may prefer us to use plane 15 (F0000-FFFFD), not plane 16 (100000-10FFFD),
		//    but that's not clear, so we're adding plane 16 as of September 15, 2005.  If
		//    there's really a reason why plane 16 was not included here, remove it and
		//    document the reason!
		private static string[][] m_customPuaRanges = new string[][] {
			new string[] {  "E000",  "EFFF"},
			new string[] { "F0000", "FFFFD"},
			new string[] {"100000","10FFFD"},
		};


		/// <summary>
		/// <c>true</c> when the codepoint is in the custom Private Use range.
		/// </summary>
		/// <param name="codepoint"></param>
		/// <returns></returns>
		public static bool IsCustomUse(string codepoint)
		{
			return IsInRange(codepoint, m_customPuaRanges);
		}

		/// <summary>
		/// <c>true</c> when the PuaCharacter is in the custom Private Use range.
		/// </summary>
		public bool CustomUse
		{
			get
			{
				return IsCustomUse(this.m_codepoint);
			}
		}


		// List of the ranges that are set aside for surrogates
		private static string[][] m_surrogateRanges = new string[][] {
																		 new string[] {"D800","DFFF"}
																	 };

		/// <summary>
		/// <c>true</c> when the codepoint is in the surrogate ranges.
		/// </summary>
		/// <param name="codepoint"></param>
		/// <returns></returns>
		public static bool IsSurrogate(string codepoint)
		{
			return IsInRange(codepoint, m_surrogateRanges);
		}

		/// <summary>
		/// <c>true</c> when the PuaCharacter is in the surrogate range.
		/// </summary>
		public bool Surrogate
		{
			get
			{
				return IsSurrogate(this.m_codepoint);
			}
		}


		/// <summary>helper function that returns 'true' when the codepoint is in the given ranges.</summary>
		/// <param name="codepoint"></param><param name="rangesToCheck"></param><returns></returns>
		private static bool IsInRange(string codepoint, string[][] rangesToCheck)
		{
			// The code is not in a range until we find a range that it is in
			foreach( string[] range in rangesToCheck)
				// if range[0] < code < range[1] then code is in the range
				if( PUACharacter.CompareHex(range[0],codepoint) <= 0 &&
					PUACharacter.CompareHex(range[1],codepoint) >= 0 )
					return true;
			return false;
		}

		#endregion
	}

	/// <summary>
	/// See UCDComparer.Compare method
	/// <see cref="UCDComparer.Compare(object, object)"/>
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
	public class UCDComparer : IComparer
	{
		#region IComparer Members

		/// <summary>
		/// The index of the bidi information in the unicode data, zero based.
		/// </summary>
		public const int bidi = 3;

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
		public int Compare(object x, object y)
		{
			if (!(x is PUACharacter) && !(y is PUACharacter))
			{
				throw new ArgumentException(
					"Must be given two PUA/UCDCharacters, or a PUA/UCDCharacter and a string", "x");
			}

			// The objects match if they are the same instance.
			if(x==y)
				return 0;

			// Assume that if we aren't given strings, then we are given something that
			// extends UCDCharacter, or a PUACharacter

			if (y is string)
				{
				PUACharacter puaChar = (PUACharacter)x;
				return puaChar.CompareTo((string)y);
				}
			else if (x is string)
				return -Compare(y, x); // call again with reversed arguments
				else
				{
				PUACharacter puaChar1 = (PUACharacter)x;
				PUACharacter puaChar2 = (PUACharacter)y;
				return puaChar1.CompareTo(puaChar2);
			}
		}

		#endregion
	}

	/// <summary>
	/// Represents a character as it appears in a specific UCD file.
	/// For example, a BidiCharacter extending this class will have a Property that returns the Bidi value,
	///  and ToString that prints the DerivedBidiClass.txt style.
	///
	///  The file is in the format:
	///
	///  <code>codepoint; property [; proper value] [# comment]</code>
	///
	///  Where the comments may be in the following format:
	///
	///  <code># generalCategory uinicodeCharacterName</code>
	///  where <code>generalCategory</code> is UnicodeData.txt field 2
	///  and <code>uinicodeCharacterName</code> is field 1, the full name
	///
	///  Ranges are indicated as follows:
	///  <code>code..code; property [;propery value] # genaralCategory [numberOfPointsInRange] name...name</code>
	///
	///  For the definition of the UCD files see:
	///  http://www.unicode.org/Public/UNIDATA/UCD.html#UCD_File_Format
	///  </summary>
	public abstract class UCDCharacter : PUACharacter
	{
		/// <summary>
		/// Constructs the UCDCharacter based off a UnicodeData.txt style line.
		/// that is:
		/// code;data
		/// (where data is data separated by ';'s)
		/// </summary>
		/// <param name="line"></param>
		public UCDCharacter(string line) : base(
			line.Substring(0,line.IndexOf(';')),
			line.Substring(line.IndexOf(';')+1)
			)
		{
		}

		/// <summary>
		/// Constructs a UCDCharacter based off a copy of the given puaChar.
		/// </summary>
		/// <param name="puaChar"></param>
		public UCDCharacter(PUACharacter puaChar) : base (puaChar)
		{
		}

		/// <summary>
		/// Constructs a UCDCharacter based off a copy of the given puaChar.
		/// </summary>
		/// <param name="puaChar"></param>
		public UCDCharacter(CharDef puaChar) : base (puaChar)
		{
		}

		/// <summary>
		/// Constructs a UCDCharacter based off a copy of the given codepoint and data
		/// </summary>
		/// <param name="codepoint">The codepoint</param>
		/// <param name="data">The data as it appears in UnicodeData.txt</param>
		public UCDCharacter(string codepoint, string data) : base(codepoint,data)
		{
		}

		/// <summary>
		/// Returns the name of the accociated filename.
		/// This DOES NOT include the entire path,
		///		it just includes the name of the specific related file.
		/// </summary>
		public abstract string FileName {get;}

		/// <summary>
		///The line that appears in field one, directly after the codepoint.
		///Technically this could be either the property value or the property name.
		///		property value - when only one property in the entire file
		///		property name - there are several properties in this file.
		///			The value will follow in the next feild.
		/// </summary>
		public abstract string Property {get; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Trims a string. Default implementation just returns the input string.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>The trimmed property.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual string TrimProperty(string property)
		{
			return property;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Conceptually, it makes sense that if you have a PUACharacter and you do .ToString()
		/// it would write a UnicodeData.txt.
		/// Prints a single line as it appears in the file.
		/// e.g.
		/// 00BA          ; L # L&amp;       MASCULINE ORDINAL INDICATO
		/// http://www.unicode.org/Public/UNIDATA/UCD.html#UCD_File_Format
		/// </summary>
		/// <returns>
		/// A formatted string representing a single line of the file.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return string.Format("{0,-14}; {1} # {2,-8} {3}", CodePoint, Property, Data[1],
				Data[0]);
		}

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
		public bool SameRegion(string property1, string property2)
		{
			return TrimProperty(property1) == TrimProperty(property2);
		}

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
		public bool SameRegion(string property)
		{
			return SameRegion(Property, property);
		}

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
		public bool SameRegion(UCDCharacter ucd)
		{
			return SameRegion(ucd.Property);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sorts "matching" Properties into the same region.
		/// This make no guarantee that the Properties will be in the same order as the given file,
		/// just that they will be groups together.
		/// </summary>
		/// <param name="property2">The property2.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected int CompareRegions(string property2)
		{
			return TrimProperty(Property).CompareTo(TrimProperty(property2));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares the Property of this with a property from another UCDCharacter,
		/// or with a string representing a property directly.
		/// </summary>
		/// <param name="obj">The PUACharacter or codepoint string to compare with</param>
		/// <returns>
		/// return is to 0 as this is to obj.
		/// That is, return&gt;0 when this&gt;obj and so on with = and &lt;
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int CompareTo(object obj)
		{
			// if they give us a UCDCharacter, compare Properties
			if (obj is UCDCharacter)
			{
				UCDCharacter ucdChar = obj as UCDCharacter;
				if (SameRegion(ucdChar))
					return CompareHex(CodePoint, ucdChar.CodePoint);
				return CompareRegions(ucdChar.Property);
			}
				// if they give us a string, assume its a codepoint
			else if (obj is string)
				return CompareRegions(((string)obj));
			else if (obj is PUACharacter)
				return base.CompareTo(obj);
			else
				throw new System.ArgumentException(
					"Must compare a UCDCharacter with a UCDCharacter or string","obj");
		}
	}



	#region implementations of UCDCharacter

	/// <summary>
	/// Represents a character as it appears in the file "DerivedBidiClass.txt".
	/// </summary>
	public class BidiCharacter : UCDCharacter
	{
		/// <summary>
		/// Constructs a BidiCharacter as it appears in the UCD file.
		/// </summary>
		/// <param name="line"></param>
		public BidiCharacter(string line) : base(line){}
		/// <summary>
		/// Constructs a new BidiCharacter, copying all the values from <c>puaChar</c>
		/// </summary>
		/// <param name="puaChar"></param>
		public BidiCharacter(PUACharacter puaChar) : base(puaChar){}
		/// <summary>
		/// Constructs a new BidiCharacter, copying all the values from <c>puaChar</c>
		/// </summary>
		/// <param name="puaChar"></param>
		public BidiCharacter(CharDef puaChar) : base (puaChar) {}
		/// <summary>
		/// Constructs a new BidiCharacter
		/// </summary>
		/// <param name="codepoint"></param>
		/// <param name="data"></param>
		public BidiCharacter(string codepoint, string data) : base(codepoint,data){}

		/// <summary>
		/// DerivedBidiClass.txt
		/// The file name associated with this UCDCharacter.
		/// This DOES NOT include the entire path, it just includes the name of the specific related file.
		/// </summary>
		public static string staticFilename
		{
			get
			{
				return "DerivedBidiClass.txt";
			}
		}

		/// <summary>
		/// The file name associated with this UCDCharacter.
		/// This DOES NOT include the entire path, it just includes the name of the specific related file.
		/// </summary>
		public override string FileName
		{
			get
			{
				return BidiCharacter.staticFilename;
			}
		}

		/// <summary>
		/// The Bidi class value.
		/// </summary>
		public override string Property
		{
			get
			{
				return this.Bidi;
			}
		}
	}

	/// <summary>
	/// Represents a character as it appears DerivedNormalizationProps.txt
	/// </summary>
	public class NormalizationCharacter : UCDCharacter
	{
		/// <summary>
		/// The string that represents the normalization property.
		/// e.g. "NFKC_QC"
		/// </summary>
		private string property = string.Empty;

		/// <summary>
		/// Constructs a new Normalization character with the given normalization property.
		/// </summary>
		/// <param name="puaChar"></param>
		/// <param name="property"></param>
		public NormalizationCharacter(PUACharacter puaChar, string property) : base(puaChar)
		{
			this.property = property;
		}
		/// <summary>
		/// Constructs a new Normalization character with the given normalization property.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="property"></param>
		public NormalizationCharacter(string line, string property) : base(line)
		{
			this.property = property;
		}

		/// <summary>
		/// The name of the UCD file associated with this UCDCharacter
		/// </summary>
		public static string staticFilename
		{
			get
			{
				return "DerivedNormalizationProps.txt";
			}
		}
		/// <summary>
		/// The name of the UCD file associated with this UCDCharacter
		/// </summary>
		public override string FileName
		{
			get
			{
				return staticFilename;
			}
		}

		/// <summary>
		/// Returns the normalization property that belongs in field one, with the "no" or "maybe" included.
		/// e.g. "NFKC_NO"
		/// </summary>
		public override string Property
		{
			get
			{
				return property;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Trims a string. Removes "_NO" and "_MAYBE" from the given string.
		/// </summary>
		/// <param name="property">The property.</param>
		/// <returns>The trimmed property.</returns>
		/// ------------------------------------------------------------------------------------
		protected override string TrimProperty(string property)
		{
			// Unicode version 4.1 (ICU version 3.4)
			int n = property.IndexOf("; N");
			if (n != -1)
				return property.Substring(0,n);
			// earlier versions:
			int no = property.IndexOf("_NO");
			if( no!=-1 )
				return property.Substring(0,no);

			int maybe = property.IndexOf("_MAYBE");
			if( maybe!=-1 )
				return property.Substring(0,maybe);
			return property;
		}
	}

	#endregion

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a property from the UnicodeCharacterDatabase
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UcdProperty
	{
		#region member variables
		/// <summary>
		/// The resource manager used to access language dependant strings.
		/// </summary>
		private static System.Resources.ResourceManager m_res =
			new System.Resources.ResourceManager("SIL.FieldWorks.Common.FwUtils.UcdCharacterResources",
			System.Reflection.Assembly.GetExecutingAssembly());

		private UcdCategories m_ucdCategory;
		private string m_description;
		private string m_ucdRepresentation;
		private bool m_displayInUnicodeData = true;
		private static Dictionary<UcdCategories, Dictionary<int, UcdProperty>> s_ucdPropertyDict;
		#endregion

		#region UcdCategories
		/// <summary>
		/// The unicode character database categories.
		/// </summary>
		public enum UcdCategories
		{
			/// <summary>
			/// Don't look up the human readable description
			/// </summary>
			dontParse,
			/// <summary>
			/// General Category (field 2)
			/// </summary>
			generalCategory,
			/// <summary>
			/// The canonical combining class (field 3)
			/// </summary>
			canonicalCombiningClass,
			/// <summary>
			/// Bidirectional Class value (field 4)
			/// </summary>
			bidiClass,
			/// <summary>
			/// The compatiability decomposition type (appears in the tag in field 5)
			/// </summary>
			compatabilityDecompositionType,
			/// <summary>
			/// Bidirectional Mirrored value (field 9)
			/// </summary>
			bidiMirrored,
			/// <summary>
			/// The numeric type (e.g. decimal digit, digit, numeric... )
			/// </summary>
			numericType
		}
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This constructor creates a single enumerated value that contains human readable descriptions
		/// </summary>
		/// <param name="ucdRepresentation">The ucd representation.</param>
		/// <param name="ucdProperty">The ucd property.</param>
		/// ------------------------------------------------------------------------------------
		private UcdProperty(string ucdRepresentation, UcdCategories ucdProperty)
		{
			// Read the human readable value from the resource file.
			switch (ucdProperty)
			{
				case UcdCategories.generalCategory:
					this.m_description = m_res.GetString("kstidGenCat" + ucdRepresentation);
					break;
				case UcdCategories.canonicalCombiningClass:
					this.m_description = m_res.GetString("kstidCanComb" + ucdRepresentation);
					break;
				case UcdCategories.bidiClass:
					this.m_description = m_res.GetString("kstidBidiClass" + ucdRepresentation);
					break;
				case UcdCategories.compatabilityDecompositionType:
					// Cut off the '<' and '>'
					if (ucdRepresentation[0] == '<' && ucdRepresentation[ucdRepresentation.Length - 1] == '>')
					{
						string clipTagChars = ucdRepresentation.Substring(1, ucdRepresentation.Length - 2);
						this.m_description = m_res.GetString("kstidDecompCompat" + clipTagChars);
					}
					else
					{
						// This "ucdRepresentation" should not be written to UnicodeData
						this.m_displayInUnicodeData = false;
						this.m_description = m_res.GetString("kstidDecompCompat" + ucdRepresentation);
					}
					break;
				case UcdCategories.numericType:
					// This "ucdRepresentation" should not be written to UnicodeData
					this.m_displayInUnicodeData = false;
					this.m_description = m_res.GetString("kstidNumType" + ucdRepresentation);
					break;
			}
			this.m_ucdRepresentation = ucdRepresentation;
			this.m_ucdCategory = ucdProperty;
		}
		#endregion

		#region Data members (attributes, and methods)

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the human readable description of what the enumeration means, e.g.
		/// "Numeric Digit". This value should be read from UCDCharacter.resx for ease of
		/// translation.
		/// </summary>
		/// <value>The description.</value>
		/// ------------------------------------------------------------------------------------
		public string Description
		{
			get { return m_description; }
			set { m_description = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the UnicdeCharacterDatabaseEnumeration as it a appears in the file,
		/// e.g. "Nd"
		/// </summary>
		/// <value>The ucd representation.</value>
		/// ------------------------------------------------------------------------------------
		public string UcdRepresentation
		{
			get { return m_ucdRepresentation; }
			set { m_ucdRepresentation = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compares two UcdEnumerations
		/// </summary>
		/// <param name="obj">The object to compare with</param>
		/// <returns>
		/// true if the specified <see cref="T:System.Object"></see> is equal to the current
		/// <see cref="T:System.Object"></see>; otherwise, false.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override bool Equals(object obj)
		{
			if( obj.GetType() != this.GetType() )
				return false;
			else
			{
				UcdProperty ucdEnum = (UcdProperty)obj;
				return
					this.m_description == ucdEnum.m_description &&
					this.m_ucdRepresentation == ucdEnum.m_ucdRepresentation &&
					this.m_ucdCategory == ucdEnum.m_ucdCategory;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Serves as a hash function for a particular type.
		/// <see cref="M:System.Object.GetHashCode"></see> is suitable for use in hashing
		/// algorithms and data structures like a hash table.
		/// </summary>
		/// <returns>
		/// A hash code for the current <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override int GetHashCode()
		{
			return base.GetHashCode() | m_description.GetHashCode() | m_ucdCategory.GetHashCode()
				| m_ucdRepresentation.GetHashCode();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the human readable representation of the string
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.String"></see> that represents the current
		/// <see cref="T:System.Object"></see>.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			// Don't display the ucdRepresentation values that don't appear in UnicodeData.txt
			if( !m_displayInUnicodeData )
				return m_description;
			// Display just UCD representation if there is no description
			if( m_description == null || m_description.Length == 0 )
				return m_ucdRepresentation;
			if( m_ucdCategory == UcdCategories.canonicalCombiningClass )
			{
				return string.Format("{0,3} - {1}", m_ucdRepresentation, m_description);
			}
			return string.Format("{0} - {1}", m_ucdRepresentation, m_description);
		}
		#endregion

		#region static accessor functions
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fills a two-tiered hash table system with all the instances of this class that
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static void InitializeHashTables()
		{
			if( s_ucdPropertyDict == null )
			{
				s_ucdPropertyDict = new Dictionary<UcdCategories, Dictionary<int, UcdProperty>>(5);

				Dictionary<int, UcdProperty> generalCategoryDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.generalCategory,generalCategoryDict);

				Dictionary<int, UcdProperty> canonicalCombiningClassDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.canonicalCombiningClass,canonicalCombiningClassDict);

				Dictionary<int, UcdProperty> bidiClassDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.bidiClass,bidiClassDict);

				Dictionary<int, UcdProperty> compatabilityDecompositionDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.compatabilityDecompositionType, compatabilityDecompositionDict);

				Dictionary<int, UcdProperty> numericTypeDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.numericType,numericTypeDict);

				s_ucdPropertyDict.Add(UcdCategories.bidiMirrored, new Dictionary<int, UcdProperty>());

				#region generalCategory
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccLu,
					new UcdProperty("Lu",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccLl,
					new UcdProperty("Ll",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccLt,
					new UcdProperty("Lt",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccLm,
					new UcdProperty("Lm",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccLo,
					new UcdProperty("Lo",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccMn,
					new UcdProperty("Mn",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccMc,
					new UcdProperty("Mc",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccMe,
					new UcdProperty("Me",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccNd,
					new UcdProperty("Nd",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccNl,
					new UcdProperty("Nl",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccNo,
					new UcdProperty("No",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccPc,
					new UcdProperty("Pc",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccPd,
					new UcdProperty("Pd",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccPs,
					new UcdProperty("Ps",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccPe,
					new UcdProperty("Pe",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccPi,
					new UcdProperty("Pi",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccPf,
					new UcdProperty("Pf",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccPo,
					new UcdProperty("Po",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccSm,
					new UcdProperty("Sm",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccSc,
					new UcdProperty("Sc",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccSk,
					new UcdProperty("Sk",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccSo,
					new UcdProperty("So",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccZs,
					new UcdProperty("Zs",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccZl,
					new UcdProperty("Zl",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccZp,
					new UcdProperty("Zp",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccCc,
					new UcdProperty("Cc",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccCf,
					new UcdProperty("Cf",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccCs,
					new UcdProperty("Cs",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccCo,
					new UcdProperty("Co",UcdCategories.generalCategory));
				generalCategoryDict.Add((int)LgGeneralCharCategory.kccCn,
					new UcdProperty("Cn",UcdCategories.generalCategory));
				#endregion

				#region canonicalCombiningClass
				canonicalCombiningClassDict.Add(0,
					new UcdProperty("0",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(1,
					new UcdProperty("1",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(7,
					new UcdProperty("7",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(8,
					new UcdProperty("8",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(9,
					new UcdProperty("9",UcdCategories.canonicalCombiningClass));
				// Add fixed position classes
				for (int iClass = 10; iClass < 200; iClass++)
				{
					canonicalCombiningClassDict.Add(iClass,
						new UcdProperty(iClass.ToString(), UcdCategories.canonicalCombiningClass));
				}
				canonicalCombiningClassDict.Add(200,
					new UcdProperty("200",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(202,
					new UcdProperty("202",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(204,
					new UcdProperty("204",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(208,
					new UcdProperty("208",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(210,
					new UcdProperty("210",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(212,
					new UcdProperty("212",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(214,
					new UcdProperty("214",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(216,
					new UcdProperty("216",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(218,
					new UcdProperty("218",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(220,
					new UcdProperty("220",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(222,
					new UcdProperty("222",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(224,
					new UcdProperty("224",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(226,
					new UcdProperty("226",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(228,
					new UcdProperty("228",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(230,
					new UcdProperty("230",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(232,
					new UcdProperty("232",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(233,
					new UcdProperty("233",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(234,
					new UcdProperty("234",UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(240,
					new UcdProperty("240",UcdCategories.canonicalCombiningClass));
				#endregion

				#region bidiClass
				bidiClassDict.Add((int)LgBidiCategory.kbicL,
					new UcdProperty("L",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicLRE,
					new UcdProperty("LRE",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicLRO,
					new UcdProperty("LRO",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicR,
					new UcdProperty("R",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicAL,
					new UcdProperty("AL",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicRLE,
					new UcdProperty("RLE",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicRLO,
					new UcdProperty("RLO",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicPDF,
					new UcdProperty("PDF",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicEN,
					new UcdProperty("EN",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicES,
					new UcdProperty("ES",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicET,
					new UcdProperty("ET",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicAN,
					new UcdProperty("AN",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicCS,
					new UcdProperty("CS",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicNSM,
					new UcdProperty("NSM",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicBN,
					new UcdProperty("BN",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicB,
					new UcdProperty("B",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicS,
					new UcdProperty("S",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicWS,
					new UcdProperty("WS",UcdCategories.bidiClass));
				bidiClassDict.Add((int)LgBidiCategory.kbicON,
					new UcdProperty("ON",UcdCategories.bidiClass));
				#endregion

				#region compatabilityDecompositionType
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_NONE,
					new UcdProperty("NONE",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_CANONICAL,
					new UcdProperty("CANONICAL",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_FONT,
					new UcdProperty("<font>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_NOBREAK,
					new UcdProperty("<noBreak>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_INITIAL,
					new UcdProperty("<initial>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_MEDIAL,
					new UcdProperty("<medial>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_FINAL,
					new UcdProperty("<final>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_ISOLATED,
					new UcdProperty("<isolated>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_CIRCLE,
					new UcdProperty("<circle>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SUPER,
					new UcdProperty("<super>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SUB,
					new UcdProperty("<sub>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_VERTICAL,
					new UcdProperty("<vertical>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_WIDE,
					new UcdProperty("<wide>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_NARROW,
					new UcdProperty("<narrow>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SMALL,
					new UcdProperty("<small>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SQUARE,
					new UcdProperty("<square>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_FRACTION,
					new UcdProperty("<fraction>",UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_COMPAT,
					new UcdProperty("<compat>",UcdCategories.compatabilityDecompositionType));
				#endregion

				#region numericType
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_DECIMAL,
					new UcdProperty("DECIMAL",UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_NUMERIC,
					new UcdProperty("NUMERIC",UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_DIGIT,
					new UcdProperty("DIGIT",UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_NONE,
					new UcdProperty("NONE",UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_COUNT,
					new UcdProperty("COUNT",UcdCategories.numericType));
				#endregion
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumeration associated with the given General Category
		/// </summary>
		/// <param name="generalCategory">The UCD general category
		/// see: http://www.unicode.org/Public/UNIDATA/UCD.html#General_Category_Values</param>
		/// <returns>
		/// Returns the only instance that matches the requested value.
		/// Thus two calls will get the same instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static UcdProperty GetInstance(LgGeneralCharCategory generalCategory)
		{
			InitializeHashTables();
			Dictionary<int, UcdProperty> propertyHash = s_ucdPropertyDict[UcdCategories.generalCategory];
			return propertyHash[(int)generalCategory];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumeration associated with the given Canonical Combining class ( field 3 )
		/// </summary>
		/// <param name="combiningClass">see:
		/// http://www.unicode.org/Public/UNIDATA/UCD.html#Canonical_Combining_Class_Values</param>
		/// <returns>
		/// Returns the only instance that matches the requested value.
		/// Thus two calls will get the same instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static UcdProperty GetInstance(int combiningClass)
		{
			InitializeHashTables();
			Dictionary<int, UcdProperty> propertyHash = s_ucdPropertyDict[UcdCategories.canonicalCombiningClass];
			return propertyHash[combiningClass];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumeration associated with the given Bidirection Class
		/// </summary>
		/// <param name="bidiClass">The Bidi Class value</param>
		/// <returns>
		/// Returns the only instance that matches the requested value.
		/// Thus two calls will get the same instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static UcdProperty GetInstance(LgBidiCategory bidiClass)
		{
			InitializeHashTables();
			Dictionary<int, UcdProperty> propertyHash = s_ucdPropertyDict[UcdCategories.bidiClass];
			return propertyHash[(int)bidiClass];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumeration associated with the given Compatability Decomposition Type
		/// </summary>
		/// <param name="decompositionType">The compatability decomposition type</param>
		/// <returns>
		/// Returns the only instance that matches the requested value.
		/// Thus two calls will get the same instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static UcdProperty GetInstance(Icu.UDecompositionType decompositionType)
		{
			InitializeHashTables();
			Dictionary<int, UcdProperty> propertyHash = s_ucdPropertyDict[UcdCategories.compatabilityDecompositionType];
			return propertyHash[(int)decompositionType];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumeration associated with the given numeric type
		/// </summary>
		/// <param name="numericType">Type of the numeric.</param>
		/// <returns>
		/// Returns the only instance that matches the requested value.
		/// Thus two calls will get the same instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static UcdProperty GetInstance(Icu.UNumericType numericType)
		{
			InitializeHashTables();
			Dictionary<int, UcdProperty> propertyHash = s_ucdPropertyDict[UcdCategories.numericType];
			return propertyHash[(int)numericType];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets an enumeration associated with the given General Category
		/// </summary>
		/// <param name="ucdRepresentation">The UCD general category e.g. "Lu" or "Nd"
		/// see: http://www.unicode.org/Public/UNIDATA/UCD.html#General_Category_Values</param>
		/// <param name="ucdCategory">The category that this enumeration represents,
		/// e.g. "generalCategory"</param>
		/// <returns>
		/// The UcdEnumeration that matches the ucdRepresentation, or null.
		/// Returns the only instance that matches the requested value.
		/// Thus two calls will get the same instance.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public static UcdProperty GetInstance(string ucdRepresentation, UcdCategories ucdCategory)
		{
			InitializeHashTables();
			if (ucdCategory != UcdCategories.dontParse)
			{
				Dictionary<int, UcdProperty> propertyHash = s_ucdPropertyDict[ucdCategory];
				foreach (UcdProperty ucdEnum in propertyHash.Values)
				{
					if(ucdEnum.m_ucdRepresentation == ucdRepresentation)
					{
						return ucdEnum;
					}
				}
			}
			else
			{
				// TODO: do we really want to add-non existant values?
				return new UcdProperty(ucdRepresentation, UcdCategories.dontParse);
			}
			// Special case: blank is the same as none for decompostion type
			if (ucdCategory == UcdCategories.compatabilityDecompositionType &&
				ucdRepresentation == "")
			{
				return GetInstance(Icu.UDecompositionType.U_DT_NONE);
			}
			// If the specified a UcdProptery, but none was found, fall through to this error
			throw new ArgumentException("The specified UcdProperty does not exist. " +
				ucdCategory.ToString() + ":" + ucdRepresentation, "ucdProperty");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets all of the possible UcdProperties of the given ucdCategory.
		/// </summary>
		/// <param name="ucdCategory">The ucd category.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static ICollection GetUCDProperty(UcdCategories ucdCategory)
		{
			InitializeHashTables();
			return s_ucdPropertyDict[ucdCategory].Values;
		}

		#endregion
	}

}
