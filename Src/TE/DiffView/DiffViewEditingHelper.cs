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
// File: DiffViewEditingHelper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;

using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Provide overrides needed for TeEditingHelper in the DiffView.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DiffViewEditingHelper : TeEditingHelper
	{
		#region Member variables
		private IScrBook m_book;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DiffViewEditingHelper"/> class.
		/// </summary>
		/// <param name="callbacks">The callbacks.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="filterInstance">The filter instance.</param>
		/// <param name="viewType">Type of the view.</param>
		/// <param name="book">The current book.</param>
		/// ------------------------------------------------------------------------------------
		public DiffViewEditingHelper(IEditingCallbacks callbacks, FdoCache cache,
			int filterInstance, TeViewType viewType, IScrBook book)
			: base(callbacks, cache, filterInstance, viewType)
		{
			m_book = book;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the current book.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <returns>the current book</returns>
		/// ------------------------------------------------------------------------------------
		protected override IScrBook GetCurrentBook(FdoCache cache)
		{
			return m_book;
		}
	}
}
