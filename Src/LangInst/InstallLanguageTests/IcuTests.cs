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
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;	// Process processing
using System.Runtime.InteropServices;	// marshaling ...
using System.Collections;
using System.IO;
using System.Windows.Forms;
using Microsoft.Win32;		// registry
using NUnit.Framework;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;	// Icu
using SIL.FieldWorks.Common.FwUtils;	// TestUtils

namespace InstallLanguageTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for checking versification-related issues.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class IcuTests
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Moves test LDF to the Languages folder
		/// </summary>
		/// <param name="xmlName">The LDF to move</param>
		/// ------------------------------------------------------------------------------------
		public static string MoveLDF(string xmlName)
		{
			string rootPath = TestUtils.GetFWRootDir();
			string srcTemp = rootPath + @"..\src\langinst\InstallLanguageTests\" + xmlName;
			string xmlFile = rootPath + @"Languages\" + xmlName;

			if (File.Exists(xmlFile))
				File.SetAttributes(xmlFile, FileAttributes.Normal);	// turn off read only bit

			File.Copy(srcTemp, xmlFile, true);
			File.SetAttributes(xmlFile, FileAttributes.Normal);	// turn off read only bit
			return xmlFile;
		}

		private void InstallLocal(string localeName)
		{
			Process myProcess = new Process();
			string rootPath = TestUtils.GetFWRootDir();
			string xxxFile = MoveLDF(localeName + ".xml");

			// Now run the InstallLanguage application
			myProcess.StartInfo.FileName = TestUtils.InstallLangPath + "InstallLanguage.exe";
			myProcess.StartInfo.WorkingDirectory = rootPath + @"..\src\langinst\InstallLanguageTests";
			myProcess.StartInfo.Arguments = "-i " + xxxFile + " -q";	// run in silent mode...(-q)
			myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			myProcess.Start();
			myProcess.WaitForExit();
			Assert.AreEqual(0, myProcess.ExitCode,"Failed to install '" + localeName + "'.");

		}

		private void RemoveLocal(string localeName)
		{
			Process myProcess = new Process();
			string rootPath = TestUtils.GetFWRootDir();

			// Now run the InstallLanguage application
			myProcess.StartInfo.FileName = TestUtils.InstallLangPath + "InstallLanguage.exe";
			myProcess.StartInfo.WorkingDirectory = rootPath + @"..\src\langinst\InstallLanguageTests";
			myProcess.StartInfo.Arguments = "-r " + localeName + " -q";	// run in silent mode...(-q)
			myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

			myProcess.Start();
			myProcess.WaitForExit();
			Assert.AreEqual(0, myProcess.ExitCode,"Failed to install '" + localeName + "'.");
		}

		private void GetIcuLocationInfo(out string icuDataDir, out string icuDir,
			out string icuPrefix)
		{
			icuDataDir = TestUtils.GetIcuDataDir();
			icuDir = TestUtils.GetIcuDir();
			icuPrefix  = TestUtils.GetIcuData();
		}

		private void CreateBackupFiles()
		{
			string icuDataDir;
			string icuDir;
			string icuPrefix;
			GetIcuLocationInfo(out icuDataDir, out icuDir, out icuPrefix);

			File.Copy(icuDir + "data\\locales\\root.txt",
				icuDir + "data\\locales\\root.txt.SAV", true);

			File.Copy(icuDir + "data\\locales\\res_index.txt",
				icuDir + "data\\locales\\res_index.txt.SAV", true);

			File.Copy(icuDir + icuPrefix + "res_index.res",
				icuDir + icuPrefix + "res_index.res.SAV", true);

			File.Copy(icuDir + icuPrefix + "root.res",
				icuDir + icuPrefix + "root.res.SAV", true);

			// These will be modified in addition to root.* for ICU 3.4 and following.
			File.Copy(icuDir + "data\\locales\\en.txt", icuDir + "data\\locales\\en.txt.SAV", true);
			File.Copy(icuDir + icuPrefix + "en.res", icuDir + icuPrefix + "en.res.SAV", true);
		}

		private void RestoreBackupFiles()
		{
			string icuDataDir;
			string icuDir;
			string icuPrefix;
			GetIcuLocationInfo(out icuDataDir, out icuDir, out icuPrefix);

			ArrayList srcFiles = new ArrayList();

			srcFiles.Add(icuDir + "data\\locales\\root.txt");
			srcFiles.Add(icuDir + "data\\locales\\res_index.txt");
			srcFiles.Add(icuDir + icuPrefix + "res_index.res");
			srcFiles.Add(icuDir + icuPrefix + "root.res");
			// These will be modified in addition to root.* for ICU 3.4 and following.
			srcFiles.Add(icuDir + "data\\locales\\en.txt");
			srcFiles.Add(icuDir + icuPrefix + "en.res");

			foreach (string fname in srcFiles)
			{
				File.Copy(fname + ".SAV", fname, true);
				TestUtils.DeleteFile(fname + ".SAV");
			}
		}

		[Test]
		public void TestAdd_and_Remove_pig()
		{
			string icuDataDir;
			string icuDir;
			string icuPrefix;
			GetIcuLocationInfo(out icuDataDir, out icuDir, out icuPrefix);

			try
			{
				CreateBackupFiles();

				InstallLocal("pig");
				string result;
				Icu.UErrorCode error;

				Icu.SetDataDirectory(icuDataDir);
				Icu.GetDisplayName("frm__snz", "en", out result, out error);
				Icu.GetDisplayName("pig", "en", out result, out error);
				Icu.Cleanup();		// clean up the ICU files / data

				Assert.AreEqual("Piggy", result, "Data 'pig' not put into the ICUFiles");

				RemoveLocal("pig");
				Icu.SetDataDirectory(icuDataDir);
				Icu.GetDisplayName("pig", "en", out result, out error);
				Icu.Cleanup();		// clean up the ICU files / data

				Assert.AreEqual("pig", result, "Data 'pig' not removed from the ICUFiles");
			}
			finally
			{
				Icu.Cleanup();		// clean up the ICU files / data
				RemoveLocal("pig");
				RestoreBackupFiles();
				TestUtils.DeleteFile(icuDir + icuPrefix + "pig.res");
			}
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Installs the file xxx1_Wwww_YY2_ZZZ3 to test installing new locales, languages, variants
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInstallLanguage_xxx1_Wwww_YY2_ZZZ3()
		{
			// First backup the 'important' icu files
			ArrayList saveFiles = new ArrayList();
			LDInstallSaveFiles(saveFiles);

			// make sure no residue from last time
			string xxTxtFile = TestUtils.GetIcuDir() + "data\\locales\\xxx1_Wwww_YY2_ZZZ3.txt";
			TestUtils.DeleteFile(xxTxtFile);

			System.CodeDom.Compiler.TempFileCollection tempFiles;
			ArrayList backupFiles;

			TestUtils.MakeTempFiles(saveFiles, out backupFiles, out tempFiles);

			string xxxFile = MoveLDF("xxx1_Wwww_YY2_ZZZ3.xml");
			tempFiles.AddFile(xxxFile, false);	// remove file when done with test

			try
			{
				TestUtils.RunProgram("InstallLanguage.exe", "-i " + xxxFile + " -q", 0);

				// Now use the ICU methods to see if the language was installed
				Checkxxx1_Wwww_YY2_ZZZ3Results();
			}
			finally
			{
				Icu.Cleanup();
				TestUtils.RunProgram("InstallLanguage.exe", "-r xxx1_Wwww_YY2_ZZZ3 -q", 0);
				TestUtils.CleanUpAndRestore(saveFiles,backupFiles,tempFiles);
			}

			// Now make sure the Language was uninstalled / icu restored properly
			Checkxxx1_Wwww_YY2_ZZZ3Restored();
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Installs the file el_Mysc_UNH_IPA.xml to test installing new locales, languages, variants
		/// where the language and variant are already installed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInstallLanguage_el_Mysc_UNH_IPA()
		{
			// First backup the 'important' icu files
			ArrayList saveFiles = new ArrayList();
			LDInstallSaveFiles(saveFiles);

			// make sure no resedue from last time
			string elTxtFile = TestUtils.GetIcuDir() + "data\\locales\\el_Mysc_UNH_IPA.txt";
			TestUtils.DeleteFile(elTxtFile);

			System.CodeDom.Compiler.TempFileCollection tempFiles;
			ArrayList backupFiles;

			TestUtils.MakeTempFiles(saveFiles,out backupFiles, out tempFiles);

			// Add files that need to be removed when we are done.
			tempFiles.AddFile(elTxtFile, false);
			tempFiles.AddFile(TestUtils.GetIcuDataDir() + "el_Mysc_UNH_IPA.res", false);

//			Process myProcess = new Process();
//			string rootPath = TestUtils.GetFWRootDir();
			string elFile = MoveLDF("el_Mysc_UNH_IPA.xml");
			tempFiles.AddFile(elFile, false);	// remove file when done with test

			try
			{
				TestUtils.RunProgram("InstallLanguage.exe","-i " + elFile + " -q",0);

				// Now use the ICU methods to see if the language was installed
				Checkel_Mysc_UNH_IPAResults();
			}
			finally
			{
				Icu.Cleanup();
				TestUtils.RunProgram("InstallLanguage.exe","-r el_Mysc_UNH_IPA -q",0);
				TestUtils.CleanUpAndRestore(saveFiles,backupFiles,tempFiles);
			}

			// Now make sure the Language was uninstalled / icu restored properly
			Checkel_Mysc_UNH_IPARestored();

		}


		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestInstallLanguage_pig()
		{
			ArrayList saveFiles = new ArrayList();
			ArrayList backupFiles;
			System.CodeDom.Compiler.TempFileCollection tempFiles;

			// Decide what files we want to save (LD pig related)
			LDInstallSaveFiles(saveFiles);
			// Save the backup
			TestUtils.MakeTempFiles(saveFiles, out backupFiles, out tempFiles);

			// Adds files to "tempFiles" that need to be removed when we are done
			LDPigTempFiles(tempFiles);

			string pigFile = MoveLDF("pig.xml");
			// remove file when done with test
			tempFiles.AddFile(pigFile, false);

			Icu.UErrorCode error;
			try
			{
				TestUtils.RunProgram("InstallLanguage.exe","-i " + pigFile + " -q",0);
				CheckLDPigResults();
				Icu.Cleanup();
				TestUtils.RunProgram("InstallLanguage.exe","-r pig -q",0);
			}
			finally
			{
				TestUtils.CleanUpAndRestore(saveFiles,backupFiles,tempFiles);
			}

			// Now make sure the Language was uninstalled / icu restored properly
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			string result2;
			Icu.GetDisplayName("pig", "en", out result2, out error);
			Icu.Cleanup();
			Assert.AreEqual("pig", result2, "Data 'pig' not removed from the ICU Files");
		}

		/// <summary>
		/// This test installs several PUA characters with a variety of attributes and
		/// checks to make sure that all the correct attributes are in the ICU database.
		/// </summary>
		[Test]
		public void TestInstallLanguage_PUA()
		{
			// First backup the 'important' icu files and unidata files
			ArrayList saveFiles = new ArrayList();
			PUAInstallSaveFiles(saveFiles);

			System.CodeDom.Compiler.TempFileCollection tempFiles;
			ArrayList backupFiles;

			TestUtils.MakeTempFiles(saveFiles,out backupFiles, out tempFiles);

			string puaTestFile = MoveLDF("pua.xml");
			// remove file when done with test
			tempFiles.AddFile(puaTestFile, false);

			try
			{
				TestUtils.RunProgram("InstallLanguage.exe","-c " + puaTestFile + " -q",0);
				Check_for_PUA_Results();
			}
			finally
			{
				TestUtils.CleanUpAndRestore(saveFiles,backupFiles,tempFiles);
			}

			//Make sure the unicodeData file was restored properly
			TestUtils.Check_PUA(0xF171,"COMBINING MACRON-ACUTE",LgGeneralCharCategory.kccMn);
		}

		/// <summary>
		/// This runs both the pig and pua tests using a single call to "InstallLanguage"
		/// </summary>
		[Test]
		public void TestInstallLanguage_PigAndPUA()
		{
			// First backup the 'important' icu files and unidata files
			ArrayList saveFiles = new ArrayList();
			LDInstallSaveFiles(saveFiles);
			PUAInstallSaveFiles(saveFiles);

			System.CodeDom.Compiler.TempFileCollection tempFiles;
			ArrayList backupFiles;

			TestUtils.MakeTempFiles(saveFiles,out backupFiles, out tempFiles);

			LDPigTempFiles(tempFiles);

			string puaTestFile = MoveLDF("puapig.xml");
			// remove file when done with test
			tempFiles.AddFile(puaTestFile, false);

			try
			{
				TestUtils.RunProgram("InstallLanguage.exe","-i -c " + puaTestFile + " -q",0);
				CheckLDPigResults();
				Check_for_PUA_Results();
				Icu.Cleanup();		// clean up the ICU files / data
				TestUtils.RunProgram("InstallLanguage.exe","-r puapig -q",0);
			}
			finally
			{
				TestUtils.CleanUpAndRestore(saveFiles,backupFiles,tempFiles);
			}

			// Now make sure the Language was uninstalled / icu restored properly
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			string result2;
			SIL.FieldWorks.Common.FwUtils.Icu.UErrorCode error;
			Icu.GetDisplayName("pig", "en", out result2, out error);
			Icu.Cleanup();
			Assert.AreEqual("pig", result2, "Data 'pig' not removed from the ICU Files");

			//Make sure the unicodeData file was restored properly
			TestUtils.Check_PUA(0xF171,"COMBINING MACRON-ACUTE",LgGeneralCharCategory.kccMn);
		}

		/// <summary>
		/// This test tries to add a PUA character outside of the range of allowable PUA characters.
		/// InstallLanguages should correctly fail and close.
		/// </summary>
		[Test]
		public void TestInstallLanguage_argumentParser()
		{
			TestUtils.Check_PUA(0xF171,"COMBINING MACRON-ACUTE",LgGeneralCharCategory.kccMn);
			// Technically we don't really need to do file backup in this function, since it doesn't change anything.
			// Just in case someone removes the "dontDoAnything" flag we'll keep this.
			// First backup the 'important' icu files and unidata files
			ArrayList saveFiles = new ArrayList();
			PUAInstallSaveFiles(saveFiles);

			System.CodeDom.Compiler.TempFileCollection tempFiles;
			ArrayList backupFiles;

			TestUtils.MakeTempFiles(saveFiles,out backupFiles, out tempFiles);

			string puaTestFile = MoveLDF("pig.xml");
			// remove file when done with test
			tempFiles.AddFile(puaTestFile, false);

			try
			{
				//Install Language Tests - PIG
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything pig",0,"dontDoAnything:, i:pig.xml, ");
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything -i pig.xml",0,"dontDoAnything:, i:pig.xml, ");

				//rem Install Language Tests - PUA
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything -c pig",0,"dontDoAnything:, c:pig.xml, ");

				//Install Language Tests - PIG and PUA
				TestUtils.CheckStdOut("InstallLanguage.exe", "-c -i pig -dontDoAnything",0,"dontDoAnything:, i:pig.xml, c:pig.xml, ");
				TestUtils.CheckStdOut("InstallLanguage.exe", "-i -c pig -dontDoAnything",0,"dontDoAnything:, i:pig.xml, c:pig.xml, ");
				TestUtils.CheckStdOut("InstallLanguage.exe", "pig.xml -c pig -dontDoAnything",0,"dontDoAnything:, i:pig.xml, c:pig.xml, ");
				TestUtils.CheckStdOut("InstallLanguage.exe", "pig -c -dontDoAnything ",0,"dontDoAnything:, i:pig.xml, c:pig.xml, ");
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything -q pig -c ",0,"dontDoAnything:, q:, i:pig.xml, c:pig.xml, ");
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything -c pig.xml -i ",0,"dontDoAnything:, i:pig.xml, c:pig.xml, ");
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything -i pig -c pig.xml",0,"dontDoAnything:, i:pig.xml, c:pig.xml, ");

				//Should break
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything -i pig -c pigTest.xml",-1,null);
				TestUtils.CheckStdOut("InstallLanguage.exe", "-dontDoAnything pig.xml -i pig -c",-1,null);
			}
			finally
			{
				TestUtils.CleanUpAndRestore(saveFiles,backupFiles,tempFiles);
			}

			//Make sure the unicodeData file was restored properly
			TestUtils.Check_PUA(0xF171,"COMBINING MACRON-ACUTE",LgGeneralCharCategory.kccMn);
		}


		/// <summary>
		/// Adds files needed by the LD installer to a list of files to save and restore.
		/// </summary>
		/// <param name="saveFiles">A list of files to which important files will be added</param>
		public static void LDInstallSaveFiles(ArrayList saveFiles)
		{
			string sIcuDir = TestUtils.GetIcuDir();
			string sIcuPrefix  = TestUtils.GetIcuData();

			// First backup the 'important' icu files
			saveFiles.Add(sIcuDir + sIcuPrefix + "res_index.res");
			saveFiles.Add(sIcuDir + sIcuPrefix + "root.res");
			saveFiles.Add(sIcuDir + "data\\locales\\res_index.txt");
			saveFiles.Add(sIcuDir + "data\\locales\\root.txt");

			// This will be modified in addition to root.* for ICU 3.4 and following.
			saveFiles.Add(sIcuDir + sIcuPrefix + "en.res");
			saveFiles.Add(sIcuDir + "data\\locales\\en.txt");
		}

		/// <summary>
		/// Make sure that we delete files that we made when we are done.
		/// </summary>
		/// <param name="tempFiles"></param>
		private void LDPigTempFiles(System.CodeDom.Compiler.TempFileCollection tempFiles)
		{
			// make sure no resedue from last time
			string pigTxtFile = TestUtils.GetIcuDir() + "data\\locales\\pig.txt";
			TestUtils.DeleteFile(pigTxtFile);
			tempFiles.AddFile(pigTxtFile, false);
			string pigResFile = TestUtils.GetIcuDataDir() + "pig.res";
			TestUtils.DeleteFile(pigResFile);
			tempFiles.AddFile(pigResFile, false);
		}

		/// <summary>
		/// Adds files needed by the PUA installer to a list of files to save and restore.
		/// </summary>
		///<param name="saveFiles">A list of files to which important files will be added</param>
		private void PUAInstallSaveFiles(ArrayList saveFiles)
		{
			string sIcuDataDir = TestUtils.GetIcuDataDir();
			// The logic changed for ICU 3.4, which uses a subdirectory instead of a
			// filename prefix to isolate the data files for a particular version.
			string sIcuDir = TestUtils.GetIcuDir();

			// First backup the 'important' UCD text files and the icu files
			saveFiles.Add(sIcuDir + "data\\unidata\\UnicodeData.txt");
			saveFiles.Add(sIcuDir + "data\\unidata\\DerivedBidiClass.txt");
			saveFiles.Add(sIcuDir + "data\\unidata\\DerivedNormalizationProps.txt");
			saveFiles.Add(sIcuDataDir + "uprops.icu");
			saveFiles.Add(sIcuDataDir + "unames.icu");
			saveFiles.Add(sIcuDataDir + "unorm.icu");
			saveFiles.Add(sIcuDataDir + "ucase.icu");
			saveFiles.Add(sIcuDataDir + "ubidi.icu");
		}


		/// <summary>
		/// Checks to make sure that "InstallLanguage pig" did what is was supposed to.
		/// Use the ICU methods to see if the minimal 'pig' language was installed.
		/// </summary>
		public void CheckLDPigResults()
		{
			// Now use the ICU methods to see if the minimal 'pig' language was installed
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			string result;
			SIL.FieldWorks.Common.FwUtils.Icu.UErrorCode error;
			Icu.GetDisplayName("pig", "en", out result, out error);
			Assert.AreEqual("Piggy", result, "Data 'pig' not put into the ICUFiles");
		}
		/// <summary>
		/// Check that xxx1_Wwww_YY2_ZZZ3 installed correctly
		/// </summary>
		private void Checkxxx1_Wwww_YY2_ZZZ3Results()
		{
			Icu.UErrorCode error;
			string result;
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			Icu.GetDisplayName("xxx1_Wwww_YY2_ZZZ3", "en", out result, out error);
			Assert.AreEqual("XXXLanguage1 (WWScript2, YYCountry2, ZZZVariant3)", result, "Data not put into the ICUFiles");
		}
		/// <summary>
		/// Check that ICU was correctly restored for xxx1_Wwww_YY2_ZZZ3
		/// </summary>
		public static void Checkxxx1_Wwww_YY2_ZZZ3Restored()
		{
			Icu.UErrorCode error;
			string result;
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			Icu.GetDisplayName("xxx1_Wwww_YY2_ZZZ3", "en", out result, out error);
			Icu.Cleanup();		// clean up the ICU files / data
			Assert.AreEqual("xxx1 (Wwww, YY2, ZZZ3)", result, "Data 'xxx1_Wwww_YY2_ZZZ3' not removed from the ICU Files");
		}

		/// <summary>
		/// Check that xxx1_Wwww_YY2_ZZZ3 installed correctly
		/// </summary>
		private void Checkel_Mysc_UNH_IPAResults()
		{
			Icu.UErrorCode error;
			string result;
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			Icu.GetDisplayName("el_Mysc_UNH_IPA", "en", out result, out error);
			Assert.AreEqual("Greek (MyScript, UnheardOf, IPA)", result, "Data not put into the ICUFiles");
			Icu.GetDisplayLanguage("el_Mysc_UNH_IPA", "en", out result, out error);
			Assert.AreEqual("Greek", result, "Greek should not be changed as it is a factory value.");
			Icu.GetDisplayScript("el_Mysc_UNH_IPA", "en", out result, out error);
			Assert.AreEqual("MyScript", result, "The display script should be changed.");
			Icu.GetDisplayCountry("el_Mysc_UNH_IPA", "en", out result, out error);
			Assert.AreEqual("UnheardOf", result, "The display country should be changed.");
			Icu.GetDisplayVariant("el_Mysc_UNH_IPA", "en", out result, out error);
			Assert.AreEqual("IPA", result, "The display variant should not be changed.");

			StreamReader reader;
			TestUtils.RunProgram("InstallLanguage","-q -customLanguages",0,out reader);
			string output = reader.ReadToEnd();
			Assert.IsTrue( output.IndexOf("<el>") == -1,
				"Should not have installed factory locale 'el' as a custom language." +
				"Installed Custom Languages: " + output.ToString());

		}
		/// <summary>
		/// Check that ICU was correctly restored for xxx1_Wwww_YY2_ZZZ3
		/// </summary>
		private void Checkel_Mysc_UNH_IPARestored()
		{
			Icu.UErrorCode error;
			string result;
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			Icu.GetDisplayName("el_Mysc_UNH_IPA", "en", out result, out error);
			Icu.Cleanup();		// clean up the ICU files / data
			// Note "Greek" and "IPA" are already added as "factory" locales.
			Assert.AreEqual("Greek (Mysc, UNH, IPA)", result, "Data 'el_Mysc_UNH_IPA' not removed from the ICU Files");
		}

		/// <summary>
		/// Tests the results of running InstallLanguage -c pua but doesn't run it
		/// </summary>
		/// This is not a test, because it won't work unless you have run "InstallLanguage -c pua" without restoring the original files
		///[Test]
		public void Check_for_PUA_Results()
		{
			//Check to see if individual pua characters were loaded properly
			//TestUtils.Check_PUA(0xF175,"COMBINING GRAVE-ACUTE-GRAVE");
			Icu.Cleanup();
			Icu.SetDataDirectory(TestUtils.GetIcuDataDir());
			TestUtils.Check_PUA (0xE001,"PIG NUMERAL 1",LgGeneralCharCategory.kccNd);
			TestUtils.Check_PUA (0xE002,"PIG NUMERAL 2",LgGeneralCharCategory.kccNd);
			TestUtils.Check_PUA (0xF00D,"PIG NUMERAL 3",LgGeneralCharCategory.kccNd);
//			TestUtils.Check_PUA (0xF00E,"PIG NUMERAL 4",LgGeneralCharCategory.kccNd);
			TestUtils.Check_PUA (0xF0ED,"PIG NUMERAL 5",LgGeneralCharCategory.kccNd);
			TestUtils.Check_PUA (0xF0EE,"PIG NUMERAL 6",LgGeneralCharCategory.kccNd);
			TestUtils.Check_PUA (0xF12E,"PIG NUMERAL 7",LgGeneralCharCategory.kccNd);
			TestUtils.Check_PUA (0xF12F,"PIG NUMERAL 8",LgGeneralCharCategory.kccNd);
			TestUtils.Check_PUA (0xF136,"PIGDASH",LgGeneralCharCategory.kccMn);
			TestUtils.Check_PUA (0xF137,"PIG Curlytail",LgGeneralCharCategory.kccMn);
			TestUtils.Check_PUA (0xF139,"PIG Jump",LgGeneralCharCategory.kccLl);
//			TestUtils.Check_PUA (0xF16F,"JIBBERISH SMALL LETTER P WITH TWO STROKES",LgGeneralCharCategory.kccSm);
			TestUtils.Check_PUA (0xF171,"PIGCHARACTER UGLY",LgGeneralCharCategory.kccLl);

			TestUtils.Check_PUA_Digit(0xF00D, 3);
			TestUtils.Check_PUA_Digit(0xF00E, 4);
			TestUtils.Check_PUA_Digit(0xF12F, -1);

			//JIBBERISH SMALL LETTER P WITH TWO STROKES;Sm;0;EN;
			//<compat> 0050;;;;Y;OLD NAME OF JIBBERISH;NO COMMENT;0050;0070;0050
			TestUtils.Check_PUA(0xF16F,"JIBBERISH SMALL LETTER P WITH TWO STROKES",LgGeneralCharCategory.kccSm,
				0,LgBidiCategory.kbicEN,"P",false,-1,"NO COMMENT",'P','p','P');

			//PIG NUMERAL 4;Nd;228;EN;<small> 0034;4;4;4;N;PIGGY 4;<none>;;;
			TestUtils.Check_PUA(0xF00E,"PIG NUMERAL 4",LgGeneralCharCategory.kccMn,
				0228,LgBidiCategory.kbicNSM,"4",false,4,"<none>",0xF00E,0xF00E,0xF00E);
			// Note: the "puaNumeric" parameter is false, since this doesn't have a type that is numeric.
			// Perhaps all characters that have a numeric value should have a numeric type

			//PIG NUMERAL 3;Nd;228;EN;<small> 0033;3;3;3;N;PIGGY 3;<none>;;;
			TestUtils.Check_PUA(0xF00D,"PIG NUMERAL 3",LgGeneralCharCategory.kccNd,
				0228,LgBidiCategory.kbicEN,"3",true,3,"<none>",0xF00D,0xF00D,0xF00D);
			// Note: the "puaNumeric" parameter is false, since this doesn't have a type that is numeric.
			// Perhaps all characters that have a numeric value should have a numeric type

			// puaOutOfRange.xml - uses non PUA characters
//			TestUtils.Check_PUA(0xE000, "PIG NUMERAL 7");
//			TestUtils.Check_PUA(0xE001, "PIG NUMERAL 8");
//			TestUtils.Check_PUA(0xDC01, "PIG NUMERAL 1");
//			TestUtils.Check_PUA(0xDC02, "PIG NUMERAL 2");
//			TestUtils.Check_PUA(0xDEEE, "PIG NUMERAL 3");
//			TestUtils.Check_PUA(0xDFFE, "PIG NUMERAL 5");
//			TestUtils.Check_PUA(0xDEEF, "PIG NUMERAL 4");
//			TestUtils.Check_PUA(0xDFFF, "PIG NUMERAL 6");
//
//			TestUtils.Check_PUA(0xF136, "PIGDASH");
//			TestUtils.Check_PUA(0xF137, "PIG Curlytail");
//			TestUtils.Check_PUA(0xF139, "PIG Jump");
//			TestUtils.Check_PUA(0xF171, "PIGCHARACTER UGLY");
//			TestUtils.Check_PUA(0xF16F, "JIBBERISH SMALL LETTER P WITH TWO STROKES");

		}

		/// <summary>
		/// Checks for characters that are already in UnicodeData.txt (standard letters)
		/// The purpose of this is to guaruntee that our checking abilities work.
		/// That is, we are checking the ICU wrapper methods that we are using for these tests.
		/// </summary>
		[Test]
		public void CheckExistingCharacter()
		{
			// Check simple letter 'A'
			//0041;LATIN CAPITAL LETTER A;Lu;0;L;;;;;N;;;;0061;
			TestUtils.Check_PUA(0x0041, "LATIN CAPITAL LETTER A",LgGeneralCharCategory.kccLu);

			// Check Combining Class (230)
			//0300;COMBINING GRAVE ACCENT;Mn;230;NSM;;;;;N;NON-SPACING GRAVE;Varia;;;
			TestUtils.Check_PUA(0x0300,"COMBINING GRAVE ACCENT",LgGeneralCharCategory.kccMn,
				230,LgBidiCategory.kbicNSM,"\u0300",false,-1,"Varia",0x300,0x300,0x300);

			// Check Decomposition (compatability)
			//FE50;SMALL COMMA;Po;0;CS;<small> 002C;;;;N;;;;;
			TestUtils.Check_PUA(0xFE50,"SMALL COMMA",LgGeneralCharCategory.kccPo,0,LgBidiCategory.kbicCS,
				"\u002C",false,-1,"",0xFE50,0xFE50,0xFE50);

			// Check Decomposition (canonical [i.e not compatability])
			//FB1F;HEBREW LIGATURE YIDDISH YOD YOD PATAH;Lo;0;R;05F2 05B7;;;;N;;;;;
			TestUtils.Check_PUA(0xFB1F,"HEBREW LIGATURE YIDDISH YOD YOD PATAH",LgGeneralCharCategory.kccLo,
				0,LgBidiCategory.kbicR,"\u05F2\u05B7",false,-1,"",0xFB1F,0xFB1F,0xFB1F);

			// Check numeric value
			//FF18;FULLWIDTH DIGIT EIGHT;Nd;0;EN;<wide> 0038;8;8;8;N;;;;;
			TestUtils.Check_PUA(0xFF18,"FULLWIDTH DIGIT EIGHT",LgGeneralCharCategory.kccNd,
				0,LgBidiCategory.kbicEN,
				"\u0038",true,8,"",'\uFF18','\uFF18','\uFF18');

			// Check surrogate pairs with decompotition and numeric value
			//1D7DA;MATHEMATICAL DOUBLE-STRUCK DIGIT TWO;Nd;0;EN;<font> 0032;2;2;2;N;;;;;
			TestUtils.Check_PUA(0x1D7DA,"MATHEMATICAL DOUBLE-STRUCK DIGIT TWO",LgGeneralCharCategory.kccNd,
				0,LgBidiCategory.kbicEN,"\u0032",true,2,"",0x1D7DA,0x1D7DA,0x1D7DA);

			//1D7EB;MATHEMATICAL SANS-SERIF DIGIT NINE;Nd;0;EN;<font> 0039;9;9;9;N;;;;;
			TestUtils.Check_PUA(0x1D7EB,"MATHEMATICAL SANS-SERIF DIGIT NINE",LgGeneralCharCategory.kccNd,
				0,LgBidiCategory.kbicEN,"9",true,9,"",0x1D7EB,0x1D7EB,0x1D7EB);

			// (i.e. a unicode value that uses surrogate pairs)
			//104A1;OSMANYA DIGIT ONE;Nd;0;L;;1;1;1;N;;;;;
			// TODO: Does this code need to be more flexible to allow both big and little endian?
			int osmanyaChar = 0x104a1;

			char osmanyaCharH = (char)(((osmanyaChar-0x10000)/0x400) + 0xD800);
			char osmanyaCharL = (char)(((osmanyaChar-0x10000)%0x400) + 0xDC00);
			TestUtils.Check_PUA(0x104A1,"OSMANYA DIGIT ONE",LgGeneralCharCategory.kccNd,
				0,LgBidiCategory.kbicL,new String(new char[]{osmanyaCharH,osmanyaCharL}),true,1,"",0x104A1,0x104A1,0x104A1);
	}

	}
	[TestFixture]
	[Ignore("These tests take about 3 mintues together to run, and they are not critical.")]
	public class IcuSlowTests
	{
		[Test]
		public void CheckMainParser()
		{
			string rootPath = TestUtils.GetFWRootDir();
			Process myProcess = new Process();

			try
			{
				// Run the InstallLanguage application with the test option
				myProcess.StartInfo.FileName = TestUtils.InstallLangPath + "InstallLanguage.exe";
				myProcess.StartInfo.WorkingDirectory = rootPath + @"..\src\langinst\InstallLanguageTests";
				myProcess.StartInfo.Arguments = "-testMainParserRoutine ";// + " -q";	// run in silent mode...(-q)
				myProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;

				myProcess.Start();
				myProcess.WaitForExit();
				Assert.AreEqual(0, myProcess.ExitCode,"Failed to parse locale file(s).");
			}
			finally
			{
			}
		}
		/// <summary>
		/// Parses every locale in icu/data/locales and writes it back, checking to make sure they are identical,
		///		except for the indentation of the lines.
		/// </summary>
		[Test]
		public void TestInstallLanguage_TestICUDataParser()
		{
			// get all the files in the data\locales directory
			string[] fileNames = Directory.GetFiles(TestUtils.GetIcuDir() + @"data\locales\");
			Queue icuLocales = new Queue();

			foreach( string icuLocaleFile in fileNames)
				if( icuLocaleFile.IndexOf("_ORIGINAL")==-1 && icuLocaleFile.IndexOf("_TEMP")==-1)
					icuLocales.Enqueue(icuLocaleFile);

			// First backup the 'important' icu files
			ArrayList saveFiles = new ArrayList();
			foreach( string icuLocaleFile in icuLocales)
				saveFiles.Add(icuLocaleFile);

			System.CodeDom.Compiler.TempFileCollection tempFiles;
			ArrayList backupFiles;
			TestUtils.MakeTempFiles(saveFiles,out backupFiles, out tempFiles);

			try
			{
				foreach( string icuLocaleFile in icuLocales)
				{
					// Read in each file and write it back out again
					TestUtils.RunProgram("InstallLanguage","-q -testICUDataParser " + icuLocaleFile,0);
					// Check to make sure all the files are the same as the original file.
					TestUtils.CheckFilesMatch(
						icuLocaleFile,(string)backupFiles[saveFiles.IndexOf(icuLocaleFile)],
						TestUtils.MatchingStrictLevel.IgnoreIndent);
				}
			}
			finally
			{
				TestUtils.CleanUpAndRestore(saveFiles,backupFiles,tempFiles);
			}

			//Make sure the unicodeData file was restored properly
			TestUtils.Check_PUA(0xF171,"COMBINING MACRON-ACUTE",LgGeneralCharCategory.kccMn);
		}

		/// <summary>
		/// Tests the new faster way to install locales. Runs several times to record the times.
		/// Runs both with and without the -slow flag several times, switching the order randomly.
		///
		/// Checks that it is successful (i.e. returns 0) but not that the files it modifies are identical.
		/// </summary>
		[Test]
		public void TestInstallLanguage_fast_speedtest()
		{
			// First backup the 'important' icu files
			ArrayList saveFiles = new ArrayList();
			InstallLanguageTests.IcuTests.LDInstallSaveFiles(saveFiles);

			// Make a list of files that should be the same between the two runs
			saveFiles.Add(TestUtils.GetIcuDir() + "data\\locales\\res_index.txt");
			saveFiles.Add(TestUtils.GetIcuDir() + "data\\locales\\root.txt");

			// make sure no resedue from last time
			string xxTxtFile = TestUtils.GetIcuDir() + "data\\locales\\xxx1_Wwww_YY2_ZZZ3.txt";
			TestUtils.DeleteFile(xxTxtFile);

			System.CodeDom.Compiler.TempFileCollection tempFiles;
			ArrayList backupFiles;

			// Files that need to be deleted between the running of the program so that they don't exist.
			System.CodeDom.Compiler.TempFileCollection specialDeleteTempFiles =
				new System.CodeDom.Compiler.TempFileCollection();
			specialDeleteTempFiles.AddFile(TestUtils.GetIcuDir()
				+ "data\\locales\\xxx1_Wwww_YY2_ZZZ3.txt", false);
			specialDeleteTempFiles.AddFile(TestUtils.GetIcuDataDir() + "xxx1_Wwww_YY2_ZZZ3.res", false);

			TestUtils.MakeTempFiles(saveFiles,out backupFiles, out tempFiles);

			// Add files that need to be removed when we are done.
			tempFiles.AddFile(xxTxtFile, false);
			tempFiles.AddFile(TestUtils.GetIcuDataDir() + "xxx1_Wwww_YY2_ZZZ3.res", false);

			Process myProcess = new Process();
			string rootPath = TestUtils.GetFWRootDir();
			string xxxFile = InstallLanguageTests.IcuTests.MoveLDF("xxx1_Wwww_YY2_ZZZ3.xml");
			tempFiles.AddFile(xxxFile, false);	// remove file when done with test

			string slow1;
			string slow2;

			int currentRun = 0;
			int maxRuns = 20;
			Random random = new Random(System.DateTime.Now.Millisecond);
			for( currentRun = 0; currentRun < maxRuns; currentRun ++)
			{
				// Randomly switch which one runs first.
				if(random.NextDouble() > .5)
				{
					slow1 = " -slow";
					slow2 = "";
				}
				else
				{
					slow2 = " -slow";
					slow1 = "";
				}

				try
				{
					TestUtils.RunProgram("InstallLanguage.exe","-i " + xxxFile + " -q " + slow1,0);
					// Retore all the originals
					// Don't delete the temporary files yet, we will need them later.
					TestUtils.CleanUpAndRestore(saveFiles,backupFiles,null);
					specialDeleteTempFiles.Delete();

					TestUtils.RunProgram("InstallLanguage.exe","-i " + xxxFile + " -q " + slow2,0);
				}
				finally
				{
					// Restore all the originals
					// Don't delete the temporary files yet, we will need them later.
					TestUtils.CleanUpAndRestore(saveFiles, backupFiles, null);
				}
			}

			TestUtils.CleanUpAndRestore(saveFiles, backupFiles, tempFiles);
			// Now make sure the Language was uninstalled / icu restored properly
			InstallLanguageTests.IcuTests.Checkxxx1_Wwww_YY2_ZZZ3Restored();
		}
	}
}
