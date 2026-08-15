// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using NUnit.Framework;
using SIL.LCModel;

namespace SIL.FieldWorks.Common.FwUtils
{
	[TestFixture]
	public class ExportTextsAsFlexTextRequestTests
	{
		[Test]
		public void Constructor_SetsTextsAndFolder_LeavesHandledFalseAndFailuresEmpty()
		{
			var texts = new List<IStText>();
			var request = new ExportTextsAsFlexTextRequest(texts, @"C:\some\folder");

			Assert.That(request.TextsToExport, Is.SameAs(texts));
			Assert.That(request.OutputFolder, Is.EqualTo(@"C:\some\folder"));
			Assert.That(request.Handled, Is.False);
			Assert.That(request.Failures, Is.Empty);
		}

		[Test]
		public void Failures_CanBeAppendedByASubscriber()
		{
			var request = new ExportTextsAsFlexTextRequest(new List<IStText>(), @"C:\folder");

			request.Failures.Add("Some Text: disk full");
			request.Handled = true;

			Assert.That(request.Failures, Is.EqualTo(new[] { "Some Text: disk full" }));
			Assert.That(request.Handled, Is.True);
		}
	}
}
