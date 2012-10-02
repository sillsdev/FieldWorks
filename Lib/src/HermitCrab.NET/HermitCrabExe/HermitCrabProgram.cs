using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

using NDesk.Options;

namespace SIL.HermitCrab
{
	class HermitCrabProgram
	{
		static void Main(string[] args)
		{
			string inputFormat = "xml";
			string inputFile = null;
			string outputFile = null;
			bool showHelp = false;
			bool quitOnError = true;

			OptionSet p = new OptionSet()
				.Add("f|input-format=", "the format of the input file {[xml|legacy]}, default: xml",
					delegate(string v) { inputFormat = v.ToLower(); })
				.Add("i|input-file=", "read configuration from {FILE}", delegate(string v) { inputFile = v; })
				.Add("o|output-file=", "write results to {FILE}", delegate(string v) {outputFile = v; })
				.Add("c|continue", "continues when an error occurs", delegate(string v) { quitOnError = v == null; })
				.Add("h|help", "show this help message and exit", delegate(string v) { showHelp = v != null; });

			try
			{
				p.Parse(args);
			}
			catch (OptionException)
			{
				ShowHelp(p);
				return;
			}

			if (showHelp || inputFile == null)
			{
				ShowHelp(p);
				return;
			}

			Loader loader = null;
			switch (inputFormat)
			{
				case "xml":
					loader = new XmlLoader();
					break;

				case "legacy":
					loader = new LegacyLoader();
					break;

				default:
					Console.WriteLine("Invalid input file format specified");
					ShowHelp(p);
					return;
			}

			try
			{
				if (outputFile != null)
					loader.Output = new DefaultOutput(new StreamWriter(new FileStream(outputFile, FileMode.Create), loader.DefaultOutputEncoding));
				else
					loader.Output = new DefaultOutput(Console.Out);

				loader.QuitOnError = quitOnError;

				loader.Load(inputFile);
			}
			catch (IOException ioe)
			{
				Console.WriteLine("IO Error: " + ioe.Message);
			}
			catch (LoadException le)
			{
				Console.WriteLine("Load Error: " + le.Message);
				if (le.InnerException != null)
					Console.WriteLine(le.InnerException.Message);
			}
			catch (MorphException me)
			{
				Console.WriteLine("Morph Error: " + me.Message);
			}

			loader.Output.Close();
		}

		static void ShowHelp(OptionSet p)
		{
			Console.WriteLine("Usage: hc [OPTIONS]");
			Console.WriteLine("HermitCrab.NET is a phonological and morphological parser.");
			Console.WriteLine();
			p.WriteOptionDescriptions(Console.Out);
		}
	}
}
