using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using System.Diagnostics;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using XCore;

namespace SIL.FieldWorks.LexText.Controls
{
	public partial class ParserReportDialog : Window
	{
		public Mediator Mediator { get; set; }
		public LcmCache Cache { get; set; }

		public ParserReportDialog()
		{
			InitializeComponent();
		}

		public ParserReportDialog(ParserReportViewModel parserReport, Mediator mediator, LcmCache cache)
		{
			InitializeComponent();
			Mediator = mediator;
			Cache = cache;
			DataContext = parserReport;
			commentLabel.Content = ParserUIStrings.ksComment + ":";
		}

		public void SaveParserReport(object sender, RoutedEventArgs e)
		{
			ParserReportViewModel parserReportViewModel = (ParserReportViewModel)DataContext;
			ParserListener.SaveParserReport(parserReportViewModel, Cache, null);
		}


		public void ReparseWord(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parseReport = button.CommandParameter as ParseReport;
			Mediator.SendMessage("TryThisWord", RemoveArrow(parseReport.Word));
		}

		public void ShowWordAnalyses(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parseReport = button.CommandParameter as ParseReport;
			var tsString = TsStringUtils.MakeString(RemoveArrow(parseReport.Word), Cache.DefaultVernWs);
			IWfiWordform wordform;
			if (Cache.ServiceLocator.GetInstance<IWfiWordformRepository>().TryGetObject(tsString, out wordform))
			{
				var fwLink = new FwLinkArgs("Analyses", wordform.Guid);
				Mediator.PostMessage("FollowLink", fwLink);
			} else
			{
				// This should never happen.
				MessageBox.Show("Unknown word " + parseReport.Word);
			}
		}

		private string RemoveArrow(string word)
		{
			return word.Replace(" => ", string.Empty);
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

		private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
		{
			if (sender is DataGrid dataGrid)
			{
				if (dataGrid.SelectedItem is ParserReportViewModel selectedItem)
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
	}
}
