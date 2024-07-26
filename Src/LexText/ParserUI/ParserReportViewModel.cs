using SIL.FieldWorks.WordWorks.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIL.FieldWorks.LexText.Controls
{
	public class ParserReportViewModel
	{
		public ParserReport ParserReport { get; set; }

		private FileTimeToDateTimeConverter m_FileTimeToDateTimeConverter = new FileTimeToDateTimeConverter();

		public string Title
		{
			get
			{
				string time = ParserReport.IsDiff
					? TimeSpan.FromTicks(ParserReport.Timestamp).ToString()
					: m_FileTimeToDateTimeConverter.Convert(ParserReport.Timestamp, null, null, null).ToString();
				return (ParserReport.IsDiff ? ParserUIStrings.ksDiffHeader + " " : "") + ParserReport.ProjectName + ", " + ParserReport.SourceText + ", " + time + ", " + ParserReport.MachineName;
			}
		}

		public IEnumerable<ParseReport> ParseReports
		{
			get
			{
				// Use ToList so that sorting the reports doesn't change the data model.
				return ParserReport.ParseReports.Values.ToList();
			}
		}

		public string TotalAnalysesWithZeros
		{
			get
			{
				return ParserReport.TotalAnalyses + " (" + ParserUIStrings.ksZeros + ": " + ParserReport.NumZeroParses + ")";
			}

		}

		public ParserReportViewModel()
		{
			ParserReport = new ParserReport();

			// Check if we're in design mode
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// Populate with design-time data
				ParserReport.AddParseReport("test", new ParseReport(null, new ParseResult(new List<ParseAnalysis>())));
				ParserReport.AddParseReport("error", new ParseReport(null, new ParseResult("error")));
			}
		}
	}
}
