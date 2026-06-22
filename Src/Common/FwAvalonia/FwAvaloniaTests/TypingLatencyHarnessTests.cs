// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Diagnostics;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Region;

namespace FwAvaloniaTests
{
	[TestFixture]
	public class TypingLatencyHarnessTests
	{
		private const int Keystrokes = 500;

		[TestCase(1.0, 6.0)]
		[TestCase(1.5, 8.0)]
		public void TypingHarness_MeetsPerKeystrokeThresholds(double dpiScale, double maxMsPerKey)
		{
			var rich = RegionRichTextEditAlgorithms.FromRuns(string.Empty,
				new[] { new RegionTextRun(string.Empty, "qaa-x-kal") });
			var text = string.Empty;

			var timer = Stopwatch.StartNew();
			for (var i = 0; i < Keystrokes; i++)
			{
				text += "a";
				rich = RegionRichTextEditAlgorithms.ApplyPlainTextEdit(rich, text);

				var rtl = (i % 11) == 0;
				_ = RegionBidirectionalTextNavigation.MoveCaret(text, rich.Runs, text.Length,
					physicalLeft: rtl, defaultRightToLeft: rtl);
			}

			timer.Stop();
			var msPerKey = timer.Elapsed.TotalMilliseconds / Keystrokes;
			TestContext.WriteLine("DPI {0:P0}: {1:F3} ms/key over {2} edits", dpiScale, msPerKey, Keystrokes);
			Assert.That(msPerKey, Is.LessThanOrEqualTo(maxMsPerKey),
				"typing latency must stay inside the change's performance gate");
		}
	}
}
