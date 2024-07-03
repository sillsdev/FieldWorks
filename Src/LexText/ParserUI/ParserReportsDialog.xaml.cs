using SIL.FieldWorks.WordWorks.Parser;
using System.Collections.ObjectModel;
using System.Windows;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Interaction logic for ParserReportsDialog.xaml
	/// </summary>
	public partial class ParserReportsDialog : Window
	{
		public ParserReportsDialog()
		{
			InitializeComponent();
		}

		public ParserReportsDialog(ObservableCollection<ParserReport> parserReports)
		{
			InitializeComponent();
			DataContext = new ParserReportsViewModel { ParserReports = parserReports };
		}
	}
}
