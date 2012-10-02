using System;
using System.Collections.Generic;
using System.Linq;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.SharpViews.Selections;

namespace SIL.FieldWorks.SharpViews
{
	public class MultiLineInsertData
	{
		public List<string> InsertedStringLines { get; private set; } // excluding the ones that get merged into the existing paragraph(s)
		public List<ITsString> InsertedTsStrLines { get; private set; }
		public List<IStyle> ParaStyles { get; private set; }
		public string StringAppendToFirstPara { get; private set; }
		public string StringPrependToLastPara { get; private set; }
		public ITsString TsStrAppendToFirstPara { get; private set; }
		public ITsString TsStrPrependToLastPara { get; private set; }
		public Selection Selection;

		/// <summary>
		/// Offset is the index of \r or \n in input.
		/// Return index + 1 unless we find a \r\n sequence exactly at index.
		/// </summary>
		static int AdvancePastLineBreak(int offset, string input)
		{
			if (input.Length > offset + 3 && input.Substring(offset, 4) == @"\par")
				return offset + 4;
			if (input[offset] == '\r' && input.Length > offset + 1 && input[offset + 1] == '\n')
				return offset + 2;
			return offset + 1;
		}

		/// <summary>
		/// Offset is the index of the character after \r or \n in input.
		/// Return index -1 unless we find a \r\n sequence ending exactly at the character before index.
		/// </summary>
		static int BackPastLineBreak(int offset, string input)
		{
			if (offset >= 4 && input.Substring(offset - 4, 4) == @"\par")
				return offset - 4;
			if (offset >= 2 && input[offset - 1] == '\n' && input[offset - 2] == '\r')
				return offset - 2;
			return offset - 1;
		}

		public MultiLineInsertData(Selection whereToInsert, string stringToInsert, List<IStyle> styles)
		{
			if (stringToInsert == "" || (stringToInsert.IndexOfAny(new[] { '\r', '\n' }) == -1 && !stringToInsert.Contains(@"\par")))
				return;
			ParaStyles = styles;
			Selection = whereToInsert;
			int startOfUnprocessedLines = stringToInsert.IndexOfAny(new[] { '\r', '\n' });
			int limOfUnprocessedLines = stringToInsert.LastIndexOfAny(new[] { '\r', '\n' }) + 1;
			int otherStartOfUnprocessedLines = stringToInsert.IndexOf(@"\par");
			int otherLimOfUnprocessedLines = stringToInsert.LastIndexOf(@"\par") + 4;
			startOfUnprocessedLines =
				Math.Min(startOfUnprocessedLines != -1 ? startOfUnprocessedLines : otherStartOfUnprocessedLines,
						 otherStartOfUnprocessedLines != -1 ? otherStartOfUnprocessedLines : startOfUnprocessedLines);
			limOfUnprocessedLines = Math.Max(limOfUnprocessedLines, otherLimOfUnprocessedLines);
			StringAppendToFirstPara = stringToInsert.Substring(0, startOfUnprocessedLines);
			StringPrependToLastPara = stringToInsert.Substring(limOfUnprocessedLines);
			if (StringAppendToFirstPara == "\r" || StringAppendToFirstPara == "\n" || StringAppendToFirstPara == @"\par")
				StringAppendToFirstPara = "";
			if (StringPrependToLastPara == "\r" || StringPrependToLastPara == "\n" || StringPrependToLastPara == @"\par")
				StringPrependToLastPara = "";
			limOfUnprocessedLines = BackPastLineBreak(limOfUnprocessedLines, stringToInsert);
			InsertedStringLines = new List<string>();
			while (true)
			{
				startOfUnprocessedLines = AdvancePastLineBreak(startOfUnprocessedLines, stringToInsert);
				if (startOfUnprocessedLines > limOfUnprocessedLines)
					break;
				int index = stringToInsert.IndexOfAny(new[] { '\r', '\n' }, startOfUnprocessedLines);
				if(index == -1)
					index = stringToInsert.IndexOf(@"\par", startOfUnprocessedLines);
				string nextString = stringToInsert.Substring(startOfUnprocessedLines, index - startOfUnprocessedLines);
				nextString = nextString.Trim(new[] { '\r', '\n' });
				nextString = nextString.Replace(@"\par", "");
				InsertedStringLines.Add(nextString);
				startOfUnprocessedLines = index;
			}
		}

