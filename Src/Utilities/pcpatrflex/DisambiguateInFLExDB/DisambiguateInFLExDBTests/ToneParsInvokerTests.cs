// Copyright (c) 2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using PtxUtils;
using SIL.DisambiguateInFLExDB;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.ToneParsFLEx;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;

namespace SIL.DisambiguateInFLExDBTests
{
	[TestFixture]
	class ToneParsInvokerTests : DisambiguateTests
	{
		string AnaExpectedString { get; set; }
		string AntExpectedString { get; set; }
		string LexExpectedString { get; set; }
		string ParserFilerXMLString { get; set; }
		string ToneParsBatchExpectedString { get; set; }
		string ToneParsCmdExpectedString { get; set; }
		string Word1ExpectedString { get; set; }
		string Word2ExpectedString { get; set; }
		string Word3ExpectedString { get; set; }
		string Word24ExpectedString { get; set; }
		ToneParsInvoker invoker { get; set; }
		const string kADCtlFile = "KuniToneParsTestadctl.txt";
		const string kGrammarFile = "KuniToneParsTestgram.txt";
		const string kLexiconFile = "KuniToneParsTestlex.txt";
		const string kLexiconTPFile = "KuniToneParsTestTPlex.txt";
		const string kTPSegFile = "KVGTP.seg";
		const string kTPIntxFile = "KVGintx.ctl";

		[SetUp]
		public override void FixtureSetup()
		{
			//IcuInit();
			TestDirInit();
			base.FixtureSetup();
			TestFile = Path.Combine(TestDataDir, "KuniToneParsTest.fwdata");
			SavedTestFile = Path.Combine(TestDataDir, "KuniToneParsTestB4.fwdata");
			ToneParsInvokerOptions.Instance.ResetAllOptions();

			base.FixtureSetup();
		}

		/// <summary></summary>
		[TearDown]
		public override void FixtureTeardown()
		{
			base.FixtureTeardown();
		}

		/// <summary>
		/// Test conversion of log file using hvos to using glosses.
		/// </summary>
		[Test]
		public void ToneParsHvoToGlossInLogTest()
		{
			var logFileWithHvos = Path.Combine(TestDataDir, "ToneParsInvokerWithHvos.log");
			var logFileWithMorphnames = Path.Combine(
				TestDataDir,
				"ToneParsInvokerWithMorphnames.log"
			);
			var converter = new ToneParsLogConverter(MyCache, logFileWithHvos);
			var expected = File.ReadAllText(logFileWithMorphnames);
			var result = converter.ConvertHvosToMorphnames();
			Assert.AreEqual(expected, result);
		}

		/// <summary>
		/// Test invoking of XAmple followed by TonePars.
		/// </summary>
		[Test]
		public void ToneParsInvokerTest()
		{
			// Check for the existence of the TonePars executable
			Assert.IsTrue(File.Exists(Path.Combine(FwDirectoryFinder.ExeOrDllDirectory, "TonePars64.exe")));
			File.Copy(
				Path.Combine(TestDataDir, kADCtlFile),
				Path.Combine(Path.GetTempPath(), kADCtlFile),
				true
			);
			File.Copy(
				Path.Combine(TestDataDir, kGrammarFile),
				Path.Combine(Path.GetTempPath(), kGrammarFile),
				true
			);
			File.Copy(
				Path.Combine(TestDataDir, kLexiconFile),
				Path.Combine(Path.GetTempPath(), kLexiconFile),
				true
			);
			File.Copy(
				Path.Combine(TestDataDir, kTPSegFile),
				Path.Combine(Path.GetTempPath(), kTPSegFile),
				true
			);
			File.Copy(
				Path.Combine(TestDataDir, kTPIntxFile),
				Path.Combine(Path.GetTempPath(), kTPIntxFile),
				true
			);
			string toneParsLexFile = Path.Combine(TestDataDir, "KvgTP.ctl");
			string toneParsRuleFile = Path.Combine(TestDataDir, "KvgTP.ctl");
			string intxCtlFile = Path.Combine(TestDataDir, "KVGintx.ctl");
			string inputFile = Path.Combine(TestDataDir, "KVGinput.txt");
			invoker = new ToneParsInvoker(toneParsRuleFile, intxCtlFile, inputFile, '+', MyCache);
			CreateExpectedFileStrings();
			File.Copy(Path.Combine(TestDataDir, "ToneParsInvoker.ana"), Path.Combine(Path.GetTempPath(), "ToneParsInvoker.ana"), true);
			ToneParsInvokerOptions.Instance.VerifyInformation = true;
			invoker.Invoke();
			CompareResultToExpectedFile(ToneParsBatchExpectedString, invoker.ToneParsBatchFile, true);
			CompareResultToExpectedFile(ToneParsCmdExpectedString, invoker.ToneParsCmdFile, true);
			CompareResultToExpectedFile(AnaExpectedString, invoker.AnaFile);
			CompareResultToExpectedFile(AntExpectedString, invoker.AntFile);
			CompareResultToExpectedFile(
				LexExpectedString,
				Path.Combine(Path.GetTempPath(), kLexiconTPFile)
			);

			Boolean found = invoker.ConvertAntToParserFilerXML(0);
			Assert.AreEqual(false, found);
			found = invoker.ConvertAntToParserFilerXML(1);
			Assert.AreEqual(true, found);
			Assert.AreEqual(Word1ExpectedString, invoker.ParserFilerXMLString);
			found = invoker.ConvertAntToParserFilerXML(2);
			Assert.AreEqual(true, found);
			Assert.AreEqual(Word2ExpectedString, invoker.ParserFilerXMLString);
			found = invoker.ConvertAntToParserFilerXML(3);
			Assert.AreEqual(true, found);
			Assert.AreEqual(Word3ExpectedString, invoker.ParserFilerXMLString);
			// find last one and then look for one beyond it
			found = invoker.ConvertAntToParserFilerXML(24);
			Assert.AreEqual(true, found);
			Assert.AreEqual(Word24ExpectedString, invoker.ParserFilerXMLString);
			found = invoker.ConvertAntToParserFilerXML(25);
			Assert.AreEqual(false, found);

			var wf﻿Mbumbukiam = invoker.GetWordformFromString("Mbumbukiam");
			Assert.NotNull(wfMbumbukiam);
			Assert.AreEqual(0, wfMbumbukiam.ParserCount);
			var wffia = invoker.GetWordformFromString("fia");
			Assert.NotNull(wffia);
			Assert.AreEqual(0, wffia.ParserCount);
			var wfndot = invoker.GetWordformFromString("ndø-tá");
			Assert.NotNull(wfndot);
			Assert.AreEqual(0, wfndot.ParserCount);
			var wfndot2 = invoker.GetWordformFromString("ndǿ-ta");
			Assert.NotNull(wfndot2);
			Assert.AreEqual(0, wfndot2.ParserCount);

			invoker.SaveResultsInDatabase();
			Assert.AreEqual(1, wfMbumbukiam.ParserCount);
			Assert.AreEqual(1, wffia.ParserCount);
			Assert.AreEqual(3, wfndot.ParserCount);
			Assert.AreEqual(9, wfndot2.ParserCount);
		}

