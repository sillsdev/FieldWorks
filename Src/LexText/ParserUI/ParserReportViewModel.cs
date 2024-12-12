using SIL.FieldWorks.WordWorks.Parser;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;

namespace SIL.FieldWorks.LexText.Controls
{
	public class ParserReportViewModel : INotifyPropertyChanged
	{
		public ParserReport ParserReport { get; set; }

		private FileTimeToDateTimeConverter m_FileTimeToDateTimeConverter = new FileTimeToDateTimeConverter();

		public string Title
		{
			get
			{
				string time = m_FileTimeToDateTimeConverter.Convert(ParserReport.Timestamp, null, null, null).ToString();
				if (ParserReport.IsDiff)
					time = m_FileTimeToDateTimeConverter.Convert(ParserReport.DiffTimestamp, null, null, null).ToString() + " => " + time;
				return (ParserReport.IsDiff ? ParserUIStrings.ksDiffHeader + " " : "") + ParserReport.ProjectName + ", " + ParserReport.SourceText + ", " + time + ", " + ParserReport.MachineName;
			}
		}

		public string DisplayComment
		{
			get
			{
				if (ParserReport.Filename == null && !ParserReport.IsDiff)
				{
					return ParserUIStrings.ksUnsavedParserReport;
				}
				return ParserReport.Comment;
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

		public DateTime Timestamp => DateTime.FromFileTime(ParserReport.Timestamp);

		public bool IsSelected
		{
			get => ParserReport.IsSelected;
			set
			{
				if (ParserReport.IsSelected != value)
				{
					ParserReport.IsSelected = value;
					OnPropertyChanged(nameof(IsSelected));
				}
			}
		}

		public bool CanSaveReport => !ParserReport.IsDiff;


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
		public event PropertyChangedEventHandler PropertyChanged;
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		public void UpdateDisplayComment()
		{
			OnPropertyChanged("DisplayComment");
		}
	}
}
