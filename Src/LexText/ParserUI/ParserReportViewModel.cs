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

		public string Title
		{
			get
			{
				string time = ParserReport.IsDiff ? new TimeSpan(ParserReport.Timestamp).ToString() : new DateTime(ParserReport.Timestamp).ToString();
				return (ParserReport.IsDiff ? "Diff " : "") + ParserReport.ProjectName + ", " + ParserReport.SourceText + ", " + time + "," + ParserReport.MachineName;
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
