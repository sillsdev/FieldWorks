using SIL.FieldWorks.WordWorks.Parser;
using System.Collections.ObjectModel;
using System.Windows;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Interaction logic for ParserReportDialog.xaml
	/// </summary>
	public partial class ParserReportDialog : Window
	{
		public ParserReportDialog()
		{
			InitializeComponent();
		}

		public ParserReportDialog(ObservableCollection<ParserReport> parserReports)
		{
			InitializeComponent();
			DataContext = new ParserReportViewModel { ParserReports = parserReports };
		}
	}
}
