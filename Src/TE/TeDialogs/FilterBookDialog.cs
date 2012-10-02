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
// File: FilterBookDialog.cs
// --------------------------------------------------------------------------------------------

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Controls;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Summary description for FilterBookDialog.
	/// </summary>
	public class FilterBookDialog : FilterScriptureDialog<IScrBook>
	{
		#region Constructor/Destructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FilterBookDialog"/> class.
		/// </summary>
		/// <param name="cache">The cache.</param>
		/// <param name="bookList">A list of books to check as an array</param>
		/// <param name="helpTopicProvider">The help topic provider.</param>
		/// ------------------------------------------------------------------------------------
		public FilterBookDialog(FdoCache cache, IScrBook[] bookList, IHelpTopicProvider helpTopicProvider)
			: base(cache, bookList, helpTopicProvider)
		{
			m_helpTopicId = "khtpBookFilter";
			Text = DlgResources.ResourceString("kstidBookFilterCaption");
		}
		#endregion
	}
}
