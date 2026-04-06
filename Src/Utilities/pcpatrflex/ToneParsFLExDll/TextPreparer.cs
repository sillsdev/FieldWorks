// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.ToneParsFLEx
{
	public sealed class TextPreparer
	{
		private static readonly TextPreparer instance = new TextPreparer();
		private HashSet<string> wordSet = new HashSet<string>();

		static TextPreparer() { }

		private TextPreparer() { }

		public static TextPreparer Instance
		{
			get { return instance; }
		}

		public string GetUniqueWordForms(ISegment segment)
		{
			wordSet.Clear();
			AddWordsInSegment(segment);
			return FormatWordForms();
		}

		public string GetUniqueWordForms(IText text)
		{
			wordSet.Clear();
			if (text == null || text.ContentsOA == null)
				return "";
			var istText = text.ContentsOA as IStText;
			for (int i = 0; i < istText.ParagraphsOS.Count; i++)
			{
				var para = istText.ParagraphsOS.ElementAtOrDefault(i) as IStTxtPara;
				foreach (ISegment segment in para.SegmentsOS)
				{
					AddWordsInSegment(segment);
				}
			}
			return FormatWordForms();
		}

		private void AddWordsInSegment(ISegment segment)
		{
			if (segment == null)
				return;
			foreach (IAnalysis analysis in segment.AnalysesRS)
			{
				if (analysis.ClassName != "PunctuationForm")
				{
					string wf = analysis.Wordform.Form.BestVernacularAnalysisAlternative.Text;
					wordSet.Add(wf);
				}
			}
		}

		private string FormatWordForms()
		{
			StringBuilder sb = new StringBuilder();
			foreach (string s in wordSet.ToList())
			{
				sb.Append(s);
				sb.Append(".\n");
			}
			return sb.ToString();
		}
	}
}
