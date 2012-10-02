using System;

using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FwCoreDlgs;

namespace RunAddConverter
{
	/// <summary>
	/// Summary description for Form1.
	/// </summary>
	public class AddConverterDlgLauncher
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			using (AddCnvtrDlg dlg = new AddCnvtrDlg(FwApp.App, null))
			{
				dlg.ShowDialog();
			}
		}
	}
}
