// Copyright (c) 2023 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Windows.Forms;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// This class wraps the HelpProvider class in order to gather analytics about what help topics are used
	/// </summary>
	public class FlexHelpProvider : HelpProvider
	{
		private Control _control;
		public override void SetShowHelp(Control ctl, bool shouldShowHelp)
		{
			_control = ctl;
			if (shouldShowHelp)
			{
				ctl.HelpRequested += Ctl_HelpRequested;
			}
			else
			{
				ctl.HelpRequested -= Ctl_HelpRequested;
			}
			base.SetShowHelp(ctl, shouldShowHelp);
		}

		private void Ctl_HelpRequested(object sender, HelpEventArgs args)
		{
			// This will be called twice, once before the help is handled, and once after.
			// We only want to track the help request once, so only do it after it is handled.
			if(sender is Control ctrl && args.Handled)
			{
				TrackingHelper.TrackHelpRequest(HelpNamespace, GetHelpKeyword(ctrl));
			}
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing && _control != null)
			{
				_control.HelpRequested -= Ctl_HelpRequested;
				_control = null;
			}
			base.Dispose(disposing);
		}
	}
}