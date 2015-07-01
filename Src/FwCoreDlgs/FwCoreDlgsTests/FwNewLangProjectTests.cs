// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TestFwNewLangProject.cs
// Responsibility:
//
// <remarks>
// </remarks>

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region DummyFwNewLangProject
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Subclass of FwNewLangProject core dialog for testing purposes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwNewLangProject : FwNewLangProject
	{

		/// <summary/>
		public void CreateNewLangProj()
		{
			CheckDisposed();

			CreateNewLangProjWithProgress();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to display the progress dialog.
		/// </summary>
		/// <value>Always <c>false</c> because we don't want to display a progress dialog when
		/// running tests.</value>
		/// ------------------------------------------------------------------------------------
		protected override bool DisplayUi
		{
			get { return false; }
		}

		/// <summary>
		/// Try out the OK button
		/// </summary>
		internal void TestOkButton()
		{
			btnOK.PerformClick();
		}

		/// <summary>
		/// Sets the project name programmatically (bypasses Windows Forms length checking)
		/// </summary>
		internal void setProjectName(string name)
		{
			ProjectName = name;
		}
	}
	#endregion // DummyFwNewLangProject

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TestFwNewLangProject.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "Tests time out")]
	public class FwNewLangProjectTests: BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure a new DB gets created
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void CreateNewLangProject()
		{
			const string dbName = "Maileingwij2025";
			string storePath = FdoFileHelper.GetWritingSystemDir(Path.Combine(FwDirectoryFinder.ProjectsDirectory, dbName));
			string sharedStorePath = DirectoryFinder.GlobalWritingSystemStoreDirectory;

			using (var dlg = new DummyFwNewLangProject())
			{
				FdoCache cache = null;
				if (DbExists(dbName))
					DestroyDb(dbName, true);

				dlg.setProjectName(dbName);
				try
				{
					dlg.CreateNewLangProj();

					Assert.IsTrue(DbExists(dbName));

					// despite of the name is DummyProgressDlg no real dialog (doesn't derive from Control), so
					// we don't need a 'using'
					cache = FdoCache.CreateCacheFromExistingData(
						new TestProjectId(FDOBackendProviderType.kXML, DbFilename(dbName)), "en", new DummyFdoUI(), FwDirectoryFinder.FdoDirectories,
						new FdoSettings(), new DummyProgressDlg());
					CheckInitialSetOfPartsOfSpeech(cache);

					Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.Count);
					Assert.AreEqual("English", cache.ServiceLocator.WritingSystems.AnalysisWritingSystems.First().LanguageName);
					Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.CurrentAnalysisWritingSystems.Count);
					Assert.AreEqual("English", cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.LanguageName);
					Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Count);
					Assert.AreEqual("French", cache.ServiceLocator.WritingSystems.VernacularWritingSystems.First().LanguageName);
					Assert.AreEqual(1, cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Count);
					Assert.AreEqual("French", cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.LanguageName);
				}
				finally
				{
					// Blow away the database to clean things up
					if (cache != null)
						cache.Dispose();
					DestroyDb(dbName, false);
				}
			}
		}
		private void CheckInitialSetOfPartsOfSpeech(FdoCache cache)
		{
			ILangProject lp = cache.LanguageProject;
			int iCount = 0;
			bool fAdverbFound = false;
			bool fNounFound = false;
			bool fProformFound = false;
			bool fVerbFound = false;
			foreach (IPartOfSpeech pos in lp.PartsOfSpeechOA.PossibilitiesOS)
			{
				iCount++;
				switch (pos.CatalogSourceId)
				{
					case "Adverb":
						fAdverbFound = true;
						break;
					case "Noun":
						fNounFound = true;
						break;
					case "Pro-form":
						fProformFound = true;
						break;
					case "Verb":
						fVerbFound = true;
						break;
					default:
						string sFmt = "Unexpected CatalogSourceId ({0}) found in PartOfSpeech {1}.";
						string sMsg = String.Format(sFmt, pos.CatalogSourceId, pos.Name.AnalysisDefaultWritingSystem);
						Assert.Fail(sMsg);
						break;
				}
			}
			Assert.AreEqual(4, iCount, "Expect four initial POSes.");
			Assert.IsTrue(fAdverbFound, "Did not find Adverb CatalogSourceId");
			Assert.IsTrue(fNounFound, "Did not find Noun CatalogSourceId");
			Assert.IsTrue(fProformFound, "Did not find Pro-form CatalogSourceId");
			Assert.IsTrue(fVerbFound, "Did not find Verb CatalogSourceId");
		}

		private static string DbDirectory(string dbName)
		{
			return Path.Combine(FwDirectoryFinder.ProjectsDirectory, dbName);
		}

		private static string DbFilename(string dbName)
		{
			return Path.Combine(DbDirectory(dbName), FdoFileHelper.GetXmlDataFileName(dbName));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the given FW database exists by attempting to establish a connection to it.
		/// </summary>
		/// <param name="dbName">Name of the FW database to look for</param>
		/// <returns>true iff the underlying file exists.</returns>
		/// ------------------------------------------------------------------------------------
		private static bool DbExists(string dbName)
		{
			string sFile = DbFilename(dbName);
			return File.Exists(sFile);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Blow away the given FW database.
		/// </summary>
		/// <param name="dbName">Name of the FW database to smoke</param>
		/// <param name="failureIsFatal">If true, then failure to delete will fail tests.</param>
		/// ------------------------------------------------------------------------------------
		internal static void DestroyDb(string dbName, bool failureIsFatal)
		{
			try
			{
				string dir = DbDirectory(dbName);
				Directory.Delete(dir, true);
			}
			catch
			{
				string msg = "The test database " + dbName + " could not be deleted.";
				if (failureIsFatal)
					Assert.Fail(msg);
				else
					Debug.WriteLine(msg);
			}
		}
	}
}
