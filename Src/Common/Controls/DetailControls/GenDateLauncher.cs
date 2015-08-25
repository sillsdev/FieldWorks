using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// This is a button launcher that launches a generic date chooser.
	/// </summary>
	public class GenDateLauncher : ButtonLauncher
	{
		private System.Windows.Forms.TextBox m_genDateTextBox;

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
			using (var dlg = new GenDateChooserDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
			{
				dlg.Text = string.Format(DetailControlsStrings.ksFieldChooserDlgTitle, m_fieldName);
				GenDate x = (m_cache.DomainDataByFlid as ISilDataAccessManaged).get_GenDateProp(m_obj.Hvo, m_flid);
				// If we don't yet have a value, make today the default.
				if (x.IsEmpty)
				{
					DateTime now = DateTime.Now;
					x = new GenDate(GenDate.PrecisionType.Exact, now.Month, now.Day, now.Year, true);
				}
				dlg.GenericDate = x;
				if (dlg.ShowDialog(PropertyTable.GetValue<IWin32Window>("window")) == DialogResult.OK)
				{
					var genDate = dlg.GenericDate;
					UndoableUnitOfWorkHelper.Do(string.Format(DetailControlsStrings.ksUndoSet, m_fieldName),
						string.Format(DetailControlsStrings.ksRedoSet, m_fieldName), m_obj, () =>
					{
						(m_cache.DomainDataByFlid as ISilDataAccessManaged).SetGenDate(m_obj.Hvo, m_flid, genDate);
					});
					m_genDateTextBox.Text = genDate.ToLongString();
				}
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
