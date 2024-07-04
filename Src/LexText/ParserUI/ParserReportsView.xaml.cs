using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using SIL.FieldWorks.WordWorks.Parser;

namespace SIL.FieldWorks.LexText.Controls
{
	/// <summary>
	/// Interaction logic for ParserReportsView.xaml
	/// </summary>
	public partial class ParserReportsView : UserControl
	{
		public ParserReportsView()
		{
			InitializeComponent();
		}

		public void ShowParserReport(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parserReport = button.CommandParameter as ParserReport;
			ParserListener.ShowParserReport(parserReport);
		}
	}
}
