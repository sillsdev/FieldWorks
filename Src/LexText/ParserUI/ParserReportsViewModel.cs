using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.LexText.Controls
{
	public class ParserReportsViewModel : INotifyPropertyChanged
	{
		public ObservableCollection<ParserReport> ParserReports { get; set; }

		public ParserReportsViewModel()
		{
			ParserReports = new ObservableCollection<ParserReport>();

			// Check if we're in design mode
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// Populate with design-time data
				ParserReports.Add(new ParserReport
				{
					ProjectName = "Example Project 1",
					MachineName = "DevMachine1",
					SourceText = "Sample Text 1",
					Timestamp = DateTime.Now.ToFileTime(), // Convert DateTime to file time
					NumWords = 1000,
					NumParseErrors = 5,
					NumZeroParses = 3
				});
				ParserReports.Add(new ParserReport
				{
					ProjectName = "Example Project 2",
					MachineName = "DevMachine2",
					SourceText = "Sample Text 2",
					Timestamp = DateTime.Now.AddHours(-1).ToFileTime(), // Convert DateTime to file time
					NumWords = 1500,
					NumParseErrors = 2,
					NumZeroParses = 1
				});
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}
	}
}
