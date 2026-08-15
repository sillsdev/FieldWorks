// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using SIL.LCModel;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>Word/analysis counts for one text, for the AI-export text picker.</summary>
	public struct WordAnalysisCounts
	{
		public WordAnalysisCounts(int words, int analyses)
		{
			Words = words;
			Analyses = analyses;
		}

		/// <summary>Every word-token occurrence, whether analyzed or not.</summary>
		public int Words { get; }

		/// <summary>Word-token occurrences that have an IWfiAnalysis/IWfiGloss attached.</summary>
		public int Analyses { get; }
	}

	/// <summary>
	/// Helpers shared by the grammar+texts-for-AI export: counting words/analyses per text
	/// for the picker dialog, deriving a display name for a text, and sanitizing text titles
	/// into safe, unique file names.
	/// </summary>
	public static class GrammarTextsAIExportHelpers
	{
		/// <summary>
		/// Counts word-token occurrences (Words) and the subset of those that have a real
		/// IWfiAnalysis/IWfiGloss attached (Analyses), across every paragraph of stText.
		/// </summary>
		public static WordAnalysisCounts CountWordsAndAnalyses(IStText stText)
		{
			var words = 0;
			var analyses = 0;
			for (var i = 0; i < stText.ParagraphsOS.Count; ++i)
			{
				var para = (IStTxtPara)stText.ParagraphsOS[i];
				foreach (var analysis in para.Analyses)
				{
					if (!analysis.HasWordform)
						continue;
					words++;
					if (!(analysis is IWfiWordform))
						analyses++;
				}
			}
			return new WordAnalysisCounts(words, analyses);
		}

		/// <summary>
		/// The display name for a text: the owning IText's Name if there is one (the normal
		/// case for interlinear texts), otherwise the IStText's own short name (covers
		/// Scripture sections, which are not owned by an IText).
		/// </summary>
		public static string GetTextDisplayName(IStText stText)
		{
			if (stText.Owner is IText text)
				return text.Name.BestAnalysisVernacularAlternative.Text;
			return stText.ShortNameTSS.Text;
		}

		private static readonly char[] InvalidFileNameChars = Path.GetInvalidFileNameChars();

		/// <summary>
		/// Replaces characters that are invalid in a file name with '_', then appends
		/// " (2)", " (3)", etc. if the result collides (case-insensitively) with a name
		/// already in usedNames. Adds the returned name to usedNames before returning it.
		/// </summary>
		public static string MakeSafeFileName(string rawName, HashSet<string> usedNames)
		{
			var sanitized = new StringBuilder(rawName.Length);
			foreach (var ch in rawName)
				sanitized.Append(InvalidFileNameChars.Contains(ch) ? '_' : ch);
			var baseName = sanitized.ToString();

			var candidate = baseName;
			var suffix = 2;
			while (usedNames.Contains(candidate))
			{
				candidate = $"{baseName} ({suffix})";
				suffix++;
			}
			usedNames.Add(candidate);
			return candidate;
		}
	}
}
