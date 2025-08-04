// Copyright (c) 2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using System.Xml.XPath;

namespace SIL.FieldWorks.WordWorks.Parser
{
	// Class to augment the XAmple data files with custom XAmple properties
	public sealed class XAmplePropertiesXAmpleDataFilesAugmenter
	{
		private static readonly XAmplePropertiesXAmpleDataFilesAugmenter instance = new XAmplePropertiesXAmpleDataFilesAugmenter();
		private LcmCache Cache { get; set; }
		private XElement Root { get; set; }
		public string DatabaseName { get; set; }
		private const string kAdCtl = "adctl.txt";
		private const string kLexicon = "lex.txt";
		private const string kGrammar = "gram.txt";
		private const string kTempAdCtl = "Tempadctl.txt";
		private const string kTempLexicon = "Templex.txt";
		private const string kTempGrammar = "Tempgram.txt";
		private string customListName = "";

		// Explicit static constructor to tell C# compiler
		// not to mark type as beforefieldinit
		static XAmplePropertiesXAmpleDataFilesAugmenter()
		{
		}

		private XAmplePropertiesXAmpleDataFilesAugmenter()
		{
		}

		public static XAmplePropertiesXAmpleDataFilesAugmenter Instance
		{
			get
			{
				return instance;
			}
		}

		public void Dispose()
		{
			throw new System.NotImplementedException();
		}
		public void Process(LcmCache cache, string database, XElement root)
		{
			Cache = cache;
			if (cache == null)
				return;
			if (root == null)
				return;
			Root = root;
			var item = Root.XPathSelectElement("CustomList/Name");
			if (item == null)
			{
				return;
			}
			customListName = item.Value;
			DatabaseName = database;
			AppendXAmplePropertiesToAdCtlFile();
			AppendXAmplePropertiesToGrammarFile();
			AddXAmplePropertiesToLexiconFile();

		}
		private void AppendXAmplePropertiesToAdCtlFile()
		{
			// Append all XAmple properties in FLEx DB as allomorph properties to the AD Ctl file
			string xAmpleAdCtlFile = Path.GetTempPath() + DatabaseName + kAdCtl;
			string xAmpleAdCtl = File.ReadAllText(xAmpleAdCtlFile);
			var props = GetAllXAmplePropsFromPossibilityList();
			var tests = GetUserTests();
			//string tempAdCtlFile = Path.GetTempPath() + DatabaseName + kQAdCtl;
			//File.WriteAllText(tempAdCtlFile, xAmpleAdCtl + props + tests);
			//File.Copy(tempAdCtlFile, xAmpleAdCtlFile, true);
			CreateXAmpleTempFileAndCopyBack(xAmpleAdCtlFile, kTempAdCtl, xAmpleAdCtl + props + tests);
		}

		private void AppendXAmplePropertiesToGrammarFile()
		{
			// Append all XAmple properties in FLEx DB as Let statements to the grammar file
			string xAmpleGrammarFile = Path.GetTempPath() + DatabaseName + kGrammar;
			string xAmpleGrammar = File.ReadAllText(xAmpleGrammarFile);
			var props = GetAllXAmpleLetStatementsFromPossibilityList();
			//string tempGrammarFile = Path.GetTempPath() + DatabaseName + kQGrammar;
			//File.WriteAllText(tempGrammarFile, xAmpleGrammar + props);
			//File.Copy(tempGrammarFile, xAmpleGrammarFile, true);
			CreateXAmpleTempFileAndCopyBack(xAmpleGrammarFile, kTempGrammar, xAmpleGrammar + props);
		}

		private string GetUserTests()
		{
			string result = "";
			var userTests = Root.XPathSelectElement("UserTests");
			if (userTests != null)
			{
				result = userTests.Value;
			}
			return result;
		}

		private string GetAllXAmplePropsFromPossibilityList()
		{
			return GetXAmpleDataFromPossibilityList("\\ap ", "\n");
		}
		private string GetAllXAmpleLetStatementsFromPossibilityList()
		{
			return GetXAmpleDataFromPossibilityList("Let ", " be\n");
		}
		private string GetXAmpleDataFromPossibilityList(string beforeProp, string afterProp)
		{
			var possListRepository = Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var customList = possListRepository
				.AllInstances()
				.FirstOrDefault(
					list =>
						list.Name.BestAnalysisAlternative.Text
						== customListName
				);
			var sb = new StringBuilder();
			foreach (var prop in customList.PossibilitiesOS)
			{
				sb.Append(beforeProp);
				sb.Append(prop.Name.AnalysisDefaultWritingSystem.Text);
				sb.Append(afterProp);
			}
			return sb.ToString();
		}

