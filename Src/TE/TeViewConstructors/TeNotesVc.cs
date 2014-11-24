// Copyright (c) 2005-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TeNotesVc.cs
// Responsibility: TE Team
//
// <remarks>
// Implements the view constructor for the footnote view
// </remarks>
// --------------------------------------------------------------------------------------------

using System;
using System.Drawing;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms.VisualStyles;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SILUBS.SharedScrUtils;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.Utils.ComTypes;
using SIL.Utils;
using System.Diagnostics.CodeAnalysis;

namespace SIL.FieldWorks.TE
{
	#region TeNotesVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class displays the notes (annotations) view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeNotesVc : TeStVc, IDisposable
	{
		#region Member Data
		private ITsStrFactory m_tsStrFactory;
		private IScripture m_scr;
		private DBMultilingScrBooks m_scrBooks;
		private Dictionary<int, bool> m_expandTable = new Dictionary<int, bool>();
		private IPicture m_picMinus;
		private IPicture m_picPlus;
		private IPicture m_picChooser;
		private int m_expansionTag = -8; // TODO: Need to get a unique tag

		// Use these variables if you ever attempt to vertically center the +/- box, the
		// chooser button and the resolve checkbox. There's some code already in place
		// to attempt that, but it wasn't worth the payoff for now. For now, there is a
		// little bit of top padding factored into the display of those picture elements.
		//private int m_dpiY;
		//private int m_chooserPadding = -1;
		//private int m_expanderPadding = -1;
		//private int m_resolutionStatusPadding = -1;
		//private Dictionary<IPicture, int> m_pixelHeights = new Dictionary<IPicture,int>();
//		private float m_zoomPercent;
		private ITsString m_quoteLabel;
		private ITsString m_detailsLabel;
		private ITsString m_discussionLabel;
		private ITsString m_messageLabel;
		private ITsString m_suggestionLabel;
		private ITsString m_resolutionLabel;
		private ITsString m_authorLabel;
		private ITsString m_createdLabel;
		private ITsString m_modifiedLabel;
		private ITsString m_resolvedLabel;
		private ITsString m_responseLabel;
		private ITsString m_tssEmpty;
		private int m_selectedNoteHvo;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the TeNotesVc class
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="initialZoom"></param>
		/// ------------------------------------------------------------------------------------
		public TeNotesVc(FdoCache cache, float initialZoom) : this(cache)
		{
//			m_zoomPercent = initialZoom;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the TeNotesVc class
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "img is a reference")]
		public TeNotesVc(FdoCache cache) : base(LayoutViewTarget.targetDraft, -1)
		{
			m_cache = cache;
			m_wsDefault = cache.DefaultAnalWs;
			m_tsStrFactory = cache.TsStrFactory;
			m_scr = cache.LanguageProject.TranslatedScriptureOA;
			m_scrBooks = new DBMultilingScrBooks(m_scr);

			Image img = ResourceHelper.MinusBox;
			m_picMinus = (IPicture)OLECvt.ToOLE_IPictureDisp(img);

			//m_pixelHeights[m_picMinus] = img.Height;
			m_picPlus = (IPicture)OLECvt.ToOLE_IPictureDisp(ResourceHelper.PlusBox);

			m_picChooser = GetChooserImage();

			//using (Form frm = new Form())
			//using (Graphics g = frm.CreateGraphics())
			//    m_dpiY = (int)g.DpiY;

			m_quoteLabel = MakeLabelFromStringID("kstidQuoteLabel");
			m_detailsLabel = MakeLabelFromStringID("kstidDetailsLabel");
			m_discussionLabel = MakeLabelFromStringID("kstidDiscussionLabel");
			m_messageLabel = MakeLabelFromStringID("kstidMessageLabel");
			m_suggestionLabel = MakeLabelFromStringID("kstidSuggestionLabel");
			m_resolutionLabel = MakeLabelFromStringID("kstidResolutionLabel");
			m_authorLabel = MakeLabelFromStringID("kstidAuthorLabel");
			m_createdLabel = MakeLabelFromStringID("kstidCreatedLabel");
			m_modifiedLabel = MakeLabelFromStringID("kstidModifiedLabel");
			m_resolvedLabel = MakeLabelFromStringID("kstidResolvedLabel");
			m_responseLabel = MakeLabelFromStringID("kstidResponseLabel");
			ITsStrFactory strFactory = TsStrFactoryClass.Create();
			m_tssEmpty = strFactory.MakeString(string.Empty, m_scr.Cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the chooser image.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "img is a reference")]
		private IPicture GetChooserImage()
		{
			Image img = ResourceHelper.ChooserButton;
			Rectangle rc = new Rectangle(0, 0, img.Width, img.Height);

			using (Graphics g = Graphics.FromImage(img))
			{
//				VisualStyleElement element = VisualStyleElement.Button.PushButton.Normal;

				//if (!PaintingHelper.CanPaintVisualStyle(element))
				//{
					// Draw a non themed button.
					ControlPaint.DrawButton(g, rc, ButtonState.Normal);
				//}
				//else
				//{
				//    // Draw a themed button.
				//    VisualStyleRenderer renderer = new VisualStyleRenderer(element);
				//    renderer.DrawBackground(g, rc);
				//}

				// Draw the ellipsis on the button.
				using (Font fnt = new Font(MiscUtils.StandardSansSerif, 9, FontStyle.Bold))
				{
					TextRenderer.DrawText(g, "...", fnt, rc,
						SystemColors.ControlDarkDark, TextFormatFlags.HorizontalCenter |
						TextFormatFlags.SingleLine | TextFormatFlags.Bottom);
				}
			}

			IPicture oleimg = (IPicture)OLECvt.ToOLE_IPictureDisp(img);
			//m_pixelHeights[oleimg] = img.Height;
			return oleimg;

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a label string to use in the view.
		/// </summary>
		/// <param name="stid">Resource id for the string to put in the label</param>
		/// <returns>An ITsString</returns>
		/// ------------------------------------------------------------------------------------
		private ITsString MakeLabelFromStringID(string stid)
		{
			return MakeLabelFromText(TeResourceHelper.GetResourceString(stid), null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a label string to use in the view.
		/// </summary>
		/// <param name="labelText">text to put in the label</param>
		/// <param name="ann">The annotation</param>
		/// <returns>An ITsString</returns>
		/// ------------------------------------------------------------------------------------
		private ITsString MakeLabelFromText(string labelText, IScrScriptureNote ann)
		{
			return MakeLabelFromText(labelText, ScrStyleNames.NotationTag, ann);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make a label string to use in the view.
		/// </summary>
		/// <param name="labelText">text to put in the label</param>
		/// <param name="styleName">style name to use for the character style on the text</param>
		/// <param name="ann">The annotation</param>
		/// <returns>An ITsString</returns>
		/// ------------------------------------------------------------------------------------
		protected ITsString MakeLabelFromText(string labelText, string styleName,
			IScrScriptureNote ann)
		{
			ITsTextProps ttpLabel = StyleUtils.CharStyleTextProps(styleName,
				m_cache.WritingSystemFactory.UserWs);
			ITsStrBldr bldr = TsStrBldrClass.Create();
			if (ann != null && ann.BeginObjectRA != null && ann.BeginObjectRA.IsValidObject)
			{
				IStTxtPara startPara = ann.BeginObjectRA as IStTxtPara;
				if (startPara != null)
				{
					int ownerOwnFlid = startPara.Owner.OwningFlid;
					if (ownerOwnFlid == ScrBookTags.kflidFootnotes)
					{
#if __MonoCS__
			const string fontname = "OpenSymbol";
			const string symbol = "\u2042";
#else
			const string fontname = "Marlett";
			const string symbol = "\u0032";
#endif
						ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily, fontname);
						propsBldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0, m_cache.WritingSystemFactory.UserWs);
						bldr.Replace(0, 0, symbol, propsBldr.GetTextProps());
					}
				}
			}
			bldr.Replace(bldr.Length, bldr.Length, labelText, ttpLabel);

			// Add a space with default paragraph characters. This prevents style bleed-through
			// from the label when the user types in the text with (part of) the label selected.
			bldr.Replace(bldr.Length, bldr.Length, " ", StyleUtils.CharStyleTextProps(null,
				m_cache.WritingSystemFactory.UserWs));
			return bldr.GetString();
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the image padding.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private int GetImagePadding(IVwEnv vwenv, IPicture pic)
		//{
		//    ITsTextProps ttp = StyleUtils.CharStyleTextProps(ScrStyleNames.NotationTag,
		//        m_cache.DefaultUserWs);
		//    ITsStrBldr bldr = TsStrBldrClass.Create();

		//    bldr.Replace(0, 0, "X", ttp);
		//    int dmpX, lineHeight;
		//    vwenv.get_StringWidth(bldr.GetString(), ttp, out dmpX, out lineHeight);

		//    float pixelsY = ((float)m_pixelHeights[pic] - 1f); // *m_zoomPercent;
		//    float inchesY = pixelsY / (float)m_dpiY;
		//    float milliptY = 72000f * inchesY;
		//    return (int)(Math.Abs((float)lineHeight - milliptY) / 2f);
		//}

		#endregion

		#region Disposable stuff
		#if DEBUG
		/// <summary/>
		~TeNotesVc()
		{
			Dispose(false);
		}
		#endif

		/// <summary/>
		public bool IsDisposed { get; private set; }

		/// <summary/>
		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary/>
		protected virtual void Dispose(bool fDisposing)
		{
			System.Diagnostics.Debug.WriteLineIf(!fDisposing, "****** Missing Dispose() call for " + GetType().ToString() + " *******");
			if (fDisposing && !IsDisposed)
			{
				// dispose managed and unmanaged objects
				var disposable = m_picMinus as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				disposable = m_picPlus as IDisposable;
				if (disposable != null)
					disposable.Dispose();
				disposable = m_picChooser as IDisposable;
				if (disposable != null)
					disposable.Dispose();
			}
			m_picMinus = null;
			m_picPlus = null;
			m_picChooser = null;

			IsDisposed = true;
		}
		#endregion

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
				case (int)ScrFrags.kfrScripture:
				{
					vwenv.AddLazyVecItems(ScriptureTags.kflidBookAnnotations, this,
						(int)ScrFrags.kfrBook);
					break;
				}
				case (int)ScrFrags.kfrBook:
				{
					vwenv.AddLazyVecItems(ScrBookAnnotationsTags.kflidNotes, this,
						(int)NotesFrags.kfrAnnotation);
					break;
				}
				case (int)NotesFrags.kfrAnnotation:
				{
					DisplayAnnotationFragment(vwenv, hvo);
					break;
				}
				case (int)NotesFrags.kfrResponse:
				{
					DisplayResponseFragment(vwenv, hvo);
					break;
				}
				case (int)NotesFrags.kfrExpansion:
				{
					vwenv.OpenParagraph();

					//if (m_expanderPadding == -1)
					//    m_expanderPadding = GetImagePadding(vwenv, m_picMinus);

					vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop,
						(int)FwTextPropVar.ktpvMilliPoint, 2500);

					ICmObject obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
					vwenv.AddPicture((m_expandTable.ContainsKey(hvo) && m_expandTable[hvo]) ?
						m_picMinus : m_picPlus,	(int)obj.OwningFlid, 0, 0);

					vwenv.CloseParagraph();
					break;
				}
				default:
					base.Display(vwenv, hvo, frag);
					break;
			}
		}

