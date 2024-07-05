using SIL.Extensions;
using SIL.FieldWorks.WordWorks.Parser;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Interaction logic for ParserReportsDialog.xaml
	/// </summary>
	public partial class ParserReportsDialog : Window
	{
		public ObservableCollection<ParserReport> ParserReports { get; }

		public ParserReportsDialog()
		{
			InitializeComponent();
		}

		public ParserReportsDialog(ObservableCollection<ParserReport> parserReports)
		{
			InitializeComponent();
			var sortedReports = new ObservableCollection<ParserReport>(parserReports.OrderByDescending(i => i.Timestamp));
			ParserReports = sortedReports;
			DataContext = new ParserReportsViewModel { ParserReports = sortedReports };
		}

		public void ShowParserReport(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parserReport = button.CommandParameter as ParserReport;
			ParserListener.ShowParserReport(parserReport);
		}
		public void DeleteParserReport(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parserReport = button.CommandParameter as ParserReport;
			parserReport.DeleteJsonFile();
			ParserReports.Remove(parserReport);
		}
		public void DiffParserReports(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parserReport = button.CommandParameter as ParserReport;
			ParserReport parserReport2 = null;
			foreach (var report in ParserReports)
			{
				if (report.IsSelected && report != parserReport)
				{
					parserReport2 = report;
				}
			}
			if (parserReport2 == null)
			{
				MessageBox.Show("Please select a second report other than this report using the radio button labelled '2nd'.");
				return;
			}
			var diff = parserReport.DiffParserReports(parserReport2);
			ParserListener.ShowParserReport(diff);
		}
	}
}
