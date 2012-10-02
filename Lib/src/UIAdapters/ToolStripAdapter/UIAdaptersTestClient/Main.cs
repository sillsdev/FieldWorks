using System;
using System.Windows.Forms;

namespace UIAdaptersTestClient
{
	/// <summary>
	/// Test program to aid in the development of SIBAdapter
	/// </summary>
	class MainClass
	{
		public static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new MainWindow());
		}
	}
}
