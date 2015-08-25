// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StVcTests.cs
// Responsibility: Eberhard Beilharz

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;
using SIL.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Summary description for StVcTests.
	/// </summary>
	[TestFixture]
	public class StVcTests : ScrInMemoryFdoTestBase
	{
		#region Dummy Footnote view
		#region Footnote fragments
		///  ----------------------------------------------------------------------------------------
		/// <summary>
		/// Possible footnote fragments
		/// </summary>
		///  ----------------------------------------------------------------------------------------
		private enum FootnoteFrags: int
		{
			/// <summary>Scripture</summary>
			kfrScripture = 100, // debug is easier if different range from tags
			/// <summary>A book</summary>
			kfrBook,
		};
		#endregion

		#region FootnoteVc
		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Dummy footnote view constructor - copy of the one in TE. Because of dependencies we
		/// can't use that one directly.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		private class DummyFootnoteVc: FwBaseVc
		{
			#region Variables
			/// <summary>The structured text view constructor that we will use</summary>
			protected StVc m_stvc;
			#endregion

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the FootnoteVc class
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public DummyFootnoteVc(FdoCache cache) : base(cache.WritingSystemFactory.UserWs)
			{
				m_stvc = new StVc(cache.WritingSystemFactory.UserWs);
				m_stvc.Cache = cache;
			}

			#region Overridden methods
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This is the main interesting method of displaying objects and fragments of them.
			/// Scripture Footnotes are displayed by displaying each footnote's reference and text.
			/// The text is displayed using the standard view constructor for StText.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="hvo"></param>
			/// <param name="frag"></param>
			/// ------------------------------------------------------------------------------------
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

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// This routine is used to estimate the height of an item. The item will be one of
			/// those you have added to the environment using AddLazyItems. Note that the calling
			/// code does NOT ensure that data for displaying the item in question has been loaded.
			/// The first three arguments are as for Display, that is, you are being asked to
			/// estimate how much vertical space is needed to display this item in the available width.
			/// </summary>
			/// <param name="hvo"></param>
			/// <param name="frag"></param>
			/// <param name="dxAvailWidth"></param>
			/// <returns>Height of an item</returns>
			/// ------------------------------------------------------------------------------------
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
		#endregion

		#region Dummy footnote view
		private class DummyFootnoteView : RootSite
		{
			private DummyFootnoteVc m_footnoteVc;
			private bool m_displayTranslation = false;
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// C'tor
			/// </summary>
			/// <param name="cache"></param>
			/// --------------------------------------------------------------------------------
			public DummyFootnoteView(FdoCache cache) : base(cache)
			{
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// C'tor
			/// </summary>
			/// <param name="cache"></param>
			/// <param name="displayTranslation">true if translation should be displayed</param>
			/// --------------------------------------------------------------------------------
			public DummyFootnoteView(FdoCache cache, bool displayTranslation) : base(cache)
			{
				m_displayTranslation = displayTranslation;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Call the OnLayout methods
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public void CallLayout()
			{
				CheckDisposed();

				OnLayout(new LayoutEventArgs(this, string.Empty));
			}

			#region Overrides of RootSite
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Makes a root box and initializes it with appropriate data
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public override void MakeRoot()
			{
				CheckDisposed();

				if (m_fdoCache == null || DesignMode)
					return;

				base.MakeRoot();

				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);

				// Set up a new view constructor.
				m_footnoteVc = new DummyFootnoteVc(m_fdoCache);
				m_footnoteVc.DisplayTranslation = m_displayTranslation;

				m_rootb.DataAccess = Cache.DomainDataByFlid;
				m_rootb.SetRootObject(Cache.LanguageProject.TranslatedScriptureOA.Hvo,
					m_footnoteVc, (int)FootnoteFrags.kfrScripture, m_styleSheet);

				m_fRootboxMade = true;
				m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

				try
				{
					m_rootb.MakeSimpleSel(true, false, false, true);
				}
				catch(COMException)
				{
					// We ignore failures since the text window may be empty, in which case making a
					// selection is impossible.
				}
			}
			#endregion
		}
		#endregion

		#endregion

		/// <summary>
		/// Set up test fixture.
		/// </summary>
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			// Set a vern ws.
			IWritingSystem french;
			Cache.ServiceLocator.WritingSystemManager.GetOrSet("fr", out french);

			NonUndoableUnitOfWorkHelper.Do(m_actionHandler, () =>
			{
				Cache.ServiceLocator.WritingSystems.VernacularWritingSystems.Add(french);
				Cache.ServiceLocator.WritingSystems.CurrentVernacularWritingSystems.Add(french);

				// The test want two books.
				IScrBook gen = AddBookToMockedScripture(1, "GEN");
				AddTitleToMockedBook(gen, "This is Genesis");
				IScrBook exo = AddBookToMockedScripture(2, "EXO");
				AddTitleToMockedBook(exo, "This is Exodus");
			});
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests getting the footnote marker from an associated guid using the GetStrForGuid
		/// method. The string returned should be an ORC with the correct properties for showing
		/// a "hot" footnote icon.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnoteMarkerFromGuid()
		{
			IScrFootnote footnote = Cache.ServiceLocator.GetInstance<IScrFootnoteFactory>().Create();
			m_scr.ScriptureBooksOS[0].FootnotesOS.Add(footnote);
			footnote.FootnoteMarker = Cache.TsStrFactory.MakeString("a", Cache.WritingSystemFactory.GetWsFromStr("en"));

			// Add the guid property so we can get it out as a string.
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			byte[] objData = TsStringUtils.GetObjData(footnote.Guid, 1);
			propsBldr.SetStrPropValueRgch(1, objData, objData.Length);

			// Get the guid property as a string.
			string sObjData;
			propsBldr.GetStrPropValue(1, out sObjData);

			StVc vc = new StVc(Cache.WritingSystemFactory.UserWs) { Cache = Cache };
			ITsString footnoteMarker = vc.GetStrForGuid(sObjData.Substring(1));

			// Call to GetStrForGuid doesn't generate a footnote marker based on the
			// Scripture Footnote properties since the method to do that is in TeStVc.
			int dummy;
			Assert.AreEqual(StringUtils.kszObject, footnoteMarker.Text);
			ITsTextProps props = footnoteMarker.get_Properties(0);
			Assert.AreEqual(2, props.IntPropCount);
			Assert.AreEqual(Cache.DefaultUserWs, props.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy));
			Assert.AreEqual(0, props.GetIntPropValues((int)FwTextPropType.ktptEditable, out dummy));
			Assert.AreEqual(1, props.StrPropCount);

			FwObjDataTypes odt;
			Guid footnoteGuid = TsStringUtils.GetGuidFromProps(props, null, out odt);
			Assert.IsTrue(odt == FwObjDataTypes.kodtPictEvenHot || odt == FwObjDataTypes.kodtPictOddHot);
			Assert.AreEqual(footnote.Guid, footnoteGuid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that footnote marker is separated from footnote text by a read-only space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SpaceAfterFootnoteMarker()
		{
			IScrBook book = m_scr.ScriptureBooksOS[0];
			IScrFootnote footnote = AddFootnote(book, (IStTxtPara)book.TitleOA.ParagraphsOS[0], 0, "This is a footnote");
			footnote.FootnoteMarker = Cache.TsStrFactory.MakeString("a", Cache.WritingSystemFactory.GetWsFromStr("en"));
			// Prepare the test by creating a footnote view
			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			{
				using (DummyFootnoteView footnoteView = new DummyFootnoteView(Cache))
				{
					footnoteView.StyleSheet = styleSheet;
					footnoteView.Visible = false;
					footnoteView.InitializeFlexComponent(propertyTable, publisher, subscriber);

					// We don't actually want to show it, but we need to force the view to create the root
					// box and lay it out so that various test stuff can happen properly.
					footnoteView.MakeRoot();
					footnoteView.CallLayout();

					// Select the footnote marker and some characters of the footnote paragraph
					footnoteView.RootBox.MakeSimpleSel(true, false, false, true);
					SelectionHelper selHelper = SelectionHelper.GetSelectionInfo(null, footnoteView);
					selHelper.IchAnchor = 0;
					selHelper.IchEnd = 5;
					SelLevInfo[] selLevInfo = new SelLevInfo[3];
					Assert.AreEqual(4, selHelper.GetNumberOfLevels(SelectionHelper.SelLimitType.End));
					Array.Copy(selHelper.GetLevelInfo(SelectionHelper.SelLimitType.End), 1, selLevInfo, 0, 3);
					selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selLevInfo);
					selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
						StTxtParaTags.kflidContents);
					selHelper.SetSelection(true);

					// Now the real test:
					IVwSelection sel = footnoteView.RootBox.Selection;
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
					Assert.IsFalse(SelectionHelper.IsEditable(vttp[0], vvps[0]),
						"Footnote marker is not read-only");
					Assert.IsFalse(SelectionHelper.IsEditable(vttp[1], vvps[1]),
						"Space after marker is not read-only");
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that translation of a footnote can be displayed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void FootnoteTranslationTest()
		{
			// get an existing footnote
			IScrBook book = m_scr.ScriptureBooksOS[1]; // book of Exodus
			IScrFootnote footnote = AddFootnote(book, (IStTxtPara)book.TitleOA.ParagraphsOS[0], 0, "This is a footnote");
			IStTxtPara para = (IStTxtPara)footnote.ParagraphsOS[0];

			// add a translation to the footnote
			ICmTranslation translation = para.GetOrCreateBT();
			int analWs = Cache.DefaultAnalWs;
			translation.Translation.set_String(analWs, TsStringHelper.MakeTSS("abcde", analWs));

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			// Prepare the test by creating a footnote view
			IPublisher publisher;
			ISubscriber subscriber;
			PubSubSystemFactory.CreatePubSubSystem(out publisher, out subscriber);
			using (var propertyTable = PropertyTableFactory.CreatePropertyTable(publisher))
			{
				using (DummyFootnoteView footnoteView = new DummyFootnoteView(Cache, true))
				{
					footnoteView.StyleSheet = styleSheet;
					footnoteView.Visible = false;
					footnoteView.InitializeFlexComponent(propertyTable, publisher, subscriber);

					// We don't actually want to show it, but we need to force the view to create the root
					// box and lay it out so that various test stuff can happen properly.
					footnoteView.MakeRoot();
					footnoteView.CallLayout();

					// Select the footnote marker and some characters of the footnote paragraph
					footnoteView.RootBox.MakeSimpleSel(true, true, false, true);

					// Now the real test:
					IVwSelection sel = footnoteView.RootBox.Selection.GrowToWord();
					ITsString tss;
					sel.GetSelectionString(out tss, string.Empty);
					Assert.AreEqual("abcde", tss.Text);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that footnote marker is separated from footnote text by a read-only space.
		/// NOTE: once this is working it should be included in the test above. We split it
		/// because we couldn't get it to work.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("TE-932: We get read-only for everything. JohnT, could you please look into this?")]
		public void ReadOnlySpaceAfterFootnoteMarker()
		{
			// Prepare the test by creating a footnote view
			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);

			using (Form form = new Form())
			using (DummyFootnoteView footnoteView = new DummyFootnoteView(Cache))
			{
				footnoteView.StyleSheet = styleSheet;
				footnoteView.Dock = DockStyle.Fill;
				footnoteView.Name = "footnoteView";
				footnoteView.Visible = true;
				form.Controls.Add(footnoteView);
				form.Show();

				try
				{
					// Select the footnote marker and some characters of the footnote paragraph
					footnoteView.RootBox.MakeSimpleSel(true, false, false, true);
					SelectionHelper selHelper = SelectionHelper.GetSelectionInfo(null, footnoteView);
					selHelper.IchAnchor = 0;
					selHelper.IchEnd = 5;
					SelLevInfo[] selLevInfo = new SelLevInfo[3];
					Assert.AreEqual(4, selHelper.GetNumberOfLevels(SelectionHelper.SelLimitType.End));
					Array.Copy(selHelper.GetLevelInfo(SelectionHelper.SelLimitType.End), 1, selLevInfo, 0, 3);
					selHelper.SetLevelInfo(SelectionHelper.SelLimitType.End, selLevInfo);
					selHelper.SetTextPropId(SelectionHelper.SelLimitType.End,
						StTxtParaTags.kflidContents);
					selHelper.SetSelection(true);

					// Now the real test:
					IVwSelection sel = footnoteView.RootBox.Selection;
					ITsString tss;
					sel.GetSelectionString(out tss, string.Empty);
					Assert.AreEqual("a ", tss.Text.Substring(0, 2));

					// make sure the marker and the space are read-only and the paragraph not.
					ITsTextProps[] vttp;
					IVwPropertyStore[] vvps;
					int cttp;
					SelectionHelper.GetSelectionProps(sel, out vttp, out vvps, out cttp);
					Assert.IsTrue(cttp >= 3);
					Assert.IsFalse(SelectionHelper.IsEditable(vttp[0], vvps[0]),
						"Footnote marker is not read-only");
					Assert.IsFalse(SelectionHelper.IsEditable(vttp[1], vvps[1]),
						"Space after marker is not read-only");
					Assert.IsTrue(SelectionHelper.IsEditable(vttp[2], vvps[2]),
						"Footnote text is read-only");
					Assert.IsTrue(SelectionHelper.IsEditable(vttp[3], vvps[3]),
						"Footnote text is read-only");
				}
				finally
				{
					form.Close();
				}
			}
		}
	}
}
