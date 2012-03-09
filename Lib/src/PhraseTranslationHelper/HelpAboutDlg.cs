// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2012, SIL International. All Rights Reserved.
// <copyright from='2012' to='2012' company='SIL International'>
//		Copyright (c) 2012, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: HelpAboutDlg.cs
// ---------------------------------------------------------------------------------------------
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class HelpAboutDlg : Form
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:Form1"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public HelpAboutDlg()
		{
			InitializeComponent();
		}

		protected override void OnVisibleChanged(System.EventArgs e)
		{
			base.OnVisibleChanged(e);
			if (Visible)
				m_txlInfo.StartMarqueeScrolling();
		}
	}
}