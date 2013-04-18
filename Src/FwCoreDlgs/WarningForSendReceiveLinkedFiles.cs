using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// When a user is trying to do a send/receive operation, warn them if the Linked Files folder is in a custom location so that they
	/// can stop the Send/Receive operation and change the Linked Files to the default location.
	/// </summary>
	public partial class WarningForSendReceiveLinkedFiles : Form
	{
		/// <summary>
		/// When a user is trying to do a send/receive operation, warn them if the Linked Files folder is in a custom location so that they
		/// can stop the Send/Receive operation and change the Linked Files to the default location.
		/// </summary>
		public WarningForSendReceiveLinkedFiles()
		{
			InitializeComponent();
		}

		private void btn_help_Click(object sender, EventArgs e)
		{
			MessageBox.Show(FwCoreDlgs.ksWarningForSendReceiveLinkedFileslesPathHelp, FwCoreDlgs.ksWarningForSendReceiveLinkedFileslesPathCaption);
		}
	}
}
