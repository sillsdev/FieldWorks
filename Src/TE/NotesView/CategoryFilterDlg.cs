// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CategoryFilterDlg.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.COMInterfaces;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CategoryFilterDlg : Form
	{
		private IHelpTopicProvider m_helpTopicProvider;
		private FdoCache m_cache;
		private ICmFilter m_filter;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private CategoryFilterDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CategoryFilterDlg(FdoCache cache, IHelpTopicProvider helpTopicProvider,
			ICmFilter filter) : this()
		{
			m_cache = cache;
			m_helpTopicProvider = helpTopicProvider;
			m_filter = filter;

			tvCategories.Load(cache, filter, null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display Help
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, System.EventArgs e)
		{
			ShowHelp.ShowHelpTopic(m_helpTopicProvider, "khtpCategoryFilterChooser");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void btnOk_Click(object sender, EventArgs e)
		{
			using (NonUndoableUnitOfWorkHelper undoHelper = new NonUndoableUnitOfWorkHelper(
				m_cache.ServiceLocator.GetInstance<IActionHandler>()))
			{
				m_filter.ColumnInfo = string.Empty;
				m_filter.RowsOS[0].CellsOS.Clear();
				tvCategories.UpdateFilter(m_filter);

				undoHelper.RollBack = false;
			}
		}
	}
}
