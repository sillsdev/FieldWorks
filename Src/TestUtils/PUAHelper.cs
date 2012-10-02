// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: PUAHelper.cs
// Responsibility:
// ---------------------------------------------------------------------------------------------
using System.IO;
using System.Runtime.InteropServices;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// Contains several methods that help unit testing PUA characters
	/// ----------------------------------------------------------------------------------------
	public static class PUAHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the character name at the memory address specified.
		/// Will assert an error if the PUA codepoint name is not correct.
		/// </summary>
		/// <param name="puaIndex">Unicode codepoint</param>
		/// <param name="puaName">Expected correct PUA codepoint name</param>
		/// <param name="puaGenCat">The expected PUA General Category</param>
		/// ------------------------------------------------------------------------------------
		public static void Check_PUA(int puaIndex, string puaName, LgGeneralCharCategory puaGenCat)
		{
			string name = string.Empty;
			LgGeneralCharCategory genCategory = LgGeneralCharCategory.kccCn;

			//Getting the character name at the memory address specified
			ILgCharacterPropertyEngine charPropEngine = LgIcuCharPropEngineClass.Create();
			try
			{
				Icu.UErrorCode error;
				Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
				Icu.u_CharName(puaIndex, choice, out name, out error);
				genCategory = charPropEngine.get_GeneralCategory(puaIndex);
			}
			finally
			{
				// Must release pointer to free memory-mapping before we try to restore files.
				Marshal.ReleaseComObject(charPropEngine);
				charPropEngine = null;
				Icu.Cleanup();		// clean up the ICU files / data
			}

			//Check to make sure expected result is the same as actual result, if not, output error
			Assert.AreEqual(puaName, name, "PUA Character " +
				puaIndex.ToString("x",new System.Globalization.NumberFormatInfo()) +
				" is incorrect");

			//Check to make sure expected result is the same as actual result, if not, output error
			Assert.AreEqual(puaGenCat, genCategory, "PUA Character " +
				puaIndex.ToString("x",new System.Globalization.NumberFormatInfo()) +
				" has an incorrect digit value");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks all the values of a character in the UnicodeData.txt.
		/// Checks: fields 1-8,11-14
		/// (Skips, 9 and 10, the "Bidi Mirrored" and "Unicode Version 1"
		/// </summary>
		/// <param name="puaIndex"></param><param name="puaName"></param>
		/// <param name="puaGenCat"></param><param name="puaCombiningClass"></param>
		/// <param name="puaBidiClass"></param><param name="puaDecomposition"></param>
		/// <param name="puaNumeric"></param><param name="puaNumericValue"></param>
		/// <param name="puaComment"></param><param name="puaToUpper"></param>
		/// <param name="puaToLower"></param><param name="puaToTitle"></param>
		/// ------------------------------------------------------------------------------------
		public static void Check_PUA(
			int puaIndex,
			string puaName,
			LgGeneralCharCategory puaGenCat,
			int puaCombiningClass,
			LgBidiCategory puaBidiClass,
			string puaDecomposition,
			bool puaNumeric,
			int puaNumericValue,
			string puaComment,
			int puaToUpper,
			int puaToLower,
			int puaToTitle
			)
		{
			string name = "";
			LgGeneralCharCategory genCategory = LgGeneralCharCategory.kccCn;
			int combiningClass = 0;
			string decomposition = "None";
			LgBidiCategory bidiCategory = LgBidiCategory.kbicL;
			//string fullDecomp = "I have no clue";
			bool isNumber = false;
			int numericValue = -1;
			int upper = -1;
			int lower = -1;
			int title = -1;
			string comment = "<none>";

			//Getting the character name at the memory address specified
			ILgCharacterPropertyEngine charPropEngine = LgIcuCharPropEngineClass.Create();
			try
			{
				Icu.UErrorCode error;
				Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
				Icu.u_CharName(puaIndex, choice, out name, out error);
				genCategory = charPropEngine.get_GeneralCategory(puaIndex);
				combiningClass = charPropEngine.get_CombiningClass(puaIndex);
				bidiCategory = charPropEngine.get_BidiCategory(puaIndex);
				decomposition = charPropEngine.get_Decomposition(puaIndex);
				//fullDecomp = charPropEngine.get_FullDecomp(puaIndex);
				// Note: isNumber merely checks the General category, it doesn't check to see if there is a valid numeric value.
				isNumber = charPropEngine.get_IsNumber(puaIndex);
				if(isNumber)
					numericValue = charPropEngine.get_NumericValue(puaIndex);
				comment = charPropEngine.get_Comment(puaIndex);

				upper = charPropEngine.get_ToUpperCh(puaIndex);
				lower = charPropEngine.get_ToLowerCh(puaIndex);
				title = charPropEngine.get_ToTitleCh(puaIndex);
			}
			finally
			{
				// Must release pointer to free memory-mapping before we try to restore files.
				Marshal.ReleaseComObject(charPropEngine);
				charPropEngine = null;
				Icu.Cleanup();		// clean up the ICU files / data
			}

			// StringWriter used to print hexadecimal values in the error messages.
			using (var stringWriter = new StringWriter(new System.Globalization.NumberFormatInfo()))
			{
				string errorMessage = "PUA Character " +
					puaIndex.ToString("x",new System.Globalization.NumberFormatInfo()) +
					" has an incorrect ";

				//Check Name [1]
				Assert.AreEqual(puaName, name, errorMessage + "name.");

				//Check general category [2]
				Assert.AreEqual(puaGenCat, genCategory, errorMessage + "general category.");

				//Check combining class [3]
				Assert.AreEqual(puaCombiningClass, combiningClass, errorMessage + "combining class.");

				//Check Bidi class [4]
				Assert.AreEqual(puaBidiClass, bidiCategory, errorMessage + "bidi class value.");

				//Check Decomposition [5]
				stringWriter.WriteLine(errorMessage + "decomposition.");
				stringWriter.WriteLine("Decomposition, {0:x}, is incorrect",(int)decomposition[0]);
				Assert.AreEqual(puaDecomposition, decomposition, stringWriter.ToString());

				//Check Numeric Value [6,7,8]
				if(puaNumeric != isNumber)
					Assert.AreEqual(puaNumeric,isNumber,errorMessage +
						"numeric type (i.e. does or doesn't have a numeric value when it should be the other).");
				if(puaNumeric)
					Assert.AreEqual(puaNumericValue, numericValue, errorMessage + "numeric value.");
				//Check ISO Comment [11]
				Assert.AreEqual(puaComment,comment, errorMessage + "ISO commment");

				//Check uppercase [12]
				stringWriter.Flush();
				stringWriter.WriteLine(errorMessage + "upper case.");
				stringWriter.WriteLine("Found uppercase value: {0:x}",upper);
				Assert.AreEqual(puaToUpper,upper, stringWriter.ToString());
				//Check lowercase [13]
				Assert.AreEqual(puaToLower,lower, errorMessage + "lower case.");
				//Check titlecase [14]
				Assert.AreEqual(puaToTitle,title, errorMessage + "title case.");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the character name at the memory address specified.
		/// Will assert an error if the PUA codepoint name and digit value are not correct.
		/// </summary>
		/// <param name="puaIndex">Unicode codepoint</param>
		/// <param name="digit">Expected correct PUA codepoint name</param>
		/// ------------------------------------------------------------------------------------
		public static void Check_PUA_Digit(int puaIndex, int digit)
		{
			string name = "";
			int icuDigit = -1;

			//Getting the character name at the memory address specified
			Icu.UErrorCode error;
			Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
			Icu.u_CharName(puaIndex, choice, out name, out error);
			// Radix means "base", so this will return the base 10 value of this digit.
			// (Note, the radix is just used to return an error if the digit isn't valid in the given radix)
			icuDigit = Icu.u_Digit(puaIndex,10);

			//Check to make sure expected result is the same as actual result, if not, output error
			Assert.AreEqual(digit, icuDigit, "PUA Character " +
				puaIndex.ToString("x",new System.Globalization.NumberFormatInfo()) +
				" has an incorrect digit value");
		}
	}
}