		private void AddXAmplePropertiesToLexiconFile()
		{
			string xAmpleLexiconFile = Path.GetTempPath() + DatabaseName + kLexicon;
			string xAmpleLexicon = File.ReadAllText(xAmpleLexiconFile);
			var allomorphHvoPropertyMapper = new Dictionary<string, string> { };
			var morphemePropertyMapper = new Dictionary<string, string> { };
			var possListRepository =
				Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var customList = possListRepository
				.AllInstances()
				.FirstOrDefault(
					list =>
						list.Name.BestAnalysisAlternative.Text
						== customListName
				);
			BuildAllomorphPropertyMapper(allomorphHvoPropertyMapper, customList);
			BuildMorphemePropertyMapper(morphemePropertyMapper, customList);
			// Add allomorph properties
			var lexWithAlloProps = allomorphHvoPropertyMapper.Aggregate(
				xAmpleLexicon,
				(current, replacement) => current.Replace(replacement.Key, replacement.Value)
			);
			// Add morpheme properties
			var lexWithAlloAndMorphProps = morphemePropertyMapper.Aggregate(
				lexWithAlloProps,
				(current, replacement) => current.Replace(replacement.Key, replacement.Value)
			);

			//string xampleLexiconFile =
			//	Path.GetTempPath() + DatabaseName + kQLexicon;
			//File.WriteAllText(xampleLexiconFile, lexWithAlloAndMorphProps);
			//File.Copy(xampleLexiconFile, xAmpleLexiconFile, true);
			CreateXAmpleTempFileAndCopyBack(xAmpleLexiconFile, kTempLexicon, lexWithAlloAndMorphProps);
		}

		private void CreateXAmpleTempFileAndCopyBack(string xAmpleFile, string tempExtension, string content)
		{
			string xAmpleTempFile =	Path.GetTempPath() + DatabaseName + tempExtension;
			File.WriteAllText(xAmpleTempFile, content);
			File.Copy(xAmpleTempFile, xAmpleFile, true);
		}

		private static void BuildAllomorphPropertyMapper(
			Dictionary<string, string> allomorphHvoPropertyMapper,
			ICmPossibilityList customList)
		{
			foreach (var prop in customList.PossibilitiesOS)
			{
				var refObjs = prop.ReferringObjects.Select(o => o).Where(o => !(o is ILexEntry));
				foreach (ICmObject obj in refObjs)
				{
					var sHvo = obj.Hvo.ToString();
					if (!allomorphHvoPropertyMapper.ContainsKey(sHvo))
					{
						var hvoMatch = " {" + sHvo + "}";
						var replaceWith =
							hvoMatch + " " + prop.Name.AnalysisDefaultWritingSystem.Text;
						allomorphHvoPropertyMapper.Add(hvoMatch, replaceWith);
					}
				}
			}
		}

		private static void BuildMorphemePropertyMapper(
			Dictionary<string, string> morphemePropertyMapper,
			ICmPossibilityList customList
		)
		{
			foreach (var prop in customList.PossibilitiesOS)
			{
				var refObjs = prop.ReferringObjects.Select(o => o).Where(o => o is ILexEntry);
				foreach (ICmObject obj in refObjs)
				{
					var entry = obj as ILexEntry;
					foreach (ILexSense sense in entry.SensesOS)
					{
						var sHvo = sense.MorphoSyntaxAnalysisRA.Hvo.ToString();
						var hvoMatch = "\\lx " + sHvo + "\r";
						if (!morphemePropertyMapper.ContainsKey(hvoMatch))
						{
							var replaceWith =
								hvoMatch
								+ "\n\\mp "
								+ prop.Name.AnalysisDefaultWritingSystem.Text
								+ "\r\n";
							morphemePropertyMapper.Add(hvoMatch, replaceWith);
						}
						else
						{
							var replaceWith = "";
							morphemePropertyMapper.TryGetValue(hvoMatch, out replaceWith);
							var addon = "\\mp " + prop.Name.AnalysisDefaultWritingSystem.Text + "\r\n";
							morphemePropertyMapper.Remove(hvoMatch);
							morphemePropertyMapper.Add(hvoMatch, replaceWith + addon);
						}
					}
				}
			}
		}
	}
}
