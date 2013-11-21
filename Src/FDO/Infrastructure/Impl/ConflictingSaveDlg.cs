using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// This dialog is like a message box, but offers two buttons, OK and "Refresh Now", which
	/// when clicked produces a DialogResult of 'Yes'.
	/// </summary>
	public partial class ConflictingSaveDlg : Form
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public ConflictingSaveDlg()
		{
			InitializeComponent();
			pictureBox1.BackgroundImage = SystemIcons.Warning.ToBitmap();
			pictureBox1.Size = SystemIcons.Warning.Size;
		}
	}
}
