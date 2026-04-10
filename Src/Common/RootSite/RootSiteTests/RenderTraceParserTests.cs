// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.RenderVerification;

namespace SIL.FieldWorks.Common.RootSites
{
	[TestFixture]
	public class RenderTraceParserTests
	{
		[TestCase("Perform-Offscreen-Layout")]
		[TestCase("Layout.v2")]
		[TestCase("cold/Layout")]
		public void ParseLine_StageNameContainsSeparators_ParsesStageName(string stageName)
		{
			var parser = new RenderTraceParser();
			double cumulativeTime = 0;

			var traceEvent = parser.ParseLine(
				$"[RENDER] Stage={stageName} Duration=12.34ms Context=phase=cold",
				ref cumulativeTime);

			Assert.That(traceEvent, Is.Not.Null);
			Assert.That(traceEvent.Stage, Is.EqualTo(stageName));
			Assert.That(traceEvent.DurationMs, Is.EqualTo(12.34).Within(0.001));
		}

		[Test]
		public void ParseLine_TimestampedStageNameContainsSeparators_ParsesStageName()
		{
			var parser = new RenderTraceParser();
			double cumulativeTime = 0;

			var traceEvent = parser.ParseLine(
				"[2026-01-22T12:34:56.789] [RENDER] Stage=warm/Layout.v2 Duration=7.5ms Context=phase=warm",
				ref cumulativeTime);

			Assert.That(traceEvent, Is.Not.Null);
			Assert.That(traceEvent.Stage, Is.EqualTo("warm/Layout.v2"));
			Assert.That(traceEvent.DurationMs, Is.EqualTo(7.5).Within(0.001));
		}
	}
}