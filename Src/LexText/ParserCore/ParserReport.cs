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
		// Name of the project
		public string ProjectName { get; set; }

		// Name of the machine that ran the parser
		// (This is relevant for ParseTime.)
		public string MachineName { get; set; }

		// Either "Testbed", "All", or the name of the text parsed
		public string SourceText { get; set; }

		// Timestamp of when CheckParser was called
		public long Timestamp { get; set; }

		// Number of words parsed
		public int NumWords { get; set; }

		// Number of words that get a parse error
		public int NumParseErrors { get; set; }

		// Number of words that get zero parses but no error
		public int NumZeroParses { get; set; }

		// Total time to parse all the words
		public long TotalParseTime { get; set; }

		// Total number of parse analyses
		public int TotalAnalyses { get; set; }

		// Total number of analyses that were marked approved by the user that did not get a parse
		public int TotalUserApprovedAnalysesMissing { get; set; }

		// Total number of parse analyses that were marked as disapproved by the user
		public int TotalUserDisapprovedAnalyses { get; set; }

		// Total number of parse analyses that were marked as noOpinion by the user
		public int TotalUserNoOpinionAnalyses { get; set; }

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
			TotalAnalyses += report.NumAnalyses;
			TotalUserApprovedAnalysesMissing += report.NumUserApprovedAnalysesMissing;
			TotalUserDisapprovedAnalyses += report.NumUserDisapprovedAnalyses;
			TotalUserNoOpinionAnalyses += report.NumUserNoOpinionAnalyses;
			if (report.ErrorMessage != null)
				NumParseErrors += 1;
			if (report.NumAnalyses == 0)
				NumZeroParses += 1;
		}

		/// <summary>
		/// Return the differences between the current reports and the old reports.
		/// </summary>
		/// <param name="oldReports"></param>
		/// <returns></returns>
		public IDictionary<string, ParseReport> DiffParseReports (IDictionary<string, ParseReport> oldReports)
		{
			IDictionary<string, ParseReport> newParseReports = new Dictionary<string, ParseReport>();
			ParseReport missingReport = new ParseReport();
			missingReport.ErrorMessage = "missing";

			foreach (string key in oldReports.Keys)
			{
				ParseReport oldReport = oldReports[key];
				ParseReport newReport = ParseReports.ContainsKey(key) ? ParseReports[key] : missingReport;
				newParseReports[key] = newReport.DiffParseReport(oldReport);
			}
			foreach (string key in ParseReports.Keys)
			{
				if (!oldReports.ContainsKey(key))
				{
					ParseReport newReport = ParseReports[key];
					newParseReports[key] = newReport.DiffParseReport(missingReport);
				}
			}

			return newParseReports;
		}
	}

	/// <summary>
	/// ParseReport reports the results of parsing a word.
	/// </summary>
	public class ParseReport
	{
		// Time to parse the word
		public long ParseTime { get; set; }

		// Error message from the parser
		public string ErrorMessage { get; set; }

		// Number of parse analyses
		public int NumAnalyses { get; set; }

		// Number of analyses that were marked approved by the user that did not get a parse
		public int NumUserApprovedAnalysesMissing { get; set; }

		// Number of parse analyses that were marked as disapproved by the user
		public int NumUserDisapprovedAnalyses {  get; set; }

		// Number of parse analyses that were marked as noOpinion by the user
		public int NumUserNoOpinionAnalyses { get; set; }

		public ParseReport() { }

		public ParseReport(IWfiWordform wordform, ParseResult result)
		{
			ParseTime = result.ParseTime;
			ErrorMessage = result.ErrorMessage;
			NumAnalyses = result.Analyses.Count();
			if (wordform == null)
				return;
			// Look for conflicts between user opinion and parser.
			var userAgent = wordform.Cache.LanguageProject.DefaultUserAgent;
			// Count missing user approved analyses.
			foreach (IWfiAnalysis wfAnalysis in wordform.AnalysesOC)
			{
				var opinion = wfAnalysis.GetAgentOpinion(userAgent);
				if (opinion == Opinions.approves)
				{
					var found = false;
					foreach (ParseAnalysis pAnalysis in result.Analyses)
						if (pAnalysis.MatchesIWfiAnalysis(wfAnalysis))
							found = true;
					if (!found)
						NumUserApprovedAnalysesMissing++;

				}
			}
			// Count parse analyses that are disapproved.
			// Count parse analyses that have no opinion.
			foreach (ParseAnalysis pAnalysis in result.Analyses)
			{
				Opinions pOpinion = Opinions.noopinion;
				foreach (IWfiAnalysis wfAnalysis in wordform.AnalysesOC)
					if (pAnalysis.MatchesIWfiAnalysis(wfAnalysis))
					{
						var wfOpinion = wfAnalysis.GetAgentOpinion(userAgent);
						if (wfOpinion == Opinions.disapproves)
							pOpinion = Opinions.disapproves;
						else if (wfOpinion == Opinions.approves && pOpinion != Opinions.disapproves)
							pOpinion = Opinions.approves;
					}
				if (pOpinion == Opinions.disapproves)
					NumUserDisapprovedAnalyses++;
				if (pOpinion == Opinions.noopinion)
					NumUserNoOpinionAnalyses++;

			}

		}

		/// <summary>
		/// Return the diff between the current report and the old report.
		/// </summary>
		/// <param name="oldReport"></param>
		/// <returns></returns>
		public ParseReport DiffParseReport(ParseReport oldReport)
		{
			ParseReport diffReport = new ParseReport
			{
				NumAnalyses = NumAnalyses - oldReport.NumAnalyses,
				NumUserApprovedAnalysesMissing = NumUserApprovedAnalysesMissing - oldReport.NumUserApprovedAnalysesMissing,
				NumUserDisapprovedAnalyses = NumUserDisapprovedAnalyses - oldReport.NumUserDisapprovedAnalyses,
				NumUserNoOpinionAnalyses = NumUserNoOpinionAnalyses - oldReport.NumUserNoOpinionAnalyses,
				ParseTime = ParseTime - oldReport.ParseTime
			};
			if (ErrorMessage != oldReport.ErrorMessage)
			{
				string oldError = oldReport.ErrorMessage ?? string.Empty;
				string newError = ErrorMessage ?? string.Empty;
				diffReport.ErrorMessage = oldError + " => " + newError;
			}
			return diffReport;
		}
	}
}
