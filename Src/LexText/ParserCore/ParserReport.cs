using Newtonsoft.Json;
using SIL.LCModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Policy;

namespace SIL.FieldWorks.WordWorks.Parser
{
	/// <summary>
	/// ParserReport reports the results of Check Parser.
	/// </summary>
	public class ParserReport: IEquatable<ParserReport>
	{
		/// <summary>
		/// Name of the project
		/// </summary>
		public string ProjectName { get; set; }

		/// <summary>
		/// Name of the machine that ran the parser
		/// (This is relevant for parse times.)
		/// </summary>
		public string MachineName { get; set; }

		/// <summary>
		/// Either "Testbed Texts", "All Texts", or the name of the text parsed
		/// </summary>
		public string SourceText { get; set; }

		/// <summary>
		/// Timestamp of when CheckParser was called as a FileTime
		/// (Use FromFileTime to convert to DateTime.)
		/// </summary>
		public long Timestamp { get; set; }

		/// <summary>
		/// Number of words parsed
		/// </summary>
		public int NumWords { get; set; }

		/// <summary>
		/// Number of words that get a parse error
		/// </summary>
		public int NumParseErrors { get; set; }

		/// <summary>
		/// Number of words that get zero parses
		/// </summary>
		public int NumZeroParses { get; set; }

		/// <summary>
		/// Total time to parse all the words in milliseconds
		/// </summary>
		public long TotalParseTime { get; set; }

		/// <summary>
		/// Total number of parse analyses
		/// </summary>
		public int TotalAnalyses { get; set; }

		/// <summary>
		/// Total number of analyses that were marked approved by the user but did not get a parse
		/// </summary>
		public int TotalUserApprovedAnalysesMissing { get; set; }

		/// <summary>
		/// Total number of parse analyses that were marked as disapproved by the user
		/// </summary>
		public int TotalUserDisapprovedAnalyses { get; set; }

		/// <summary>
		/// Total number of parse analyses that were marked as noOpinion by the user
		/// </summary>
		public int TotalUserNoOpinionAnalyses { get; set; }

		/// <summary>
		/// Parse reports for each word
		/// </summary>
		public IDictionary<string, ParseReport> ParseReports { get; set; }

		/// <summary>
		/// Whether this report is selected
		/// </summary>
		[JsonIgnore]
		public Boolean IsSelected { get; set; }

		/// <summary>
		/// Is this report the result of DiffParserReports?
		/// </summary>
		[JsonIgnore]
		public Boolean IsDiff { get; set; }

		/// <summary>
		/// Title used for window.
		/// This should be unique.
		/// </summary>
		[JsonIgnore]
		public string Title
		{
			get
			{
				string time = IsDiff ? new TimeSpan(Timestamp).ToString() : new DateTime(Timestamp).ToString();
				return (IsDiff ? "Diff " : "") + ProjectName + ", " + SourceText + ", " + time + "," + MachineName;
			}
		}

		/// <summary>
		/// The filename that the report came from.
		/// </summary>
		[JsonIgnore]
		public string Filename { get; set; }

		public ParserReport()
		{
			ParseReports = new Dictionary<string, ParseReport>();
		}

