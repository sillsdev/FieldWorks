// Copyright (c) 2008-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CommentToolTip.cs
// Responsibility: Lothers
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Widgets;
using System.Windows.Forms;
using System.Drawing;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.TE.TeEditorialChecks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class CommentToolTip : FwPopup
	{
		/// <summary>Handles the timer tick event.</summary>
		public event EventHandler TimerTick;

		#region Member variables
		private FwMultiParaTextBox m_resolutionText;
		private FwStyleSheet m_stylesheet;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="CommentToolTip"/> class.
		/// </summary>
		/// <param name="stylesheet">The stylesheet.</param>
		/// ------------------------------------------------------------------------------------
		public CommentToolTip(FwStyleSheet stylesheet)
		{
			m_stylesheet = stylesheet;
			BorderStyle = BorderStyle.FixedSingle;
		}
		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Makes the text box containing the specified text.
		/// </summary>
		/// <param name="text">The text.</param>
		/// ------------------------------------------------------------------------------------
		private void MakeTextBox(IStText text)
		{
			if (m_resolutionText != null)
			{
				Controls.Remove(m_resolutionText);
				m_resolutionText.Dispose();
			}

			Size = new Size(175, 1);
			m_resolutionText = new FwMultiParaTextBox(text, m_stylesheet);
			m_resolutionText.Dock = DockStyle.Fill;
			m_resolutionText.BackColor = SystemColors.Info;
			m_resolutionText.BorderStyle = BorderStyle.None;
			m_resolutionText.ReadOnly = true;
			m_resolutionText.AutoScroll = false;
			Controls.Add(m_resolutionText);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shows the specified comment text for the specified cell in the tooltip.
		/// </summary>
		/// <param name="comment">The comment to display in the tooltip.</param>
		/// <param name="cell">The status cell whose comment is being displayed.</param>
		/// ------------------------------------------------------------------------------------
		public void Show(IStText comment, DataGridViewCell cell)
		{
			Debug.Assert(cell != null);
			Debug.Assert(cell.DataGridView != null);

			MakeTextBox(comment);

			Rectangle rcCell = cell.DataGridView.GetCellDisplayRectangle(
				cell.ColumnIndex, cell.RowIndex, true);

			// Subtract one from the bottom so the tooltip
			// covers the grey border between grid rows.
			Point pt = new Point(rcCell.Right - Width, rcCell.Bottom - 1);
			base.Show(cell.DataGridView, pt);
		}
		#endregion

		#region Event handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Handles the Opened event of the popup.
		/// </summary>
		/// <param name="e"></param>
		/// ------------------------------------------------------------------------------------
		protected override void OnOpened(EventArgs e)
		{
			base.OnOpened(e);

			int i = m_resolutionText.GetEstimatedHeight();
			if (i > 300)
			{
				Width = 300;
				m_resolutionText.AdjustLayout();
				i = m_resolutionText.GetEstimatedHeight();
			}

			Height = Math.Min(300, i + 6);
		}
		#endregion

		#region Message handlers
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Fires when the timer Tick event occurs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnTimerTick()
		{
			base.OnTimerTick();

			if (TimerTick != null)
				TimerTick(this, EventArgs.Empty);
		}
		#endregion
	}
}
