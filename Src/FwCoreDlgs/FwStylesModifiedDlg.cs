// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwStylesModifiedDlg.cs
// Responsibility: TE Team

using System.Collections.Generic;
using SIL.CoreImpl;
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
