// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//-------------------------------------------------------------------------------
#if WANTTESTPORT //(FLEx) Need to port these tests to the new FDO & to use FileUtils
using System;
using System.IO;
using SIL.FieldWorks.Common.FwUtils;
using NUnit.Framework;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// Test SFM export
	/// </summary>
	[TestFixture]
	public class StandardFormat : FxtTestBase
	{
		/// <summary>
		/// Location of simple test FXT files
		/// </summary>
		protected string m_testDir;

		public override void Init()
		{
			base.Init();
			m_testDir = Path.Combine(FwDirectoryFinder.FlexFolder, "Export Templates");
		}

		[Test]
		[Ignore("TestLangProj export tests need upgrading.")]
		public void MDF()
		{
			string sFxtPath = Path.Combine(m_testDir, "mdf.xml");
			string sAnswerFile = Path.Combine(m_sExpectedResultsPath, "TLPStandardFormatMDF.sfm");
			DoDump("TestLangProj", "MDF", sFxtPath, sAnswerFile);
		}
		[Test]
		[Ignore("TestLangProj export tests need upgrading.")]
		public void RootBasedMDF()
		{
			string sFxtPath = Path.Combine(m_testDir, "RootBasedMDF.xml");
			string sAnswerFile = Path.Combine(m_sExpectedResultsPath, "TLPRootBasedMDF.sfm");
			DoDump("TestLangProj", "RootBasedMDF", sFxtPath, sAnswerFile);
		}
		[Test]
		[Ignore("TestLangProj export tests need upgrading.")]
		public void TwoTimesSpeedTest()
		{
			string sFxtPath = Path.Combine(m_testDir, "mdf.xml");
			XDumper dumper = PrepareDumper("TestLangProj",sFxtPath, false);
			PerformDump(dumper, @"C:\first.txt", "TestLangProj", "first");
			PerformDump(dumper, @"C:\second.txt", "TestLangProj", "second");
			string sAnswerFile = Path.Combine(m_sExpectedResultsPath, "TLPStandardFormatMDF.sfm");
			CheckFilesEqual(sAnswerFile, @"C:\first.txt");
			CheckFilesEqual(@"C:\first.txt", @"C:\second.txt");
		}

		public void CheckFilesEqual(string sAnswerPath, string outputPath)
		{
			using (StreamReader test = new StreamReader(outputPath))
			using (StreamReader control = new StreamReader(sAnswerPath))
			{
				string testResult = test.ReadToEnd();
				string expected = control.ReadToEnd();
				if (Environment.OSVersion.Platform == PlatformID.Unix)
				{
					// The xslt processor on linux inserts a BOM at the beginning, and writes \r\n for newlines.
					int iBegin = testResult.IndexOf("\\lx ");
					if (iBegin > 0 && iBegin < 6)
						testResult = testResult.Substring(iBegin);
					testResult = testResult.Replace("\r\n", "\n");
				}
				Assert.AreEqual(expected, testResult,
					"FXT Output Differs. If you have done a model change, you can update the 'correct answer' xml files by runing fw\\bin\\FxtAnswersUpdate.bat.");
			}
		}

	}
}
#endif
