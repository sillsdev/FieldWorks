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
// File: TeTests.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.IO;			// for Path
using System.Threading;
using NUnit.Framework;

namespace GuiTestDriver
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	[TestFixture]
	public class ProgManage
	{
		RunTest m_rt = new RunTest("LT");

		public ProgManage(){}

		[Test]
		public void paaSounds(){m_rt.fromFile("paaSounds");}
		[Test]
		public void pabHyperlink(){m_rt.fromFile("pabHyperlink.xml");}
		[Test]
		public void pmeVernacularWsSwap(){m_rt.fromFile("pmeVernacularWsSwap.xml");}
		[Test]
		public void pmgAddDelStyle() { m_rt.fromFile("pmgAddDelStyle.xml"); }
		[Test]
		public void pmgEditFindUndo() { m_rt.fromFile("pmgEditFindUndo.xml"); }
		[Test]
		public void pmgExtLink2Dn(){m_rt.fromFile("pmgExtLink2Dn.xml");}
		[Test]
		public void pmgHelp() { m_rt.fromFile("pmgHelp.xml"); }
		[Test]
		public void pmgLT_2602() { m_rt.fromFile("pmgLT_2602.xml"); }
		[Test]
		public void pmgProp() { m_rt.fromFile("pmgProp.xml"); }
		[Test]
		public void pmgShortcuts() { m_rt.fromFile("pmgShortcuts.xml"); }
		[Test]
		public void pmgStartTE(){m_rt.fromFile("pmgStartTE.xml");}
		[Test]
		public void pmpNewProject(){m_rt.fromFile("pmpNewProject.xml");}

		private string MakeValidVarPath(string text)
		{
			return text.Replace("~", "{~}");
		}

		[Test]
		public void pmzImport()
		{
			// for now the path is hard coded, in the future it will be temporary dirs and
			// taken from the model
			string dbFileName = System.IO.Path.GetTempFileName();
			string mapFileName = System.IO.Path.GetTempFileName();

			m_rt.AddVariable("SFMImportFileName", MakeValidVarPath(dbFileName));	// import file
			m_rt.AddVariable("SFMMapFileName", MakeValidVarPath(mapFileName));		// map file

			StreamWriter writer = new StreamWriter(dbFileName);
			writer.WriteLine(@"\lx men");
			writer.WriteLine(@"\mn man ï from moon");
			writer.WriteLine(@"This is a test.");
			writer.WriteLine(@"Of a lónger field");
			writer.WriteLine(@"\mn this is a good line.");
			writer.WriteLine(@"\mn2 hiâ there");
			writer.WriteLine(@"\mn2 this is a good line.");
			writer.WriteLine(@"\notUsedMarker");
			writer.Close();
			try
			{
				// this script will use the SFMImportFileName for the import file
				m_rt.fromFile("pmzImport");

				// Now check the output files against the key_output files
//				string keyP1 = "FLEX_A_Phase1Output.xml";	// phase 1 key output file
//				string keyP2 = "FLEX_A_Phase2Output.xml";	// phase 2 key output file
//				string keyP3 = "FLEX_A_Phase3Output.xml";	// phase 3 key output file
//				string keyP4 = "FLEX_A_Phase4Output.xml";	// phase 4 key output file
//				string keyPath = "";
//				string tempPath = System.IO.Path.GetTempPath();
//				if (tempPath.EndsWith(@"\") == false)
//					tempPath += @"\";
//				tempPath += "LexText\\";

				// The best way would be using the XmlDiffPatch by MS, but not installed on client machines yet
//				Microsoft.XmlDiffPatch.XmlDiff diff = new Microsoft.XmlDiffPatch.XmlDiff(
//					Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreChildOrder |
//					Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreComments |
//					Microsoft.XmlDiffPatch.XmlDiffOptions.IgnoreWhitespace );
//				bool same = diff.Compare(tempPath+"Phase1Output.xml", "FLEX_A_Phase1Output.xml", false);
//				if (!same)
//					System.Diagnostics.Debug.WriteLine("Files weren't the same.");
			}
			finally
			{
				File.Delete(dbFileName);	// be sure to remove the temp file after this test
				File.Delete(mapFileName);	// be sure to remove the temp file after this test
			}
		}

	}
}
