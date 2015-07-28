// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// A warning dialog that we can't quite do with MessageBox because custom text is required for the "Yes" button.
	/// </summary>
	public partial class DeleteWritingSystemWarningDialog : Form
	{
		/// <summary>
		/// Grrr....make one.
		/// </summary>
		public DeleteWritingSystemWarningDialog()
		{
			InitializeComponent();
			warningIconBox.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Center;
			warningIconBox.BackgroundImage = System.Drawing.SystemIcons.Warning.ToBitmap();
		}

		internal void SetWsName(string name)
		{
			mainMessage.Text = string.Format(mainMessage.Text, name);
		}
	}
}
