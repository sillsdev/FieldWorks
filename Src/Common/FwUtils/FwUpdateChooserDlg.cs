// Copyright (c) 2021-2021 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	// ReSharper disable LocalizableElement - Only testers will see this
	public partial class FwUpdateChooserDlg : Form
	{
		public FwUpdateChooserDlg()
		{
			InitializeComponent();
		}

		public FwUpdateChooserDlg(FwUpdate current, IEnumerable<FwUpdate> available) : this()
		{
			tbInstructions.Text =
				$"The following installers are available. You currently have {current} installed. " +
				"Additional patches may be available on the listed base (online and offline) installers. " +
				"To download an update, select it and click OK. To install base installers, you may need to uninstall first.";

			lbChooseVersion.Items.AddRange(available.ToArray());
		}

		public FwUpdate Choice => lbChooseVersion.SelectedItem as FwUpdate;
	}
}
