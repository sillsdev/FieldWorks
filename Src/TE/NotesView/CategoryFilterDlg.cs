// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CategoryFilterDlg.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CategoryFilterDlg : Form
	{
		private FdoCache m_cache;
		private ICmFilter m_filter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CategoryFilterDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CategoryFilterDlg(FdoCache cache, ICmFilter filter) : this()
		{
			m_cache = cache;
			m_filter = filter;

			string listName =
				cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA.Name.UserDefaultWritingSystem;

			tvCategories.Load(cache, filter, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display Help
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpCategoryFilterChooser");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, EventArgs e)
		{
			m_filter.ColumnInfo = string.Empty;
			m_filter.RowsOS[0].CellsOS.RemoveAll();
			tvCategories.UpdateFilter(m_filter);
		}
	}
}
