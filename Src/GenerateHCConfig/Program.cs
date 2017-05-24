using System;
using System.IO;
using SIL.LCModel.Core.Text;
using SIL.LCModel;
using SIL.FieldWorks.WordWorks.Parser;
using SIL.HermitCrab;
using SIL.Machine.Annotations;
using SIL.LCModel.Utils;
using SIL.WritingSystems;

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

			Icu.InitIcuDataDir();
			Sldr.Initialize();
			var synchronizeInvoke = new SingleThreadedSynchronizeInvoke();
			var spanFactory = new ShapeSpanFactory();

			var projectId = new ProjectIdentifier(args[0]);
			var logger = new ConsoleLogger(synchronizeInvoke);
			var dirs = new NullFdoDirectories();
			var settings = new LcmSettings {DisableDataMigration = true};
			var progress = new NullThreadedProgress(synchronizeInvoke);
			Console.WriteLine("Loading FieldWorks project...");
			try
			{
				using (LcmCache cache = LcmCache.CreateCacheFromExistingData(projectId, "en", logger, dirs, settings, progress))
				{
					Language language = HCLoader.Load(spanFactory, cache, logger);
					Console.WriteLine("Loading completed.");
					Console.WriteLine("Writing HC configuration file...");
					XmlLanguageWriter.Save(language, args[1]);
					Console.WriteLine("Writing completed.");
				}
				return 0;
			}
			catch (LcmFileLockedException)
			{
				Console.WriteLine("Loading failed.");
				Console.WriteLine("The FieldWorks project is currently open in another application.");
				Console.WriteLine("Close the application and try to run this command again.");
				return 1;
			}
			catch (LcmDataMigrationForbiddenException)
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
