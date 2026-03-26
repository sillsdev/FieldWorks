using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.HCSynthByGloss;
using SIL.Machine.Morphology.HermitCrab;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;


namespace SIL.HCSynthByGlossDllTests
{
	public class HCSynthByGlossDllTests
	{
		string glossFile = "";
		string testDataDir = "";
		string expectedWordFormsFile = "";
		string tempFile;
		HCSynthByGlossDll dll;
		string hcConfig = "";
		string result = "";

		[SetUp]
		public void Setup()
		{
			testDataDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "HCSynthByGloss", "HCSynthByGloss", "TestData");
			hcConfig = Path.Combine(testDataDir, "indoHC4FLExrans.xml");
			glossFile = Path.Combine(testDataDir, "IndonesianAnalyses.txt");
			tempFile = Path.Combine(Path.GetTempPath(), "results.txt");
			dll = new HCSynthByGlossDll(tempFile);
		}

		[Test]
		public void SetFilesTest()
		{
			string result = dll.SetHcXmlFile("abc");
			Assert.AreEqual(dll.kError1 + dll.kHCXmlFile + dll.kError2 + "abc" + dll.kError3, result);
			result = dll.SetHcXmlFile(hcConfig);
			Assert.AreEqual(dll.kSuccess, result);
			result = dll.SetGlossFile("abc");
			Assert.AreEqual(dll.kError1 + dll.kGlossFile + dll.kError2 + "abc" + dll.kError3, result);
			result = dll.SetGlossFile(glossFile);
			Assert.AreEqual(dll.kSuccess, result);
		}

		[Test]
		public void ProcessTest()
		{
			Stopwatch stopwatch = Stopwatch.StartNew();
			dll = new HCSynthByGlossDll(tempFile);
			result = dll.SetHcXmlFile(hcConfig);
			Assert.AreEqual(dll.kSuccess, result);
			result = dll.SetGlossFile(glossFile);
			Assert.AreEqual(dll.kSuccess, result);
			result = dll.Process();
			stopwatch.Stop();
			Console.WriteLine("Initial processing time = " + stopwatch.ElapsedMilliseconds);
			Assert.AreEqual(dll.kSuccess, result);
			expectedWordFormsFile = Path.Combine(testDataDir, "expectedWordForms.txt");
			string expectedWordForms = File.ReadAllText(expectedWordFormsFile, Encoding.UTF8)
				.Replace("\r", "");
			string synthesizedWordForms = File.ReadAllText(tempFile, Encoding.UTF8)
				.Replace("\r", "");
			//Console.Write(synthesizedWordForms);
			Assert.AreEqual(expectedWordForms, synthesizedWordForms);
			// Process again without initializing the input files
			stopwatch.Restart();
			result = dll.Process();
			stopwatch.Stop();
			Console.WriteLine("Restart processing time = " + stopwatch.ElapsedMilliseconds);
			Assert.AreEqual(dll.kSuccess, result);
			synthesizedWordForms = File.ReadAllText(tempFile, Encoding.UTF8)
				.Replace("\r", "");
			//Console.Write(synthesizedWordForms);
			Assert.AreEqual(expectedWordForms, synthesizedWordForms);
		}

		[Test]
		public void ProcessBadInputFilesTest()
		{
			dll = new HCSynthByGlossDll(tempFile);
			result = dll.Process();
			Assert.AreEqual(dll.kError1 + dll.kHCXmlFile + dll.kError2 + "" + dll.kError3, result);
			result = dll.SetHcXmlFile(hcConfig);
			Assert.AreEqual(dll.kSuccess, result);
			result = dll.Process();
			Assert.AreEqual(dll.kError1 + dll.kGlossFile + dll.kError2 + "" + dll.kError3, result);
		}
	}
}
