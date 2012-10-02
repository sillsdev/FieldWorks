// --------------------------------------------------------------------------------------------
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
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Runtime.InteropServices;
using SIL.Utils;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.CoreImpl
{
	/// <summary>
	/// This represents a Private Use Area character.
	/// (Actually, it has begun to be used to represent any Unicode Character)
	/// </summary>
	public class PUACharacter : IPuaCharacter
	{
		#region constructors
		/// <summary>
		/// Makes a PUACharcter using the SIL.FieldWorks.Common.Utils.CharDef.
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
			string[] dataProperties = data.Split(';');

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
		public PUACharacter(IPuaCharacter puaChar)
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
				return Convert.ToInt32(m_codepoint, 16);
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
		/// <param name="sourceIPuaChar">The character to copy.</param>
		public void Copy(IPuaCharacter sourceIPuaChar)
		{
			PUACharacter sourcePuaChar = (PUACharacter)sourceIPuaChar;
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

			if(!Icu.IsValidCodepoint(m_codepoint))
				return false;

			// use the codepoint
			int parsedCodepoint = Character;

			// set the name
			Icu.UErrorCode error;
			Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
			Icu.u_CharName(parsedCodepoint, choice, out m_name, out error);

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
			using (var writer = new System.IO.StringWriter())
			{
				writer.Write("{0,-14}; {1} # {2,-8} {3}",
					this.CodePoint,this.Bidi,this.Data[1],this.Data[0]);
				return writer.ToString();
			}
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
				return MiscUtils.CompareHex(m_codepoint, ((PUACharacter)obj).m_codepoint);
			// if they give us a string, assume its a codepoint
			else if (obj is string)
				return MiscUtils.CompareHex(m_codepoint, ((string)obj));
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
		/// Converts a decimal value to its fractional equivalent as it appears in the UnicodeData.txt.
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
					return Surrogates.StringFromCodePoint(ConvertToIntegerCodepoint(codepoint));
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the given codepoint to its hexadecimal value
		/// </summary>
		/// <param name="codepoint">The integer version of the codepoint</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string ConvertToHexString(int codepoint)
		{
			return codepoint.ToString("x4").ToUpper();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Converts the given string of characters to a spaces separated string its hexadecimal values
		/// </summary>
		/// <param name="codepoints">The string of unicode characters</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private static string ConvertToHexString(string codepoints)
		{
			string returnCodePoints = "";
			foreach( int character in codepoints)
				returnCodePoints += ConvertToHexString(character) + " ";

			return (returnCodePoints.Length == 0) ? string.Empty :
				returnCodePoints.Substring(0, returnCodePoints.Length-1);
		}

		/// <summary>
		/// <c>true</c> when the codepoint is in the Private Use range.
		/// </summary>
		/// <param name="codepoint"></param>
		/// <returns></returns>
		public static bool IsPrivateUse(string codepoint)
		{
			return Icu.IsPrivateUse(codepoint);
		}

		/// <summary>
		/// <c>true</c> when the PuaCharacter is in the custom Private Use range.
		/// </summary>
		public bool CustomUse
		{
			get { return Icu.IsCustomUse(this.m_codepoint); }
		}

		/// <summary>
		/// <c>true</c> when the PuaCharacter is in the surrogate range.
		/// </summary>
		public bool Surrogate
		{
			get
			{
				return Icu.IsSurrogate(this.m_codepoint);
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
	public abstract class UCDCharacter : PUACharacter, IUcdCharacter
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
		public UCDCharacter(IPuaCharacter puaChar) : base (puaChar)
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
		public bool SameRegion(IUcdCharacter ucd)
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
					return MiscUtils.CompareHex(CodePoint, ucdChar.CodePoint);
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
		public BidiCharacter(IPuaCharacter puaChar) : base(puaChar){}
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
		/// The file name associated with this UCDCharacter.
		/// This DOES NOT include the entire path, it just includes the name of the specific related file.
		/// </summary>
		public override string FileName
		{
			get
			{
				return "DerivedBidiClass.txt";
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
		public NormalizationCharacter(IPuaCharacter puaChar, string property) : base(puaChar)
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
		public override string FileName
		{
			get
			{
				return "DerivedNormalizationProps.txt";
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
	/// ----------------------------------------------------------------------------------------
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
			if (!(x is IPuaCharacter) && !(y is IPuaCharacter))
			{
				throw new ArgumentException(
					"Must be given two PUA/UCDCharacters, or a PUA/UCDCharacter and a string", "x");
			}

			// The objects match if they are the same instance.
			if (x == y)
				return 0;

			// Assume that if we aren't given strings, then we are given something that
			// extends UCDCharacter, or a PUACharacter

			if (y is string)
			{
				IPuaCharacter puaChar = (IPuaCharacter)x;
				return puaChar.CompareTo((string)y);
			}
			else if (x is string)
				return -Compare(y, x); // call again with reversed arguments
			else
			{
				IPuaCharacter puaChar1 = (IPuaCharacter)x;
				IPuaCharacter puaChar2 = (IPuaCharacter)y;
				return puaChar1.CompareTo(puaChar2);
			}
		}

		#endregion
	}
}
