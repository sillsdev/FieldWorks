using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	/// <summary>
	/// Dialog shown in place of MessageBox when connection lost, so it can have an Exit button.
	/// </summary>
	public partial class ConnectionLostDlg : Form
	{
		/// <summary>
		/// Make one. Grrr.
		/// </summary>
		public ConnectionLostDlg()
		{
			InitializeComponent();
		}
	}
}
