using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks
{
	/// <summary>
	/// Dialog used to ask whether we can send information to developers. DialogResult will be OK (send it) or Abort (don't).
	/// </summary>
	public partial class DeveloperReportingWarningDialog : Form
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public DeveloperReportingWarningDialog()
		{
			InitializeComponent();
		}

		private void m_doNotSendLink_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			DialogResult = DialogResult.Abort;
			Close();
		}
	}
}
