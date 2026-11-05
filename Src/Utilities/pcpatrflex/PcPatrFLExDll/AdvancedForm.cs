using SIL.DisambiguateInFLExDB;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SIL.PcPatrFLEx
{
	public partial class AdvancedForm : Form
	{
		public int MaxAmbigs { get; set; }
		public int TimeLimit { get; set; }
		public bool RunIndividually { get; set; }
		public bool OkPressed { get; set; }
		public ShowFailures ShowFailures { get; set; }

		public AdvancedForm()
		{
			InitializeComponent();
		}

		public void Initialize(int max, int time, bool runIndividually, ShowFailures showFailures)
		{
			MaxAmbigs = max;
			TimeLimit = time;
			tbMaxAmbiguities.Text = max.ToString();
			tbTimeLimit.Text = time.ToString();
			cbRunIndividually.Checked = runIndividually;
			OkPressed = false;
			ShowFailures = showFailures;
			UpdateShowFailuresChecked();
		}

		private void tbMaxAmbiguities_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (Char.IsDigit(e.KeyChar) || e.KeyChar == '\b')
			{
				e.Handled = false;
			}
			else
			{
				e.Handled = true;
			}
		}

		private void tbTimeLimit_KeyPress(object sender, KeyPressEventArgs e)
		{
			if (Char.IsDigit(e.KeyChar) || e.KeyChar == '\b')
			{
				e.Handled = false;
			}
			else
			{
				e.Handled = true;
			}
		}

		private void cbRunIndividually_CheckedChanged(object sender, EventArgs e)
		{
			RunIndividually = cbRunIndividually.Checked;
		}

		private void btnOK_Click(object sender, EventArgs e)
		{
			MaxAmbigs = Convert.ToInt32(tbMaxAmbiguities.Text);
			TimeLimit = Convert.ToInt32(tbTimeLimit.Text);
			OkPressed = true;
			this.Close();
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			OkPressed = false;
			this.Close();
		}

		private void tbMaxAmbiguities_TextChanged(object sender, EventArgs e)
		{

		}

		private void rbShowFailuresOff_CheckedChanged(object sender, EventArgs e)
		{
			UpdateShowFailuresValue();
		}

		private void rbShowFailuresPhraseStructureRules_CheckedChanged(object sender, EventArgs e)
		{
			UpdateShowFailuresValue();

		}

		private void rbShowFailuresOn_CheckedChanged(object sender, EventArgs e)
		{
			UpdateShowFailuresValue();

		}
		private void UpdateShowFailuresValue()
		{
			if (rbShowFailuresOff.Checked)
				ShowFailures = ShowFailures.Off;
			else if (rbShowFailuresPhraseStructureRules.Checked)
				ShowFailures = ShowFailures.PSROnly;
			else
				ShowFailures = ShowFailures.On;
		}
		private void UpdateShowFailuresChecked()
		{
			if (ShowFailures == ShowFailures.Off)
				rbShowFailuresOff.Checked = true;
			else if (ShowFailures == ShowFailures.PSROnly)
				rbShowFailuresPhraseStructureRules.Checked = true;
			else
				rbShowFailuresOn.Checked = true;
		}
	}
}
