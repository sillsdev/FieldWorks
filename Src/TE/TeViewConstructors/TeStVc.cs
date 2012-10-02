// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TeStVc.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Diagnostics;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.IText;

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
		kfrBtTranslationStatus
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
		private StTxtPara m_overridePara;
		/// <summary>
		/// The initilizer defines the properties that will be applied to the relevant run of the override paragraph.
		/// </summary>
		private DispPropInitializer m_overrideInitializer;
		/// <summary>
		/// The range of characters that should have the override property.
		/// </summary>
		private int m_ichMinOverride;
		private int m_ichLimOverride;

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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="TeStVc"/> class.
		/// </summary>
		/// <param name="target">The LayoutViewTarget</param>
		/// <param name="filterInstance">Number used to make filters unique for each main
		/// window</param>
		/// ------------------------------------------------------------------------------------
		public TeStVc(LayoutViewTarget target, int filterInstance) :
			base()
		{
			m_target = target;
			m_filterInstance = filterInstance;
			m_updatedPrompts = new Set<int>();
		}

		/// <summary>
		/// Override to ensure flex virtuals (if we previously set the cache and the type requires them).
		/// </summary>
		public override ContentTypes ContentType
		{
			get
			{
				return base.ContentType;
			}
			set
			{
				base.ContentType = value;
				if (Cache != null && ContentType == ContentTypes.kctSegmentBT)
					FdoUi.LexEntryUi.EnsureFlexVirtuals(Cache, null);
			}
		}

		/// <summary>
		/// Override to ensure flex virtuals (if the content type requires them).
		/// </summary>
		public override FdoCache Cache
		{
			get
			{
				return base.Cache;
			}
			set
			{
				base.Cache = value;
				if (Cache != null && ContentType == ContentTypes.kctSegmentBT)
					FdoUi.LexEntryUi.EnsureFlexVirtuals(Cache, null);
			}
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
				if (m_updatedPrompts != null)
					m_updatedPrompts.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_updatedPrompts = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

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
				CheckDisposed();

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
					IVwVirtualHandler vh = FilteredScrBooks.GetFilterInstance(Cache, m_filterInstance);
					if (vh != null)
						m_booksTag = vh.Tag;
					else
						m_booksTag = (int)Scripture.ScriptureTags.kflidScriptureBooks;
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
		/// <param name="hvoPara">the StTxtPara for which we want a paragraph</param>
		/// ------------------------------------------------------------------------------------
		protected override void OpenPara(IVwEnv vwenv, int hvoPara)
		{
			if (m_overridePara != null && hvoPara == m_overridePara.Hvo)
			{
				MakeDispPropOverrides(m_overridePara, m_ichMinOverride, m_ichLimOverride, m_overrideInitializer);
				vwenv.OpenOverridePara(m_DispPropOverrides.Count, m_DispPropOverrides.ToArray());
			}
			else
				base.OpenPara(vwenv, hvoPara);
		}

		/// <summary>
		/// Set up the specified overrides (as controlled by the initializer) for the specified
		/// range of characters in the specified paragraph. Return true if anything changed.
		/// </summary>
		public bool SetupOverrides(StTxtPara para, int ichMin, int ichLim, DispPropInitializer initializer, IVwRootBox sourceRootBox)
		{
			bool changed = false;
			if (para == null)
			{
				if (m_overridePara != null)
					changed = true;
			}
			else
			{
				if (m_overridePara == null)
					changed = true;
				else
					changed = para.Hvo != m_overridePara.Hvo;
			}
			if (ichMin != m_ichMinOverride)
				changed = true;
			if (ichLim != m_ichLimOverride)
				changed = true;
			// Enhance JohnT: if more than one thing uses this at the same time, it may become necessary to
			// check whether we're setting up the same initializer. I don't know how to do this. Might need
			// another argument and variable to keep track of a type of override.
			if (!changed)
				return false;

			if (m_overridePara != null && m_overridePara.IsValidObject())
			{
				// remove the old override.
				StTxtPara oldPara = m_overridePara;
				m_overridePara = null; // remove it so the redraw won't show highlighting!
				UpdateDisplayOfPara(oldPara, sourceRootBox);
			}
			m_overridePara = para;
			m_ichMinOverride = ichMin;
			m_ichLimOverride = ichLim;
			m_overrideInitializer = initializer;
			if (m_overridePara!= null && m_overridePara.IsValidObject())
			{
				// show the new override.
				UpdateDisplayOfPara(m_overridePara, sourceRootBox);
			}
			return true;
		}

		// Simiulate inserting and deleting the paragraph to get it redisplayed.
		private void UpdateDisplayOfPara(StTxtPara para, IVwRootBox sourceRootBox)
		{
			if (sourceRootBox == null)
				para.Cache.PropChanged(para.OwnerHVO, para.OwningFlid, para.IndexInOwner, 1, 1);
			else
				para.Cache.PropChanged(sourceRootBox, PropChangeType.kpctNotifyAllButMe, para.OwnerHVO, para.OwningFlid, para.IndexInOwner, 1, 1);
		}

		/// <summary>
		/// A delegate which takes (typically modifies) a DispPropOverride.
		/// </summary>
		/// <param name="prop"></param>
		public delegate void DispPropInitializer(ref DispPropOverride prop);

		/// <summary>
		/// Make the DispPropOverrides we need to give the specified run of characters the display
		/// properties defined by the initializer.
		/// </summary>
		protected void MakeDispPropOverrides(StTxtPara para, int paraMinHighlight, int paraLimHighlight,
			DispPropInitializer initializer)
		{
			if (paraLimHighlight < paraMinHighlight)
				throw new ArgumentOutOfRangeException("paraLimHighlight", "ParaLimHighlight must be greater or equal to paraMinHighlight");

			m_DispPropOverrides.Clear();
			// Add the needed properties to each run, within our range
			ITsString tss = para.Contents.UnderlyingTsString;
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
				DispPropOverride prop = MakeDispPropOverride(ichOverrideMin, ichOverrideLim);
				initializer(ref prop);
				m_DispPropOverrides.Add(prop);
				// advance Min for next run
				ichOverrideMin = ichOverrideLim;
			}
			while (ichOverrideLim < paraLimHighlight);
		}
		/// <summary>
		///  Make a (default) DispPropOverride that does nothing for the specified range of characters.
		/// </summary>
		/// <returns></returns>
		private DispPropOverride MakeDispPropOverride(int ichOverrideMin, int ichOverrideLim)
		{
			const uint knNinch = 0x80000000;
			DispPropOverride prop = new DispPropOverride();
			prop.chrp.clrBack = knNinch;

			prop.chrp.clrFore = knNinch;
			prop.chrp.clrUnder = knNinch;
			prop.chrp.dympOffset = -1;
			prop.chrp.ssv = -1;
			prop.chrp.unt = -1;
			prop.chrp.ttvBold = -1;
			prop.chrp.ttvItalic = -1;
			prop.chrp.dympHeight = -1;
			prop.chrp.szFaceName = null;
			prop.chrp.szFontVar = null;
			prop.ichMin = ichOverrideMin;
			prop.ichLim = ichOverrideLim;
			return prop;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the string that should be displayed in place of an object character associated
		/// with the specified GUID.
		/// </summary>
		/// <param name="bstrGuid"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public override ITsString GetStrForGuid(string bstrGuid)
		{
			CheckDisposed();

			Debug.Assert(Cache != null);
			Debug.Assert(bstrGuid.Length == 8);

			Guid guid = MiscUtils.GetGuidFromObjData(bstrGuid);

			int hvoObj = Cache.GetIdFromGuid(guid);

			if (hvoObj != 0)
			{
				if (Cache.GetClassOfObject(hvoObj) == StFootnote.kclsidStFootnote)
				{
					ScrFootnote footnote = new ScrFootnote(Cache, hvoObj);
					ITsStrBldr bldr = footnote.MakeFootnoteMarker(DefaultWs);
					if (bldr.Length == 0 && Options.ShowMarkerlessIconsSetting)
					{
						// Use the restore window icon character from Marlett as the
						// footnote marker.
						ITsPropsBldr propsBldr = bldr.get_Properties(0).GetBldr();
						propsBldr.SetStrPropValue((int) FwTextPropType.ktptFontFamily,
							"Marlett");
						if (PrintLayout) // don't display markerless icon in print layout view
							bldr.Replace(0, 0, string.Empty, propsBldr.GetTextProps());
						else
							bldr.Replace(0, 0, "\u0032", propsBldr.GetTextProps());
					}
					else if (bldr.Length > 0)
					{
						// This is to prevent the word-wrap from wrapping at the beginning of
						// a footnote marker.
						// REVIEW (TimS): \uFEFF is deprecated as a non-break character in Unicode now. Should
						//                use \u2060 but no fonts/renderers seem to have it defined. Should
						//                maybe change this eventually.
						string directionIndicator = (RightToLeft) ? "\u200F" : "\u200E";
						bldr.Replace(0, 0, directionIndicator + "\uFEFF" + directionIndicator, null);
					}

					bldr.SetIntPropValues(0, bldr.Length,
						(int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvEnum,
						(int)TptEditable.ktptNotEditable);

					return bldr.GetString();
				}
			}

			// If the GUID was not found then return a missing object string
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			return tsf.MakeString("<missing object>", Cache.DefaultUserWs);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Insert end of paragraph marks if needed.
		/// </summary>
		/// <param name="vwenv"></param>
		/// <param name="hvoPara"></param>
		/// ------------------------------------------------------------------------------------
		protected override void InsertEndOfParaMarks(IVwEnv vwenv, int hvoPara)
		{
			if (Options.ShowFormatMarksSetting && m_target == LayoutViewTarget.targetDraft)
			{
				// Set up for an end mark.

				// If this is the last paragraph of a section then insert an
				// end of section mark, otherwise insert a paragraph mark.
				VwBoundaryMark boundaryMark;
				StTxtPara para = new StTxtPara(m_cache, hvoPara);
				StText text = new StText(m_cache, para.OwnerHVO);
				int[] paraArray = text.ParagraphsOS.HvoArray;
				if (hvoPara == paraArray[paraArray.Length - 1])
					boundaryMark = VwBoundaryMark.endOfSection; // "§"
				else
					boundaryMark = VwBoundaryMark.endOfParagraph; // "¶"
				vwenv.SetParagraphMark(boundaryMark);
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
		/// <param name="hvo">id of paragraph to be displayed</param>
		/// <returns>true if an empty string was substituted for missing/empty StText</returns>
		/// -----------------------------------------------------------------------------------
		protected override bool InsertParaContentsUserPrompt(IVwEnv vwenv, int hvo)
		{
			Debug.Assert((Cache == null && vwenv.DataAccess != null) ||
				Cache.MainCacheAccessor == vwenv.DataAccess,
				"Oops! We don't expect to get a different data access object from IVwEnv");
			Debug.Assert(!DisplayTranslation);

			// No user prompt in any of these conditions
			if (hvo == 0
				|| Cache.GetClassOfObject(hvo) != (int)StTxtPara.kClassId
				|| !Options.ShowEmptyParagraphPromptsSetting // tools options setting
				|| m_target == LayoutViewTarget.targetPrint // any print layout view
				|| m_updatedPrompts.Contains(hvo)) // user interaction has updated prompt
			{
				return false;
			}

			// User prompt is only for title & heading paras
			StText text = new StText(Cache, Cache.GetOwnerOfObject(hvo)); // para owner
			if (text.OwningFlid != (int)ScrBook.ScrBookTags.kflidTitle &&
				text.OwningFlid != (int)ScrSection.ScrSectionTags.kflidHeading)
				return false;

			int paraCount = text.ParagraphsOS.Count;
			Debug.Assert(paraCount != 0,
				"We shouldn't come here if StText doesn't contain any paragraphs");
			// By design, if there is more than one para, don't display the user prompt.
			if (paraCount != 1)
				return false;

			// If first para is empty, insert user prompt for paragraph content
			if (((StTxtPara)text.ParagraphsOS.FirstItem).Contents.Text == null)
			{
				vwenv.NoteDependency(new int[] { hvo },
					new int[] { (int)StTxtPara.StTxtParaTags.kflidContents}, 1);
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
		/// <param name="hvo">id of translation to be displayed</param>
		/// <returns>true if an empty string was substituted for missing/empty StText</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool InsertTranslationUserPrompt(IVwEnv vwenv, int hvo)
		{
			// No user prompt in any of these conditions
			if (hvo == 0
				|| Cache.GetClassOfObject(hvo) != (int)CmTranslation.kClassId
				|| m_updatedPrompts.Contains(hvo)) // user interaction has updated prompt
			{
				return false;
			}

			// If there is text in the translation then do not add a prompt.
			CmTranslation trans = new CmTranslation(Cache, hvo);
			if (trans.Translation.GetAlternative(m_wsDefault).Text != null)
				return false;

			// If there is no text in the parent paragraph then do not place a prompt in the
			// back translation.
			StTxtPara parentPara = new StTxtPara(Cache, trans.OwnerHVO);
			if (parentPara.Contents.Text == null)
				return false;

			// Insert the prompt.
			vwenv.NoteDependency(new int[] { hvo },
				new int[] { (int)CmTranslation.CmTranslationTags.kflidTranslation}, 1);
			vwenv.AddProp(SimpleRootSite.kTagUserPrompt, this,
				(int)CmTranslation.CmTranslationTags.kflidTranslation);
			return true;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load data needed to display the specified objects using the specified fragment.
		/// This is called before attempting to Display an item that has been listed for lazy
		/// display using AddLazyItems. It may be used to load the necessary data into the
		/// DataAccess object.
		/// </summary>
		/// <param name="vwenv">view environment in the state appropriate for the subsequent call to
		/// Display for the first item</param>
		/// <param name="rghvo">the items we want to display</param>
		/// <param name="chvo">number of items we want to display</param>
		/// <param name="hvoParent">HVO of the parent</param>
		/// <param name="tag">the tag we are going to display</param>
		/// <param name="frag">the fragment argument that will be passed to Display to show
		/// each of them</param>
		/// <param name="ihvoMin">the index of the first item in prghvo, in the overall
		/// property. Ignored in this implementation.</param>
		/// ------------------------------------------------------------------------------------
		public override void LoadDataFor(IVwEnv vwenv, int[] rghvo, int chvo, int hvoParent, int tag,
			int frag, int ihvoMin)
		{
			CheckDisposed();

			if (tag == (int) ScrBook.ScrBookTags.kflidSections)
			{
				LoadSegmentInfoForSection(rghvo);
			}
			else if (HasBooks && tag == BooksTag)
			{
				LoadSegmentInfoForBooks(rghvo);
			}
		}
		private void LoadSegmentInfoForBooks(int[] rghvo)
		{
			if (ContentType != ContentTypes.kctSegmentBT)
				return;
			foreach (int hvoBook in rghvo)
			{
				ScrBook book = (ScrBook)ScrBook.CreateFromDBObject(Cache, hvoBook);
				LoadDataForStText(book.TitleOA);
			}
		}

		/// <summary>
		/// Loading the sections: need to parse the paragraphs to ensure various annotations exist
		/// and are loaded.
		/// </summary>
		/// <param name="rghvo"></param>
		protected void LoadSegmentInfoForSection(int[] rghvo)
		{
			if (ContentType != ContentTypes.kctSegmentBT)
				return;
			foreach (int hvoSection in rghvo)
			{
				ScrSection section = (ScrSection)ScrSection.CreateFromDBObject(Cache, hvoSection);
				LoadDataForStText(section.HeadingOA);
				LoadDataForStText(section.ContentOA);
			}
		}

		internal void LoadDataForStText(IStText text)
		{
			bool fDidParse;
			bool fSupressSubTasks = (m_cache.ActionHandlerAccessor == null ||
				m_cache.ActionHandlerAccessor.CurrentDepth == 0);
			ParagraphParser.ParseText(text, fSupressSubTasks, new NullProgressState(), out fDidParse);
			using (fSupressSubTasks ? new SuppressSubTasks(m_cache) : null)
			{
				StTxtPara.LoadSegmentFreeTranslations(text.ParagraphsOS.HvoArray, Cache, BackTranslationWS);
				foreach (StTxtPara para in text.ParagraphsOS)
				{
					if (para.HasNoSegmentBt(BackTranslationWS))
						ConvertOldBtToNew(para);
				}
			}
		}

		private void ConvertOldBtToNew(IStTxtPara para)
		{
			new BtConverter(para).ConvertCmTransToInterlin(BackTranslationWS);
		}

		private bool ParaHasNoSegmentBt(IStTxtPara para)
		{
			int kflidSegments = StTxtPara.SegmentsFlid(para.Cache);
			int kflidFt = StTxtPara.SegmentFreeTranslationFlid(Cache);
			ISilDataAccess sda = para.Cache.MainCacheAccessor;
			int cseg = sda.get_VecSize(para.Hvo, kflidSegments);
			for (int iseg = 0; iseg < cseg; iseg++)
			{
				int hvoSeg = sda.get_VecItem(para.Hvo, kflidSegments, iseg);
				int hvoFt = sda.get_ObjectProp(hvoSeg, kflidFt);
				if (sda.get_MultiStringAlt(hvoFt, (int)CmAnnotation.CmAnnotationTags.kflidComment, BackTranslationWS).Length != 0)
					return false;
			}
			return true;
		}

		/// <summary>
		/// We override this to check that segments are properly loaded. Normally this is done by LoadDataForStText
		/// when the containing section (the smallest lazy element) is loaded, but that doesn't run when we insert a paragraph.
		/// </summary>
		protected override void InsertBtSegments(StVc vc, IVwEnv vwenv, int hvo)
		{
			int kflidSegments = StTxtPara.SegmentsFlid(Cache);
			if (!Cache.MainCacheAccessor.get_IsPropInCache(hvo, kflidSegments, (int)CellarModuleDefns.kcptReferenceSequence, 0))
			{
				StTxtPara para = CmObject.CreateFromDBObject(Cache, hvo) as StTxtPara;
				ParagraphParserOptions options = new ParagraphParserOptions();
				options.CreateRealSegments = true;
				ParagraphParser.ParseParagraph(para, options);
				StTxtPara.LoadSegmentFreeTranslations(new int[] {hvo}, Cache, BackTranslationWS);
				// This is probably redundant, AFAIK this can only happen for newly created paragraphs,
				// but it's safe and (for the same reason) rare.
				if (para.HasNoSegmentBt(BackTranslationWS))
					ConvertOldBtToNew(para);
			}
			base.InsertBtSegments(vc, vwenv, hvo);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Add a user prompt for an empty paragraph.
		/// </summary>
		/// <param name="vwenv">View environment</param>
		/// <param name="tag">Tag</param>
		/// <param name="v">usually <c>null</c></param>
		/// <param name="frag">Fragment to identify what type of prompt should be created.</param>
		/// <returns>ITsString that will be displayed as user prompt.</returns>
		/// ------------------------------------------------------------------------------------
		public override ITsString DisplayVariant(IVwEnv vwenv, int tag, object v, int frag)
		{
			CheckDisposed();

			string userPrompt = null;
			Color promptColor = Color.LightGray;
			int promptWs = Cache.DefaultUserWs;
			switch (frag)
			{
				case (int) ScrBook.ScrBookTags.kflidTitle:
					int hvoText = Cache.GetOwnerOfObject(vwenv.CurrentObject());
					ScrBook book = new ScrBook(Cache, Cache.GetOwnerOfObject(hvoText));
					userPrompt = string.Format(TeResourceHelper.GetResourceString("kstidBookTitleUserPrompt"),
											   book.Name.UserDefaultWritingSystem);
					break;
				case (int) ScrSection.ScrSectionTags.kflidHeading:
					userPrompt = TeResourceHelper.GetResourceString("kstidSectionHeadUserPrompt");
					break;
				case (int) CmTranslation.CmTranslationTags.kflidTranslation:
					if (!PrintLayout && Editable)
						userPrompt = TeResourceHelper.GetResourceString("kstidBackTranslationUserPrompt");
					else
						userPrompt = TeResourceHelper.GetResourceString("kstidBackTranslationUserPromptPrintLayout");
					break;
				case (int) CmAnnotation.CmAnnotationTags.kflidComment:
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
			ttpBldr.SetIntPropValues((int) FwTextPropType.ktptBackColor,
									 (int) FwTextPropVar.ktpvDefault, (int) ColorUtil.ConvertColorToBGR(promptColor));
			ttpBldr.SetIntPropValues((int) FwTextPropType.ktptWs,
									 (int) FwTextPropVar.ktpvDefault, promptWs);

			// The use of the user-defined property here is to indicate that this is a
			// user prompt string.
			ttpBldr.SetIntPropValues(SimpleRootSite.ktptUserPrompt,
									 (int) FwTextPropVar.ktpvDefault, 1);
			ttpBldr.SetIntPropValues((int) FwTextPropType.ktptSpellCheck,
									 (int) FwTextPropVar.ktpvEnum, (int) SpellingModes.ksmDoNotCheck);

			ITsStrBldr bldr = TsStrBldrClass.Create();
			bldr.Replace(0, 0, userPrompt, ttpBldr.GetTextProps());
			// Begin the prompt with a zero-width space in the view's default writing system.
			// 200B == zero-width space.
			// This helps select the correct keyboard when the user selects the user prompt and
			// begins typing. Also prevent the DoNotSpellCheck and back color being applied
			// to what is typed.
			ITsPropsBldr ttpBldr2 = TsPropsBldrClass.Create();
			ttpBldr2.SetIntPropValues((int) FwTextPropType.ktptWs,
									  (int) FwTextPropVar.ktpvDefault, m_wsDefault);
			ttpBldr2.SetIntPropValues(SimpleRootSite.ktptUserPrompt,
									  (int) FwTextPropVar.ktpvDefault, 1);
			bldr.Replace(0, 0, "\u200B", ttpBldr2.GetTextProps());
			return bldr.GetString();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Replace the user prompt with the text the user typed.  This method is called from
		/// the views code when the user prompt is edited.
		/// </summary>
		/// <param name="vwsel">Current selection in rootbox where this prop was updated</param>
		/// <param name="hvo">Hvo of the paragraph</param>
		/// <param name="tag">Tag</param>
		/// <param name="frag">Fragment</param>
		/// <param name="tssVal">Text the user just typed</param>
		/// <returns>possibly modified ITsString.</returns>
		/// <remarks>The return value is currently ignored in production code, but we use it
		/// in our tests.</remarks>
		/// -----------------------------------------------------------------------------------
		public override ITsString UpdateProp(IVwSelection vwsel, int hvo, int tag, int frag,
			ITsString tssVal)
		{
			CheckDisposed();

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
			ITsStrBldr bldr = (ITsStrBldr)tssVal.GetBldr();
			if (frag != (int)CmAnnotation.CmAnnotationTags.kflidComment)
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
			int tagTextProp;
			int cpropPrevious;
			int ichAnchor;
			int ichEnd;
			int ihvoEnd;
			bool fAssocPrev;
			int ws;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwsel, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);
			// Prior to the Commit in selection changed which causes this UpdateProp to be called,
			// earlier selection changed code has expanded the selection (because it is in a user prompt)
			// to the whole prompt. It is therefore a range selection, and the value of fAssocPrev we got
			// is useless. We want to make a selection associated with the previous character at the END of
			// the range.

			if (frag == (int)CmAnnotation.CmAnnotationTags.kflidComment)
			{
				// If the length is zero...we need to suppress replacing the comment with a prompt.
				if (tssVal.Length == 0)
					m_hvoSuppressCommentPrompt = hvo;
				CmIndirectAnnotation ann = new CmIndirectAnnotation(Cache, hvo);
				if (ann.Comment.GetAlternative(BackTranslationWS).Length == 0)
				{
					// Undo needs to unset suppressing the comment prompt.
					Cache.ActionHandlerAccessor.AddAction(new UndoSuppressCommentPrompt(this, ann));
				}
				// Turn the prompt property off for future typing, too.
				ITsPropsBldr pb = ttp.GetBldr();
				pb.SetIntPropValues(SimpleRootSite.ktptUserPrompt, -1, -1);
				ann.Comment.SetAlternative(tssVal, BackTranslationWS);
				rootbox.MakeTextSelection(ihvoRoot, cvsli, rgvsli,
					(int)CmAnnotation.CmAnnotationTags.kflidComment, cpropPrevious, ichEnd, ichEnd,
					BackTranslationWS, fAssocPrev, ihvoEnd, pb.GetTextProps(), true);
				return tssVal;
			}

			ReplacePromptUndoAction undoAction =
				new ReplacePromptUndoAction(hvo, m_cache, m_updatedPrompts);

			if (m_cache.ActionHandlerAccessor != null)
				m_cache.ActionHandlerAccessor.AddAction(undoAction);

			// Mark the user prompt as having been updated - will not show prompt again.
			// Note: ReplacePromptUndoAction:Undo removes items from the Set.
			m_updatedPrompts.Add(hvo);

			// Replace the ITsString in the paragraph or translation
			// - this destroys the selection because we replace the user prompt.
			StTxtPara para;
			CmTranslation trans;
			ITsTextProps props = StyleUtils.CharStyleTextProps(null, m_wsDefault);
			if (frag == (int)CmTranslation.CmTranslationTags.kflidTranslation)
			{
				trans = new CmTranslation(Cache, hvo);
				trans.Translation.GetAlternative(m_wsDefault).UnderlyingTsString = tssVal;
				undoAction.ParaHvo = trans.OwnerHVO;

				// now set the selection to the end of the text that was just put in.
				ichAnchor = ichEnd;
				rootbox.MakeTextSelection(ihvoRoot, cvsli, rgvsli,
					(int)CmTranslation.CmTranslationTags.kflidTranslation, cpropPrevious, ichAnchor, ichEnd,
					m_wsDefault, true, ihvoEnd, props, true);
			}
			else
			{
				para = new StTxtPara(Cache, hvo);
				para.Contents.UnderlyingTsString = tssVal;
				undoAction.ParaHvo = hvo;

				// now set the selection to the end of the text that was just put in.
				ichAnchor = ichEnd;
				rootbox.MakeTextSelection(ihvoRoot, cvsli, rgvsli,
					(int)StTxtPara.StTxtParaTags.kflidContents, cpropPrevious, ichAnchor, ichEnd,
					m_wsDefault, true, ihvoEnd, props, true);
			}

			return tssVal;
		}

		/// <summary>
		/// Given that segment is a 'label' segment (typically an embedded identifying number),
		/// figure out what to display as the corresponding label in a back translation view.
		/// This is made separate so that TeStVc can override to use a different numbering scheme
		/// in the BT.
		/// </summary>
		/// <param name="segment"></param>
		/// <returns></returns>
		protected override ITsString GetBackTransLabelText(CmBaseAnnotation segment)
		{
			ITsString tssLabel = segment.TextAnnotated;
			if (tssLabel == null)
				return null;
			IScripture scr = segment.Cache.LangProject.TranslatedScriptureOA;
			return scr.ConvertCVNumbersInStringForBT(tssLabel, BackTranslationWS);
		}

		/// <summary>
		/// The HVO of for which we want to suppress displaying a prompt for annotation comments.
		/// </summary>
		public int SuppressCommentPromptHvo
		{
			get { return m_hvoSuppressCommentPrompt; }
			set { m_hvoSuppressCommentPrompt = value;}
		}

		private int m_hvoSuppressCommentPrompt;
		/// <summary>
		/// Suppress displaying a user prompt (typically because the user typed backspace or delete at the prompt).
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <returns></returns>
		protected override bool SuppressPrompt(int hvo, int tag)
		{
			return hvo == m_hvoSuppressCommentPrompt && tag == (int)CmAnnotation.CmAnnotationTags.kflidComment;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clears the flags that are set when user prompts are updated. Clearing the flags
		/// will allow the user prompts to be shown again.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ClearUserPromptUpdates()
		{
			CheckDisposed();

			m_updatedPrompts.Clear();
		}
		#endregion
	}
	#endregion
	/// <summary>
	/// Reverses the prompt-suppression when undoing typing the first character in a segmented BT.
	/// </summary>
	public class UndoSuppressCommentPrompt : IUndoAction
	{
		private CmIndirectAnnotation m_ft;
		private TeStVc m_vc;

		/// <summary>
		/// make one.
		/// </summary>
		public UndoSuppressCommentPrompt(TeStVc vc, CmIndirectAnnotation ft)
		{
			m_vc = vc;
			m_ft = ft;
		}
		#region IUndoAction Members

		/// <summary>
		/// nothing to do
		/// </summary>
		public void Commit()
		{
		}

		/// <summary>
		/// no, it doesn't.
		/// </summary>
		public bool IsDataChange()
		{
			return false;
		}

		/// <summary>
		/// Yes, you can.
		/// </summary>
		public bool IsRedoable()
		{
			return true;
		}

		/// <summary>
		/// Start suppressing it again.
		/// </summary>
		public bool Redo(bool fRefreshPending)
		{
			m_vc.SuppressCommentPromptHvo = m_ft.Hvo;
			ForceUpdate();
			return true;
		}

		/// <summary>
		/// No, it doesn't
		/// </summary>
		public bool RequiresRefresh()
		{
			return false;
		}

		/// <summary>
		/// Doesn't do any; nothing to do.
		/// </summary>
		public bool SuppressNotification
		{
			set { }
		}

		/// <summary>
		/// Return to normal state, suppressing nothing.
		/// </summary>
		public bool Undo(bool fRefreshPending)
		{
			m_vc.SuppressCommentPromptHvo = 0;
			ForceUpdate();
			return true;
		}

		#endregion

		void ForceUpdate()
		{
			CmBaseAnnotation seg = m_ft.AppliesToRS[0] as CmBaseAnnotation;
			if (seg == null)
				return; // paranoia
			StTxtPara para = seg.BeginObjectRA as StTxtPara;
			if (para == null)
				return; // paranoia
			int kflidSegments = StTxtPara.SegmentsFlid(para.Cache);
			int ihvo = para.Cache.GetObjIndex(para.Hvo, kflidSegments, seg.Hvo);
			if (ihvo == -1)
				return; // paranoia
			para.Cache.PropChanged(para.Hvo, kflidSegments, ihvo, 1, 1);
		}
	}
}
