using System;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;


namespace FDOBrowser
{
	static class Program
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			// initialize ICU
			Icu.InitIcuDataDir();
			RegistryHelper.ProductName = "FieldWorks"; // inorder to find correct Registry keys
			using (var form = new FDOBrowserForm())
			{
				Application.Run(form);
			}
		}
	}
}