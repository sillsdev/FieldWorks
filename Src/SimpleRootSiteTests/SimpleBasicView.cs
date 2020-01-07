// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using FieldWorks.TestUtilities;
using NUnit.Framework;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;

namespace SIL.FieldWorks.Common.RootSites.SimpleRootSiteTests
{
	/// <summary>
	/// Implementation of a basic view for testing, similar to DraftView
	/// </summary>
	public sealed class SimpleBasicView : SimpleRootSite
	{
		public ISilDataAccess Cache;
		/// <summary />
		private IContainer components;
		/// <summary>Text for the first and third test paragraph (English)</summary>
		internal const string kFirstParaEng = "This is the first test paragraph.";
		/// <summary>Text for the second and fourth test paragraph (English).</summary>
		/// <remarks>This text needs to be shorter than the text for the first para!</remarks>
		internal const string kSecondParaEng = "This is the 2nd test paragraph";
		internal SelectionHelper RequestedSelectionAtEndOfUow = null;

		#region Constructor, Dispose, InitializeComponent

		/// <summary />
		public SimpleBasicView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
				var disposable = ViewConstructor as IDisposable;
				disposable?.Dispose();
			}
			RequestedSelectionAtEndOfUow = null;
			Cache = null;
			ViewConstructor = null;
			SelectionHelper = null;
		}

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			SuspendLayout();
			Name = "DummyBasicView";
			ResumeLayout(false);

		}
		#endregion

		#region Event handling methods
		/// <summary>
		/// Activates the view
		/// </summary>
		public void ActivateView()
		{
			PerformLayout();
			Show();
			Focus();
		}

		/// <summary>
		/// Simulate a scroll to end
		/// </summary>
		public override void ScrollToEnd()
		{
			base.ScrollToEnd();
			RootBox.MakeSimpleSel(false, true, false, true);
			PerformLayout();
		}

		/// <summary>
		/// Simulate scrolling to top
		/// </summary>
		public override void ScrollToTop()
		{
			base.ScrollToTop();
			// The actual DraftView code for handling Ctrl-Home doesn't contain this method call.
			// The call to CallOnExtendedKey() in OnKeyDown() handles setting the IP.
			RootBox.MakeSimpleSel(true, true, false, true);
		}

		#endregion

		#region Overrides of Control methods
		/// <summary>
		/// Focus got set to the draft view
		/// </summary>
		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);

			if (DesignMode || !m_fRootboxMade)
			{
				return;
			}
			if (SelectionHelper != null)
			{
				SelectionHelper.SetSelection(this);
				SelectionHelper = null;
			}
		}

		/// <summary>
		/// For AdjustScrollRangeTestHScroll we need the dummy to allow horizontal scrolling.
		/// </summary>
		protected override bool WantHScroll => true;

		/// <summary>
		/// Recompute the layout
		/// </summary>
		protected override void OnLayout(LayoutEventArgs levent)
		{
			if (!SkipLayout)
			{
				base.OnLayout(levent);
			}
		}
		#endregion

		/// <summary>
		/// Call the OnLayout methods
		/// </summary>
		public void CallLayout()
		{
			OnLayout(new LayoutEventArgs(this, string.Empty));
		}

		#region Properties

		/// <summary>
		/// Gets or sets the type of boxes to display: lazy or non-lazy or both
		/// </summary>
		public DisplayType MyDisplayType { get; set; }

		/// <summary>
		/// Gets the draft view's selection helper object
		/// </summary>
		public SelectionHelper SelectionHelper { get; private set; }

		/// <summary>
		/// Gets or sets a flag if OnLayout should be skipped.
		/// </summary>
		public bool SkipLayout { get; set; } = false;

		/// <summary>
		/// Gets the view constructor.
		/// </summary>
		public VwBaseVc ViewConstructor { get; private set; }
		#endregion

		/// <summary>
		/// Check for presence of proper paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoAnchor">[out] Start index of selection</param>
		/// <param name="ihvoEnd">[out] End index of selection</param>
		/// <returns>Return <c>false</c> if neither selection nor paragraph property. Otherwise
		/// return <c>true</c>.</returns>
		public bool IsParagraphProps(out IVwSelection vwsel, out int hvoText, out int tagText, out IVwPropertyStore[] vqvps, out int ihvoAnchor, out int ihvoEnd)
		{
			vwsel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoAnchor = 0;
			ihvoEnd = 0;

			return EditingHelper.IsParagraphProps(out vwsel, out hvoText, out tagText, out vqvps, out ihvoAnchor, out ihvoEnd);
		}

		/// <summary>
		/// Get the view selection and paragraph properties.
		/// </summary>
		/// <param name="vwsel">[out] The selection</param>
		/// <param name="hvoText">[out] The HVO</param>
		/// <param name="tagText">[out] The tag</param>
		/// <param name="vqvps">[out] The paragraph properties</param>
		/// <param name="ihvoFirst">[out] Start index of selection</param>
		/// <param name="ihvoLast">[out] End index of selection</param>
		/// <param name="vqttp">[out] The style rules</param>
		/// <returns>Return false if there is neither a selection nor a paragraph property.
		/// Otherwise return true.</returns>
		public bool GetParagraphProps(out IVwSelection vwsel, out int hvoText, out int tagText, out IVwPropertyStore[] vqvps,
			out int ihvoFirst, out int ihvoLast, out ITsTextProps[] vqttp)
		{
			vwsel = null;
			hvoText = 0;
			tagText = 0;
			vqvps = null;
			ihvoFirst = 0;
			ihvoLast = 0;
			vqttp = null;
			return EditingHelper.GetParagraphProps(out vwsel, out hvoText, out tagText, out vqvps, out ihvoFirst, out ihvoLast, out vqttp);
		}

		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.GetCoordRects"/>.
		/// </summary>
		public new void GetCoordRects(out Rectangle rcSrcRoot, out Rectangle rcDstRoot)
		{
			rcSrcRoot = Rectangle.Empty;
			rcDstRoot = Rectangle.Empty;
			base.GetCoordRects(out rcSrcRoot, out rcDstRoot);
		}

		/// <summary>
		/// Provides access to <see cref="ScrollableControl.HScroll"/>
		/// </summary>
		public new bool HScroll
		{
			get
			{
				return base.HScroll;
			}
			set
			{
				base.HScroll = value;
			}
		}

		/// <summary>
		/// Gets the height of the selection.
		/// </summary>
		public int SelectionHeight
		{
			get
			{
				var nLineHeight = 0;
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);

					Rect rdIP;
					Rect rdSecondary;
					bool fSplit;
					bool fEndBeforeAnchor;
					var vwsel = RootBox.Selection;
					if (vwsel != null)
					{
						vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rdIP, out rdSecondary, out fSplit, out fEndBeforeAnchor);
						nLineHeight = rdIP.bottom - rdIP.top;
					}
				}

				return nLineHeight;
			}
		}

		/// <summary>
		/// Gets the width of the selection.
		/// </summary>
		public int SelectionWidth
		{
			get
			{
				var nSelWidth = 0;
				using (new HoldGraphics(this))
				{
					Rectangle rcSrcRoot;
					Rectangle rcDstRoot;
					GetCoordRects(out rcSrcRoot, out rcDstRoot);

					Rect rdIP;
					Rect rdSecondary;
					bool fSplit;
					bool fEndBeforeAnchor;
					var vwsel = RootBox.Selection;
					if (vwsel != null)
					{
						vwsel.Location(m_graphicsManager.VwGraphics, rcSrcRoot, rcDstRoot, out rdIP, out rdSecondary, out fSplit, out fEndBeforeAnchor);
						nSelWidth = rdIP.right - rdIP.left;
					}
				}

				return nSelWidth;
			}
		}

		/// <summary>
		/// Provides access to <see cref="SimpleRootSite.OnMouseDown"/>
		/// </summary>
		public void CallOnMouseDown(MouseEventArgs e)
		{
			OnMouseDown(e);
		}

		/// <summary>
		/// Makes a root box and initializes it with appropriate data
		/// </summary>
		/// <param name="hvoRoot">Hvo of the root object</param>
		/// <param name="flid">Flid in which hvoRoot contains a sequence of StTexts</param>
		/// <param name="frag">Fragment for view constructor</param>
		/// <param name="hvoWs">The ID of thje default Writing System to use</param>
		public void MakeRoot(int hvoRoot, int flid, int frag, int hvoWs)
		{
			if (Cache == null || DesignMode)
			{
				return;
			}
			MakeRoot();

			// Set up a new view constructor.
			ViewConstructor = new SimpleViewVc(MyDisplayType, flid)
			{
				DefaultWs = hvoWs
			};

			RootBox.DataAccess = Cache;
			RootBox.SetRootObject(hvoRoot, ViewConstructor, frag, m_styleSheet);

			m_fRootboxMade = true;
			m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.

			// Added this to keep from Asserting if the user tries to scroll the draft window
			// before clicking into it to place the insertion point.
			try
			{
				RootBox.MakeSimpleSel(true, true, false, true);
			}
			catch (COMException)
			{
				// We ignore failures since the text window may be empty, in which case making a
				// selection is impossible.
			}
		}

		/// <summary />
		public void SetRootBox(IVwRootBox rootb)
		{
			RootBox = rootb;
		}

		/// <summary>
		/// Method called from views code to deal with complex pastes, overridden for testing.
		/// </summary>
		public override VwInsertDiffParaResponse OnInsertDiffParas(IVwRootBox prootb, ITsTextProps ttpDest, int cPara, ITsTextProps[] ttpSrc, ITsString[] tssParas, ITsString tssTrailing)
		{
			return VwInsertDiffParaResponse.kidprFail;
		}

		/// <summary>see OnInsertDiffParas</summary>
		public override VwInsertDiffParaResponse OnInsertDiffPara(IVwRootBox prootb, ITsTextProps ttpDest, ITsTextProps ttpSrc, ITsString tssParas, ITsString tssTrailing)
		{
			return VwInsertDiffParaResponse.kidprFail;
		}

		/// <summary>
		/// In this simple implementation, we just record the information about the requested
		/// selection.
		/// </summary>
		/// <param name="rootb">The rootbox</param>
		/// <param name="ihvoRoot">Index of root element</param>
		/// <param name="cvlsi">count of levels</param>
		/// <param name="rgvsli">levels</param>
		/// <param name="tagTextProp">tag or flid of property containing the text (TsString)</param>
		/// <param name="cpropPrevious">number of previous occurrences of the text property</param>
		/// <param name="ich">character offset into the text</param>
		/// <param name="wsAlt">The id of the writing system for the selection.</param>
		/// <param name="fAssocPrev">Flag indicating whether to associate the insertion point
		/// with the preceding character or the following character</param>
		/// <param name="selProps">The selection properties.</param>
		public override void RequestSelectionAtEndOfUow(IVwRootBox rootb, int ihvoRoot, int cvlsi, SelLevInfo[] rgvsli, int tagTextProp, int cpropPrevious, int ich, int wsAlt, bool fAssocPrev, ITsTextProps selProps)
		{
			Assert.AreEqual(RootBox, rootb);
			Assert.IsNull(RequestedSelectionAtEndOfUow);

			RequestedSelectionAtEndOfUow = new SelectionHelper();
			RequestedSelectionAtEndOfUow.RootSite = this;
			RequestedSelectionAtEndOfUow.IhvoRoot = ihvoRoot;
			RequestedSelectionAtEndOfUow.NumberOfLevels = cvlsi;
			RequestedSelectionAtEndOfUow.SetLevelInfo(SelLimitType.Anchor, rgvsli);
			RequestedSelectionAtEndOfUow.TextPropId = tagTextProp;
			RequestedSelectionAtEndOfUow.NumberOfPreviousProps = cpropPrevious;
			RequestedSelectionAtEndOfUow.IchAnchor = ich;
			RequestedSelectionAtEndOfUow.AssocPrev = fAssocPrev;
		}
		/// <summary>
		/// The class that displays the draft view.
		/// </summary>
		private sealed class SimpleViewVc : VwBaseVc
		{
			private readonly DisplayType m_displayType;
			private readonly int m_flid;
			private int m_counter = 1;

			/// <summary />
			public SimpleViewVc(DisplayType display, int flid)
			{
				m_displayType = display;
				m_flid = flid;
			}

			/// <summary />
			/// <summary />
			private const int kMarginTop = 60000;
			/// <summary />
			private const int kEstimatedParaHeight = 30;

			#region Overridden methods

			/// <summary />
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				if ((m_displayType & DisplayType.kLiteralStringLabels) != 0)
				{
					vwenv.AddString(TsStringUtils.MakeString("Label" + m_counter++, m_wsDefault));
				}
				switch (frag)
				{
					case 1: // the root; display the subitems, first using non-lazy view, then lazy one.
						if ((m_displayType & DisplayType.kFootnoteDetailsSeparateParas) == DisplayType.kFootnoteDetailsSeparateParas)
						{
							vwenv.AddObjVecItems(m_flid, this, 10);
						}
						if ((m_displayType & DisplayType.kFootnoteDetailsSinglePara) == DisplayType.kFootnoteDetailsSinglePara)
						{
							vwenv.AddObjVecItems(m_flid, this, 11);
						}
						else
						{
							if ((m_displayType & DisplayType.kNormal) == DisplayType.kNormal)
							{
								vwenv.AddObjVecItems(m_flid, this, 3);
							}
							if ((m_displayType & DisplayType.kLazy) == DisplayType.kLazy)
							{
								vwenv.AddObjVecItems(m_flid, this, 2);
							}
						}
						if ((m_displayType & DisplayType.kBookTitle) == DisplayType.kBookTitle)
						{
							vwenv.AddObjProp(SimpleRootsiteTestsConstants.kflidDocTitle, this, 3);
						}
						if (m_displayType == DisplayType.kOuterObjDetails)
						{
							vwenv.AddObjVecItems(m_flid, this, 6);
						}
						break;
					case 2: // An StText, display paragraphs lazily
						if ((m_displayType & DisplayType.kWithTopMargin) == DisplayType.kWithTopMargin)
						{
							vwenv.AddLazyVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 4);
						}
						vwenv.AddLazyVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 5);
						break;
					case 3: // An StText, display paragraphs not lazily.
						if ((m_displayType & DisplayType.kWithTopMargin) == DisplayType.kWithTopMargin)
						{
							vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 4);
						}
						vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 5);
						if ((m_displayType & DisplayType.kDuplicateParagraphs) != 0)
						{
							vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 5);
						}
						break;
					case 4: // StTxtPara, display contents with top margin
						OpenParaIfNeeded(vwenv, hvo);
						vwenv.set_IntProperty((int)FwTextPropType.ktptMarginTop, (int)FwTextPropVar.ktpvMilliPoint, kMarginTop);
						AddParagraphContents(vwenv);
						break;
					case 5: // StTxtPara, display contents without top margin
						OpenParaIfNeeded(vwenv, hvo);
						AddParagraphContents(vwenv);
						break;
					case 6: // StTxtPara, display details of our outer object
						int hvoOuter, tag, ihvo;
						vwenv.GetOuterObject(vwenv.EmbeddingLevel - 1, out hvoOuter, out tag, out ihvo);
						var tss = TsStringUtils.MakeString("Hvo = " + hvoOuter + "; Tag = " + tag + "; Ihvo = " + ihvo, m_wsDefault);
						vwenv.AddString(tss);
						break;
					case SimpleRootsiteTestsConstants.kflidDocDivisions:
						vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidDocDivisions, this, SimpleRootsiteTestsConstants.kflidSectionStuff);
						break;
					case SimpleRootsiteTestsConstants.kflidSectionStuff:
						if ((m_displayType & DisplayType.kNormal) == DisplayType.kNormal)
						{
							vwenv.AddObjProp(frag, this, 3);
						}
						if ((m_displayType & DisplayType.kLazy) == DisplayType.kLazy)
						{
							vwenv.AddObjProp(frag, this, 2);
						}
						break;
					case 7: // ScrBook
						vwenv.OpenDiv();
						vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidDocFootnotes, this, 8);
						vwenv.CloseDiv();
						break;
					case 8: // StFootnote
						vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 9);
						break;
					case 9: // StTxtPara
						vwenv.AddStringProp(SimpleRootsiteTestsConstants.kflidParaContents, null);
						break;
					case 10:
						// Display a Footnote by displaying its "FootnoteMarker" in a paragraph
						// by itself, followed by the sequence of paragraphs.
						vwenv.AddStringProp(SimpleRootsiteTestsConstants.kflidFootnoteMarker, null);
						vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 9);
						break;
					case 11:
						// Display a Footnote by displaying its "FootnoteMarker" followed by the
						// contents of its first paragraph (similar to the way footnotes are displayed in
						// real life.
						vwenv.AddObjVecItems(SimpleRootsiteTestsConstants.kflidTextParas, this, 12);
						break;
					case 12: // Footnote paragraph with marker
						vwenv.OpenMappedTaggedPara();
						// The footnote marker is not editable.
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
						vwenv.AddStringProp(SimpleRootsiteTestsConstants.kflidFootnoteMarker, null);

						// add a read-only space after the footnote marker
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
						var strBldr = TsStringUtils.MakeIncStrBldr();
						strBldr.Append(" ");
						vwenv.AddString(strBldr.GetString());
						vwenv.AddStringProp(SimpleRootsiteTestsConstants.kflidParaContents, null);
						vwenv.CloseParagraph();
						break;
					default:
						throw new ApplicationException("Unexpected frag in DummyBasicViewVc");
				}
			}

			/// <summary>
			/// Adds the current paragraph's contents (either once or thrice, depending on display
			/// flags) and then close the paragraph if necessary.
			/// </summary>
			private void AddParagraphContents(IVwEnv vwenv)
			{
				vwenv.AddStringProp(SimpleRootsiteTestsConstants.kflidParaContents, null);
				if ((m_displayType & DisplayType.kOnlyDisplayContentsOnce) != DisplayType.kOnlyDisplayContentsOnce)
				{
					vwenv.AddStringProp(SimpleRootsiteTestsConstants.kflidParaContents, null);
					vwenv.AddStringProp(SimpleRootsiteTestsConstants.kflidParaContents, null);
				}
				if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
				{
					vwenv.CloseParagraph();
				}
			}

			/// <summary>
			/// Conditionally open a mapped paragraph (depending on the requested display type).
			/// Also, if requested, apply the paragraph properties of the given paragraph
			/// </summary>
			private void OpenParaIfNeeded(IVwEnv vwenv, int hvo)
			{
				if ((m_displayType & DisplayType.kMappedPara) == DisplayType.kMappedPara)
				{
					if ((m_displayType & DisplayType.kUseParaProperties) == DisplayType.kUseParaProperties)
					{
						vwenv.Props = (ITsTextProps)vwenv.DataAccess.get_UnknownProp(hvo, SimpleRootsiteTestsConstants.kflidParaProperties);
					}
					vwenv.OpenMappedPara();
				}
			}

			/// <summary>
			/// This routine is used to estimate the height of an item. The item will be one of
			/// those you have added to the environment using AddLazyItems. Note that the calling
			/// code does NOT ensure that data for displaying the item in question has been loaded.
			/// The first three arguments are as for Display, that is, you are being asked to
			/// estimate how much vertical space is needed to display this item in the available width.
			/// </summary>
			public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
			{
				return kEstimatedParaHeight;  // just give any arbitrary number
			}

			/// <summary>
			/// Get the string that should be displayed in place of an object character associated
			/// with the specified GUID. This dummy version just returns something similar to what
			/// TE would normally put in for an alpha footnote.
			/// </summary>
			public override ITsString GetStrForGuid(string bstrGuid)
			{
				return TsStringUtils.MakeString("\uFEFFa", m_wsDefault);
			}
			#endregion
		}
	}
}