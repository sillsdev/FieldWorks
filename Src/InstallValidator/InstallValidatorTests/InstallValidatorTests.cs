// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

#if !__MonoCS__

using System.Diagnostics;
using System.IO;
using System.Linq;
using NUnit.Framework;
using SIL.FieldWorks.Build.Tasks;
using SIL.TestUtilities;

namespace SIL.InstallValidator
{
	[TestFixture]
	public class InstallValidatorTests
	{
		private TemporaryFolder m_TestInstallDir;

		private string m_startingDirectory;

		private const string LogFileName = "metadataLog.csv";
		private const string ResultsFlieName = "resultsLog.csv";

		[SetUp]
		public void SetUp()
		{
			m_startingDirectory = Directory.GetCurrentDirectory();
			m_TestInstallDir = new TemporaryFolder("InstallValidatorTest");
			Directory.SetCurrentDirectory(m_TestInstallDir.Path);
		}

		[TearDown]
		public void TearDown()
		{
			Directory.SetCurrentDirectory(m_startingDirectory); // CD out of the directory before deleting it
			m_TestInstallDir.Dispose();
		}

		[Test]
		public void InstallValidatorTest()
		{
			// "install" some files
			var currentDir = Directory.GetCurrentDirectory();

			const string goodFileName = "correctly-installed-file.txt";
			var goodFilePath = Path.Combine(currentDir, goodFileName);
			File.WriteAllText(goodFileName, "contents");

			const string badFileName = "incorrectly-installed-file.txt";
			var badFilePath = Path.Combine(currentDir, badFileName);
			File.WriteAllText(badFileName, "contents");

			const string missingFileName = "not-installed-file.txt";
			var missingFilePath = Path.Combine(currentDir, missingFileName);
			File.WriteAllText(missingFileName, "contents");

			// record installed files' info in a CSV
			new LogMetadata
			{
				Files = new[] { goodFilePath, badFilePath, missingFilePath },
				LogFile = LogFileName,
				PathPrefixToDrop = $"{currentDir}/"
			}.Execute();

			// damage some installed files
			File.Delete(missingFileName);
			File.AppendAllText(badFileName, " are incorrect!");

			// SUT
			SystemUnderTest();

			var results = File.ReadLines(ResultsFlieName).Skip(2).Select(line => line.Split(',')).ToDictionary(line => line[0]);

			Assert.Contains(goodFileName, results.Keys);
			var result = results[goodFileName];
			Assert.AreEqual(2, result.Length, "correctly-installed files should have two columns in the report");
			StringAssert.EndsWith("installed correctly", result[1]);

			Assert.Contains(badFileName, results.Keys);
			result = results[badFileName];
			Assert.GreaterOrEqual(result.Length, 2, "'bad file' report");
			StringAssert.Contains("incorrect", result[1]);

			Assert.Contains(missingFileName, results.Keys);
			result = results[missingFileName];
			Assert.GreaterOrEqual(result.Length, 2, "'missing file' report");
			StringAssert.EndsWith("missing", result[1]);

			if (results.Count > 3)
			{
				LogInputAndOutputFiles();
				Assert.Fail("Too many lines in the report");
			}
		}

		private void SystemUnderTest()
		{
			var sut = new Process
			{
				StartInfo = new ProcessStartInfo(Path.Combine(m_startingDirectory, "InstallValidator.exe"), $"{LogFileName} {ResultsFlieName}")
				{
					RedirectStandardError = true,
					UseShellExecute = false,
					CreateNoWindow = true
				}
			};
			sut.Start();
			var error = sut.StandardError.ReadToEnd(); // wait for the process to complete
			if (!string.IsNullOrWhiteSpace(error))
			{
				LogInputAndOutputFiles();
				Assert.Fail(error);
			}
		}

		private static void LogInputAndOutputFiles()
		{
				Debug.WriteLine("Input file:");
				Debug.WriteLine(File.ReadAllText(LogFileName));
				Debug.WriteLine("Output File:");
				Debug.WriteLine(File.ReadAllText(ResultsFlieName));
		}
	}
}

#endif
