// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwStylesModifiedDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Message box displayed when the stylesheet has been modified to notify the user
	/// that they may want to check their styles.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FwStylesModifiedDlg : FwUpdateReportDlg
	{
		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for FwStylesModifiedDlg
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwStylesModifiedDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for FwStylesModifiedDlg
		/// </summary>
		/// <param name="modifiedStyles">List of styles that the user has modified.</param>
		/// <param name="projectName">Name of the project.</param>
		/// <param name="helpTopicProvider">context sensitive help</param>
		/// ------------------------------------------------------------------------------------
		public FwStylesModifiedDlg(List<string> modifiedStyles, string projectName,
			IHelpTopicProvider helpTopicProvider) :
			base(modifiedStyles, projectName, helpTopicProvider)
		{
			InitializeComponent();
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help topic key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string HelpTopicKey
		{
			get { return "khtpModifiedStylesDialog"; }
		}
	}
}