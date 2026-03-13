// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.AlloGenModel;
using SIL.AlloGenService;
using SIL.FieldWorks.Common.FwUtils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace SIL.AlloGenServiceTest
{
	public class ReplacerTests
	{
		XmlBackEndProvider provider = new XmlBackEndProvider();
		string TestDataDir { get; set; }
		string AlloGenFile { get; set; }
		string AlloGenExpected { get; set; }
		Replacer replacer { get; set; }
		List<WritingSystem> writingSystems = new List<WritingSystem>();

		[SetUp]
		public void Setup()
		{
			TestDataDir = Path.Combine(FwDirectoryFinder.SourceDirectory, "Utilities", "AlloVarGen", "AlloGenService", "AlloGenServiceTests", "TestData");
			AlloGenExpected = Path.Combine(TestDataDir, "AlloGenReplace.xml");
		}

		[Test]
		public void ReplaceTest()
		{
			provider.LoadDataFromFile(AlloGenExpected);
			AllomorphGenerators allomorphGenerators = provider.AlloGens;
			Assert.NotNull(allomorphGenerators);
			Operation operation = allomorphGenerators.Operations[0];
			replacer = new Replacer(allomorphGenerators.ReplaceOperations);
			writingSystems = allomorphGenerators.WritingSystems;
			string input = "";
			compareResult(input, "\u00A0", writingSystems[0]);
			compareResult(input, "\u00A0", writingSystems[1]);
			compareResult(input, "\u00A0", writingSystems[2]);
			compareResult(input, "\u00A0", writingSystems[3]);
			compareResult(input, "\u00A0", writingSystems[4]);
			input = "*a:";
			compareResult(input, "a", writingSystems[0]);
			compareResult(input, "a", writingSystems[1]);
			compareResult(input, "a", writingSystems[2]);
			compareResult(input, "a", writingSystems[3]);
			//{ 74D68073 - 0CA7 - 4EBE - B3EC - 9D4BFB4D8FFF}
			compareResult(input, "aa", writingSystems[4]);
			input = "*arka:";
			compareResult(input, "arka", writingSystems[0]);
			compareResult(input, "arka", writingSystems[1]);
			compareResult(input, "arka", writingSystems[2]);
			compareResult(input, "arka", writingSystems[3]);
			compareResult(input, "arkaa", writingSystems[4]);
			input = "*chillinya:";
			compareResult(input, "chillinya", writingSystems[0]);
			compareResult(input, "chillinya", writingSystems[1]);
			compareResult(input, "chillinya", writingSystems[2]);
			compareResult(input, "chillinya", writingSystems[3]);
			compareResult(input, "chillinyaa", writingSystems[4]);
			input = "*yarqa:.v2";
			compareResult(input, "yarqa", writingSystems[0]);
			compareResult(input, "yarqa", writingSystems[1]);
			compareResult(input, "yarqa", writingSystems[2]);
			compareResult(input, "yarqa", writingSystems[3]);
			compareResult(input, "yarqaa", writingSystems[4]);
			input = "+yusulpa:";
			compareResult(input, "yusulpa", writingSystems[0]);
			compareResult(input, "yusulpa", writingSystems[1]);
			compareResult(input, "yusulpa", writingSystems[2]);
			compareResult(input, "yusulpa", writingSystems[3]);
			compareResult(input, "yusulpaa", writingSystems[4]);
		}

		private void compareResult(string input, string expected, WritingSystem ws)
		{
			string result = replacer.ApplyReplaceOpToOneWS(input, ws.Name);
			Assert.AreEqual(expected, result);
		}
	}
}
