// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using SIL.DisambiguateInFLExDB;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using XAmpleWithToneParse;

namespace SIL.ToneParsFLEx
{
	public class XAmpleMorpherAnaProducer : MorpherAnaProducer
	{
		string InputFilePath { get; set; }
		public string DatabaseName { get; set; }
		protected const string kAdCtl = "adctl.txt";
		protected const String kLexicon = "lex.txt";

		public XAmpleMorpherAnaProducer(bool useUniqueWordForms, LcmCache cache, string intxCtlFile)
		{
			UseUniqueWordForms = useUniqueWordForms;
			Cache = cache;
			IntxCtlFile = intxCtlFile;
			InputFilePath = Path.Combine(Path.GetTempPath(), "ToneParsInvoker.txt");
			DatabaseName = ConvertNameToUseAnsiCharacters(cache.ProjectId.Name);
		}

		public override void ProduceANA(SegmentToShow segmentToShow)
		{
			CreateInputFile(segmentToShow);
			XAmpleParseFile();
		}

		public override void ProduceANA(IText selectedTextToShow)
		{
			CreateInputFile(selectedTextToShow);
			XAmpleParseFile();
		}

		private void CreateInputFile(SegmentToShow selectedSegmentToShow)
		{
			string textToUse;
			if (UseUniqueWordForms)
			{
				TextPreparer preparer = TextPreparer.Instance;
				textToUse = preparer.GetUniqueWordForms(selectedSegmentToShow.Segment);
			}
			else
			{
				textToUse = selectedSegmentToShow.Baseline;
			}
			File.WriteAllText(InputFilePath, textToUse);
		}

		private void XAmpleParseFile()
		{
			AppendToneParsPropertiesToAdCtlFile();
			AddToneParsPropertiesToLexiconFile();

			string cdTableDir = Path.Combine(
				FwDirectoryFinder.CodeDirectory,
				FwDirectoryFinder.ksFlexFolderName,
				"Configuration",
				"Grammar"
			);
			XAmpleWrapperForTonePars m_xampleTP = new XAmpleWrapperForTonePars();
			m_xampleTP.InitForTonePars();
			int maxToReturn = GetMaxAnalysesToReturn();
			m_xampleTP.LoadFilesForTonePars(
				cdTableDir,
				Path.GetTempPath(),
				DatabaseName,
				IntxCtlFile,
				maxToReturn
			);
			var results = m_xampleTP.ParseFileForTonePars(InputFilePath, AnaFilePath);
		}

		private void AppendToneParsPropertiesToAdCtlFile()
		{
			// Append all TonePars properties in FLEx DB as allomorph properties to the AD Ctl file
			String xAmpleAdCtlFile = Path.GetTempPath() + DatabaseName + kAdCtl;
			String xAmpleAdCtl = File.ReadAllText(xAmpleAdCtlFile);
			var props = GetAllToneParsPropsFromPossibilityList();
			String toneParsAdCtlFile = Path.GetTempPath() + DatabaseName + ToneParsInvoker.kTPAdCtl;
			File.WriteAllText(toneParsAdCtlFile, xAmpleAdCtl + props);
		}

		private string GetAllToneParsPropsFromPossibilityList()
		{
			var possListRepository =
				Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var toneParsList = possListRepository
				.AllInstances()
				.FirstOrDefault(
					list =>
						list.Name.BestAnalysisAlternative.Text
						== ToneParsConstants.ToneParsPropertiesList
				);
			var sb = new StringBuilder();
			foreach (var prop in toneParsList.PossibilitiesOS)
			{
				sb.Append("\\ap ");
				sb.Append(prop.Name.AnalysisDefaultWritingSystem.Text);
				sb.Append("\n");
			}
			return sb.ToString();
		}

		private void AddToneParsPropertiesToLexiconFile()
		{
			String xAmpleLexiconFile = Path.GetTempPath() + DatabaseName + kLexicon;
			String xAmpleLexicon = File.ReadAllText(xAmpleLexiconFile);
			var allomorphHvoPropertyMapper = new Dictionary<string, string> { };
			var morphemePropertyMapper = new Dictionary<string, string> { };
			var possListRepository =
				Cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>();
			var toneParsList = possListRepository
				.AllInstances()
				.FirstOrDefault(
					list =>
						list.Name.BestAnalysisAlternative.Text
						== ToneParsConstants.ToneParsPropertiesList
				);
			BuildAllomorphPropertyMapper(allomorphHvoPropertyMapper, toneParsList);
			BuildMorphemePropertyMapper(morphemePropertyMapper, toneParsList);
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

			String toneParsLexiconFile =
				Path.GetTempPath() + DatabaseName + ToneParsInvoker.kTPLexicon;
			File.WriteAllText(toneParsLexiconFile, lexWithAlloAndMorphProps);
		}

		private static void BuildAllomorphPropertyMapper(
			Dictionary<string, string> allomorphHvoPropertyMapper,
			ICmPossibilityList toneParsList
		)
		{
			foreach (var prop in toneParsList.PossibilitiesOS)
			{
				var refObjs = prop.ReferringObjects.Select(o => o).Where(o => !(o is ILexSense));
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
			ICmPossibilityList toneParsList
		)
		{
			foreach (var prop in toneParsList.PossibilitiesOS)
			{
				var refObjs = prop.ReferringObjects.Select(o => o).Where(o => o is ILexSense);
				foreach (ICmObject obj in refObjs)
				{
					var sense = obj as ILexSense;
					var sHvo = sense.MorphoSyntaxAnalysisRA.Hvo.ToString();
					if (!morphemePropertyMapper.ContainsKey(sHvo))
					{
						var hvoMatch = "\\lx " + sHvo + "\r\n";
						var replaceWith =
							hvoMatch
							+ "\\mp "
							+ prop.Name.AnalysisDefaultWritingSystem.Text
							+ "\r\n";
						morphemePropertyMapper.Add(hvoMatch, replaceWith);
					}
				}
			}
		}

		private int GetMaxAnalysesToReturn()
		{
			string parameters = Cache.LangProject.MorphologicalDataOA.ParserParameters;
			XmlDocument doc = new XmlDocument();
			doc.LoadXml(parameters);
			XmlNodeList elems = doc.GetElementsByTagName("MaxAnalysesToReturn");
			if (elems != null)
			{
				int max = Int32.Parse(elems.Item(0).InnerText);
				if (max < 0)
				{
					max = 1000;
				}
				return max;
			}
			return 100;
		}

		private void CreateInputFile(IText selectedTextToShow)
		{
			string textToUse;
			if (UseUniqueWordForms)
			{
				TextPreparer preparer = TextPreparer.Instance;
				textToUse = preparer.GetUniqueWordForms(selectedTextToShow);
			}
			else
			{
				textToUse = GetTextBaselines(selectedTextToShow);
			}
			File.WriteAllText(InputFilePath, textToUse);
		}

		private string GetTextBaselines(IText selectedTextToShow)
		{
			var sb = new StringBuilder();
			var contents = selectedTextToShow.ContentsOA;
			IList<IStPara> paragraphs = contents.ParagraphsOS;
			foreach (IStPara para in paragraphs)
			{
				var paraUse = para as IStTxtPara;
				if (paraUse != null)
				{
					foreach (var segment in paraUse.SegmentsOS)
					{
						sb.Append(segment.BaselineText);
						sb.Append("\n");
					}
				}
			}
			return sb.ToString();
		}
	}
}
