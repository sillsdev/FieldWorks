using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.LexText.Controls
{
	public class ParserReportsViewModel : INotifyPropertyChanged
	{
		private ObservableCollection<ParserReportViewModel> _parserReports;

		public ObservableCollection<ParserReportViewModel> ParserReports
		{
			get => _parserReports;
			set
			{
				// Do this even if value == _parserReports because it may have new items.
				// Unsubscribe from PropertyChanged events of old collection items
				if (_parserReports != null)
				{
					foreach (var report in _parserReports)
					{
						report.PropertyChanged -= OnReportPropertyChanged;
					}
				}

				_parserReports = value;

				// Subscribe to PropertyChanged events of new collection items
				if (_parserReports != null)
				{
					foreach (var report in _parserReports)
					{
						report.PropertyChanged += OnReportPropertyChanged;
					}
				}

				// Don't call OnPropertyChanged here!  It suppresses the default behavior.
				UpdateButtonStates(); // Update button states when the collection changes
			}
		}
		public bool CanShowReport => ParserReports.Count(report => report.IsSelected) == 1;
		public bool CanDiffReports => ParserReports.Count(report => report.IsSelected) == 2;
		public bool CanDeleteReports => ParserReports.Any(report => report.IsSelected);
		public bool CanSaveReport => ParserReports.Count(report => report.IsSelected) == 1;

		public string DeleteButtonContent => string.Format(ParserUIStrings.ksDelete,
			ParserReports.Count(report => report.IsSelected));
		public ParserReportsViewModel()
		{
			ParserReports = new ObservableCollection<ParserReportViewModel>();

			// Check if we're in design mode
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// Populate with design-time data
				ParserReports.Add(new ParserReportViewModel { ParserReport = new ParserReport
				{
					ProjectName = "Example Project 1",
					MachineName = "DevMachine1",
					SourceText = "Sample Text 1",
					Timestamp = DateTime.Now.ToFileTime(), // Convert DateTime to file time
					NumWords = 1000,
					NumParseErrors = 5,
					NumZeroParses = 3
				}});
				ParserReports.Add(new ParserReportViewModel { ParserReport = new ParserReport
				{
					ProjectName = "Example Project 2",
					MachineName = "DevMachine2",
					SourceText = "Sample Text 2",
					Timestamp = DateTime.Now.AddHours(-1).ToFileTime(), // Convert DateTime to file time
					NumWords = 1500,
					NumParseErrors = 2,
					NumZeroParses = 1
				}});
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;


		private void OnReportPropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			if (e.PropertyName == nameof(ParserReportViewModel.IsSelected))
			{
				// Notify changes to button state properties
				UpdateButtonStates();
			}
		}
		protected virtual void OnPropertyChanged(string propertyName)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		// Call this method whenever the IsSelected property of any ParserReport changes
		public void UpdateButtonStates()
		{
			OnPropertyChanged(nameof(CanShowReport));
			OnPropertyChanged(nameof(CanSaveReport));
			OnPropertyChanged(nameof(CanDiffReports));
			OnPropertyChanged(nameof(CanSaveReport));
			OnPropertyChanged(nameof(CanDeleteReports));
			OnPropertyChanged(nameof(DeleteButtonContent));
		}
	}
}
