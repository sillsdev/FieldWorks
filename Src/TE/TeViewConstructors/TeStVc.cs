// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2004' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeStVc.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.Utils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region TeViewGroup enum
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Determines the group the height estimates will come from when constructing the view
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum TeViewGroup
	{
		/// <summary>
		/// Includes DraftView, PrintLayout, BackTranslation, and KeyTerms (they should have
		/// the same paragraph layout)
		/// </summary>
		Scripture,
		/// <summary>
		/// FootnoteView and FootnoteBackTranlationView
		/// </summary>
		Footnote,
	}
	#endregion

	#region Scripture fragments
	///  ----------------------------------------------------------------------------------------
	/// <summary>
	/// Possible scripture fragments
	/// </summary>
	///  ----------------------------------------------------------------------------------------
	public enum ScrFrags: int
	{
		/// <summary></summary>
		kfrGroup = 111, // debug is easier if different range from tags
		/// <summary>Scripture reference</summary>
		kfrScrRef,
		/// <summary>Detail line</summary>
		kfrDetailLine,
		/// <summary>Reference paragraph</summary>
		kfrRefPara,
		/// <summary>Scripture</summary>
		kfrScripture,
		/// <summary>A book</summary>
		kfrBook,
		/// <summary>Book separator</summary>
		kfrSeparator,
		/// <summary>A section</summary>
		kfrSection,
		/// <summary>Context</summary>
		kfrContext,
		/// <summary>Count</summary>
		kfrCount,
		/// <summary>Display an StText by showing the style of each paragraph.</summary>
		kfrTextStyles,
		/// <summary>Display an StTxtPara by showing the name of its style.</summary>
		kfrParaStyles,
		/// <summary>Display the status for a back translation.</summary>
		kfrBtTranslationStatus,
	}
	#endregion

	#region Footnote fragments
	///  ----------------------------------------------------------------------------------------
	/// <summary>
	/// Possible footnote fragments
	/// </summary>
	///  ----------------------------------------------------------------------------------------
	public enum FootnoteFrags: int
	{
		/// <summary>Scripture</summary>
		kfrScripture = 333, // debug is easier if different range from tags
		/// <summary>A book</summary>
		kfrBook,
		/// <summary>Display a Footnote by showing the style of each paragraph.</summary>
		kfrFootnoteStyles,
		/// <summary>
		/// Display the footnotes of a dummy Scripture object used for a root box which
		/// displays one page worth of footnotes.
		/// </summary>
		kfrRootInPageSeq,
		/// <summary>
		/// For AddObjVec/DisplayVec, the complete sequence of footnotes on the page.
		/// </summary>
		kfrAllFootnotesWithinPagePara,
		/// <summary>
		/// One footnote displayed within the single paragraph that smushes all the footnotes
		/// on a particular page.
		/// </summary>
		kfrFootnoteWithinPagePara,
		/// <summary>
		/// One (and only!) footnote 'paragraph' that in a smushed view is displayed as part of
		/// a larger paragraph.
		/// </summary>
		kfrFootnoteParaWithinPagePara,
	};
	#endregion

	#region Notes fragments
	///  ----------------------------------------------------------------------------------------
	/// <summary>
	/// Possible notes (annotations) fragments
	/// </summary>
	///  ----------------------------------------------------------------------------------------
	public enum NotesFrags: int
	{
		/// <summary>A ScrScriptureNote</summary>
		kfrAnnotation = 444,
		/// <summary>annotation's expansion status</summary>
		kfrExpansion,
		/// <summary>annotation's Status</summary>
		kfrStatus,
		/// <summary>annotation's CONNOT Category</summary>
		kfrConnotCategory,
		/// <summary>annotation's Quote</summary>
		kfrQuote,
		/// <summary>annotation's Discussion or text of a response</summary>
		kfrText,
		/// <summary>annotation's Suggestion</summary>
		kfrSuggestion,
		/// <summary>annotation's Resolution</summary>
		kfrResolution,
		/// <summary>annotation's Author</summary>
		kfrAuthor,
		/// <summary>annotation's Created Date</summary>
		kfrCreatedDate,
		/// <summary>annotation's Modified Date</summary>
		kfrModifiedDate,
		/// <summary>annotation's Resolution Date</summary>
		kfrResolvedDate,
		/// <summary>List of responses to an annotation</summary>
		kfrResponse,
		/// <summary>The Scripture reference of the annotation (can be a ref range)</summary>
		kfrScrRef,
	};
	#endregion

	#region TeStVc class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TE specific stuff for StText view constructor
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TeStVc: StVc
	{
		#region Member variables
		/// <summary></summary>
		protected LayoutViewTarget m_target;
		/// <summary>
		/// Used to save objects where prompts have been updated. This will allow prompt to be
		/// turned off when delete is used, even though paragraph is still empty.
		/// </summary>
		private Set<int> m_updatedPrompts;
		/// <summary></summary>
		protected int m_filterInstance = -1;
		/// <summary></summary>
		protected int m_booksTag = 0;

		// can't be static because different databases have different default ws
		private ITsTextProps m_captionProps;

		/// <summary>
		/// This variable is initialized in case (int)StTextFrags.kfrPara: of Display() if the paragraph in question
		/// is one that needs special display properties. NB: this means that we can't handle independent edits
		/// of text within that paragraph. For example, if something changes the text of the paragraph but does
		/// not cause a PropChanged that redisplays the whole paragraph object, the display properties will
		/// not be updated and confusion or even a crash may result.
		/// </summary>
		protected List<DispPropOverride> m_DispPropOverrides = new List<DispPropOverride>();
		/// <summary>
		/// The paragraph to which m_DispPropOverrides currently applies. (Nb: DiffView subclass
		/// ignores this and may override on more than one paragraph.)
		/// </summary>
		private IScrTxtPara m_overridePara;
		/// <summary>
		/// The initilizer defines the properties that will be applied to the relevant run of the override paragraph.
		/// </summary>
		private DispPropInitializer m_overrideInitializer;
		/// <summary>
		/// The range of characters that should have the override property.
		/// </summary>
		private int m_ichMinOverride;
		private int m_ichLimOverride;
		private int m_hvoOfSegmentWhoseBtPromptIsToBeSupressed;
		#endregion

		#region LayoutViewTarget enum
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines the layout target of the view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public enum LayoutViewTarget
		{
			/// <summary></summary>
			targetDraft,
			/// <summary></summary>
			targetPrint,
			/// <summary></summary>
			targetStyleBar
		};
		#endregion

		#region Delegates
		/// <summary>
		/// A delegate which takes (typically modifies) a DispPropOverride.
		/// </summary>
		/// <param name="prop"></param>
		public delegate void DispPropInitializer(ref DispPropOverride prop);
		#endregion

		#region Constructor
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeStVc"/> class.
		/// </summary>
		/// <param name="target">The LayoutViewTarget</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// ------------------------------------------------------------------------------------
		public TeStVc(LayoutViewTarget target, int filterInstance)
		{
			m_target = target;
			m_filterInstance = filterInstance;
			m_updatedPrompts = new Set<int>();
		}
		#endregion

		#region Properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the ITsTextProps to apply to the caption of pictures
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override ITsTextProps CaptionProps
		{
			get
			{
				if (m_captionProps == null)
				{
					ITsPropsBldr bldr = TsPropsBldrClass.Create();
					bldr.SetStrPropValue((int)FwTextPropType.ktptNamedStyle, ScrStyleNames.Figure);
					m_captionProps = bldr.GetTextProps();
				}
				return m_captionProps;
			}
		}

		/// <summary>
		/// return true if the view displays books (and hence BooksTag may be called).
		/// </summary>
		protected virtual bool HasBooks
		{
			get
			{
				return true;
			}
		}

		///  ------------------------------------------------------------------------------------
		/// <summary>
		/// Property to get the tag to use for Scripture Books. Normally, this will be a virtual
		/// tag for a filtered list of books. Nb: in the subclass TeNotesVc, there are no books,
		/// and calling this will crash (or at least Assert).
		/// </summary>
		///  ------------------------------------------------------------------------------------
		protected int BooksTag
		{
			get
			{
				if (m_booksTag == 0) // if we haven't calculated and cached the value yet, do it.
				{
					FilteredScrBooks bookFilter = Cache.ServiceLocator.GetInstance<IFilteredScrBookRepository>().GetFilterInstance(m_filterInstance);
					m_booksTag = (bookFilter != null) ? bookFilter.Tag :
						ScriptureTags.kflidScriptureBooks;
				}
				return m_booksTag;
			}
		}
		#endregion

		#region Overridden methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Call the appropriate "OpenPara" method.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo">the HVO of the paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected override void OpenPara(IVwEnv vwenv, int paraHvo)
		{
			IStTxtPara para = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraHvo);
			if (para == m_overridePara)
			{
				m_DispPropOverrides.Clear();
				MakeDispPropOverrides(m_overridePara, m_ichMinOverride, m_ichLimOverride,
					m_overrideInitializer);
				vwenv.OpenOverridePara(m_DispPropOverrides.Count, m_DispPropOverrides.ToArray());
			}
			else
				base.OpenPara(vwenv, paraHvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID.
		/// </summary>
		/// <param name="bstrGuid">The guid for the object for which we want to get a string.</param>
		/// <returns>The ITsString to display for the specified guid. If the object does not
		/// exist, then the string will be set to &lt;missing object&gt;. In the case of a
		/// (moveable) picture, we return null, which signals to the caller that it should
		/// build the appropriate string for displaying a picture.</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString GetStrForGuid(string bstrGuid)
		{
			Debug.Assert(Cache != null);
			Debug.Assert(bstrGuid.Length == 8);

			Guid guid = MiscUtils.GetGuidFromObjData(bstrGuid);

			IScrFootnote footnote;
			if (m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().TryGetObject(guid, out footnote))
			{
				ITsStrBldr bldr = footnote.MakeFootnoteMarker(DefaultWs);
				if (bldr.Length == 0 && Options.ShowMarkerlessIconsSetting)
				{
					if (!PrintLayout) // don't display markerless icon in print layout view
						return GetFootnoteIconString(DefaultWs, guid);
				}
				else if (bldr.Length > 0)
				{
					// This is to prevent the word-wrap from wrapping at the beginning of
					// a footnote marker.
					// REVIEW (TimS): \uFEFF is deprecated in Unicode now.  Should use
					//                \u2040 but no fonts seem to have it defined. Should
					//                maybe change this eventually.
					string directionIndicator = (RightToLeft) ? "\u200F" : "\u200E";
					bldr.Replace(0, 0, directionIndicator + "\uFEFF" + directionIndicator, null);
				}

				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptEditable,
					(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);

				return bldr.GetString();
			}

			// Couldn't find a footnote object. Try looking to see if it is a picture. If the
			// GUID can't be found in the picture repository either, then return a
			// <missing object> string.
			ICmPicture picture;
			return m_cache.ServiceLocator.GetInstance<ICmPictureRepository>().TryGetObject(guid, out picture) ?
				null : Cache.TsStrFactory.MakeString("<missing object>", Cache.WritingSystemFactory.UserWs);

		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert end of paragraph marks if needed.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="paraHvo"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InsertEndOfParaMarks(IVwEnv vwenv, int paraHvo)
		{
			if (Options.ShowFormatMarksSetting && m_target == LayoutViewTarget.targetDraft)
			{
				IStPara para = m_cache.ServiceLocator.GetInstance<IStParaRepository>().GetObject(paraHvo);
				// If this is the last paragraph of a section then insert an
				// end of section mark, otherwise insert a paragraph mark.
				VwBoundaryMark boundaryMark = (para.IsFinalParaInText) ?
					VwBoundaryMark.endOfSection : VwBoundaryMark.endOfParagraph;
				vwenv.SetParagraphMark(boundaryMark);
				int flid = m_cache.ServiceLocator.GetInstance<Virtuals>().StParaIsFinalParaInText;
				vwenv.NoteDependency(new [] { paraHvo}, new [] { flid}, 1);
			}
		}
		#endregion

		#region User prompt stuff
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// In certain contexts, check the given paragraph to see if it is empty. If so,
		/// insert a user prompt.
		/// </summary>
		/// <param name="vwenv">view environment</param>
		/// <param name="paraHvo">HVO of the paragraph to be displayed</param>
		/// <returns>true if an empty string was substituted for missing/empty StText</returns>
		/// -----------------------------------------------------------------------------------
		protected override bool InsertParaContentsUserPrompt(IVwEnv vwenv, int paraHvo)
		{
			Debug.Assert(!DisplayTranslation);

			// No user prompt in any of these conditions
			IStTxtPara para = m_cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(paraHvo);
			if (!(para is IScrTxtPara)
				|| !Options.ShowEmptyParagraphPromptsSetting // tools options setting
				|| m_target == LayoutViewTarget.targetPrint // any print layout view
				|| m_updatedPrompts.Contains(para.Hvo)) // user interaction has updated prompt
			{
				return false;
			}

			// User prompt is only for title & heading paras
			IStText text = (IStText)para.Owner; // para owner
			if (text.OwningFlid != ScrBookTags.kflidTitle &&
				text.OwningFlid != ScrSectionTags.kflidHeading)
				return false;

			int paraCount = text.ParagraphsOS.Count;
			Debug.Assert(paraCount != 0,
				"We shouldn't come here if StText doesn't contain any paragraphs");
			// By design, if there is more than one para, don't display the user prompt.
			if (paraCount != 1)
				return false;

			// If first para is empty, insert user prompt for paragraph content
			if (text[0].Contents.Text == null)
			{
				vwenv.NoteDependency(new int[] { para.Hvo },
					new int[] { StTxtParaTags.kflidContents}, 1);
				vwenv.AddProp(SimpleRootSite.kTagUserPrompt, this, text.OwningFlid);
				return true;
			}

			return false;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Check the given translation to see if the text is empty. If so, then insert a
		/// user prompt.
		/// </summary>
		/// <param name="vwenv">view environment</param>
		/// <param name="trans">translation to be displayed</param>
		/// <returns>true if an empty string was substituted for missing/empty StText</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool InsertTranslationUserPrompt(IVwEnv vwenv, ICmTranslation trans)
		{
			// No user prompt in any of these conditions
			if (trans == null || m_updatedPrompts.Contains(trans.Hvo)) // user interaction has updated prompt
				return false;

			// If there is text in the translation then do not add a prompt.
			if (trans.Translation.get_String(m_wsDefault).Length > 0)
				return false;

			// If there is no text in the parent paragraph then do not place a prompt in the
			// back translation.
			if (((IStTxtPara)trans.Owner).Contents.Length == 0)
				return false;

			// Insert the prompt.
			vwenv.NoteDependency(new int[] { trans.Hvo },
				new int[] { CmTranslationTags.kflidTranslation}, 1);
			vwenv.AddProp(SimpleRootSite.kTagUserPrompt, this, (int)CmTranslationTags.kflidTranslation);
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a user prompt for an empty paragraph.
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// <param name="tag">Tag</param>
		/// <param name="frag">Fragment to identify what type of prompt should be created.</param>
		/// <returns>ITsString that will be displayed as user prompt.</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, int frag)
		{
			string userPrompt = null;
			Color promptColor = Color.LightGray;
			int promptWs = Cache.DefaultUserWs;
			switch (frag)
			{
				case ScrBookTags.kflidTitle:
					ICmObject obj = Cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(vwenv.CurrentObject());
					IScrBook book = obj.Owner.Owner as IScrBook;
					userPrompt = string.Format(TeResourceHelper.GetResourceString("kstidBookTitleUserPrompt"),
						book.Name.UserDefaultWritingSystem.Text);
					break;
				case ScrSectionTags.kflidHeading:
					userPrompt = TeResourceHelper.GetResourceString("kstidSectionHeadUserPrompt");
					break;
				case CmTranslationTags.kflidTranslation:
					if (!PrintLayout && Editable)
						userPrompt = TeResourceHelper.GetResourceString("kstidBackTranslationUserPrompt");
					else
						userPrompt = TeResourceHelper.GetResourceString("kstidBackTranslationUserPromptPrintLayout");
					break;
				case SegmentTags.kflidFreeTranslation:
					// A missing BT of a segment in an interlinear draft back translation.
					if (!PrintLayout && Editable)
					{
						// There are typically several of these in a paragraph so we want to be subtle.
						// Also we have a difference of colors: this view has a subtle BG color to show everything
						// except the BTs is non-editable. The prompt needs to be white to show it IS editable.
						userPrompt = "\xA0\xA0\xA0";
						// Three non-breaking spaces (trying not altogether successfully to prevent disappearing at margin)
						promptColor = SystemColors.Window;
					}
					else
					{
						userPrompt = TeResourceHelper.GetResourceString("kstidBackTranslationUserPromptPrintLayout");
					}
					break;
				default:
					Debug.Assert(false, "DisplayVariant called with unexpected fragment");
					break;
			}

			if (userPrompt == null)
				userPrompt = string.Empty;

			ITsPropsBldr ttpBldr = TsPropsBldrClass.Create();
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptBackColor,
				(int) FwTextPropVar.ktpvDefault, (int)ColorUtil.ConvertColorToBGR(promptColor));
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int) FwTextPropVar.ktpvDefault, promptWs);

			// The use of the user-defined property here is to indicate that this is a
			// user prompt string.
			ttpBldr.SetIntPropValues(SimpleRootSite.ktptUserPrompt,
				(int)FwTextPropVar.ktpvDefault, 1);
			ttpBldr.SetIntPropValues((int)FwTextPropType.ktptSpellCheck,
				(int)FwTextPropVar.ktpvEnum, (int)SpellingModes.ksmDoNotCheck);

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, userPrompt, ttpBldr.GetTextProps());
			// Begin the prompt with a zero-width space in the view's default writing system.
			// 200B == zero-width space.
			// This helps select the correct keyboard when the user selects the user prompt and
			// begins typing. Also prevent the DoNotSpellCheck and back color being applied
			// to what is typed.
			ITsPropsBldr ttpBldr2 = TsPropsBldrClass.Create();
			ttpBldr2.SetIntPropValues((int)FwTextPropType.ktptWs,
				(int)FwTextPropVar.ktpvDefault, m_wsDefault);
			ttpBldr2.SetIntPropValues(SimpleRootSite.ktptUserPrompt,
				(int)FwTextPropVar.ktpvDefault, 1);
			bldr.Replace(0, 0, "\u200B", ttpBldr2.GetTextProps());
			return bldr.GetString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Replace the user prompt with the text the user typed.  This method is called from
		/// the views code when the user prompt is edited.
		/// </summary>
		/// <param name="vwsel">Current selection in rootbox where this prop was updated</param>
		/// <param name="hvo">Hvo of the paragraph/string/segment whose contents are being
		/// changed</param>
		/// <param name="tag">Tag (must be SimpleRootSite.kTagUserPrompt)</param>
		/// <param name="frag">Owning flid of the text/object that owns the paragraph/string/
		/// segment whose user prompt is being replaced with typed text</param>
		/// <param name="tssVal">Text the user just typed</param>
		/// <returns>possibly modified ITsString.</returns>
		/// <remarks>The return value is currently ignored in production code, but we use it
		/// in our tests.</remarks>
		/// -----------------------------------------------------------------------------------
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag,
			ITsString tssVal)
		{
			Debug.Assert(tag == SimpleRootSite.kTagUserPrompt, "Got an unexpected tag");
			Debug.Assert(vwsel != null, "Got a null selection!");
			Debug.Assert(vwsel.IsValid, "Got an invalid selection!");
			IVwRootBox rootbox = vwsel.RootBox;

			// If a (typically Chinese) character composition is in progress, replacing the prompt will
			// destroy the selection and end the composition, causing weird typing problems (TE-8267).
			// Ending the composition does another Commit, which ensures that this will eventually be
			// called when there is NOT a composition in progress.
			if (rootbox.IsCompositionInProgress)
				return tssVal;

			// Remove the UserPrompt pseudo-property from the text the user typed.
			// when appropriate also ensure the correct writing system.
			// The correct WS is m_wsDefault in the view constructor
			ITsStrBldr bldr = tssVal.GetBldr();
			if (frag != SegmentTags.kflidFreeTranslation)
			{
				bldr.SetIntPropValues(0, bldr.Length, (int)FwTextPropType.ktptWs,
					(int)FwTextPropVar.ktpvDefault, m_wsDefault);
			}
			// Delete the user prompt property from the string (TE-3994)
			bldr.SetIntPropValues(0, bldr.Length, SimpleRootSite.ktptUserPrompt, -1, -1);
			tssVal = bldr.GetString();

			// Get information about current selection
			int cvsli = vwsel.CLevels(false);
			cvsli--; // CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			int ihvoRoot;
			int tagTextProp_Ignore;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd;
			// Prior to the Commit in selection changed which causes this UpdateProp to be called,
			// earlier selection changed code has expanded the selection (because it is in a user prompt)
			// to the whole prompt. It is therefore a range selection, and the value of fAssocPrev we got
			// is useless.
			bool fAssocPrev_Ignore;
			int ws;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp_Ignore, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev_Ignore, out ihvoEnd, out ttp);

			int tagTextProp;
			ITsTextProps props = null;

			if (frag == SegmentTags.kflidFreeTranslation)
			{
				// If the length is zero...we need to suppress replacing the comment with a prompt.
				if (tssVal.Length == 0)
					m_hvoOfSegmentWhoseBtPromptIsToBeSupressed = hvo;
				ISegment seg = Cache.ServiceLocator.GetInstance<ISegmentRepository>().GetObject(hvo);
				if (seg.FreeTranslation.get_String(BackTranslationWS).Length == 0)
				{
					// Undo needs to unset suppressing the comment prompt.
					Cache.ActionHandlerAccessor.AddAction(new UndoSuppressBtPrompt(this, seg));
				}

				ws = BackTranslationWS;
				tagTextProp = frag;
				seg.FreeTranslation.set_String(ws, tssVal);
				rootbox.PropChanged(seg.Paragraph.Owner.Hvo, StTextTags.kflidParagraphs,
					seg.Paragraph.IndexInOwner, 1, 1);
			}
			else
			{
				ReplacePromptUndoAction undoAction = new ReplacePromptUndoAction(hvo, rootbox, m_updatedPrompts);

				if (m_cache.ActionHandlerAccessor != null)
				{
					m_cache.ActionHandlerAccessor.AddAction( new UndoSelectionAction(rootbox.Site, true, vwsel));
					m_cache.ActionHandlerAccessor.AddAction(undoAction);
				}

				// Mark the user prompt as having been updated - will not show prompt again.
				// Note: ReplacePromptUndoAction:Undo removes items from the Set.
				m_updatedPrompts.Add(hvo);

				// Replace the ITsString in the paragraph or translation
				props = StyleUtils.CharStyleTextProps(null, m_wsDefault);
				if (frag == CmTranslationTags.kflidTranslation)
				{
					ICmTranslation trans = Cache.ServiceLocator.GetInstance<ICmTranslationRepository>().GetObject(hvo);
					trans.Translation.set_String(m_wsDefault, tssVal);
					undoAction.ParaHvo = trans.Owner.Hvo;
					ws = BackTranslationWS;
					tagTextProp = frag;
				}
				else
				{
					IStTxtPara para = Cache.ServiceLocator.GetInstance<IStTxtParaRepository>().GetObject(hvo);
					para.Contents = tssVal;
					undoAction.ParaHvo = hvo;
					ws = 0;
					tagTextProp = StTxtParaTags.kflidContents;
				}
				// Do a fake propchange to update the prompt
				rootbox.PropChanged(undoAction.ParaHvo, StParaTags.kflidStyleRules, 0, 1, 1);
			}

			// Now request a selection at the end of the text that was just put in.
			rootbox.Site.RequestSelectionAtEndOfUow(rootbox, ihvoRoot, cvsli, rgvsli,
				tagTextProp, cpropPrevious, ichEnd, ws, true, props);

			return tssVal;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given that segment is a 'label' segment (typically an embedded identifying number),
		/// figure out what to display as the corresponding label in a back translation view.
		/// This is made separate so that TeStVc can override to use a different numbering scheme
		/// in the BT.
		/// </summary>
		/// <param name="segment"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected override ITsString GetBackTransLabelText(ISegment segment)
		{
			ITsString tssLabel = segment.BaselineText;
			if (tssLabel == null)
				return null;
			IScripture scr = segment.Cache.LangProject.TranslatedScriptureOA;
			return scr.ConvertCVNumbersInStringForBT(tssLabel, BackTranslationWS);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the hvo of the segment whose back translation prompt is to be supressed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int HvoOfSegmentWhoseBtPromptIsToBeSupressed
		{
			get { return m_hvoOfSegmentWhoseBtPromptIsToBeSupressed; }
			set { m_hvoOfSegmentWhoseBtPromptIsToBeSupressed = value;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Suppress displaying a user prompt (typically because the user typed backspace or
		/// delete at the prompt).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override bool SuppressPrompt(int hvo, int tag)
		{
			return (hvo == m_hvoOfSegmentWhoseBtPromptIsToBeSupressed && tag == SegmentTags.kflidFreeTranslation);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the flags that are set when user prompts are updated. Clearing the flags
		/// will allow the user prompts to be shown again.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearUserPromptUpdates()
		{
			m_updatedPrompts.Clear();
		}
		#endregion

		#region Methods for highlighting text
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set up the specified overrides (as controlled by the initializer) for the specified
		/// range of characters in the specified paragraph. Return true if anything changed.
		/// </summary>
		/// <param name="para">The paragraph .</param>
		/// <param name="ichMin">The index (in logical characters, relative to
		/// para.Contents) of the first character whose properties will be overridden.</param>
		/// <param name="ichLim">The character "limit" (in logical characters,
		/// relative to para.Contents) of the text whose properties will be overridden.</param>
		/// <param name="initializer">A delegate that modifies the display props of characters
		/// between ichMin and ichLim.</param>
		/// <param name="sourceRootBox">The source root box.</param>
		/// <returns><c>true</c> if the text overrides changed; <c>false</c> otherwise.</returns>
		/// ------------------------------------------------------------------------------------
		public bool SetupOverrides(IScrTxtPara para, int ichMin, int ichLim,
			DispPropInitializer initializer, IVwRootBox sourceRootBox)
		{
			// ENHANCE JohnT: if more than one thing uses this at the same time, it may become necessary to
			// check whether we're setting up the same initializer. I don't know how to do this. Might need
			// another argument and variable to keep track of a type of override.
			if (para == m_overridePara && ichMin == m_ichMinOverride && ichLim == m_ichLimOverride)
				return false;

			if (m_overridePara != null && m_overridePara.IsValidObject)
			{
				// remove the old override.
				IStTxtPara oldPara = m_overridePara;
				m_overridePara = null; // remove it so the redraw won't show highlighting!
				UpdateDisplayOfPara(oldPara, sourceRootBox);
			}

			m_ichMinOverride = ichMin;
			m_ichLimOverride = ichLim;
			m_overridePara = para;
			m_overrideInitializer = initializer;
			if (m_overridePara != null && m_overridePara.IsValidObject)
			{
				// show the new override.
				UpdateDisplayOfPara(m_overridePara, sourceRootBox);
			}
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make the DispPropOverrides we need to give the specified run of characters the
		/// display properties defined by the initializer.
		/// </summary>
		/// <param name="para">The paragraph.</param>
		/// <param name="paraMinHighlight">The index (in logical characters, relative to
		/// para.Contents) of the first character whose properties will be overridden.</param>
		/// <param name="paraLimHighlight">The character "limit" (in logical characters,
		/// relative to para.Contents) of the text whose properties will be overridden.</param>
		/// <param name="initializer">A delegate that modifies the display props of characters
		/// between ichMin and ichLim.</param>
		/// ------------------------------------------------------------------------------------
		protected void MakeDispPropOverrides(IScrTxtPara para, int paraMinHighlight,
			int paraLimHighlight, DispPropInitializer initializer)
		{
			if (paraLimHighlight < paraMinHighlight)
				throw new ArgumentOutOfRangeException("paraLimHighlight", "ParaLimHighlight must be greater or equal to paraMinHighlight");

			int offsetToStartOfParaContents = 0;
			if (para.Owner is IScrFootnote && para.IndexInOwner == 0)
			{
				IScrFootnote footnote = (IScrFootnote)para.Owner;
				string sMarker = footnote.MarkerAsString;
				if (sMarker != null)
					offsetToStartOfParaContents += sMarker.Length + OneSpaceString.Length;
				offsetToStartOfParaContents += footnote.RefAsString.Length;
			}

			// Add the needed properties to each run, within our range
			ITsString tss = para.Contents;
			TsRunInfo runInfo;
			int ichOverrideLim;
			int ichOverrideMin = Math.Min(Math.Max(paraMinHighlight, 0), tss.Length);
			int prevLim = 0;
			do
			{
				tss.FetchRunInfoAt(ichOverrideMin, out runInfo);
				ichOverrideLim = Math.Min(paraLimHighlight, runInfo.ichLim);
				Debug.Assert(ichOverrideLim <= tss.Length, "If we get a run it should be in the bounds of the paragraph");

				// Prevent infinite loop in case of bad data in difference
				if (ichOverrideLim == prevLim)
					break;
				prevLim = ichOverrideLim;
				DispPropOverride prop = DispPropOverrideFactory.Create(
					ichOverrideMin + offsetToStartOfParaContents, ichOverrideLim + offsetToStartOfParaContents);
				initializer(ref prop);
				m_DispPropOverrides.Add(prop);
				// advance Min for next run
				ichOverrideMin = ichOverrideLim;
			}
			while (ichOverrideLim < paraLimHighlight);
		}

		// Simiulate inserting and deleting the paragraph to get it redisplayed.
		private static void UpdateDisplayOfPara(IStTxtPara para, IVwRootBox sourceRootBox)
		{
			if (sourceRootBox != null)
				sourceRootBox.PropChanged(para.Owner.Hvo, para.OwningFlid, para.IndexInOwner, 1, 1);
		}
		#endregion

		#region Private helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Displays the footnote reference.
		/// </summary>
		/// <param name="vwenv">The view environment.</param>
		/// <param name="hvoFootnote">The HVO of the footnote.</param>
		/// ------------------------------------------------------------------------------------
		internal void DisplayFootnoteReference(IVwEnv vwenv, int hvoFootnote)
		{
			IScrFootnote footnote = m_cache.ServiceLocator.GetInstance<IScrFootnoteRepository>().GetObject(hvoFootnote);
			ITsString tssRef = TsStringUtils.MakeTss(footnote.GetReference(m_wsDefault),
				m_wsDefault, ScrStyleNames.FootnoteTargetRef);
			vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
				(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
			vwenv.AddString(tssRef);
		}
		#endregion
	}
	#endregion

	#region UndoSuppressBtPrompt class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Reverses the prompt-suppression when undoing typing the first character in a segmented BT.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class UndoSuppressBtPrompt : IUndoAction
	{
		private readonly ISegment m_seg;
		private readonly TeStVc m_vc;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// make one.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public UndoSuppressBtPrompt(TeStVc vc, ISegment segment)
		{
			m_vc = vc;
			m_seg = segment;
		}

		#region IUndoAction Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// nothing to do
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void Commit()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// no, it doesn't.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDataChange
		{
			get { return false; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Yes, you can.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsRedoable
		{
			get { return true; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Start suppressing it again.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Redo()
		{
			m_vc.HvoOfSegmentWhoseBtPromptIsToBeSupressed = m_seg.Hvo;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Doesn't do any; nothing to do.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool SuppressNotification
		{
			set { }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Return to normal state, suppressing nothing.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool Undo()
		{
			m_vc.HvoOfSegmentWhoseBtPromptIsToBeSupressed = 0;
			return true;
		}

		#endregion
	}
	#endregion
}