		public MultiLineInsertData(Selection whereToInsert, List<ITsString> stringToInsert, List<IStyle> styles)
		{
			if (stringToInsert == null || stringToInsert.Count <= 0)
				return;
			ParaStyles = styles;
			Selection = whereToInsert;
			if(stringToInsert.Count == 1)
			{
				ITsString tsString = stringToInsert[0];
				int charIndexOfNextRun;
				int startOfUnprocessedLines = tsString.Text.IndexOfAny(new[] { '\r', '\n' });
				int limOfUnprocessedLines = tsString.Text.LastIndexOfAny(new[] { '\r', '\n' }) + 1;
				int otherStartOfUnprocessedLines = tsString.Text.IndexOf(@"\par");
				int otherLimOfUnprocessedLines = tsString.Text.LastIndexOf(@"\par")+1;
				startOfUnprocessedLines =
					Math.Min(startOfUnprocessedLines != -1 ? startOfUnprocessedLines : otherStartOfUnprocessedLines,
							 otherStartOfUnprocessedLines != -1 ? otherStartOfUnprocessedLines : startOfUnprocessedLines);
				limOfUnprocessedLines = Math.Max(limOfUnprocessedLines, otherLimOfUnprocessedLines);
				TsStrAppendToFirstPara = tsString.Substring(0, startOfUnprocessedLines);
				if (limOfUnprocessedLines == tsString.Length)
					TsStrPrependToLastPara =
						TsStrFactoryClass.Create().EmptyString(
							tsString.Runs().Reverse().Where(run => run.Props.GetWs() > 0).First().Props.GetWs());
				else
				{
					TsStrPrependToLastPara = tsString.Substring(limOfUnprocessedLines);
					if (TsStrPrependToLastPara.Text == "\r" || TsStrPrependToLastPara.Text == "\n" ||
						TsStrPrependToLastPara.Text == @"\par")
					{
						var bldr = TsStrPrependToLastPara.GetBldr();
						bldr.Replace(0, TsStrPrependToLastPara.Text.Length, "", null);
						TsStrPrependToLastPara = bldr.GetString();
					}
				}
				if (TsStrAppendToFirstPara.Text == "\r" || TsStrAppendToFirstPara.Text == "\n" || TsStrAppendToFirstPara.Text == @"\par")
				{
					var bldr = TsStrAppendToFirstPara.GetBldr();
					bldr.Replace(0, TsStrAppendToFirstPara.Text.Length, "", null);
					TsStrAppendToFirstPara = bldr.GetString();
				}
				InsertedTsStrLines = new List<ITsString>();
				while (true)
				{
					startOfUnprocessedLines = AdvancePastLineBreak(startOfUnprocessedLines, tsString.Text);
					if (startOfUnprocessedLines >= limOfUnprocessedLines)
						break;
					int limIndex = tsString.Text.IndexOfAny(new[] { '\r', '\n' }, startOfUnprocessedLines);
					int	otherIndex = tsString.Text.IndexOf(@"\par", startOfUnprocessedLines);
					limIndex = Math.Min(limIndex != -1 ? limIndex : otherIndex, otherIndex != -1 ? otherIndex : limIndex);
					string nextString = tsString.Text.Substring(startOfUnprocessedLines, limIndex - startOfUnprocessedLines);
					nextString = nextString.Trim(new[] { '\r', '\n' });
					nextString = nextString.Replace(@"\par", "");
					var bldr =
						TsStrFactoryClass.Create().MakeString(nextString, tsString.get_WritingSystemAt(startOfUnprocessedLines)).GetBldr();
					for (int runIndex = tsString.get_RunAt(startOfUnprocessedLines); runIndex < tsString.get_RunAt(limIndex); runIndex++)
					{
						int start = tsString.get_MinOfRun(runIndex) - startOfUnprocessedLines;
						int end = tsString.get_LimOfRun(runIndex) - startOfUnprocessedLines;
						bldr.Replace(start, end, bldr.Text.Substring(start, end - start), tsString.get_Properties(runIndex));
					}
					InsertedTsStrLines.Add(bldr.GetString());
					startOfUnprocessedLines = limIndex;
				}
			}
			else
			{
				TsStrAppendToFirstPara = stringToInsert.First();
				TsStrPrependToLastPara = stringToInsert.Last();
				stringToInsert.Remove(stringToInsert.First());
				stringToInsert.Remove(stringToInsert.Last());
				InsertedTsStrLines = stringToInsert;
			}
		}
	}
}
