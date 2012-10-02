// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StVcTests.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Summary description for StVcTests.
	/// </summary>
	[TestFixture]
	public class StVcTests : InDatabaseFdoTestBase
	{
		private IScripture m_scr;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		public override void Initialize()
		{
			base.Initialize();
			m_scr = m_fdoCache.LangProject.TranslatedScriptureOA;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the footnote marker from an associated guid.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetFootnoteMarkerFromGuid()
		{
			StFootnote footnote = new StFootnote();
			m_scr.ScriptureBooksOS[0].FootnotesOS.Append(footnote);
			footnote.FootnoteMarker.Text = "a";

			Guid guid = m_fdoCache.GetGuidFromId(footnote.Hvo);

			// Add the guid property so we can get it out as a string.
			ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
			byte[] objData = MiscUtils.GetObjData(guid, 1);
			propsBldr.SetStrPropValueRgch(1, objData, objData.Length);

			// Get the guid property as a string.
			string sObjData;
			propsBldr.GetStrPropValue(1, out sObjData);

			using (StVc vc = new StVc())
			{
				vc.Cache = m_fdoCache;
				vc.DefaultWs = m_fdoCache.LanguageWritingSystemFactoryAccessor.UserWs;
				ITsString footnoteMarker = vc.GetStrForGuid(sObjData.Substring(1));

				Assert.AreEqual("2", footnoteMarker.Text);
			}
		}

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
		private class DummyFootnoteVc: VwBaseVc
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
			public DummyFootnoteVc(FdoCache cache)
			{
				m_stvc = new StVc();
				m_stvc.Cache = cache;
				m_stvc.DefaultWs = cache.LanguageWritingSystemFactoryAccessor.UserWs;
			}

			#region IDisposable override

			/// <summary>
			/// Executes in two distinct scenarios.
			///
			/// 1. If disposing is true, the method has been called directly
			/// or indirectly by a user's code via the Dispose method.
			/// Both managed and unmanaged resources can be disposed.
			///
			/// 2. If disposing is false, the method has been called by the
			/// runtime from inside the finalizer and you should not reference (access)
			/// other managed objects, as they already have been garbage collected.
			/// Only unmanaged resources can be disposed.
			/// </summary>
			/// <param name="disposing"></param>
			/// <remarks>
			/// If any exceptions are thrown, that is fine.
			/// If the method is being done in a finalizer, it will be ignored.
			/// If it is thrown by client code calling Dispose,
			/// it needs to be handled by fixing the bug.
			///
			/// If subclasses override this method, they should call the base implementation.
			/// </remarks>
			protected override void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (IsDisposed)
					return;

				if (disposing)
				{
					// Dispose managed resources here.
					if (m_stvc != null)
						m_stvc.Dispose();
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_stvc = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

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
				CheckDisposed();

				switch (frag)
				{
					case (int)FootnoteFrags.kfrScripture:
					{
						vwenv.AddLazyVecItems((int)Scripture.ScriptureTags.kflidScriptureBooks,
							this, (int)FootnoteFrags.kfrBook);
						break;
					}
					case (int)FootnoteFrags.kfrBook:
					{
						vwenv.AddObjVecItems((int)ScrBook.ScrBookTags.kflidFootnotes, m_stvc,
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
				CheckDisposed();

				switch ((FootnoteFrags)frag)
				{
					default:
						return 400;
				}
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Load data needed to display the specified objects using the specified fragment.
			/// This is called before attempting to Display an item that has been listed for lazy
			/// display using AddLazyItems. It may be used to load the necessary data into the
			/// DataAccess object.
			/// </summary>
			/// <param name="vwenv"></param>
			/// <param name="rghvo"></param>
			/// <param name="chvo"></param>
			/// <param name="hvoParent"></param>
			/// <param name="tag"></param>
			/// <param name="frag"></param>
			/// <param name="ihvoMin"></param>
			/// ------------------------------------------------------------------------------------
			public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag,
				int frag, int ihvoMin)
			{
				CheckDisposed();

				try
				{
					switch ((FootnoteFrags)frag)
					{
						case FootnoteFrags.kfrBook:
						{
							ScrBook scrBook;

							foreach (int hvo in rghvo)
							{
								try
								{
									scrBook = new ScrBook(m_stvc.Cache, hvo);
									foreach (StFootnote stFootnote in scrBook.FootnotesOS)
									{
										foreach (StTxtPara stPara in stFootnote.ParagraphsOS)
										{
											string text = stPara.Contents.Text;
											//Debug.WriteLine("LDF fn: " + text);
										}
									}
								}
								catch(Exception e)
								{
									Debug.WriteLine("Got exception while loading footnotes for book: " + e.Message);
									throw;
								}
							}

							break;
						}
						default:
							Debug.Assert(false);
							break;
					}
				}
				catch(Exception e)
				{
					Debug.WriteLine("Got exception in LoadDataFor: " + e.Message);
					throw;
				}
			}
			#endregion

			public bool DisplayTranslation
			{
				get
				{
					CheckDisposed();
					return m_stvc.DisplayTranslation;
				}
				set
				{
					CheckDisposed();
					m_stvc.DisplayTranslation = value;
				}
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

			#region IDisposable override

			/// <summary>
			/// Executes in two distinct scenarios.
			///
			/// 1. If disposing is true, the method has been called directly
			/// or indirectly by a user's code via the Dispose method.
			/// Both managed and unmanaged resources can be disposed.
			///
			/// 2. If disposing is false, the method has been called by the
			/// runtime from inside the finalizer and you should not reference (access)
			/// other managed objects, as they already have been garbage collected.
			/// Only unmanaged resources can be disposed.
			/// </summary>
			/// <param name="disposing"></param>
			/// <remarks>
			/// If any exceptions are thrown, that is fine.
			/// If the method is being done in a finalizer, it will be ignored.
			/// If it is thrown by client code calling Dispose,
			/// it needs to be handled by fixing the bug.
			///
			/// If subclasses override this method, they should call the base implementation.
			/// </remarks>
			protected override void Dispose(bool disposing)
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (IsDisposed)
					return;

				base.Dispose(disposing);

				if (disposing)
				{
					// Dispose managed resources here.
					if (m_footnoteVc != null)
						m_footnoteVc.Dispose();
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_footnoteVc = null;
			}

			#endregion IDisposable override

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

				m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
				m_rootb.SetRootObject(m_fdoCache.LangProject.TranslatedScriptureOAHvo,
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that footnote marker is separated from footnote text by a read-only space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SpaceAfterFootnoteMarker()
		{
			// Prepare the test by creating a footnote view
			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(m_fdoCache, m_scr.Hvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			using (DummyFootnoteView footnoteView = new DummyFootnoteView(m_fdoCache))
			{
				footnoteView.StyleSheet = styleSheet;
				footnoteView.Visible = false;

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
					(int)StTxtPara.StTxtParaTags.kflidContents);
				selHelper.SetSelection(true);

				// Now the real test:
				IVwSelection sel = footnoteView.RootBox.Selection;
				ITsString tss;
				sel.GetSelectionString(out tss, string.Empty);
				Assert.AreEqual("a ", tss.Text.Substring(0, 2));

				//				// make sure the marker and the space are read-only and the paragraph not.
				//				ITsTextProps[] vttp;
				//				IVwPropertyStore[] vvps;
				//				int cttp;
				//				SelectionHelper.GetSelectionProps(sel, out vttp, out vvps, out cttp);
				//				Assert.IsTrue(cttp >= 3);
				//				Assert.IsFalse(SelectionHelper.IsEditable(vttp[0], vvps[0]),
				//					"Footnote marker is not read-only");
				//				Assert.IsFalse(SelectionHelper.IsEditable(vttp[1], vvps[1]),
				//					"Space after marker is not read-only");
				//				Assert.IsTrue(SelectionHelper.IsEditable(vttp[2], vvps[2]),
				//					"Footnote text is read-only");
				//				Assert.IsTrue(SelectionHelper.IsEditable(vttp[3], vvps[3]),
				//					"Footnote text is read-only");
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
			IScrBook book = m_scr.ScriptureBooksOS[1]; // book of James
			IStFootnote footnote = book.FootnotesOS[0];
			StTxtPara para = (StTxtPara)footnote.ParagraphsOS[0];

			// add a translation to the footnote
			ICmTranslation translation = para.GetOrCreateBT();
			translation.Translation.AnalysisDefaultWritingSystem.UnderlyingTsString =
				TsStringHelper.MakeTSS("abcde", m_fdoCache.DefaultAnalWs);

			FwStyleSheet styleSheet = new FwStyleSheet();
			styleSheet.Init(m_fdoCache, m_scr.Hvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			// Prepare the test by creating a footnote view
			using (DummyFootnoteView footnoteView = new DummyFootnoteView(m_fdoCache, true))
			{
				footnoteView.StyleSheet = styleSheet;
				footnoteView.Visible = false;

				// We don't actually want to show it, but we need to force the view to create the root
				// box and lay it out so that various test stuff can happen properly.
				footnoteView.MakeRoot();
				footnoteView.CallLayout();

				// Select the footnote marker and some characters of the footnote paragraph
				footnoteView.RootBox.MakeSimpleSel(true, true, false, true);
				SelectionHelper selHelper = SelectionHelper.GetSelectionInfo(null, footnoteView);

				// Now the real test:
				IVwSelection sel = footnoteView.RootBox.Selection.GrowToWord();
				ITsString tss;
				sel.GetSelectionString(out tss, string.Empty);
				Assert.AreEqual("abcde", tss.Text);
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
			styleSheet.Init(m_fdoCache, m_scr.Hvo, (int)Scripture.ScriptureTags.kflidStyles);

			using (Form form = new Form())
			using (DummyFootnoteView footnoteView = new DummyFootnoteView(m_fdoCache))
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
						(int)StTxtPara.StTxtParaTags.kflidContents);
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
