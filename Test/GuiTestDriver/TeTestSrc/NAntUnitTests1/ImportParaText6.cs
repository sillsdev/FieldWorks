// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2003' to='2006' company='SIL International'>
//		Copyright (c) 2006, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ImportParaText6.cs
// Responsibility: JonesT, LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Threading;
using NUnit.Framework;
using System.IO;
using System.Xml;
using System.Data.SqlClient;
using System.Web;

namespace GuiTestDriver
{
	/// <summary>
	/// Test scripts for Importing Paratext 6 projects.
	/// </summary>
	[TestFixture]
	public class ImportParatext6
	{
		RunTest m_rt;
		string m_testBranchDir = string.Empty;
		string m_defaultTestBranchDir = "C:\\FW";
		int m_testCaseNumber = 0;
		string m_testPlan = string.Empty;
		XmlNodeList m_xmlTestCases;
		bool m_displayXmlScriptTimeSpan = true;
		bool m_skipWaitForImportExtraSleep = false;
		TimeSpan m_totalTimeSpan = new TimeSpan(0,0,0);

		public ImportParatext6()
		{
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test Fixture Setup - run once before any of the tests are run.
		/// Use the defaultTestBranchDir if the GUITESTDIR env var does not exist.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public void TestFixtureSetUpInit()
		{
			m_testBranchDir = Environment.GetEnvironmentVariable("GUITESTDIR");
			if (m_testBranchDir == null)
				m_testBranchDir = m_defaultTestBranchDir;

			Console.WriteLine(" ");
			Console.WriteLine("Computer Name: " + Environment.MachineName);
			Console.WriteLine(" ");
			Console.WriteLine("Current Date & Time:  " + DateTime.Now.ToShortDateString() + "   " + DateTime.Now.ToShortTimeString());
			Console.WriteLine(" ");
		}

		/////--------------------------------------------------------------------------------------
		///// <summary>
		///// Test Fixture TearDown - run once after all the tests are run.
		///// </summary>
		/////--------------------------------------------------------------------------------------
		//[TestFixtureTearDown]
		//public void Cleanup()
		//{
		//}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test Setup - run before each and every test.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[SetUp]
		public void Init()
		{
			m_rt = new RunTest("TE");
			m_rt.AddVariable("TestBranchDir", m_testBranchDir);
			m_rt.AddVariable("TestCase", m_testCaseNumber.ToString());

			#region // Save MachineName
			//if (Running_On_745())
			//    m_rt.AddVariable("Running_On_745", "TRUE");
			//else
			//    m_rt.AddVariable("Running_On_745", "FALSE");

			//if (Running_On_GX270())
			//    m_rt.AddVariable("Running_On_GX270", "TRUE");
			//else
			//    m_rt.AddVariable("Running_On_GX270", "FALSE");
			#endregion
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tear Down - run after each and every test.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[TearDown]
		public void Dispose()
		{
			// Shut down the TE app
			new RunTest("TE").fromFile("IncShutDown.xml");
		}

		#region // Determine MachineName
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the current computer is the development machine.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool Running_On_745()
		{
			return (Environment.MachineName == "SWD-JONES");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the current computer is the test machine.
		/// </summary>
		///--------------------------------------------------------------------------------------
		//public bool Running_On_GX270()
		//{
		//    return (Environment.MachineName == "ITS-JONEST");
		//}
		#endregion

		#region // Determine Test Plan
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the test plan is 'Import Paratext 5'.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool ImportPt5TestPlan()
		{
			return (m_testPlan == "ImportPt5");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the test plan is 'Import Paratext 5 BT'.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool ImportPt5BTTestPlan()
		{
			return (m_testPlan == "ImportPt5BT");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the test plan is 'Import Paratext 6'.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool ImportPt6TestPlan()
		{
			return (m_testPlan == "ImportPt6");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the test plan is 'Import Paratext 6 BT'.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool ImportPt6BTTestPlan()
		{
			return (m_testPlan == "ImportPt6BT");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the test plan is 'ImportOtherSF'.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool ImportOtherSFTestPlan()
		{
			return (m_testPlan == "ImportOtherSF");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the test plan is 'ImportOtherSFSepBT'.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public bool ImportOtherSFSepBTTestPlan()
		{
			return (m_testPlan == "ImportOtherSFSepBT");
		}
		#endregion

		#region // Compute Time Span
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Compute and report the time taken to execute the test case.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void ComputeTestCaseTimeSpan()
		{
			string totalTime = string.Empty;
			if (m_totalTimeSpan.Minutes > 0)
				totalTime = String.Format("{0} Minutes, {1} Seconds", m_totalTimeSpan.Minutes.ToString(), m_totalTimeSpan.Seconds.ToString());
			else
				totalTime = String.Format("{0} Seconds", m_totalTimeSpan.Seconds.ToString());

			Console.WriteLine(" ");
			Console.WriteLine("Time for test case " + m_testCaseNumber.ToString() + " - " + totalTime);
			Console.WriteLine("==============================================");
			Console.WriteLine(" ");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Compute the time span.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public string ComputeTimeSpan(DateTime startDateTime)
		{
			TimeSpan span = new TimeSpan(DateTime.Now.Ticks - startDateTime.Ticks);

			int totalHours = m_totalTimeSpan.Hours + span.Hours;
			int totalMins = m_totalTimeSpan.Minutes + span.Minutes;
			int totalSecs = m_totalTimeSpan.Seconds + span.Seconds;
			m_totalTimeSpan = new TimeSpan(totalHours, totalMins, totalSecs);

			string seconds = span.Seconds.ToString();
			if (seconds.Length == 1)
				seconds = "0" + seconds;

			string totalSeconds = m_totalTimeSpan.Seconds.ToString() + " - ";
			if (totalSeconds.Length == 4)
				totalSeconds = "0" + totalSeconds;

			if (span.Minutes > 0)
			{
				if (m_totalTimeSpan.Minutes > 0)
					return (String.Format("{0}:{1} / {2}:{3}", span.Minutes.ToString(), seconds, m_totalTimeSpan.Minutes.ToString(), totalSeconds));
				else
					return (String.Format("{0}:{1} / 0:{2}", span.Minutes.ToString(), seconds, totalSeconds));
			}
			else
			{
				if (m_totalTimeSpan.Minutes > 0)
					return (String.Format("0:{0} / {1}:{2}", seconds, m_totalTimeSpan.Minutes.ToString(), totalSeconds));
				else
					return (String.Format("0:{0} / 0:{1}", seconds, totalSeconds));
			}
		}
		#endregion

		#region // WaitForFileAccess
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// WaitForFileAccess() Overloader
		/// Use a default of 2 sleep seconds and not displaying the IOException message.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void WaitForFileAccess(string filePath)
		{
			WaitForFileAccess(filePath, " ", 2000, false);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// WaitForFileAccess() Overloader
		/// Use a default of not displaying the IOException message.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void WaitForFileAccess(string filePath, int sleepSeconds)
		{
			WaitForFileAccess(filePath, " ", sleepSeconds, false);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Keep looping until file is available for read access.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void WaitForFileAccess(string filePath, string fileName, int sleepSeconds, bool displayMsg)
		{
			string msgIOException;

			if (!File.Exists(filePath))
				return;

			FileStream reader = null;
			while (reader == null)
			{
				try
				{
					reader = File.OpenRead(filePath);
				}
				catch (IOException e)
				{
					if (displayMsg)
						Console.WriteLine("IOException - " + fileName + " is not available for read/write");
					msgIOException = e.Message;
					Thread.Sleep(sleepSeconds);
				}
			}
		}
		#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Write the SetupFW.exe information
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void WriteSetupFwInfo()
		{
			string filePath = @"c:\\SetupFW.msi";
			Console.WriteLine(" ");
			if (File.Exists(filePath))
				Console.WriteLine("C:\\SetupFW.msi 'Modified Date' = " + File.GetLastWriteTime(filePath));
			else
				Console.WriteLine("*** C:\\SetupFW.msi does NOT exist! ***");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Get the test case version, date, and testplan
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void TestCasesVersionDateTestplan()
		{
			string path = "C:\\fw\\Test\\GuiTestDriver\\TeModel\\XmlFromCsv2.xml";

			XmlDocument doc = XmlFiler.getDocument(path, false);
			if (doc == null) return;
			XmlElement docElt = doc.DocumentElement;
			if (docElt == null) return;
			m_xmlTestCases = docElt.SelectNodes("test");
			if (m_xmlTestCases == null) return;

			foreach (XmlNode test in m_xmlTestCases)
			{
				m_testPlan = test.Attributes["Test-Plan"].Value;
				string version = test.Attributes["version"].Value;
				string date = test.Attributes["date"].Value;

				Console.WriteLine(" ");
				Console.WriteLine("Test Cases Plan = " + m_testPlan + " | Date = " + date + " | Version = " + version);
				// 'Return' immediately since the data is only in the first test case record
				return;
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Determine whether WaitForImportToFinish() needs to sleep some additional time.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void CheckForWaitForImportExtraSleep()
		{
			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value == "(none)")
			{
				if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value == "(none)")
				{
					if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value == "(none)")
						m_skipWaitForImportExtraSleep = true;
				}
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Wait for the import processing for the specified 'databaseName' to finish.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void WaitForImportToFinish(string databaseName)
		{
			string sConnection = "Server=.\\SILFW; Database=" + databaseName + "; User ID=FWDeveloper; Password=careful; Pooling=false; Asynchronous Processing=true;";

			SqlConnection sqlConnection = new SqlConnection(sConnection);
			sqlConnection.Open();

			SqlCommand cmd = new SqlCommand("SELECT MAX(id) FROM CmObject", sqlConnection);

			int previousCount = 99;
			int currentCount = 0;

			while (currentCount != previousCount)
			{
				previousCount = currentCount;
				currentCount = (int)cmd.ExecuteScalar();
				Thread.Sleep(3000);
			}

			if (sqlConnection != null)
				sqlConnection.Close();

			if (m_skipWaitForImportExtraSleep)
				return;

			// This is some extra time for clean up and housekeeping
			//HAS to be greater than 4000 -> 7000 seems safe.
			if (Running_On_745())
				Thread.Sleep(7000);
			else
				Thread.Sleep(12000);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Run the Xml Script.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void RunXmlScript(string xmlScriptFileName)
		{
			DateTime startDateTime = DateTime.Now;
			m_rt.fromFile(xmlScriptFileName);
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + xmlScriptFileName);
			Init();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Import Paratext 5 test functions.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void ImportPt5()
		{
			Console.WriteLine("1) Start Import");
			RunXmlScript("Ipt5StartImport.xml");

			Console.WriteLine("2) Import Pt5");
			RunXmlScript("Ipt5ImportAddFiles.xml");
			RunXmlScript("Ipt5ImportMappings.xml");

			Console.WriteLine("3) Import Standard Format");
			RunXmlScript("IptImportStdFmt.xml");

			Console.WriteLine("4) WaitForImportToFinish");
			DateTime startDateTime = DateTime.Now;
			WaitForImportToFinish("MALAY PARATEXT 5 IMPORT TEST");
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForImportToFinish()");

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["BT-Intro"].Value == "err")
			{
				//Handle the "BT-Intro" error
				Console.WriteLine("*** BT-Intro error check");
				RunXmlScript("Ipt5ImportErrChk.xml");
				Console.WriteLine(" ");
				Console.WriteLine("BT-Intro is marked as 'err', and the dialog 'Unable to Import Back Translation'");
				Console.WriteLine("handled this error. No further processing is necessary.");
				return;
			}

			if (ImportPt5BTTestPlan())
				RunXmlScript("Ipt5NoImpBtChk.xml");

			// Export and Compare Scripture

			string exportScriptureFilePath = "C:\\GuiTestResults\\ImportParatext5\\Export Paratext5 Scripture.sf";
			WaitForFileAccess(exportScriptureFilePath);

			Console.WriteLine("5) Export Scripture");
			RunXmlScript("Ipt5ExportScripture.xml");

			Console.WriteLine("     Waiting for Scripture export to finish");
			startDateTime = DateTime.Now;
			//WaitForFileAccess(exportScriptureFilePath, 5000);
			WaitForFileAccess(exportScriptureFilePath);
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

			Console.WriteLine("5b) Compare Scripture");
			RunXmlScript("Ipt5CompareScripture.xml");

			// Export and Compare Back Translation

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value != "(none)")
			{
				//Make sure exportBtFilePath is available
				string exportBtFilePath = "C:\\GuiTestResults\\ImportParatext5\\Export Paratext5 BT.sf";
				WaitForFileAccess(exportBtFilePath);

				Console.WriteLine("6) Export Back Trans");
				RunXmlScript("Ipt5ExportBT.xml");

				Console.WriteLine("     Waiting for Back Trans export to finish");
				startDateTime = DateTime.Now;
				WaitForFileAccess(exportBtFilePath);
				if (m_displayXmlScriptTimeSpan)
					Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

				Console.WriteLine("6b) Compare Back Trans");
				RunXmlScript("Ipt5CompareBackTrans.xml");
			}

			// Export and Compare Notes

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value != "(none)")
			{
				//Make sure exportNotesFilePath is available
				string exportNotesFilePath = "C:\\GuiTestResults\\ImportParatext5\\Export Paratext5 Notes.sf";
				WaitForFileAccess(exportNotesFilePath);

				Console.WriteLine("7) Export Notes");
				RunXmlScript("Ipt5ExportNotes.xml");

				Console.WriteLine("     Waiting for Notes export to finish");
				startDateTime = DateTime.Now;
				WaitForFileAccess(exportNotesFilePath);
				if (m_displayXmlScriptTimeSpan)
					Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

				Console.WriteLine("7b) Compare Notes");
				RunXmlScript("Ipt5CompareNotes.xml");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Import Paratext 6 test functions.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void ImportPt6()
		{
			Console.WriteLine("1) Start Import");
			RunXmlScript("Ipt6StartImport.xml");

			Console.WriteLine("2) Import Pt6");
			RunXmlScript("Ipt6Import.xml");

			Console.WriteLine("3) Import Standard Format");
			RunXmlScript("IptImportStdFmt.xml");

			Console.WriteLine("4) WaitForImportToFinish");
			DateTime startDateTime = DateTime.Now;
			WaitForImportToFinish("MALAY PARATEXT 6 IMPORT TEST");
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForImportToFinish()");

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["BT-Intro"].Value == "err")
			{
				//Handle the "BT-Intro" error
				Console.WriteLine("*** BT-Intro error check");
				RunXmlScript("Ipt6ImportErrChk.xml");
				Console.WriteLine(" ");
				Console.WriteLine("BT-Intro is marked as 'err', and the dialog 'Unable to Import Back Translation'");
				Console.WriteLine("handled this error. No further processing is necessary.");
				return;
			}

			if (ImportPt6BTTestPlan())
				RunXmlScript("Ipt6NoImpBtChk.xml");

			// Export and Compare Scripture

			string exportScriptureFilePath = "C:\\GuiTestResults\\ImportParatext6\\Export Paratext6 Scripture.sf";
			WaitForFileAccess(exportScriptureFilePath);

			Console.WriteLine("5) Export Scripture");
			RunXmlScript("Ipt6ExportScripture.xml");

			Console.WriteLine("     Waiting for Scripture export to finish");
			startDateTime = DateTime.Now;
			WaitForFileAccess(exportScriptureFilePath, 5000);
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

			Console.WriteLine("5b) Compare Scripture");
			RunXmlScript("Ipt6CompareScripture.xml");

			// Export and Compare Back Translation

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value != "(none)")
			{
				//Make sure exportBtFilePath is available
				string exportBtFilePath = "C:\\GuiTestResults\\ImportParatext6\\Export Paratext6 BT.sf";
				WaitForFileAccess(exportBtFilePath);

				Console.WriteLine("6) Export Back Trans");
				RunXmlScript("Ipt6ExportBT.xml");

				Console.WriteLine("     Waiting for Back Trans export to finish");
				startDateTime = DateTime.Now;
				WaitForFileAccess(exportBtFilePath);
				if (m_displayXmlScriptTimeSpan)
					Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

				Console.WriteLine("6b) Compare Back Trans");
				RunXmlScript("Ipt6CompareBackTrans.xml");
			}

			// Export and Compare Notes

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value != "(none)")
			{
				//Make sure exportNotesFilePath is available
				string exportNotesFilePath = "C:\\GuiTestResults\\ImportParatext6\\Export Paratext6 Notes.sf";
				WaitForFileAccess(exportNotesFilePath);

				Console.WriteLine("7) Export Notes");
				RunXmlScript("Ipt6ExportNotes.xml");

				Console.WriteLine("     Waiting for Notes export to finish");
				startDateTime = DateTime.Now;
				WaitForFileAccess(exportNotesFilePath);
				if (m_displayXmlScriptTimeSpan)
					Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

				Console.WriteLine("7b) Compare Notes");
				RunXmlScript("Ipt6CompareNotes.xml");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Create the TmpPictures directory if needed and copy books.gif to the directory.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void CreateTmpPicturesDir()
		{
			string tmpPicturesPath = @"c:\\Tmp\\Pictures";
			string sourceFileName = @"c:\\fw\\Test\GuiTestDriver\\TeTestImport\\ImportOtherSF\\books.gif";
			string destFileName = @"c:\\Tmp\\Pictures\\books.gif";

			if (!Directory.Exists(tmpPicturesPath))
				Directory.CreateDirectory(tmpPicturesPath);
			else
				// Remove the ReadOnly attribute
				File.SetAttributes(destFileName, FileAttributes.Normal);

			File.Copy(sourceFileName, destFileName, true);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Import ImportOtherSF test functions.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void ImportOtherSFTest()
		{
			//m_displayXmlScriptTimeSpan = false;

			Console.WriteLine("0) Create Pictures Directory");
			DateTime startDateTime = DateTime.Now;
			CreateTmpPicturesDir();
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "CreateTmpPicturesDir()");

			Console.WriteLine("1) Start Import");
			RunXmlScript("OsfStartImport.xml");

			Console.WriteLine("2) Import OtherSF");
			RunXmlScript("OsfImportAddFiles.xml");
			RunXmlScript("OsfImportMappings.xml");

			Console.WriteLine("3) Import Standard Format");
			RunXmlScript("IptImportStdFmt.xml");

			Console.WriteLine("4) WaitForImportToFinish");
			startDateTime = DateTime.Now;
			WaitForImportToFinish("MALVI OTHERSF IMPORT TEST");
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForImportToFinish()");

			//if (ImportPt5BTTestPlan())
			//    RunXmlScript("Ipt5NoImpBtChk.xml");

			// Export and Compare Scripture

			string exportScriptureFilePath = "C:\\GuiTestResults\\ImportOtherSF\\Export OtherSF Scripture.sf";
			WaitForFileAccess(exportScriptureFilePath);

			Console.WriteLine("5) Export Scripture");
			RunXmlScript("OsfExportScripture.xml");

			Console.WriteLine("     Waiting for Scripture export to finish");
			startDateTime = DateTime.Now;
			//WaitForFileAccess(exportScriptureFilePath, 5000);
			WaitForFileAccess(exportScriptureFilePath);
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

			Console.WriteLine("5b) Compare Scripture");
			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value != "(none)")
				RunXmlScript("OsfCompareScripture.xml");

			// Export and Compare Back Translation

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value != "(none)")
			{
				//Make sure exportBtFilePath is available
				string exportBtFilePath = "C:\\GuiTestResults\\ImportOtherSF\\Export OtherSF BT.sf";
				WaitForFileAccess(exportBtFilePath);

				Console.WriteLine("6) Export Back Trans");
				RunXmlScript("OsfExportBackTrans.xml");

				Console.WriteLine("     Waiting for Back Trans export to finish");
				startDateTime = DateTime.Now;
				WaitForFileAccess(exportBtFilePath);
				if (m_displayXmlScriptTimeSpan)
					Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

				Console.WriteLine("6b) Compare Back Trans");
				RunXmlScript("OsfCompareBackTrans.xml");
			}

			// Export and Compare Notes

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value != "(none)")
			{
				//Make sure exportNotesFilePath is available
				string exportNotesFilePath = "C:\\GuiTestResults\\ImportOtherSF\\Export OtherSF Notes.sf";
				WaitForFileAccess(exportNotesFilePath);

				Console.WriteLine("7) Export Notes");
				RunXmlScript("OsfExportNotes.xml");

				Console.WriteLine("     Waiting for Notes export to finish");
				startDateTime = DateTime.Now;
				WaitForFileAccess(exportNotesFilePath);
				if (m_displayXmlScriptTimeSpan)
					Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

				Console.WriteLine("7b) Compare Notes");
				RunXmlScript("OsfCompareNotes.xml");
			}
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Perform the common test functions.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void CommonTestFunctions()
		{
			//m_displayXmlScriptTimeSpan = false;
			WriteSetupFwInfo();
			TestCasesVersionDateTestplan();
			CheckForWaitForImportExtraSleep();
			Console.WriteLine(" ");
			Console.WriteLine("Test Case: " + m_testCaseNumber.ToString());
			Console.WriteLine(" ");

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["run-test"].Value != "N")
			{
				if (ImportPt5TestPlan() || ImportPt5BTTestPlan())
					ImportPt5();
				else if (ImportPt6TestPlan() || ImportPt6BTTestPlan())
					ImportPt6();
				else if (ImportOtherSFTestPlan() || ImportOtherSFSepBTTestPlan())
					ImportOtherSFTest();
				Console.WriteLine("     Success!");
			}
			else
				Console.WriteLine("User Specified To NOT Run Test.  'Run Test' => 'N'");

			ComputeTestCaseTimeSpan();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Create and setup the empty base test project.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void CreateParatextProject()
		{
			//m_displayXmlScriptTimeSpan = false;
			WriteSetupFwInfo();
			TestCasesVersionDateTestplan();
			Console.WriteLine(" ");
			Console.WriteLine("1) Creating Malay Paratext Project");
			if (ImportPt5TestPlan() || ImportPt5BTTestPlan())
				RunXmlScript("Ipt5CreateProject.xml");
			else if (ImportPt6TestPlan() || ImportPt6BTTestPlan())
				RunXmlScript("Ipt6CreateProject.xml");

			Console.WriteLine(" ");
			Console.WriteLine(" ");

			Console.WriteLine("  Success!");
			ComputeTestCaseTimeSpan();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Create and setup the empty base test project.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void CreateOtherSFProject()
		{
			//m_displayXmlScriptTimeSpan = false;
			WriteSetupFwInfo();
			TestCasesVersionDateTestplan();
			Console.WriteLine(" ");
			Console.WriteLine("1) Creating Malvi Other SF Base Project");
			RunXmlScript("OsfCreateProject.xml");

			if (ImportOtherSFTestPlan())
			{
				Console.WriteLine("2) Adding a 2nd Analysis Writing System");
				RunXmlScript("OsfAddAnalysisWritingSys.xml");
			}

			Console.WriteLine(" ");
			Console.WriteLine(" ");

			Console.WriteLine("  Success!");
			ComputeTestCaseTimeSpan();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Roundtrip Export Import Compare.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void RoundtripExportImportCompare()
		{
			//m_displayXmlScriptTimeSpan = false;
			WriteSetupFwInfo();
			TestCasesVersionDateTestplan();
			Console.WriteLine(" ");
			Console.WriteLine("Test Case: RoundtripExportImportCompare");
			Console.WriteLine(" ");

			Console.WriteLine("1) Start Backup Restore");
			RunXmlScript("Ipt6StartSena3.xml");

			Console.WriteLine("2) Export Xml");
			RunXmlScript("RndTripExportXml.xml");

			Console.WriteLine("3) Import Xml");
			RunXmlScript("RndTripImportXml.xml");

			m_skipWaitForImportExtraSleep = true;
			Console.WriteLine("4) WaitForImportToFinish");
			DateTime startDateTime = DateTime.Now;
			WaitForImportToFinish("Sena 3");
			if (m_displayXmlScriptTimeSpan)
				Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForImportToFinish()");

			Console.WriteLine("5) Import Xml");
			RunXmlScript("RndTripBooksCompare.xml");

			Console.WriteLine(" ");
			Console.WriteLine("  Success!");
			ComputeTestCaseTimeSpan();
		}

	#region // Restore Tests
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Create and setup the empty base test project for UI Restore testing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void CreateParatextRestoreProject()
		{
			//m_displayXmlScriptTimeSpan = false;
			WriteSetupFwInfo();
			TestCasesVersionDateTestplan();
			Console.WriteLine(" ");
			Console.WriteLine("Test Case: CreateParatextRestoreProject");
			Console.WriteLine(" ");

			Console.WriteLine("1) Start Backup Restore");
			RunXmlScript("Ipt6StartSena3.xml");

			Console.WriteLine(" ");
			Console.WriteLine("2) Create Restore Project");
			RunXmlScript("Ipt6CreateRestoreProject.xml");

			Console.WriteLine(" ");
			Console.WriteLine("  Success!");
			ComputeTestCaseTimeSpan();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Restore Options Replace Version.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void PT6RestoreOptionsReplaceVersion()
		{
			//m_displayXmlScriptTimeSpan = false;
			WriteSetupFwInfo();
			TestCasesVersionDateTestplan();
			Console.WriteLine(" ");
			Console.WriteLine("Test Case: PT6RestoreOptionsReplaceVersion");
			Console.WriteLine(" ");

			Console.WriteLine("1) Start Backup Restore");
			RunXmlScript("Ipt6StartBackupRestore.xml");

			Console.WriteLine(" ");
			Console.WriteLine("3) Insert Scripture");
			RunXmlScript("Ipt6InsertScripture.xml");

			Console.WriteLine(" ");
			Console.WriteLine("4) Backup Project");
			RunXmlScript("Ipt6BackupProject.xml");

		// Create a reference to the directory.
			//DirectoryInfo di = new DirectoryInfo(@"C:\GuiTestResults\BackupRestoreUsingUI");
		// Create an array representing the files in the current directory.
			//FileInfo[] fi = di.GetFiles();
			//File.Copy("C:\\GuiTestResults\\BackupRestoreUsingUI\\" + fi[0].Name, "C:\\GuiTestResults\\BackupRestoreUsingUI\\MALAY PARATEXT 6 IMPORT TEST.zip", true);

			//Console.WriteLine(" ");
			//Console.WriteLine("4) Compare Backup");
			//RunXmlScript("Ipt6CompareBackup.xml");

			Console.WriteLine(" ");
			Console.WriteLine("5) Delete & Restore Project");
			RunXmlScript("RestoreOptionsReplaceVersion.xml");

			//Console.WriteLine(" ");
			//Console.WriteLine("6) View Inserted Scripture");
			//RunXmlScript("Ipt6InsertScriptureView.xml");

			Console.WriteLine(" ");
			Console.WriteLine("     Success!");
			ComputeTestCaseTimeSpan();
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Restore Options Seperate Database.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void PT6RestoreOptionsSeperateDatabase()
		{
			//m_displayXmlScriptTimeSpan = false;
			WriteSetupFwInfo();
			TestCasesVersionDateTestplan();
			Console.WriteLine(" ");
			Console.WriteLine("Test Case: PT6RestoreOptionsSeperateDatabase");
			Console.WriteLine(" ");

			Console.WriteLine("1) Start Backup Restore");
			RunXmlScript("Ipt6StartBackupRestore.xml");

			Console.WriteLine(" ");
			Console.WriteLine("3) Insert Scripture");
			RunXmlScript("Ipt6InsertScripture.xml");

			Console.WriteLine(" ");
			Console.WriteLine("4) Backup Project");
			RunXmlScript("Ipt6BackupProject.xml");

			Console.WriteLine(" ");
			Console.WriteLine("5) Delete & Restore Project");
			RunXmlScript("RestoreOptionsSeperateDB.xml");

			//Console.WriteLine(" ");
			//Console.WriteLine("6) View Inserted Scripture");
			//RunXmlScript("Ipt6InsertScriptureView.xml");

			Console.WriteLine(" ");
			Console.WriteLine("     Success!");
			ComputeTestCaseTimeSpan();
		}
	#endregion

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// This function is only for testing.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void ForTesting()
		{
			// Put test code here
			m_rt.fromFile("TestViewAccess.xml");
		}

	#region // Test Cases
		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Test Cases
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Category("Testing")]
		public void Testing()
		{
			ForTesting();
		}

		[Test]
		[Category("Roundtrip Export Import Compare")]
		public void RndTripExportImportCompare()
		{
			m_testCaseNumber = 0;
			RoundtripExportImportCompare();
		}


		[Test]
		[Category("Restore Options Replace Version")]
		public void RestoreOptReplaceVer()
		{
			m_testCaseNumber = 0;
			PT6RestoreOptionsReplaceVersion();
		}

		[Test]
		[Category("Restore Options Seperate Database")]
		public void RestoreOptSeperateDatabase()
		{
			m_testCaseNumber = 0;
			PT6RestoreOptionsSeperateDatabase();
		}

		[Test]
		[Category("Create PT Project")]
		public void CreatePtProject()
		{
			m_testCaseNumber = 0;
			CreateParatextProject();
		}

		[Test]
		[Category("Create OtherSF Project")]
		public void CreateOSFProject()
		{
			m_testCaseNumber = 0;
			CreateOtherSFProject();
		}

		[Test]
		[Category("Create Pt Restore Project")]
		public void CreatePtRestoreProject()
		{
			m_testCaseNumber = 0;
			CreateParatextRestoreProject();
		}

		[Test]
		[Category("Test Case 1")]
		public void ImportCase01()
		{
			m_testCaseNumber = 1;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 2")]
		public void ImportCase02()
		{
			m_testCaseNumber = 2;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 3")]
		public void ImportCase03()
		{
			m_testCaseNumber = 3;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 4")]
		public void ImportCase04()
		{
			m_testCaseNumber = 4;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 5")]
		public void ImportCase05()
		{
			m_testCaseNumber = 5;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 6")]
		public void ImportCase06()
		{
			m_testCaseNumber = 6;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 7")]
		public void ImportCase07()
		{
			m_testCaseNumber = 7;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 8")]
		public void ImportCase08()
		{
			m_testCaseNumber = 8;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 9")]
		public void ImportCase09()
		{
			m_testCaseNumber = 9;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 10")]
		public void ImportCase10()
		{
			m_testCaseNumber = 10;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 11")]
		public void ImportCase11()
		{
			m_testCaseNumber = 11;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 12")]
		public void ImportCase12()
		{
			m_testCaseNumber = 12;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 13")]
		public void ImportCase13()
		{
			m_testCaseNumber = 13;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 14")]
		public void ImportCase14()
		{
			m_testCaseNumber = 14;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 15")]
		public void ImportCase15()
		{
			m_testCaseNumber = 15;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 16")]
		public void ImportCase16()
		{
			m_testCaseNumber = 16;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 17")]
		public void ImportCase17()
		{
			m_testCaseNumber = 17;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 18")]
		public void ImportCase18()
		{
			m_testCaseNumber = 18;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 19")]
		public void ImportCase19()
		{
			m_testCaseNumber = 19;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 20")]
		public void ImportCase20()
		{
			m_testCaseNumber = 20;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 21")]
		public void ImportCase21()
		{
			m_testCaseNumber = 21;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 22")]
		public void ImportCase22()
		{
			m_testCaseNumber = 22;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 23")]
		public void ImportCase23()
		{
			m_testCaseNumber = 23;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 24")]
		public void ImportCase24()
		{
			m_testCaseNumber = 24;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 25")]
		public void ImportCase25()
		{
			m_testCaseNumber = 25;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 26")]
		public void ImportCase26()
		{
			m_testCaseNumber = 26;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 27")]
		public void ImportCase27()
		{
			m_testCaseNumber = 27;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 28")]
		public void ImportCase28()
		{
			m_testCaseNumber = 28;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 29")]
		public void ImportCase29()
		{
			m_testCaseNumber = 29;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 30")]
		public void ImportCase30()
		{
			m_testCaseNumber = 30;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 31")]
		public void ImportCase31()
		{
			m_testCaseNumber = 31;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 32")]
		public void ImportCase32()
		{
			m_testCaseNumber = 32;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 33")]
		public void ImportCase33()
		{
			m_testCaseNumber = 33;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 34")]
		public void ImportCase34()
		{
			m_testCaseNumber = 34;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 35")]
		public void ImportCase35()
		{
			m_testCaseNumber = 35;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 36")]
		public void ImportCase36()
		{
			m_testCaseNumber = 36;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 37")]
		public void ImportCase37()
		{
			m_testCaseNumber = 37;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 38")]
		public void ImportCase38()
		{
			m_testCaseNumber = 38;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 39")]
		public void ImportCase39()
		{
			m_testCaseNumber = 39;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 40")]
		public void ImportCase40()
		{
			m_testCaseNumber = 40;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 41")]
		public void ImportCase41()
		{
			m_testCaseNumber = 41;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 42")]
		public void ImportCase42()
		{
			m_testCaseNumber = 42;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 43")]
		public void ImportCase43()
		{
			m_testCaseNumber = 43;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 44")]
		public void ImportCase44()
		{
			m_testCaseNumber = 44;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 45")]
		public void ImportCase45()
		{
			m_testCaseNumber = 45;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 46")]
		public void ImportCase46()
		{
			m_testCaseNumber = 46;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 47")]
		public void ImportCase47()
		{
			m_testCaseNumber = 47;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 48")]
		public void ImportCase48()
		{
			m_testCaseNumber = 48;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 49")]
		public void ImportCase49()
		{
			m_testCaseNumber = 49;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 50")]
		public void ImportCase50()
		{
			m_testCaseNumber = 50;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 51")]
		public void ImportCase51()
		{
			m_testCaseNumber = 51;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 52")]
		public void ImportCase52()
		{
			m_testCaseNumber = 52;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 53")]
		public void ImportCase53()
		{
			m_testCaseNumber = 53;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 54")]
		public void ImportCase54()
		{
			m_testCaseNumber = 54;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 55")]
		public void ImportCase55()
		{
			m_testCaseNumber = 55;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 56")]
		public void ImportCase56()
		{
			m_testCaseNumber = 56;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 57")]
		public void ImportCase57()
		{
			m_testCaseNumber = 57;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 58")]
		public void ImportCase58()
		{
			m_testCaseNumber = 58;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 59")]
		public void ImportCase59()
		{
			m_testCaseNumber = 59;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 60")]
		public void ImportCase60()
		{
			m_testCaseNumber = 60;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 61")]
		public void ImportCase61()
		{
			m_testCaseNumber = 61;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 62")]
		public void ImportCase62()
		{
			m_testCaseNumber = 62;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 63")]
		public void ImportCase63()
		{
			m_testCaseNumber = 63;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 64")]
		public void ImportCase64()
		{
			m_testCaseNumber = 64;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 65")]
		public void ImportCase65()
		{
			m_testCaseNumber = 65;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 66")]
		public void ImportCase66()
		{
			m_testCaseNumber = 66;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 67")]
		public void ImportCase67()
		{
			m_testCaseNumber = 67;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 68")]
		public void ImportCase68()
		{
			m_testCaseNumber = 68;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 69")]
		public void ImportCase69()
		{
			m_testCaseNumber = 69;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 70")]
		public void ImportCase70()
		{
			m_testCaseNumber = 70;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 71")]
		public void ImportCase71()
		{
			m_testCaseNumber = 71;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 72")]
		public void ImportCase72()
		{
			m_testCaseNumber = 72;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 73")]
		public void ImportCase73()
		{
			m_testCaseNumber = 73;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 74")]
		public void ImportCase74()
		{
			m_testCaseNumber = 74;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 75")]
		public void ImportCase75()
		{
			m_testCaseNumber = 75;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 76")]
		public void ImportCase76()
		{
			m_testCaseNumber = 76;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 77")]
		public void ImportCase77()
		{
			m_testCaseNumber = 77;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 78")]
		public void ImportCase78()
		{
			m_testCaseNumber = 78;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 79")]
		public void ImportCase79()
		{
			m_testCaseNumber = 79;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 80")]
		public void ImportCase80()
		{
			m_testCaseNumber = 80;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 81")]
		public void ImportCase81()
		{
			m_testCaseNumber = 81;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 82")]
		public void ImportCase82()
		{
			m_testCaseNumber = 82;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 83")]
		public void ImportCase83()
		{
			m_testCaseNumber = 83;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test Case 84")]
		public void ImportCase84()
		{
			m_testCaseNumber = 84;
			CommonTestFunctions();
		}
	}
	#endregion
}
