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
// File: HFDialogsPageInfo.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// This is the page info that is used by the Header/Footer view constructor
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public class HFDialogsPageInfo : IPageInfo
	{
		private int m_pageNumber = 1;
		private bool m_IsLeftBound;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new HFDialogsPageInfo.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HFDialogsPageInfo(bool isLeftBound)
		{
			m_IsLeftBound = isLeftBound;
		}

		#region IPageInfo implementation
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the page number that this page info represents.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageNumber
		{
			get {return m_pageNumber;}
			set { m_pageNumber = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of pages in the publication (just a dummy value for editing)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int PageCount
		{
			get { return 20; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets which side the publication is bound on: left, right or top.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BindingSide PublicationBindingSide
		{
			get
			{
				return m_IsLeftBound ? BindingSide.Left : BindingSide.Right;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a selection to the top of page in the main stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper TopOfPageSelection
		{
			get {return null;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a selection to the bottom of page in the main stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public SelectionHelper BottomOfPageSelection
		{
			get {return null;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating how sheets are laid out for this page's publication:
		/// simplex, duplex, etc. In the dialog, we always give the appearance of duplex layout
		/// in our preview.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public MultiPageLayout SheetLayout
		{
			get { return MultiPageLayout.Duplex; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IRootSite Publication
		{
			get { return null; }
		}
		#endregion
	}
}
