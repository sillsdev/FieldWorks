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
// File: EmptyTePrintLayoutConfigurer.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.PrintLayout;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Special PrintLayout Configurer for "showing" an empty print layout
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class EmptyTePrintLayoutConfigurer : IPrintLayoutConfigurer
	{
		#region Data members
		/// <summary></summary>
		protected FdoCache m_fdoCache;
		/// <summary></summary>
		protected IVwStylesheet m_styleSheet;
		/// <summary></summary>
		protected TeViewType m_viewType;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a EmptyTePrintLayoutConfigurer to configure the main print layout
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="viewType">Type of the view.</param>
		/// ------------------------------------------------------------------------------------
		public EmptyTePrintLayoutConfigurer(FdoCache cache, IVwStylesheet styleSheet,
			TeViewType viewType)
		{
			m_fdoCache = cache;
			m_styleSheet = styleSheet;
			m_viewType = viewType;
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
			return new EmptyTePrintLayoutVc();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// No-op
		/// </summary>
		/// <param name="div">The division layout manager</param>
		/// ------------------------------------------------------------------------------------
		public virtual void ConfigureSubordinateViews(DivisionLayoutMgr div)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level fragment for the main view (the one used to display
		/// MainObjectId).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int MainFragment
		{
			get { return (int)ScrFrags.kfrScripture; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the id of the top-level object that the main view displays.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual int MainObjectId
		{
			get { return m_fdoCache.LangProject.TranslatedScriptureOAHvo; }
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
			get { return null; }
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
			get { return 0; }
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
			get { return 0; }
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
			get { return null; }
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
			get { return false; }
		}
		#endregion
	}
}