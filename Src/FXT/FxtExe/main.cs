// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using System.Collections.Generic;
using System.Xml;
using FDOBrowser;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Resources;
using SIL.Utils;

namespace SIL.FieldWorks.Common.FXT
{
	/// <summary>
	/// Summary description for main.
	/// </summary>
	public class main
	{
		/// <summary>
		/// The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main(string[] arguments)
		{
			// <summary>
			// any filters that we want, for example, to only output items which satisfy their constraint.
			// </summary>
			IFilterStrategy[] filters=null;

			if (arguments.Length < 3)
			{
				Console.WriteLine("usage: fxt dbName fxtTemplatePath xmlOutputPath (-guids)");
				Console.WriteLine("");
				Console.WriteLine("example using current directory: fxt TestLangProj WebPageSample.xhtml LangProj.xhtml");
				Console.WriteLine("example with environment variables: fxt ZPU \"%fwroot%/distfiles/fxtTest.fxt\" \"%temp%/fxtTest.xml\"");
				return;
			}


			string fxtPath = System.Environment.ExpandEnvironmentVariables(arguments[1]);
			if(!File.Exists(fxtPath))
			{
				Console.WriteLine("could not find the file "+fxtPath);
				return;
			}

			string outputPath = System.Environment.ExpandEnvironmentVariables(arguments[2]);

			FdoCache cache = null;
			try
			{
				Console.WriteLine("Initializing cache...");
				var bepType = GetBEPTypeFromFileExtension(fxtPath);
				var isMemoryBEP = bepType == FDOBackendProviderType.kMemoryOnly;
				var threadHelper = new ThreadHelper();
				var consoleProj = new ConsoleProgress();

				if (isMemoryBEP)
					cache = FdoCache.CreateCacheWithNewBlankLangProj(new BrowserProjectId(bepType, null), "en", "en", "en", threadHelper);
				else
				{
					using (var progressDlg = new ProgressDialogWithTask(consoleProj))
					{
						cache = FdoCache.CreateCacheFromExistingData(new BrowserProjectId(bepType, fxtPath), "en", progressDlg);
					}
				}
			}
			catch (Exception error)
			{
				Console.WriteLine(error.Message);
				return;
			}
			//try
			//{
			//    Console.WriteLine("Initializing cache...");
			//    Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			//    cacheOptions.Add("db", arguments[0]);
			//    cache = FdoCache.Create(cacheOptions);
			//}
			//catch (Exception error)
			//{
			//    Console.WriteLine(error.Message);
			//    return;
			//}

			Console.WriteLine("Beginning output...");
			DateTime dtstart = DateTime.Now;
			XDumper d = new XDumper(cache);
			if (arguments.Length == 4)
			{
				if(arguments[3] == "-parserDump")
				{
					filters = new IFilterStrategy[]{new ConstraintFilterStrategy()};
				}
				else
					//boy do we have a brain-dead argument parser in this app!
					System.Diagnostics.Debug.Assert(arguments[3] == "-guids");
				d.OutputGuids = true;
			}
			try
			{
				d.Go(cache.LangProject as ICmObject, fxtPath, File.CreateText(outputPath), filters);

				//clean up, add the <?xml tag, etc. Won't be necessary if/when we make the dumper use an xmlwriter instead of a textwriter
				//was introducing changes such as single quote to double quote				XmlDocument doc=new XmlDocument();
				//				doc.Load(outputPath);
				//				doc.Save(outputPath);
			}
			catch (Exception error)
			{
				if (cache != null)
					cache.Dispose();

				Console.WriteLine(error.Message);
				return;
			}


			TimeSpan tsTimeSpan = new TimeSpan(DateTime.Now.Ticks - dtstart.Ticks);

			Console.WriteLine("Finished: " + tsTimeSpan.TotalSeconds.ToString() + " Seconds");

			if(outputPath.ToLower().IndexOf("fxttestout") > -1)
				System.Diagnostics.Debug.WriteLine(File.OpenText(outputPath).ReadToEnd());

			if (cache != null)
				cache.Dispose();

			System.Diagnostics.Debug.WriteLine("Finished: " + tsTimeSpan.TotalSeconds.ToString() + " Seconds");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the BEP type from the specified file path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private static FDOBackendProviderType GetBEPTypeFromFileExtension(string pathname)
		{
			switch (Path.GetExtension(pathname).ToLower())
			{
				default:
					return FDOBackendProviderType.kMemoryOnly;
				case FwFileExtensions.ksFwDataXmlFileExtension:
					return FDOBackendProviderType.kXML;
				case FwFileExtensions.ksFwDataDb4oFileExtension:
					return FDOBackendProviderType.kDb4oClientServer;

			}
		}
	}
}
