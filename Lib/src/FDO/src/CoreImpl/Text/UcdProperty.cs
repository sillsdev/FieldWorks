// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;

namespace SIL.CoreImpl.Text
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Represents a property from the UnicodeCharacterDatabase
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UcdProperty
	{
		#region member variables

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
					this.m_description = UcdCharacterResources.ResourceManager.GetString("kstidGenCat" + ucdRepresentation);
					break;
				case UcdCategories.canonicalCombiningClass:
					this.m_description = UcdCharacterResources.ResourceManager.GetString("kstidCanComb" + ucdRepresentation);
					break;
				case UcdCategories.bidiClass:
					this.m_description = UcdCharacterResources.ResourceManager.GetString("kstidBidiClass" + ucdRepresentation);
					break;
				case UcdCategories.compatabilityDecompositionType:
					// Cut off the '<' and '>'
					if (ucdRepresentation[0] == '<' && ucdRepresentation[ucdRepresentation.Length - 1] == '>')
					{
						string clipTagChars = ucdRepresentation.Substring(1, ucdRepresentation.Length - 2);
						this.m_description = UcdCharacterResources.ResourceManager.GetString("kstidDecompCompat" + clipTagChars);
					}
					else
					{
						// This "ucdRepresentation" should not be written to UnicodeData
						this.m_displayInUnicodeData = false;
						this.m_description = UcdCharacterResources.ResourceManager.GetString("kstidDecompCompat" + ucdRepresentation);
					}
					break;
				case UcdCategories.numericType:
					// This "ucdRepresentation" should not be written to UnicodeData
					this.m_displayInUnicodeData = false;
					this.m_description = UcdCharacterResources.ResourceManager.GetString("kstidNumType" + ucdRepresentation);
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
			// TODO-Linux: System.Boolean System.Type::op_Inequality(System.Type,System.Type) is
			// marked with a [MonoTODO] attribute and might not work as expected in 4.0
			if (obj.GetType() != this.GetType())
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
			if (!m_displayInUnicodeData)
				return m_description;
			// Display just UCD representation if there is no description
			if (m_description == null || m_description.Length == 0)
				return m_ucdRepresentation;
			if (m_ucdCategory == UcdCategories.canonicalCombiningClass)
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
			if (s_ucdPropertyDict == null)
			{
				s_ucdPropertyDict = new Dictionary<UcdCategories, Dictionary<int, UcdProperty>>(5);

				Dictionary<int, UcdProperty> generalCategoryDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.generalCategory, generalCategoryDict);

				Dictionary<int, UcdProperty> canonicalCombiningClassDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.canonicalCombiningClass, canonicalCombiningClassDict);

				Dictionary<int, UcdProperty> bidiClassDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.bidiClass, bidiClassDict);

				Dictionary<int, UcdProperty> compatabilityDecompositionDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.compatabilityDecompositionType, compatabilityDecompositionDict);

				Dictionary<int, UcdProperty> numericTypeDict = new Dictionary<int, UcdProperty>();
				s_ucdPropertyDict.Add(UcdCategories.numericType, numericTypeDict);

				s_ucdPropertyDict.Add(UcdCategories.bidiMirrored, new Dictionary<int, UcdProperty>());

				#region generalCategory
				generalCategoryDict.Add((int)Icu.UCharCategory.U_UPPERCASE_LETTER,
					new UcdProperty("Lu", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_LOWERCASE_LETTER,
					new UcdProperty("Ll", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_TITLECASE_LETTER,
					new UcdProperty("Lt", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_MODIFIER_LETTER,
					new UcdProperty("Lm", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_OTHER_LETTER,
					new UcdProperty("Lo", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_NON_SPACING_MARK,
					new UcdProperty("Mn", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_COMBINING_SPACING_MARK,
					new UcdProperty("Mc", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_ENCLOSING_MARK,
					new UcdProperty("Me", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_DECIMAL_DIGIT_NUMBER,
					new UcdProperty("Nd", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_LETTER_NUMBER,
					new UcdProperty("Nl", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_OTHER_NUMBER,
					new UcdProperty("No", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_CONNECTOR_PUNCTUATION,
					new UcdProperty("Pc", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_DASH_PUNCTUATION,
					new UcdProperty("Pd", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_START_PUNCTUATION,
					new UcdProperty("Ps", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_END_PUNCTUATION,
					new UcdProperty("Pe", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_INITIAL_PUNCTUATION,
					new UcdProperty("Pi", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_FINAL_PUNCTUATION,
					new UcdProperty("Pf", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_OTHER_PUNCTUATION,
					new UcdProperty("Po", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_MATH_SYMBOL,
					new UcdProperty("Sm", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_CURRENCY_SYMBOL,
					new UcdProperty("Sc", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_MODIFIER_SYMBOL,
					new UcdProperty("Sk", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_OTHER_SYMBOL,
					new UcdProperty("So", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_SPACE_SEPARATOR,
					new UcdProperty("Zs", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_LINE_SEPARATOR,
					new UcdProperty("Zl", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_PARAGRAPH_SEPARATOR,
					new UcdProperty("Zp", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_CONTROL_CHAR,
					new UcdProperty("Cc", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_FORMAT_CHAR,
					new UcdProperty("Cf", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_SURROGATE,
					new UcdProperty("Cs", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_PRIVATE_USE_CHAR,
					new UcdProperty("Co", UcdCategories.generalCategory));
				generalCategoryDict.Add((int)Icu.UCharCategory.U_UNASSIGNED,
					new UcdProperty("Cn", UcdCategories.generalCategory));
				#endregion

				#region canonicalCombiningClass
				canonicalCombiningClassDict.Add(0,
					new UcdProperty("0", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(1,
					new UcdProperty("1", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(7,
					new UcdProperty("7", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(8,
					new UcdProperty("8", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(9,
					new UcdProperty("9", UcdCategories.canonicalCombiningClass));
				// Add fixed position classes
				for (int iClass = 10; iClass < 200; iClass++)
				{
					canonicalCombiningClassDict.Add(iClass,
						new UcdProperty(iClass.ToString(), UcdCategories.canonicalCombiningClass));
				}
				canonicalCombiningClassDict.Add(200,
					new UcdProperty("200", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(202,
					new UcdProperty("202", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(204,
					new UcdProperty("204", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(208,
					new UcdProperty("208", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(210,
					new UcdProperty("210", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(212,
					new UcdProperty("212", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(214,
					new UcdProperty("214", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(216,
					new UcdProperty("216", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(218,
					new UcdProperty("218", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(220,
					new UcdProperty("220", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(222,
					new UcdProperty("222", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(224,
					new UcdProperty("224", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(226,
					new UcdProperty("226", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(228,
					new UcdProperty("228", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(230,
					new UcdProperty("230", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(232,
					new UcdProperty("232", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(233,
					new UcdProperty("233", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(234,
					new UcdProperty("234", UcdCategories.canonicalCombiningClass));
				canonicalCombiningClassDict.Add(240,
					new UcdProperty("240", UcdCategories.canonicalCombiningClass));
				#endregion

				#region bidiClass
				bidiClassDict.Add((int)Icu.UCharDirection.U_LEFT_TO_RIGHT,
					new UcdProperty("L", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_LEFT_TO_RIGHT_EMBEDDING,
					new UcdProperty("LRE", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_LEFT_TO_RIGHT_OVERRIDE,
					new UcdProperty("LRO", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_RIGHT_TO_LEFT,
					new UcdProperty("R", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_RIGHT_TO_LEFT_ARABIC,
					new UcdProperty("AL", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_RIGHT_TO_LEFT_EMBEDDING,
					new UcdProperty("RLE", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_RIGHT_TO_LEFT_OVERRIDE,
					new UcdProperty("RLO", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_POP_DIRECTIONAL_FORMAT,
					new UcdProperty("PDF", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_EUROPEAN_NUMBER,
					new UcdProperty("EN", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_EUROPEAN_NUMBER_SEPARATOR,
					new UcdProperty("ES", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_EUROPEAN_NUMBER_TERMINATOR,
					new UcdProperty("ET", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_ARABIC_NUMBER,
					new UcdProperty("AN", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_COMMON_NUMBER_SEPARATOR,
					new UcdProperty("CS", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_DIR_NON_SPACING_MARK,
					new UcdProperty("NSM", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_BOUNDARY_NEUTRAL,
					new UcdProperty("BN", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_BLOCK_SEPARATOR,
					new UcdProperty("B", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_SEGMENT_SEPARATOR,
					new UcdProperty("S", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_WHITE_SPACE_NEUTRAL,
					new UcdProperty("WS", UcdCategories.bidiClass));
				bidiClassDict.Add((int)Icu.UCharDirection.U_OTHER_NEUTRAL,
					new UcdProperty("ON", UcdCategories.bidiClass));
				#endregion

				#region compatabilityDecompositionType
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_NONE,
					new UcdProperty("NONE", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_CANONICAL,
					new UcdProperty("CANONICAL", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_FONT,
					new UcdProperty("<font>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_NOBREAK,
					new UcdProperty("<noBreak>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_INITIAL,
					new UcdProperty("<initial>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_MEDIAL,
					new UcdProperty("<medial>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_FINAL,
					new UcdProperty("<final>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_ISOLATED,
					new UcdProperty("<isolated>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_CIRCLE,
					new UcdProperty("<circle>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SUPER,
					new UcdProperty("<super>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SUB,
					new UcdProperty("<sub>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_VERTICAL,
					new UcdProperty("<vertical>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_WIDE,
					new UcdProperty("<wide>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_NARROW,
					new UcdProperty("<narrow>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SMALL,
					new UcdProperty("<small>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_SQUARE,
					new UcdProperty("<square>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_FRACTION,
					new UcdProperty("<fraction>", UcdCategories.compatabilityDecompositionType));
				compatabilityDecompositionDict.Add((int)Icu.UDecompositionType.U_DT_COMPAT,
					new UcdProperty("<compat>", UcdCategories.compatabilityDecompositionType));
				#endregion

				#region numericType
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_DECIMAL,
					new UcdProperty("DECIMAL", UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_NUMERIC,
					new UcdProperty("NUMERIC", UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_DIGIT,
					new UcdProperty("DIGIT", UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_NONE,
					new UcdProperty("NONE", UcdCategories.numericType));
				numericTypeDict.Add((int)Icu.UNumericType.U_NT_COUNT,
					new UcdProperty("COUNT", UcdCategories.numericType));
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
		public static UcdProperty GetInstance(Icu.UCharCategory generalCategory)
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
		public static UcdProperty GetInstance(Icu.UCharDirection bidiClass)
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
					if (ucdEnum.m_ucdRepresentation == ucdRepresentation)
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
