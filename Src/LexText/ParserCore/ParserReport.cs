using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// ParserReport reports the results of Check Parser.
	/// </summary>
	public class ParserReport
	{
		// Name of the project.
		public string ProjectName { get; set; }

		// Name of the machine that ran the parser
		// This is relevant for ParseTime.
		public string MachineName { get; set; }

		// Either "Testbed", "All", or the name of the text parsed
		public string SourceText { get; set; }

		public long Timestamp { get; set; }

		// Number of words parsed
		public int NumWords { get; set; }

		// Number of words that get a parse error
		public int NumParseErrors { get; set; }

		// Number of words that get zero parses but no error
		public int NumZeroParses { get; set; }

		public long TotalParseTime { get; set; }

		// Total number of analyses
		public int NumAnalyses { get; set; }

		// Number of HumanApproved analyses that did not get a parse
		public int NumHumanApprovedAnalysesMissing { get; set; }

		// Number of HumanDisapproved analyses that got a parse
		public int NumHumanDisapprovedAnalyses {  get; set; }

		// Parse reports for each word
		public IDictionary<string, ParseReport> ParseReports { get; set; }

		public ParserReport()
		{
			ParseReports = new Dictionary<string, ParseReport>();
		}

		/// <summary>
		/// Adds parse report to ParseReports and update statistics.
		/// </summary>
		/// <param name="word"></param>
		/// <param name="report"></param>
		public void AddParseReport(string word, ParseReport report)
		{
			ParseReports[word] = report;
			NumWords += 1;
			TotalParseTime += report.ParseTime;
			NumAnalyses += report.NumAnalyses;
			NumHumanApprovedAnalysesMissing += report.NumHumanApprovedAnalysesMissing;
			NumHumanDisapprovedAnalyses += report.NumHumanDisapprovedAnalyses;
			if (report.ErrorMessage != null)
				NumParseErrors += 1;
			else if (report.NumAnalyses == 0)
				NumZeroParses += 1;
		}

	}

	public class ParseReport
	{
		public long ParseTime { get; set; }

		public string ErrorMessage { get; set; }

		public int NumAnalyses { get; set; }

		public int NumHumanApprovedAnalysesMissing { get; set; }

		public int NumHumanDisapprovedAnalyses {  get; set; }


		public ParseReport() { }

		public ParseReport(IWfiWordform wordform, ParseResult result)
		{
			ParseTime = result.ParseTime;
			ErrorMessage = result.ErrorMessage;
			NumAnalyses = result.Analyses.Count();
			if (NumAnalyses == 0 || wordform == null)
			{
				// Don't count conflicts if there are zero parses.
				return;
			}
			// Look for conflicts between human opinion and parser.
			var humanAgent = wordform.Cache.LanguageProject.DefaultUserAgent;
			foreach (IWfiAnalysis wfAnalysis in wordform.AnalysesOC)
			{
				var opinion = wfAnalysis.GetAgentOpinion(humanAgent);
				if (opinion != Opinions.noopinion)
				{
					// Look for matching analysis.
					var found = false;
					foreach (ParseAnalysis pAnalysis in result.Analyses)
						if (pAnalysis.MatchesIWfiAnalysis(wfAnalysis))
							found = true;
					if (opinion == Opinions.approves && !found)
						NumHumanApprovedAnalysesMissing++;
					if (opinion == Opinions.disapproves && found)
						NumHumanDisapprovedAnalyses++;
				}
			}
		}

	}
}
