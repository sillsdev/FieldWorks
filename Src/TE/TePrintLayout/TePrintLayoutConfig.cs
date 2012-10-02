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
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.PrintLayout; // what about this?
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.UIAdapters;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Used to control which portions of a book are layed out in a division.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public enum PrintLayoutPortion
	{
		/// <summary>
		/// Division will have all content of the book.
		/// </summary>
		AllContent,
		/// <summary>
		/// Division will have only the title and introduction sections.
		/// </summary>
		TitleAndIntro,
		/// <summary>
		/// Division will have only the scripture sections.
		/// </summary>
		ScriptureSections
	} ;

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TePrintLayoutConfig : IPrintLayoutConfigurer, IHeightEstimator
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
		/// <summary>Portion of book to be layed out in this division</summary>
		protected PrintLayoutPortion m_divisionPortion;
		private IParagraphCounter m_paraCounter;
		/// <summary>The HVO of the book we're displaying.</summary>
		private int m_hvoBook;
		/// <summary>
		/// A layout stream used for footnotes which is shared across multiple divisions
		/// </summary>
		protected IVwLayoutStream m_sharedStream;
		/// <summary>The WS to use (usually for back translation)</summary>
		protected int m_ws;

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
		/// <param name="divisionPortion">portion of the book to be layed out in this division</param>
		/// <param name="hvoBook">The hvo of the book we're displaying.</param>
		/// <param name="sharedStream">A layout stream used for footnotes which is shared across
		/// multiple divisions</param>
		/// <param name="ws">The writing system to use for the view.</param>
		/// ------------------------------------------------------------------------------------
		public TePrintLayoutConfig(FdoCache cache, IVwStylesheet styleSheet,
			IPublication publication, TeViewType viewType, int filterInstance,
			DateTime printDateTime, PrintLayoutPortion divisionPortion, int hvoBook, IVwLayoutStream sharedStream,
			int ws)
		{
			m_fdoCache = cache;
			m_scr = m_fdoCache.LangProject.TranslatedScriptureOA;
			m_styleSheet = styleSheet;
			m_pub = publication;
			m_viewType = viewType;
			m_bookFilterInstance = filterInstance;
			m_printDateTime = printDateTime;
			m_divisionPortion = divisionPortion;
			m_hvoBook = hvoBook;
			m_sharedStream = sharedStream;
			m_ws = ws;

			m_paraCounter = cache.ServiceLocator.GetInstance<IParagraphCounterRepository>().GetParaCounter((int)TeViewGroup.Scripture);
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
			if (m_divisionPortion == PrintLayoutPortion.TitleAndIntro)
			{
				vc = new ScriptureBookIntroVc(TeStVc.LayoutViewTarget.targetPrint,
					div.FilterInstance, m_styleSheet, false);
			}
			else if (m_divisionPortion == PrintLayoutPortion.ScriptureSections)
			{
				vc = new ScriptureBodyVc(TeStVc.LayoutViewTarget.targetPrint,
					div.FilterInstance, m_styleSheet, false);
			}
			else
			{
				vc = new DraftViewVc(TeStVc.LayoutViewTarget.targetPrint, div.FilterInstance,
					m_styleSheet, false);
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
			int hvoScripture = m_fdoCache.LangProject.TranslatedScriptureOA.Hvo;

			ISilDataAccess decorator;
			if (m_divisionPortion != PrintLayoutPortion.AllContent)
				decorator = new PrintLayoutDataByFlidDecorator(m_fdoCache, m_bookFilterInstance,
					m_divisionPortion == PrintLayoutPortion.TitleAndIntro);
			else
				decorator = new ScrBookFilterDecorator(m_fdoCache, m_bookFilterInstance);
			NLevelOwnerSvd ownerSvd = new NLevelOwnerSvd(2, decorator, hvoScripture);

			if (m_sharedStream == null)
			{
				FootnoteVc footnoteVc = new FootnoteVc(TeStVc.LayoutViewTarget.targetPrint,
					div.FilterInstance);
				footnoteVc.DefaultWs = DefaultWs;
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
		public virtual ISilDataAccess DataAccess
		{
			get
			{
				return new PrintLayoutDataByFlidDecorator(m_fdoCache, m_bookFilterInstance,
					m_divisionPortion == PrintLayoutPortion.TitleAndIntro);
			}
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
			ICmObject obj;
			if (m_fdoCache.ServiceLocator.ObjectRepository.TryGetObject(guid, out obj))
				return obj.Hvo;
			return 0;
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
				IPubDivision pubDiv = m_pub.DivisionsOS[0];
				int hvoHFSet = pubDiv.HFSetOA.Hvo;
				return new TeHeaderFooterConfigurer(m_fdoCache, hvoHFSet, DefaultWs,
					m_bookFilterInstance, m_printDateTime);
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
		/// When RootOnEachPage is true, this obtains the hvo of the root object under which the
		/// dependent object is stored
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int DependentRootHvo
		{
			get { return m_hvoBook; }
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
		public IDependentObjectsVc DependentRootVc
		{
			get
			{
				// Review JohnT: -1 is very arbitrary, based on assumption that we're not showing the
				// books in a footnote VC and hence won't be using the BooksTag. It will cause an
				// assertion if BooksTag is called.
				// This code is adapted from code elsewhere in this class that makes a FootnoteVc,
				// but which does a good bit of other stuff as well. Some of that may prove
				// needed in a dependent root as well, in which case, we may have to pass
				// a DivisionLayoutMgr to this routine.
				FootnoteVc vc = new FootnoteVc(TeStVc.LayoutViewTarget.targetPrint, -1);
				vc.DefaultWs = m_fdoCache.DefaultVernWs; // we never smush back translations.
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

		#region Filter Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks the given object agains the filter criteria
		/// </summary>
		/// <param name="hvoObj">ID of object to check against the filter criteria</param>
		/// <returns><c>true</c> if the object passes the filter criteria; otherwise
		/// <c>false</c></returns>
		/// ------------------------------------------------------------------------------------
		public bool MatchesCriteria(int hvoObj)
		{
			// Back translations don't distinguish between the sections, so return true
			// if hvoObj is a section
			if ((m_viewType & TeViewType.BackTranslation) != 0)
				return m_fdoCache.ServiceLocator.GetObject(hvoObj) is IScrSection;

			IScrSection section = m_fdoCache.ServiceLocator.GetObject(hvoObj) as IScrSection;

			return section != null && section.IsIntro == (m_divisionPortion == PrintLayoutPortion.TitleAndIntro);
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
			return ScrBookTags.kflidFootnotes;
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

		#region Private properties

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the default ws, depending on whether this is a vernacular or BT view
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private int DefaultWs
		{
			get
			{
				return ((m_viewType & TeViewType.BackTranslation) != 0 ?
					m_ws : m_fdoCache.DefaultVernWs);
			}
		}
		#endregion
	}
}
