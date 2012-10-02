using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.Widgets;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class CheckingErrorCommentDlg : Form
	{
		private CheckingError m_error;
		private FwMultiParaTextBox m_text;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckingErrorCommentDlg()
		{
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public CheckingErrorCommentDlg(CheckingError error, IVwStylesheet stylesheet) : this()
		{
			System.Diagnostics.Debug.Assert(error != null);
			m_text = new FwMultiParaTextBox(error.ResolutionOA, stylesheet);
			m_text.Dock = DockStyle.Fill;
			pnlTextBox.Controls.Add(m_text);
			m_error = error;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnFormClosing(FormClosingEventArgs e)
		{
			base.OnFormClosing(e);

			if (DialogResult == DialogResult.Cancel)
				return;

			FdoOwningSequence<IStPara> oldParas = m_error.ResolutionOA.ParagraphsOS;
			ITsString[] newParas = m_text.Paragraphs;

			// If there are fewer paragraphs in the new comment, then remove from the end
			// of the old comment the number paragraphs that is the difference between
			// the number of old and new paragraphs.
			if (newParas.Length < oldParas.Count)
			{
				for (int i = oldParas.Count - 1; i >= newParas.Length; i--)
					oldParas.RemoveAt(i);
			}

			for (int i = 0; i < newParas.Length; i++)
			{
				if (i < oldParas.Count)
				{
					// Reuse the old paragraph.
					((StTxtPara)oldParas[i]).Contents.UnderlyingTsString = newParas[i];
				}
				else
				{
					// Create a new paragraph
					StTxtPara newStPara = new StTxtPara();
					oldParas.Append(newStPara);
					newStPara.Contents.UnderlyingTsString = newParas[i];
					ITsPropsFactory tspf = TsPropsFactoryClass.Create();
					newStPara.StyleRules = tspf.MakeProps(ScrStyleNames.Remark, m_text.CurrentWs, 0);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Click event of the btnHelp control.
		/// </summary>
		/// <param name="sender">The source of the event.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		/// ------------------------------------------------------------------------------------
		private void btnHelp_Click(object sender, EventArgs e)
		{
			ShowHelp.ShowHelpTopic(FwApp.App, "khtpAnnotationForIgnoredInconsistency");
		}
	}
}
