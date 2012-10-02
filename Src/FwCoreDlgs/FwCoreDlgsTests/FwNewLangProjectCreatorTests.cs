// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwNewLangProjectCreatorTests.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using NUnit.Framework;

using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region DummyFwNewLangProjectCreator
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwNewLangProjectCreator : FwNewLangProjectCreator
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallCreateNewDbFiles(string projectName, out string dbFileName,
			out string logFileName)
		{
			base.CreateNewDbFiles(projectName, out dbFileName, out logFileName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CallCreateVernacularWritingSystem(FdoCache cache)
		{
			base.CreateVernacularWritingSystem(cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CultureInfo CallFindCultureInfoForLanguageName(string languageName)
		{
			return base.FindCultureInfoForLanguageName(languageName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the AttachDatabase method in the base class
		/// </summary>
		/// <param name="dbName"></param>
		/// <param name="logFileName"></param>
		/// <returns>The name of the created database</returns>
		/// ------------------------------------------------------------------------------------
		public string CallAttachDatabase(string dbName, string logFileName)
		{
			return AttachDatabase(dbName, logFileName);
		}

	}
	#endregion // DummyFwNewLangProjectCreator

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwNewLangProjectCreatorTests: BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when database files already exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateNewLangProject_DbFilesExist()
		{
			DummyFwNewLangProjectCreator creator = new DummyFwNewLangProjectCreator();

			// Setup: Create "pre-existing" DB filenames
			using (
				DummyFileMaker existingDB =
					new DummyFileMaker(DirectoryFinder.DataDirectory + @"\Gumby.mdf"),
					existingDB2 =
					new DummyFileMaker(DirectoryFinder.DataDirectory + @"\Gumby2.mdf"),
					existingLogFile =
					new DummyFileMaker(DirectoryFinder.DataDirectory + @"\Gumby_log.ldf"),
					existingLogFile1 =
					new DummyFileMaker(DirectoryFinder.DataDirectory + @"\Gumby1_log.ldf"))
			{
				List<string> preExistingFiles =
					new List<string>(Directory.GetFiles(DirectoryFinder.DataDirectory));
				List<string> postExistingFiles = null;

				try
				{
					string sNewDbFileName;
					string sNewLogFileName;
					creator.CallCreateNewDbFiles("Gumby", out sNewDbFileName, out sNewLogFileName);

					postExistingFiles =
						new List<string>(Directory.GetFiles(DirectoryFinder.DataDirectory));

					Assert.AreEqual(DirectoryFinder.DataDirectory + @"\Gumby3.mdf", sNewDbFileName);
					Assert.AreEqual(DirectoryFinder.DataDirectory + @"\Gumby3_log.ldf", sNewLogFileName);

					Assert.IsTrue(File.Exists(sNewDbFileName));
					Assert.IsTrue(File.Exists(sNewLogFileName));
					Assert.AreEqual(preExistingFiles.Count + 2, postExistingFiles.Count);
				}
				finally
				{
					// Blow away the files to clean things up
					if (postExistingFiles == null)
					{
						postExistingFiles =
							new List<string>(Directory.GetFiles(DirectoryFinder.DataDirectory));
					}
					foreach (string fileName in postExistingFiles)
					{
						try
						{
							if (!preExistingFiles.Contains(fileName))
								File.Delete(fileName);
						}
						catch
						{
						}
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test when a database already exists having the same DB name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateNewLangProject_DbExists()
		{
			DummyFwNewLangProjectCreator dlg = new DummyFwNewLangProjectCreator();

			List<string> preExistingFiles =
				new List<string>(Directory.GetFiles(DirectoryFinder.DataDirectory));
			List<string> postExistingFiles = null;
			string createdDBName1 = string.Empty;
			string createdDBName2 = string.Empty;

			try
			{
				string sNewDbFileName;
				string sNewLogFileName;
				dlg.CallCreateNewDbFiles("whataboutbob", out sNewDbFileName, out sNewLogFileName);
				createdDBName1 = dlg.CallAttachDatabase(sNewDbFileName, sNewLogFileName);

				dlg.CallCreateNewDbFiles("whataboutbob", out sNewDbFileName, out sNewLogFileName);
				createdDBName2 = dlg.CallAttachDatabase(sNewDbFileName, sNewLogFileName);

				postExistingFiles =
					new List<string>(Directory.GetFiles(DirectoryFinder.DataDirectory));

			}
			finally
			{
				FwNewLangProjectTests.DestroyDb(createdDBName1, false);
				FwNewLangProjectTests.DestroyDb(createdDBName2, false);
				// Blow away the files to clean things up
				if (postExistingFiles == null)
				{
					postExistingFiles =
						new List<string>(Directory.GetFiles(DirectoryFinder.DataDirectory));
				}
				foreach (string fileName in postExistingFiles)
				{
					try
					{
						if (!preExistingFiles.Contains(fileName))
							File.Delete(fileName);
					}
					catch
					{
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test handling of single quote in language project name.
		/// JIRA Issue TE-6138.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("The corresponding JIRA issue (TE-6138) has been postponed.")]
		public void CreateNewLangProject_NameWithSingleQuote()
		{
			DummyFwNewLangProjectCreator creator = new DummyFwNewLangProjectCreator();

			string dbName = "!!t'st";
			string dbFileName;
			string logFileName;

			creator.CallCreateNewDbFiles(dbName, out dbFileName, out logFileName);
			string createdName = creator.CallAttachDatabase(dbFileName, logFileName);

			Assert.AreEqual(dbName, createdName);

			string dbFileBase = Path.GetFileNameWithoutExtension(dbFileName);
			Assert.AreEqual(dbName, dbFileBase);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds the name of the culture info for language.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FindCultureInfoForLanguageName()
		{
			CultureInfo ci;
			DummyFwNewLangProjectCreator dlg = new DummyFwNewLangProjectCreator();

			// See if an entry is found for something that we know will be there
			ci = dlg.CallFindCultureInfoForLanguageName("English");
			Assert.IsNotNull(ci);

			ci = dlg.CallFindCultureInfoForLanguageName("eng");
			Assert.IsNotNull(ci);

			// test an entry that will definitely not be found
			ci = dlg.CallFindCultureInfoForLanguageName("Mailengwij");
			Assert.IsNull(ci);

			// it should find this one
			ci = dlg.CallFindCultureInfoForLanguageName("Divehi");
			Assert.IsNotNull(ci);
		}


	}
}
