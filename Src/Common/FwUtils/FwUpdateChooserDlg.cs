// Copyright (c) 2021-2023 SIL International
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
			// FwUpdate.ToString does not always include the base build number (which could be informative) but does include the installer type,
			// even though we have no way of knowing what that was for the current version; define our own relevant parts here.
			tbInstructions.Text =
				$"The following installers are available. You currently have {current.Version}_b{current.BaseBuild} built {current.Date:yyyy-MM-dd} installed" +
				" (date is invalid for FLEx Bridge). Additional patches may be available on the listed base (online and offline) installers. " +
				"To download an update, double-click, or select it and click Download; another dialog will appear when the download is complete. " +
				$"To install FieldWorks base installers, you may need to uninstall FLEx and then install manually from {FwDirectoryFinder.DownloadedUpdates}.";

			// ReSharper disable once CoVariantArrayConversion
			lbChooseVersion.Items.AddRange(available.ToArray());
		}

		public FwUpdate Choice => lbChooseVersion.SelectedItem as FwUpdate;

		private void lbChooseVersion_DoubleClick(object sender, System.EventArgs e)
		{
			DialogResult = DialogResult.OK;
		}
	}
}
