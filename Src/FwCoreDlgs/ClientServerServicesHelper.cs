﻿// Copyright (c) 2013-2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Provides a common place for calls which utilize FDO's ClientServerServices
	/// </summary>
	public static class ClientServerServicesHelper
	{
		/// <summary>
		/// Display a warning indicating that it may be dangerous to change things in the dialog that
		/// is about to open when other users are connected. The warning should only be shown if, in fact,
		/// other users are currently connected. The dialog may contain some information about the other
		/// users that are connected. Return true to continue, false to cancel opening the dialog.
		/// </summary>
		/// <returns></returns>
		public static bool WarnOnOpeningSingleUserDialog(FdoCache cache)
		{
			var others = ClientServerServices.Current.CountOfOtherUsersConnected(cache);
			if (others == 0)
				return true;
			var msg = string.Format(Strings.ksWarnOnOpeningSingleUserDialog.Replace("\\n", Environment.NewLine), others);
			return ThreadHelper.ShowMessageBox(null, msg, Strings.ksOthersConnectedCaption,
				MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK;
		}

		/// <summary>
		/// Display a warning indicating that it may be dangerous to change things that the user has just
		/// asked to change when other users are connected. The warning should only be shown if, in fact,
		/// other users are currently connected. The dialog may contain some information about the other
		/// users that are connected. Return true to continue, false to discard the changes. This is typically
		/// called in response to clicking an OK button in a dialog which changes dangerous user settings.
		/// </summary>
		/// <returns></returns>
		public static bool WarnOnConfirmingSingleUserChanges(FdoCache cache)
		{
			var others = ClientServerServices.Current.CountOfOtherUsersConnected(cache);
			if (others == 0)
				return true;
			var msg = string.Format(Strings.ksWarnOnConfirmingSingleUserChanges.Replace("\\n", Environment.NewLine), others);
			return ThreadHelper.ShowMessageBox(null, msg, Strings.ksNotAdvisableOthersConnectedCaption,
				MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
		}
	}
}