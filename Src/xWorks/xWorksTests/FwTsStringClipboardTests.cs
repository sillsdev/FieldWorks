// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// Task 3.13 — the shared cross-framework clipboard seam. Proves the bridge speaks the legacy
	/// <c>"TsString"</c> + <c>UnicodeText</c> OS clipboard contract in both directions: what the bridge
	/// writes, legacy code reads (same <see cref="TsStringWrapper"/> format), and what legacy
	/// <c>EditingHelper</c> writes, the bridge reads — with multi-writing-system runs preserved.
	/// </summary>
	[TestFixture]
	public class FwTsStringClipboardTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		public override void TestSetup()
		{
			base.TestSetup();
			ClipboardUtils.Manager.SetClipboardAdapter(new ClipboardStub());
		}

		private FwTsStringClipboard CreateClipboard()
			=> new FwTsStringClipboard(Cache.WritingSystemFactory);

		private ITsString MakeMultiWsString()
		{
			var bldr = TsStringUtils.MakeString("casa", Cache.DefaultVernWs).GetBldr();
			bldr.ReplaceTsString(bldr.Length, bldr.Length,
				TsStringUtils.MakeString(" house", Cache.DefaultAnalWs));
			return bldr.GetString();
		}

		[Test]
		public void RoundTrip_MultiWsString_PreservesWritingSystemRuns()
		{
			var clipboard = CreateClipboard();
			var original = MakeMultiWsString();

			clipboard.SetText(clipboard.FromTsString(original));
			var payload = clipboard.GetText();

			Assert.That(payload, Is.Not.Null);
			Assert.That(payload.PlainText, Is.EqualTo("casa house"));
			Assert.That(payload.RichXml, Is.Not.Null.And.Not.Empty, "the rich lane must survive the clipboard");

			var roundTripped = clipboard.ToTsString(payload);
			Assert.That(roundTripped.Text, Is.EqualTo(original.Text));
			Assert.That(roundTripped.RunCount, Is.EqualTo(2), "both writing-system runs survive");
			Assert.That(roundTripped.get_WritingSystem(0), Is.EqualTo(Cache.DefaultVernWs));
			Assert.That(roundTripped.get_WritingSystem(1), Is.EqualTo(Cache.DefaultAnalWs));
		}

		[Test]
		public void SetText_WritesTheLegacyTsStringFormat_LegacyReaderSeesIt()
		{
			var clipboard = CreateClipboard();
			clipboard.SetText(clipboard.FromTsString(MakeMultiWsString()));

			// Read exactly the way legacy EditingHelper.GetTsStringFromClipboard does.
			var dataObject = ClipboardUtils.GetDataObject();
			var wrapper = dataObject.GetData(TsStringWrapper.TsStringFormat) as TsStringWrapper;
			Assert.That(wrapper, Is.Not.Null, "legacy surfaces must find the TsString format the bridge wrote");
			Assert.That(wrapper.GetTsString(Cache.WritingSystemFactory).Text, Is.EqualTo("casa house"));
			Assert.That(dataObject.GetData(DataFormats.UnicodeText), Is.EqualTo("casa house"),
				"the plain-text lane is present for external consumers");
		}

		[Test]
		public void GetText_ReadsWhatLegacyEditingHelperWrote()
		{
			// Write through the real legacy code path.
			SIL.FieldWorks.Common.RootSites.EditingHelper.SetTsStringOnClipboard(
				MakeMultiWsString(), false, Cache.WritingSystemFactory);

			var payload = CreateClipboard().GetText();
			Assert.That(payload, Is.Not.Null);
			Assert.That(payload.RichXml, Is.Not.Null, "a legacy Views copy must surface the rich lane");
			Assert.That(CreateClipboard().ToTsString(payload).Text, Is.EqualTo("casa house"));
		}

		[Test]
		public void GetText_PlainTextOnlyClipboard_FallsBackToPlainLane()
		{
			ClipboardUtils.SetDataObject(new DataObject(DataFormats.UnicodeText, "plain only"));

			var payload = CreateClipboard().GetText();
			Assert.That(payload, Is.Not.Null);
			Assert.That(payload.PlainText, Is.EqualTo("plain only"));
			Assert.That(payload.RichXml, Is.Null, "no rich lane is invented for external text");
		}

		[Test]
		public void GetText_EmptyClipboard_ReturnsNull()
		{
			var clipboard = CreateClipboard();
			Assert.That(clipboard.GetText(), Is.Null);
			Assert.That(clipboard.ContainsText(), Is.False);
		}
	}
}