		/// <summary>
		/// return true if the view displays books (and hence BooksTag may be called).
		/// This class does NOT support books.
		/// </summary>
		protected override bool HasBooks
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Estimate the height of a lazy box
		/// </summary>
		/// <returns>The height of an item in points</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			// ENHANCE: May need to consider taking filter into consideration for getting better
			// estimates.
			switch (frag)
			{
				case (int)ScrFrags.kfrBook:
					// Assume that any annotations in a book lazy box are not expanded yet...
					IScrBookAnnotations bookAnnotations =
						m_cache.ServiceLocator.GetInstance<IScrBookAnnotationsRepository>().GetObject(hvo);
					int height = 0;
					foreach (IScrScriptureNote note in bookAnnotations.NotesOS)
						height += EstimateHeight(note.Hvo, (int)NotesFrags.kfrAnnotation, dxAvailWidth);
					return height;
				case (int)NotesFrags.kfrAnnotation:
					return m_expandTable.ContainsKey(hvo) ? 112 : 14;
			}
			Debug.Assert(false, "unexpected frag");
			return 14;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the annotation fragment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayAnnotationFragment(IVwEnv vwenv, int hvo)
		{
			IScrScriptureNote ann = m_cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(hvo);
			bool isExpanded = (m_expandTable.ContainsKey(hvo) && m_expandTable[hvo]);
			int hvoFirstResponse = ann.ResponsesOS.Count > 0 ? ann.ResponsesOS[0].Hvo : 0;
			bool responseExpanded = m_expandTable.ContainsKey(hvoFirstResponse) && m_expandTable[hvoFirstResponse];

			// put the separator at the top of the note
			InsertNoteSeparator(vwenv);

			// Open a table to display the first line of the annotation.
			VwLength vlTable; // we use this to specify that the table takes 100% of the width.
			vlTable.nVal = 10000;
			vlTable.unit = VwUnit.kunPercent100;

			int borderThickness = 2000;
			int columnCount = 9;
			VwFramePosition framePos = VwFramePosition.kvfpVoid;
			if (ann.Hvo == SelectedNoteHvo)
			{
				columnCount = 7;
				framePos = (ann.ResponsesOS.Count == 0 || !isExpanded || !responseExpanded) ?
					VwFramePosition.kvfpBox : (VwFramePosition.kvfpAbove | VwFramePosition.kvfpVsides);
			}

			vwenv.OpenTable(columnCount,
				vlTable, // Table uses 100% of available width.
				borderThickness,
				VwAlignment.kvaLeft, // Default alignment.
				framePos,
				//VwFramePosition.kvfpBox,
				//VwRule.kvrlAll, // rule lines between cells
				VwRule.kvrlNone,
				0, //No space between cells.
				0, //No padding inside cells.
				false);

			vwenv.NoteDependency(new int[] { hvo, hvo }, new int[]{m_expansionTag,
				ScrScriptureNoteTags.kflidResponses}, 2);

			// Specify column widths. The first argument is the number of columns,
			// not a column index.
			VwLength vlColumn;
			if (ann.Hvo != SelectedNoteHvo)
			{
				vlColumn.nVal = borderThickness;
				vlColumn.unit = VwUnit.kunPoint1000;
				vwenv.MakeColumns(1, vlColumn);
			}
			vlColumn.nVal = 13000;
			vlColumn.unit = VwUnit.kunPoint1000;
			vwenv.MakeColumns(2, vlColumn);
			vlColumn.nVal = 60000;
			vlColumn.unit = VwUnit.kunPoint1000;
			vwenv.MakeColumns(1, vlColumn);
			vlColumn.nVal = 2;
			vlColumn.unit = VwUnit.kunRelative;
			vwenv.MakeColumns(1, vlColumn); // Column for the CONNOT categories
			vlColumn.nVal = 12000;
			vlColumn.unit = VwUnit.kunPoint1000;
			vwenv.MakeColumns(1, vlColumn); // Column for the chooser button when showing quote
			vlColumn.nVal = 3;
			vlColumn.unit = VwUnit.kunRelative;
			vwenv.MakeColumns(1, vlColumn); // Column for the quote when collapsed
			vlColumn.nVal = 12000;
			vlColumn.unit = VwUnit.kunPoint1000;
			vwenv.MakeColumns(1, vlColumn); // Column for the chooser button when not showing quote
			if (ann.Hvo != SelectedNoteHvo)
			{
				vlColumn.nVal = borderThickness;
				vlColumn.unit = VwUnit.kunPoint1000;
				vwenv.MakeColumns(1, vlColumn);
			}
			vwenv.OpenTableBody();

			DisplayAnnotation(vwenv, ann, isExpanded);

			// Only display the responses if this note is expanded and has responses.
			if (!isExpanded || ann.ResponsesOS.Count == 0)
			{
				// Close table
				vwenv.CloseTableBody();
				vwenv.CloseTable();
				return;
			}

			vwenv.NoteDependency(new int[] { ann.ResponsesOS.ToHvoArray()[0] },
				new int[] { m_expansionTag }, 1);
			OpenTableRow(vwenv, ann);

			// Display empty first cell
			vwenv.OpenTableCell(1, 1);
			vwenv.CloseTableCell();

			// Display expand box (+/-) in the second cell
			vwenv.OpenTableCell(1, 1);
			vwenv.AddObj(ann.ResponsesOS.ToHvoArray()[0], this, (int)NotesFrags.kfrExpansion);
			vwenv.CloseTableCell();

			// Display response label in the remaining cells
			vwenv.OpenTableCell(1, 5);
			vwenv.OpenConcPara(0, 0, 0, 0);
			vwenv.AddString(m_responseLabel);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();
			CloseTableRow(vwenv, ann);

			// Close table
			vwenv.CloseTableBody();
			vwenv.CloseTable();

			if (responseExpanded)
			{
				vwenv.AddObjVecItems((int)ScrScriptureNoteTags.kflidResponses,
					this, (int)NotesFrags.kfrResponse);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens the table row.
		/// </summary>
		/// <param name="vwenv">The view environment.</param>
		/// <param name="ann">The annotation being displayed.</param>
		/// ------------------------------------------------------------------------------------
		private void OpenTableRow(IVwEnv vwenv, IScrScriptureNote ann)
		{
			vwenv.OpenTableRow();

			if (ann.Hvo != SelectedNoteHvo)
			{
				// Display cell used for padding (to indent unselected annotations by the width
				// of the frame used to highlight the selected annotation).
				vwenv.OpenTableCell(1, 1);
				vwenv.AddString(m_tssEmpty);
				vwenv.CloseTableCell();
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Closes the table row.
		/// </summary>
		/// <param name="vwenv">The view environment.</param>
		/// <param name="ann">The annotation being displayed.</param>
		/// ------------------------------------------------------------------------------------
		private void CloseTableRow(IVwEnv vwenv, IScrScriptureNote ann)
		{
			if (ann.Hvo != SelectedNoteHvo)
			{
				// Display cell used for padding (to right-indent unselected annotations by
				// the width of the frame used to highlight the selected annotation).
				vwenv.OpenTableCell(1, 1);
				vwenv.AddString(m_tssEmpty);
				vwenv.CloseTableCell();
			}

			vwenv.CloseTableRow();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays an annotation expanded or contracted
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="ann"></param>
		/// <param name="expanded"></param>
		/// ------------------------------------------------------------------------------------
		private void DisplayAnnotation(IVwEnv vwenv, IScrScriptureNote ann, bool expanded)
		{
			#region First row has status, ref, category, & quote
			SetBackgroundColorForNote(ann, vwenv);

			OpenTableRow(vwenv, ann);

			// Display expand box (+/-) in the first cell
			//InsertNoteSeparator(vwenv);
			vwenv.OpenTableCell(1, 1);
			vwenv.AddObj(ann.Hvo, this, (int)NotesFrags.kfrExpansion);
			vwenv.CloseTableCell();

			// Display status in the second cell
			vwenv.OpenTableCell(1, 1);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop,
				(int)FwTextPropVar.ktpvMilliPoint, 1000);

			if (ann.AnnotationType == NoteType.CheckingError)
			{
				// When the annotation is a checking error, we don't want clicking on the status
				// to change the status. Therefore, make the min and max the same.
				vwenv.AddIntPropPic((int)ScrScriptureNoteTags.kflidResolutionStatus,
					this, (int)NotesFrags.kfrStatus, 0, 0);
			}
			else
			{
				vwenv.AddIntPropPic((int)ScrScriptureNoteTags.kflidResolutionStatus,
					this, (int)NotesFrags.kfrStatus, (int)NoteStatus.Open, (int)NoteStatus.Closed);
			}
			vwenv.CloseTableCell();

			// Display reference in the third cell and make it readonly.
			vwenv.OpenTableCell(1, 1);
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, 0);
			vwenv.OpenParagraph();
			vwenv.AddProp((int)CmBaseAnnotationTags.kflidBeginRef, this, (int)NotesFrags.kfrScrRef);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			// Display CONNOT category in the fourth (and possibly fifth and sixth) cell(s)
			vwenv.OpenTableCell(1, expanded ? 3 : 1);
			IStTxtPara quotePara = ann.QuoteOA[0];
			bool fQuoteParaRtoL = IsParaRightToLeft(quotePara);

			// Conc paragraphs don't work well for R-to-L: If the text doesn't fit, it will
			// show the trailing text rather than the leading text.
			if (fQuoteParaRtoL || expanded)
				vwenv.OpenParagraph();
			else
				vwenv.OpenConcPara(0, 0, 0, 0);
			vwenv.AddObjVec((int)ScrScriptureNoteTags.kflidCategories, this,
				(int)NotesFrags.kfrConnotCategory);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			// Display CONNOT category chooser button in the penultimate or last cell
			vwenv.OpenTableCell(1, 1);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadTop,
				(int)FwTextPropVar.ktpvMilliPoint, 1000);
			vwenv.set_IntProperty((int)FwTextPropType.ktptPadLeading,
				(int)FwTextPropVar.ktpvMilliPoint, 2000);

			vwenv.AddPicture(m_picChooser, -(int)NotesFrags.kfrConnotCategory, 0, 0);
			vwenv.CloseTableCell();

			// If not expanded, display the quote in the last cell
			if (!expanded)
			{
				vwenv.OpenTableCell(1, 1);
				SetupWsAndDirectionForPara(vwenv, quotePara.Hvo);
				if (fQuoteParaRtoL)
					vwenv.OpenParagraph(); // Conc paragraphs don't work well for R-to-L
				else
					vwenv.OpenConcPara(0, 0, 0, 0);
				vwenv.AddString(quotePara.Contents);
				vwenv.CloseParagraph();
				vwenv.CloseTableCell();
			}

			CloseTableRow(vwenv, ann);
			#endregion

			if (!expanded)
				return;

			#region Second through fifth rows
			bool fRegularAnnotation = ann.AnnotationType != NoteType.CheckingError;
			//Second row has quote
			DisplayExpandableAnnotation(vwenv, ann,
				(int)ScrScriptureNoteTags.kflidQuote,
				ann.QuoteOA.Hvo, ann.QuoteOA,
				fRegularAnnotation ? m_quoteLabel : m_detailsLabel, !fRegularAnnotation);

			// Third row has discussion
			DisplayExpandableAnnotation(vwenv, ann,
				ScrScriptureNoteTags.kflidDiscussion,
				ann.DiscussionOA.Hvo, ann.DiscussionOA,
				fRegularAnnotation ? m_discussionLabel : m_messageLabel, !fRegularAnnotation);

			// Fourth row has recommendation (i.e. suggestion)
			DisplayExpandableAnnotation(vwenv, ann,
				ScrScriptureNoteTags.kflidRecommendation,
				ann.RecommendationOA.Hvo, ann.RecommendationOA, m_suggestionLabel);

			// Fifth row has resolution
			DisplayExpandableAnnotation(vwenv, ann,
				ScrScriptureNoteTags.kflidResolution,
				ann.ResolutionOA.Hvo, ann.ResolutionOA, m_resolutionLabel);

			#endregion

			#region Sixth row has author
			SetBackgroundColorForNote(ann, vwenv);
			OpenTableRow(vwenv, ann);

			// Display empty first and second cell
			vwenv.OpenTableCell(1, 2);
			vwenv.CloseTableCell();

			// Display author in third cell
			vwenv.OpenTableCell(1, 3);
			vwenv.OpenParagraph();
			SetDisabledColorForNote(vwenv);
			vwenv.AddString(m_authorLabel);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			CloseTableRow(vwenv, ann);
			#endregion

			#region Seventh row has dates
			SetBackgroundColorForNote(ann, vwenv);
			OpenTableRow(vwenv, ann);

			// Display empty first and second cell
			vwenv.OpenTableCell(1, 2);
			vwenv.CloseTableCell();

			// Display date created in third cell
			vwenv.OpenTableCell(1, 1);
			vwenv.OpenParagraph();
			vwenv.AddString(m_createdLabel);
			vwenv.AddTimeProp(CmAnnotationTags.kflidDateCreated, 0);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			// Display date modified in fourth/fifth cells
			vwenv.OpenTableCell(1, 2);
			vwenv.OpenParagraph();
			vwenv.AddString(m_modifiedLabel);
			vwenv.AddTimeProp(CmAnnotationTags.kflidDateModified, 0);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			// Display date resolved in last two cells
			vwenv.OpenTableCell(1, 2);
			vwenv.OpenParagraph();
			if (ann.ResolutionStatus == NoteStatus.Closed)
			{
				SetDisabledColorForNote(vwenv);
				vwenv.AddString(m_resolvedLabel);
				// TODO (TE-4039) This date is incorrect
				//vwenv.AddTimeProp(ScrScriptureNote.ScrScriptureNoteTags.kflidDateResolved, 0);
			}
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			CloseTableRow(vwenv, ann);
			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handle display of a Scripture reference or range.
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// <param name="tag">Tag</param>
		/// <param name="frag">Fragment to identify what type of prompt should be created.</param>
		/// <returns>
		/// ITsString that will be displayed as user prompt.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			if (frag == (int)NotesFrags.kfrScrRef)
			{
				IScrScriptureNote ann = m_cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(vwenv.CurrentObject());
				BCVRef startRef = new BCVRef(ann.BeginRef);
				string bookAbbrev = m_scrBooks.GetBookAbbrev(startRef.Book);
				string sAnnRef = ScrReference.MakeReferenceString(bookAbbrev, ann.BeginRef,
					ann.EndRef, m_scr.ChapterVerseSepr, m_scr.Bridge);
				return MakeLabelFromText(sAnnRef, ann);
			}
			return base.DisplayVariant(vwenv, tag, frag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays an expandable annotation of the specified type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayExpandableAnnotation(IVwEnv vwenv, IScrScriptureNote ann,
			int tag, int hvo, IStJournalText paras, ITsString label)
		{
			DisplayExpandableAnnotation(vwenv, ann, tag, hvo, paras, label, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays an expandable annotation of the specified type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayExpandableAnnotation(IVwEnv vwenv, IScrScriptureNote ann,
			int tag, int hvo, IStJournalText paras, ITsString label, bool readOnly)
		{
			vwenv.NoteDependency(new int[] { hvo }, new int[] { m_expansionTag }, 1);
			SetBackgroundColorForNote(ann, vwenv);
			OpenTableRow(vwenv, ann);

			// Display empty first cell
			vwenv.OpenTableCell(1, 1);
			vwenv.CloseTableCell();

			// Display +/- in the second cell, unless this is read-only
			vwenv.OpenTableCell(1, 1);
			if (!readOnly)
				vwenv.AddObjProp(tag, this, (int)NotesFrags.kfrExpansion);
			vwenv.CloseTableCell();

			// Display text in the remaining cells
			vwenv.OpenTableCell(1, 5);
			vwenv.OpenConcPara(0, 0, 0, 0);
			vwenv.AddString(label);

			if (!m_expandTable.ContainsKey(hvo))
				vwenv.AddString(paras[0].Contents);

			vwenv.CloseParagraph();
			vwenv.CloseTableCell();
			CloseTableRow(vwenv, ann);

			if (!m_expandTable.ContainsKey(hvo))
				return;

			SetBackgroundColorForNote(ann, vwenv);
			OpenTableRow(vwenv, ann);

			// Display empty first and second cell
			vwenv.OpenTableCell(1, 2);
			vwenv.CloseTableCell();

			// Display text in cells 3-7
			vwenv.OpenTableCell(1, 5);
			if (!readOnly)
				SetEditBackground(vwenv);
			vwenv.AddObjProp(tag, this, (int)StTextFrags.kfrText);
			vwenv.CloseTableCell();
			CloseTableRow(vwenv, ann);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the response fragment.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayResponseFragment(IVwEnv vwenv, int hvo)
		{
			IStJournalText text = m_cache.ServiceLocator.GetInstance<IStJournalTextRepository>().GetObject(hvo);

			// Open a table to display the the response.
			VwLength vlTable; // we use this to specify that the table takes 100% of the width.
			vlTable.nVal = 10000;
			vlTable.unit = VwUnit.kunPercent100;

			VwFramePosition framePos = VwFramePosition.kvfpVoid;
			if (text.Owner.Hvo == SelectedNoteHvo)
			{
				framePos = (VwFramePosition.kvfpBelow | VwFramePosition.kvfpVsides);
			}

			vwenv.OpenTable(5, // Number of columns.
				vlTable, // Table uses 100% of available width.
				2000, // Border thickness.
				VwAlignment.kvaLeft, // Default alignment.
				framePos, // No border.
				//VwFramePosition.kvfpBox,
				//VwRule.kvrlAll, // rule lines between cells
				VwRule.kvrlNone,
				0, //No space between cells.
				0, //No padding inside cells.
				false);

			// Specify column widths. The first argument is the number of columns,
			// not a column index.
			VwLength vlColumn; // and this one to specify 90% for the text
			vlColumn.nVal = 12000;
			vlColumn.unit = VwUnit.kunPoint1000;
			vwenv.MakeColumns(3, vlColumn);
			//					vlColumn.nVal = 60000;
			//					vlColumn.unit = VwUnit.kunPoint1000;
			//					vwenv.MakeColumns(1, vlColumn);
			vlColumn.nVal = 1;
			vlColumn.unit = VwUnit.kunRelative;
			vwenv.MakeColumns(2, vlColumn);
			vwenv.OpenTableBody();

			DisplayResponse(vwenv, text, m_expandTable.ContainsKey(hvo) && m_expandTable[hvo]);

			// Close table
			vwenv.CloseTableBody();
			vwenv.CloseTable();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display a response to an annotation.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="text">StJournalText containing the response</param>
		/// <param name="expanded">whether response should be expanded</param>
		/// ------------------------------------------------------------------------------------
		private void DisplayResponse(IVwEnv vwenv, IStJournalText text, bool expanded)
		{
			#region Response text
			// Display response text
			vwenv.OpenTableRow();
			// Display empty first, second, and third cells
			vwenv.OpenTableCell(1, 3);
			vwenv.CloseTableCell();

			vwenv.set_IntProperty((int)FwTextPropType.ktptBorderTop,
				(int)FwTextPropVar.ktpvMilliPoint, 500);
			vwenv.OpenTableCell(1, 2);
			vwenv.AddObjVecItems((int)StTextTags.kflidParagraphs, this, (int)StTextFrags.kfrPara);
			vwenv.CloseTableCell();
			vwenv.CloseTableRow();
			#endregion

			#region Author and creation date
			// Author and creation date
			vwenv.OpenTableRow();

			// Display empty first, second, and third cells
			vwenv.OpenTableCell(1, 3);
			vwenv.CloseTableCell();

			// Display author in the third cell
			vwenv.OpenTableCell(1, 1);
			SetDisabledColorForNote(vwenv);
			vwenv.OpenConcPara(0, 0, 0, 0);
			vwenv.AddString(m_authorLabel);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			// Display creation date in the fourth cell
			vwenv.OpenTableCell(1, 1);
			vwenv.OpenConcPara(0, 0, 0, 0);
			vwenv.AddString(m_createdLabel);
			vwenv.AddString(m_tsStrFactory.MakeString(text.DateCreated.ToShortDateString(),
				m_cache.DefaultUserWs));
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();
			vwenv.CloseTableRow();
			#endregion
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the background color for editable fields
		/// </summary>
		/// <param name="vwenv"></param>
		/// ------------------------------------------------------------------------------------
		private void SetEditBackground(IVwEnv vwenv)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.Window)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the background color based on the type of annotation
		/// </summary>
		/// <param name="ann">annotation</param>
		/// <param name="vwenv"></param>
		/// ------------------------------------------------------------------------------------
		private void SetBackgroundColorForNote(IScrScriptureNote ann, IVwEnv vwenv)
		{
			int color = (int)ColorUtil.ConvertColorToBGR(GetNoteBackgroundColor(ann));

			switch (ann.AnnotationType)
			{
				case NoteType.Consultant:
					vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
						(int)FwTextPropVar.ktpvDefault, color);
					break;

				case NoteType.Translator:
					vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
						(int)FwTextPropVar.ktpvDefault, color);
					break;

				case NoteType.CheckingError:
					vwenv.set_IntProperty((int)FwTextPropType.ktptBackColor,
						(int)FwTextPropVar.ktpvDefault, color);
					break;

				default:
					throw new Exception("Unexpected annotation type");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the color of the note background.
		/// </summary>
		/// <param name="ann">The ann.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		private Color GetNoteBackgroundColor(IScrScriptureNote ann)
		{
			switch (ann.AnnotationType)
			{
				case NoteType.Consultant: return Color.PaleGoldenrod;
				case NoteType.Translator: return Color.LightBlue;
				case NoteType.CheckingError: return Color.PaleGreen;
				default: return Color.Red;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the foreground color to gray for a disabled feature
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void SetDisabledColorForNote(IVwEnv vwenv)
		{
			vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
				(int)FwTextPropVar.ktpvDefault,
				(int)ColorUtil.ConvertColorToBGR(Color.FromKnownColor(KnownColor.GrayText)));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Toggle the expansion state of the given item
		/// </summary>
		/// <param name="hvo">The ID of the object to expand/collapse</param>
		/// <param name="rootbox">The rootbox of the caller, which will be notified of the change
		/// (hence causing the appropriate fragments in this VC to get laid out)</param>
		/// <returns>true if the item is expanded. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ToggleItemExpansion(int hvo, IVwRootBox rootbox)
		{
			if (m_expandTable.ContainsKey(hvo))
				m_expandTable.Remove(hvo);
			else
			{
				m_expandTable[hvo] = true;
				OpenNoteFieldsWithContent(hvo, rootbox);
			}

			rootbox.PropChanged(hvo, m_expansionTag, 0, 0, 0);
			return m_expandTable.ContainsKey(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens annotation fields that have content.
		/// </summary>
		/// <param name="hvo">The ID of the object to expand (no action taken if the object is
		/// not a ScrScriptureNote</param>
		/// <param name="rootbox">The rootbox of the caller, which will be notified of the change
		/// (hence causing the appropriate fragments in this VC to get laid out)</param>
		/// <returns>true if the item is expanded. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private void OpenNoteFieldsWithContent(int hvo, IVwRootBox rootbox)
		{
			IScrScriptureNote note;
			if (!m_cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().TryGetObject(hvo, out note))
				return; // This hvo is not for a ScrScriptureNote

			// Expand fields if they contain content and aren't already open.
			if (!string.IsNullOrEmpty(((IStTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.QuoteOA.Hvo, rootbox);

			if (!string.IsNullOrEmpty(((IStTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.DiscussionOA.Hvo, rootbox);

			if (!string.IsNullOrEmpty(((IStTxtPara)note.RecommendationOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.RecommendationOA.Hvo, rootbox);

			if (!string.IsNullOrEmpty(((IStTxtPara)note.ResolutionOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.ResolutionOA.Hvo, rootbox);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expand the item if it is collapsed.
		/// </summary>
		/// <param name="hvo">hvo of the item to expand</param>
		/// <param name="root">The rootbox.</param>
		/// ------------------------------------------------------------------------------------
		public void ExpandItem(int hvo, IVwRootBox root)
		{
			if (!m_expandTable.ContainsKey(hvo))
			{
				m_expandTable[hvo] = true;
				root.PropChanged(hvo, m_expansionTag, 0, 0, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collapses all annotations.
		/// </summary>
		/// <param name="rootbox">The rootbox of the caller, which will be notified of the change
		/// (hence causing the appropriate fragments in this VC to get laid out)</param>
		/// ------------------------------------------------------------------------------------
		public void CollapseAllAnnotations(IVwRootBox rootbox)
		{
			// Make a copy of the table (since collapsing causes keys to be deleted from the table
			// and we are enumerating through it).
			Dictionary<int, bool> expandTable = new Dictionary<int, bool>(m_expandTable);

			var annotationRepo = m_cache.ServiceLocator.GetInstance <IScrScriptureNoteRepository>();
			IScrScriptureNote ann;
			foreach (int annHvo in expandTable.Keys)
			{
				// Confirm that the object is an annotation because we only want to collapse at
				// the annotation level so that we can maintain the expansion status of the
				// fields within the annotation.
				if (annotationRepo.TryGetObject(annHvo, out ann) && IsExpanded(annHvo))
					ToggleItemExpansion(annHvo, rootbox);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns whether or not the given hvo is in an expanded state
		/// </summary>
		/// <param name="hvo">hvo of the item to check</param>
		/// <returns>True if the item is expanded, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool IsExpanded(int hvo)
		{
			return m_expandTable.ContainsKey(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the note status as a picture
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// <param name="hvo">ID of the annotation (not used)</param>
		/// <param name="tag">Always kflidResolutionStatus</param>
		/// <param name="val">See ScrScriptureNote.NoteStatus for values</param>
		/// <param name="frag">Always NotesFrags.kfrStatus</param>
		/// <returns>A picture representing the status</returns>
		/// ------------------------------------------------------------------------------------
		[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
			Justification = "img is a reference")]
		public override IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val, int frag)
		{
			Debug.Assert(frag == (int)NotesFrags.kfrStatus);
			Debug.Assert(tag == (int)ScrScriptureNoteTags.kflidResolutionStatus);
			Image img;
			ButtonState state = ButtonState.Normal;
			IScrScriptureNote ann = m_cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(hvo);
			Color clrBack = GetNoteBackgroundColor(ann);

			switch ((NoteStatus)val)
			{
				default:
				case NoteStatus.Open:
					img = (ann.AnnotationType == NoteType.CheckingError ?
						TeResourceHelper.UnignoredInconsistency : ResourceHelper.UncheckedCheckBox);
					break;

				case NoteStatus.Closed:
					img = (ann.AnnotationType == NoteType.CheckingError ?
						TeResourceHelper.IgnoredInconsistency : ResourceHelper.CheckedCheckBox);
					state = ButtonState.Checked;
					break;
			}

			// Draw an image over the one pulled from the resources. The one drawn will
			// depend on what the OS theme setting is.
			Rectangle rc = new Rectangle(0, 0, img.Width, img.Height);
			using (Graphics g = Graphics.FromImage(img))
			{
				if (ann.AnnotationType == NoteType.CheckingError)
				{
					Image imgTmp = img.Clone() as Image;
					using (SolidBrush br = new SolidBrush(clrBack))
						g.FillRectangle(br, rc);

					g.DrawImageUnscaledAndClipped(imgTmp, rc);
				}
				else
				{
					using (SolidBrush br = new SolidBrush(clrBack))
						g.FillRectangle(br, rc);

					VisualStyleElement element = (state == ButtonState.Normal ?
						VisualStyleElement.Button.CheckBox.UncheckedNormal :
						VisualStyleElement.Button.CheckBox.CheckedNormal);

					if (!PaintingHelper.CanPaintVisualStyle(element))
						ControlPaint.DrawCheckBox(g, rc, ButtonState.Flat | state);
					else
					{
						VisualStyleRenderer renderer = new VisualStyleRenderer(element);
						renderer.DrawBackground(g, rc);
					}
				}
			}

			return (IPicture)OLECvt.ToOLE_IPictureDisp(img);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the categories as a comma-separated string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			Debug.Assert(frag == (int)NotesFrags.kfrConnotCategory);
			Debug.Assert(tag == (int)ScrScriptureNoteTags.kflidCategories);

			IScrScriptureNote ann = m_cache.ServiceLocator.GetInstance<IScrScriptureNoteRepository>().GetObject(hvo);
			bool fFirst = true;
			foreach (ICmPossibility cat in ann.CategoriesRS)
			{
				if (!fFirst)
					vwenv.AddString(m_tsStrFactory.MakeString(", ", m_wsDefault)); // TODO: Comma should come from resources
				vwenv.AddString(cat.Name.BestAnalysisAlternative);
				fFirst = false;
			}
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert a bar to separate this note from the following note (unless this is the first
		/// note being displayed).
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// ------------------------------------------------------------------------------------
		protected virtual void InsertNoteSeparator(IVwEnv vwenv)
		{
			vwenv.AddSimpleRect((int)FwTextColor.kclrTransparent, -1, 7000, 0);
			vwenv.AddSimpleRect((int)ColorUtil.ConvertColorToBGR(ColorUtil.LightInverse(m_BackColor)), -1, 1000, 0);
			vwenv.AddSimpleRect((int)FwTextColor.kclrTransparent, -1, 4000, 0);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Indicates whether the given paragraph should be laid out as right-to-left (depending
		/// on the value of <see cref="BaseDirectionOnParaContents"/>, this can be based on the
		/// paragraph itself or simply derived from the VC).
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// ------------------------------------------------------------------------------------
		protected bool IsParaRightToLeft(IStTxtPara para)
		{
			bool fIsRightToLeftPara;
			int wsPara;
			GetWsAndDirectionForPara(para.Hvo, out fIsRightToLeftPara, out wsPara);
			return fIsRightToLeftPara;
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether to set the base WS and direction according to the
		/// first run in the paragraph contents.
		/// </summary>
		/// <value>
		/// 	<c>true</c> to base the direction on para contents; <c>false</c> to use the
		/// 	default writing system of the view constructor.
		/// </value>
		/// ------------------------------------------------------------------------------------
		public override bool BaseDirectionOnParaContents
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the HVO of the selected annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int SelectedNoteHvo
		{
			get { return m_selectedNoteHvo; }
			set { m_selectedNoteHvo = value; }
		}

		#endregion
	}
	#endregion
}
