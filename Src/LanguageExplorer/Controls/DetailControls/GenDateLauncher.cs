// Copyright (c) 2009-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// This is a button launcher that launches a generic date chooser.
	/// </summary>
	internal class GenDateLauncher : ButtonLauncher
	{
		private TextBox m_genDateTextBox;

		public GenDateLauncher()
		{
			InitializeComponent();
		}

		public void UpdateDisplayFromDatabase()
		{
			var genDate = (m_cache.DomainDataByFlid as ISilDataAccessManaged).get_GenDateProp(m_obj.Hvo, m_flid);
			m_genDateTextBox.Text = genDate.ToLongString();
		}

		protected override void HandleChooser()
		{
			using (var dlg = new GenDateChooserDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
			{
				dlg.Text = string.Format(DetailControlsStrings.ksFieldChooserDlgTitle, m_fieldName);
				var currentGenDate = m_cache.GetManagedSilDataAccess().get_GenDateProp(m_obj.Hvo, m_flid);
				// If we don't yet have a value, make today the default.
				if (currentGenDate.IsEmpty)
				{
					var now = DateTime.Now;
					currentGenDate = new GenDate(GenDate.PrecisionType.Exact, now.Month, now.Day, now.Year, true);
				}
				dlg.GenericDate = currentGenDate;
				if (dlg.ShowDialog(PropertyTable.GetValue<IWin32Window>(FwUtils.window)) != DialogResult.OK)
				{
					return;
				}
				var genDate = dlg.GenericDate;
				UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName), string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), m_obj, () =>
				{
					m_cache.GetManagedSilDataAccess().SetGenDate(m_obj.Hvo, m_flid, genDate);
				});
				m_genDateTextBox.Text = genDate.ToLongString();
			}
		}

		private void InitializeComponent()
		{
			this.m_genDateTextBox = new System.Windows.Forms.TextBox();
			this.m_panel.SuspendLayout();
			this.SuspendLayout();
			//
			// m_panel
			//
			this.m_panel.Size = new System.Drawing.Size(22, 20);
			//
			// m_genDateTextBox
			//
			this.m_genDateTextBox.BackColor = System.Drawing.SystemColors.Window;
			this.m_genDateTextBox.BorderStyle = System.Windows.Forms.BorderStyle.None;
			this.m_genDateTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
			this.m_genDateTextBox.Location = new System.Drawing.Point(0, 0);
			this.m_genDateTextBox.Name = "m_genDateTextBox";
			this.m_genDateTextBox.ReadOnly = true;
			this.m_genDateTextBox.Size = new System.Drawing.Size(150, 13);
			this.m_genDateTextBox.TabIndex = 0;
			//
			// GenDateLauncher
			//
			this.Controls.Add(this.m_genDateTextBox);
			this.MainControl = this.m_genDateTextBox;
			this.Name = "GenDateLauncher";
			this.Size = new System.Drawing.Size(150, 20);
			this.Controls.SetChildIndex(this.m_genDateTextBox, 0);
			this.Controls.SetChildIndex(this.m_panel, 0);
			this.m_panel.ResumeLayout(false);
			this.ResumeLayout(false);
			this.PerformLayout();

		}
	}
}