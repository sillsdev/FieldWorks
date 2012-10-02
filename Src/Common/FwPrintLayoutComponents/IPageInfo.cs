// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2004, SIL International. All Rights Reserved.
// <copyright from='2004' to='2004' company='SIL International'>
//		Copyright (c) 2004, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: IPageInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.Common.PrintLayout
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// The IPageInfo interface is used by view contructors that need information about the
	/// contents/context of a page. Real publication pages (i.e., the Page class) should
	/// implement this, but it can also be implemented to simulate a page where this info is
	/// needed in a dialog, etc.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public interface IPageInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets/sets the page number of the page
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int PageNumber
		{
			get;
			set;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the number of pages in the publication that this page is a part of.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		int PageCount
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets which side the publication is bound on: left, right or top.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		BindingSide PublicationBindingSide
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a selection to the top of page in the main stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		SelectionHelper TopOfPageSelection
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a selection to the bottom of page in the main stream.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		SelectionHelper BottomOfPageSelection
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating how sheets are laid out for this page's publication:
		/// simplex, duplex, etc.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		MultiPageLayout SheetLayout
		{
			get;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the publication control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		IRootSite Publication
		{
			get;
		}
	}
}
