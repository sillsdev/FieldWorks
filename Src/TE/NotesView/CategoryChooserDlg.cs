// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2006, SIL International. All Rights Reserved.
// <copyright from='2006' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CategoryChooserDlg.cs
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
using SIL.FieldWorks.FwCoreDlgs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CategoryChooserDlg : FwChooserDlg
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CategoryChooserDlg"/> class.
		/// </summary>
		/// <param name="list">The list of categories used to populate the tree</param>
		/// <param name="initiallySelectedHvos">The sequence of HVOs of initially selected
		/// categories.</param>
		/// ------------------------------------------------------------------------------------
		public CategoryChooserDlg(ICmPossibilityList list, int[] initiallySelectedHvos) :
			base(list, initiallySelectedHvos, null, null)
		{
			lblInfo.Text = Properties.Resources.kstidCategoryChooserDlgInfoText;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the label control in which the tree control will display the names of the
		/// checked possibilities.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override Label SelectedPossibilitiesLabel
		{
			// We're not using this feature.
			get { return null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Display Help
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpScrNoteCategoriesChooser");
		}
	}
}
