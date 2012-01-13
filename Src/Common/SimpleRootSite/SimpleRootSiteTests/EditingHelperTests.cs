// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: EditingHelperTests.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Text;
using System.Windows.Forms;

using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	#region DummyEditingHelper
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DummyEditingHelper : EditingHelper
	{
		private PalasoWritingSystemManager m_privateWsFactory;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the text from clipboard.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public ITsString CallGetTextFromClipboard()
		{
			CheckDisposed();

			ITsPropsFactory propsFact = TsPropsFactoryClass.Create();
			return GetTextFromClipboard(null, false, propsFact.MakeProps("bla", 1, 0));
		}

		protected override bool IsParagraphLevelTag(int tag)
		{
			throw new NotImplementedException();
		}

		protected override int ParagraphContentsTag
		{
			get { throw new NotImplementedException(); }
		}

		protected override int ParagraphPropertiesTag
		{
			get { throw new NotImplementedException(); }
		}

		protected override ILgWritingSystemFactory WritingSystemFactory
		{
			get
			{
				if (base.WritingSystemFactory == null)
				{
					if (m_privateWsFactory == null)
						m_privateWsFactory = new PalasoWritingSystemManager();
					return m_privateWsFactory;
				}
				return base.WritingSystemFactory;
			}
		}
	}
	#endregion

	/// --------------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for EditingHelper class.
	/// </summary>
	/// --------------------------------------------------------------------------------------------
	[TestFixture]
	public class EditingHelperTests : SelectionHelperTestsBase
	{
		#region Paste tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates a Paste operation when the clipboard contains a paragraph whose
		/// properties differ from that of the destination paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteParagraphsWithDifferentStyles()
		{
			// Add a title to the root object
			int hvoTitle = m_cache.MakeNewObject(kclsidStText, m_hvoRoot, kflidDocTitle, -2);
			int hvoTitlePara1 = m_cache.MakeNewObject(kclsidStTxtPara, hvoTitle, kflidTextParas, 0);
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			m_cache.CacheStringProp(hvoTitlePara1, kflidParaContents,
				tsStrFactory.MakeString("The First Book of the Law given by Moses", m_wsEng));
			ITsPropsFactory fact  = TsPropsFactoryClass.Create();
			m_cache.SetUnknown(hvoTitlePara1, kflidParaProperties, fact.MakeProps("Title", m_wsEng, 0));

			int hvoTitlePara2 = m_cache.MakeNewObject(kclsidStTxtPara, hvoTitle, kflidTextParas, 1);
			string secondParaContents = "and Aaron";
			m_cache.CacheStringProp(hvoTitlePara2, kflidParaContents,
				tsStrFactory.MakeString(secondParaContents, m_wsEng));
			m_cache.SetUnknown(hvoTitlePara2, kflidParaProperties, fact.MakeProps("Conclusion", m_wsEng, 0));

			ShowForm(SimpleViewVc.DisplayType.kTitle |
				SimpleViewVc.DisplayType.kUseParaProperties |
				SimpleViewVc.DisplayType.kOnlyDisplayContentsOnce);

			// Make a selection from the top of the view to the bottom.
			IVwSelection sel0 = m_basicView.RootBox.MakeSimpleSel(true, false, false, false);
			IVwSelection sel1 = m_basicView.RootBox.MakeSimpleSel(false, false, false, false);
			m_basicView.RootBox.MakeRangeSelection(sel0, sel1, true);

			// Copy the selection and then paste it at the start of the view.
			Assert.IsTrue(m_basicView.EditingHelper.CopySelection());
			// Install a simple selection at the start of the view.
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			// This is an illegal paste, so the paste will fail.
			m_basicView.EditingHelper.PasteClipboard();

			// We expect the contents to remain unchanged.
			Assert.AreEqual(2, m_cache.get_VecSize(hvoTitle, kflidTextParas));
			Assert.IsNull(m_basicView.RequestedSelectionAtEndOfUow);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Simulates a Paste operation when the clipboard contains a paragraph whose
		/// properties differ from that of the destination paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteParagraphsWithSameStyle()
		{
			// Add a title to the root object
			int hvoTitle = m_cache.MakeNewObject(kclsidStText, m_hvoRoot, kflidDocTitle, -2);
			int hvoTitlePara1 = m_cache.MakeNewObject(kclsidStTxtPara, hvoTitle, kflidTextParas, 0);
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			m_cache.CacheStringProp(hvoTitlePara1, kflidParaContents,
				tsStrFactory.MakeString("The First Book of the Law given by Moses", m_wsEng));
			ITsPropsFactory fact = TsPropsFactoryClass.Create();
			m_cache.SetUnknown(hvoTitlePara1, kflidParaProperties, fact.MakeProps("Title", m_wsEng, 0));

			int hvoTitlePara2 = m_cache.MakeNewObject(kclsidStTxtPara, hvoTitle, kflidTextParas, 1);
			string secondParaContents = "and Aaron";
			m_cache.CacheStringProp(hvoTitlePara2, kflidParaContents,
				tsStrFactory.MakeString(secondParaContents, m_wsEng));
			m_cache.SetUnknown(hvoTitlePara2, kflidParaProperties, fact.MakeProps("Title", m_wsEng, 0));

			ShowForm(SimpleViewVc.DisplayType.kTitle |
				SimpleViewVc.DisplayType.kUseParaProperties |
				SimpleViewVc.DisplayType.kOnlyDisplayContentsOnce);

			// Make a selection from the top of the view to the bottom.
			IVwSelection sel0 = m_basicView.RootBox.MakeSimpleSel(true, false, false, false);
			IVwSelection sel1 = m_basicView.RootBox.MakeSimpleSel(false, false, false, false);
			m_basicView.RootBox.MakeRangeSelection(sel0, sel1, true);

			// Copy the selection and then paste it at the start of the view.
			Assert.IsTrue(m_basicView.EditingHelper.CopySelection());
			// Install a simple selection at the start of the view.
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);

			// This is a legal paste.
			m_basicView.EditingHelper.PasteClipboard();

			// We expect the contents to change.
			Assert.AreEqual(4, m_cache.get_VecSize(hvoTitle, kflidTextParas));
			Assert.AreEqual(hvoTitlePara2 + 1, m_cache.get_VecItem(hvoTitle, kflidTextParas, 0));
			Assert.AreEqual(hvoTitlePara2 + 2, m_cache.get_VecItem(hvoTitle, kflidTextParas, 1));
			Assert.AreEqual(hvoTitlePara1, m_cache.get_VecItem(hvoTitle, kflidTextParas, 2));
			Assert.AreEqual(hvoTitlePara2, m_cache.get_VecItem(hvoTitle, kflidTextParas, 3));

			Assert.IsNotNull(m_basicView.RequestedSelectionAtEndOfUow);
			// WANTTESTPORT: (Common) FWR-1649 Check properties of RequestedSelectionAtEndOfUow
		}
		#endregion

		#region GoToNextPara tests
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from first instance (CPropPrev == 0) to
		/// second instance (CPropPrev == 1) of the same paragraph's contents.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_NextInstanceOfSameParaContents()
		{
			ShowForm(Lng.English, SimpleViewVc.DisplayType.kNormal |
				SimpleViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 0, 0, 1, 6, 6, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the next paragraph.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				2, 0, 0, false, 2, kflidDocFootnotes, 0, 0,
				SimpleRootsiteTestsBase.kflidTextParas, 0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// text.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_NextText()
		{
			ShowForm(Lng.English, SimpleViewVc.DisplayType.kNormal |
				SimpleViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 1, 0, 2, 6, 6, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the next paragraph.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 2, kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsBase.kflidTextParas, 0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// text, which is the next property (flid) being displayed for the same object.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_NextFlid()
		{
			// Add a title to the root object
			int hvoTitle = m_cache.MakeNewObject(kclsidStText, m_hvoRoot, kflidDocTitle, -2);
			int hvoTitlePara = m_cache.MakeNewObject(kclsidStTxtPara, hvoTitle, kflidTextParas, 0);
			ITsStrFactory tsStrFactory = TsStrFactoryClass.Create();
			m_cache.CacheStringProp(hvoTitlePara, kflidParaContents,
				tsStrFactory.MakeString("The First Book of the Law given by Moses", m_wsFrn));

			ShowForm(Lng.English, SimpleViewVc.DisplayType.kNormal |
				SimpleViewVc.DisplayType.kTitle);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			// Set the IP at the beginning of the only (0th) instance of the only (0th) paragraph
			// of the only (0th) instance of the second (1th) footnote of the book we're displaying.
			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 0, 0, 2, 0, 0, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the book title.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 2, kflidDocTitle, 0, 0,
				SimpleRootsiteTestsBase.kflidTextParas, 0, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when going from the last instance of the last
		/// paragraph in an StText to the first instance of the first paragraph in the next
		/// object (at a different level in the hierarchy).
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_FirstFlidInNextObject()
		{
			ShowForm(Lng.English, SimpleViewVc.DisplayType.kFootnoteDetailsSeparateParas);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 0, 0, 0, 0, 0, 0, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the second footnote's marker.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 1, -1, -1, -1, kflidDocFootnotes, 0, 1);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when starting in the last paragraph in the view --
		/// nothing should happen.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_LastParaInView()
		{
			ShowForm(Lng.English, SimpleViewVc.DisplayType.kNormal |
				SimpleViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);
			SetSelection(0, 1, 1, 0, 2, 6, 0, true);
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			Assert.IsTrue(m_basicView.IsSelectionVisible(null), "Selection is not visible");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be unchanged.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsTrue(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				2, 6, 0, true, 2, kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsBase.kflidTextParas, 1, 0);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.End, selectionHelper, 0,
				2, 0, 0, false, 2, kflidDocFootnotes, 0, 1,
				SimpleRootsiteTestsBase.kflidTextParas, 1, 0);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Test the GoToNextPara method, when starting with a range selection covering more
		/// than one paragraph. Selection should be at the start of the second (relative to
		/// top, not anchor) paragraph in the selected range. (This is analogous to what Excel
		/// does when you have a range of cells selected and you press Enter.)
		/// </summary>
		/// -----------------------------------------------------------------------------------
		[Test]
		public void GoToNextPara_MultiParaRangeSelection()
		{
			ShowForm(Lng.English, SimpleViewVc.DisplayType.kNormal |
				SimpleViewVc.DisplayType.kDuplicateParagraphs);
			m_basicView.Show();
			m_basicView.RefreshDisplay();

			m_SelectionHelper = new DummySelectionHelper(null, m_basicView);

			// Make a bottom-up selection, just to be sure we're not using the anchor instead of
			// the top.
			SetSelection(0, 0, 0, 0, 2, 1, 1, false); // Set end
			SetSelection(0, 1, 0, 0, 1, 12, 12, true); // Set anchor
			IVwSelection vwsel = m_SelectionHelper.SetSelection(true);
			Assert.IsNotNull(vwsel, "No selection made");
			m_basicView.EditingHelper.GoToNextPara();

			// We expect that the selection will be at the start of the second paragraph in
			// the selected range.
			SelectionHelper selectionHelper = SelectionHelper.GetSelectionInfo(null, m_basicView);
			Assert.IsFalse(selectionHelper.IsRange);
			CheckSelectionHelperValues(SelectionHelper.SelLimitType.Anchor, selectionHelper, 0,
				0, 0, 0, false, 2, kflidDocFootnotes, 0, 0,
				SimpleRootsiteTestsBase.kflidTextParas, 1, 0);
		}
		#endregion
	}

	/// --------------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for EditingHelper class that test code that deals with the clipboard.
	/// </summary>
	/// <remarks>This base class uses a clipboard stub.</remarks>
	/// --------------------------------------------------------------------------------------------
	[TestFixture]
	public class EditingHelperTests_Clipboard : SimpleRootsiteTestsBase
	{
		protected virtual void SetClipboardAdapter()
		{
			ClipboardUtils.Manager.SetClipboardAdapter(new ClipboardStub());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setup
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			SetClipboardAdapter();
			ClipboardUtils.SetDataObject(new DataObject());
			ClipboardUtils.SetText("I want a monkey");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting Unicode characters from the clipboard (TE-4633)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void PasteUnicode()
		{
			ClipboardUtils.SetText("\u091C\u092E\u094D\u200D\u092E\u0947\u0906",
				TextDataFormat.UnicodeText);

			using (var editingHelper = new DummyEditingHelper())
			{
				ITsString str = editingHelper.CallGetTextFromClipboard();

				Assert.AreEqual("\u091C\u092E\u094D\u200D\u092E\u0947\u0906", str.Text);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests setting and retrieving data on/from the clipboard.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetSetClipboard()
		{
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;

			IWritingSystem swgWs;
			wsManager.GetOrSet("swg", out swgWs);
			int wsSwg = swgWs.Handle;

			var strFactory = TsStrFactoryClass.Create();
			var incStrBldr = strFactory.GetIncBldr();
			incStrBldr.AppendTsString(strFactory.MakeString("Gogomer ", wsSwg));
			incStrBldr.AppendTsString(strFactory.MakeString("cucumber", wsEng));
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
			var wsManager = new PalasoWritingSystemManager();
			IWritingSystem enWs;
			wsManager.GetOrSet("en", out enWs);
			int wsEng = enWs.Handle;
			var strFactory = TsStrFactoryClass.Create();
			var originalInput = "\x7a7a\x60f3\x79d1\x5b78\x0020\xd558";
			var input = originalInput.Normalize(NormalizationForm.FormD);
			Assert.That(originalInput, Is.Not.EqualTo(input)); // make sure input is NOT NFC
			var tss = strFactory.MakeString(input, wsEng);
			EditingHelper.SetTsStringOnClipboard(tss, false, wsManager);
			var newDataObj = ClipboardUtils.GetDataObject();
			Assert.IsNotNull(newDataObj, "Couldn't get DataObject from clipboard");
			Assert.AreEqual(originalInput, newDataObj.GetData("Text"));
		}
	}

	/// --------------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for EditingHelper class that test code that deals with the clipboard.
	/// </summary>
	/// <remarks>This derived class uses the real clipboard.</remarks>
	/// --------------------------------------------------------------------------------------------
	[TestFixture]
	public class EditingHelperTests_ClipboardReal: EditingHelperTests_Clipboard
	{
		protected override void SetClipboardAdapter()
		{
			// do nothing so that we get the default (system) clipboard
		}
	}
}
