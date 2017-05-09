using System;
using System.IO;
using SIL.FieldWorks.Common.FwKernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.HermitCrab;
using SIL.Machine.Annotations;
using SIL.Utils;

namespace GenerateHCConfig
{
	internal class Program
	{
		static int Main(string[] args)
		{
			if (args.Length < 2)
			{
				WriteHelp();
				return 0;
			}

			if (!File.Exists(args[0]))
			{
				Console.WriteLine("The FieldWorks project file could not be found.");
				return 1;
			}

			RegistryHelper.CompanyName = "SIL";
			Icu.InitIcuDataDir();
			var synchronizeInvoke = new SingleThreadedSynchronizeInvoke();
			var spanFactory = new ShapeSpanFactory();

			var projectId = new ProjectIdentifier(args[0]);
			var logger = new ConsoleLogger(synchronizeInvoke);
			var dirs = new NullFdoDirectories();
			var settings = new FdoSettings {DisableDataMigration = true};
			var progress = new NullThreadedProgress(synchronizeInvoke);
			Console.WriteLine("Loading FieldWorks project...");
			try
			{
				using (FdoCache cache = FdoCache.CreateCacheFromExistingData(projectId, "en", logger, dirs, settings, progress))
				{
					Language language = HCLoader.Load(spanFactory, cache, logger);
					Console.WriteLine("Loading completed.");
					Console.WriteLine("Writing HC configuration file...");
					XmlLanguageWriter.Save(language, args[1]);
					Console.WriteLine("Writing completed.");
				}
				return 0;
			}
			catch (FdoFileLockedException)
			{
				Console.WriteLine("Loading failed.");
				Console.WriteLine("The FieldWorks project is currently open in another application.");
				Console.WriteLine("Close the application and try to run this command again.");
				return 1;
			}
			catch (FdoDataMigrationForbiddenException)
			{
				Console.WriteLine("Loading failed.");
				Console.WriteLine("The FieldWorks project was created with an older version of FLEx.");
				Console.WriteLine("Migrate the project to the latest version by opening it in FLEx.");
				return 1;
			}
		}

		private static void WriteHelp()
		{
			Console.WriteLine("Generates a HermitCrab configuration file from a FieldWorks project.");
			Console.WriteLine();
			Console.WriteLine("generatehcconfig <input-project> <output-config>");
			Console.WriteLine();
			Console.WriteLine("  <input-project>  Specifies the FieldWorks project path.");
			Console.WriteLine("  <output-config>  Specifies the HC configuration path.");
		}
	}
}
