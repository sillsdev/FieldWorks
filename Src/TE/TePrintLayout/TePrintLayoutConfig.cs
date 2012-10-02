// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: TePrintLayoutConfig.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TePrintLayoutConfig : IPrintLayoutConfigurer, IHeightEstimator, IFilter, ISortSpec
	{
		#region Data members
		/// <summary></summary>
		protected FdoCache m_fdoCache;
		/// <summary></summary>
		protected IScripture m_scr;
		/// <summary></summary>
		protected IVwStylesheet m_styleSheet;
		/// <summary></summary>
		protected TeViewType m_viewType;
		/// <summary></summary>
		protected IPublication m_pub;
		/// <summary></summary>
		protected int m_bookFilterInstance;
		/// <summary></summary>
		protected DateTime m_printDateTime;
		/// <summary>Wether or not the division contains intro sections.</summary>
		protected bool m_fIntroDivision;
		private IParagraphCounter m_paraCounter;
		/// <summary>The instance of the section filter</summary>
		protected int m_sectionFilterInstance;
		/// <summary>The section filter</summary>
		protected FilteredSequenceHandler m_sectionFilter;
		/// <summary>The HVO of the book we're displaying.</summary>
		private int m_hvoBook;
		/// <summary>
		/// A layout stream used for footnotes which is shared across multiple divisions
		/// </summary>
		protected IVwLayoutStream m_sharedStream;
		/// <summary>The WS to use (usually for back translation)</summary>
		protected int m_ws;

		private static int g_nextSectionFilterInstance = 0;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a TePrintLayoutConfig to configure the main print layout
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="publication">The publication.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="filterInstance">the book filter instance in effect</param>
		/// <param name="printDateTime">printing date and time</param>
		/// <param name="fIntroDivision">set to <c>true</c> for a division that displays book
		/// title and introduction material, <c>false</c> for a division that displays main
		/// scripture text.</param>
		/// <param name="hvoBook">The hvo of the book we're displaying.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="ws">The writing system to use for the view.</param>
		/// ------------------------------------------------------------------------------------
		public TePrintLayoutConfig(FdoCache cache, IVwStylesheet styleSheet,
			IPublication publication, TeViewType viewType, int filterInstance,
			DateTime printDateTime, bool fIntroDivision, int hvoBook, IVwLayoutStream sharedStream,
			int ws)
		{
			m_fdoCache = cache;
			m_scr = m_fdoCache.LangProject.TranslatedScriptureOA;
			m_styleSheet = styleSheet;
			m_pub = publication;
			m_viewType = viewType;
			m_bookFilterInstance = filterInstance;
			m_printDateTime = printDateTime;
			m_fIntroDivision = fIntroDivision;
			m_hvoBook = hvoBook;
			m_sharedStream = sharedStream;
			m_ws = ws;

			m_sectionFilterInstance = g_nextSectionFilterInstance++;
			m_sectionFilter = new FilteredSequenceHandler(cache, ScrBook.kClassId,
				m_sectionFilterInstance, this, this,
				new SimpleFlidProvider((int)ScrBook.ScrBookTags.kflidSections));

			m_paraCounter = ParagraphCounterManager.GetParaCounter(cache, (int)TeViewGroup.Scripture);
		}

		#region Implementation of IPrintLayoutConfigurer
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates and returns the primary view construtor for the main view in the layout.
		/// This is only called once.
		/// </summary>
		/// <param name="div"></param>
		/// <returns>The view constructor to be used for the main view</returns>
		/// ------------------------------------------------------------------------------------
		public virtual IVwViewConstructor MakeMainVc(DivisionLayoutMgr div)
		{
			DraftViewVc vc;
			if (m_fIntroDivision)
			{
				vc = new ScriptureBookIntroVc(TeStVc.LayoutViewTarget.targetPrint,
					div.FilterInstance, m_styleSheet, false, m_sectionFilter.Tag);
			}
			else
			{
				vc = new ScriptureBodyVc(TeStVc.LayoutViewTarget.targetPrint,
					div.FilterInstance, m_styleSheet, false, m_sectionFilter.Tag);
			}
			vc.HeightEstimator = this;
			if ((m_viewType & TeViewType.BackTranslation) != 0)
			{
				vc.ContentType = Options.UseInterlinearBackTranslation ? StVc.ContentTypes.kctSegmentBT : StVc.ContentTypes.kctSimpleBT;
				vc.DefaultWs = m_ws;
			}
			vc.Cache = m_fdoCache;
			vc.PrintLayout = true;
			return vc;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes a call to MakeSubordinateView, to add a view containing footnotes.
		/// </summary>
		/// <param name="div">The division layout manager</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ConfigureSubordinateViews(DivisionLayoutMgr div)
		{
			int hvoScripture = m_fdoCache.LangProject.TranslatedScriptureOAHvo;
			int wsDefault = ((m_viewType & TeViewType.BackTranslation) != 0 ?
				m_fdoCache.DefaultAnalWs : m_fdoCache.DefaultVernWs);

			NLevelOwnerSvd ownerSvd = new NLevelOwnerSvd(2, m_fdoCache.MainCacheAccessor,
				hvoScripture);

			IVwVirtualHandler vh =
				FilteredScrBooks.GetFilterInstance(m_fdoCache, div.FilterInstance);

			if (vh != null)
			{
				ownerSvd.AddTagLookup((int)Scripture.ScriptureTags.kflidScriptureBooks,
					vh.Tag);
			}

			if (m_sharedStream == null)
			{
				FootnoteVc footnoteVc = new FootnoteVc(div.FilterInstance,
					TeStVc.LayoutViewTarget.targetPrint, wsDefault);
				footnoteVc.Cache = m_fdoCache;
				footnoteVc.DisplayTranslation = (m_viewType & TeViewType.BackTranslation) != 0;

				div.AddSubordinateStream(hvoScripture, (int)FootnoteFrags.kfrScripture,
					footnoteVc, ownerSvd);
			}
			else
			{
				int hvoRoot;
				IVwViewConstructor vc;
				int frag;
				IVwStylesheet stylesheet;
				((IVwRootBox)m_sharedStream).GetRootObject(out hvoRoot, out vc, out frag, out stylesheet);
				div.AddSharedSubordinateStream(m_sharedStream, vc, ownerSvd);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level fragment for the main view (the one used to display
		/// MainObjectId).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int MainFragment
		{
			//get { return (int)ScrFrags.kfrScripture; }
			get { return (int)ScrFrags.kfrBook; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level object that the main view displays.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int MainObjectId
		{
			//get { return m_fdoCache.LangProject.TranslatedScriptureOAHvo; }
			get { return m_hvoBook; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the stylesheet.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IVwStylesheet StyleSheet
		{
			get { return m_styleSheet; }
		}

		/// -------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the data access object used for all views.
		/// Review JohnT: is this the right way to get this? And, should it be an FdoCache?
		/// I'd prefer not to make PrintLayout absolutely dependent on having an FdoCache,
		/// but usually it will have, and there are things to take advantage of if it does.
		/// </summary>
		/// -------------------------------------------------------------------------------------
		public ISilDataAccess DataAccess
		{
			get { return m_fdoCache.MainCacheAccessor; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the ID of the CmObject in the database that matches this guid.
		/// </summary>
		/// <param name="guid">A GUID that represents an object in the cache.</param>
		/// <returns>The ID corresponding to the GUID (the Id field), or zero if the
		/// GUID is invalid.</returns>
		/// -----------------------------------------------------------------------------------
		public int GetIdFromGuid(Guid guid)
		{
			return m_fdoCache.GetIdFromGuid(guid);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create and return a configurer for headers and footers.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual IHeaderFooterConfigurer HFConfigurer
		{
			get
			{
				PubDivision pubDiv = (PubDivision)m_pub.DivisionsOS[0];
				int hvoHFSet = pubDiv.HFSetOAHvo;
				int wsDefault = ((m_viewType & TeViewType.BackTranslation) != 0 ?
					m_fdoCache.DefaultAnalWs : m_fdoCache.DefaultVernWs);
				return new TeHeaderFooterConfigurer(m_fdoCache, hvoHFSet, wsDefault,
					m_bookFilterInstance, m_printDateTime, m_sectionFilter.Tag);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// If this property is true, ConfigureSubordinateViews will not be called, and there
		/// will be no subordinate streams divided across pages. Instead, a root box will
		/// be created for the dependent objects on each page. If there is more than one
		/// division on each page, the configurer for any of them may be asked to provide the
		/// information needed to initialize it.
		/// In the RootOnEachPage mode, the DLM creates for each page a root box and a
		/// dummy object ID which becomes the root object for the dependent objects on the page.
		/// The configurer provides a view constructor, a fragment ID, and a flid. The DLM
		/// will arrange that the IDs of the dependent objects on the page are made the value
		/// of the specified flid of the dummy root object. The VC will be asked to display
		/// the root object using the root fragment, which should somehow display the
		/// specified properties.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public bool RootOnEachPage
		{
			get
			{
				return (m_viewType & TeViewType.BackTranslation) == 0 &&
					m_viewType != TeViewType.Correction;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the property of the dummy root object
		/// under which the dependent object IDs are stored.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public int DependentRootTag
		{
			get
			{
				return GetDependentRootTag(m_fdoCache);
				// Thought we could get away with this since it's a dummy object, but there's a watcher on it that
				// tries to create the Scripture object...and can't without a real HVO.
				//return (int)ScrBook.ScrBookTags.kflidFootnotes;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the fragment ID that should be used to
		/// display the dummy root object.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public int DependentRootFrag
		{
			get { return GetDependentRootFrag(); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the VC which is used to display things
		/// in the dependent root view.
		/// </summary>
		/// <value></value>
		/// ------------------------------------------------------------------------------------
		public IVwViewConstructor DependentRootVc
		{
			get
			{
				int wsDefault = m_fdoCache.DefaultVernWs; // we never smush back translations.

				// Review JohnT: -1 is very arbitrary, based on assumption that we're not showing the
				// books in a footnote VC and hence won't be using the BooksTag. It will cause an
				// assertion if BooksTag is called.
				// This code is adapted from code elsewhere in this class that makes a FootnoteVc,
				// but which does a good bit of other stuff as well. Some of that may prove
				// needed in a dependent root as well, in which case, we may have to pass
				// a DivisionLayoutMgr to this routine.
				FootnoteVc vc = new FootnoteVc(-1, TeStVc.LayoutViewTarget.targetPrint, wsDefault);
				vc.Cache = m_fdoCache;
				vc.Stylesheet = m_styleSheet;
				return vc;
			}
		}
		#endregion

		#region Implementation of IHeightEstimator
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get average paragraph height (in points)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int AverageParaHeight
		{
			// REVIEW (TimS/EberhardB): 51 seems to work pretty well, but is this true in all
			// cases (different DPI settings/screen resolution...)?
			get { return 51; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Calculate height for books and sections.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="frag"></param>
		/// <param name="dxAvailWidth"></param>
		/// <returns>The estimated height for the specified hvo in points</returns>
		/// ------------------------------------------------------------------------------------
		public int EstimateHeight(int hvo, int frag, int dxAvailWidth)
		{
			int ret = m_paraCounter.GetParagraphCount(hvo, frag) * AverageParaHeight;
			return ret;
		}
		#endregion

		#region IFilter Members

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the filter so it can check for matches. This must be called once before
		/// calling <see cref="M:SIL.FieldWorks.FDO.IFilter.MatchesCriteria(System.Int32)"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		void IFilter.InitCriteria()
		{
			// no-op
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns><c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		bool IFilter.MatchesCriteria(int hvoObj)
		{
			// Back translations don't distinguish between the sections, so return true
			// if hvoObj is a section
			if ((m_viewType & TeViewType.BackTranslation) != 0)
				return (m_fdoCache.GetClassOfObject(hvoObj) == ScrSection.kClassId);

			try
			{
				ScrSection section = new ScrSection(m_fdoCache, hvoObj);
				return section.IsIntro == m_fIntroDivision;

			}
			catch
			{
				// The HVO we got isn't for a section!
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		string IFilter.Name
		{
			get { return "SectionFilter"; }
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the section filter.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal FilteredSequenceHandler SectionFilter
		{
			get { return m_sectionFilter; }
		}
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the property of the dummy root object
		/// under which the dependent object IDs are stored.
		/// </summary>
		/// <param name="cache">FDO cache</param>
		/// <returns>Tag for dependent root object</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetDependentRootTag(FdoCache cache)
		{
			return DummyVirtualHandler.InstallDummyHandler(cache.VwCacheDaAccessor,
				"Scripture", "FootnotesOnPage", (int)CellarModuleDefns.kcptReferenceSequence).Tag;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// When RootOnEachPage is true, this obtains the fragment ID that should be used to
		/// display the dummy root object.
		/// </summary>
		/// <returns>Fragment for dependent root object</returns>
		/// ------------------------------------------------------------------------------------
		public static int GetDependentRootFrag()
		{
			return (int)FootnoteFrags.kfrRootInPageSeq;
		}
		#endregion
	}
}
