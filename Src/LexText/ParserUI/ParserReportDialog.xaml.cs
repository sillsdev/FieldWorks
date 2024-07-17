using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using System;
using System.Collections.Generic;
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
			Mediator.SendMessage("TryThisWord", parseReport.Word);
		}

		public void ShowWordAnalyses(object sender, RoutedEventArgs e)
		{
			var button = sender as Button;
			var parseReport = button.CommandParameter as ParseReport;
			var wordForm = TsStringUtils.MakeString(parseReport.Word, Cache.DefaultVernWs);
			var analysis = WfiWordformServices.FindOrCreateWordform(Cache, wordForm);
			var fwLink = new FwLinkArgs("Analyses", analysis.Guid);
			List<Property> additionalProps = fwLink.PropertyTableEntries;
			Mediator.PostMessage("FollowLink", fwLink);
		}
	}
}
