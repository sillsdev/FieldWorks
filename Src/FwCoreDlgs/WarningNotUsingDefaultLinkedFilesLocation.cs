using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Warn the user when they choose a custom location for linked files that Send/Receive will not send these files.
	/// </summary>
	public partial class WarningNotUsingDefaultLinkedFilesLocation : Form
	{
		/// <summary>
		/// Warn the user when they choose a custom location for linked files that Send/Receive will not send these files.
		/// </summary>
		public WarningNotUsingDefaultLinkedFilesLocation()
		{
			InitializeComponent();
		}

		private void btn_help_Click(object sender, EventArgs e)
		{

		}
	}
}
