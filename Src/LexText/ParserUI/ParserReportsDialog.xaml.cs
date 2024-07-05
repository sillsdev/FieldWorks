using SIL.FieldWorks.WordWorks.Parser;
using System.Collections.ObjectModel;
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
			ParserReports = parserReports;
			DataContext = new ParserReportsViewModel { ParserReports = parserReports };
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
			// parserReport.Delete();
			ParserReports.Remove(parserReport);
		}
	}
}
