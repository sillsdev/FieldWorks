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
// File: FwNewLangProjectCreator.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Resources;
using System.Text;
using System.Globalization;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>Holds the return data from the creation of a new language project</summary>
	internal struct NewLangProjReturnData
	{
		public int m_hvoLp;
		public string m_dbName;
		public string m_serverName;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:NewLangProjReturnData"/> class.
		/// </summary>
		/// <param name="hvoLp">The hvo of the language project.</param>
		/// <param name="serverName">The name of the database server.</param>
		/// <param name="dbName">Name of the new database.</param>
		/// ------------------------------------------------------------------------------------
		public NewLangProjReturnData(int hvoLp, string serverName, string dbName)
		{
			m_hvoLp = hvoLp;
			m_serverName = serverName;
			m_dbName = dbName;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Helper class that does all the hard work of creating a new language project database.
	/// </summary>
	/// <remarks>Note: This class can't access any UI elements since it runs on a background
	/// thread.</remarks>
	/// ----------------------------------------------------------------------------------------
	public class FwNewLangProjectCreator
	{
		private string m_dbName;
		private string m_serverName;
		private NamedWritingSystem m_analWrtSys;
		private NamedWritingSystem m_vernWrtSys;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new language project for the new language name
		/// </summary>
		/// <param name="progressDlg">The progress dialog.</param>
		/// <param name="parameters">The parameters: first parameter is resources, second is
		/// name of the project (string), analysis and vernacular writing system
		/// (NamedWritingSystem).</param>
		/// <returns>Always null.</returns>
		/// <remarks>Override DisplayUi to prevent progress dialog from showing.</remarks>
		/// ------------------------------------------------------------------------------------
		protected internal object CreateNewLangProj(IAdvInd4 progressDlg, params object[] parameters)
		{
			Debug.Assert(parameters.Length == 4);
			ResourceManager resources = (ResourceManager)parameters[0];
			string projectName = (string)parameters[1];
			m_analWrtSys = (NamedWritingSystem)parameters[2];
			m_vernWrtSys = (NamedWritingSystem)parameters[3];
			string dbFileName;
			string logFileName;

			// Loading the new language project (LoadNewLangProj) took 120 steps. To be safe
			// we calculate with 140. Loading newlangproj took 94% of the time, so the total is
			// 150.
			int nMax = 150;
			progressDlg.SetRange(0, nMax);
			if (resources != null)
				progressDlg.Message = resources.GetString("kstidCreatingDB");
			CreateNewDbFiles(projectName, out dbFileName, out logFileName);

			progressDlg.Step(0);
			m_dbName = AttachDatabase(dbFileName, logFileName);

			progressDlg.Step(0);
			if (resources != null)
				progressDlg.Message = resources.GetString("kstidInitializingDB");
			LoadNewLangProj(progressDlg);

			// Create the FDO cache and writing systems.
			progressDlg.Position = (int)(nMax * 0.95);
			if (resources != null)
				progressDlg.Message = resources.GetString("kstidCreatingWS");

			int hvoLp = 0;
			using (FdoCache cache = FdoCache.Create(m_dbName))
			{
				progressDlg.Step(0);
				CreateAnalysisWritingSystem(cache);
				progressDlg.Step(0);
				CreateVernacularWritingSystem(cache);

				// Fix sort methods that should use the vernacular writing system.
				progressDlg.Step(0);
				progressDlg.Step(0);
				FixVernacularWritingSystemReferences(cache);
				progressDlg.Step(0);

				// set defaults so can access them now
				cache.LangProject.CacheDefaultWritingSystems();
				progressDlg.Step(0);
				AssignVernacularWritingSystemToDefaultPhPhonemes(cache);
				progressDlg.Step(0);

				// Create a reversal index for the original default analysis writing system. (LT-4480)
				IReversalIndex newIdx = cache.LangProject.LexDbOA.ReversalIndexesOC.Add(
					new ReversalIndex());
				ILgWritingSystem wsAnalysis = cache.LangProject.CurAnalysisWssRS[0];
				newIdx.WritingSystemRA = wsAnalysis;
				newIdx.Name.AnalysisDefaultWritingSystem = wsAnalysis.Name.AnalysisDefaultWritingSystem;
				progressDlg.Step(0);

				// Give the language project a default name. Later this can be modified by the
				// user by changing it on the project properties dialog.
				cache.LangProject.Name.UserDefaultWritingSystem = projectName;
				hvoLp = cache.LangProject.Hvo;
				cache.Save();
				progressDlg.Position = nMax;
			}

			return new NewLangProjReturnData(hvoLp, m_serverName, m_dbName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Build the file names for the new database and copy the template files.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateNewDbFiles(string projectName, out string dbFileName,
			out string logFileName)
		{
			m_dbName = MiscUtils.FilterForFileName(projectName,
				MiscUtils.FilenameFilterStrength.kFilterMSDE);

			dbFileName = DirectoryFinder.DataDirectory + @"\" + m_dbName + ".mdf";
			logFileName = DirectoryFinder.DataDirectory + @"\" + m_dbName + "_log.ldf";

			// If the dest file already exists then append a number to it to make it unique.
			for (int fileNum = 1; File.Exists(dbFileName) || File.Exists(logFileName); fileNum++)
			{
				dbFileName = DirectoryFinder.DataDirectory + @"\" + m_dbName + fileNum + ".mdf";
				logFileName = DirectoryFinder.DataDirectory + @"\" + m_dbName + fileNum + "_log.ldf";
			}

			try
			{
				// Make a copy of the template database that will become the new database
				File.Copy(DirectoryFinder.TemplateDirectory + @"\BlankLangProj.mdf", dbFileName, false);
				File.Copy(DirectoryFinder.TemplateDirectory + @"\BlankLangProj_log.ldf", logFileName, false);
			}
			catch (Exception e)
			{
				throw new ApplicationException("CreateNewDbFiles", e);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the NewLangProj.xml file into the new database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void LoadNewLangProj(IAdvInd4 progressDlg)
		{
			try
			{
				// Load the NewLangProj.xml into the new database.
				IFwXmlData xmlDataLoader = FwXmlDataClass.Create();
				xmlDataLoader.Open(m_serverName, m_dbName);
				xmlDataLoader.LoadXml(DirectoryFinder.TemplateDirectory + @"\NewLangProj.xml",
					progressDlg);
				xmlDataLoader.Close();
			}
			catch (Exception)
			{
				// TODO: Handle any XML loading errors
				throw;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attach the new database to the server.
		/// </summary>
		/// <returns>The name of the database that was created.</returns>
		/// ------------------------------------------------------------------------------------
		protected string AttachDatabase(string dbFileName, string logFileName)
		{
			int dbNumber = 0;
			string dbName;
			m_serverName = MiscUtils.LocalServerName;
			SqlConnection sqlConMaster = new SqlConnection(
				string.Format("Server={0}; Database=master; User ID = sa; Password=inscrutable; " +
					"Pooling=false;", m_serverName));
			sqlConMaster.Open();

			try
			{
				while (true)
				{
					try
					{
						// Attach the new database
						SqlCommand attachCommand = sqlConMaster.CreateCommand();
						if (dbNumber != 0)
							dbName = m_dbName + dbNumber.ToString();
						else
							dbName = m_dbName;
						attachCommand.CommandText = string.Format("EXEC sp_attach_db N'{0}', N'{1}', N'{2}'",
							dbName, dbFileName, logFileName);
						attachCommand.ExecuteNonQuery();
					}
					catch (SqlException sqle)
					{
						// If the database already exists then increment the number and try again.
						// This will append a number to the database name and increment it until a
						// unique database name is found.
						if (sqle.Number == 1801)	// 1801 = database already exists
							dbNumber++;
						else
							throw sqle;
						continue;
					}
					break;
				}
			}
			finally
			{
				sqlConMaster.Close();
			}
			return dbName;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finds or creates the writing system.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="icuLocale">The icu locale.</param>
		/// <returns>The writing system</returns>
		/// ------------------------------------------------------------------------------------
		private LgWritingSystem FindOrCreateWs(FdoCache cache, string icuLocale)
		{
			// Look to see if the writing system already exists in the database
			foreach (LgWritingSystem ws in cache.LanguageEncodings)
			{
				if (LanguageDefinition.SameLocale(ws.ICULocale, icuLocale))
					return ws;
			}

			// Create a new writing system based on the one noted in the XML file.
			// Load it in from the XML and save into the database.
			LanguageDefinitionFactory ldf =
				new LanguageDefinitionFactory(cache.LanguageWritingSystemFactoryAccessor, icuLocale);
			ldf.LanguageDefinition.SaveWritingSystem(icuLocale);
			cache.ResetLanguageEncodings();
			// search again. It better be there now!
			foreach (LgWritingSystem ws in cache.LanguageEncodings)
			{
				if (LanguageDefinition.SameLocale(ws.ICULocale, icuLocale))
					return ws;
			}
			Debug.Assert(false);
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set an analysis writing system in the database and create one if needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void CreateAnalysisWritingSystem(FdoCache cache)
		{
			string icuLocale = (m_analWrtSys == null) ? "en" : m_analWrtSys.IcuLocale;
			LgWritingSystem wsAnalysis = FindOrCreateWs(cache, icuLocale);

			// Add the writing system to the list of Analysis writing systems and make it the
			// first one in the list of current Analysis writing systems.
			cache.LangProject.AnalysisWssRC.Add(wsAnalysis);
			cache.LangProject.CurAnalysisWssRS.InsertAt(wsAnalysis, 0);

			// hard wire to "en" for now - future should be below: "UserWs"
			if ("en" != icuLocale)
			{
				// TE-985: Don't need to tell the user we're doing this when they don't have a choice anyway.
				//				// show the message box with the information about adding it.
				//				string msg = (res != null) ? res.GetString("kstidAutoAddEnglish") : string.Empty;
				//				string caption = (res != null) ? string.Format(res.GetString("kstidCreateLangProjCaption"),
				//					ProjectName) : string.Empty;
				//
				//				MessageBox.Show(msg, caption, MessageBoxButtons.OK,
				//					MessageBoxIcon.Information);

				// Add the "en" writing system to the list of Analysis writing systems and
				// append it to the the list of current Analysis writing systems.
				LgWritingSystem lgwsEN = FindOrCreateWs(cache, "en");
				cache.LangProject.AnalysisWssRC.Add(lgwsEN);
				cache.LangProject.CurAnalysisWssRS.Append(lgwsEN);
			}

			//			// get UI writing system
			//			int wsUser = cache.LanguageWritingSystemFactoryAccessor.UserWs;
			//			string icuUILocale = cache.LanguageWritingSystemFactoryAccessor.GetStrFromWs(wsUser);
			//			if (icuUILocale != icuLocale)
			//			{
			//				// Add the UI writing system to the list of Vernacular writing systems
			//				LgWritingSystem lgwsUI = FindOrCreateWs(cache, icuUILocale);
			//				cache.LangProject.AnalysisWssRC.Add(lgwsUI);
			//			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Attempt to find the language name in the Windows culture information
		/// </summary>
		/// <param name="languageName"></param>
		/// <returns>Windows CultureInfo</returns>
		/// ------------------------------------------------------------------------------------
		protected CultureInfo FindCultureInfoForLanguageName(string languageName)
		{
			// query for all of the cultures that Windows knows about (country non-specific)
			CultureInfo[] availCultures = CultureInfo.GetCultures(CultureTypes.NeutralCultures);

			languageName = languageName.Trim().ToLower();

			// Don't match any names that are 3 times the length of the given name
			int bestMatchLength = languageName.Length * 3;
			string bestMatch = null;
			CultureInfo bestCI = null;

			// Look for the best match.  The best match is the name that starts with the given
			// name and is the closest in length.
			foreach (CultureInfo ci in availCultures)
			{
				if (ci.NativeName.ToLower().StartsWith(languageName))
				{
					if (ci.NativeName.Length < bestMatchLength)
					{
						bestMatch = ci.Name;
						bestMatchLength = ci.NativeName.Length;
						bestCI = ci;
					}
				}
				if (ci.EnglishName.ToLower().StartsWith(languageName))
				{
					if (ci.NativeName.Length < bestMatchLength)
					{
						bestMatch = ci.Name;
						bestMatchLength = ci.NativeName.Length;
						bestCI = ci;
					}
				}
				if (ci.DisplayName.ToLower().StartsWith(languageName))
				{
					if (ci.NativeName.Length < bestMatchLength)
					{
						bestMatch = ci.Name;
						bestMatchLength = ci.NativeName.Length;
						bestCI = ci;
					}
				}
				if (languageName.Length == bestMatchLength)
					break;
			}

			// return what we found
			return bestCI;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new Vernacular writing system based on the name given.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void CreateVernacularWritingSystem(FdoCache cache)
		{
			string icuLocale = (m_vernWrtSys == null) ? "fr" : m_vernWrtSys.IcuLocale;
			LgWritingSystem wsVern = FindOrCreateWs(cache, icuLocale);

			// Add the writing system to the list of Vernacular writing systems and make it the
			// first one in the list of current Vernacular writing systems.
			cache.LangProject.VernWssRC.Add(wsVern);
			cache.LangProject.CurVernWssRS.InsertAt(wsVern, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fix any writing system references in the database that should be using the newly
		/// created vernacular writing system. Currently this only affects Sort methods.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void FixVernacularWritingSystemReferences(FdoCache cache)
		{
			// Force the first vernacular writing system into the canned sort specs coming
			// from NewLangProj.xml where they refer to names or abbreviations in people and
			// location lists, since these default to showing vernacular languages first.
			string sSql = "declare @ws int " +
				"select top 1 @ws = Dst from LangProject_CurVernWss " +
				"update CmSortSpec set PrimaryWs = @ws " +
				"  where PrimaryField in ('13,13009,7001','13,13006,7001','13,7001','13,7002'," +
					"'12,7001','12,7002','4006,4006003,7001','4006,4006007,7001','4006,4006002,7001') " +
				"update CmSortSpec set SecondaryWs = @ws " +
				"  where SecondaryField in ('13,13009,7001','13,13006,7001','13,7001','13,7002'," +
				"'12,7001','12,7002','4006,4006003,7001','4006,4006007,7001','4006,4006002,7001') " +
				"update CmSortSpec set TertiaryWs = @ws " +
				"  where TertiaryField in ('13,13009,7001','13,13006,7001','13,7001','13,7002'," +
					"'12,7001','12,7002','4006,4006003,7001','4006,4006007,7001','4006,4006002,7001')";

			IOleDbCommand odc = null;
			cache.DatabaseAccessor.CreateCommand(out odc);
			try
			{
				odc.ExecCommand(sSql, (int)SqlStmtType.knSqlStmtNoResults);
			}
			finally
			{
				DbOps.ShutdownODC(ref odc);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Assign the Vernacular writing system to all default PhCodes and PhPhoneme Names.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void AssignVernacularWritingSystemToDefaultPhPhonemes(FdoCache cache)
		{
			// For all PhCodes in the default phoneme set, change the writing system from "en" to icuLocale
			IPhPhonemeSet phSet = cache.LangProject.PhonologicalDataOA.PhonemeSetsOS[0];
			foreach (IPhPhoneme phone in phSet.PhonemesOC)
			{
				foreach (IPhCode code in phone.CodesOS)
				{
					code.Representation.VernacularDefaultWritingSystem = code.Representation.UserDefaultWritingSystem;
				}
				phone.Name.VernacularDefaultWritingSystem = phone.Name.UserDefaultWritingSystem;
			}
			foreach (IPhBdryMarker mrkr in phSet.BoundaryMarkersOC)
			{
				foreach (IPhCode code in mrkr.CodesOS)
				{
					code.Representation.VernacularDefaultWritingSystem = code.Representation.UserDefaultWritingSystem;
				}
				mrkr.Name.VernacularDefaultWritingSystem = mrkr.Name.UserDefaultWritingSystem;
			}
		}


	}
}