		private void CompareResultToExpectedFile(string expectedFileString, string actualFile, bool normalizeContent = false)
		{
			string result = CreateFileString(actualFile);
			string expected = expectedFileString;
			if (normalizeContent)
			{
				result = NormalizeContent(result);
				expected = NormalizeContent(expectedFileString);
			}
			Assert.AreEqual(expected, result);
		}

		private string NormalizeContent(string input)
		{
			string tp = "tonepars64";
			string normalized = NormalizeViaIndex(input, "AppData", "");
			normalized = NormalizeViaIndex(normalized, "TestData", "");
			normalized = NormalizeViaIndex(normalized, tp, tp);
			return normalized;
		}

		private static string NormalizeViaIndex(string input, string match, string change)
		{
			// I tried to use regular expressions but never got them to match...
			int iAppData = input.IndexOf(match);
			if (iAppData != -1)
			{
				int iColon = input.IndexOf(":");
				if (iColon != -1)
				{
					iColon--; // skip the drive letter, too
					string appdataPath = input.Substring(iColon, iAppData - iColon);
					input = input.Replace(appdataPath, change);
				}
			}
			return input;
		}

		private void CreateExpectedFileStrings()
		{
			AnaExpectedString = CreateFileString(
				Path.Combine(TestDataDir, Path.GetFileName(invoker.AnaFile))
			);
			AntExpectedString = CreateFileString(
				Path.Combine(TestDataDir, Path.GetFileName(invoker.AntFile))
			);
			LexExpectedString = CreateFileString(
				Path.Combine(TestDataDir, Path.GetFileName(kLexiconTPFile))
			);
			Word1ExpectedString = CreateFileString(Path.Combine(TestDataDir, "Word1Expected.xml"));
			Word2ExpectedString = CreateFileString(Path.Combine(TestDataDir, "Word2Expected.xml"));
			Word3ExpectedString = CreateFileString(Path.Combine(TestDataDir, "Word3Expected.xml"));
			Word24ExpectedString = CreateFileString(
				Path.Combine(TestDataDir, "Word24Expected.xml")
			);
			ToneParsBatchExpectedString = CreateFileString(
				Path.Combine(TestDataDir, Path.GetFileName(invoker.ToneParsBatchFile))
			);
			ToneParsCmdExpectedString = CreateFileString(
				Path.Combine(TestDataDir, Path.GetFileName(invoker.ToneParsCmdFile))
			);
		}

		private string CreateFileString(string fileName)
		{
			string expectedFile = Path.Combine(TestDataDir, fileName);
			string result = "";
			using (var streamReader = new StreamReader(expectedFile, Encoding.UTF8))
			{
				result = streamReader.ReadToEnd().Replace("\r", "");
			}
			return result;
		}
	}
}
