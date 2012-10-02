// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: AffixPositionAnalyzer.cs
// Responsibility: RandyR
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Windows.Forms;

using SIL.WordWorks.GAFAWS;
using SIL.WordWorks.GAFAWS.PlainWordlistConverter;
using SIL.WordWorks.GAFAWS.ANAConverter;
using SIL.WordWorks.GAFAWS.FWConverter;

namespace SIL.GAFAWS.Apps.AffixPositionAnalyzer
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public partial class AffixPositionAnalyzer : Form
	{
		private IGAFAWSConverter m_selectedConverter = null;
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default c'tor
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public AffixPositionAnalyzer()
		{
			InitializeComponent();

			// Load up the converters.
			IGAFAWSConverter converter = new PlainWordlistConverter();
			ListViewItem lvi = new ListViewItem(converter.Name);
			lvi.Tag = converter;
			m_lvConverters.Items.Add(lvi);
			lvi.Selected = true;

			converter = new ANAGAFAWSConverter();
			lvi = new ListViewItem(converter.Name);
			lvi.Tag = converter;
			m_lvConverters.Items.Add(lvi);

			converter = new FWConverter();
			lvi = new ListViewItem(converter.Name);
			lvi.Tag = converter;
			m_lvConverters.Items.Add(lvi);

			// TODO: Load up the other converters (FWConverter).
		}

		private void m_btnProcess_Click(object sender, EventArgs e)
		{
			if (m_selectedConverter == null)
				return;

			Cursor = Cursors.WaitCursor;
			try
			{
				m_selectedConverter.Convert();
			}
			catch
			{
				MessageBox.Show("There were problems with the original data, and it could not be processed.", "Information");
			}
			finally
			{
				Cursor = Cursors.Default;
			}
		}

		private void m_btnClose_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void m_lvConverters_SelectedIndexChanged(object sender, EventArgs e)
		{
			m_selectedConverter = null;
			if (m_lvConverters.SelectedItems.Count > 0)
			{
				m_selectedConverter = m_lvConverters.SelectedItems[0].Tag as IGAFAWSConverter;
				m_tbDescription.Text = m_selectedConverter.Description;
			}
		}

		private void m_lvConverters_DoubleClick(object sender, EventArgs e)
		{
			m_selectedConverter = null;
			if (m_lvConverters.SelectedItems.Count > 0)
			{
				m_selectedConverter = m_lvConverters.SelectedItems[0].Tag as IGAFAWSConverter;
				m_btnProcess.PerformClick();
			}
		}
	}
}