using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Palaso.Linq;

namespace SIL.FieldWorks.XWorks
{
	public partial class DictionaryConfigurationManagerDlg : Form
	{
		public DictionaryConfigurationManagerDlg()
		{
			InitializeComponent();

			// allow renaming via the keyboard
			configurationsListView.KeyUp += ConfigurationsListViewKeyUp;
			// Make the Configuration selection more obvious when the control loses focus (LT-15450).
			configurationsListView.LostFocus += OnLostFocus;
			configurationsListView.GotFocus += OnGotFocus;
		}

		private void OnGotFocus(object sender, EventArgs eventArgs)
		{
			if (configurationsListView.Items.Count < 1)
				return;
			var nonBoldFont = new Font(configurationsListView.Items[0].Font, FontStyle.Regular);
			configurationsListView.Items.Cast<ListViewItem>().ForEach(item => item.Font = nonBoldFont);
		}

		private void OnLostFocus(object sender, EventArgs eventArgs)
		{
			if (configurationsListView.Items.Count < 1)
				return;
			var boldFont = new Font(configurationsListView.Items[0].Font, FontStyle.Bold);
			configurationsListView.SelectedItems.Cast<ListViewItem>().ForEach(item => { item.Font = boldFont; });

		}

		private void ConfigurationsListViewKeyUp(object sender, KeyEventArgs e)
		{
			// Match Windows Explorer behaviour: allow renaming from the keyboard by pressing F2 or through the
			// "context menu" (since there is no context menu, go straight to rename from the "Application" key)
			if ((e.KeyCode == Keys.F2 || e.KeyCode == Keys.Apps) && configurationsListView.SelectedItems.Count == 1)
				configurationsListView.SelectedItems[0].BeginEdit();
		}
	}
}
