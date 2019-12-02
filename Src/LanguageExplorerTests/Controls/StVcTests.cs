// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using LanguageExplorer.Controls;
using LanguageExplorer.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.Controls
{
	/// <summary />
	[TestFixture]
	public class StVcTests : ScrInMemoryLcmTestBase
	{
		/// <summary />
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// Set a vern ws.
			CoreWritingSystemDefinition french;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out french);
			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(french);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(french);
				// The test want two books.
				var gen = AddBookToMockedScripture(1, "GEN");
				AddTitleToMockedBook(gen, "This is Genesis");
				var exo = AddBookToMockedScripture(2, "EXO");
				AddTitleToMockedBook(exo, "This is Exodus");
			});
		}

		/// <summary>
		/// Tests getting the footnote marker from an associated guid using the GetStrForGuid
		/// method. The string returned should be an ORC with the correct properties for showing
		/// a "hot" footnote icon.
		/// </summary>
		[Test]
		public void GetFootnoteMarkerFromGuid()
		{
			var footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_scr.ScriptureBooksOS[0].FootnotesOS.Add(footnote);
			footnote.FootnoteMarker = TsStringUtils.MakeString("a", Cache.WritingSystemFactory.GetWsFromStr("en"));

			// Add the guid property so we can get it out as a string.
			var propsBldr = TsStringUtils.MakePropsBldr();
			var objData = TsStringUtils.GetObjData(footnote.Guid, FwObjDataTypes.kodtPictEvenHot);
			propsBldr.SetStrPropValueRgch(1, objData, objData.Length);

			// Get the guid property as a string.
			var sObjData = propsBldr.GetStrPropValue(1);

			var vc = new StVc(Cache.WritingSystemFactory.UserWs) { Cache = Cache };
			var footnoteMarker = vc.GetStrForGuid(sObjData.Substring(1));

			// Call to GetStrForGuid doesn't generate a footnote marker based on the
			// Scripture Footnote properties since the method to do that is in TeStVc.
			int dummy;
			Assert.AreEqual(StringUtils.kszObject, footnoteMarker.Text);
			var props = footnoteMarker.get_Properties(0);
			Assert.AreEqual(2, props.IntPropCount);
			Assert.AreEqual(Cache.DefaultUserWs, props.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy));
			Assert.AreEqual(0, props.GetIntPropValues((int)FwTextPropType.ktptEditable, out dummy));
			Assert.AreEqual(1, props.StrPropCount);

			FwObjDataTypes odt;
			var footnoteGuid = TsStringUtils.GetGuidFromProps(props, null, out odt);
			Assert.IsTrue(odt == FwObjDataTypes.kodtPictEvenHot || odt == FwObjDataTypes.kodtPictOddHot);
			Assert.AreEqual(footnote.Guid, footnoteGuid);
		}

		/// <summary>
		/// Tests that footnote marker is separated from footnote text by a read-only space.
		/// </summary>
		[Test]
		public void SpaceAfterFootnoteMarker()
		{
			var book = m_scr.ScriptureBooksOS[0];
			var footnote = AddFootnote(book, (IStTxtPara)book.TitleOA.ParagraphsOS[0], 0, "This is a footnote");
			footnote.FootnoteMarker = TsStringUtils.MakeString("a", Cache.WritingSystemFactory.GetWsFromStr("en"));
			// Prepare the test by creating a footnote view
			var styleSheet = new LcmStyleSheet();
			styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);

			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (var footnoteView = new DummyFootnoteView(Cache))
				{
					footnoteView.StyleSheet = styleSheet;
					footnoteView.Visible = false;
					footnoteView.InitializeFlexComponent(flexComponentParameters);

					// We don't actually want to show it, but we need to force the view to create the root
					// box and lay it out so that various test stuff can happen properly.
					footnoteView.MakeRoot();
					footnoteView.CallLayout();

					// Select the footnote marker and some characters of the footnote paragraph
					footnoteView.RootBox.MakeSimpleSel(true, false, false, true);
					var selHelper = SelectionHelper.GetSelectionInfo(null, footnoteView);
					selHelper.IchAnchor = 0;
					selHelper.IchEnd = 5;
					var selLevInfo = new SelLevInfo[3];
					Assert.AreEqual(4, selHelper.GetNumberOfLevels(SelLimitType.End));
					Array.Copy(selHelper.GetLevelInfo(SelLimitType.End), 1, selLevInfo, 0, 3);
					selHelper.SetLevelInfo(SelLimitType.End, selLevInfo);
					selHelper.SetTextPropId(SelLimitType.End, StTxtParaTags.kflidContents);
					selHelper.SetSelection(true);

					// Now the real test:
					var sel = footnoteView.RootBox.Selection;
					ITsString tss;
					sel.GetSelectionString(out tss, string.Empty);
					Assert.AreEqual("a ", tss.Text.Substring(0, 2));

					// make sure the marker and the space are read-only (maybe have to select each run
					// separately to make this test truly correct)
					ITsTextProps[] vttp;
					IVwPropertyStore[] vvps;
					int cttp;
					SelectionHelper.GetSelectionProps(sel, out vttp, out vvps, out cttp);
					Assert.IsTrue(cttp >= 2);
					Assert.IsFalse(SelectionHelper.IsEditable(vttp[0], vvps[0]), "Footnote marker is not read-only");
					Assert.IsFalse(SelectionHelper.IsEditable(vttp[1], vvps[1]), "Space after marker is not read-only");
				}
			}
			finally
			{
				TestSetupServices.DisposeTrash(flexComponentParameters);
			}
		}

		/// <summary>
		/// Tests that translation of a footnote can be displayed.
		/// </summary>
		[Test]
		public void FootnoteTranslationTest()
		{
			// get an existing footnote
			var book = m_scr.ScriptureBooksOS[1]; // book of Exodus
			var footnote = AddFootnote(book, (IStTxtPara)book.TitleOA.ParagraphsOS[0], 0, "This is a footnote");
			var para = (IStTxtPara)footnote.ParagraphsOS[0];

			// add a translation to the footnote
			var translation = para.GetOrCreateBT();
			var analWs = Cache.DefaultAnalWs;
			translation.Translation.set_String(analWs, TsStringUtils.MakeString("abcde", analWs));

			var styleSheet = new LcmStyleSheet();
			styleSheet.Init(Cache, Cache.LangProject.Hvo, LangProjectTags.kflidStyles);

			// Prepare the test by creating a footnote view
			var flexComponentParameters = TestSetupServices.SetupTestTriumvirate();
			try
			{
				using (var footnoteView = new DummyFootnoteView(Cache, true))
				{
					footnoteView.StyleSheet = styleSheet;
					footnoteView.Visible = false;
					footnoteView.InitializeFlexComponent(flexComponentParameters);

					// We don't actually want to show it, but we need to force the view to create the root
					// box and lay it out so that various test stuff can happen properly.
					footnoteView.MakeRoot();
					footnoteView.CallLayout();

					// Select the footnote marker and some characters of the footnote paragraph
					footnoteView.RootBox.MakeSimpleSel(true, true, false, true);

					// Now the real test:
					var sel = footnoteView.RootBox.Selection.GrowToWord();
					ITsString tss;
					sel.GetSelectionString(out tss, string.Empty);
					Assert.AreEqual("abcde", tss.Text);
				}
			}
			finally
			{
				TestSetupServices.DisposeTrash(flexComponentParameters);
			}
		}

		/// <summary>
		/// Possible footnote fragments
		/// </summary>
		private enum FootnoteFrags
		{
			/// <summary>Scripture</summary>
			/// <remarks>debug is easier if different range from tags</remarks>
			kfrScripture = 100,
			/// <summary>A book</summary>
			kfrBook,
		}

		/// <summary>
		/// Dummy footnote view constructor.
		/// </summary>
		private sealed class DummyFootnoteVc : FwBaseVc
		{
			/// <summary>The structured text view constructor that we will use</summary>
			private StVc m_stvc;

			/// <summary />
			public DummyFootnoteVc(LcmCache cache) : base(cache.WritingSystemFactory.UserWs)
			{
				m_stvc = new StVc(cache.WritingSystemFactory.UserWs);
				m_stvc.Cache = cache;
			}

			#region Overridden methods

			/// <summary>
			/// This is the main interesting method of displaying objects and fragments of them.
			/// Scripture Footnotes are displayed by displaying each footnote's reference and text.
			/// The text is displayed using the standard view constructor for StText.
			/// </summary>
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case (int)FootnoteFrags.kfrScripture:
						{
							vwenv.AddLazyVecItems(ScriptureTags.kflidScriptureBooks,
								this, (int)FootnoteFrags.kfrBook);
							break;
						}
					case (int)FootnoteFrags.kfrBook:
						{
							vwenv.AddObjVecItems(ScrBookTags.kflidFootnotes, m_stvc,
								(int)StTextFrags.kfrFootnote);
							break;
						}
					default:
						Debug.Assert(false);
						break;
				}
			}

			/// <summary />
			public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
			{
				switch ((FootnoteFrags)frag)
				{
					default:
						return 400;
				}
			}

			#endregion

			public bool DisplayTranslation
			{
				get { return m_stvc.DisplayTranslation; }
				set { m_stvc.DisplayTranslation = value; }
			}
		}

		private sealed class DummyFootnoteView : RootSite
		{
			private DummyFootnoteVc m_footnoteVc;
			private bool m_displayTranslation;

			/// <summary />
			public DummyFootnoteView(LcmCache cache) : base(cache)
			{
			}

			/// <summary />
			public DummyFootnoteView(LcmCache cache, bool displayTranslation) : base(cache)
			{
				m_displayTranslation = displayTranslation;
			}

			/// <summary />
			public void CallLayout()
			{
				OnLayout(new LayoutEventArgs(this, string.Empty));
			}

			#region Overrides of RootSite

			/// <summary>
			/// Makes a root box and initializes it with appropriate data
			/// </summary>
			public override void MakeRoot()
			{
				if (m_cache == null || DesignMode)
				{
					return;
				}
				base.MakeRoot();
				// Set up a new view constructor.
				m_footnoteVc = new DummyFootnoteVc(m_cache);
				m_footnoteVc.DisplayTranslation = m_displayTranslation;
				RootBox.DataAccess = Cache.DomainDataByFlid;
				RootBox.SetRootObject(Cache.LanguageProject.TranslatedScriptureOA.Hvo, m_footnoteVc, (int)FootnoteFrags.kfrScripture, m_styleSheet);
				m_fRootboxMade = true;
				m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
				try
				{
					RootBox.MakeSimpleSel(true, false, false, true);
				}
				catch (COMException)
				{
					// We ignore failures since the text window may be empty, in which case making a
					// selection is impossible.
				}
			}
			#endregion
		}
	}
}