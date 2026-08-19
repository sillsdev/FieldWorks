// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class AiAnalysisExportRequestTests
	{
		[Test]
		public void Constructor_SetsTextsFolderAndGrammarPath_LeavesHandledFalseAndMessagesEmpty()
		{
			var texts = new List<IStText>();
			var request = new AiAnalysisExportRequest(texts, @"C:\some\folder", @"C:\some\folder\HCGrammar.xml");

			Assert.That(request.TextsToExport, Is.SameAs(texts));
			Assert.That(request.OutputFolder, Is.EqualTo(@"C:\some\folder"));
			Assert.That(request.GrammarPath, Is.EqualTo(@"C:\some\folder\HCGrammar.xml"));
			Assert.That(request.Handled, Is.False);
			Assert.That(request.Messages, Is.Empty);
		}

		[Test]
		public void Messages_CanBeAppendedByASubscriber()
		{
			var request = new AiAnalysisExportRequest(new List<IStText>(), @"C:\folder", @"C:\folder\HCGrammar.xml");

			request.Messages.Add("Some Text: disk full");
			request.Handled = true;

			Assert.That(request.Messages, Is.EqualTo(new[] { "Some Text: disk full" }));
			Assert.That(request.Handled, Is.True);
		}
	}
}
