// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AddRenderingDlg.cs
// ---------------------------------------------------------------------------------------------
using System;
using System.Windows.Forms;

namespace SILUBS.PhraseTranslationHelper
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class AddRenderingDlg : Form
	{
		private readonly Action<bool> m_selectKeyboard;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:AddRenderingDlg"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AddRenderingDlg(Action<bool> selectKeyboard)
		{
			m_selectKeyboard = selectKeyboard;
			InitializeComponent();
			if (selectKeyboard != null)
				selectKeyboard(true);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Raises the <see cref="E:System.Windows.Forms.Form.Closed"/> event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnClosed(EventArgs e)
		{
			m_selectKeyboard(false);
			 base.OnClosed(e);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the rendering.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Rendering
		{
			get { return DialogResult == DialogResult.OK ? m_txtRendering.Text : null; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the TextChanged event of the m_txtRendering control.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void m_txtRendering_TextChanged(object sender, System.EventArgs e)
		{
			btnOk.Enabled = !string.IsNullOrEmpty(m_txtRendering.Text);
		}
	}
}