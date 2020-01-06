// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// A warning dialog that we can't quite do with MessageBox because custom text is required for the "Yes" button.
	/// </summary>
	internal sealed partial class DeleteWritingSystemWarningDialog : Form
	{
		/// <summary />
		internal DeleteWritingSystemWarningDialog()
		{
			InitializeComponent();
			warningIconBox.BackgroundImageLayout = ImageLayout.Center;
			warningIconBox.BackgroundImage = System.Drawing.SystemIcons.Warning.ToBitmap();
		}

		internal void SetWsName(string name)
		{
			mainMessage.Text = string.Format(mainMessage.Text, name);
		}
	}
}