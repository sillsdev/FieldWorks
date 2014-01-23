// Copyright (c) 2007-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TE_Tests.cs
// Responsibility: JonesT
//
// <remarks>
// TE_Tests.cs contains all the automated tests for the Translation Editor application.
//
// Tests will be run in file name alphabetical order, so Export_Paratext_Basic() will run
// before Export_Toolbox_Basic(), etc...
// </remarks>
// -------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Text;
using System.IO;
using GuiTestDriver;
using NUnit.Framework;
using System.Data.Common;
using System.Diagnostics;

namespace SIL.FieldWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class TE_Tests
	{
		RunTest m_rt;
		string testBranchDir = string.Empty;
		string defaultTestBranchDir = "C:\\FW";
		string testScriptsDir = @"\\Test\\GuiTestDriver\\TeScripts\\";
		int m_totalTestMinutes = 0;
		int m_totalTestSeconds = 0;

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test Fixture Setup - run once before any of the tests are run.
		/// Use the defaultTestBranchDir if the GUITESTDIR env var does not exist.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void TestFixtureSetUpInit()
		{
			testBranchDir = Environment.GetEnvironmentVariable("GUITESTDIR");
			if (testBranchDir == null)
				testBranchDir = defaultTestBranchDir;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test Fixture Tear Down - run once after all the tests are run.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TestFixtureTearDown]
		public void Dispose()
		{
			// Shut down the TE app
			new RunTest("TE").fromFile("IncShutDown.xml");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test Setup - run before each and every test.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_rt = new RunTest("TE");
			m_rt.AddVariable("TestBranchDir", testBranchDir);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// If the file is ReadOnly, make it Normal.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void Remove_ReadOnly_Attribute(string fileName)
		{
			string filePath = testBranchDir + testScriptsDir + fileName;

			// If filePath exists, then we're running in the devl environment.
			if (File.Exists(filePath))
			{
				// If the file is ReadOnly, make it Normal.
				if ((File.GetAttributes(filePath) & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
					File.SetAttributes(filePath, FileAttributes.Normal);
			}
			else
				return;
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Add_Import_Std_Fmt_Variables.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void Add_Import_Std_Fmt_Variables(DbDataReader dr)
		{
			if (dr["What to Import"].ToString().IndexOf("Entire Scripture Project") >= 0)
				m_rt.AddVariable("EntireScriptureProject", "TRUE");
			else
				m_rt.AddVariable("EntireScriptureProject", "FALSE");

			if (dr["Intro"].ToString() == "No")
				m_rt.AddVariable("ChkedBookIntros", "FALSE");
			else if (dr["Intro"].ToString() == "Yes")
				m_rt.AddVariable("ChkedBookIntros", "TRUE");

			string typesOfData = dr["Types of Data"].ToString();

			if (typesOfData.IndexOf("Scripture") >= 0)
				m_rt.AddVariable("ChkedTranslation", "TRUE");
			else
				m_rt.AddVariable("ChkedTranslation", "FALSE");

			if (typesOfData.IndexOf("BT") >= 0)
				m_rt.AddVariable("ChkedBackTrans", "TRUE");
			else
				m_rt.AddVariable("ChkedBackTrans", "FALSE");

			if (typesOfData.IndexOf("Annotations") >= 0)
				m_rt.AddVariable("ChkedAnnotations", "TRUE");
			else
				m_rt.AddVariable("ChkedAnnotations", "FALSE");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Add_Import_Std_Fmt_Compare_Variables.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void Add_Import_Std_Fmt_Compare_Variables(DbDataReader dr)
		{
			string scriptureMstrResultsFile = dr["MRF Scr"].ToString();
			if (scriptureMstrResultsFile.IndexOf("none") >= 0)
				m_rt.AddVariable("ScriptureMstrResultsFile", "FALSE");
			else
				m_rt.AddVariable("ScriptureMstrResultsFile", scriptureMstrResultsFile);

			string notesMstrResultsFile = dr["MRF Notes"].ToString();
			if (notesMstrResultsFile.IndexOf("none") >= 0)
				m_rt.AddVariable("NotesMstrResultsFile", "FALSE");
			else
				m_rt.AddVariable("NotesMstrResultsFile", notesMstrResultsFile);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Add_Import_Std_Fmt_Export_Variables.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void Add_Import_Std_Fmt_Export_Variables(DbDataReader dr)
		{
			bool scriptureFileExists =
				File.Exists("C:\\GuiTestResults\\ImportParatext6\\Export Paratext6 Scripture.sf");
			if (scriptureFileExists)
				m_rt.AddVariable("Scripture_File_Exists", "TRUE");
			else
				m_rt.AddVariable("Scripture_File_Exists", "FALSE");

			bool notesFileExists =
				File.Exists("C:\\GuiTestResults\\ImportParatext6\\Export Paratext6 Notes.sf");
			if (notesFileExists)
				m_rt.AddVariable("Notes_File_Exists", "TRUE");
			else
				m_rt.AddVariable("Notes_File_Exists", "FALSE");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Add_Import_Std_Fmt_Setup_Variables.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void Add_Import_Std_Fmt_Setup_Variables(DbDataReader dr)
		{
			// Language Project ---------------------------------------
			string malsIntro = "FALSE";
			string interleavedScripture = "FALSE";
			string langProj_ExpVal = string.Empty;

			string langProject = dr["Language Project"].ToString();
			if (langProject.IndexOf("MLYS1") >= 0)
			{
				langProj_ExpVal = "Malay-SC-NoIntro-NT (MLYS1)";
			}
			else if (langProject.IndexOf("MLYS2") >= 0)
			{
				malsIntro = "TRUE";
				langProj_ExpVal = "Malay-SC-Intro-NT (MLYS2)";
			}
			else if (langProject.IndexOf("MLYS3") >= 0)
			{
				langProj_ExpVal = "Malay-SC-NoIntro-Interleaved-NT (MLYS3)";
				interleavedScripture = "TRUE";
			}
			else if (langProject.IndexOf("MLYS4") >= 0)
			{
				malsIntro = "TRUE";
				langProj_ExpVal = "Malay-SC-Intro-Interleaved-NT (MLYS4)";
				interleavedScripture = "TRUE";
			}

			//Console.WriteLine("+++ LangProj_ExpVal: " + langProj_ExpVal);
			m_rt.AddVariable("LangProj_ExpVal", langProj_ExpVal);
			m_rt.AddVariable("Mals_Intro", malsIntro);
			m_rt.AddVariable("LangProj_Interleaved", interleavedScripture);

			// Back Trans ---------------------------------------
			string btNone = "FALSE";
			string backTrans_ExpVal = string.Empty;

			string backTrans = dr["Back Trans"].ToString();
			if (backTrans.IndexOf("(none)") >= 0)
			{
				btNone = "TRUE";
				backTrans_ExpVal = "(none)";
			}
			else if (backTrans.IndexOf("MLYB1") >= 0)
				backTrans_ExpVal = "Malay-BT-NoIntro-NT (MLYB1)";
			else if (backTrans.IndexOf("MLYB2") >= 0)
				backTrans_ExpVal = "Malay-BT-Intro-NT (MLYB2)";

			//Console.WriteLine("+++ BackTrans_ExpVal: " + backTrans_ExpVal);
			m_rt.AddVariable("BackTrans_ExpVal", backTrans_ExpVal);
			m_rt.AddVariable("BackTrans_none", btNone);

			// Trans Notes ---------------------------------------
			string malnNone = "FALSE";
			string transNotes_ExpVal = string.Empty;

			string transNotes = dr["Trans Notes"].ToString();
			if (transNotes.IndexOf("(none)") >= 0)
			{
				malnNone = "TRUE";
				transNotes_ExpVal = "(none)";
			}
			else if (transNotes.IndexOf("MLYN1") >= 0)
				transNotes_ExpVal = "Malay-NotesOnly-NoIntro-NT (MLYN1)";
			else if (transNotes.IndexOf("MLYN2") >= 0)
				transNotes_ExpVal = "Malay-Notes-Intro (MLYN2)";

			//Console.WriteLine("+++ TransNotes_ExpVal: " + transNotes_ExpVal);
			m_rt.AddVariable("TransNotes_ExpVal", transNotes_ExpVal);
			m_rt.AddVariable("TransNotes_none", malnNone);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Compute and report the time taken to execute the method, methodName.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void ComputeTimeSpan(DateTime start, String methodName)
		{
			DateTime end = DateTime.Now;
			TimeSpan span = new TimeSpan(end.Ticks - start.Ticks);

			// Keep track of the total time for each test
			m_totalTestMinutes += span.Minutes;
			m_totalTestSeconds += span.Seconds;

			string totalTime = string.Empty;
			if (span.Minutes > 0)
				totalTime = String.Format("{0} Minutes, {1} Seconds", span.Minutes.ToString(), span.Seconds.ToString());
			else
				totalTime = String.Format("{0} Seconds", span.Seconds.ToString());

			Console.WriteLine("    Finish " + methodName + " - " + totalTime);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return if the current computer is the test machine.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool RunningTestMachine()
		{
			return (Environment.MachineName == "ITS-JONEST");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Loop thru and read the rows of the Import_Paratext6.xlg excel spreadsheet.
		/// For each row, save certain column data and run the Import_Paratext6.xml test.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Category("Import")]
		public void Import_Paratext6()
		{
			bool firstRun = true;
			string connectionString = @"Provider=Microsoft.Jet.OLEDB.4.0;Data Source=C:\fw\Test\GuiTestDriver\TeScripts\ImportParatext6.xls;Extended Properties=""Excel 8.0;HDR=YES;""";

			DbProviderFactory factory = DbProviderFactories.GetFactory("System.Data.OleDb");

			using (DbConnection connection = factory.CreateConnection())
			{
				connection.ConnectionString = connectionString;

				using (DbCommand command = connection.CreateCommand())
				{
					// TestCases-ExpectedResults$ comes from the name of the worksheet
					command.CommandText =
					"SELECT [Test Case],[Language Project],[Back Trans],[Trans Notes],[What to Import],Intro,[Types of Data],[MRF Scr],[MRF Notes],Version,Date FROM [TestCases-ExpectedResults$]";

					connection.Open();

					using (DbDataReader dr = command.ExecuteReader())
					{
						while (dr.Read())
						{
							m_totalTestMinutes = 0;
							m_totalTestSeconds = 0;
							if (firstRun)
							{
								firstRun = false;
								Console.WriteLine(" ");
								Console.WriteLine("Computer Name: " + Environment.MachineName);
								if (RunningTestMachine())
									m_rt.AddVariable("Running_Test_Machine", "TRUE");
								else
									m_rt.AddVariable("Running_Test_Machine", "FALSE");
								Console.WriteLine(" ");
								Console.WriteLine("Current Date & Time:  " + DateTime.Now.ToShortDateString() + "   " + DateTime.Now.ToShortTimeString());
								Console.WriteLine(" ");
								Console.WriteLine("Test Case Table - Version: " + dr["Version"].ToString() + "   Date: " + dr["Date"].ToString());
							}
							Console.WriteLine(" ");
							Console.WriteLine("*** Test Case Number: " + dr["Test Case"].ToString());
							Console.WriteLine(" ");

							//if (RunningTestMachine())
							//    MessageBox.Show("Test Case Number: " + dr["Test Case"].ToString());

							//  ***  Restore_Project  ***
							DateTime start = DateTime.Now;
							Remove_ReadOnly_Attribute("Restore_Project.xlg");
							Console.WriteLine("1) Start    Restore_Project.xml");
							m_rt.fromFile("Restore_Project.xml");
							Init(); // Create a new TE RunTest
							ComputeTimeSpan(start, "Restore_Project.xml");

							//  ***  Import Setup  ***
							DateTime start2 = DateTime.Now;
							Remove_ReadOnly_Attribute("Import_Std_Fmt_Setup.xlg");
							Add_Import_Std_Fmt_Setup_Variables(dr);
							Console.WriteLine("2) Start    Import_Std_Fmt_Setup.xml");
							m_rt.fromFile("Import_Std_Fmt_Setup.xml");
							Init(); // Create a new TE RunTest
							ComputeTimeSpan(start2, "Import_Std_Fmt_Setup.xml");

							//  ***  Import ***
							DateTime start3 = DateTime.Now;
							Remove_ReadOnly_Attribute("Import_Std_Fmt.xlg");
							Add_Import_Std_Fmt_Variables(dr);
							Console.WriteLine("3) Start    Import_Std_Fmt.xml");
							m_rt.fromFile("Import_Std_Fmt.xml");
							Init();
							ComputeTimeSpan(start3, "Import_Std_Fmt.xml");

							//  ***  Export  ***
							DateTime start4 = DateTime.Now;
							Remove_ReadOnly_Attribute("Import_Std_Fmt_Export.xlg");
							Add_Import_Std_Fmt_Export_Variables(dr);
							Console.WriteLine("4) Start    Import_Std_Fmt_Export.xml");
							m_rt.fromFile("Import_Std_Fmt_Export.xml");
							Init();
							ComputeTimeSpan(start4, "Import_Std_Fmt_Export.xml");

							// ***  Compare  ***
							DateTime start5 = DateTime.Now;
							Remove_ReadOnly_Attribute("Import_Std_Fmt_Compare.xlg");
							Add_Import_Std_Fmt_Compare_Variables(dr);
							Console.WriteLine("5) Start    Import_Std_Fmt_Compare.xml");
							m_rt.fromFile("Import_Std_Fmt_Compare.xml");
							Init();
							ComputeTimeSpan(start5, "Import_Std_Fmt_Compare.xml");

							// Report the Total Time for the test
							Console.WriteLine("--------------------------------------------------------");
							TimeSpan totalTimeSpan = new TimeSpan(0, m_totalTestMinutes, m_totalTestSeconds);
							string totaltestTime = String.Format("{0} Minutes, {1} Seconds",
								totalTimeSpan.Minutes.ToString(), totalTimeSpan.Seconds.ToString());
							Console.WriteLine("Total Test Time - " + totaltestTime);
							Console.WriteLine("===============================");
						}
					}
				}
			}
			Console.WriteLine(" ");
			Console.WriteLine("TEST ENDED");
			Console.WriteLine("Current Date & Time:  " + DateTime.Now.ToShortDateString() + "   " + DateTime.Now.ToShortTimeString());
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests Export.
		/// </summary>
		///--------------------------------------------------------------------------------------
		//[Test]
		//[Category("Export")]
		//public void Export_Paratext_Basic()
		//{
		//    Remove_ReadOnly_Attribute("Export_Paratext.xlg");
		//    m_rt.fromFile("Export_Paratext.xml");
		//}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test1.
		/// </summary>
		///--------------------------------------------------------------------------------------
		//[Test]
		//[Category("Startup App")]
		//public void Export_Test1()
		//{
		//    Remove_ReadOnly_Attribute("Export_Test1.xlg");
		//    m_rt.fromFile("Export_Test1.xml");
		//}
	}
}
