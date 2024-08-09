using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using System.Windows;
using System.Windows.Controls;
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

		public ParserReportDialog(ParserReport parserReport, Mediator mediator, LcmCache cache)
		{
			InitializeComponent();
			Mediator = mediator;
			Cache = cache;
			DataContext = new ParserReportViewModel { ParserReport = parserReport };
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
	}
}
