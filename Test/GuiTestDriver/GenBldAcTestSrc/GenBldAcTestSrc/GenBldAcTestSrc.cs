// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: GenBldAcTestSrc.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections;
using System.IO;

namespace GuiTestDriver
{
	/// <summary>
	/// Finds out where FieldWorks is installed.
	/// Reads the GtdConfig.xml file to find out where the acceptance test scripts are.
	/// Queries the file system to find out what test scripts are there.
	/// Sorts them in run order.
	/// Generates a c-sharp source file for an Nunit test fixture dll to run them.
	/// The generated file is called FlexAcTests.cs.
	/// </summary>
	public class GenBldAcTestSrc
	{
		public GenBldAcTestSrc() {}

		public static int Main(string[] args)
		{
			if (args.Length != 1)
			{
				System.Console.WriteLine("Usage: GenBldAcTestSrc scriptPath");
				return -1;
			}
			string scriptPath = args[0];
			// Create a reference to the current directory.
			DirectoryInfo di = new DirectoryInfo(scriptPath);
			if (di.Exists)
			{ // Create an array representing the files in the script directory.
				FileInfo[] scripts = di.GetFiles("*.xml");
				ArrayList src = genNunitSrc(scripts);
				// write src to a file in the ?? directory
				// Create an instance of StreamWriter to write text to a file.
				// The using statement also closes the StreamWriter.
				using (StreamWriter sw = new StreamWriter(scriptPath+@"\FlexAcTests.cs"))
				{
					foreach ( string line in src)
					{
						sw.WriteLine(line);
					}
				}
			}
			else
			{
				System.Console.WriteLine("GenBldAcTestSrc could not find "+scriptPath);
				return 1;
			}
			return 0;
		}

		private static ArrayList genNunitSrc(FileInfo[] scripts)
		{
			ArrayList src = genStaticSrc();
			foreach (FileInfo script in scripts)
			{
				string noExt = script.Name.Substring(0,script.Name.Length-4);
				src.Add("");
				src.Add("    [Test]");
				src.Add("    public void "+noExt+"()");
				src.Add("    { m_rt.fromFile(\""+noExt+"\"); }");
			}
			src.Add("");
			src.Add("  }");
			src.Add("}");
			return src;
		}

		private static ArrayList genStaticSrc()
		{
			ArrayList src = new ArrayList(100);
			src.Add("// --------------------------------------------------------------------------------------------");
			src.Add("#region // Copyright (c) 2003, SIL International. All Rights Reserved.");
			src.Add("// <copyright from='2006' to='2006' company='SIL International'>");
			src.Add("//		Copyright (c) 2003, SIL International. All Rights Reserved.");
			src.Add("//");
			src.Add("//		Distributable under the terms of either the Common Public License or the");
			src.Add("//		GNU Lesser General Public License, as specified in the LICENSING.txt file.");
			src.Add("// </copyright>");
			src.Add("#endregion");
			src.Add("//");
			src.Add("// File: FlexAcTests.cs");
			src.Add("// Responsibility: LastufkaM");
			src.Add("// Last reviewed: yesturday");
			src.Add("//");
			src.Add("// <remarks>");
			src.Add("// Generated with the acceptance tests of the day, daily.");
			src.Add("// </remarks>");
			src.Add("// --------------------------------------------------------------------------------------------");
			src.Add("using System;");
			src.Add("using System.Threading;");
			src.Add("using NUnit.Framework;");
			src.Add("");
			src.Add("namespace GuiTestDriver");
			src.Add("{");
			src.Add("  [TestFixture]");
			src.Add("  public class FlexAcTests");
			src.Add("  {");
			src.Add("    RunTest m_rt = new RunTest(\"LC\");");
			src.Add("");
			src.Add("    public FlexAcTests() {}");
			src.Add("");
			// insert tests here!
			//src.Add("");
			//src.Add("  }");
			//src.Add("}");
			return src;
		}
	}
}
