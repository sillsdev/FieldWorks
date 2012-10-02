using System;
using System.IO;
using System.Text;
using NUnit.Framework;
using PatrParserWrapper;

namespace PatrParserWrapperTest
{
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "Currently not all linux's able to load libpatr.so for libc reasons.")]
	public class PatrParserInteropTests
	{
		protected const string LocationToSrcDirFromOutputDebug =
			"../../Src/LexText/ParserEngine/ParserCore/PatrParserWrapper/PatrParserWrapperTests/";

		protected const string GrammarFileName =
			LocationToSrcDirFromOutputDebug + "StemNameWordGrammar.txt";

		protected const string LexiconFileName =
			LocationToSrcDirFromOutputDebug + "MyLex.txt";

		protected PatrParser CreateAndSetupTestParser()
		{
			PatrParser parser = null;
			try
			{
				parser = new PatrParser { CommentChar = '|', CodePage = Encoding.UTF8.CodePage };
			Assert.NotNull(parser);
			parser.LoadGrammarFile(GrammarFileName);
			parser.LoadLexiconFile(LexiconFileName, 1);

			return parser;
		}
			catch (Exception)
			{
				if (parser != null)
					parser.Dispose();
				throw;
			}
		}

		[Test]
		public void ParseString()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.NotNull(parser.ParseString("hi"));
		}

		[Test]
		public void ParseFile()
		{
			string tempfile = Path.GetTempFileName();
			string outputfile = Path.GetTempFileName();
			try
			{
				using (StreamWriter writer = File.CreateText(tempfile))
				{
					writer.WriteLine("hi");
					writer.Close();
				}
				using (var parser = CreateAndSetupTestParser())
				{
					parser.ParseFile(tempfile, outputfile);
					var f = new FileInfo(outputfile);
					Assert.Greater(f.Length, 0, "File length should be greater than 0 bytes");
				}
			}
			finally
			{
				File.Delete(tempfile);
				File.Delete(outputfile);
			}
		}

		[Test]
		public void LoadGrammarFile()
		{
			using (var parser = new PatrParser())
			{
				parser.CommentChar = '|';
				parser.CodePage = Encoding.UTF8.CodePage;
				Assert.NotNull(parser);
				parser.LoadGrammarFile(GrammarFileName);
			}
		}

		[Test]
		public void LoadLexiconFile()
		{
			using (var parser = new PatrParser())
			{
				parser.CommentChar = '|';
				parser.CodePage = Encoding.UTF8.CodePage;
				Assert.NotNull(parser);
				parser.LoadLexiconFile(LexiconFileName, 1);
			}
		}

		[Test]
		public void Clear()
		{
			using (var parser = CreateAndSetupTestParser())
				parser.Clear();
		}

		[Test]
		public void TestLogFile()
		{
			string tempfile = Path.GetTempFileName();
			try
			{
				using (var parser = CreateAndSetupTestParser())
				{
					parser.OpenLog(tempfile);
					Assert.AreEqual(parser.LogFile.TrimEnd('\0'), tempfile);

					parser.ParseString("hi");
					// cause some log file to be generated
					parser.CloseLog();
				}

				var f = new FileInfo(tempfile);
				Assert.Greater(f.Length, 0, "Log file length should be greater than 0 bytes");
			}
			finally
			{
				File.Delete(tempfile);
			}
		}

		[Test]
		public void GrammarFile()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.GrammarFile.TrimEnd('\0'), GrammarFileName);
		}

		[Test]
		public void LexiconFile()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.get_LexiconFile(0).TrimEnd('\0'), LexiconFileName);
		}

		[Test]
		public void Unification()
		{
			using (var parser = CreateAndSetupTestParser())
			{
				Assert.IsTrue(parser.Unification == 1, "Unification should default to true");
				parser.Unification = 0;
				Assert.IsTrue(parser.Unification == 0, "Unification should be false");
				parser.Unification = 1;
				Assert.IsTrue(parser.Unification == 1, "Unification should be true");
			}
		}

		[Test]
		public void TreeDisplay()
		{
			using (var parser = CreateAndSetupTestParser())
			{
				Assert.IsTrue(parser.TreeDisplay == 2, "TreeDisplay should default to 2");
				parser.TreeDisplay = 0;
				Assert.IsTrue(parser.TreeDisplay == 0, "TreeDisplay should be 0");
				parser.TreeDisplay = 2;
				Assert.IsTrue(parser.TreeDisplay == 2, "TreeDisplay should be 2");
			}
		}

		[Test]
		public void RootGlossFeature()
		{
			using (var parser = CreateAndSetupTestParser())
			{
				Assert.IsTrue(parser.RootGlossFeature == 0, "RootGlossFeature should default to 0");
				parser.RootGlossFeature = 1;
				Assert.IsTrue(parser.RootGlossFeature == 1, "RootGlossFeature should be 1");
				parser.RootGlossFeature = 0;
				Assert.IsTrue(parser.RootGlossFeature == 0, "RootGlossFeature should be 0");
			}
		}

		[Test]
		public void Gloss()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.Gloss == 1, "Gloss should default to true");
		}

		[Test]
		public void MaxAmbiguity()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.MaxAmbiguity == 10, "MaxAmbiguity should default to 10");
		}

		[Test]
		public void CheckCycles()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.CheckCycles == 1, "CheckCycles should default to true");
		}

		[Test]
		public void CommentChar()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.CommentChar == '|', "CommentChar should be '|'");
		}

		[Test]
		public void TimeLimit()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.TimeLimit == 0, "TimeLimit should default to 0");
		}

		[Test]
		public void TopDownFilter()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.TopDownFilter == 1, "TopDownFilter should default to true");
		}

		[Test]
		public void TrimEmptyFeatures()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.TrimEmptyFeatures != 0, "TrimEmptyFeatures should default to true");
		}

		[Test]
		public void DebuggingLevel()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.DebuggingLevel == 0, "DebuggingLevel should default to 0");
		}

		[Test]
		public void LexRecordMarker()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.LexRecordMarker.TrimEnd('\0'), @"\w", "LexRecordMarker isn't as expected");
		}

		[Test]
		public void LexWordMarker()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.LexWordMarker.TrimEnd('\0'), @"\w", "LexWordMarker isn't as expected");
		}

		[Test]
		public void LexCategoryMarker()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.LexCategoryMarker.TrimEnd('\0'), @"\c", "LexCategoryMarker isn't as expected");
		}

		[Test]
		public void LexFeaturesMarker()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.LexFeaturesMarker.TrimEnd('\0'), @"\f", "LexFeaturesMarker isn't as expected");
		}

		[Test]
		public void LexGlossMarker()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.LexGlossMarker.TrimEnd('\0'), @"\g", "LexGlossMarker isn't as expected");
		}

		[Test]
		public void LexRootGlossMarker()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreEqual(parser.LexRootGlossMarker.TrimEnd('\0'), @"\r", "LexRootGlossMarker isn't as expected");
		}

		[Test]
		public void TopFeatureOnly()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.TopFeatureOnly != 0, "TopFeatureOnly should default to true");
		}

		[Test]
		public void DisplayFeatures()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.DisplayFeatures != 0, "DisplayFeatures should default to true");
		}

		[Test]
		public void FlatFeatureDisplay()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.FlatFeatureDisplay == 0, "FlatFeatureDisplay should default to false");
		}

		[Test]
		public void Failures()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.Failures == 0, "Failures should default to false");
		}

		[Test]
		public void CodePage()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.CodePage == Encoding.UTF8.CodePage, "Encoding.UTF8.CodePage should be UTF8");
		}

		[Test]
		public void DisambiguateAnaFile()
		{
			const string anafile = LocationToSrcDirFromOutputDebug + "Ephtst.ana";
			string outputFile = Path.GetTempFileName();
			try
			{
				using (var parser = CreateAndSetupTestParser())
				{
					parser.DisambiguateAnaFile(anafile, outputFile);
					var f = new FileInfo(outputFile);
					Assert.Greater(f.Length, 0, "File length should be greater than 0 bytes");
				}
			}
			finally
			{
				File.Delete(outputFile);
			}
		}

		[Test]
		public void WriteAmpleParses()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.WriteAmpleParses != 0, "WriteAmpleParses should default to true");
		}

		[Test]
		public void LoadAnaFile()
		{
			const string anafile = LocationToSrcDirFromOutputDebug + "bears.ana";
			string outputFile = Path.GetTempFileName();
			try
			{
				using (var parser = CreateAndSetupTestParser())
				{
					parser.DisambiguateAnaFile(anafile, outputFile);
					parser.LoadAnaFile(anafile, 0);
				}
			}
			finally
			{
				File.Delete(outputFile);
			}
		}

		[Test]
		public void ReloadLexicon()
		{
			using (var parser = CreateAndSetupTestParser())
			{
				Assert.IsTrue(parser.LexiconFileCount == 1, "LexiconFileCount should be 1");
				parser.ReloadLexicon();
				Assert.IsTrue(parser.LexiconFileCount == 1, "LexiconFileCount should be 1 after reloading");
			}
		}

		[Test]
		public void LexiconFileCount()
		{
			using (var parser = CreateAndSetupTestParser())
			{
				Assert.IsTrue(parser.LexiconFileCount == 1, "LexiconFileCount should be 1");
				parser.Clear();
				Assert.IsTrue(parser.LexiconFileCount == 0, "LexiconFileCount should be 0 after clear");
			}
		}

		[Test]
		public void PromoteDefaultAtoms()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.AreNotEqual(0, parser.PromoteDefaultAtoms, "PromoteDefaultAtoms should default to true");
		}

		[Test]
		public void SentenceFinalPunctuation()
		{
			using (var parser = CreateAndSetupTestParser())
			{
				Assert.AreEqual(parser.SentenceFinalPunctuation.TrimEnd('\0'), @". ? ! : ;", "SentenceFinalPunctuation isn't as expected");

				// this adds to SentenceFinalPunctuation
				parser.SentenceFinalPunctuation = "a b";

				Assert.AreEqual(parser.SentenceFinalPunctuation.TrimEnd('\0'), @". ? ! : ; a b", "SentenceFinalPunctuation isn't as expected after modification");
			}
		}

		[Test]
		public void AmplePropertyIsFeature()
		{
			using (var parser = CreateAndSetupTestParser())
				Assert.IsTrue(parser.AmplePropertyIsFeature == 0, "AmplePropertyIsFeature should default to false");
		}
	}
}
