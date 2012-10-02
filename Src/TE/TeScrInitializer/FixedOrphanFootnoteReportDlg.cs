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
// File: FixedOrphanFootnoteReportDlg.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FwCoreDlgs;
using XCore;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Message box displayed when TE has detected problems with orphaned ORC and/or footnotes.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class FixedOrphanFootnoteReportDlg : FwUpdateReportDlg
	{
		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FixedOrphanFootnoteReportDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FixedOrphanFootnoteReportDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FixedOrphanFootnoteReportDlg"/> class.
		/// </summary>
		/// <param name="issues">List of styles that the user has modified.</param>
		/// <param name="projectName">Name of the project.</param>
		/// <param name="helpTopicProvider">context sensitive help</param>
		/// ------------------------------------------------------------------------------------
		public FixedOrphanFootnoteReportDlg(List<string> issues, string projectName,
			IHelpTopicProvider helpTopicProvider) :
			base(issues, projectName, helpTopicProvider)
		{
			InitializeComponent();
			RepeatTitleOnEveryPage = true;
			RepeatColumnHeaderOnEveryPage = false;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the help topic key.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override string HelpTopicKey
		{
			get { return "khtpProblemsWithFootnotesOrPicturesDialog"; }
		}
	}
}