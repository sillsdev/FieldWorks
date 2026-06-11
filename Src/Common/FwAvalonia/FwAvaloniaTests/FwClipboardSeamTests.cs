// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using NUnit.Framework;
using SIL.FieldWorks.Common.FwAvalonia.Seams;

namespace FwAvaloniaTests
{
	/// <summary>Task 3.13 — the LCModel-free clipboard seam contract and its in-memory implementation.</summary>
	[TestFixture]
	public class FwClipboardSeamTests
	{
		[Test]
		public void InMemoryClipboard_StartsEmpty()
		{
			var clipboard = new InMemoryFwClipboard();
			Assert.That(clipboard.ContainsText(), Is.False);
			Assert.That(clipboard.GetText(), Is.Null);
		}

		[Test]
		public void InMemoryClipboard_RoundTripsBothLanes()
		{
			var clipboard = new InMemoryFwClipboard();
			clipboard.SetText(new FwClipboardText("plain", "<Str><Run ws='en'>plain</Run></Str>"));

			var payload = clipboard.GetText();
			Assert.That(clipboard.ContainsText(), Is.True);
			Assert.That(payload.PlainText, Is.EqualTo("plain"));
			Assert.That(payload.RichXml, Does.Contain("Run"));
		}

		[Test]
		public void Payload_PlainLaneIsNeverNull_RichLaneIsOptional()
		{
			var payload = new FwClipboardText(null);
			Assert.That(payload.PlainText, Is.EqualTo(string.Empty));
			Assert.That(payload.RichXml, Is.Null);
		}
	}
}
