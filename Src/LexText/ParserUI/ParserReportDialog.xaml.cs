using SIL.FieldWorks.Common.FwUtils;
using static SIL.FieldWorks.Common.FwUtils.FwUtils;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using System;
using System.Diagnostics;
using System.Drawing;
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

		private readonly PropertyTable m_propertyTable;

		public ParserReportDialog()
		{
			InitializeComponent();
		}

		public ParserReportDialog(ParserReportViewModel parserReport, Mediator mediator, LcmCache cache, PropertyTable propertyTable)
		{
			InitializeComponent();
			Mediator = mediator;
			Cache = cache;
			m_propertyTable = propertyTable;
			DataContext = parserReport;
			commentLabel.Content = ParserUIStrings.ksComment + ":";
			SetFont();
		}

		public void SetFont()
		{
			Font font = FontHeightAdjuster.GetFontForNormalStyle(Cache.DefaultVernWs, Cache.WritingSystemFactory, m_propertyTable);
			if (font != null)
			{
				FontFamily = new System.Windows.Media.FontFamily(font.FontFamily.Name);
				FontSize = font.Size;
			}
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
#pragma warning disable 618 // suppress obsolete warning
				Mediator.PostMessage("FollowLink", fwLink);
#pragma warning restore 618
			}
			else
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
					Publisher.Publish(new PublisherParameterObject(EventConstants.ShowParserReport, selectedItem));
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
