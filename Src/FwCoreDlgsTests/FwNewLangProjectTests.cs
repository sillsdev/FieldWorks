// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary />
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "Tests time out")]
	public class FwNewLangProjectTests
	{
		/// <summary>
		/// Make sure a new DB gets created
		/// </summary>
		[Test]
		[Category("LongRunning")]
		public void CreateNewLangProject()
		{
			const string dbName = "Maileingwij2025";

			using (var dlg = new DummyFwNewLangProject())
			{
				LcmCache cache = null;
				if (DbExists(dbName))
				{
					DestroyDb(dbName, true);
				}
				dlg.setProjectName(dbName);
				try
				{
					dlg.CreateNewLangProj();

					Assert.IsTrue(DbExists(dbName));

					// despite of the name is DummyProgressDlg no real dialog (doesn't derive from Control), so
					// we don't need a 'using'
					cache = LcmCache.CreateCacheFromExistingData(new TestProjectId(BackendProviderType.kXMLWithMemoryOnlyWsMgr, DbFilename(dbName)), "en", new DummyLcmUI(),
						FwDirectoryFinder.LcmDirectories, new LcmSettings(), new DummyProgressDlg());
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
					cache?.Dispose();
					DestroyDb(dbName, false);
				}
			}
		}

		private void CheckInitialSetOfPartsOfSpeech(LcmCache cache)
		{
			var lp = cache.LanguageProject;
			var iCount = 0;
			var fAdverbFound = false;
			var fNounFound = false;
			var fProformFound = false;
			var fVerbFound = false;
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
						var sFmt = "Unexpected CatalogSourceId ({0}) found in PartOfSpeech {1}.";
						var sMsg = String.Format(sFmt, pos.CatalogSourceId, pos.Name.AnalysisDefaultWritingSystem);
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
			return Path.Combine(DbDirectory(dbName), LcmFileHelper.GetXmlDataFileName(dbName));
		}

		/// <summary>
		/// See if the given FW database exists by attempting to establish a connection to it.
		/// </summary>
		/// <param name="dbName">Name of the FW database to look for</param>
		/// <returns>true iff the underlying file exists.</returns>
		private static bool DbExists(string dbName)
		{
			return File.Exists(DbFilename(dbName));
		}

		/// <summary>
		/// Blow away the given FW database.
		/// </summary>
		/// <param name="dbName">Name of the FW database to smoke</param>
		/// <param name="failureIsFatal">If true, then failure to delete will fail tests.</param>
		internal static void DestroyDb(string dbName, bool failureIsFatal)
		{
			try
			{
				Directory.Delete(DbDirectory(dbName), true);
			}
			catch
			{
				var msg = "The test database " + dbName + " could not be deleted.";
				if (failureIsFatal)
				{
					Assert.Fail(msg);
				}
				else
				{
					Debug.WriteLine(msg);
				}
			}
		}

		/// <summary>
		/// Subclass of FwNewLangProject core dialog for testing purposes.
		/// </summary>
		private sealed class DummyFwNewLangProject : FwNewLangProject
		{
			/// <summary>
			/// Initializes a new instance of the <see cref="DummyFwNewLangProject"/> class.
			/// </summary>
			internal DummyFwNewLangProject()
				: base(true)
			{
			}

			/// <summary />
			internal void CreateNewLangProj()
			{
				CreateNewLangProjWithProgress();
			}

			/// <summary>
			/// Gets a value indicating whether to display the progress dialog.
			/// </summary>
			protected override bool DisplayUi => false;

			/// <summary>
			/// Sets the project name programmatically (bypasses Windows Forms length checking)
			/// </summary>
			internal void setProjectName(string name)
			{
				ProjectName = name;
			}
		}
	}
}