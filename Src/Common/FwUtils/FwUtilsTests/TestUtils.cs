// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IcuTests.cs
// Responsibility: Dan Hinton
// (Also added tests by Tres London and Zachriah Yoder)
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
// TODO: Figure out where these shard test functions should really go.

using NUnit.Framework;
using Microsoft.Win32;		// registry
using System.Diagnostics;	// Process processing
using System.Runtime.InteropServices;	// marshaling ...
using System.Collections;
using System.IO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;	// Icu
//using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// Summary description for TestUtils.
	/// </summary>
	public class TestUtils
	{
		#region Registry helper methods

		private static string m_icuDataDir = null;
		private static string m_icuData = null;
		private static string m_icuDir = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the ICU Data dir from the registry.
		/// </summary>
		/// <returns>path to the icu data dir.</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetIcuDataDir()
		{
			if(m_icuDataDir == null)
			{
				m_icuDataDir = DirectoryFinder.GetIcuDirectory;
			}
			return m_icuDataDir;
		}

		/// <summary>
		/// Get the Icu data string used by the current version of Icu.
		/// </summary>
		/// <returns>Icu string like "icudt34l\"</returns>
		public static string GetIcuData()
		{

			if (m_icuData == null)
			{
				string sIcuDataDir = GetIcuDataDir();
				int idxBackSlash = sIcuDataDir.LastIndexOf("\\", sIcuDataDir.Length - 2);
				if (idxBackSlash == -1)
					return "unknown";
				m_icuData = sIcuDataDir.Substring(idxBackSlash + 1);
			}
			return m_icuData;
		}

		/// <summary>
		/// Get the base ICU directory from the registry.
		/// The logic changed between versions 2.8 and 3.4 of ICU.  ICU now uses a subdirectory
		/// instead of a filename prefix to isolate the data files for a particular version.
		/// </summary>
		/// <returns>path to the ICU directory</returns>
		public static string GetIcuDir()
		{
			if (m_icuDir == null)
			{
				string sIcuDataDir = GetIcuDataDir();
				int idxBackSlash = sIcuDataDir.LastIndexOf("\\", sIcuDataDir.Length - 2);
				if (idxBackSlash == -1)
					return sIcuDataDir;
				m_icuDir = sIcuDataDir.Substring(0, idxBackSlash + 1);
			}
			return m_icuDir;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="file"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static bool DeleteFile(string file)
		{
			bool rval = false;
			if (File.Exists(file))
			{
				File.SetAttributes(file, FileAttributes.Normal);
				File.Delete(file);
				rval = true;
			}
			return rval;
		}



		/// <summary>
		/// Compares two files to make sure that they are identical. (Strict)
		/// </summary>
		/// <param name="fileNameExpected">The path of the file that has the "correct" value.</param>
		/// <param name="fileNameActual">The path of the file to compare against the "correct" file.</param>
		public static void CheckFilesMatch(string fileNameExpected, string fileNameActual)
		{
			CheckFilesMatch(fileNameExpected,fileNameActual,MatchingStrictLevel.Strict);
		}

		/// <summary>
		/// Represents the various strict levels of
		/// </summary>
		public enum MatchingStrictLevel
		{
			/// <summary>Character for character</summary>
			Strict = 0,
			/// <summary>Allow lines with different leading whitespace to be considered identical
			/// </summary>
			IgnoreIndent = 1,
			/// <summary>Allow the lines to be considered identical with surrounding whitespace.
			/// </summary>
			Trim = 2,
			/// <summary>Compare the lines, ignoring the one line comments.</summary>
			IgnoreOneLineComments = 3
		}

		/// <summary>
		/// Compares two files to make sure that they are identical.
		/// Allows non strict file comparison (e.g. ignoring white space and single-line comments)
		/// </summary>
		/// <param name="fileNameExpected">The path of the correct expected file.</param>
		/// <param name="fileNameActual">The path of the tested actual file.</param>
		/// <param name="strictLevel">How strict to be when comparing the two files.</param>
		public static void CheckFilesMatch(string fileNameExpected, string fileNameActual,
			MatchingStrictLevel strictLevel)
		{
			StreamReader readerExpected = new StreamReader(fileNameExpected);
			StreamReader readerActual = new StreamReader(fileNameActual);
			try
			{
				// Character by character check must be identical.
				if( strictLevel == MatchingStrictLevel.Strict )
				{
					Assert.AreEqual(readerExpected.ReadToEnd(),readerActual.ReadToEnd(),
						fileNameExpected + " does not match " + fileNameActual);
				}
					// Line by line check
				else
				{
					String lineExpected;
					String lineActual;
					int currentLineNumber  = 1;
					while((lineExpected = readerExpected.ReadLine())!=null)
					{
						lineActual = readerActual.ReadLine();
						Assert.IsNotNull(lineActual,
							fileNameExpected + " does not match " + fileNameActual + ". " +
							fileNameActual + " has fewer lines than " + fileNameExpected);
						CheckLinesMatch(lineExpected, lineActual, fileNameExpected, fileNameActual, strictLevel, currentLineNumber);
						currentLineNumber++;
					}
					lineActual = readerActual.ReadLine();
					Assert.AreEqual(readerActual.ReadLine(),null,
						fileNameExpected + " does not match " + fileNameActual+
						fileNameActual + " has more lines than " + fileNameExpected);
				}
			}
			finally
			{
				readerExpected.Close();
				readerActual.Close();
			}
		}

		/// <summary>
		/// Strips off while space and Asserts that the lines match.
		/// </summary>
		/// <param name="lineExpected">A line in the expected file.</param>
		/// <param name="lineActual">The corresponding line in the actual file.</param>
		/// <param name="fileNameExpected">The name of the expected file.</param>
		/// <param name="fileNameActual">The name of the actual file.</param>
		/// <param name="strictLevel">How strict to be when comparing the two files.</param>
		/// <param name="currentLineNumber">The current line number used to display any error messages.</param>
		public static void CheckLinesMatch(string lineExpected, string lineActual,
			string fileNameExpected, string fileNameActual,
			MatchingStrictLevel strictLevel, int currentLineNumber)
		{
			if(strictLevel == MatchingStrictLevel.IgnoreIndent)
			{
				lineExpected = lineExpected.TrimStart(new char[]{' ','\t'});
				lineActual = lineActual.TrimStart(new char[]{' ','\t'});
			}
			else if(strictLevel == MatchingStrictLevel.Trim)
			{
				lineExpected = lineExpected.Trim();
				lineActual = lineActual.Trim();
			}
			else if(strictLevel == MatchingStrictLevel.IgnoreOneLineComments)
			{
				lineExpected = TrimSingleLineComment(lineExpected).Trim();
				lineActual = TrimSingleLineComment(lineActual).Trim();
			}
			Assert.AreEqual(lineExpected,lineActual,fileNameExpected + " does not match " + fileNameActual +
				Environment.NewLine + " line number: " + currentLineNumber);
		}

		/// <summary>
		/// Trims any single line comments off a given string.
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		public static string TrimSingleLineComment(string line)
		{
			// trims off the comments.
			int slashIndex = FindTextNotInQuotes(line,"//");
			if( slashIndex < 0)
				return line;
			else
				return line.Substring(0,slashIndex);
		}

		/// <summary>
		/// Finds the given text, but does not search in the quoted region.
		/// </summary>
		/// <param name="line"></param>
		/// <param name="text"></param>
		/// <returns>The index, or -1 if the character is not found.</returns>
		public static int FindTextNotInQuotes(string line, string text)
		{
			// Where we are in the string, indicates the location of the first " of a pair
			// starts just behind the string, so that we will look including the first part of the string.
			int currentIndex = -1;

			// The last '"' that we encountered
			// starts just behind the string, so that we will look including the first part of the string.
			int lastIndex=-1;

			// Find every double quote text (") and jump to the next one (")
			while(line.IndexOf("\"", currentIndex + 1) != -1)
			{
				// jump to the first '"' of a pair
				findNextQuote(ref currentIndex,line);
				// If the text is found in the valid range between two " " regions.
				int foundIndex = line.IndexOf(text, lastIndex + 1, currentIndex - lastIndex - 1);
				if( foundIndex != -1)
					return foundIndex;

				// If index is right at the end there is no pair, throw an exception.
				if(currentIndex + 1 == line.Length)
				{
					// There isn't a matching quote, text not found
					return -1;
				}
				// look for the last '"' of the pair
				findNextQuote(ref currentIndex, line);
				if(currentIndex == -1)
				{
					// There isn't a matching quote, text not found
					return -1;
				}
				// set the "last index" to be the on the end of the pair, so that we look between it and the next pair
				lastIndex = currentIndex;

				// If the index is right at the end, we won't find any thing else, so don't try.
				if(currentIndex + 1 == line.Length)
				{
					return -1;
				}

			}
			return line.IndexOf(text, currentIndex+1);
		}

		/// <summary>
		/// Find the next quote, skipping character escapes.
		/// </summary>
		/// <returns></returns>
		private static void findNextQuote(ref int currentIndex, string line)
		{
			currentIndex = line.IndexOf("\"", currentIndex+1);
			// Skip over all \" character esapes
			if( currentIndex>0 )
				while((line[currentIndex-1]=='\\') && currentIndex < line.Length)
				{
					currentIndex = line.IndexOf("\"", currentIndex+1);
				}
		}

		/// <summary>
		/// Creates temporary and backup files.
		/// </summary>
		/// <param name="saveFiles">The files that need to be backed up.</param>
		/// <param name="backupFiles">The backup that will overwrite the saved files when we are done.</param>
		/// <param name="tempFiles">The "TempFileCollection" that will be used
		///		to delete the backup files when we are done</param>
		public static void MakeTempFiles(ArrayList saveFiles, out ArrayList backupFiles,
			out System.CodeDom.Compiler.TempFileCollection tempFiles)
		{
			tempFiles = new System.CodeDom.Compiler.TempFileCollection();
			backupFiles = new ArrayList();

			foreach (string str in saveFiles)
			{
				string tmpName = System.IO.Path.GetTempFileName();
				File.Copy(str, tmpName, true);
				backupFiles.Add(tmpName);			// create a 1 to 1 relationship from src to tmp file
				tempFiles.AddFile(tmpName, false);	// remove files when container goes out of scope
			}
		}


		/// <summary>
		/// Cleans up all the shared stuff.
		/// 1. Icu.Cleanup()
		/// 2. Restores all the files to their original values
		/// </summary>
		/// <param name="saveFiles">The files to overwrite with their backups</param>
		/// <param name="backupFiles">The files to copy from</param>
		/// <param name="tempFiles">The files to delete, may be null, in which case they will not be deleted.</param>
		public static void CleanUpAndRestore(ArrayList saveFiles, ArrayList backupFiles,
			System.CodeDom.Compiler.TempFileCollection tempFiles)
		{
			Icu.Cleanup();		// clean up the ICU files / data

			// Now restore all the original files (pre-test files)
			for (int i = 0; i < saveFiles.Count; i++)
			{
				File.Copy((string)(backupFiles[i]), (string)(saveFiles[i]), true);
			}
			if(tempFiles != null)
				// removes all the temporary files
				tempFiles.Delete();
		}

		/// <summary>
		/// Attempts to run the program in the "InstallLanguageTests" directory=
		/// Asserts if anything but "0" (no error) is returned.
		/// Does not re-direct standard out.
		/// </summary>
		/// <param name="name">The name of the program, not including the path.
		/// Assumes the program is in ...fw\Output\Debug\ or ...fw\Output\Release</param>
		/// <param name="arguments"></param>
		/// <param name="exitCode">The code that the program should exit with.
		/// Will assert if it doesn't match.</param>
		public static void RunProgram(string name, string arguments, int exitCode)
		{
			string rootPath = GetFWRootDir();
			Process myProcess = new Process();

			// Now run the InstallLanguage application
			myProcess.StartInfo.FileName = InstallLangPath + name;
			myProcess.StartInfo.WorkingDirectory = rootPath + @"..\src\langinst\InstallLanguageTests";
			myProcess.StartInfo.Arguments = arguments;
			myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			WriteToTimingFile("Starting: " + name + " " + arguments,true);
			myProcess.Start();
			myProcess.WaitForExit();
			WriteToTimingFile("Finished: " + name + " " + arguments,false);

			Assert.AreEqual(exitCode, myProcess.ExitCode,"Failed to run: " + name + " " + arguments);
		}

		/// <summary>
		/// Attempts to run the program in the "InstallLanguageTests" directory=
		/// Asserts if anything but "0" (no error) is returned.
		/// Redirects std out to "stdOut"
		/// </summary>
		/// <param name="name"></param>
		/// <param name="arguments"></param>
		/// <param name="exitCode">The value returned by the program.</param>
		/// <param name="stdOut">The output of </param>
		public static void RunProgram(string name, string arguments, int exitCode, out StreamReader stdOut)
		{
			string rootPath = GetFWRootDir();
			Process myProcess = new Process();

			// Now run the InstallLanguage application
			myProcess.StartInfo.FileName = InstallLangPath + name;
			myProcess.StartInfo.WorkingDirectory = rootPath + @"..\src\langinst\InstallLanguageTests";
			myProcess.StartInfo.Arguments = arguments;
			myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			//Redirects stdout
			myProcess.StartInfo.UseShellExecute = false;
			myProcess.StartInfo.RedirectStandardOutput = true;

			WriteToTimingFile("Starting: " + name + " " + arguments,true);
			myProcess.Start();
			myProcess.WaitForExit();
			WriteToTimingFile("Finished: " + name + " " + arguments,false);

			Assert.AreEqual(exitCode, myProcess.ExitCode,"Failed to run: " + name + " " + arguments);

			stdOut = myProcess.StandardOutput;
		}


		/// <summary>
		/// Runs InstallLanguage.exe with the given arguments and checks to make sure Standard Out is correct.
		/// </summary>
		/// <param name="name">The name of the program to run. See RunProgram</param>
		/// <param name="args">The aruments to run with.</param>
		/// <param name="errorCode">The correct error code.</param>
		/// <param name="correctStdOut">The correct standard output.</param>
		public static void CheckStdOut(string name, string args, int errorCode, string correctStdOut)
		{
			StreamReader stdOut;
			RunProgram(name, args, errorCode, out stdOut);
			// Don't check standard output unless we have successfully completed
			if(errorCode==0)
				Assert.AreEqual(stdOut.ReadToEnd().Trim(),correctStdOut.Trim(),
					"Error running : " + name + " " + args);
		}

		/// <summary>
		/// The name of the temporary file to write timing statics to.
		/// </summary>
		private static StreamWriter m_timingFile = null;
		/// <summary>
		/// The name of the temporary file to write timing statics to.
		/// </summary>
		private static System.DateTime m_previousTime = DateTime.Now;
		/// <summary>
		/// Writes the message with a timestamp to the timing file.
		/// </summary>
		/// <param name="message">A message describing what the timing represents</param>
		/// <param name="start">Whether this is before the thing we wish to time, or after the item we wish to time.</param>
		public static void WriteToTimingFile(string message,bool start)
		{
			try
			{
				if(m_timingFile==null)
				{
					m_timingFile = new StreamWriter(
						GetFWRootDir() + @"..\src\langinst\InstallLanguageTests\timing.txt",true);
					m_timingFile.WriteLine("-------------------------------------------------");
				}
			}
			catch (System.IO.IOException e)
			{
				Console.WriteLine("Can't log timing info :" + e.Message);
				// silently fail if we can't do this timing test.
				return;
			}
			DateTime now = DateTime.Now;
			TimeSpan difference = now - m_previousTime;
			if(!start)
			{
				m_timingFile.WriteLine("Delta,{0,10},ms,{1},{2}",difference.TotalMilliseconds,System.DateTime.Now.ToLongTimeString(),message);
				m_timingFile.Flush();
			}
			// The previous time
			m_previousTime = now;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the FW root dir from the registry.
		/// </summary>
		/// <returns>path to the root dir.</returns>
		/// ------------------------------------------------------------------------------------
		public static string GetFWRootDir()
		{
			RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\SIL\FieldWorks");
			Assert.IsNotNull(regKey);
			string path = (string)regKey.GetValue("RootCodeDir");
			regKey.Close();
			if (path[path.Length-1] != '\\')
				path += '\\';
			return path;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the path to InstallLanguage.exe
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static string InstallLangPath
		{
			get
			{
				string installLangPath = GetFWRootDir() +
#if DEBUG
					@"..\Output\Debug\";
#else
					@"..\Output\Release\";
#endif
				return installLangPath;
			}
		}

		/// <summary>
		/// Test the character name at the memory address specified.
		/// Will assert an error if the PUA codepoint name is not correct.
		/// </summary>
		/// <param name="puaIndex">Unicode codepoint</param>
		/// <param name="puaName">Expected correct PUA codepoint name</param>
		/// <param name="puaGenCat">The expected PUA General Category</param>
		public static void Check_PUA(int puaIndex, string puaName, LgGeneralCharCategory puaGenCat)
		{
			string name = "";
			LgGeneralCharCategory genCategory = LgGeneralCharCategory.kccCn;

			//Getting the character name at the memory address specified
			ILgCharacterPropertyEngine charPropEngine = LgIcuCharPropEngineClass.Create();
			try
			{
				string icuDataDir = GetIcuDataDir();
				Icu.SetDataDirectory(icuDataDir);
				Icu.UErrorCode error;
				Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
				int len = Icu.u_CharName(puaIndex, choice, out name, out error);
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
			string fullDecomp = "I have no clue";
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
				string icuDataDir = GetIcuDataDir();
				Icu.SetDataDirectory(icuDataDir);
				Icu.UErrorCode error;
				Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
				int len = Icu.u_CharName(puaIndex, choice, out name, out error);
				genCategory = charPropEngine.get_GeneralCategory(puaIndex);
				combiningClass = charPropEngine.get_CombiningClass(puaIndex);
				bidiCategory = charPropEngine.get_BidiCategory(puaIndex);
				decomposition = charPropEngine.get_Decomposition(puaIndex);
				fullDecomp = charPropEngine.get_FullDecomp(puaIndex);
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
			StringWriter stringWriter = new StringWriter(new System.Globalization.NumberFormatInfo());

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

		/// <summary>
		/// Test the character name at the memory address specified.
		/// Will assert an error if the PUA codepoint name and digit value are not correct.
		/// </summary>
		/// <param name="puaIndex">Unicode codepoint</param>
		/// <param name="digit">Expected correct PUA codepoint name</param>
		public static void Check_PUA_Digit(int puaIndex, int digit)
		{
			string name = "";
			int icuDigit = -1;

			//Getting the character name at the memory address specified
			try
			{
				string icuDataDir = GetIcuDataDir();
				Icu.SetDataDirectory(icuDataDir);
				Icu.UErrorCode error;
				Icu.UCharNameChoice choice = Icu.UCharNameChoice.U_UNICODE_CHAR_NAME;
				int len = Icu.u_CharName(puaIndex, choice, out name, out error);
				// Radix means "base", so this will return the base 10 value of this digit.
				// (Note, the radix is just used to return an error if the digit isn't valid in the given radix)
				icuDigit = Icu.u_Digit(puaIndex,10);

			}
			finally
			{
				Icu.Cleanup();		// clean up the ICU files / data
			}

			//Check to make sure expected result is the same as actual result, if not, output error
			Assert.AreEqual(digit, icuDigit, "PUA Character " +
				puaIndex.ToString("x",new System.Globalization.NumberFormatInfo()) +
				" has an incorrect digit value");

		}

	}
}
