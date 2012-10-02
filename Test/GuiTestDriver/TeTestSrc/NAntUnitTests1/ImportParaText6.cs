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
		 try { new RunTest("TE").fromFile("IncShutDown.xml"); }
		 catch { }
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
	   if (m_totalTimeSpan.Minutes == 1)
		  totalTime = String.Format("{0} Minute, {1} Seconds", m_totalTimeSpan.Minutes.ToString(), m_totalTimeSpan.Seconds.ToString());
	   else if (m_totalTimeSpan.Minutes > 0)
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
		/// Log the compare files for the python program to use.
		/// </summary>
		///--------------------------------------------------------------------------------------
	  public void WriteCompareFilesLogFile(string mrfPathName, string testCaseResultFile, string exportScriptureFilePath)
	  {
		 string filesCompString = "\"" + mrfPathName + "\"";
		 string exportScriptureFilePathString = "\"" + exportScriptureFilePath + "\"";
		 string compareFiles = "C:\\GuiTestResults\\CompareFiles.txt";
		 if (File.Exists(compareFiles))
			File.Delete(compareFiles);

		 StreamWriter aStreamWriter;
		 aStreamWriter = File.CreateText(compareFiles);
		 aStreamWriter.WriteLine(filesCompString);
		 aStreamWriter.WriteLine(testCaseResultFile);
		 aStreamWriter.WriteLine(exportScriptureFilePathString);
		 aStreamWriter.Close();
	  }

	  ///--------------------------------------------------------------------------------------
	  /// <summary>
	  ///
	  /// </summary>
	  ///--------------------------------------------------------------------------------------
	  public void WriteTimingLogFile(string description, string logFileName, DateTime startDateTime)
	  {
		 string logFile = "C:\\fw\\Test\\GuiTestDriver\\TeScripts\\" + logFileName;
		 if (File.Exists(logFile))
			File.Delete(logFile);

		 TimeSpan span = new TimeSpan(DateTime.Now.Ticks - startDateTime.Ticks);
		 int milliSeconds = (int)span.TotalSeconds * 1000;

		 string writeString = "<gtdLog><set-up date=\"" + DateTime.Now.ToShortDateString() + "\"/><result tag=\"monitor-time\" desc=\"" + description + "\" ellapsed-time=\"" + milliSeconds.ToString() + "\" expect=\"" + "TestCase=" + m_testCaseNumber.ToString() + "\"/></gtdLog>";

		 StreamWriter aStreamWriter;
		 aStreamWriter = File.CreateText(logFile);
		 aStreamWriter.WriteLine(writeString);
		 aStreamWriter.Close();
	  }

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Import Paratext 5 test functions.
		/// </summary>
		///--------------------------------------------------------------------------------------
		public void ImportPt5()
	 {
	  Console.WriteLine("1) Start Import");
	  DateTime startDateTime = DateTime.Now;
			RunXmlScript("Ipt5StartImport.xml");

			Console.WriteLine("2) Import Pt5");
			RunXmlScript("Ipt5ImportAddFiles.xml");
			RunXmlScript("Ipt5ImportMappings.xml");

			Console.WriteLine("3) Import Standard Format");
			RunXmlScript("IptImportStdFmt.xml");

			Console.WriteLine("4) WaitForImportToFinish");
	   DateTime fileIoStartDateTime = DateTime.Now;
			WaitForImportToFinish("MALAY PARATEXT 5 IMPORT TEST");
			if (m_displayXmlScriptTimeSpan)
		  Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForImportToFinish()");
	   WriteTimingLogFile("Malay PT5 Import Test db, Import", "aImportScr.xlg", startDateTime);

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

	   // Test to see if BT is being imported but Scripture isn't being imported
	   string dataTypes = m_xmlTestCases[m_testCaseNumber - 1].Attributes["data-types"].Value;
	   if ((dataTypes.IndexOf("Scripture") == -1) && (dataTypes.IndexOf("BT") > 0))
	   {
		  if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["back-trans"].Value != "(none)")
		  {
			 //Handle the "No corresponding vernacular book for back translation" error
			 Console.WriteLine("*** Import BT but No Scripture");
			 RunXmlScript("Ipt5ImportErrChk.xml");
			 Console.WriteLine(" ");
			 Console.WriteLine("The dialog 'Unable to Import Back Translation' with the message ");
			 Console.WriteLine("'No corresponding vernacular book for back translation.' ");
			 Console.WriteLine("handled this condition. BT was being imported but no Scripture. ");
			 Console.WriteLine("No further processing is necessary.");
			 return;
		  }
	   }
	   else
		  // There is an Error if BT-Intro is NOT marked as 'err' and the dialog 'Unable to Import Back
		  // Translation' appears at this point
		  RunXmlScript("Ipt5NoImpBtChk.xml");

			// Export and Compare Scripture

	   Console.WriteLine("5) Export Scripture");
	   startDateTime = DateTime.Now;
			RunXmlScript("Ipt5ExportScripture.xml");

			Console.WriteLine("     Waiting for Scripture export to finish");
	  fileIoStartDateTime = DateTime.Now;

	  string exportScriptureFilePath = string.Empty;
	  if (ImportPt5TestPlan())
		 exportScriptureFilePath = "C:\\GuiTestResults\\ImportParatext5\\Test Case " + m_testCaseNumber.ToString() +  " Scripture.sf";
	  else if (ImportPt5BTTestPlan())
		 exportScriptureFilePath = "C:\\GuiTestResults\\ImportParatext5BT\\Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";

			WaitForFileAccess(exportScriptureFilePath);
			if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
	   WriteTimingLogFile("Malay PT5 Import Test db, Export Scripture", "aExportScr.xlg", startDateTime);

	  // Create a compare file
	  string mrfScr = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value;
	  string mrfScrPathName = "C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportParatext5\\" + mrfScr;
	  string scrTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";
	  WriteCompareFilesLogFile(mrfScrPathName, scrTestCaseResultFile, exportScriptureFilePath);

	   Console.WriteLine("5b) Compare Scripture");
	   RunXmlScript("Ipt5CompareScripture.xml");

			// Export and Compare Back Translation

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value != "(none)")
			{
		 Console.WriteLine("6) Export Back Trans");
		 startDateTime = DateTime.Now;
				RunXmlScript("Ipt5ExportBT.xml");

				Console.WriteLine("     Waiting for Back Trans export to finish");
		 fileIoStartDateTime = DateTime.Now;

		 string exportBtFilePath = string.Empty;
		 if (ImportPt5TestPlan())
			exportBtFilePath = "C:\\GuiTestResults\\ImportParatext5\\Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";
		 else if (ImportPt5BTTestPlan())
			exportBtFilePath = "C:\\GuiTestResults\\ImportParatext5BT\\Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";

				WaitForFileAccess(exportBtFilePath);
				if (m_displayXmlScriptTimeSpan)
			   Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
		 WriteTimingLogFile("Malay PT5 Import Test db, Export BT", "aExportBT.xlg", startDateTime);

		 // Create a compare file
		 string mrfBt = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value;
		 string mrfBtPathName =
		   "C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportParatext5\\" + mrfBt;
		 string btTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";
		 WriteCompareFilesLogFile(mrfBtPathName, btTestCaseResultFile, exportBtFilePath);

		 Console.WriteLine("6b) Compare Back Trans");
		 RunXmlScript("Ipt5CompareBackTrans.xml");
			}

			// Export and Compare Notes

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value != "(none)")
			{
		 Console.WriteLine("7) Export Notes");
		 startDateTime = DateTime.Now;
				RunXmlScript("Ipt5ExportNotes.xml");

				Console.WriteLine("     Waiting for Notes export to finish");
		 fileIoStartDateTime = DateTime.Now;

		 string exportNotesFilePath = string.Empty;
		 if (ImportPt5TestPlan())
			exportNotesFilePath = "C:\\GuiTestResults\\ImportParatext5\\Test Case " + m_testCaseNumber.ToString() + " Notes.sf";
		 else if (ImportPt5BTTestPlan())
			exportNotesFilePath = "C:\\GuiTestResults\\ImportParatext5BT\\Test Case " + m_testCaseNumber.ToString() + " Notes.sf";

				WaitForFileAccess(exportNotesFilePath);
				if (m_displayXmlScriptTimeSpan)
			   Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
		 WriteTimingLogFile("Malay PT5 Import Test db, Export Notes", "aExportNotes.xlg", startDateTime);

		 // Create a compare file
		 string mrfNotes = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value;
		 string mrfNotesPathName =
			"C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportParatext5\\" + mrfNotes;
		 string notesTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " Notes.sf";
		 WriteCompareFilesLogFile(mrfNotesPathName, notesTestCaseResultFile, exportNotesFilePath);

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
	   DateTime startDateTime = DateTime.Now;
			RunXmlScript("Ipt6StartImport.xml");

			Console.WriteLine("2) Import Pt6");
			RunXmlScript("Ipt6Import.xml");

			Console.WriteLine("3) Import Standard Format");
			RunXmlScript("IptImportStdFmt.xml");

			Console.WriteLine("4) WaitForImportToFinish");
	   DateTime fileIoStartDateTime = DateTime.Now;
			WaitForImportToFinish("MALAY PARATEXT 6 IMPORT TEST");
			if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForImportToFinish()");
	   WriteTimingLogFile("Malay PT6 Import Test db, Import", "aImportScr.xlg", startDateTime);

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

	   // Test to see if BT is being imported but Scripture isn't being imported
	   string dataTypes = m_xmlTestCases[m_testCaseNumber - 1].Attributes["data-types"].Value;
	   if ((dataTypes.IndexOf("Scripture") == -1) && (dataTypes.IndexOf("BT") > 0))
	   {
		  if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["back-trans"].Value != "(none)")
		  {
			 //Handle the "No corresponding vernacular book for back translation" error
			 Console.WriteLine("*** Import BT but No Scripture");
			 RunXmlScript("Ipt6ImportErrChk.xml");
			 Console.WriteLine(" ");
			 Console.WriteLine("The dialog 'Unable to Import Back Translation' with the message ");
			 Console.WriteLine("'No corresponding vernacular book for back translation.' ");
			 Console.WriteLine("handled this condition. BT was being imported but no Scripture. ");
			 Console.WriteLine("No further processing is necessary.");
			 return;
		  }
	   }
	   else
		  RunXmlScript("Ipt6NoImpBtChk.xml");

			// Export and Compare Scripture

			Console.WriteLine("5) Export Scripture");
	   startDateTime = DateTime.Now;
			RunXmlScript("Ipt6ExportScripture.xml");

			Console.WriteLine("     Waiting for Scripture export to finish");
	   fileIoStartDateTime = DateTime.Now;

	   string exportScriptureFilePath = string.Empty;
	   if (ImportPt6TestPlan())
		  exportScriptureFilePath = "C:\\GuiTestResults\\ImportParatext6\\Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";
	   else if (ImportPt6BTTestPlan())
		  exportScriptureFilePath = "C:\\GuiTestResults\\ImportParatext6BT\\Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";

			WaitForFileAccess(exportScriptureFilePath);
			if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
	   WriteTimingLogFile("Malay PT6 Import Test db, Export Scripture", "aExportScr.xlg", startDateTime);

	  // Create a compare file
	  string mrfScr = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value;
	  string mrfScrPathName = "C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportParatext6\\" + mrfScr;
	  string scrTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";
	  WriteCompareFilesLogFile(mrfScrPathName, scrTestCaseResultFile, exportScriptureFilePath);

	  Console.WriteLine("5b) Compare Scripture");
	  RunXmlScript("Ipt6CompareScripture.xml");

			// Export and Compare Back Translation

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value != "(none)")
			{
		 Console.WriteLine("6) Export Back Trans");
		 startDateTime = DateTime.Now;
				RunXmlScript("Ipt6ExportBT.xml");

				Console.WriteLine("     Waiting for Back Trans export to finish");
		 fileIoStartDateTime = DateTime.Now;

		 string exportBtFilePath = string.Empty;
		 if (ImportPt6TestPlan())
			exportBtFilePath = "C:\\GuiTestResults\\ImportParatext6\\Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";
		 else if (ImportPt6BTTestPlan())
			exportBtFilePath = "C:\\GuiTestResults\\ImportParatext6BT\\Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";

				WaitForFileAccess(exportBtFilePath);
				if (m_displayXmlScriptTimeSpan)
			   Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
		 WriteTimingLogFile("Malay PT6 Import Test db, Export BT", "aExportBT.xlg", startDateTime);

		 // Create a compare file
		 string mrfBt = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value;
		 string mrfBtPathName =
			"C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportParatext6\\" + mrfBt;
		 string btTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";
		 WriteCompareFilesLogFile(mrfBtPathName, btTestCaseResultFile, exportBtFilePath);

		 Console.WriteLine("6b) Compare Back Trans");
		 RunXmlScript("Ipt6CompareBackTrans.xml");
			}

			// Export and Compare Notes

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value != "(none)")
			{
		 Console.WriteLine("7) Export Notes");
		 startDateTime = DateTime.Now;
				RunXmlScript("Ipt6ExportNotes.xml");

				Console.WriteLine("     Waiting for Notes export to finish");
		 fileIoStartDateTime = DateTime.Now;

		 string exportNotesFilePath = string.Empty;
		 if (ImportPt6TestPlan())
			exportNotesFilePath = "C:\\GuiTestResults\\ImportParatext6\\Test Case " + m_testCaseNumber.ToString() + " Notes.sf";
		 else if (ImportPt6BTTestPlan())
			exportNotesFilePath = "C:\\GuiTestResults\\ImportParatext6BT\\Test Case " + m_testCaseNumber.ToString() + " Notes.sf";

				WaitForFileAccess(exportNotesFilePath);
				if (m_displayXmlScriptTimeSpan)
			   Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
		 WriteTimingLogFile("Malay PT6 Import Test db, Export Notes", "aExportNotes.xlg", startDateTime);

		 // Create a compare file
		 string mrfNotes = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value;
		 string mrfNotesPathName =
			"C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportParatext6\\" + mrfNotes;
		 string notesTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " Notes.sf";
		 WriteCompareFilesLogFile(mrfNotesPathName, notesTestCaseResultFile, exportNotesFilePath);

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
	  DateTime fileIoStartDateTime = DateTime.Now;
	  WaitForImportToFinish("MALVI OTHERSF IMPORT TEST");
			if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForImportToFinish()");
	   WriteTimingLogFile("Malay OtherSF Import Test db, Import", "aImportScr.xlg", startDateTime);

		 //    RunXmlScript("Ipt5NoImpBtChk.xml");

	   if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["BT-Intro"].Value == "err")
	   {
		  //Handle the "BT-Intro" error
		  Console.WriteLine("*** BT-Intro error check");
		  RunXmlScript("OsfImportErrChk.xml");
		  Console.WriteLine(" ");
		  Console.WriteLine("BT-Intro is marked as 'err', and the dialog 'Unable to Import Back Translation'");
		  Console.WriteLine("handled this error. No further processing is necessary.");
		  return;
	   }

			// Export and Compare Scripture

	   Console.WriteLine("5) Export Scripture");
	   startDateTime = DateTime.Now;
	   if (ImportOtherSFTestPlan())
		  RunXmlScript("OsfExportScripture.xml");
	   else if (ImportOtherSFSepBTTestPlan())
		  RunXmlScript("OsfSepBtExportScripture.xml");

	   Console.WriteLine("     Waiting for Scripture export to finish");
	   fileIoStartDateTime = DateTime.Now;
	   string exportScriptureFilePath = string.Empty;
	   if (ImportOtherSFTestPlan())
		  exportScriptureFilePath = "C:\\GuiTestResults\\ImportOtherSF\\Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";
	   else if (ImportOtherSFSepBTTestPlan())
		  exportScriptureFilePath = "C:\\GuiTestResults\\ImportOtherSF_SepBT\\Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";

			WaitForFileAccess(exportScriptureFilePath);
			if (m_displayXmlScriptTimeSpan)
		  Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
	   WriteTimingLogFile("Malay OtherSF Import Test db, Export Scripture", "aExportScr.xlg", startDateTime);

	   // Create a compare file
	   //string mrfScr = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value;
	   //string mrfScrPathName = "C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportOtherSF\\" + mrfScr;
	   //string scrTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";
	   //WriteCompareFilesLogFile(mrfScrPathName, scrTestCaseResultFile, exportScriptureFilePath);

	   //if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value != "(none)")
	   //{
	   //   Console.WriteLine("5b) Compare Scripture");
	   //   if (ImportOtherSFTestPlan())
	   //      RunXmlScript("OsfCompareScripture.xml");
	   //   else if (ImportOtherSFSepBTTestPlan())
	   //      RunXmlScript("OsfSepBtCompareScripture.xml");
	   //}

			// Export and Compare Back Translation

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value != "(none)")
	   {
		 Console.WriteLine("6) Export Back Trans");
		 startDateTime = DateTime.Now;
		 if (ImportOtherSFTestPlan())
			RunXmlScript("OsfExportBackTrans.xml");
		 else if (ImportOtherSFSepBTTestPlan())
			RunXmlScript("OsfSepBtExportBackTrans.xml");

		 Console.WriteLine("     Waiting for Back Trans export to finish");
		 fileIoStartDateTime = DateTime.Now;
		 string exportBtFilePath = string.Empty;
		 if (ImportOtherSFTestPlan())
			exportBtFilePath = "C:\\GuiTestResults\\ImportOtherSF\\Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";
		 else if (ImportOtherSFSepBTTestPlan())
			exportBtFilePath = "C:\\GuiTestResults\\ImportOtherSF_SepBT\\Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";

				WaitForFileAccess(exportBtFilePath);
				if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
		 WriteTimingLogFile("Malay OtherSF Import Test db, Export BT", "aExportBT.xlg", startDateTime);

		 // Create a compare file
		 string mrfBt = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-bt"].Value;
		 string mrfBtPathName =
			"C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportOtherSF\\" + mrfBt;
		 string btTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " BackTrans.sf";
		 WriteCompareFilesLogFile(mrfBtPathName, btTestCaseResultFile, exportBtFilePath);

		 Console.WriteLine("6b) Compare Back Trans");
		 if (ImportOtherSFTestPlan())
			RunXmlScript("OsfCompareBackTrans.xml");
		 else if (ImportOtherSFSepBTTestPlan())
			RunXmlScript("OsfSepBtCompareBackTrans.xml");
			}

			// Export and Compare Notes

			if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value != "(none)")
	   {
		 Console.WriteLine("7) Export Notes");
		 startDateTime = DateTime.Now;
		 if (ImportOtherSFTestPlan())
			RunXmlScript("OsfExportNotes.xml");
		 else if (ImportOtherSFSepBTTestPlan())
			RunXmlScript("OsfSepBtExportNotes.xml");

				Console.WriteLine("     Waiting for Notes export to finish");
		 fileIoStartDateTime = DateTime.Now;
		 string exportNotesFilePath = string.Empty;
		 if (ImportOtherSFTestPlan())
			exportNotesFilePath = "C:\\GuiTestResults\\ImportOtherSF\\Test Case " + m_testCaseNumber.ToString() + " Notes.sf";
		 else if (ImportOtherSFSepBTTestPlan())
			exportNotesFilePath = "C:\\GuiTestResults\\ImportOtherSF_SepBT\\Test Case " + m_testCaseNumber.ToString() + " Notes.sf";

				WaitForFileAccess(exportNotesFilePath);
				if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
		 WriteTimingLogFile("Malay OtherSF Import Test db, Export Notes", "aExportNotes.xlg", startDateTime);

		 // Create a compare file
		 string mrfNotes = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr-notes"].Value;
		 string mrfNotesPathName =
		   "C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportOtherSF\\" + mrfNotes;
		 string notesTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " Notes.sf";
		 WriteCompareFilesLogFile(mrfNotesPathName, notesTestCaseResultFile, exportNotesFilePath);

		Console.WriteLine("7b) Compare Notes");
		if (ImportOtherSFTestPlan())
		   RunXmlScript("OsfCompareNotes.xml");
		else if (ImportOtherSFSepBTTestPlan())
		   RunXmlScript("OsfSepBtCompareNotes.xml");
			}

	   // Create a compare file
	   string mrfScr = m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value;
	   string mrfScrPathName = "C:\\fw\\Test\\GuiTestDriver\\TeExpectedTestResults\\ImportOtherSF\\" + mrfScr;
	   string scrTestCaseResultFile = "Test Case " + m_testCaseNumber.ToString() + " Scripture.sf";
	   WriteCompareFilesLogFile(mrfScrPathName, scrTestCaseResultFile, exportScriptureFilePath);

	   if (m_xmlTestCases[m_testCaseNumber - 1].Attributes["mfr"].Value != "(none)")
	   {
		  Console.WriteLine("5b) Compare Scripture");
		  if (ImportOtherSFTestPlan())
			 RunXmlScript("OsfCompareScripture.xml");
		  else if (ImportOtherSFSepBTTestPlan())
			 RunXmlScript("OsfSepBtCompareScripture.xml");
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

		 //if (ImportOtherSFTestPlan())
		 //{
		 //   Console.WriteLine("2) Adding a 2nd Analysis Writing System");
		 //   RunXmlScript("OsfAddAnalysisWritingSys.xml");
		 //}

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

	   DateTime startDateTime = DateTime.Now;
			Console.WriteLine("1) Start Backup Restore");
			RunXmlScript("Ipt6StartSena3.xml");

	   //Console.WriteLine("2) Turn Seg BT option ON");
	   //RunXmlScript("IptSelectSegBT.xml");
	   //Console.WriteLine("3) Restart Sena3");
	   //RunXmlScript("Ipt6StartSena3.xml");

			Console.WriteLine("4) Export Open Xml");

	   RunXmlScript("RndTripExportXml.xml");

	   Console.WriteLine("     Waiting for RoundTrip export to finish");
	   DateTime fileIoStartDateTime = DateTime.Now;
	   string rndTripExportFilePath = "C:\\GuiTestResults\\RoundtripCompare\\Sena 3.oxes";
	   WaitForFileAccess(rndTripExportFilePath);
	   if (m_displayXmlScriptTimeSpan)
		  Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForFileAccess()");
	   WriteTimingLogFile("Sena3 Test db, Oxes RoundTrip Export", "aExportOxes.xlg", startDateTime);

	   Console.WriteLine("5) Import Open Xml");
			RunXmlScript("RndTripImportXml.xml");

			m_skipWaitForImportExtraSleep = true;
			Console.WriteLine("6) WaitForImportToFinish");
	   fileIoStartDateTime = DateTime.Now;
			WaitForImportToFinish("Sena 3");
			if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(fileIoStartDateTime) + "WaitForImportToFinish()");
	   WriteTimingLogFile("Sena3 Test db, Oxes RoundTrip Import", "aImportOxes.xlg", startDateTime);

	  Console.WriteLine("7) Compare Open Xml");
			RunXmlScript("RndTripBooksCompare.xml");

			Console.WriteLine(" ");
			Console.WriteLine("  Success!");
			ComputeTestCaseTimeSpan();
	  }

	  ///--------------------------------------------------------------------------------------
	  /// <summary>
	  /// Roundtrip export, create and import into an empty, new project, export again and compare the export files.
	  /// </summary>
	  ///--------------------------------------------------------------------------------------
	  public void RoundtripExportImportNewProjectCompare()
	  {
		 //m_displayXmlScriptTimeSpan = false;
		 WriteSetupFwInfo();
		 TestCasesVersionDateTestplan();
		 Console.WriteLine(" ");
		 Console.WriteLine("Test Case: RoundtripExportImportCompare");
		 Console.WriteLine(" ");

		 Console.WriteLine("1) Start Backup Restore");
		 RunXmlScript("Ipt6StartSena3.xml");

		 Console.WriteLine("2) Turn Seg BT option ON");
		 RunXmlScript("IptSelectSegBT.xml");
		 Console.WriteLine("3) Restart Sena3");
		 RunXmlScript("Ipt6StartSena3.xml");

		 Console.WriteLine("4) Export Open Xml");

		 RunXmlScript("RndTripExportXml.xml");

		 Console.WriteLine("     Waiting for RoundTrip export to finish");
		 DateTime startDateTime = DateTime.Now;
		 string rndTripExportFilePath = "C:\\GuiTestResults\\RoundtripCompare\\Sena 3.oxes";
		 WaitForFileAccess(rndTripExportFilePath);
		 if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForFileAccess()");

		 Console.WriteLine("5) Import Open Xml");
		 RunXmlScript("RndTripImportXml.xml");

		 m_skipWaitForImportExtraSleep = true;
		 Console.WriteLine("6) WaitForImportToFinish");
		 startDateTime = DateTime.Now;
		 WaitForImportToFinish("Sena 3");
		 if (m_displayXmlScriptTimeSpan)
			Console.WriteLine("   " + ComputeTimeSpan(startDateTime) + "WaitForImportToFinish()");

		 Console.WriteLine("7) Compare Open Xml");
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

	 // [Test]
	 // [Category("Roundtrip Export Import New Project Compare")]
	 //public void RndTripExportImportNewProjectCompare()
	 // {
	 //    m_testCaseNumber = 0;
	 //  RoundtripExportImportNewProjectCompare();
	 // }

		[Test]
		[Category("Restore_Options_Replace_Version")]
		public void RestoreOptReplaceVer()
		{
			m_testCaseNumber = 0;
			PT6RestoreOptionsReplaceVersion();
		}

		[Test]
	  [Category("Restore_Options_Seperate_Database")]
		public void RestoreOptSeperateDatabase()
		{
			m_testCaseNumber = 0;
			PT6RestoreOptionsSeperateDatabase();
		}

		[Test]
		[Category("Create_PT_Project")]
		public void CreatePtProject()
		{
			m_testCaseNumber = 0;
			CreateParatextProject();
		}

		[Test]
		[Category("Create_OtherSF_Project")]
		public void CreateOSFProject()
		{
			m_testCaseNumber = 0;
			CreateOtherSFProject();
		}

		[Test]
		[Category("Create_Pt_Restore_Project")]
		public void CreatePtRestoreProject()
		{
			m_testCaseNumber = 0;
			CreateParatextRestoreProject();
	}

	[Test]
	[Category("Roundtrip_Export_Import_Compare")]
	public void RndTripExportImportCompare()
	{
	   m_testCaseNumber = 0;
	   RoundtripExportImportCompare();
	}

		[Test]
		[Category("Test_Case_1")]
		public void ImportCase01()
		{
			m_testCaseNumber = 1;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_2")]
		public void ImportCase02()
		{
			m_testCaseNumber = 2;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_3")]
		public void ImportCase03()
		{
			m_testCaseNumber = 3;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_4")]
		public void ImportCase04()
		{
			m_testCaseNumber = 4;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_5")]
		public void ImportCase05()
		{
			m_testCaseNumber = 5;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_6")]
		public void ImportCase06()
		{
			m_testCaseNumber = 6;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_7")]
		public void ImportCase07()
		{
			m_testCaseNumber = 7;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_8")]
		public void ImportCase08()
		{
			m_testCaseNumber = 8;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_9")]
		public void ImportCase09()
		{
			m_testCaseNumber = 9;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_10")]
		public void ImportCase10()
		{
			m_testCaseNumber = 10;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_11")]
		public void ImportCase11()
		{
			m_testCaseNumber = 11;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_12")]
		public void ImportCase12()
		{
			m_testCaseNumber = 12;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_13")]
		public void ImportCase13()
		{
			m_testCaseNumber = 13;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_14")]
		public void ImportCase14()
		{
			m_testCaseNumber = 14;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_15")]
		public void ImportCase15()
		{
			m_testCaseNumber = 15;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_16")]
		public void ImportCase16()
		{
			m_testCaseNumber = 16;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_17")]
		public void ImportCase17()
		{
			m_testCaseNumber = 17;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_18")]
		public void ImportCase18()
		{
			m_testCaseNumber = 18;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_19")]
		public void ImportCase19()
		{
			m_testCaseNumber = 19;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_20")]
		public void ImportCase20()
		{
			m_testCaseNumber = 20;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_21")]
		public void ImportCase21()
		{
			m_testCaseNumber = 21;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_22")]
		public void ImportCase22()
		{
			m_testCaseNumber = 22;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_23")]
		public void ImportCase23()
		{
			m_testCaseNumber = 23;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_24")]
		public void ImportCase24()
		{
			m_testCaseNumber = 24;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_25")]
		public void ImportCase25()
		{
			m_testCaseNumber = 25;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_26")]
		public void ImportCase26()
		{
			m_testCaseNumber = 26;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_27")]
		public void ImportCase27()
		{
			m_testCaseNumber = 27;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_28")]
		public void ImportCase28()
		{
			m_testCaseNumber = 28;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_29")]
		public void ImportCase29()
		{
			m_testCaseNumber = 29;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_30")]
		public void ImportCase30()
		{
			m_testCaseNumber = 30;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_31")]
		public void ImportCase31()
		{
			m_testCaseNumber = 31;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_32")]
		public void ImportCase32()
		{
			m_testCaseNumber = 32;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_33")]
		public void ImportCase33()
		{
			m_testCaseNumber = 33;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_34")]
		public void ImportCase34()
		{
			m_testCaseNumber = 34;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_35")]
		public void ImportCase35()
		{
			m_testCaseNumber = 35;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_36")]
		public void ImportCase36()
		{
			m_testCaseNumber = 36;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_37")]
		public void ImportCase37()
		{
			m_testCaseNumber = 37;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_38")]
		public void ImportCase38()
		{
			m_testCaseNumber = 38;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_39")]
		public void ImportCase39()
		{
			m_testCaseNumber = 39;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_40")]
		public void ImportCase40()
		{
			m_testCaseNumber = 40;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_41")]
		public void ImportCase41()
		{
			m_testCaseNumber = 41;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_42")]
		public void ImportCase42()
		{
			m_testCaseNumber = 42;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_43")]
		public void ImportCase43()
		{
			m_testCaseNumber = 43;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_44")]
		public void ImportCase44()
		{
			m_testCaseNumber = 44;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_45")]
		public void ImportCase45()
		{
			m_testCaseNumber = 45;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_46")]
		public void ImportCase46()
		{
			m_testCaseNumber = 46;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_47")]
		public void ImportCase47()
		{
			m_testCaseNumber = 47;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_48")]
		public void ImportCase48()
		{
			m_testCaseNumber = 48;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_49")]
		public void ImportCase49()
		{
			m_testCaseNumber = 49;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_50")]
		public void ImportCase50()
		{
			m_testCaseNumber = 50;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_51")]
		public void ImportCase51()
		{
			m_testCaseNumber = 51;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_52")]
		public void ImportCase52()
		{
			m_testCaseNumber = 52;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_53")]
		public void ImportCase53()
		{
			m_testCaseNumber = 53;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_54")]
		public void ImportCase54()
		{
			m_testCaseNumber = 54;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_55")]
		public void ImportCase55()
		{
			m_testCaseNumber = 55;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_56")]
		public void ImportCase56()
		{
			m_testCaseNumber = 56;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_57")]
		public void ImportCase57()
		{
			m_testCaseNumber = 57;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_58")]
		public void ImportCase58()
		{
			m_testCaseNumber = 58;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_59")]
		public void ImportCase59()
		{
			m_testCaseNumber = 59;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_60")]
		public void ImportCase60()
		{
			m_testCaseNumber = 60;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_61")]
		public void ImportCase61()
		{
			m_testCaseNumber = 61;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_62")]
		public void ImportCase62()
		{
			m_testCaseNumber = 62;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_63")]
		public void ImportCase63()
		{
			m_testCaseNumber = 63;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_64")]
		public void ImportCase64()
		{
			m_testCaseNumber = 64;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_65")]
		public void ImportCase65()
		{
			m_testCaseNumber = 65;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_66")]
		public void ImportCase66()
		{
			m_testCaseNumber = 66;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_67")]
		public void ImportCase67()
		{
			m_testCaseNumber = 67;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_68")]
		public void ImportCase68()
		{
			m_testCaseNumber = 68;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_69")]
		public void ImportCase69()
		{
			m_testCaseNumber = 69;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_70")]
		public void ImportCase70()
		{
			m_testCaseNumber = 70;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_71")]
		public void ImportCase71()
		{
			m_testCaseNumber = 71;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_72")]
		public void ImportCase72()
		{
			m_testCaseNumber = 72;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_73")]
		public void ImportCase73()
		{
			m_testCaseNumber = 73;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_74")]
		public void ImportCase74()
		{
			m_testCaseNumber = 74;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_75")]
		public void ImportCase75()
		{
			m_testCaseNumber = 75;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_76")]
		public void ImportCase76()
		{
			m_testCaseNumber = 76;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_77")]
		public void ImportCase77()
		{
			m_testCaseNumber = 77;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_78")]
		public void ImportCase78()
		{
			m_testCaseNumber = 78;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_79")]
		public void ImportCase79()
		{
			m_testCaseNumber = 79;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_80")]
		public void ImportCase80()
		{
			m_testCaseNumber = 80;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_81")]
		public void ImportCase81()
		{
			m_testCaseNumber = 81;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_82")]
		public void ImportCase82()
		{
			m_testCaseNumber = 82;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_83")]
		public void ImportCase83()
		{
			m_testCaseNumber = 83;
			CommonTestFunctions();
		}

		[Test]
		[Category("Test_Case_84")]
		public void ImportCase84()
		{
			m_testCaseNumber = 84;
			CommonTestFunctions();
		}

	  [Test]
	  [Category("Test_Case_85")]
	  public void ImportCase85()
	  {
		 m_testCaseNumber = 85;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_86")]
	  public void ImportCase86()
	  {
		 m_testCaseNumber = 86;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_87")]
	  public void ImportCase87()
	  {
		 m_testCaseNumber = 87;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_88")]
	  public void ImportCase88()
	  {
		 m_testCaseNumber = 88;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_89")]
	  public void ImportCase89()
	  {
		 m_testCaseNumber = 89;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_90")]
	  public void ImportCase90()
	  {
		 m_testCaseNumber = 90;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_91")]
	  public void ImportCase91()
	  {
		 m_testCaseNumber = 91;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_92")]
	  public void ImportCase92()
	  {
		 m_testCaseNumber = 92;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_93")]
	  public void ImportCase93()
	  {
		 m_testCaseNumber = 93;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_94")]
	  public void ImportCase94()
	  {
		 m_testCaseNumber = 94;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_95")]
	  public void ImportCase95()
	  {
		 m_testCaseNumber = 95;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_96")]
	  public void ImportCase96()
	  {
		 m_testCaseNumber = 96;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_97")]
	  public void ImportCase97()
	  {
		 m_testCaseNumber = 97;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_98")]
	  public void ImportCase98()
	  {
		 m_testCaseNumber = 98;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_99")]
	  public void ImportCase99()
	  {
		 m_testCaseNumber = 99;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_100")]
	  public void ImportCase100()
	  {
		 m_testCaseNumber = 100;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_101")]
	  public void ImportCase101()
	  {
		 m_testCaseNumber = 101;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_102")]
	  public void ImportCase102()
	  {
		 m_testCaseNumber = 102;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_103")]
	  public void ImportCase103()
	  {
		 m_testCaseNumber = 103;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_104")]
	  public void ImportCase104()
	  {
		 m_testCaseNumber = 104;
		 CommonTestFunctions();
	  }

	  [Test]
	  [Category("Test_Case_105")]
	  public void ImportCase105()
	  {
		 m_testCaseNumber = 105;
		 CommonTestFunctions();
	  }
	}
	#endregion
}
