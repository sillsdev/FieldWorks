// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FilterBookDialog.cs
// --------------------------------------------------------------------------------------------

using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// <summary>
	/// Summary description for FilterBookDialog.
	/// </summary>
	public class FilterBookDialog : FilterAllTextsDialog<IScrBook>
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

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Load the books of Scripture
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void LoadTexts()
		{
			m_treeTexts.LoadScriptureTexts(m_cache, null);
		}
	}
}
