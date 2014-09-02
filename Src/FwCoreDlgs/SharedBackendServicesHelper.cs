using System;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Provides a common place for calls which utilize FDO's SharedBackendServices
	/// </summary>
	public static class SharedBackendServicesHelper
	{
		/// <summary>
		/// Display a warning indicating that it may be dangerous to change things in the dialog that
		/// is about to open when there are other applications using this project. The warning should only
		/// be shown if, in fact, other applications currently have this project open. Return true to continue,
		/// false to cancel opening the dialog.
		/// </summary>
		/// <returns></returns>
		public static bool WarnOnOpeningSingleUserDialog(FdoCache cache)
		{
			if (!SharedBackendServices.AreMultipleApplicationsConnected(cache))
				return true;
			string msg = Strings.ksWarnOnOpeningSingleAppDialog.Replace("\\n", Environment.NewLine);
			return ThreadHelper.ShowMessageBox(null, msg, Strings.ksOtherAppsUsingProjectCaption,
				MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK;
		}

		/// <summary>
		/// Display a warning indicating that it may be dangerous to change things that the user has just
		/// asked to change when other applications are using this project. The warning should only be shown
		/// if, in fact, other applications currently have this project open. Return true to continue, false
		/// to discard the changes. This is typically called in response to clicking an OK button in a dialog
		/// which changes dangerous user settings.
		/// </summary>
		/// <returns></returns>
		public static bool WarnOnConfirmingSingleUserChanges(FdoCache cache)
		{
			if (!SharedBackendServices.AreMultipleApplicationsConnected(cache))
				return true;
			string msg = Strings.ksWarnOnConfirmingSingleAppChanges.Replace("\\n", Environment.NewLine);
			return ThreadHelper.ShowMessageBox(null, msg, Strings.ksNotAdvisableOtherAppsUsingProjectCaption,
				MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
		}
	}
}
