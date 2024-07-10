using SIL.FieldWorks.WordWorks.Parser;
using System.Windows;
using System.Windows.Controls;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class ParserReportDialog : Window
	{
		public Mediator Mediator { get; set; }

		public ParserReportDialog()
		{
			InitializeComponent();
		}

		public ParserReportDialog(ParserReport parserReport, Mediator mediator)
		{
			InitializeComponent();
			Mediator = mediator;
			DataContext = new ParserReportViewModel { ParserReport = parserReport };
		}

		public void ReparseWord(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parseReport = button.CommandParameter as ParseReport;
			Mediator.SendMessage("TryThisWord", parseReport.Word);
		}


	}
}
