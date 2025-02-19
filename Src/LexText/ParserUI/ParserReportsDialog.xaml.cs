using SIL.Extensions;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Interaction logic for ParserReportsDialog.xaml
	/// </summary>
	public partial class ParserReportsDialog : Window
	{
		public ObservableCollection<ParserReportViewModel> ParserReports { get; }

		public Mediator Mediator { get; set; }

		public LcmCache Cache { get; set; }

		public string DefaultComment = null;

		public ParserReportsDialog()
		{
			InitializeComponent();
		}

		public ParserReportsDialog(ObservableCollection<ParserReportViewModel> parserReports, Mediator mediator, LcmCache cache, string defaultComment)
		{
			InitializeComponent();
			parserReports.Sort((x, y) => y.Timestamp.CompareTo(x.Timestamp));
			ParserReports = parserReports;
			Mediator = mediator;
			Cache = cache;
			DataContext = new ParserReportsViewModel { ParserReports = parserReports };
			DefaultComment = defaultComment;
		}

		private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
		{
			ScrollViewer scrollViewer = sender as ScrollViewer;
			if (scrollViewer != null)
				if (e.Delta > 0)
					scrollViewer.LineUp();
				else
					scrollViewer.LineDown();
			e.Handled = true;
		}

		public void ShowParserReport(object sender, RoutedEventArgs e)
		{
			foreach (var report in ParserReports)
			{
				if (report.IsSelected)
				{
					ParserListener.ShowParserReport(report, Mediator, Cache);
					break;
				}
			}
		}

		public void SaveParserReport(object sender, RoutedEventArgs e)
		{
			foreach (var report in ParserReports)
			{
				if (report.IsSelected)
				{
					ParserListener.SaveParserReport(report, Cache, DefaultComment);
				}
			}
			((ParserReportsViewModel)DataContext).UpdateButtonStates();

		}

		public void DeleteParserReport(object sender, RoutedEventArgs e)
		{
			foreach (var report in ParserReports.ToArray()) // ToArray to avoid modifying the collection while iterating
			{
				if (report.IsSelected)
				{
					report.ParserReport.DeleteJsonFile();
					report.IsSelected = false;
					ParserReports.Remove(report);
				}
			}
		}

		public void DiffParserReports(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			ParserReportViewModel parserReport = null;
			ParserReportViewModel parserReport2 = null;
			foreach (var report in ParserReports)
			{
				if (report.ParserReport.IsSelected)
				{
					if (parserReport == null)
					{
						parserReport = report;
					}
					else if(parserReport2 == null)
					{
						parserReport2 = report;
					}
					else
					{
						// other logic should prevent this case, but if we break that logic just throw an exception.
						throw new System.Exception("Only two reports can be selected for diffing.");
					}
				}
			}
			if (parserReport2 == null)
			{
				throw new System.Exception("Two reports must be selected for diffing.");
			}
			if (parserReport.Timestamp < parserReport2.Timestamp)
			{
				// swap the two variables.
				ParserReportViewModel temp = parserReport;
				parserReport = parserReport2;
				parserReport2 = temp;
			}
			var diff = parserReport.ParserReport.DiffParserReports(parserReport2.ParserReport);
			ParserReportViewModel viewModel = new ParserReportViewModel() { ParserReport = diff };
			ParserListener.ShowParserReport(viewModel, Mediator, Cache);
		}
		private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is DataGrid dataGrid)
			{
				if(dataGrid.SelectedItem is ParserReportViewModel selectedItem)
					ParserListener.ShowParserReport(selectedItem, Mediator, Cache);
			}
			else
				Debug.Fail("Type of Contents of DataGrid changed, adjust double click code.");
		}
		private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (sender is DataGrid dataGrid)
			{
				// Turn off selection in favor of the check box.
				Dispatcher.BeginInvoke(DispatcherPriority.Render, new Action(() => dataGrid.UnselectAll()));
			}
		}
		private void CheckBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
		{
			if (sender is CheckBox checkbox)
			{
				checkbox.Focus(); // Focus the checkbox to handle the click event
				e.Handled = true; // Prevent the row from being selected
				var newValue = !checkbox.IsChecked ?? true; // Toggle the checkbox value
				checkbox.IsChecked = newValue; // Set the new value
				var bindingExpression = checkbox.GetBindingExpression(ToggleButton.IsCheckedProperty);
				bindingExpression?.UpdateSource(); // Update the binding source
			}
		}
	}
}
