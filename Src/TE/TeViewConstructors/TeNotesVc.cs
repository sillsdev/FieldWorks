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
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SILUBS.SharedScrUtils;
using System.Windows.Forms;

namespace SIL.FieldWorks.TE
{
	#region TeNotesVc
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class displays the notes (annotations) view.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeNotesVc : TeStVc
	{
		#region Member Data
		private ITsStrFactory m_tsStrFactory = TsStrFactoryClass.Create();
		private IScripture m_scr;
		private DBMultilingScrBooks m_scrBooks;
		private Dictionary<int, bool> m_expandTable = new Dictionary<int, bool>();
		private stdole.IPicture m_picMinus;
		private stdole.IPicture m_picPlus;
		private stdole.IPicture m_picChooser;
		private int m_expansionTag = -8; // TODO: Need to get a unique tag

		// Use these variables if you ever attempt to vertically center the +/- box, the
		// chooser button and the resolve checkbox. There's some code already in place
		// to attempt that, but it wasn't worth the payoff for now. For now, there is a
		// little bit of top padding factored into the display of those picture elements.
		//private int m_dpiY;
		//private int m_chooserPadding = -1;
		//private int m_expanderPadding = -1;
		//private int m_resolutionStatusPadding = -1;
		//private Dictionary<stdole.IPicture, int> m_pixelHeights = new Dictionary<stdole.IPicture,int>();
		private float m_zoomPercent;
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
		private FilteredSequenceHandler m_notesSequenceHandler = null;
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
			m_zoomPercent = initialZoom;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the TeNotesVc class
		/// </summary>
		/// <param name="cache"></param>
		/// ------------------------------------------------------------------------------------
		public TeNotesVc(FdoCache cache) : base(TeStVc.LayoutViewTarget.targetDraft, -1)
		{
			m_wsDefault = cache.DefaultAnalWs;
			m_cache = cache;
			m_scr = cache.LangProject.TranslatedScriptureOA;
			m_scrBooks = new DBMultilingScrBooks((Scripture)m_scr);

			Image img = ResourceHelper.MinusBox;
			m_picMinus = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(img);
			//m_pixelHeights[m_picMinus] = img.Height;
			m_picPlus = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(ResourceHelper.PlusBox);
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
		private stdole.IPicture GetChooserImage()
		{
			Image img = ResourceHelper.ChooserButton;
			Rectangle rc = new Rectangle(0, 0, img.Width, img.Height);

			using (Graphics g = Graphics.FromImage(img))
			{
				VisualStyleElement element = VisualStyleElement.Button.PushButton.Normal;

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
				using (Font fnt = new Font("Arial", 9, FontStyle.Bold))
				{
					TextRenderer.DrawText(g, "...", fnt, rc,
						SystemColors.ControlDarkDark, TextFormatFlags.HorizontalCenter |
						TextFormatFlags.SingleLine | TextFormatFlags.Bottom);
				}
			}

			stdole.IPicture oleimg = (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(img);
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
		private ITsString MakeLabelFromText(string labelText, ScrScriptureNote ann)
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
			ScrScriptureNote ann)
		{
			ITsTextProps ttpLabel = StyleUtils.CharStyleTextProps(styleName, m_cache.DefaultUserWs);
			ITsStrBldr bldr = TsStrBldrClass.Create();

			if (ann != null && m_cache.IsValidObject(ann.BeginObjectRAHvo))
			{
				StTxtPara startPara = ann.BeginObjectRA as StTxtPara;

				if (startPara != null)
				{
					int ownerOwnFlid = m_cache.GetOwningFlidOfObject(startPara.OwnerHVO);
					if (ownerOwnFlid == (int)ScrBook.ScrBookTags.kflidFootnotes)
					{
						ITsPropsBldr propsBldr = TsPropsBldrClass.Create();
						propsBldr.SetStrPropValue((int)FwTextPropType.ktptFontFamily,
							"Marlett");
						bldr.Replace(0, 0, "\u0032", propsBldr.GetTextProps());
					}
				}
			}
			bldr.Replace(bldr.Length, bldr.Length, labelText, ttpLabel);

			// Add a space with default paragraph characters. This prevents style bleed-through
			// from the label when the user types in the text with (part of) the label selected.
			bldr.Replace(bldr.Length, bldr.Length, " ",
				StyleUtils.CharStyleTextProps(null,	m_cache.DefaultUserWs));
			return bldr.GetString();
		}

		///// ------------------------------------------------------------------------------------
		///// <summary>
		///// Gets the image padding.
		///// </summary>
		///// ------------------------------------------------------------------------------------
		//private int GetImagePadding(IVwEnv vwenv, stdole.IPicture pic)
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
				case (int)ScrFrags.kfrScripture:
				{
					vwenv.AddObjVecItems((int)Scripture.ScriptureTags.kflidBookAnnotations,
						this, (int)ScrFrags.kfrBook);
					break;
				}
				case (int)ScrFrags.kfrBook:
				{
					// REVIEW: This dependency causes the relevent portions of the view to be
					// re-laid out when the underlying sequence property (kflidNotes) changes
					// because PropChanged never gets called for the virtual property, though
					// maybe it should.
					vwenv.NoteDependency(new int[] { hvo },
						new int[] { (int)ScrBookAnnotations.ScrBookAnnotationsTags.kflidNotes }, 1);
					vwenv.AddLazyVecItems(VirtualNotesTag, this, (int)NotesFrags.kfrAnnotation);
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

					vwenv.AddPicture((m_expandTable.ContainsKey(hvo) && m_expandTable[hvo]) ?
						m_picMinus : m_picPlus,	m_cache.GetOwningFlidOfObject(hvo), 0, 0);

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
			get
			{
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Estimate the height of a lazy box
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>The height of an item in points</returns>
		/// ------------------------------------------------------------------------------------
		public override int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			CheckDisposed();

			// ENHANCE: May need to consider taking filter into consideration for getting better
			// estimates.
			switch (frag)
			{
				case (int)ScrFrags.kfrBook:
					// Assume that any annotations in a book lazy box are not expanded yet...
					int annCount = m_cache.GetVectorSize(hvo, (int)ScrBookAnnotations.ScrBookAnnotationsTags.kflidNotes);
					int height = 0;
					for (int i = 0; i < annCount; i++)
						height += EstimateHeight(m_cache.GetVectorItem(hvo, (int)ScrBookAnnotations.ScrBookAnnotationsTags.kflidNotes, i), (int)NotesFrags.kfrAnnotation, dxAvailWidth);
					return height;
				case (int)NotesFrags.kfrAnnotation:
					return (int)((m_expandTable.ContainsKey(hvo) ? 112 : 14) * m_zoomPercent);
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
			ScrScriptureNote ann = new ScrScriptureNote(m_cache, hvo);
			bool isExpanded = (m_expandTable.ContainsKey(hvo) && m_expandTable[hvo]);
			int hvoFirstResponse = ann.ResponsesOS.Count > 0 ? ann.ResponsesOS.HvoArray[0] : 0;
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
				(int)ScrScriptureNote.ScrScriptureNoteTags.kflidResponses}, 2);

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

			vwenv.NoteDependency(new int[] { ann.ResponsesOS.HvoArray[0] },
				new int[] { m_expansionTag }, 1);
			OpenTableRow(vwenv, ann);

			// Display empty first cell
			vwenv.OpenTableCell(1, 1);
			vwenv.CloseTableCell();

			// Display expand box (+/-) in the second cell
			vwenv.OpenTableCell(1, 1);
			vwenv.AddObj(ann.ResponsesOS.HvoArray[0], this, (int)NotesFrags.kfrExpansion);
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
				vwenv.AddObjVecItems((int)ScrScriptureNote.ScrScriptureNoteTags.kflidResponses,
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
		private void DisplayAnnotation(IVwEnv vwenv, ScrScriptureNote ann, bool expanded)
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
				vwenv.AddIntPropPic((int)ScrScriptureNote.ScrScriptureNoteTags.kflidResolutionStatus,
					this, (int)NotesFrags.kfrStatus, 0, 0);
			}
			else
			{
				vwenv.AddIntPropPic((int)ScrScriptureNote.ScrScriptureNoteTags.kflidResolutionStatus,
					this, (int)NotesFrags.kfrStatus, (int)NoteStatus.Open, (int)NoteStatus.Closed);
			}
			vwenv.CloseTableCell();

			// Display reference in the third cell and make it readonly.
			vwenv.OpenTableCell(1, 1);
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable, (int)FwTextPropVar.ktpvDefault, 0);
			vwenv.OpenParagraph();
			vwenv.AddProp((int)CmBaseAnnotation.CmBaseAnnotationTags.kflidBeginRef, this,
				(int)NotesFrags.kfrScrRef);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			// Display CONNOT category in the fourth (and possibly fifth and sixth) cell(s)
			vwenv.OpenTableCell(1, expanded ? 3 : 1);
			int hvoQuotePara = ann.QuoteOA.ParagraphsOS.HvoArray[0];
			bool fQuoteParaRtoL = IsParaRightToLeft(hvoQuotePara);

			// Conc paragraphs don't work well for R-to-L: If the text doesn't fit, it will
			// show the trailing text rather than the leading text.
			if (fQuoteParaRtoL || expanded)
				vwenv.OpenParagraph();
			else
				vwenv.OpenConcPara(0, 0, 0, 0);
			vwenv.AddObjVec((int)ScrScriptureNote.ScrScriptureNoteTags.kflidCategories, this,
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
				StTxtPara quotePara = (StTxtPara)ann.QuoteOA.ParagraphsOS[0];
				SetupWsAndDirectionForPara(vwenv, quotePara.Hvo);
				if (fQuoteParaRtoL)
					vwenv.OpenParagraph(); // Conc paragraphs don't work well for R-to-L
				else
					vwenv.OpenConcPara(0, 0, 0, 0);
				vwenv.AddString(quotePara.Contents.UnderlyingTsString);
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
				(int)ScrScriptureNote.ScrScriptureNoteTags.kflidQuote,
				ann.QuoteOAHvo, ann.QuoteOA,
				fRegularAnnotation ? m_quoteLabel : m_detailsLabel, !fRegularAnnotation);

			// Third row has discussion
			DisplayExpandableAnnotation(vwenv, ann,
				(int)ScrScriptureNote.ScrScriptureNoteTags.kflidDiscussion,
				ann.DiscussionOAHvo, ann.DiscussionOA,
				fRegularAnnotation ? m_discussionLabel : m_messageLabel, !fRegularAnnotation);

			// Fourth row has recommendation (i.e. suggestion)
			DisplayExpandableAnnotation(vwenv, ann,
				(int)ScrScriptureNote.ScrScriptureNoteTags.kflidRecommendation,
				ann.RecommendationOAHvo, ann.RecommendationOA, m_suggestionLabel);

			// Fifth row has resolution
			DisplayExpandableAnnotation(vwenv, ann,
				(int)ScrScriptureNote.ScrScriptureNoteTags.kflidResolution,
				ann.ResolutionOAHvo, ann.ResolutionOA, m_resolutionLabel);

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
			vwenv.AddTimeProp((int)ScrScriptureNote.CmAnnotationTags.kflidDateCreated, 0);
			vwenv.CloseParagraph();
			vwenv.CloseTableCell();

			// Display date modified in fourth/fifth cells
			vwenv.OpenTableCell(1, 2);
			vwenv.OpenParagraph();
			vwenv.AddString(m_modifiedLabel);
			vwenv.AddTimeProp((int)CmAnnotation.CmAnnotationTags.kflidDateModified, 0);
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
		/// <param name="v">Not used. (usually <c>null</c>)</param>
		/// <param name="frag">Fragment to identify what type of prompt should be created.</param>
		/// <returns>
		/// ITsString that will be displayed as user prompt.
		/// </returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
		{
			if (frag == (int)NotesFrags.kfrScrRef)
			{
				ScrScriptureNote ann = new ScrScriptureNote(Cache, vwenv.CurrentObject());
				BCVRef startRef = new BCVRef(ann.BeginRef);
				string bookAbbrev = m_scrBooks.GetBookAbbrev(startRef.Book);
				string sAnnRef = ScrReference.MakeReferenceString(bookAbbrev, ann.BeginRef,
					ann.EndRef, m_scr.ChapterVerseSepr, m_scr.Bridge);
				return MakeLabelFromText(sAnnRef, ann);
			}
			return base.DisplayVariant(vwenv, tag, v, frag);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays an expandable annotation of the specified type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayExpandableAnnotation(IVwEnv vwenv, ScrScriptureNote ann,
			int tag, int hvo, IStJournalText paras, ITsString label)
		{
			DisplayExpandableAnnotation(vwenv, ann, tag, hvo, paras, label, false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays an expandable annotation of the specified type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void DisplayExpandableAnnotation(IVwEnv vwenv, ScrScriptureNote ann,
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
				vwenv.AddString(((StTxtPara)paras.ParagraphsOS[0]).Contents.UnderlyingTsString);

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
			StJournalText text = new StJournalText(m_cache, hvo);

			// Open a table to display the the response.
			VwLength vlTable; // we use this to specify that the table takes 100% of the width.
			vlTable.nVal = 10000;
			vlTable.unit = VwUnit.kunPercent100;

			VwFramePosition framePos = VwFramePosition.kvfpVoid;
			if (text.OwnerHVO == SelectedNoteHvo)
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
		private void DisplayResponse(IVwEnv vwenv, StJournalText text, bool expanded)
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
			vwenv.AddObjVecItems((int)StText.StTextTags.kflidParagraphs, this, (int)StTextFrags.kfrPara);
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
		private void SetBackgroundColorForNote(ScrScriptureNote ann, IVwEnv vwenv)
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
		private Color GetNoteBackgroundColor(ScrScriptureNote ann)
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
		/// <param name="root">The rootbox of the caller, which will be notified of the change
		/// (hence causing the appropriate fragments in this VC to get laid out)</param>
		/// <returns>true if the item is expanded. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		public bool ToggleItemExpansion(int hvo, IVwRootBox root)
		{
			CheckDisposed();

			if (m_expandTable.ContainsKey(hvo))
				m_expandTable.Remove(hvo);
			else
			{
				m_expandTable[hvo] = true;
				OpenNoteFieldsWithContent(hvo);
			}

			root.PropChanged(hvo, m_expansionTag, 0, 0, 0);
			return m_expandTable.ContainsKey(hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Opens annotation fields that have content.
		/// </summary>
		/// <param name="hvo">The ID of the object to expand (no action taken if the object is
		/// not a ScrScriptureNote</param>
		/// <returns>true if the item is expanded. Otherwise, false.</returns>
		/// ------------------------------------------------------------------------------------
		private void OpenNoteFieldsWithContent(int hvo)
		{
			// Confirm that this object is a ScrScriptureNote
			if (m_cache.GetClassOfObject(hvo) != ScrScriptureNote.kClassId)
				return; // This hvo is not for a ScrScriptureNote

			ScrScriptureNote note = new ScrScriptureNote(m_cache, hvo);

			// Expand fields if they contain content and aren't already open.
			if (!string.IsNullOrEmpty(((StTxtPara)note.QuoteOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.QuoteOAHvo);

			if (!string.IsNullOrEmpty(((StTxtPara)note.DiscussionOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.DiscussionOAHvo);

			if (!string.IsNullOrEmpty(((StTxtPara)note.RecommendationOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.RecommendationOAHvo);

			if (!string.IsNullOrEmpty(((StTxtPara)note.ResolutionOA.ParagraphsOS[0]).Contents.Text))
				ExpandItem(note.ResolutionOAHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Expand the item if it is collapsed.
		/// </summary>
		/// <param name="hvo">hvo of the item to expand</param>
		/// ------------------------------------------------------------------------------------
		public void ExpandItem(int hvo)
		{
			CheckDisposed();

			if (!m_expandTable.ContainsKey(hvo))
			{
				m_expandTable[hvo] = true;
				m_cache.PropChanged(null, PropChangeType.kpctNotifyAll, hvo,
					m_expansionTag, 0, 0, 0);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Collapses all annotations.
		/// </summary>
		/// <param name="root">The rootbox of the caller, which will be notified of the change
		/// (hence causing the appropriate fragments in this VC to get laid out)</param>
		/// ------------------------------------------------------------------------------------
		public void CollapseAllAnnotations(IVwRootBox root)
		{
			// Make a copy of the table (since collapsing causes keys to be deleted from the table
			// and we are enumerating through it).
			Dictionary<int, bool> expandTable = new Dictionary<int, bool>(m_expandTable);

			foreach (int annHvo in expandTable.Keys)
			{
				// Confirm that the object is an annotation because we only want to collapse at
				// the annotation level so that we can maintain the expansion status of the
				// fields within the annotation.
				if (m_cache.GetClassOfObject(annHvo) != ScrScriptureNote.kClassId)
					continue; // Expansion field is not for an annotation

				if (IsExpanded(annHvo))
					ToggleItemExpansion(annHvo, root);
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
			CheckDisposed();
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
		public override stdole.IPicture DisplayPicture(IVwEnv vwenv, int hvo, int tag, int val, int frag)
		{
			CheckDisposed();
			Debug.Assert(frag == (int)NotesFrags.kfrStatus);
			Debug.Assert(tag == (int)ScrScriptureNote.ScrScriptureNoteTags.kflidResolutionStatus);
			Image img;
			ButtonState state = ButtonState.Normal;
			ScrScriptureNote ann = new ScrScriptureNote(m_cache, hvo);
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

			return (stdole.IPicture)OLECvt.ToOLE_IPictureDisp(img);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display the categories as a comma-separated string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();

			Debug.Assert(frag == (int)NotesFrags.kfrConnotCategory);
			Debug.Assert(tag == (int)ScrScriptureNote.ScrScriptureNoteTags.kflidCategories);

			ScrScriptureNote ann = new ScrScriptureNote(m_cache, hvo);

			bool fFirst = true;
			foreach (CmPossibility cat in ann.CategoriesRS)
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
			uint transparent = 0xC0000000; // FwTextColor.kclrTransparent won't convert to uint
			vwenv.AddSimpleRect(transparent, -1, 7000, 0);
			vwenv.AddSimpleRect(ColorUtil.ConvertColorToBGR(ColorUtil.LightInverse(m_BackColor)), -1, 1000, 0);
			vwenv.AddSimpleRect(transparent, -1, 4000, 0);
		}
		#endregion

		#region Properties
		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Property to get the tag to use for Scripture Books. Normally, this will be a virtual
		/// tag for a filtered list of books.
		/// </summary>
		///  ------------------------------------------------------------------------------------
		public int VirtualNotesTag
		{
			get
			{
				if (m_notesSequenceHandler != null)
					return m_notesSequenceHandler.Tag;
				else
					return (int)ScrBookAnnotations.ScrBookAnnotationsTags.kflidNotes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the handler used for filtering and sorting notes
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FilteredSequenceHandler NotesSequenceHandler
		{
			get
			{
				CheckDisposed();
				return m_notesSequenceHandler;
			}
			set
			{
				CheckDisposed();
				m_notesSequenceHandler = value;
			}
		}

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
