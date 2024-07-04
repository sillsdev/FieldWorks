using SIL.FieldWorks.WordWorks.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class ParserReportDialog : Window
	{
		public ParserReportDialog()
		{
			InitializeComponent();
		}

		public ParserReportDialog(ParserReport parserReport)
		{
			InitializeComponent();
			DataContext = new ParserReportViewModel { ParserReport = parserReport };
		}
	}
}
