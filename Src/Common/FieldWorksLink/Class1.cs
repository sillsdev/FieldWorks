using System;
using System. Diagnostics;
using SIL.FieldWorks.XWorks;
using SIL.FieldWorks.FdoUi;

namespace SIL.FieldWorks.Linking
{
	/// <summary>
	/// Summary description for Class1.
	/// </summary>
	class Class1
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] args)
		{
			Debug.Assert(args.Length == 1, "FwLink [url]");
			//Debug.Fail(args[0]);
			//Console.WriteLine(args[1]);
			Class1 c = new Class1();
			//c.Go("SilFw://link?app=LexText&tool=lexiconEdit&hvo=6111");
			c.Go(args[0]);
			//Console.WriteLine("Press Enter");
			//Console.ReadLine();
		}

		public void Go(string url)
		{
			try
			{
				FwLink.Activate(url);
			}
			catch (Exception error)
			{
				Console.WriteLine(error.Message);
				Console.WriteLine("Press Enter");
				Console.ReadLine();
			}
		}

	}
}
