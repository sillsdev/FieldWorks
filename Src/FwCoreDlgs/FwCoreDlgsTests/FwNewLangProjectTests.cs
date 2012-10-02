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
// File: TestFwNewLangProject.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Globalization;
using System.Resources;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Test.TestUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region DummyFwNewLangProject
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyFwNewLangProject : FwNewLangProject
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void CreateNewLangProj()
		{
			CheckDisposed();

			base.CreateNewLangProjWithProgress();
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
	}
	#endregion // DummyFwNewLangProject

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for TestFwNewLangProject.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwNewLangProjectTests: BaseTest
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure a new DB gets created
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("This test needs some tweaking - currently it fails because there was a pro-form added")]
		[Category("LongRunning")]
		public void CreateNewLangProject()
		{
			DummyFwNewLangProject dlg = new DummyFwNewLangProject();

			string dbName = "Maileingwij2025";
			FdoCache cache = null;
			if (DbExists(dbName))
				DestroyDb(dbName, true);

			dlg.ProjectName = dbName;
			try
			{
				dlg.CreateNewLangProj();

				Assert.IsTrue(DbExists(dbName));

				//				// REVIEW: An FDO cache can not be created at this time until the scripture
				//				// object is created. This happens when the database is loaded.
				//				// Check to see if the writing systems were added correctly.
				//				cache = FdoCache.Create(dbName);
				//				Assert.AreEqual(dbName, cache.LangProject.Name.AnalysisDefaultWritingSystem);
				//				Assert.AreEqual(1, cache.LangProject.AnalysisWssRC.Count);
				//				Assert.AreEqual(1, cache.LangProject.VernWssRC.Count);
				//				Assert.AreEqual(1, cache.LangProject.CurAnalysisWssRS.Count);
				//				Assert.AreEqual(1, cache.LangProject.CurVernWssRS.Count);
				//
				//				foreach (LgWritingSystem ws in cache.LangProject.AnalysisWssRC)
				//				{
				//					Assert.AreEqual("something", ws.Name.GetAlternative(cache.DefaultUserWs));
				//				}
				//				foreach (LgWritingSystem ws in cache.LangProject.VernWssRC)
				//				{
				//					Assert.AreEqual(dbName, ws.Name.GetAlternative(cache.DefaultUserWs));
				//				}
				//				foreach (LgWritingSystem ws in cache.LangProject.CurAnalysisWssRS)
				//				{
				//					Assert.AreEqual("something", ws.Name.GetAlternative(cache.DefaultUserWs));
				//				}
				//				foreach (LgWritingSystem ws in cache.LangProject.CurVernWssRS)
				//				{
				//					Assert.AreEqual(dbName, ws.Name.GetAlternative(cache.DefaultUserWs));
				//				}

				cache = FdoCache.Create(dbName);
				CheckInitialSetOfPartsOfSpeech(cache);
			}
			finally
			{
				// Blow away the database to clean things up
				if (cache != null)
					cache.Dispose();
				DestroyDb(dbName, false);
			}
		}

		private void CheckInitialSetOfPartsOfSpeech(FdoCache cache)
		{
			ILangProject lp = cache.LangProject;
			int iCount = 0;
			bool fAdverbFound = false;
			bool fNounFound = false;
			bool fPronounFound = false;
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
					case "Pronoun":
						fPronounFound = true;
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
			Assert.AreEqual(iCount, 4, "Expect four initial POSes.");
			Assert.IsTrue(fAdverbFound, "Did not find Adverb CatalogSourceId");
			Assert.IsTrue(fNounFound, "Did not find Noun CatalogSourceId");
			Assert.IsTrue(fPronounFound, "Did not find Pronoun CatalogSourceId");
			Assert.IsTrue(fVerbFound, "Did not find Verb CatalogSourceId");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// See if the given FW database exists by attempting to establish a connection to it.
		/// </summary>
		/// <param name="dbName">Name of the FW database to look for</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private bool DbExists(string dbName)
		{
			try
			{
				using (SqlConnection sqlCon = new SqlConnection("Server=" +
					MiscUtils.LocalServerName + "; Database=" + dbName +
					"; User ID = sa; Password=inscrutable; Pooling=false;"))
				{
					sqlCon.Open();
					sqlCon.Close();
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Blow away the given FW database.
		/// </summary>
		/// <param name="dbName">Name of the FW database to smoke</param>
		/// <param name="failureIsFatal">If true, then failure to delete will fail tests.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		internal static void DestroyDb(string dbName, bool failureIsFatal)
		{
			using (SqlConnection sqlConMaster = new SqlConnection(
				string.Format("Server={0}; Database=master; User ID = sa; Password=inscrutable;" +
				" Pooling=false;", MiscUtils.LocalServerName)))
			{
				sqlConMaster.Open();

				try
				{
					using (SqlCommand dropCommand = sqlConMaster.CreateCommand())
					{
						dropCommand.CommandText = string.Format("drop database {0}", dbName);
						dropCommand.ExecuteNonQuery();
					}
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
}
