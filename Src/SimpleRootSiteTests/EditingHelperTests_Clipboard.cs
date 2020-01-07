// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Text;
using System.Windows.Forms;
using NUnit.Framework;
using SIL.FieldWorks.CacheLight;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Tests for EditingHelper class that test code that deals with the clipboard.
	/// </summary>
	/// <remarks>This base class uses a clipboard stub.</remarks>
	[TestFixture]
	public class EditingHelperTests_Clipboard : SimpleRootsiteTestsBase<RealDataCache>
	{
		protected virtual void SetClipboardAdapter()
		{
			ClipboardUtils.SetClipboardAdapter(new ClipboardStub());
		}

		/// <summary>
		/// Test setup
		/// </summary>
		public override void TestSetup()
		{
			base.TestSetup();
			SetClipboardAdapter();
			ClipboardUtils.SetDataObject(new DataObject());
			ClipboardUtils.SetText("I want a monkey");
		}

		/// <summary>
		/// Tests getting Unicode characters from the clipboard (TE-4633)
		/// </summary>
		[Test]
		public void PasteUnicode()
		{
			const string originalInput = "\u091C\u092E\u094D\u200D\u092E\u0947\u0906";
			ClipboardUtils.SetText(originalInput, TextDataFormat.UnicodeText);

			using (var editingHelper = new DummyEditingHelper())
			{
				Assert.AreEqual(originalInput, editingHelper.CallGetTextFromClipboard().Text);
			}
		}

		/// <summary>
		/// Tests setting and retrieving data on/from the clipboard.
		/// </summary>
		[Test]
		public void GetSetClipboard()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			var wsEng = enWs.Handle;

			CoreWritingSystemDefinition swgWs;
			wsManager.GetOrSet("swg", out swgWs);
			var wsSwg = swgWs.Handle;

			var incStrBldr = TsStringUtils.MakeIncStrBldr();
			incStrBldr.AppendTsString(TsStringUtils.MakeString("Gogomer ", wsSwg));
			incStrBldr.AppendTsString(TsStringUtils.MakeString("cucumber", wsEng));
			EditingHelper.SetTsStringOnClipboard(incStrBldr.GetString(), false, wsManager);

			var tss = m_basicView.EditingHelper.GetTsStringFromClipboard(wsManager);
			Assert.IsNotNull(tss, "Couldn't get TsString from clipboard");
			Assert.AreEqual(2, tss.RunCount);
			Assert.AreEqual("Gogomer ", tss.get_RunText(0));
			Assert.AreEqual("cucumber", tss.get_RunText(1));

			var newDataObj = ClipboardUtils.GetDataObject();
			Assert.IsNotNull(newDataObj, "Couldn't get DataObject from clipboard");
			Assert.AreEqual("Gogomer cucumber", newDataObj.GetData("Text"));
		}

		/// <summary>
		/// Verifies that data is normalized NFC when placed on clipboard.
		/// </summary>
		[Test]
		public void SetTsStringOnClipboard_UsesNFC()
		{
			var wsManager = new WritingSystemManager();
			CoreWritingSystemDefinition enWs;
			wsManager.GetOrSet("en", out enWs);
			var wsEng = enWs.Handle;
			const string originalInput = "\x7a7a\x60f3\x79d1\x5b78\x0020\xd558";
			var input = originalInput.Normalize(NormalizationForm.FormD);
			Assert.That(originalInput, Is.Not.EqualTo(input)); // make sure input is NOT NFC
			var tss = TsStringUtils.MakeString(input, wsEng);
			EditingHelper.SetTsStringOnClipboard(tss, false, wsManager);
			var newDataObj = ClipboardUtils.GetDataObject();
			Assert.IsNotNull(newDataObj, "Couldn't get DataObject from clipboard");
			Assert.AreEqual(originalInput, newDataObj.GetData("Text"));
		}
	}
}