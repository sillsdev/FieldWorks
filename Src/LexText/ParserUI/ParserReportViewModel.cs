using SIL.FieldWorks.WordWorks.Parser;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SIL.FieldWorks.LexText.Controls
{
	public class ParserReportViewModel
	{
		public ParserReport ParserReport { get; set; }

		public ParserReportViewModel()
		{
			ParserReport = new ParserReport();

			// Check if we're in design mode
			if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
			{
				// Populate with design-time data
			}
			else
			{
				// Runtime data loading logic here
			}
		}
	}
}