		public ParserReport(LcmCache cache)
		{
			ProjectName = cache.LanguageProject.ShortName;
			MachineName = Environment.MachineName;
			Timestamp = DateTime.UtcNow.ToFileTime();
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
			report.Word = word;
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
		/// Read the given json file as a ParserReport.
		/// </summary>
		/// <param name="filename"></param>
		/// <returns></returns>
		public static ParserReport ReadJsonFile(string filename)
		{
			string json = File.ReadAllText(filename);
			ParserReport report = Newtonsoft.Json.JsonConvert.DeserializeObject<ParserReport>(json);
			foreach (var word in report.ParseReports.Keys)
			{
				var parseReport = report.ParseReports[word];
				if (parseReport.Word == null)
					parseReport.Word = word;
			}
			report.Filename = filename;
			return report;
		}

		/// <summary>
		/// Write this parser report as json on the given filename.
		/// </summary>
		/// <param name="filename"></param>
		public void WriteJsonFile(string filename)
		{
			string json = JsonConvert.SerializeObject(this);
			using (StreamWriter outputFile = new StreamWriter(filename))
			{
				outputFile.WriteLine(json);
			}
			Filename = filename;
		}

		/// <summary>
		/// Write this parser report as json in the standard place and return the filename.
		/// </summary>
		/// <param name="cache"></param>
		public string WriteJsonFile(LcmCache cache)
		{
			var reportDir = GetProjectReportsDirectory(cache);
			var filename = Path.Combine(reportDir, Guid.NewGuid().ToString() + ".json");
			WriteJsonFile(filename);
			return filename;
		}

		/// <summary>
		/// Delete the JSON file that this report came from.
		/// </summary>
		public void DeleteJsonFile()
		{
			if (Filename == null) return;
			File.Delete(Filename);
		}

		/// <summary>
		/// Get the project reports directory for the project.
		/// This is where project reports are stored.
		/// </summary>
		/// <param name="cache"></param>
		/// <returns></returns>
		public static string GetProjectReportsDirectory(LcmCache cache)
		{
			// TODO: Handle the case when the project isn't local.
			var projectDir = Path.GetDirectoryName(cache.ProjectId.Path);
			var reportDir = Path.Combine(projectDir, "ProjectReports");
			System.IO.Directory.CreateDirectory(reportDir);
			return reportDir;
		}

		/// <summary>
		/// Return the differences between the current report and another report.
		/// </summary>
		/// <param name="report2"></param>
		/// <returns></returns>
		public ParserReport DiffParserReports (ParserReport report2)
		{
			ParserReport diff = new ParserReport();
			diff.IsDiff = true;
			diff.ProjectName = DiffNames(ProjectName, report2.ProjectName);
			diff.SourceText = DiffNames(SourceText, report2.SourceText);
			diff.MachineName = DiffNames(MachineName, report2.MachineName);
			diff.Timestamp = Timestamp - report2.Timestamp;
			diff.NumWords = NumWords - report2.NumWords;
			diff.NumParseErrors = NumParseErrors - report2.NumParseErrors;
			diff.NumZeroParses = NumZeroParses - report2.NumZeroParses;
			diff.TotalParseTime = TotalParseTime - report2.TotalParseTime;
			diff.TotalAnalyses = TotalAnalyses - report2.TotalAnalyses;
			diff.TotalUserApprovedAnalysesMissing = TotalUserApprovedAnalysesMissing - report2.TotalUserApprovedAnalysesMissing;
			diff.TotalUserDisapprovedAnalyses = TotalUserDisapprovedAnalyses - report2.TotalUserDisapprovedAnalyses;
			diff.TotalUserNoOpinionAnalyses = TotalUserNoOpinionAnalyses - report2.TotalUserNoOpinionAnalyses;

			ParseReport missingReport = new ParseReport
			{
				ErrorMessage = "missing"
			};

			foreach (string key in report2.ParseReports.Keys)
			{
				ParseReport oldReport = report2.ParseReports[key];
				ParseReport newReport = ParseReports.ContainsKey(key) ? ParseReports[key] : missingReport;
				ParseReport diffReport = newReport.DiffParseReport(oldReport);
				diff.AddParseReport(key, diffReport);
			}
			foreach (string key in ParseReports.Keys)
			{
				if (!report2.ParseReports.ContainsKey(key))
				{
					ParseReport newReport = ParseReports[key];
					ParseReport diffReport = newReport.DiffParseReport(missingReport);

					diff.AddParseReport(key, diffReport);
				}
			}

			return diff;
		}

		string DiffNames(string name1, string name2)
		{
			if (name1 == name2)
				return name1;
			return name1 + " - " + name2;
		}

		/// <summary>
		/// Is this parse report equal to other?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(ParserReport other)
		{
			if (other is null)
			{
				return false;
			}

			// Optimization for a common success case.
			if (Object.ReferenceEquals(this, other))
			{
				return true;
			}

			// If run-time types are not exactly the same, return false.
			if (this.GetType() != other.GetType()) return false;

			if (ProjectName != other.ProjectName) return false;

			if (MachineName != other.MachineName) return false;

			if (Timestamp != other.Timestamp) return false;

			if (SourceText != other.SourceText) return false;

			if (NumWords != other.NumWords) return false;

			if (NumParseErrors != other.NumParseErrors) return false;

			if (NumZeroParses != other.NumZeroParses) return false;

			if (TotalParseTime != other.TotalParseTime) return false;

			if (TotalAnalyses != other.TotalAnalyses) return false;

			if (TotalUserApprovedAnalysesMissing != other.TotalUserApprovedAnalysesMissing) return false;

			if (TotalUserDisapprovedAnalyses != other.TotalUserDisapprovedAnalyses) return false;

			if (TotalUserNoOpinionAnalyses != other.TotalUserNoOpinionAnalyses) return false;

			if (ParseReports.Count != other.ParseReports.Count) return false;

			foreach (string key in ParseReports.Keys)
				if (!ParseReports[key].Equals(other.ParseReports[key])) return false;

			return true;
		}
	}

	/// <summary>
	/// ParseReport reports the results of parsing a word.
	/// </summary>
	public class ParseReport : IEquatable<ParseReport>
	{
		/// <summary>
		/// The word parsed
		/// </summary>
		public string Word { get; set; }

		/// <summary>
		/// Time to parse the word in milliseconds
		/// </summary>
		public long ParseTime { get; set; }

		/// <summary>
		/// Error message from the parser
		/// </summary>
		public string ErrorMessage { get; set; }

		/// <summary>
		/// Number of parse analyses
		/// </summary>
		public int NumAnalyses { get; set; }

		/// <summary>
		/// Number of analyses that were marked approved by the user but did not get a parse
		/// </summary>
		public int NumUserApprovedAnalysesMissing { get; set; }

		/// <summary>
		/// Number of parse analyses that were marked as disapproved by the user
		/// </summary>
		public int NumUserDisapprovedAnalyses {  get; set; }

		/// <summary>
		/// Number of parse analyses that were marked as noOpinion by the user
		/// </summary>
		public int NumUserNoOpinionAnalyses { get; set; }

		public ParseReport() { }

		/// <summary>
		/// Create a parse report from a wordform and a parse result.
		/// The wordform is needed to check user approval of parse analyses.
		/// </summary>
		/// <param name="wordform"></param>
		/// <param name="result"></param>
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

		/// <summary>
		/// Is this parse report equal to other?
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public bool Equals(ParseReport other)
		{
			if (ParseTime != other.ParseTime) return false;

			if (ErrorMessage != other.ErrorMessage) return false;

			if (NumAnalyses != other.NumAnalyses) return false;

			if (NumUserApprovedAnalysesMissing != other.NumUserApprovedAnalysesMissing) return false;

			if (NumUserDisapprovedAnalyses != other.NumUserDisapprovedAnalyses) return false;

			if (NumUserNoOpinionAnalyses != other.NumUserNoOpinionAnalyses) return false;

			return true;
		}
	}
}