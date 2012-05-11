using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks.LexText
{
	public partial class Db4oSendReceiveDialog : Form
	{
		public Db4oSendReceiveDialog()
		{
			InitializeComponent();
			SetDialogCaption();
			SetDialogMessage();
		}

		public void SetDialogCaption()
		{
			Text = LexTextStrings.ksDb4oProjectNotShareableTitle;
		}

		public void SetDialogMessage()
		{
			var lineArray = new string[4];
			lineArray[0] = LexTextStrings.ksDb4oProjectNotShareableTextLine1;
			lineArray[1] = "";
			lineArray[2] = LexTextStrings.ksDb4oProjectNotShareableTextLine2;
			lineArray[3] = LexTextStrings.ksClickOK;
			m_dialogText.Lines = lineArray;
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			this.DialogResult = DialogResult.Abort; // report to caller that user clicked link
			Close();
		}
	}
}
